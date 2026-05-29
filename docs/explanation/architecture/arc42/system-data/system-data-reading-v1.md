# System Data Model — Reading (V1 Async Projections)

> **Module:** Reading  
> **Purpose:** Define Reading-owned public serving projections for article list/detail/slug/search/related queries.  
> **V1 posture:** Reading serves ordinary public requests from local asynchronous projections only.

---

## 1. Ownership

Reading owns derived public-serving state:

```text
ArticleReadModel
ArticleSeoRouteProjection
ReadingConsumedMessage / consumer apply state
Optional cache derived from safe Reading projection state
```

Reading does not own source truth:

| Concern | Source owner |
|---|---|
| Article lifecycle, editorial fields and public visibility truth | Content |
| Canonical slug, route and SEO metadata truth | SEO |
| Media asset and public presentation truth | Media |
| Views, likes, comments, moderation and counter truth | Interaction |

Rules:

```text
Projected data remains derived.
Copied data does not transfer source ownership.
Reading must not synchronously query source modules in ordinary public serving.
```

---

## 2. V1 Runtime Dataflow

### Article content and visibility

```text
Content truth + outbox
    -> Content projection message
    -> Reading consumer
    -> reading.ArticleReadModel
```

### Slug route and SEO metadata

```text
SEO truth + outbox
    -> SEO route projection message
    -> Reading consumer
    -> reading.ArticleSeoRouteProjection
```

### Media presentation

```text
Media truth + outbox
    -> Media public presentation message
    -> Reading consumer
    -> media fields in reading.ArticleReadModel
```

### Interaction counters

```text
Interaction counter materialization + outbox
    -> interaction.article_counters_projection_published
    -> Reading consumer
    -> counter fields in reading.ArticleReadModel
```

### Public read paths

```text
GET by public id:
    ArticleReadModel
        -> local visibility check
        -> response or safe 404

GET by slug:
    ArticleSeoRouteProjection
        -> ArticleReadModel
        -> local route + visibility checks
        -> response or safe 404
```

---

## 3. V1 Tables

## 3.1. `reading.ArticleReadModel`

Primary public article serving projection.

### Purpose

Supports:

```text
Public list
Public detail by ArticlePublicId
Basic search
Related-article query inputs
Optional media display
Optional counter display
```

### Logical Fields

| Field | Purpose |
|---|---|
| `ArticleReadModelId` | Reading-local surrogate primary key |
| `ArticlePublicId` | Stable public article identity; unique |
| `Title` | Projected public title |
| `Summary` | Projected public summary |
| `Body` | Projected public body |
| `CategoryPublicId` | Projected category identity, nullable |
| `CategoryName` | Projected category name, nullable |
| `AuthorUserId` | Projected author identity where retained |
| `AuthorDisplayName` | Public author display name, nullable |
| `Status` | Content-derived state |
| `IsPublic` | Local public serving flag derived from Content |
| `VisibilityRequiresResync` | Unsafe/gapped visibility marker |
| `PublishedAtUtc` | Public publish timestamp |
| `ArticleUpdatedAtUtc` | Content update timestamp |
| `SearchText` | Searchable denormalized public text, nullable |
| `CoverMediaPublicId` | Projected cover identity, nullable |
| `CoverMediaUrl` | Projected public cover URL, nullable |
| `CoverAlt` | Projected cover alt text, nullable |
| `ViewCount` | Displayed projected view count; default `0` |
| `LikeCount` | Displayed projected active-like count; default `0` |
| `VisibleCommentCount` | Displayed projected visible-comment count; default `0` |
| `ContentSourceVersion` | Latest applied Content snapshot version |
| `MediaSourceVersion` | Latest applied Media snapshot version, nullable |
| `InteractionStatsVersion` | Latest applied Interaction counter snapshot version, nullable |
| `ContentLastMessageId` | Last applied Content message id |
| `MediaLastMessageId` | Last applied Media message id, nullable |
| `InteractionLastMessageId` | Last applied Interaction message id, nullable |
| `LastContentAppliedAtUtc` | Content apply timestamp |
| `LastMediaAppliedAtUtc` | Media apply timestamp, nullable |
| `LastInteractionAppliedAtUtc` | Interaction apply timestamp, nullable |
| `CreatedAtUtc` | Reading row creation timestamp |
| `UpdatedAtUtc` | Reading row update timestamp |

### Invariants

```text
ArticlePublicId is unique.

Public responses may serve a row only when:
    Status = Published
    AND IsPublic = true
    AND VisibilityRequiresResync = false.

ViewCount >= 0.
LikeCount >= 0.
VisibleCommentCount >= 0.

Interaction counters are enrichment only.
They must not determine public visibility.
```

