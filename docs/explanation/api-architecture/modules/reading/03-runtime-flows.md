# Reading — Runtime Flows (V1 Async Projections)

Reading is the public serving module for CommercialNews.

Reading serves public article responses entirely from Reading-owned asynchronous projections.

Primary runtime scenarios:

```text
Public article list
Public article detail by ArticlePublicId
Public article detail by slug
Async projection apply from Content / SEO / Media / Interaction
Replay, repair and rebuild of Reading-owned projections
```

Related architecture and ADRs:

```text
../../../architecture/arc42/04-runtime-view-v1.md
../../../architecture/arc42/13-transactions-and-consistency-v1.md
../../../architecture/arc42/16-batch-processing-and-derived-data-v1.md
../../../architecture/arc42/17-dataflow-and-batch-workflows-v1.md
../../../architecture/arc42/18-stream-processing-and-derived-state-v1.md
../../../architecture/arc42/19-stream-processing-runtime-v1.md

../../../decisions/adr-0013-outbox-and-delivery-semantics-v1.md
../../../decisions/adr-0018-transaction-boundaries-and-consistency-model-v1.md
../../../decisions/adr-0020-timeout-retry-and-failure-detection-policy-v1.md
../../../decisions/adr-0027-stream-processing-and-derived-state-policy-v1.md
../../../decisions/adr-0028-consumer-idempotency-replay-and-rebuild-policy-v1.md
```

---

## 1. Runtime Posture in V1

Reading participates in three runtime lanes.

### 1.1. Synchronous public read lane

Used for:

```text
Public article listing
Public article detail by ArticlePublicId
Public article detail by slug
Public search
Related article composition
Response composition from Reading-owned projections
```

Normal read-path rule:

```text
Public request
    -> Reading-owned projection query
    -> Safe local visibility check
    -> Response
```

Reading does not synchronously call Content, SEO, Media or Interaction during ordinary public response composition.

### 1.2. Async projection apply lane

Used for consuming known-value projection inputs from:

```text
Content      -> article public/read projection
SEO          -> article route/metadata projection
Media        -> article public media enrichment
Interaction  -> article counter snapshot projection
```

Each source lane has its own version/freshness marker.

### 1.3. Repair and rebuild lane

Used for:

```text
Projection reconciliation
Missing/stale projection repair
Bounded replay
Full rebuild where required
Consumer diagnostics
Resync-required recovery
```

### Core runtime rules

```text
Reading owns serving projections, not upstream truth.

Core visibility uncertainty fails closed.

Optional media/counter lag degrades safely.

Slug-based reads use Reading-owned ArticleSeoRouteProjection.

Interaction counters are versioned enrichment only.

Reading does not calculate Interaction truth.

At-least-once delivery, duplicates, stale arrival and replay are expected.
```

---

## 2. Reading-Owned Runtime State

Reading serves public responses from local projection state including:

```text
ArticleReadModel
ArticleSeoRouteProjection
Reading consumer dedupe/apply tracking
```

### `ArticleReadModel`

Provides:

```text
Article content
Public visibility state
Media enrichment fields
Interaction counter enrichment fields
Source-specific applied versions
```

### `ArticleSeoRouteProjection`

Provides:

```text
Scope + Slug -> ArticlePublicId
Canonical SEO metadata where projected
Route active/unsafe status
SeoSourceVersion
```

### Source-specific versions

| Input source | Applied version field |
|---|---|
| Content article state | `ContentSourceVersion` |
| SEO route state | `SeoSourceVersion` |
| Media enrichment | `MediaSourceVersion` |
| Interaction counters | `InteractionStatsVersion` |

Rule:

```text
Do not compare Content version against SEO, Media or Interaction version.
Each lane applies freshness independently.
```

---

## 3. Flow A — List Public Articles

### Goal

Return public article summaries quickly from Reading-owned projections while allowing optional enrichment lag.

### Normal flow

```text
Client calls GET /api/v1/articles
    -> Reading queries ArticleReadModel
    -> Filter:
         Status = Published
         IsPublic = true
    -> Apply allowlisted filter/sort/paging rules
    -> Compose projected media/counter fields if available
    -> Return response
```

