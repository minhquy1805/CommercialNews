# Reading — Security & Abuse Controls (V1 Async Projections)

## Purpose

Reading APIs are public-facing, anonymous-safe and scrape-prone.

This document defines security and abuse controls for public Reading endpoints served from Reading-owned asynchronous projections.

Reading must prioritize:

```text
Safe public exposure
No leakage of hidden content existence
Bounded query cost
Rate-limit friendliness
Local route and visibility safety
Cache safety
Safe logging
Graceful degradation of optional enrichment
No hidden synchronous dependency on source modules
```

Reading serves ordinary public requests from:

```text
ArticleReadModel
ArticleSeoRouteProjection
```

Source truth remains owned by:

| Concern | Source owner |
|---|---|
| Article lifecycle and public visibility truth | Content |
| Canonical slug and route truth | SEO |
| Media asset and presentation truth | Media |
| Engagement, moderation and counter truth | Interaction |

---

## 1. Public Exposure Rules

Reading public endpoints may expose an article only when local Reading projection state confirms that serving is safe.

### Detail or list visibility condition

```text
ArticleReadModel.Status = Published
AND ArticleReadModel.IsPublic = true
AND local article visibility is not unsafe / requires-resync
```

### Slug-based detail additional condition

```text
ArticleSeoRouteProjection.Scope = public
AND ArticleSeoRouteProjection.Slug matches request
AND ArticleSeoRouteProjection.IsActive = true
AND ArticleSeoRouteProjection.RequiresResync = false
AND target ArticleReadModel passes public visibility checks
```

Rules:

```text
Article visibility is evaluated from locally applied Content projection state.

Slug mapping is evaluated from locally applied SEO route projection state.

Route success alone never authorizes public article exposure.

Counters or media presence never authorize public article exposure.
```

---

## 2. Fail-Closed Local Safety Rule

Reading must fail closed when required local serving state is missing, denied or unsafe.

```text
Article projection missing
OR article locally non-public
OR article visibility unsafe / requires-resync
OR required slug route missing
OR slug route inactive
OR slug route unsafe / requires-resync
OR route target article locally non-public
    -> safe public not-found behavior
```

Recommended public response code:

```text
READING.NOT_FOUND
```

Allowed behavior:

```text
Return safe 404 for detail requests.
Omit article from list, search and related outputs.
Trigger repair/reconciliation outside the public request path.
```

Not allowed:

```text
Optimistic article exposure.
Synchronously calling Content to confirm visibility.
Synchronously calling SEO to resolve missing or unsafe slug state.
Using counters or media as public-visibility evidence.
Serving through locally unsafe route state.
```

---

## 3. Bounded Eventual-Consistency Security Boundary

Reading follows Content and SEO asynchronously.

Therefore, a source owner may have committed a change before Reading has received and applied the newer snapshot.

Examples:

```text
Content has unpublished an article,
but Reading has not yet applied the non-public article snapshot.

SEO has deactivated a slug route,
but Reading has not yet applied the route-deactivation snapshot.
```

V1 accepts this as bounded asynchronous propagation lag.

Security controls required for this limitation:

```text
Measure Content -> Reading visibility projection lag.

Measure SEO -> Reading route projection lag.

Prioritize operational detection for non-public and route-deactivation changes.

Reject stale Content snapshots that could overwrite newer local deny state.

Reject stale SEO snapshots that could reactivate newer local route denial.

Support repair/reconciliation when drift or excessive lag is detected.

Fail closed immediately once local state becomes known denied or unsafe.
```

Reading must not claim instantaneous global revocation through asynchronous projections.

---

## 4. Non-Public Content Protection

Reading must not intentionally expose locally known:

```text
Draft articles
Unpublished articles
Archived articles
Soft-deleted articles
Visibility-unsafe articles
Inactive or unsafe slug routes
Admin-only data
Moderation/report data
Audit data
Internal lifecycle history
Projection diagnostics
Consumed-message state
Outbox or broker identifiers
```

Public responses must not expose internal fields such as:

```text
ContentSourceVersion
SeoSourceVersion
MediaSourceVersion
InteractionStatsVersion
Last source message identifiers
Last applied timestamps used only for operations
RequiresResync reason
Internal visibility decision reason
Consumer apply-decision metadata
```

---

