# Reading — Domain Contracts (V1 Async Projections)

Reading is the public read-side module for CommercialNews.

Reading owns:

```text
Public query semantics
Public response composition
Reading-owned serving projections
Projection apply / dedupe / freshness behavior
Safe public visibility enforcement from local projected state
```

Reading does not own source truth.

Reading serves public article requests entirely from its own asynchronously maintained projections.

---

## 1. Module Role

Reading is responsible for:

```text
Public article listing
Public article detail by ArticlePublicId
Public article detail by slug
Search result composition
Related article composition
Public visibility enforcement
Graceful degradation of optional enrichments
Reading-owned projection apply and reconciliation
```

Reading is not responsible for:

```text
Creating or editing article truth
Publishing, unpublishing, archiving or deleting articles
Generating canonical SEO truth
Managing media asset truth
Owning views, likes or comments truth
Sending notifications
Owning canonical audit evidence
```

---

## 2. Ownership and Source Inputs

| Concern | Source owner | Reading-owned usage |
|---|---|---|
| Article public content and visibility | Content | `ArticleReadModel` core serving state |
| Public slug routing and SEO metadata | SEO | `ArticleSeoRouteProjection` and optional SEO response fields |
| Public media presentation | Media | Projected cover/media enrichment |
| Public interaction counters | Interaction | Projected counter snapshot fields |
| Public response composition | Reading | Reading-owned API/query behavior |

Rule:

```text
Copied or projected data inside Reading does not transfer ownership.
```

Reading must not synchronously query Content, SEO, Media or Interaction during ordinary public article response composition.

---

## 3. Reading-Owned Projections

## 3.1. `ArticleReadModel`

`ArticleReadModel` is Reading's primary public article serving projection.

It stores the public article response state assembled from asynchronously applied source snapshots.

### Expected fields

| Field | Purpose |
|---|---|
| `ArticleReadModelId` | Internal Reading primary key |
| `ArticlePublicId` | Stable public article identity |
| `Title` | Projected public title |
| `Summary` | Projected public summary |
| `Body` | Projected public body/rendered content |
| `CategoryPublicId` | Projected public category identity, nullable |
| `CategoryName` | Projected category display name, nullable |
| `AuthorUserId` | Projected author identity where retained |
| `AuthorDisplayName` | Projected public author display name, nullable |
| `Status` | Content-derived article state |
| `IsPublic` | Whether Reading may expose this article |
| `PublishedAtUtc` | Source publish timestamp |
| `ArticleUpdatedAtUtc` | Source article update timestamp |
| `SearchText` | Denormalized searchable text, nullable |
| `CoverMediaPublicId` | Projected primary media identity, nullable |
| `CoverMediaUrl` | Projected public cover URL, nullable |
| `CoverAlt` | Projected cover alt text, nullable |
| `ViewCount` | Interaction-derived public view count |
| `LikeCount` | Interaction-derived public active-like count |
| `VisibleCommentCount` | Interaction-derived public visible-comment count |
| `ContentSourceVersion` | Latest applied Content projection version |
| `MediaSourceVersion` | Latest applied Media projection version, nullable |
| `InteractionStatsVersion` | Latest applied Interaction stats snapshot version, nullable |
| `ContentLastMessageId` | Latest applied Content message id |
| `MediaLastMessageId` | Latest applied Media message id, nullable |
| `InteractionLastMessageId` | Latest applied Interaction message id, nullable |
| `LastContentAppliedAtUtc` | Content projection apply time |
| `LastMediaAppliedAtUtc` | Media projection apply time, nullable |
| `LastInteractionAppliedAtUtc` | Interaction stats apply time, nullable |
| `CreatedAtUtc` | Local projection creation time |
| `UpdatedAtUtc` | Local projection update time |

### Rules

```text
ArticleReadModel is derived state.
ArticleReadModel may lag behind source truth.
ArticleReadModel must be rebuildable/reconcilable.
Public visibility is determined only from safe local projected state.
Interaction counters are enrichment fields only.
```

---

## 3.2. `ArticleSeoRouteProjection`

`ArticleSeoRouteProjection` is Reading's local slug-routing projection consumed asynchronously from SEO.

Reading does not call SEO synchronously in the normal public slug-detail path.

### Expected fields