### Rules

```text
Only safely public ArticleReadModel rows may be returned.

Unpublished, archived, soft-deleted or visibility-uncertain rows are excluded.

Media and counter lag must not block list response.

Interaction counters do not decide article visibility.

Public list does not query upstream modules synchronously.
```

### Counter behavior

When Interaction counter snapshot has not arrived:

```text
Return default zero values or currently documented response defaults.
```

When Interaction counter snapshot is stale but locally applied:

```text
Return last-known counter values.
```

### Failure behavior

| Failure | Result |
|---|---|
| Reading article projection unavailable/unsafe | Safe failure or empty result according to API contract |
| Media enrichment missing | Return null/omitted media fields |
| Interaction counter snapshot missing | Return safe default counters |
| Search/related optional enrichment delayed | Omit or degrade safely |

### Observability

Track:

```text
Public list latency and failure rate
Rows excluded due to non-public/unsafe visibility
Media enrichment missing rate
Counter snapshot missing/defaulted rate
Content projection freshness
```

---

## 4. Flow B — Open Public Article by `ArticlePublicId`

### Goal

Serve public detail from Reading-owned article projection without synchronous upstream dependency.

### Normal flow

```text
Client calls GET /api/v1/articles/{articlePublicId}
    -> Reading loads ArticleReadModel by ArticlePublicId
    -> Verify:
         Status = Published
         IsPublic = true
    -> Compose article response from local projected state
    -> Return detail or safe 404
```

### Rules

```text
ArticleReadModel is the serving source.

Reading does not query Content for visibility confirmation during normal reads.

Reading does not query Interaction for fresher counters.

Reading does not query Media for missing cover data.

Optional enrichment missing does not block safely public content.
```

### Failure behavior

| Failure | Result |
|---|---|
| Article projection missing | Safe 404 |
| Article is non-public | Safe 404 |
| Visibility is unsafe/uncertain | Safe 404 |
| Media enrichment unavailable | Return article without unavailable media fields |
| Counter snapshot delayed | Return last-known/default counters |

---

## 5. Flow C — Open Public Article by Slug

### Goal

Serve slug-based public detail entirely from Reading-owned async route and article projections.

### Normal flow

```text
Client calls GET /api/v1/articles/slug/{slug}
    -> Reading queries ArticleSeoRouteProjection by:
         Scope = public
         Slug = requested slug
    -> Verify route:
         IsActive = true
         RequiresResync = false
    -> Read ArticlePublicId from route projection
    -> Reading loads ArticleReadModel by ArticlePublicId
    -> Verify article:
         Status = Published
         IsPublic = true
    -> Compose response from Reading-owned projections
    -> Return detail or safe 404
```

### Rules

```text
SEO owns canonical routing truth.

Reading owns its consumed serving projection of SEO routing state.

Reading does not call SEO synchronously in the normal slug hot path.

A valid route does not make an article public by itself.

Both route safety and article visibility must pass.
```

### Failure behavior

| Situation | Result |
|---|---|
| Route missing | Safe 404 |
| Route inactive | Safe 404 |
| Route marked `RequiresResync` | Safe 404 |
| Route points to missing article projection | Safe 404 |
| Article projection is non-public/unsafe | Safe 404 |
| SEO projection lag | Public slug may temporarily fail closed |
| Media/counter enrichment lag | Detail still succeeds when route/article visibility are safe |

### Observability

Track:

```text
Slug lookup latency and failure rate
Route miss rate
Inactive route denial count
RequiresResync route denial count
Route-found-but-article-missing count
Route-found-but-article-non-public count
SEO projection lag
```

---

## 6. Flow D — Client View Contribution to Interaction

### Goal

Allow article view counting without coupling public Reading response latency or availability to Interaction writes.

### Runtime flow

```text
Reading successfully returns public article detail
    -> Client separately calls Interaction view endpoint
    -> Interaction checks local article eligibility projection
    -> Interaction applies abuse/repeat-view policy
    -> Interaction atomically updates ArticleViewCount when accepted
    -> Interaction later publishes updated counter snapshot
    -> Reading consumes refreshed counter snapshot asynchronously
```