## 5. Hidden Resource Enumeration Protection

Article-id and slug endpoints are enumeration-prone.

Affected endpoints:

```text
GET /api/v1/articles/{articlePublicId}
GET /api/v1/articles/slug/{slug}
```

Public clients must not be able to distinguish:

```text
Article is absent.
Article is draft.
Article is unpublished.
Article is archived.
Article is soft-deleted.
Article visibility is unsafe.
Slug route is absent.
Slug route is inactive.
Slug route requires resync.
Slug route points to hidden content.
```

All such public failures should normally collapse into:

```text
404
READING.NOT_FOUND
```

### Required Controls

```text
Use public identifiers only.

Do not expose internal numeric ids.

Do not return internal deny-reason codes publicly.

Avoid obvious response-body or timing distinctions between hidden-state outcomes.

Monitor repeated slug/public-id misses.

Rate limit high-volume enumeration behavior at the edge/API layer.
```

Internal logging may retain safe diagnostic categorization.

---

## 6. Public Query Abuse Controls

List, search and related endpoints are scrape-prone and may become query-expensive.

Affected endpoints:

```text
GET /api/v1/articles
GET /api/v1/articles/search
GET /api/v1/articles/{articlePublicId}/related
```

### Required Controls

```text
Enforce maximum pageSize.

Enforce maximum related limit.

Reject invalid paging values.

Reject unsupported filter values.

Use allowlisted sort values only.

Bound search query length.

Require non-empty search query.

Define minimum search query length if needed.

Avoid unbounded wildcard scans.

Apply deterministic paging tie-breakers.

Rate limit expensive or high-frequency patterns.
```

### V1 Allowed Sort Values

```text
-publishedAt
publishedAt
```

Not supported in V1:

```text
-popularity
popularity
```

Popularity and trending are deferred beyond V1.

### Suggested Defaults

| Setting | Default |
|---|---:|
| `page` | `1` |
| `pageSize` | `20` |
| `pageSize` maximum | Policy-defined |
| Related `limit` | `6` |
| Related `limit` maximum | Policy-defined |

---

## 7. Search Abuse Posture

Search is public, query-heavy and easily abused.

### Controls

```text
Require q.

Normalize whitespace.

Reject empty queries.

Reject overly long queries.

Optionally reject overly short queries.

Rate limit repeated or high-frequency search behavior.

Avoid returning internal ranking/debug information.

Filter locally denied or unsafe article projection rows.

Monitor high query-volume clients and unusual query distributions.
```

### V1 Safety Rules

```text
Search reads Reading-owned public projection/search state only.

Search must not synchronously query Content or SEO during ordinary public requests.

Search must not expose locally known non-public or unsafe content.

External search infrastructure is not required in V1.

Popularity/relevance ranking beyond basic V1 search is not assumed.
```

### Degraded Search Behavior

If optional search presentation enrichment is missing:

```text
Return safely reduced response fields where possible.
```

If required Reading-owned search/query capability is unavailable:

```text
Return standard service unavailable behavior according to API error contract.
```

Reading must not bypass local visibility rules to preserve search availability.

---

## 8. Rate Limiting and Edge Protection

Reading endpoints should be protected by edge/API-level rate limiting.

### Candidate Rate-Limit Dimensions

```text
IP address
Anonymous client fingerprint where privacy policy permits
Authenticated user identity if later applicable
Route group
Search-heavy behavior
Repeated miss behavior
High page-number crawling
```

### Route Groups

| Route group | Example | Primary abuse risk |
|---|---|---|
| Listing | `/articles` | Bulk scraping |
| Detail by id | `/articles/{articlePublicId}` | Public-id enumeration |
| Detail by slug | `/articles/slug/{slug}` | Slug probing / scraping |
| Search | `/articles/search` | Expensive query abuse |
| Related | `/articles/{articlePublicId}/related` | Graph crawling / scraping |

### Security Rules

```text
Rate-limit responses must not reveal whether a target article exists but is hidden.

Rate limiting should be applied consistently across missing and hidden-resource lookups.

Search may use stricter limits than normal article detail reads.

Legitimate crawler support must not weaken visibility or route safety.
```

---

## 9. Slug Route Projection Safety

Reading serves slug-based article requests from local:

```text
ArticleSeoRouteProjection
```

