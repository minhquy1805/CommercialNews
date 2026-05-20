# Reading — Security & Abuse Controls (V1)

## Purpose

Reading APIs are public-facing and scrape-prone.

This document defines security and abuse controls for public Reading endpoints.

Reading must prioritize:

* safe public exposure
* no leakage of non-public content
* bounded query cost
* rate-limit friendliness
* cache safety
* safe logging
* graceful degradation under abuse or dependency pressure

---

## 1) Public exposure rules

Reading public endpoints must only expose publicly visible content.

Public visibility requires:

* source-derived status is `Published`
* projection `IsPublic = true`
* article is not archived
* article is not soft-deleted
* slug is active if accessed by slug
* visibility is not uncertain

If visibility is uncertain:

```text
Unknown visibility => not public.
Safe 404 is preferred over incorrect exposure.
```

---

## 2) Non-public content protection

Reading must not expose:

* draft articles
* unpublished articles
* archived articles
* soft-deleted articles
* admin-only fields
* audit fields
* internal lifecycle history
* internal projection diagnostics
* source aggregate versions in public responses
* internal event/message identifiers
* internal visibility reason codes

Public clients must not be able to distinguish:

* article does not exist
* article exists but is draft
* article exists but unpublished
* article exists but archived
* article exists but soft-deleted
* slug points to hidden content
* projection exists but visibility is uncertain

These cases should usually collapse into:

```text
READING.NOT_FOUND
```

---

## 3) Slug and ID enumeration protection

Slug and public id endpoints are enumeration-prone.

Endpoints affected:

```text
GET /api/v1/articles/{articlePublicId}
GET /api/v1/articles/slug/{slug}
```

Rules:

* return safe `404` for non-public or unknown resources
* do not reveal internal reason in public error body
* do not expose internal database ids
* use public identifiers only
* avoid response timing differences that obviously reveal hidden resources
* rate limit repeated misses from the same client/network where possible

Internal logs may record richer reason codes, but public responses must remain safe.

---

## 4) Query abuse controls

List and search endpoints are scrape-prone.

Affected endpoints:

```text
GET /api/v1/articles
GET /api/v1/articles/search
GET /api/v1/articles/{articlePublicId}/related
```

Required controls:

* enforce maximum `pageSize`
* enforce maximum `limit`
* reject invalid paging values
* reject unsupported sort fields
* reject unsupported filter fields
* bound keyword search length
* reject empty required search query
* define minimum search query length if needed
* avoid unbounded wildcard scans
* use allowlisted sort values only

Recommended V1 defaults:

| Setting | Default |
|---|---|
| `page` | `1` |
| `pageSize` | `20` |
| `pageSize` max | policy-defined |
| related `limit` | `6` |
| related `limit` max | policy-defined |

---

## 5) Search abuse posture

Search can be expensive and easily abused.

Controls:

* require `q`
* reject overly short or overly long queries according to policy
* normalize whitespace
* avoid exposing internal scoring details
* do not return non-public content from stale search/materialized indexes
* rate limit high-frequency search requests
* monitor unusual search patterns

If search backend or search projection is degraded:

* fall back only where policy allows
* return safe omission or `503` where needed
* never expose non-public content due to stale search state

---

## 6) Rate limiting and edge protection

Reading endpoints should be protected by edge/API-level rate limiting.

Recommended dimensions:

* IP address
* authenticated user id if available
* anonymous client fingerprint where policy allows
* route group
* search-heavy endpoints separately from normal detail reads

Suggested route groups:

| Route group | Examples | Abuse risk |
|---|---|---|
| Listing | `/articles` | scraping |
| Detail | `/articles/{articlePublicId}` | enumeration |
| Slug detail | `/articles/slug/{slug}` | slug probing |
| Search | `/articles/search` | expensive query abuse |
| Related | `/articles/{articlePublicId}/related` | scraping / graph crawling |

Rate limiting must not reveal whether hidden content exists.

---

## 7) Caching safety

Cache is acceleration only.

Cache must not become hidden truth.

Rules:

* cache hit must not bypass source-derived visibility
* stale cache must not expose unpublished, archived, or soft-deleted content
* cache keys must not include sensitive tokens
* public cache entries must only contain public-safe fields
* cache TTL must be bounded
* cache invalidation may lag, but visibility-sensitive stale data must fail closed where detected
* cache refresh failure must not break a safe projection-backed response

If cache data conflicts with projection visibility:

```text
Projection visibility wins.
```

If projection visibility is uncertain:

```text
Fail closed unless explicit fallback confirms visibility safely.
```

---

## 8) Projection safety

Reading projection is derived state.

Security rules:

* projection must store only public-safe serving fields
* projection diagnostics must not be returned publicly
* projection freshness metadata should be internal-only
* projection lag must not leak non-public content
* stale projection must not override source-derived visibility rules
* rebuild/reconciliation output must not be exposed before safe publication/cutover when correctness matters