### Rules

```text
Reading does not persist view counts.

Reading does not wait for view contribution success.

Reading does not consume raw per-view events.

Interaction owns view acceptance and ArticleViewCount.

A missing or failed view contribution never invalidates a successful article response.
```

### Failure behavior

| Failure | Result |
|---|---|
| Client never sends view request | Article response remains correct |
| Interaction rejects/suppresses view | Article response remains correct |
| Interaction unavailable | Article response remains correct |
| Counter publication delayed | Reading displays stale/default counters |

---

## 7. Flow E — Content Projection Apply

### Goal

Maintain Reading's public article serving projection from Content-owned public article output.

### Runtime flow

```text
Content commits article truth + OutboxMessage
    -> Worker publishes Content public article projection event
    -> Reading consumer receives event
    -> Reading dedupes MessageId
    -> Reading compares incoming ContentSourceVersion
    -> If newer:
         upsert/update ArticleReadModel core state
         update public visibility
         record Content applied metadata
    -> If duplicate/stale:
         record/ignore without changing projection
```

### Conceptual event

```text
content.article_read_projection_published
```

### Apply behavior

For published article state:

```text
Set article public content fields.
Set Status = Published.
Set IsPublic = true when source snapshot declares public availability.
```

For unpublished, archived or soft-deleted article state:

```text
Set IsPublic = false.
Preserve ArticleReadModel row.
Preserve optional enrichment fields for diagnostics/reconciliation.
```

### Rules

```text
Content is the authority for article public visibility.

Incoming ContentSourceVersion must be newer than current ContentSourceVersion.

A stale Content message must never re-expose a non-public article.

Timestamp is diagnostic only; version determines freshness.
```

### Failure behavior

| Failure | Result |
|---|---|
| Duplicate event | Ignore safely |
| Older event after newer event | Ignore by `ContentSourceVersion` |
| Gap/unsafe apply detected | Mark visibility unsafe or require resync; fail closed publicly |
| Consumer retry | Safe through dedupe/version guards |

---

## 8. Flow F — SEO Route Projection Apply

### Goal

Maintain Reading-local slug route state used by public slug-detail queries.

### Runtime flow

```text
SEO commits route/metadata truth + OutboxMessage
    -> Worker publishes SEO route projection event
    -> Reading consumer receives event
    -> Reading dedupes MessageId
    -> Reading compares incoming SeoSourceVersion
    -> If newer:
         upsert ArticleSeoRouteProjection
         update slug, ArticlePublicId, metadata and route activity
    -> If duplicate/stale:
         ignore safely
```

### Conceptual event direction

```text
SEO public article route projection
    -> Reading ArticleSeoRouteProjection
```

### Apply behavior

Active route snapshot:

```text
Scope + Slug resolves locally to ArticlePublicId.
IsActive = true.
RequiresResync = false.
```

Inactive or removed route snapshot:

```text
Route remains stored for diagnostics.
IsActive = false.
Public slug lookup no longer serves through that route.
```

Unsafe/gapped route state:

```text
RequiresResync = true.
Slug lookup fails closed until repaired.
```

### Rules

```text
SEO remains owner of canonical route truth.

Reading consumes route snapshots for serving.

Reading does not fall back to synchronous SEO resolve in the normal public path.

Incoming SeoSourceVersion must be newer than current route version.
```

### Failure behavior

| Failure | Result |
|---|---|
| Duplicate route event | Ignore safely |
| Older route event | Ignore by `SeoSourceVersion` |
| Route projection missing | Slug request fails closed |
| Route projection requires resync | Slug request fails closed |
| SEO consumer lag | New/changed slug may temporarily be unavailable publicly |

---

## 9. Flow G — Media Projection Apply

### Goal

Update optional public media presentation fields without affecting core article visibility.

### Runtime flow