| Field | Purpose |
|---|---|
| `ArticleSeoRouteProjectionId` | Internal Reading primary key |
| `Scope` | Route scope, for example `public` |
| `Slug` | Public slug |
| `ArticlePublicId` | Target article identity |
| `CanonicalUrl` | Projected canonical URL, nullable |
| `MetaTitle` | Projected SEO title, nullable |
| `MetaDescription` | Projected SEO description, nullable |
| `IsActive` | Whether this route may be used |
| `SeoSourceVersion` | Latest applied SEO route snapshot version |
| `LastSourceMessageId` | Latest applied SEO message id |
| `LastSourceOccurredAtUtc` | Source event time for diagnostics |
| `LastAppliedAtUtc` | Local projection apply time |
| `RequiresResync` | Unsafe/gapped projection marker |
| `CreatedAtUtc` | Local creation time |
| `UpdatedAtUtc` | Local update time |

### Rules

```text
Unique active route identity follows SEO route contract, typically Scope + Slug.

Slug lookup uses ArticleSeoRouteProjection locally.

Missing, inactive or RequiresResync route fails closed.

SEO remains owner of canonical slug/routing truth.
```

---

## 4. Public Visibility Contract

Reading public APIs may return an article only when its local projection safely confirms public visibility.

Required article conditions:

```text
ArticleReadModel.Status = Published
AND ArticleReadModel.IsPublic = true
```

Required slug-route conditions for slug-based reads:

```text
ArticleSeoRouteProjection.Scope = public
AND ArticleSeoRouteProjection.IsActive = true
AND ArticleSeoRouteProjection.RequiresResync = false
AND matching ArticleReadModel is public
```

Fail-closed rule:

```text
Unknown, missing, stale-unsafe or resync-required visibility/route state
    -> do not expose public content
    -> return safe 404 according to API contract
```

Reading must not expose:

```text
Draft articles
Unpublished articles
Archived articles
Soft-deleted articles
Visibility-uncertain articles
Inactive or unsafe slug routes
```

Optional enrichment lag does not make a public article non-public:

```text
Missing/delayed media or counters
    -> article may still be served if public article visibility is safe.
```

---

## 5. Version and Freshness Contract

Reading consumes projections from multiple independent source owners.

Therefore Reading must not use one shared `SourceVersion` for all upstream state.

### Source-specific freshness markers

| Projection input | Freshness marker |
|---|---|
| Content article projection | `ContentSourceVersion` |
| SEO route projection | `SeoSourceVersion` |
| Media projection | `MediaSourceVersion` |
| Interaction counter snapshot | `InteractionStatsVersion` |

### Apply rule

For each independent source lane:

```text
If IncomingVersion > CurrentAppliedVersion:
    apply snapshot
Else:
    ignore as duplicate or stale
```

Timestamps are diagnostic only.

Do not use:

```text
Largest UpdatedAtUtc wins
Largest OccurredAtUtc wins
Largest ProcessedAtUtc wins
```

Use versions to determine ordered freshness.

---

## 6. Event Identity Contract

Important messages consumed by Reading must carry enough information for dedupe, version-aware apply and diagnostics.

| Field | Purpose |
|---|---|
| `MessageId` | Message-level identity and dedupe key |
| `EventType` | Handler routing |
| `AggregateType` | Source aggregate/projection type |
| `AggregateId` | Source identity |
| `AggregatePublicId` | Public identity where required |
| `AggregateVersion` / projection version | Ordered freshness marker |
| `Payload` | Known-value source snapshot |
| `CorrelationId` | Cross-flow tracing |
| `OccurredAtUtc` | Source occurrence timestamp for diagnostics |

Rules:

```text
MessageId prevents duplicate application of the same message.
Source-specific version prevents stale messages from overwriting newer projected state.
Both protections are required.
```

---

## 7. Inbound Projection Contracts

## 7.1. Content → Reading Article Projection

Reading consumes the Content-owned public article projection event.

Conceptual event type:

```text
content.article_read_projection_published
```

Payload provides known-value article-serving state, such as:

```text
ArticlePublicId
Title
Summary
Body
Category fields
Author display fields where included
Status
IsPublic
PublishedAtUtc
ArticleUpdatedAtUtc
ContentSourceVersion
```

Expected behavior:

```text
Apply only when incoming ContentSourceVersion is newer.
Upsert ArticleReadModel core fields.
Set public visibility from Content-projected state.
Preserve the projection row when article becomes non-public.
```

For unpublish/archive/soft-delete state represented by the projection:

```text
ArticleReadModel.IsPublic = false
Existing Reading projection row is preserved for diagnostics and replay safety.
```

---

## 7.2. SEO → Reading Route Projection

Reading consumes SEO-owned public route/metadata projection output asynchronously.

Conceptual event direction:

```text
SEO public article route projection
    -> Reading ArticleSeoRouteProjection
```

Expected payload values:

```text
Scope
Slug
ArticlePublicId
CanonicalUrl
MetaTitle
MetaDescription
IsActive
SeoSourceVersion
```

Expected behavior:

```text
Apply only when incoming SeoSourceVersion is newer.
Upsert ArticleSeoRouteProjection.
Deactivate or fail closed on inactive/unsafe routes.
Do not synchronously call SEO during normal slug article reads.
```

SEO remains source owner for route truth.

---

## 7.3. Media → Reading Public Media Projection

Reading may consume Media-owned public presentation output for article cover/media fields.

Conceptual event direction:

```text
Media public article presentation projection
    -> Reading article media enrichment
```

Expected behavior:

```text
Apply only when incoming MediaSourceVersion is newer.
Update cover/media fields only.
Do not change article visibility owned through Content-derived state.
If media enrichment is missing or delayed, serve the article without unavailable media fields.
```

---

## 7.4. Interaction → Reading Counter Snapshot

Reading consumes:

```text
interaction.article_counters_projection_published
```

This event carries a versioned known-value snapshot, not raw engagement deltas.

### Expected payload

```text
ArticlePublicId
ViewCount
LikeCount
VisibleCommentCount
StatsVersion
OccurredAtUtc
```

### Expected behavior

```text
If IncomingStatsVersion > CurrentInteractionStatsVersion:
    set ViewCount = incoming ViewCount
    set LikeCount = incoming LikeCount
    set VisibleCommentCount = incoming VisibleCommentCount
    set InteractionStatsVersion = incoming StatsVersion
    update InteractionLastMessageId / LastInteractionAppliedAtUtc
Else:
    ignore as duplicate or stale
```

### Rules

Reading must not:

```text
Consume one message per article view.
Blindly increment counters from delivered events.
Rebuild Interaction truth from counter snapshots.
Use Interaction counters to decide article visibility.
Synchronously query Interaction for normal public response composition.
```

Counter lag is acceptable:

```text
Delayed Interaction snapshot
    -> public article remains readable
    -> counters may show last-known values or configured defaults.
```

---

## 8. Query Contracts

## 8.1. `GetArticles`

Returns public article summaries from Reading-owned projections.

Rules:

```text
Only ArticleReadModel rows with IsPublic = true and Published status are returned.
Paging is required.
Sort values are allowlisted.
Paging uses deterministic tie-breakers.
Optional media/counter enrichment may be stale or missing.
```

Recommended default ordering:

```text
PublishedAtUtc DESC, ArticlePublicId DESC
```

---

## 8.2. `GetArticleByPublicId`

Returns one public article detail by article public identity.

Flow:

```text
Read ArticleReadModel by ArticlePublicId
    -> verify local public visibility
    -> return response or safe 404
```

Rules:

```text
No synchronous Content/SEO/Media/Interaction dependency.
Missing optional media or counters does not block a safely public article.
```

---

## 8.3. `GetArticleBySlug`

Returns one public article detail by slug from Reading-owned route state.

V1 flow:

```text
Client requests public slug
    -> Reading looks up ArticleSeoRouteProjection by Scope + Slug
    -> require active and safe route
    -> load ArticleReadModel by ArticlePublicId
    -> require article public visibility
    -> compose response from Reading-owned projections
    -> return detail or safe 404
```

Rules:

```text
SEO owns canonical route truth.
Reading owns its asynchronously projected serving route.
Reading does not call SEO synchronously in the normal slug read path.
Inactive, missing or unsafe route returns safe 404.
Public article visibility still comes from ArticleReadModel.
```

---

## 8.4. `SearchArticles`

Returns matching public articles from Reading-owned search/read projection state.

Rules:

```text
Only public rows may be returned.
Search lag is allowed.
Safe omission is preferred over stale exposure.
Search failure must not cause hidden content exposure.
```

---

## 8.5. `GetRelatedArticles`

Returns related public articles.

Rules:

```text
Current article is excluded.
Only safely public articles are returned.
Missing related signals degrades to empty list or deterministic fallback.
Optional interaction counters must not decide visibility.
```

---

## 9. Sorting and Filtering Contract

V1 allowed baseline sort values:

```text
-publishedAt
publishedAt
```

Default:

```text
-publishedAt
```

Rules:

```text
Sort values must be allowlisted.
Paging must be stable.
Derived counter values may be displayed but do not establish a V1 popularity-ranking pipeline.
```

Popularity/trending sorting is deferred until a defined ranking projection and consistency contract exist.

Supported filters may include:

```text
Category
Tag
Keyword search
Paging values
```

---

## 10. Counter Contract

Interaction owns counter truth and snapshot publication.

