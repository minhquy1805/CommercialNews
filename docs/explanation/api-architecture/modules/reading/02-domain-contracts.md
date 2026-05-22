# Reading — Domain Contracts (V1)

Reading is the public read-side module for CommercialNews.

Reading owns public query semantics, response composition, and derived read projections.

Reading does not own article truth.

Reading follows upstream source truth asynchronously and serves article content from Reading-owned projection data. In baseline V1, slug-based reads use explicit SEO route resolution before Reading loads the article projection.

---

## 1) Module role

Reading is responsible for:

* public article listing
* public article detail
* slug-based public article detail
* search result composition
* related article composition
* public visibility enforcement from source-derived projection state
* graceful degradation of optional enrichments
* read-optimized projection ownership

Reading is not responsible for:

* creating or editing articles
* publishing or unpublishing articles
* archiving or soft-deleting articles
* generating canonical slugs
* managing media asset lifecycle
* owning interaction counters truth
* sending notifications
* owning audit truth

---

## 2) Source ownership

| Concern | Source of truth | Reading usage |
|---|---|---|
| Article lifecycle | Content | Projected status and visibility |
| Article title/body/summary | Content | Projected public content |
| Category/tag data | Content | Projected filters and display fields |
| Slug and canonical metadata | SEO | Explicit route resolution for slug-based reads; optional projected SEO fields for response/rebuild optimization |
| Media assets and primary media | Media | Projected cover/media fields |
| Views, likes, comments | Interaction | Projected counters or summaries |
| Public read model | Reading | Owned derived serving data |

Copied data inside Reading does not transfer ownership.

If source data changes, the source-owning module must publish events or provide rebuild/reconciliation input.

---

## 3) Core read model

### ArticleReadModel

`ArticleReadModel` is the public serving projection of an article.

It is derived state.

It may lag behind source truth.

It must be rebuildable from authoritative source modules.

Expected fields:

| Field | Purpose |
|---|---|
| `ArticleId` | Internal source article id from Content |
| `ArticlePublicId` | Public article identifier |
| `Slug` | Optional projected public slug from SEO; not the baseline routing authority in V1 |
| `CanonicalUrl` | Optional projected canonical URL from SEO |
| `MetaTitle` | Optional projected SEO title |
| `MetaDescription` | Optional projected SEO description |
| `Title` | Projected public title |
| `Summary` | Projected public summary |
| `Body` | Projected public body/rendered content |
| `CategoryId` | Projected category id |
| `CategoryName` | Projected category name |
| `AuthorUserId` | Projected author id |
| `AuthorDisplayName` | Projected author display name |
| `CoverMediaId` | Projected primary media id |
| `CoverMediaUrl` | Projected primary media URL |
| `CoverAlt` | Projected cover alt text |
| `Status` | Source-derived article status |
| `IsPublic` | Whether Reading may expose this row publicly |
| `PublishedAtUtc` | Source publish timestamp |
| `UpdatedAtUtc` | Source update timestamp |
| `SearchText` | Denormalized searchable text |
| `ViewCount` | Projected view count |
| `LikeCount` | Projected like count |
| `CommentCount` | Projected comment count |
| `SourceVersion` | Last applied source aggregate version |
| `LastEventMessageId` | Last applied or observed message id |
| `LastSourceOccurredAtUtc` | Source event occurrence timestamp |
| `LastSyncedAtUtc` | Projection sync timestamp |

---

## 4) Public visibility contract

Reading public APIs must only return publicly visible content.

Public visibility requires:

* source-derived status is `Published`
* projection `IsPublic = true`
* article is not archived
* article is not soft-deleted
* SEO route is active if accessed by slug
* visibility is not uncertain

If visibility is uncertain:

```text
Unknown visibility => not public.
Safe 404 is preferred over incorrect public exposure.
```

Reading must not expose draft, unpublished, archived, soft-deleted, or visibility-uncertain content.

---

## 5) Freshness and ordering contract

Reading must not use wall-clock timestamps as the primary freshness authority.

Do not use:

* `largest UpdatedAtUtc wins`
* `largest OccurredAtUtc wins`
* `latest ProcessedAtUtc wins`

Use source versioning instead:

* `AggregateId + Version`
* `SourceVersion / LastAppliedVersion`

Approved apply rule:

```text
If IncomingVersion > CurrentSourceVersion:
    apply event
Else:
    ignore or reject as duplicate/stale
```

Timestamps are allowed for:

* business display
* audit/investigation
* projection lag measurement
* reporting
* scheduling

They are not ordering authority.

---

## 6) Event identity contract

Important events consumed by Reading should carry:
Baseline V1 Reading consumes Content events for core article projection. Other source events are optional or future optimizations unless explicitly adopted.

