# Reading — Errors & Status Codes (V1 Async Projections)

## Purpose

This document defines public status-code and error behavior for the Reading module.

Reading serves ordinary public requests from Reading-owned asynchronous projections:

```text
ArticleReadModel
ArticleSeoRouteProjection
```

Reading APIs must prioritize:

```text
Safe public exposure
Bounded latency
Stable public response contracts
Safe degradation of optional enrichments
No leakage of hidden article existence
No synchronous fallback to upstream source modules in ordinary public reads
```

Core rule:

```text
Locally missing, denied or unsafe route/visibility state
    -> fail closed using safe public not-found behavior.

Missing or delayed optional media/counter enrichment
    -> return degraded success when core article visibility is safe.
```

Reading V1 accepts bounded eventual-consistency lag before a newer upstream change reaches and is applied to Reading.

---

## 1. Standard Error Envelope

Reading follows the system-wide public error envelope.

Reference:

```text
../../02-contracts-and-standards.md
```

Expected shape:

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

Rules:

```text
Errors must include a stable machine-readable code.

Public error messages must not reveal hidden resource existence.

CorrelationId should be returned where available.

Internal projection, event, dependency and visibility diagnostics
must not be exposed publicly.
```

---

## 2. Public Status-Code Mapping

| Status | Meaning | Reading behavior |
|---|---|---|
| `200 OK` | Successful public response | Returned when safely public Reading projection state is available. Optional media/counters may be missing, defaulted or stale. |
| `400 Bad Request` | Invalid client request | Invalid paging, filter, search, route input or unsupported sort values. |
| `404 Not Found` | Safe public denial | Article/route projection missing, locally non-public, locally unsafe, route inactive or route requires resync. |
| `429 Too Many Requests` | Request rate limited | Applied by public API/edge rate-limit policy. |
| `500 Internal Server Error` | Unexpected defect | Unhandled failure; must not disclose internal details. |
| `503 Service Unavailable` | Required Reading serving capability unavailable | Reading projection store or required serving infrastructure is unavailable, preventing safe handling of requests. |

### Status Selection Rule

```text
A specific article or route cannot be served safely from available local state
    -> 404.

Reading cannot access required serving infrastructure at service level
    -> 503.

Only optional enrichment is absent or delayed
    -> 200 with safe degradation.
```

---

## 3. Public Not-Found Posture

Reading must not disclose whether a hidden article exists upstream or inside internal projection state.

Public detail requests should normally return:

```text
404 + READING.NOT_FOUND
```

for all of the following cases:

```text
ArticleReadModel is missing.

ArticleReadModel.Status is not Published.

ArticleReadModel.IsPublic is false.

Article visibility is locally unsafe or requires resync.

ArticleSeoRouteProjection is missing for slug request.

Slug route is inactive.

Slug route requires resync.

Slug route points to a missing ArticleReadModel.

Slug route points to an article that fails local public visibility checks.
```

Recommended public response:

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

### Internal Reason Codes

Internal logs or operational diagnostics may distinguish:

```text
ArticleProjectionMissing
ArticleNotPublished
ArticleNotPublic
ArticleVisibilityUnsafe
ArticleVisibilityRequiresResync
RouteProjectionMissing
RouteInactive
RouteRequiresResync
RouteTargetProjectionMissing
RouteTargetNotPublic
```

These internal reasons must not appear as separate public article-existence disclosures.

---

## 4. Bounded Eventual-Consistency Clarification

Reading follows Content and SEO asynchronously.

Therefore, upstream state may have changed before Reading has applied the newer projection snapshot.

Examples:

```text
Content published an article, but Reading has not received the new public projection yet.
    -> Request may temporarily return safe 404.

SEO activated a new slug route, but Reading has not received the route projection yet.
    -> Slug request may temporarily return safe 404.

Content unpublished an article, but Reading has not received the newer non-public projection yet.
    -> Reading may temporarily still hold prior local public state within accepted bounded lag.

SEO deactivated a route, but Reading has not received the route-deactivation projection yet.
    -> Reading may temporarily still hold prior active route state within accepted bounded lag.
```

Rules:

```text
Reading evaluates requests from current local projection state.

Once local state is known denied or unsafe, Reading must fail closed.

Reading must not synchronously call source modules to eliminate normal propagation lag.

Visibility and route lag must be monitored against SLOs.

Repair/reconciliation applies when drift or prolonged lag is detected.
```

---

## 5. Validation Errors

Invalid client-controlled input returns:

```text
400 Bad Request
```

Examples:

```text
page < 1
pageSize < 1
pageSize exceeds the configured maximum
unsupported sort value
invalid categoryPublicId
invalid tagPublicId
missing search query q
invalid search query format
invalid related-article limit
invalid articlePublicId or slug format where validation policy applies
```

### V1 Sort Validation

Supported list/search sort values:

```text
-publishedAt
publishedAt
```

The following are not supported in V1:

```text
-popularity
popularity
```

Popularity/trending sorting is deferred beyond V1.

### Example Response

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

## 6. Degraded Success

Reading should return:

```text
200 OK
```

with degraded optional response data when:

```text
Core article projection exists.
Local public visibility passes.
Required slug route state passes for slug-based requests.
Only optional enrichment is absent, delayed or stale.
```

Optional enrichments in V1 include:

```text
Cover/media presentation
SEO response metadata that is not required for route resolution
Interaction counters
Related article result state
Optional search presentation fields
```

### Safe Degradation Table

| Missing or delayed data | Allowed public behavior |
|---|---|
| Interaction counter snapshot has never arrived | Return documented zero/default counters |
| Interaction counter snapshot is behind current Interaction truth | Return last-known counters |
| Cover media missing | Return `cover: null` or omit cover according to response standard |
| Media gallery missing | Return empty array or omit according to response standard |
| Optional SEO metadata missing | Return null/omitted SEO fields |
| Related state missing | Return empty result or deterministic fallback |
| Optional search enrichment missing | Omit safely |

Rules:

```text
Optional enrichment absence must not turn a public article into 404.

Optional enrichment presence must not make a hidden article public.

Reading must not synchronously query upstream modules to fill missing optional fields.
```

---

## 7. Required Reading Serving Failures

## 7.1. Specific Resource Cannot Be Confirmed Public Locally

Return:

```text
404 Not Found
```

Examples:

```text
Requested article projection is absent.
Requested article projection is locally non-public.
Requested slug route projection is absent or inactive.
Requested route/article state is locally unsafe.
```

Reason:

```text
The API can safely deny this specific public resource
without revealing internal details.
```

## 7.2. Required Reading Infrastructure Is Unavailable

Return:

```text
503 Service Unavailable
```

Examples:

```text
Reading projection database is unavailable.
Required query infrastructure cannot execute requests.
Required local serving state cannot be accessed because of infrastructure failure.
```

Example response:

```json
{
  "error": {
    "code": "READING.SERVICE_UNAVAILABLE",
    "message": "The service is temporarily unavailable.",
    "details": [],
    "correlationId": "string"
  }
}
```

## 7.3. Optional Enrichment Infrastructure Is Unavailable

Do not return `503` merely because:

```text
Counter enrichment is unavailable.
Media enrichment is absent.
Related enrichment is unavailable.
Optional SEO presentation metadata is missing.
```

When core public state is safe, prefer:

```text
200 OK with documented degradation.
```

---

## 8. Article Listing Errors

For:

```http
GET /api/v1/articles
```

| Case | Status | Behavior |
|---|---:|---|
| Valid request with no public articles | `200` | Return empty `items` list |
| Invalid paging/filter/sort | `400` | Return validation error |
| Optional counters/media missing | `200` | Return safe degraded items |
| Reading projection query store unavailable | `503` | Cannot serve list safely |
| Locally non-public candidate rows encountered | `200` | Exclude those rows |

Rules:

```text
List responses must not include locally denied or unsafe articles.

List requests do not synchronously query upstream source modules.

Popularity sort requests are invalid in V1.
```

---

## 9. Article Detail by Public ID Errors

For:

```http
GET /api/v1/articles/{articlePublicId}
```

| Case | Status | Public behavior |
|---|---:|---|
| Public article projection exists and is safe | `200` | Return detail |
| Article projection missing | `404` | `READING.NOT_FOUND` |
| Article locally non-public | `404` | `READING.NOT_FOUND` |
| Article visibility locally unsafe | `404` | `READING.NOT_FOUND` |
| Optional media/counters missing | `200` | Return degraded detail |
| Reading projection infrastructure unavailable | `503` | `READING.SERVICE_UNAVAILABLE` |

Public response must not distinguish:

```text
missing article
hidden article
unpublished article
archived article
deleted article
unsafe article projection
```

---

## 10. Article Detail by Slug Errors

For:

```http
GET /api/v1/articles/slug/{slug}
```

Reading uses:

```text
ArticleSeoRouteProjection
    -> ArticleReadModel
```

It does not synchronously call SEO `/resolve` in the ordinary V1 request path.