Reading stores only a consumed serving copy:

```text
ViewCount
LikeCount
VisibleCommentCount
InteractionStatsVersion
```

Counter behavior:

```text
Counters may lag.
Counters may be absent before the first Interaction snapshot arrives.
Counters may safely default to zero or configured response defaults.
Counters must be replaced by newer known-value snapshots only.
```

Reading must avoid:

```text
Per-event increments
Counter-based visibility decisions
Sync fallback calls to Interaction during ordinary public reads
```

---

## 11. Degradation Contract

| Enrichment / projection lane | Safe degradation |
|---|---|
| Content public visibility | Fail closed; do not serve uncertain content |
| SEO route for slug request | Fail closed; return safe 404 if route unavailable/unsafe |
| Cover media | Return article with null/omitted media fields |
| Interaction counters | Return last-known values or default zero values |
| Related articles | Return empty list or deterministic fallback |
| Search enrichment | Safe omission according to query policy |

Principle:

```text
Core visibility uncertainty fails closed.
Optional enrichment lag degrades safely.
```

---

## 12. Idempotency, Replay and Reconciliation Contract

Reading handlers must be safe under:

```text
Duplicate delivery
Out-of-order delivery
Consumer restart
Outbox retry
Broker redelivery
Manual replay
Bounded reconciliation
Projection rebuild
```

Required protections:

```text
Durable message dedupe by MessageId / ConsumerName where adopted
Source-specific version-aware apply
Idempotent projection upsert
Stale-version rejection
Resync/rebuild posture
```

Examples:

```text
Duplicate Interaction counter snapshot
    -> no repeated effect.

Older Interaction StatsVersion after newer snapshot
    -> ignore.

Older SEO route version after newer route projection
    -> ignore.

Content projection uncertain or requires repair
    -> do not expose article publicly.
```

---

## 13. Rebuildability Contract

Reading projections must be recoverable from source-owned state or approved retained projection inputs.

| Reading projection state | Recovery source |
|---|---|
| Article core/read visibility | Content |
| Slug route and SEO metadata | SEO |
| Cover/media enrichment | Media |
| Public interaction counters | Interaction snapshot/reconciliation source |

Rules:

```text
RabbitMQ is transport, not the permanent replay source.
Rebuild must not mutate upstream truth.
Full rebuild must not expose partial candidate output as complete serving state.
Interaction counters are rebuilt/reconciled from Interaction, not calculated by Reading.
```

---

## 14. Transaction Boundary Contract

A Reading consumer transaction may update:

```text
Reading-owned projection rows
Source-specific applied versions
Consumed-message/dedupe state
Local diagnostics metadata
```

It must not update:

```text
Content truth
SEO truth
Media truth
Interaction truth
Notifications truth
Audit truth
```

Source modules do not wait for Reading projection completion as part of their truth transactions.

---

## 15. Normal Public Runtime Path

### Public article by id

```text
Public API
    -> Reading ArticleReadModel
    -> Safe visibility check
    -> Response
```

### Public article by slug

```text
Public API
    -> Reading ArticleSeoRouteProjection
    -> Reading ArticleReadModel by ArticlePublicId
    -> Safe visibility check
    -> Response
```

### Interaction view signal

View tracking is separate from public rendering:

```text
Article response succeeds from Reading
    -> client sends separate view contribution request to Interaction
```

Reading must not block response delivery while waiting for view persistence.

---

## 16. Non-Goals

Reading V1 does not:

```text
Synchronously resolve slug through SEO on the public hot path
Synchronously query Interaction for counters
Consume raw individual view events
Calculate like/comment/view truth
Own Content lifecycle legality
Own SEO slug uniqueness
Own Media asset validity
Own moderation/report workflow
Implement popularity/trending ranking without an explicit later design
Guarantee immediate projection freshness after source truth changes
```

---

## 17. Final V1 Posture

```text
Reading is a fully asynchronous public serving module.

Content publishes public article projection state consumed into ArticleReadModel.

SEO publishes route/metadata state consumed into ArticleSeoRouteProjection.

Media may publish public presentation enrichment consumed by Reading.

Interaction publishes interaction.article_counters_projection_published
containing ViewCount, LikeCount, VisibleCommentCount and StatsVersion.

Reading composes public responses only from Reading-owned projection data.

Slug-based detail reads use Reading-owned ArticleSeoRouteProjection,
not a synchronous Reading-to-SEO request.

Interaction counters are derived enrichment and may lag;
they never determine article public visibility.

Each upstream lane uses its own version marker,
and Reading applies only newer known-value snapshots.
```