| Field | Purpose |
|---|---|
| `MessageId` | Message-level identity and dedupe key |
| `EventType` | Handler routing |
| `AggregateId` | Source aggregate identity |
| `Version` | Per-aggregate freshness marker |
| `OccurredAtUtc` | Event occurrence time for investigation and lag |
| `CorrelationId` | Cross-flow tracing |

Reading handlers should log these fields for every important apply decision.

---

## 7) Projection apply contracts

### 7.1 Content article published

Source event example:

```text
content.article_published
```

Expected behavior:

* upsert `ArticleReadModel`
* set source-derived status to `Published`
* set `IsPublic = true` if visibility rules pass
* update public article fields
* update `SourceVersion`
* update `LastEventMessageId`
* update `LastSourceOccurredAtUtc`
* update `LastSyncedAtUtc`

### 7.2 Content article updated

Source event example:

```text
content.article_updated
```

Expected behavior:

* update projected title, summary, body, category, tags, author, and update timestamp
* preserve or recalculate visibility based on source-derived status
* apply only if incoming version is newer

### 7.3 Content article unpublished

Source event example:

```text
content.article_unpublished
```

Expected behavior:

* set `IsPublic = false`
* update source-derived status
* preserve projection row for idempotency, diagnostics, and rebuild support

### 7.4 Content article archived

Source event example:

```text
content.article_archived
```

Expected behavior:

* set `IsPublic = false`
* update source-derived status to archived
* preserve projection row

### 7.5 Content article soft-deleted

Source event example:

```text
content.article_soft_deleted
```

Expected behavior:

* set `IsPublic = false`
* preserve projection row unless a later hard-purge policy is introduced

### 7.6 SEO route / metadata synchronization

Baseline V1:

* SEO does not have to emit integration events for Reading.
* SEO consumes Content events and maintains SEO-owned slug routing and metadata truth.
* Reading obtains slug routing through explicit SEO route resolution on slug-based reads.

Possible future optimization:

```text
seo.slug_route_changed
seo.metadata_updated
```

If SEO event emission is adopted later, Reading may consume SEO route/metadata events to update optional projected SEO fields.

Expected baseline behavior:

* Reading must not depend on SEO-emitted events for correctness.
* `GET /articles/slug/{slug}` resolves the slug through SEO.
* SEO returns `ResourcePublicId` as a routing aid.
* Reading fetches `ArticleReadModel` by `ArticlePublicId`.
* Reading visibility still decides whether public response may be returned.
* Projected slug/metadata fields in Reading are optional and may lag.

### 7.7 Media primary changed

If Media event emission is adopted, Reading may consume:

```text
media.article_primary_media_changed
```

Expected behavior:

* update cover media fields
* preserve article visibility state
* degrade gracefully if media projection is missing or delayed

### 7.8 Interaction counters changed

If Interaction counter event emission is adopted, Reading may consume:

```text
interaction.article_counters_updated
```

Expected behavior:

* update counters by setting known aggregate values
* avoid blind increments unless raw events are deduped
* allow counters to lag without breaking public read correctness

---

## 8) Query contracts

### 8.1 GetArticles

Returns public article summaries.

Rules:

* only public articles are returned
* paging is required
* allowed sort values must be allowlisted
* stable paging requires deterministic tie-breakers
* optional enrichments may be stale or missing

Recommended stable sort tie-breakers:

```text
publishedAt desc, articlePublicId desc
```

or:

```text
publishedAt desc, articleId desc
```

### 8.2 GetArticleByPublicId

Returns one public article detail by public id.

Rules:

* only public articles are returned
* unknown or non-public articles return safe 404
* missing optional enrichments should be omitted or defaulted safely

### 8.3 GetArticleBySlug

Returns one public article detail by slug.

Baseline V1 flow:

```text
Client requests article by slug
    ↓
Reading calls SEO resolve with scope=public and slug
    ↓
SEO returns ResourcePublicId and route metadata
    ↓
Reading loads ArticleReadModel by ArticlePublicId
    ↓
Reading applies public visibility rules
    ↓
Return detail or safe 404
```

Rules:

* SEO owns `Scope + Slug -> ResourcePublicId` routing truth.
* Reading does not treat projected slug as the baseline routing authority in V1.
* SEO resolve is a routing aid, not final public visibility authority.
* Reading must still require `Status = Published`, `IsPublic = true`, and non-uncertain visibility.
* Unknown slug, inactive route, missing projection, non-public article, or visibility uncertainty returns safe 404.
* If future SEO events or rebuild sync projected slug into Reading, direct Reading slug lookup may be used as an optimization only.

### 8.4 SearchArticles