Public responses should not expose:

* `SourceVersion`
* `LastEventMessageId`
* `LastSourceOccurredAtUtc`
* `LastSyncedAtUtc`
* internal visibility reason
* internal projection status

These fields are for diagnostics and observability, not public API contracts.

---

## 9) Safe degradation under dependency pressure

Reading should degrade safely when optional enrichments are unavailable.

Optional enrichments:

* counters
* media gallery
* cover media
* SEO metadata
* related article signals
* popularity/trending signals

Allowed behavior:

* omit optional fields
* return null optional fields
* return empty arrays
* return safe defaults
* return stale non-sensitive enrichments if policy allows

Not allowed:

* exposing non-public content
* making visibility permissive because an enrichment is missing
* returning internal dependency errors to public clients
* blocking public reads indefinitely waiting for optional enrichments

---

## 10) View tracking abuse boundary

Reading does not expose interaction write endpoints in V1.

View tracking is owned by Interaction.

Recommended public flow:

```text
Reading returns article detail
    ↓
Client sends view signal to Interaction
    ↓
Interaction handles dedupe, counting, rate limits, and abuse policy
```

Reading rules:

* Reading success must not depend on view tracking success
* Reading must not duplicate Interaction abuse logic
* Reading counters may lag
* view tracking failure must not affect article visibility

Interaction module must define:

* duplicate view handling
* bot filtering policy
* rate limiting
* aggregation/recompute posture

---

## 11) Logging rules

Reading logs must support investigation without leaking sensitive data.

Recommended log fields:

* `correlationId`
* route template
* status code
* latency
* client category if available
* `articlePublicId` when provided
* slug hash or normalized slug where policy allows
* visibility decision category
* fallback used
* degradation reason
* cache hit/miss
* projection freshness category
* dependency degraded flag

Avoid logging:

* full article body
* full search query if privacy policy disallows it
* sensitive headers
* cookies
* authorization tokens
* internal exception details in public logs
* large response bodies

For search queries, prefer:

* normalized query length
* coarse query category
* hash where useful
* sampled logging for debugging

---

## 12) Metrics and abuse signals

Reading should expose metrics for:

* request count by endpoint
* latency by endpoint
* error rate by endpoint
* `404` rate by endpoint
* rate-limit count
* search request rate
* invalid query count
* `pageSize`/`limit` rejection count
* slug miss rate
* article public id miss rate
* cache hit/miss rate
* fallback rate
* degraded response count
* projection unavailable count
* visibility uncertain count
* omitted enrichment count

Abuse indicators:

* high slug miss rate
* high public id miss rate
* high search request rate from same client/network
* repeated max-page scraping
* repeated high page number crawling
* high invalid query rate
* unusual route distribution
* high related-article crawling

---

## 13) Response hardening

Public Reading responses should:

* use stable public identifiers
* avoid internal ids where possible
* avoid internal status reason codes
* avoid internal timestamps unrelated to public display
* avoid projection/debug metadata
* avoid exposing stack traces or dependency names
* maintain consistent safe `404` behavior

For public not-found responses, use generic wording:

```json
{
  "error": {
    "code": "READING.NOT_FOUND",
    "message": "The requested article was not found.",
    "details": [],
    "correlationId": "string"
  }
}
```

---

## 14) Authorization posture

Reading public endpoints generally do not require authentication.

However:

* admin preview must not be handled by public Reading endpoints unless separately designed
* draft preview must require explicit authorization if introduced later
* personalized recommendations require privacy and authorization review before V1 adoption
* user-specific reading history must not be mixed into public anonymous responses without explicit policy

V1 public Reading endpoints should remain anonymous-safe.

---

## 15) Bot and crawler posture

Reading should expect legitimate crawlers and abusive scrapers.

Recommended controls:

* edge caching for public list/detail where safe
* robots policy at website layer
* rate limiting for high-volume clients
* separate monitoring for known crawlers if needed
* bounded pagination
* safe canonical URLs
* stable cache headers where policy allows

Crawler support must not weaken visibility correctness.

---

## 16) Incident posture

During abuse, scraping, or dependency degradation:

Prefer:

* rate limiting
* cache serving where safe
* degraded optional enrichments
* safe `404`
* safe omission
* temporary `503` for required projection outage

Do not prefer:

* exposing stale non-public content
* disabling visibility checks
* increasing page size limits
* bypassing projection safety
* leaking internal state to aid debugging

---

## 17) Non-goals

This document does not define:

* Interaction anti-fraud implementation
* full bot detection system
* WAF vendor configuration
* SEO crawler policy details
* admin preview authorization
* personalized recommendation privacy model
* internal Worker retry/DLQ behavior

Those belong to their owning modules or infrastructure docs.