| Case | Status | Public behavior |
|---|---:|---|
| Active safe route and public article projection exist | `200` | Return detail |
| Route projection missing | `404` | `READING.NOT_FOUND` |
| Route inactive | `404` | `READING.NOT_FOUND` |
| Route requires resync | `404` | `READING.NOT_FOUND` |
| Route target article projection missing | `404` | `READING.NOT_FOUND` |
| Route target locally non-public/unsafe | `404` | `READING.NOT_FOUND` |
| Optional media/counter/SEO metadata missing | `200` | Return degraded detail |
| Reading route/article projection infrastructure unavailable | `503` | `READING.SERVICE_UNAVAILABLE` |

Rules:

```text
Public clients must not receive distinct route-versus-article hidden-state errors.

Canonical redirect behavior remains deferred to its later ADR.

Missing local route state is not resolved through synchronous SEO fallback in V1.
```

---

## 11. Search Errors

For:

```http
GET /api/v1/articles/search
```

### Missing Query

If `q` is missing:

```text
400 Bad Request
READING.SEARCH_QUERY_REQUIRED
```

### Invalid Query

Invalid or policy-disallowed query input may return:

```text
400 Bad Request
READING.INVALID_SEARCH_QUERY
```

or:

```text
400 Bad Request
READING.SEARCH_QUERY_TOO_SHORT
```

if a minimum length policy is adopted.

### Search Serving Failure

| Situation | Status | Behavior |
|---|---:|---|
| Valid query with no matches | `200` | Empty result |
| Optional result enrichment missing | `200` | Safe degraded results |
| Reading-owned search projection/query unavailable | `503` | `READING.SEARCH_UNAVAILABLE` |
| Candidate item locally denied/unsafe | `200` | Omit candidate |

Rules:

```text
Search reads Reading-owned public projection/search state only.

Search must not expose locally known hidden content.

External search integration is outside V1.
```

---

## 12. Related Articles Errors

For:

```http
GET /api/v1/articles/{articlePublicId}/related
```

| Case | Status | Behavior |
|---|---:|---|
| Parent article locally public; related items available | `200` | Return public related items |
| Parent article missing/non-public/unsafe | `404` | `READING.NOT_FOUND` |
| Invalid `limit` | `400` | `READING.INVALID_LIMIT` |
| Related state unavailable but parent is public | `200` | Return empty list or deterministic fallback |
| Optional related item media/counters missing | `200` | Return degraded related items |
| Required Reading serving store unavailable | `503` | `READING.SERVICE_UNAVAILABLE` |

Rules:

```text
Related failure must not invalidate an independently successful article detail response.

Related results must exclude the current article.

Only locally safe public related articles may be returned.

Popularity-based related ranking is not part of V1.
```

---

## 13. Counter and View-Contribution Error Posture

## 13.1. Counters Displayed by Reading

Reading consumes versioned counter snapshots from Interaction:

```text
interaction.article_counters_projection_published
```

Displayed fields may include:

```json
{
  "counters": {
    "views": 123,
    "likes": 45,
    "visibleComments": 6
  }
}
```

Rules:

```text
Counter truth belongs to Interaction.

Reading returns last-known or default counter values.

Counter snapshot delay does not cause Reading detail/list failure.

Reading does not synchronously query Interaction for fresher values.

Reading does not expose counter freshness/version internals publicly in V1.
```

## 13.2. View Contribution

Reading does not expose view mutation endpoints.

The client may separately send a view contribution request to Interaction after article rendering.

If that Interaction request fails:

```text
The previously successful Reading response remains valid.

Counters may remain unchanged or catch up later.

Interaction owns error codes, rate limiting, retry and abuse behavior
for the view contribution endpoint.
```

---

## 14. Public Error Codes

### Publicly Returned General Codes

| Code | Typical Status | Meaning |
|---|---:|---|
| `READING.VALIDATION_FAILED` | `400` | Request query or route input validation failed. |
| `READING.NOT_FOUND` | `404` | Article cannot be served publicly from safe local Reading state. |
| `READING.RATE_LIMITED` | `429` | Public request rate limit exceeded. |
| `READING.SERVICE_UNAVAILABLE` | `503` | Required Reading serving infrastructure is unavailable. |
| `READING.INTERNAL_ERROR` | `500` | Unexpected internal failure. |

### Publicly Returned Query Validation Codes