Returns public articles matching search criteria.

Rules:

* only public articles are returned
* search must not expose hidden content
* search/index/materialized query output remains derived and may lag
* safe omission is preferred over stale exposure

### 8.5 GetRelatedArticles

Returns related public articles.

Rules:

* current article must not be included
* only public articles are returned
* deterministic fallback is required
* same category, shared tags, and recency may be used as signals
* related/trending algorithms must document time semantics if they become important pipelines

---

## 9) Sorting and filtering contract

Reading must define sort and filter behavior explicitly.

V1 allowed sort values:

* `-publishedAt`
* `publishedAt`
* `-popularity`
* `popularity`

Default sort:

```text
-publishedAt
```

Rules:

* sort values must be allowlisted
* unsupported sort values must return validation error or safe default according to API standards
* paging must be stable
* popularity is derived and may lag
* popularity must not override public visibility rules

Supported filters may include:

* category id
* tag id
* keyword search
* paging parameters

---

## 10) Related content contract

Related articles must be deterministic.

Recommended priority:

* same category
* shared tags
* same author if available
* newest public articles as fallback

Rules:

* only public articles are returned
* current article is excluded
* stale related signals must not block detail response
* missing related signals should fall back deterministically

---

## 11) Counter contract

Counters are derived.

Counters may lag or be unavailable.

Examples:

* views
* likes
* comments

Reading may:

* return counters
* return zero counters
* omit counters
* expose a future countersPartial flag if API policy adopts it

Reading must not blindly increment counters under replay.

Preferred counter update shape:

```text
Set counter to known aggregate value.
```

Avoid:

```text
Increment counter on every delivered event.
```

Counter truth belongs to Interaction.

---

## 12) Degradation contract

Optional enrichments may degrade safely.

Examples:

| Enrichment | Safe degradation |
|---|---|
| Cover media | return null or omit cover |
| Media gallery | return empty array |
| SEO metadata | return null fields or projected defaults |
| Counters | return zero, null, omitted, or partial flag |
| Related articles | return empty list or deterministic fallback |
| Search index | safe omission or fallback according to policy |

Degradation must not expose non-public content.

Degradation must not cause public visibility to become permissive.

---

## 13) Idempotency and replay contract

Reading projection handlers must be safe under:

* duplicate message delivery
* consumer restart
* outbox retry
* broker redelivery
* manual replay
* rebuild/reconciliation

Required protections:

* message-level identity using `MessageId`
* projection-level freshness using `SourceVersion`
* idempotent upsert
* stale version rejection
* rebuild/reconciliation posture

Message-level dedupe alone is not sufficient.

Different older messages may arrive after newer ones.

---

## 14) Rebuildability contract

`ArticleReadModel` must have a documented recovery path.

Approved recovery strategies:

* rebuild article core projection from Content truth
* resolve/reconcile slug and metadata from SEO truth
* rebuild media projection from Media truth
* rebuild/reconcile counters from Interaction
* bounded recomputation
* replay from retained operational history where policy allows

RabbitMQ is not the permanent replay source.

Production rebuild should avoid exposing partial output as complete.

Candidate-before-cutover or equivalent safe publication is preferred for full rebuilds.

---

## 15) Transaction boundary contract

Reading projection updates must use bounded local transactions.

A Reading projection transaction may update:

* projection fields
* `SourceVersion`
* `LastEventMessageId`
* `LastSourceOccurredAtUtc`
* `LastSyncedAtUtc`

It must not update source module truth.

It must not open long-running cross-module transactions.

Content, SEO, Media, and Interaction must not wait for Reading projection completion as part of their truth transactions.

---

## 16) Normal path and fallback contract

Normal public read path for list/detail/search/related:

```text
Public API
    ↓
Reading projection
    ↓
Response
```

Slug-based public read path:

```text
Public API
    ↓
SEO route resolve
    ↓
Reading projection by ResourcePublicId
    ↓
Reading visibility check
    ↓
Response
```

Rules:

* SEO resolve is an explicit hot-path dependency for slug routing in baseline V1.
* SEO resolve does not make content public.
* Reading projection remains the serving source for article content.
* Reading visibility still decides safe public response.
* Source fallback must remain explicit and observable.

Policy-controlled fallback may be used when correctness requires it.

Fallback must be explicit.

Fallback must not create hidden cross-module ownership.

Fallback must not make public requests wait on background rebuild or projection catch-up.

---

## 17) Non-goals

Reading does not:

* enforce Content lifecycle state-machine legality
* own SEO slug uniqueness
* own media file validity
* own interaction raw events
* own source aggregate versions
* own outbound side effects
* serve as permanent event history
* guarantee immediate visibility after source truth changes

---
