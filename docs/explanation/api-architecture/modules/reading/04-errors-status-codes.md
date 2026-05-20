# Reading — Errors & Status Codes (V1)

## Purpose

This document defines the public error and status-code behavior for the Reading module.

Reading APIs are public-facing and must prioritize:

* safe public exposure
* bounded latency
* graceful degradation
* stable response contracts
* no leakage of draft, unpublished, archived, soft-deleted, or visibility-uncertain content

Reading must prefer safe `404` or safe omission over incorrect public exposure.

---

## 1) Standard error envelope

Reading follows the system-wide error envelope.

Reference:

```text
../../02-contracts-and-standards.md
```

Expected shape:

```json
{
  "error": {
    "code": "READING.NOT_FOUND",
    "message": "The requested resource was not found.",
    "details": [],
    "correlationId": "string"
  }
}
```

Rules:

* error responses must include a stable machine-readable code
* public messages must be safe and must not reveal whether non-public content exists
* correlation id should be included where available
* internal dependency details must not be exposed to public clients

---

## 2) Status code mapping

| Status | Meaning | Reading behavior |
|---|---|---|
| `200` | Success | Returned when public content can be safely served. Optional enrichments may be stale, omitted, or defaulted. |
| `400` | Invalid request | Invalid query parameters, invalid paging, unsupported sort/filter values. |
| `404` | Safe not found | Resource does not exist, is not public, is archived, is soft-deleted, slug is inactive, or visibility is uncertain. |
| `408` | Request timeout | Optional if API gateway/framework maps request timeout explicitly. Prefer bounded internal timeouts and safe failure. |
| `429` | Rate limited | Edge/API rate limiting for public endpoints. |
| `500` | Unexpected server error | Rare. Used for unexpected bugs or unhandled failures. Must not leak internal details. |
| `503` | Temporarily unavailable | Used when Reading cannot serve safely due to required projection/storage dependency being unavailable and no safe fallback exists. |

---

## 3) Public visibility error posture

Reading must fail closed for public visibility.

The following cases should return safe `404`:

* article public id not found
* slug not found
* article exists but is not public
* source-derived status is not `Published`
* projection `IsPublic` is false
* article is archived
* article is soft-deleted
* slug is inactive or not mapped to a public article
* projection visibility is uncertain
* projected route exists but visibility does not pass

Public `404` responses must not reveal whether the resource exists internally.

Recommended public message:

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

Internal logs may record the real reason, such as:

* `NotFound`
* `NotPublic`
* `Archived`
* `SoftDeleted`
* `SlugInactive`
* `VisibilityUncertain`
* `ProjectionMissing`

These internal reasons must not be exposed directly to public clients.

---

## 4) Validation errors

Invalid client-controlled query values should return `400`.

Examples:

* `page < 1`
* `pageSize < 1`
* `pageSize` exceeds maximum allowed value
* unsupported `sort`
* invalid `categoryId`
* invalid `tagId`
* missing required search query `q`
* invalid `limit` for related articles

Example response:

```json
{
  "error": {
    "code": "READING.VALIDATION_FAILED",
    "message": "The request contains invalid query parameters.",
    "details": [
      {
        "field": "sort",
        "code": "READING.INVALID_SORT_FIELD",
        "message": "The selected sort field is not supported."
      }
    ],
    "correlationId": "string"
  }
}
```

---

## 5) Degraded success

Reading should return `200` with degraded content when:

* core public article data is available
* source-derived projection visibility is public
* optional enrichments are missing, stale, or unavailable

Optional enrichments include:

* counters
* cover media
* media gallery
* SEO metadata
* related article signals
* popularity/trending signals

Examples of safe degradation:

| Missing or stale data | Allowed behavior |
|---|---|
| Counters | return zero, null, omitted counters, or future `countersPartial = true` |
| Cover media | return null cover, omitted cover, or placeholder according to API policy |
| Media gallery | return empty array |
| SEO metadata | return projected defaults or null fields |
| Related signals | return empty list or deterministic fallback |
| Popularity score | fall back to published date sort if policy allows |

Degradation must not make visibility more permissive.

Degradation must not expose non-public content.

---

## 6) Dependency and projection failures

Reading distinguishes required data from optional enrichments.

### 6.1 Required projection unavailable

If Reading cannot access required projection data needed to determine safe public visibility, return:

```text
503 Temporarily Unavailable
```

Or return safe `404` if the specific request cannot be verified and policy requires fail-closed behavior.

Use `503` when the service-level dependency is unavailable.

Use `404` when a specific resource cannot be safely confirmed as public.

### 6.2 Optional enrichment unavailable

If optional enrichment is unavailable, prefer degraded `200` if the core article/list response is safe.

Do not return `500` or `503` merely because optional enrichments are delayed or missing.

### 6.3 Projection lag

Projection lag after publish may produce:

* `404` for newly published article not yet projected
* missing item from article list
* stale counters or enrichments

Projection lag must not produce public exposure of content that is known or suspected to be non-public.

### 6.4 Stale projection

If a projection is known to be stale for visibility-sensitive data:

* fail closed
* return safe `404`
* or use explicit policy-controlled fallback

Do not serve stale public visibility with confidence.

---

## 7) Search errors

### Missing query

`GET /articles/search` without `q` should return `400`.

Error code:

```text
READING.SEARCH_QUERY_REQUIRED
```