| Code | Typical Status | Meaning |
|---|---:|---|
| `READING.INVALID_PAGE` | `400` | `page` is invalid. |
| `READING.INVALID_PAGE_SIZE` | `400` | `pageSize` is invalid or exceeds configured maximum. |
| `READING.INVALID_SORT_FIELD` | `400` | `sort` is not supported in V1. |
| `READING.INVALID_FILTER` | `400` | A filter value is invalid. |
| `READING.INVALID_LIMIT` | `400` | Related-item limit is invalid. |
| `READING.SEARCH_QUERY_REQUIRED` | `400` | Search query `q` is required. |
| `READING.INVALID_SEARCH_QUERY` | `400` | Search query is invalid. |
| `READING.SEARCH_QUERY_TOO_SHORT` | `400` | Search query is shorter than adopted minimum policy. |
| `READING.SEARCH_UNAVAILABLE` | `503` | Required Reading-owned search capability is unavailable. |

---

## 15. Internal-Only Diagnostic Codes

The following reasons may be recorded internally but must normally collapse to safe public codes:

| Internal reason | Public response |
|---|---|
| `READING.ARTICLE_PROJECTION_MISSING` | `404 / READING.NOT_FOUND` |
| `READING.ARTICLE_NOT_PUBLIC` | `404 / READING.NOT_FOUND` |
| `READING.ARTICLE_VISIBILITY_UNSAFE` | `404 / READING.NOT_FOUND` |
| `READING.ARTICLE_VISIBILITY_REQUIRES_RESYNC` | `404 / READING.NOT_FOUND` |
| `READING.ROUTE_PROJECTION_MISSING` | `404 / READING.NOT_FOUND` |
| `READING.ROUTE_INACTIVE` | `404 / READING.NOT_FOUND` |
| `READING.ROUTE_REQUIRES_RESYNC` | `404 / READING.NOT_FOUND` |
| `READING.ROUTE_TARGET_MISSING` | `404 / READING.NOT_FOUND` |
| `READING.OPTIONAL_MEDIA_DEGRADED` | No hard error; metric/log only |
| `READING.OPTIONAL_COUNTERS_DEFAULTED` | No hard error; metric/log only |
| `READING.PROJECTION_STORE_UNAVAILABLE` | `503 / READING.SERVICE_UNAVAILABLE` |

Removed from V1 public/error posture:

```text
READING.FALLBACK_UNAVAILABLE
```

Reason:

```text
Reading V1 does not synchronously fall back to upstream modules
during ordinary public serving.
```

---

## 16. Public vs Internal Error Detail

Public errors must not expose:

```text
Whether a hidden article exists.
Whether a draft/unpublished/archived/deleted article exists.
Whether a route points to hidden content.
Which projection row exists internally.
Why a route requires resync.
Which source event has or has not applied.
Which source version is current.
Whether broker/outbox/consumer lag caused a public miss.
```

Internal logs may record:

```text
CorrelationId
ArticlePublicId
Slug
Route scope
Internal deny reason
ContentSourceVersion
SeoSourceVersion
MediaSourceVersion
InteractionStatsVersion
Last source message identifiers
RequiresResync marker/reason
Consumer apply decision
Dependency name
Lag/freshness metrics
```

---

## 17. Retry Guidance for Public Clients

Public clients may retry:

| Status | Retry posture |
|---|---|
| `503` | May retry using bounded client policy/backoff |
| `429` | Retry only after server/client rate-limit policy allows |
| Network timeout | Retry according to client request policy |
| `400` | Do not blindly retry without correcting input |
| `404` | Do not blindly retry as part of normal API behavior |

A newly published article or newly activated slug may temporarily return `404` due to async projection lag, but the public API must not disclose internal propagation details as part of the response contract.

---

## 18. Non-Goals

This document does not define:

```text
Reading consumer retry algorithm
RabbitMQ retry/dead-letter behavior
Outbox publication state
Stored procedure result enums
Repair/rebuild scheduling
Projection freshness SLO threshold values
Canonical redirect behavior
Emergency/operator-controlled upstream fallback
```

These belong to:

```text
06-idempotency-consistency.md
07-observability-slos.md
09-open-questions.md
Worker/runtime implementation documents
Future ADRs where required
```

---

## 19. V1 Error Posture Summary

| Concern | V1 behavior |
|---|---|
| Missing/non-public/unsafe local article projection | Safe `404` |
| Missing/inactive/unsafe local slug route projection | Safe `404` |
| Reading required projection store unavailable | `503` |
| Invalid query or unsupported popularity sort | `400` |
| Media missing/delayed | Degraded `200` |
| Interaction counters missing/delayed | Degraded `200` with default/last-known counters |
| Client view contribution fails in Interaction | No effect on successful Reading response |
| Upstream change not yet projected locally | Bounded lag accepted; local state governs response |
| Sync SEO/Content/Media/Interaction fallback | Not used in ordinary V1 public serving |
| Public disclosure of internal deny reason | Forbidden |