---

## 3.2. `reading.ArticleSeoRouteProjection`

Local slug-route serving projection consumed asynchronously from SEO.

### Purpose

Supports:

```text
GET /api/v1/articles/slug/{slug}
Projected SEO metadata for public detail responses
Local fail-closed route serving
```

### Logical Fields

| Field | Purpose |
|---|---|
| `ArticleSeoRouteProjectionId` | Reading-local surrogate primary key |
| `Scope` | Route scope, normally `public` |
| `Slug` | Public route slug |
| `ArticlePublicId` | Target article identity |
| `CanonicalUrl` | Projected canonical URL, nullable |
| `MetaTitle` | Projected SEO title, nullable |
| `MetaDescription` | Projected SEO description, nullable |
| `IsActive` | Whether the route may be served locally |
| `RequiresResync` | Unsafe/gapped route marker |
| `SeoSourceVersion` | Latest applied SEO route snapshot version |
| `LastSourceMessageId` | Last applied SEO message id |
| `LastSourceOccurredAtUtc` | Source event time for diagnostics |
| `LastAppliedAtUtc` | Local apply timestamp |
| `CreatedAtUtc` | Local creation timestamp |
| `UpdatedAtUtc` | Local update timestamp |

### Invariants

```text
Scope + Slug identifies a local route projection.

A slug request may proceed only when:
    Scope = public
    AND IsActive = true
    AND RequiresResync = false.

A valid route does not make an article public.
The target ArticleReadModel must also pass public visibility checks.
```

---

## 3.3. `reading.ReadingConsumedMessage`

Durable consumer idempotency/apply tracking table.

### Purpose

Supports:

```text
At-least-once message delivery
Duplicate detection
Apply diagnostics
Replay and repair investigation
```

### Logical Fields

| Field | Purpose |
|---|---|
| `ReadingConsumedMessageId` | Reading-local primary key |
| `ConsumerName` | Handler/consumer identity |
| `MessageId` | Delivered message identity |
| `EventType` | Source event type |
| `SourceLane` | `Content`, `Seo`, `Media`, or `Interaction` |
| `AggregatePublicId` | Target public aggregate identity, nullable |
| `IncomingVersion` | Incoming source-lane version, nullable |
| `ApplyDecision` | `Applied`, `DuplicateIgnored`, `StaleIgnored`, `RequiresResync`, `Failed` |
| `CorrelationId` | Trace correlation id, nullable |
| `OccurredAtUtc` | Source occurrence timestamp |
| `ReceivedAtUtc` | Consumer receive timestamp |
| `ProcessedAtUtc` | Consumer processing completion timestamp, nullable |
| `FailureCode` | Internal failure code, nullable |

### Invariants

```text
ConsumerName + MessageId is unique.

Message-level dedupe does not replace source-specific version checks.

ApplyDecision is internal-only and must not be exposed in public APIs.
```

---

## 4. Source-Specific Version Rules

Reading receives independent projection lanes.

| Source lane | Projection state affected | Version field |
|---|---|---|
| Content | Article content and visibility | `ContentSourceVersion` |
| SEO | Route and SEO metadata | `SeoSourceVersion` |
| Media | Cover/media presentation | `MediaSourceVersion` |
| Interaction | Displayed counters | `InteractionStatsVersion` |

Apply rule:

```text
If IncomingVersion > CurrentAppliedVersion:
    apply known-value snapshot
Else:
    ignore as duplicate or stale
```

Rules:

```text
Versions from different source lanes are not comparable.

Timestamps are for diagnostics and lag measurement only.

A stale Content snapshot must not re-expose locally hidden content.

A stale SEO snapshot must not reactivate a locally inactive route.

A stale Interaction snapshot must not overwrite newer counters.
```

---

## 5. Public Read Invariants

### 5.1. Article visibility

Reading may publicly expose an article only when local state confirms:

```text
Status = Published
AND IsPublic = true
AND VisibilityRequiresResync = false
```

Otherwise:

```text
Detail request -> safe 404.
List/search/related -> omit row.
```

### 5.2. Slug route safety

Slug detail requires:

```text
ArticleSeoRouteProjection exists
AND Scope = public
AND IsActive = true
AND RequiresResync = false
AND target ArticleReadModel is locally public.
```

### 5.3. Optional enrichment

```text
Missing media fields -> null or omitted fields.

Missing Interaction snapshot -> zero/default counters.

Delayed Interaction snapshot -> last-known counters.

Optional enrichment must not make visibility permissive.
```

### 5.4. Async lag