```text
Media commits public media state + OutboxMessage
    -> Worker publishes Media public presentation event
    -> Reading consumer receives event
    -> Dedupe MessageId
    -> Compare MediaSourceVersion
    -> If newer:
         update cover/media fields in ArticleReadModel
```

### Apply behavior

```text
Update:
    CoverMediaPublicId
    CoverMediaUrl
    CoverAlt
    MediaSourceVersion
    MediaLastMessageId
    LastMediaAppliedAtUtc
```

### Rules

```text
Media updates do not turn an article public or non-public.

Missing cover/media is safe degradation.

Older MediaSourceVersion must not overwrite newer media presentation.
```

### Failure behavior

| Failure | Result |
|---|---|
| Media projection delayed | Serve article with null/old media fields |
| Duplicate/stale media event | Ignore safely |
| Media unavailable | Core article response still works when visibility is safe |

---

## 10. Flow H — Interaction Counter Snapshot Apply

### Goal

Update Reading-local public counters from Interaction-owned versioned snapshots.

### Runtime flow

```text
Interaction materializes ArticleInteractionStats
    -> Interaction commits updated stats + OutboxMessage
    -> Worker publishes interaction.article_counters_projection_published
    -> Reading consumer receives event
    -> Reading dedupes MessageId
    -> Reading compares incoming StatsVersion
    -> If newer:
         set ViewCount
         set LikeCount
         set VisibleCommentCount
         set InteractionStatsVersion
         record Interaction applied metadata
    -> If duplicate/stale:
         ignore safely
```

### Event type

```text
interaction.article_counters_projection_published
```

### Snapshot payload

```text
ArticlePublicId
ViewCount
LikeCount
VisibleCommentCount
StatsVersion
OccurredAtUtc
```

### Apply rule

```text
If IncomingStatsVersion > CurrentInteractionStatsVersion:
    apply known-value snapshot
Else:
    ignore duplicate or stale snapshot
```

### Rules

```text
Reading applies known-value counter snapshots only.

Reading never blindly increments counters from event delivery.

Reading does not consume individual view/like/comment mutation events to build public totals.

Counter freshness does not control article visibility.

Counter lag does not block public responses.
```

### Failure behavior

| Failure | Result |
|---|---|
| Snapshot duplicated | Ignore safely |
| Older snapshot arrives later | Ignore by `StatsVersion` |
| Snapshot delayed | Return last-known/default counters |
| Article projection not yet present | Retain/apply according to projection implementation policy or repair later; never create public visibility from counters alone |
| Consumer failure | Retry safely through dedupe/version rules |

### Observability

Track:

```text
Interaction counter consumer lag
Counter snapshot apply count
Duplicate/stale snapshot count
StatsVersion gap/resync signal where detected
Articles served with default/last-known counters
```

---

## 11. Flow I — Consumer Dedupe and Apply Decision

### Goal

Make every inbound Reading projection consumer safe under at-least-once delivery and replay.

### General runtime flow

```text
Reading consumer receives message
    -> Begin bounded local transaction
    -> Try reserve/record MessageId for this consumer
    -> If already processed:
         record duplicate outcome / acknowledge safely
    -> Else:
         compare source-specific version
         apply newer projection snapshot or ignore stale input
         complete consumer apply record
    -> Commit
```

### Required protections

| Concern | Protection |
|---|---|
| Same message delivered again | Message-level dedupe using `MessageId` |
| Different older message arrives after newer apply | Source-specific version check |
| Consumer retry after failure | Local transaction + dedupe/apply safety |
| Repair/replay | Idempotent known-value upsert |
| Visibility unsafe/gap detected | Mark unsafe/resync-required and fail closed |

### Source lanes

| Source | Version gate |
|---|---|
| Content | `ContentSourceVersion` |
| SEO | `SeoSourceVersion` |
| Media | `MediaSourceVersion` |
| Interaction | `InteractionStatsVersion` |

---

## 12. Flow J — Reading Reconciliation and Repair

### Goal

Repair divergence between upstream projection sources and Reading-owned serving state.

### Typical repair cases