Reading does not synchronously call SEO `/resolve` during ordinary V1 public requests.

### Required Checks

```text
Scope = public
Slug matches requested route
IsActive = true
RequiresResync = false
Target ArticlePublicId resolves to locally safe public ArticleReadModel
```

### Security Rules

```text
Missing local route state fails closed.

Inactive local route state fails closed.

RequiresResync route state fails closed.

Route existence alone never exposes article content.

A stale route snapshot must not overwrite a newer local deactivation.

Cache must not serve a route locally known inactive or unsafe.
```

### Signals Worth Monitoring

```text
Slug miss rate
Inactive route denial rate
RequiresResync route denial rate
Route-target non-public denial rate
SEO -> Reading route projection lag
Stale route reactivation prevented count
```

---

## 10. Article Visibility Projection Safety

Content remains the owner of publication truth.

Reading serves only from locally applied Content-derived visibility state stored in `ArticleReadModel`.

### Required Checks

```text
Status = Published
IsPublic = true
Visibility not unsafe / requires-resync
```

### Security Rules

```text
A locally applied non-public Content snapshot must deny public exposure immediately.

A stale Content snapshot must not restore older public visibility.

A missing or unsafe ArticleReadModel must not be fixed synchronously inside a public request.

Repair/reconciliation is outside the public request path.
```

### Signals Worth Monitoring

```text
Visibility deny rate
Visibility unsafe deny rate
Content -> Reading visibility projection lag
Non-public snapshot apply lag
Stale public re-exposure prevented count
Reconciliation mismatch count
```

---

## 11. Cache Safety

Cache is acceleration only.

Cache must contain public-safe Reading-derived response data only.

### Cache Rules

```text
Cache must not become source truth.

Cache hit must not bypass local article visibility safety.

Cache hit must not bypass local slug route safety.

Locally known deny or unsafe state wins over cached public content.

Cache miss must not trigger hidden synchronous calls to source modules.

Cache refresh failure must not break a safe projection-backed response.

Cache TTL and invalidation behavior must be bounded and observable.
```

### Cache Must Not Expose

```text
Draft-only content
Known non-public content
Known unsafe route output
Admin/moderation fields
Audit fields
Projection diagnostics
Source version fields
Message/outbox/broker identifiers
Sensitive tokens or headers
```

### Unsafe Cache Conditions

Treat as critical:

```text
Cached public content served after local article visibility denial.

Cached slug response served after local route deactivation or RequiresResync.

Partial rebuild output cached or served as active complete state.
```

---

## 12. Optional Enrichment Safety

Optional enrichment may be absent, delayed or stale without making safely public article content unavailable.

V1 optional enrichment includes:

```text
Cover/media presentation
SEO response metadata not required for local slug-route resolution
Interaction counter values
Related result state
Optional search presentation fields
```

Not included in V1:

```text
Popularity/trending scores
```

### Allowed Degradation

| Missing or delayed enrichment | Allowed behavior |
|---|---|
| Media cover | Return null/omitted cover |
| Media gallery | Return empty array or omit according to API contract |
| Optional SEO metadata | Return null/omitted fields |
| Interaction counter snapshot missing | Return documented zero/default counters |
| Interaction counter snapshot delayed | Return last-known counters |
| Related state missing | Return empty/deterministic fallback |
| Optional search display data missing | Omit safely |

### Security Rules

```text
Optional enrichment absence must not grant visibility.

Optional enrichment presence must not prove visibility.

Reading must not block public requests waiting for enrichment catch-up.

Reading must not synchronously query upstream modules to fill optional data.
```

---

## 13. Interaction Counter and View-Contribution Boundary

Interaction owns:

```text
View eligibility and acceptance
Like truth
Comment truth
Moderation/report workflow
Counter materialization
Counter publication
Abuse and repeat-view policy
```

Reading consumes only versioned counter snapshots:

```text
interaction.article_counters_projection_published
```

Displayed public fields may include:

```text
ViewCount
LikeCount
VisibleCommentCount
```

### Reading Security Rules

```text
Reading must not process raw view signals.

Reading must not duplicate Interaction anti-abuse logic.

Reading must not blindly increment displayed counters from delivered events.

Reading must not use counters to determine article visibility.

Reading must not expose Interaction moderation/report state publicly.

Reading must not query Interaction synchronously during public response handling.
```