### Invalid query

Invalid or too-short query may return `400` depending on API policy.

Error codes:

```text
READING.INVALID_SEARCH_QUERY
READING.SEARCH_QUERY_TOO_SHORT
```

### Search backend unavailable

If search uses Reading projection and projection is available, return normal results.

If a future external search backend is unavailable:

* fall back to Reading projection where policy allows
* otherwise return `503`
* never expose non-public content from stale search index

---

## 8) Related articles errors

For `GET /articles/{articlePublicId}/related`:

| Case | Status |
|---|---|
| Parent article not found or not public | `404` |
| Invalid `limit` | `400` |
| Related signals unavailable but parent is public | `200` with empty list or deterministic fallback |
| Related projection unavailable and no fallback exists | `200` empty list or `503` depending on policy |

Recommended V1 behavior:

* if parent article is public, related failures should not fail article detail
* standalone related endpoint may return empty result if related data is unavailable
* non-public related candidates must be filtered out

---

## 9) Slug resolution errors

For `GET /articles/slug/{slug}`:

| Case | Status |
|---|---|
| Slug not found | `404` |
| Slug exists but target is not public | `404` |
| Slug exists but projection visibility is uncertain | `404` |
| Slug exists but canonical redirect policy is unavailable | `200` or `404` depending on safe serving policy |
| SEO fallback unavailable and Reading projection cannot verify public visibility | `404` or `503` depending on scope |

Public clients should not receive internal distinction between:

* slug missing
* target article unpublished
* target article archived
* target article soft-deleted
* projection visibility uncertain

All should be safe not found.

---

## 10) View tracking errors

Reading does not expose interaction write endpoints in V1.

View tracking is owned by Interaction.

If the client sends view signal to Interaction and it fails:

* Reading response remains unaffected
* counters may lag
* Interaction defines retry, dedupe, rate limit, and abuse behavior

Reading must not return failure for article detail because view tracking failed.

---

## 11) Error codes

### General

| Code | Meaning |
|---|---|
| `READING.VALIDATION_FAILED` | Request validation failed. |
| `READING.NOT_FOUND` | Resource is not found or is not publicly visible. |
| `READING.SERVICE_UNAVAILABLE` | Required Reading dependency or projection store is unavailable. |
| `READING.INTERNAL_ERROR` | Unexpected internal failure. |
| `READING.RATE_LIMITED` | Request was rate limited. |

### Query validation

| Code | Meaning |
|---|---|
| `READING.INVALID_PAGE` | `page` is invalid. |
| `READING.INVALID_PAGE_SIZE` | `pageSize` is invalid or exceeds max. |
| `READING.INVALID_SORT_FIELD` | Unsupported sort value. |
| `READING.INVALID_FILTER` | Filter parameter is invalid. |
| `READING.INVALID_LIMIT` | Related article limit is invalid. |

### Search

| Code | Meaning |
|---|---|
| `READING.SEARCH_QUERY_REQUIRED` | Search query `q` is required. |
| `READING.INVALID_SEARCH_QUERY` | Search query is invalid. |
| `READING.SEARCH_UNAVAILABLE` | Search backend or materialized search projection is unavailable. |

### Slug/detail

| Code | Meaning |
|---|---|
| `READING.SLUG_NOT_FOUND` | Internal-only or safe-public alias to `READING.NOT_FOUND`. |
| `READING.ARTICLE_NOT_PUBLIC` | Internal-only reason; public response should usually use `READING.NOT_FOUND`. |
| `READING.VISIBILITY_UNCERTAIN` | Internal-only reason; public response should usually use `READING.NOT_FOUND`. |

### Dependency / degraded behavior

| Code | Meaning |
|---|---|
| `READING.DEPENDENCY_DEGRADED` | Optional dependency/enrichment is degraded. Usually logged/observed, not returned as hard error. |
| `READING.PROJECTION_UNAVAILABLE` | Required projection data unavailable. |
| `READING.PROJECTION_STALE` | Projection is known stale beyond policy. Public response depends on visibility risk. |
| `READING.FALLBACK_UNAVAILABLE` | Explicit fallback path was required but unavailable. |

---

## 12) Public vs internal error details

Public APIs should avoid revealing:

* whether a draft exists
* whether an unpublished article exists
* whether a slug points to hidden content
* whether an archived article exists
* whether a soft-deleted article exists
* whether a projection row exists internally

Public response should usually collapse these into:

```text
READING.NOT_FOUND
```

Internal logs may include richer details for investigation.

Recommended internal fields:

* `correlationId`
* `articlePublicId`
* `slug`
* `visibilityState`
* `projectionVersion`
* `sourceVersion`
* `lastSyncedAtUtc`
* `dependencyName`
* `fallbackUsed`
* `degradationReason`

---

## 13) Retry guidance

Reading public clients may retry:

* `503`
* `429` after the indicated retry window
* network timeouts according to client policy

Reading public clients should not blindly retry:

* `400`
* `404`

A `404` for a newly published article may be caused by projection lag, but the API must not expose internal lag details publicly.

---

## 14) Non-goals

This document does not define:

* Worker retry behavior for Reading consumers
* RabbitMQ DLQ policy
* Outbox publication status codes
* database stored procedure result codes
* internal projection apply result enums

Those belong to:

* `06-idempotency-consistency.md`
* `07-observability-slos.md`
* Worker/runtime implementation docs