```text
Content public article state exists but ArticleReadModel is missing.
ArticleReadModel still exposes an article that should now be non-public.
SEO route projection is missing, inactive incorrectly or marked RequiresResync.
Media enrichment is stale or missing.
Interaction counters are stale or missing after consumer failure.
Consumed-message/apply diagnostics indicate a gap or failed apply.
```

### Runtime shape

```text
Select bounded repair scope
    -> Obtain approved source-owned rebuild/reconciliation input
    -> Compare against Reading-owned projection
    -> Produce bounded candidate corrections
    -> Validate candidate changes
    -> Apply repair locally with source-specific version safety
    -> Record repair outcome and metrics
```

### Rules

```text
Repair may update Reading-owned projections only.

Repair must not modify Content, SEO, Media or Interaction truth.

Public visibility remains fail-closed while core visibility/route state is unsafe.

Interaction counter repair uses source-owned counter snapshot/reconciliation input,
not Reading-side recomputation from raw engagement behavior.

RabbitMQ is transport, not permanent replay storage.
```

### Full rebuild posture

For a broad rebuild:

```text
Build candidate projection state
    -> Validate completeness/safety
    -> Cut over only when candidate is acceptable
```

Partial or uncertain rebuilt state must not be silently exposed as complete public state.

---

## 13. Cache Behavior

### Goal

Allow cache acceleration without letting cache become hidden truth.

### Runtime flow

```text
Public request
    -> Optional cache lookup
    -> If safe usable hit:
         serve cached Reading projection response
    -> If miss/stale/unsafe:
         query Reading-owned projection
         optionally refresh cache
    -> return response
```

### Rules

```text
Cache does not replace Reading projection ownership.

Cache must not bypass public visibility checks.

Cache must not call upstream modules as an implicit fallback.

Cached counters/media may be stale, but cached visibility must never re-expose non-public content.

After an unsafe visibility or route state is known, safe denial wins over stale cached content.
```

### Failure behavior

| Failure | Result |
|---|---|
| Cache miss | Read Reading projection |
| Cache refresh failure | Response may succeed from Reading projection |
| Stale counter/media cache | Safe optional-enrichment lag |
| Stale public visibility cache | Must not be served when local safe state denies or is unsafe |

---

## 14. Serving Under Projection Lag

### Core visibility / route lag

| Situation | Result |
|---|---|
| Article visibility projection missing | Safe 404 / deny |
| Article visibility projection unsafe | Safe 404 / deny |
| Slug route projection missing | Safe 404 / deny |
| Slug route inactive | Safe 404 / deny |
| Slug route requires resync | Safe 404 / deny |

### Optional enrichment lag

| Situation | Result |
|---|---|
| Interaction snapshot missing | Use zero/default counters |
| Interaction snapshot delayed | Use last-known counters |
| Media projection missing | Omit/null media fields |
| Related content delayed | Return empty/deterministic fallback |

Principle:

```text
Visibility and routing uncertainty fail closed.

Optional enrichment lag degrades safely.
```

---

## 15. V1 Non-Goals

Reading V1 does not implement:

```text
Synchronous Reading -> SEO slug resolution
Synchronous Reading -> Interaction counter query
Synchronous Reading -> Content visibility query in ordinary public requests
Raw view-event analytics
Counter increments from raw Interaction activity events
Popularity/trending ranking pipeline
Reading-owned engagement truth
Reading-owned moderation workflow
Hidden fallback to upstream truth during public hot-path requests
```

---

## 16. Runtime Summary

Reading runtime in V1 is governed by these rules:

1. Reading serves ordinary public responses only from Reading-owned projections.
2. Content projection state controls article public visibility.
3. SEO route projection controls local slug resolution, while SEO remains route truth owner.
4. Media enrichment is optional and must degrade safely.
5. Interaction publishes versioned known-value counter snapshots; Reading only consumes and serves them.
6. Reading never blocks article delivery on view tracking.
7. Each source lane applies its own independent version marker.
8. Duplicate, stale and replayed messages are expected and must be harmless.
9. Unsafe visibility or route state fails closed.
10. Repair and rebuild update Reading projections only; they never take ownership of source truth.