```text
New publication or route activation may temporarily be absent.

Upstream unpublish/delete/route-deactivate changes may remain unseen locally
until newer snapshots are applied.

V1 accepts bounded propagation lag and requires observability/repair posture.
```

---

## 6. Index Guidance

## 6.1. `ArticleReadModel`

Required hot-path indexes:

```text
UQ_ArticleReadModel_ArticlePublicId

IX_ArticleReadModel_Public_PublishedAt
    (IsPublic, Status, VisibilityRequiresResync, PublishedAtUtc DESC, ArticlePublicId DESC)

IX_ArticleReadModel_Category_Public_PublishedAt
    (CategoryPublicId, IsPublic, Status, VisibilityRequiresResync, PublishedAtUtc DESC, ArticlePublicId DESC)
```

Optional when implemented:

```text
SearchText / full-text support for public search.

Tag-related projection/index support if tag filtering or related-by-tag
is included in the concrete schema.
```

## 6.2. `ArticleSeoRouteProjection`

Required hot-path indexes:

```text
UQ_ArticleSeoRouteProjection_Scope_Slug
    (Scope, Slug)

IX_ArticleSeoRouteProjection_ArticlePublicId
    (ArticlePublicId)
```

## 6.3. `ReadingConsumedMessage`

Required reliability/cleanup indexes:

```text
UQ_ReadingConsumedMessage_ConsumerName_MessageId
    (ConsumerName, MessageId)

IX_ReadingConsumedMessage_SourceLane_ProcessedAtUtc
    (SourceLane, ProcessedAtUtc)

IX_ReadingConsumedMessage_ApplyDecision_ProcessedAtUtc
    (ApplyDecision, ProcessedAtUtc)
```

---

## 7. Cache Posture

Cache is optional acceleration over Reading-owned projection state.

Possible public cache keys:

```text
cn:reading:article:{articlePublicId}
cn:reading:slug:{scope}:{slug}
cn:reading:feed:{page}:{pageSize}:{filtersHash}
```

Rules:

```text
Cache must contain public-safe Reading-derived data only.

Cache must not synchronously query upstream source modules on miss.

Locally known article or route denial/unsafe state wins over cached output.

Cache invalidation may respond to locally applied Content, SEO, Media
or Interaction projection updates.

Cache TTL and invalidation policy remain a separate operational decision.
```

---

## 8. Repair and Rebuild Posture

Reading projections are derived and must be repairable or rebuildable.

| Reading state | Approved input owner |
|---|---|
| Article content and visibility | Content |
| Slug route and SEO metadata | SEO |
| Media presentation | Media |
| Displayed counters | Interaction |

Rules:

```text
Repair updates Reading-owned state only.

Repair must not mutate upstream truth.

RabbitMQ is transport, not permanent replay history.

Broad rebuilds should use candidate validation before cutover.

Partial or unsafe rebuilt state must not become active public-serving state.
```

---

## 9. V1 Decisions Locked

| Topic | V1 decision |
|---|---|
| Public serving model | Reading-owned async projections |
| Required serving tables | `ArticleReadModel`, `ArticleSeoRouteProjection` |
| Consumer idempotency state | `ReadingConsumedMessage` recommended for durable apply safety |
| Detail by public id | Local `ArticleReadModel` |
| Detail by slug | Local `ArticleSeoRouteProjection` then `ArticleReadModel` |
| Synchronous source composition | Not used in ordinary public reads |
| Article visibility owner | Content |
| Canonical route owner | SEO |
| Media | Optional projected presentation enrichment |
| Counters | Interaction snapshot projected into Reading |
| Counter event | `interaction.article_counters_projection_published` |
| Counter application | Set known values by newer `StatsVersion` |
| Popularity/trending | Deferred beyond V1 |
| Source freshness | Independent version per source lane |
| Missing/unsafe core local state | Fail closed |
| Optional media/counter lag | Degrade safely |
| Async propagation lag | Bounded lag accepted and monitored |

---

## 10. Deferred Decisions

Deferred beyond baseline V1:

```text
Canonical redirect and historical slug behavior
Exact Content / SEO / Media projection event schemas
Tag projection shape for filtering and related results
Search implementation and full-text strategy
Related-article precomputation
Cache TTL/invalidation details
Consumed-message retention policy
Version-gap/resync operational policy
Rebuild/cutover implementation
Media projection initial field scope
Projection freshness SLO thresholds
Popularity/trending pipeline
Personalization and preview capabilities
```

---

## 11. ERD

See:

```text
../diagrams/erd/reading-composition-v1.dbml
```

The ERD must now represent Reading-owned projection tables rather than a synchronous composition view over source-module truth.