### Client View-Contribution Flow

```text
Reading returns safely public article detail
    -> Client separately sends view contribution to Interaction
    -> Interaction applies abuse / suppression / eligibility policy
    -> Reading may later receive a newer counter snapshot
```

Rules:

```text
A failed or suppressed view contribution does not invalidate Reading response.

Interaction owns rate limiting and errors for its write endpoint.

Counter lag is safe optional-enrichment degradation.
```

---

## 14. Projection and Consumer Security

Reading projection is derived state, but projection updates can affect public exposure.

### Consumer Requirements

```text
Process inbound messages idempotently.

Use MessageId-based duplicate protection.

Use source-specific version guards.

Reject stale snapshots that would overwrite newer local state.

Mark or handle unsafe/resync-required state explicitly.

Use bounded local transactions for apply operations.

Record apply decisions for investigation.
```

### Independent Source Version Lanes

| Source lane | Reading freshness marker |
|---|---|
| Content article visibility/content | `ContentSourceVersion` |
| SEO route/metadata | `SeoSourceVersion` |
| Media presentation | `MediaSourceVersion` |
| Interaction counters | `InteractionStatsVersion` |

### Forbidden Behavior

```text
Using timestamps as freshness authority.

Using one SourceVersion across independent source lanes.

Applying stale Content state that re-exposes hidden content.

Applying stale SEO state that reactivates deactivated route state.

Blindly applying repeated counter deltas.

Publishing or caching partial rebuild state as active output.
```

---

## 15. Logging Rules

Reading logs must support investigation without leaking sensitive or unnecessarily identifying data.

### Recommended Structured Log Fields

```text
correlationId
routeTemplate
httpStatusCode
durationMs
clientCategory where available
articlePublicId where supplied
slugHash or normalized slug where policy permits
localVisibilityDecisionCategory
localRouteDecisionCategory
degradationReason
cacheHit / cacheMiss
sourceLane for consumer logs
incomingVersion / currentAppliedVersion for consumer logs
applyDecision for consumer logs
failureCode where applicable
```

### Avoid Logging

```text
Full article body
Full response bodies
Sensitive headers
Cookies
Authorization tokens
Raw secret values
Full internal exception details in public request logs
Moderation/report content in public request logs
Raw IP/user-agent outside adopted privacy/security policy
```

### Search Logging

For search query telemetry, prefer:

```text
Normalized query length
Validation outcome
Hash or coarse category where useful
Sampling for debugging
```

Avoid storing complete user search strings unless policy explicitly permits it.

### Removed V1 Log Field

Do not log:

```text
fallbackUsed
```

as a normal Reading public-path concern, because synchronous upstream fallback is not part of ordinary V1 serving.

---

## 16. Metrics and Abuse Signals

Reading should expose metrics for:

### Public Request Abuse Signals

```text
Request count by endpoint group
Latency by endpoint group
404 rate by endpoint group
429 rate by endpoint group
Search request rate
Invalid search-query rate
Invalid paging/filter/sort rejection count
High page-number request rate
Slug miss rate
Article public-id miss rate
Related crawling rate
```

### Local Safety Signals

```text
Local visibility-deny count
Local unsafe-visibility deny count
Local route-missing deny count
Local route-inactive deny count
Local route-requires-resync deny count
Route-target-not-public deny count
Stale Content re-exposure prevented count
Stale SEO route reactivation prevented count
Cache stale-public-response rejected count
```

### Optional Degradation Signals

```text
Counter-defaulted response count
Counter-last-known response count
Media-missing response count
SEO-metadata-missing response count
Related-empty/fallback response count
Cache miss/refresh failure count
```

### Projection Security Signals

```text
Content visibility projection lag
SEO route projection lag
Consumer duplicate ignored count
Consumer stale snapshot ignored count
Version gap/resync-required count
Projection apply failure count
Reconciliation mismatch count
Repair/rebuild safety failure count
```

Abuse indicators include:

```text
High slug miss rate from a client/network.
High article-public-id miss rate.
Repeated max-page or high-page crawling.
High-frequency search requests.
Repeated invalid-query attempts.
Unusual route distribution.
High related-article traversal rate.
```

---

## 17. Response Hardening

Public Reading responses should:

```text
Use stable public identifiers.

Avoid exposing internal ids.

Avoid internal route/visibility reason codes.

Avoid source-version or message metadata.

Avoid projection/debug state.

Avoid stack traces or dependency names.

Use stable generic not-found behavior for hidden/unsafe resources.

Return optional enrichment only in documented public-safe shape.
```

### Public Not-Found Example

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

## 18. Authorization and Privacy Posture

Reading V1 public endpoints are anonymous-safe and generally do not require authentication.

Reading V1 must not silently mix user-specific information into anonymous public responses.

Deferred features requiring explicit authorization/privacy design:

```text
Draft preview
Admin/editor preview
Personalized recommendations
User-specific reading history
User-specific ranking
Personalized response caching
```

Rules:

```text
Public Reading must not expose moderation/report data.

Public Reading must not expose user-specific interaction details.

Public cache keys must not depend on sensitive identity data in V1.
```

---

## 19. Bot and Crawler Posture

Reading should expect both legitimate crawlers and abusive scraping.

Recommended controls:

```text
Bounded pagination
Rate limits by route group
Search-specific rate limits
Repeated-miss detection
Safe edge caching where visibility rules are preserved
Robots/crawler policy at website layer
Stable canonical metadata where projected and safe
Abuse monitoring and operational alerting
```

Rules:

```text
Crawler support must not weaken local visibility enforcement.

Crawler support must not bypass local slug-route safety.

No crawler policy may justify exposing locally known hidden content.
```

---

## 20. Incident Posture

During scraping, abuse, cache failure, projection lag or optional dependency degradation, Reading should prefer:

```text
Rate limiting
Safe projection-backed cache use
Safe 404
Safe omission
Degraded optional enrichment
503 when required Reading serving infrastructure is unavailable
Repair/reconciliation outside request path
```

Reading must not respond by:

```text
Disabling visibility checks
Serving through unsafe local route state
Exposing locally known non-public content
Increasing unbounded query limits
Bypassing projection safety
Calling source modules synchronously as hidden fallback
Leaking internal diagnostics to clients
```

### Critical Incidents

Treat as critical:

```text
Serving article content locally marked non-public or unsafe.

Serving through route locally inactive or marked RequiresResync.

Stale Content snapshot restoring newer locally denied state.

Stale SEO snapshot restoring newer locally deactivated route.

Cache overriding local deny/unsafe state.

Partial rebuild candidate becoming publicly active without safe cutover.

Public exposure of internal diagnostics, moderation or hidden-state reason details.
```

### Propagation-Lag Warning

An upstream source update not yet observed by Reading is an accepted bounded V1 risk, not automatically an implementation violation.

However:

```text
Excessive propagation lag
Detected divergence
SLO breach
Unrepaired known drift
```

must trigger operational attention according to policy.

---

## 21. V1 Non-Goals

This document does not define:

```text
Interaction anti-fraud implementation
Interaction view suppression algorithm
Full bot-detection platform
WAF vendor configuration
Canonical redirect policy
Exact cache TTL or invalidation implementation
Exact SLO thresholds
Admin preview authorization
Personalized recommendation privacy model
Worker retry/DLQ mechanics
Emergency upstream fallback policy
Popularity/trending security model
```

Those belong to owning modules, infrastructure policy or later ADRs.

---

## 22. V1 Security Summary

| Concern | V1 security rule |
|---|---|
| Public serving source | Reading-owned local projections only |
| Article public exposure | Require locally safe public `ArticleReadModel` |
| Slug public exposure | Require safe `ArticleSeoRouteProjection` and safe public `ArticleReadModel` |
| Hidden resource disclosure | Collapse to generic `READING.NOT_FOUND` |
| Synchronous upstream fallback | Not used in ordinary public reads |
| Visibility/route propagation lag | Bounded risk accepted and monitored |
| Media/counter absence | Safe degradation only |
| Counters | Versioned snapshot display only; never visibility authority |
| Popularity/trending | Deferred beyond V1 |
| Cache | Acceleration only; local deny/unsafe state wins |
| Query abuse | Bounded inputs, allowlisted sort/filter, rate limiting |
| Projection update safety | Message dedupe plus source-specific version guards |
| Internal diagnostics | Never exposed publicly |
| Public identity posture | Anonymous-safe; no silent personalization |
```