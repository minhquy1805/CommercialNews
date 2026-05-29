# Reading — Dependencies & Ownership (V1 Async Projections)

Reading is the public serving module for CommercialNews.

Reading serves ordinary public article requests entirely from Reading-owned asynchronous projections.

Related:

```text
../../../architecture/arc42/03-building-blocks-modularity.md
../../../architecture/arc42/13-transactions-and-consistency-v1.md
../../../architecture/arc42/18-stream-processing-and-derived-state-v1.md
../../../architecture/arc42/19-stream-processing-runtime-v1.md

../../../decisions/adr-0013-outbox-and-delivery-semantics-v1.md
../../../decisions/adr-0015-cache-policy-and-invalidation-redis-v1.md
../../../decisions/adr-0018-transaction-boundaries-and-consistency-model-v1.md
../../../decisions/adr-0020-timeout-retry-and-failure-detection-policy-v1.md
../../../decisions/adr-0021-clock-time-and-ordering-policy-v1.md
../../../decisions/adr-0022-versioning-and-fencing-strategy-v1.md
../../../decisions/adr-0025-batch-processing-and-derived-state-policy-v1.md
../../../decisions/adr-0026-batch-job-orchestration-and-materialization-policy-v1.md
../../../decisions/adr-0027-stream-processing-and-derived-state-policy-v1.md
../../../decisions/adr-0028-consumer-idempotency-replay-and-rebuild-policy-v1.md
```

---

## 1. Module Ownership

Reading owns:

```text
ArticleReadModel
ArticleSeoRouteProjection
Public query semantics
Public response composition
Local public-serving visibility evaluation
Reading-side projection freshness metadata
Reading-side consumed-message / apply-decision state
Reading cache policy for public APIs
Reading projection repair and rebuild posture
Reading-owned read-facing derived outputs
```

Reading does not own:

```text
Article lifecycle truth
Article editorial truth
Publication truth
Canonical slug or route truth
Media asset truth
View / like / comment truth
Interaction counter materialization truth
Moderation or report workflow truth
Notification delivery truth
Canonical audit evidence truth
```

Core rule:

```text
Reading owns public serving projections.
Reading does not own the source facts represented inside those projections.
```

---

## 2. Source Ownership

| Concern | Source owner | Reading-owned usage |
|---|---|---|
| Article lifecycle and public visibility | Content | Project into `ArticleReadModel` |
| Article title/body/summary/category/author presentation | Content | Project into `ArticleReadModel` |
| Slug route and canonical metadata | SEO | Project into `ArticleSeoRouteProjection` and response metadata fields |
| Cover/media presentation | Media | Project optional public media fields |
| Public interaction counters | Interaction | Project known-value counter snapshots |
| Public serving response | Reading | Query and compose from local projections |

Rules:

```text
Copied state does not transfer source ownership.

Each source owner publishes projection input asynchronously
or supplies approved rebuild/reconciliation input.

Reading never mutates source-owned truth.
```

---

## 3. Normal Public Dependency Posture

### 3.1. List, detail by public id, search and related reads

```text
Public API
    -> Reading ArticleReadModel / Reading-owned query projections
    -> Local public visibility checks
    -> Response
```

### 3.2. Detail by slug

```text
Public API
    -> Reading ArticleSeoRouteProjection by Scope + Slug
    -> Reading ArticleReadModel by ArticlePublicId
    -> Local route and public visibility checks
    -> Response
```

Rules:

```text
Reading does not synchronously query Content during ordinary public reads.

Reading does not synchronously resolve slug through SEO.

Reading does not synchronously query Media for response composition.

Reading does not synchronously query Interaction for counters.

Missing or unsafe core route/visibility projection fails closed.

Missing optional media/counter enrichment degrades safely.
```

---

## 4. Approved Dependency Shapes

## 4.1. Async Projection Consumption

Reading consumes asynchronous source-owned projection outputs from:

| Producer | Reading result |
|---|---|
| Content | Article content and visibility fields in `ArticleReadModel` |
| SEO | Route and metadata fields in `ArticleSeoRouteProjection` |
| Media | Optional cover/media presentation fields |
| Interaction | Counter snapshot fields in `ArticleReadModel` |

Conceptual inbound projection lanes:

```text
Content     -> Reading article public projection
SEO         -> Reading article route/metadata projection
Media       -> Reading article media presentation projection
Interaction -> Reading article counter snapshot projection
```

Confirmed Interaction inbound event:

```text
interaction.article_counters_projection_published
```

Rules:

```text
Delivery is at-least-once.

Reading handlers must be idempotent.

Each source lane applies its own version marker.

Older source-lane snapshots must not overwrite newer local state.

RabbitMQ is transport, not permanent replay history.
```

---

## 4.2. Reading-Owned Projection Reads

Reading public APIs may read:

```text
reading.ArticleReadModel
reading.ArticleSeoRouteProjection
Reading-owned public search projection where adopted
Reading-owned related-article projection where adopted
Reading-owned cache entries where configured
```

Rules:

```text
Public serving uses local Reading state only.

Core public visibility and slug route safety must be confirmed locally.

Projection diagnostics and internal message metadata must not leak publicly.

Counter and media enrichment may be stale without making visibility permissive.
```

---

## 4.3. Cache-Aside Acceleration

Reading may use cache for:

```text
Public article detail responses
Public article list responses
Public search / related response fragments where adopted
Projection-derived media/counter fragments
```

Rules:

```text
Cache is acceleration only.

Cache does not become source truth.

Cache must not bypass local visibility or route checks.

Locally known deny/unsafe projection state wins over cached public output.

Cache failure must not force a synchronous upstream read fallback.
```

---

## 4.4. Repair and Rebuild Input

Reading may obtain bounded source-owned input for:

```text
Repairing ArticleReadModel
Repairing ArticleSeoRouteProjection
Repairing media enrichment
Repairing Interaction counter enrichment
Running bounded reconciliation
Running safe full rebuilds
```

Rules:

```text
Repair/rebuild is not the ordinary public request path.

Input authority remains with the source-owning module.

Output remains Reading-owned derived state.

Rebuild must be rerun-safe.

Partial candidate output must not be served as complete active state.
```

---

## 5. Forbidden Dependencies

Reading must not:

```text
Write into Content truth tables.
Write into SEO truth tables.
Write into Media truth tables.
Write into Interaction truth tables.
Write into Notification or Audit truth tables.

Synchronously call SEO to resolve slugs in the normal public hot path.
Synchronously query Interaction to compose ordinary counter responses.
Synchronously query Content to confirm visibility on ordinary reads.
Synchronously query Media to fill missing presentation fields on ordinary reads.

Infer public visibility from route existence alone.
Infer engagement truth from projected counters.
Blindly increment counters from delivered interaction messages.
Consume one raw message per article view for public counters.

Treat cache presence as publication truth.
Treat projection presence alone as source truth.
Use timestamp last-write-wins as freshness authority.
Use one shared SourceVersion across independent source modules.
Allow stale projection events to overwrite newer local state.

Block source transactions on Reading projection completion.
Require Notifications or Audit completion for public response.
Use RabbitMQ as permanent replay storage.
Publish incomplete rebuild output as complete serving state.
```

---

## 6. Source Module Expectations

## 6.1. Content

Content owns:

```text
Article lifecycle legality
Editorial truth
Publication and visibility truth
Content-side source versioning
```

Reading expects Content to provide asynchronous public-serving projection input containing committed article state, such as:

```text
ArticlePublicId
Title
Summary
Body
Category / author presentation fields where included
Status
IsPublic
PublishedAtUtc
ArticleUpdatedAtUtc
ContentSourceVersion
```

Reading applies Content input into `ArticleReadModel`.

Rules:

```text
Content determines whether article state is public.

Reading does not judge whether Content lifecycle transitions were legal.

A locally applied non-public Content snapshot must immediately prevent public serving.

Before a newer Content snapshot reaches Reading,
bounded eventual-consistency lag may exist and must be observed.
```

---

## 6.2. SEO

SEO owns:

```text
Slug creation rules
Canonical route truth
Scope + Slug routing uniqueness
Canonical URL / metadata truth
Route activation and deactivation decisions
```

Reading expects SEO to publish asynchronous route/metadata projection input for:

```text
ArticleSeoRouteProjection
```

Projected route state may include:

```text
Scope
Slug
ArticlePublicId
CanonicalUrl
MetaTitle
MetaDescription
IsActive
SeoSourceVersion
RequiresResync where applicable
```

Normal slug serving path:

```text
Reading ArticleSeoRouteProjection
    -> ArticleReadModel
    -> Local route and visibility checks
    -> Public response or safe 404
```

Rules:

```text
SEO remains canonical routing owner.

Reading owns only its local serving projection of SEO state.

Reading does not call SEO synchronously during ordinary slug requests.

Inactive, missing or unsafe local route state fails closed.

A valid route alone is not enough to expose article content;
ArticleReadModel must also be locally public.
```

---

## 6.3. Media

Media owns:

```text
Media asset lifecycle
Storage location
Media metadata
Article-media attachment truth
Primary media selection truth
```

Reading expects Media to publish asynchronous public presentation input for fields such as:

```text
CoverMediaPublicId
CoverMediaUrl
CoverAlt
MediaSourceVersion
```

Rules:

```text
Media enrichment is optional for public serving correctness.

Media updates must not change article visibility.

Missing or delayed media state may be omitted or returned as null.

Reading must not synchronously query Media to patch ordinary public responses.
```

---

## 6.4. Interaction

Interaction owns:

```text
ArticleViewCount
ArticleLike truth
Comment truth
Comment report and moderation workflow truth
ArticleInteractionStats
Counter materialization and publication policy
View eligibility / abuse / suppression policy
```

Reading consumes:

```text
interaction.article_counters_projection_published
```

Expected snapshot values:

```text
ArticlePublicId
ViewCount
LikeCount
VisibleCommentCount
StatsVersion
OccurredAtUtc
```

Reading applies the snapshot into local serving counter fields.

Rules:

```text
Reading sets counters from newer known-value snapshots.

Reading does not calculate or reconstruct Interaction truth.

Reading does not consume raw view/like/comment events to build totals.

Reading does not query Interaction synchronously during public response composition.

Counter lag or absence does not prevent safely public content from being served.
```

---

## 7. Reading-Owned Derived Outputs

Reading may own derived outputs such as:

```text
ArticleReadModel
ArticleSeoRouteProjection
Reading-side media enrichment fields
Reading-side Interaction counter fields
Public search projections
Related-article projections
Cache entries derived from safe Reading projection state
Projection diagnostics
Rebuild candidate artifacts
Reconciliation reports
Consumed-message/apply-decision state
```

These outputs must remain:

```text
Derived
Observable
Rebuildable or repairable
Version-aware where ordered freshness matters
Subordinate to upstream source ownership
```

Reading V1 does not introduce:

```text
Popularity/trending ranking pipeline
Raw interaction analytics pipeline
Reading-owned counter aggregation truth
```

Those require a later explicit design.

---

## 8. Projection Freshness Ownership

Reading owns local applied-version metadata for each independent source lane.

| Source lane | Reading freshness field |
|---|---|
| Content article state | `ContentSourceVersion` |
| SEO route state | `SeoSourceVersion` |
| Media presentation state | `MediaSourceVersion` |
| Interaction counter snapshot | `InteractionStatsVersion` |

Reading may also store source-specific message/apply metadata:

```text
ContentLastMessageId
SeoLastMessageId
MediaLastMessageId
InteractionLastMessageId

LastContentAppliedAtUtc
LastSeoAppliedAtUtc
LastMediaAppliedAtUtc
LastInteractionAppliedAtUtc
```

Rules:

```text
MessageId protects against processing the same message twice.

Source-specific version protects against stale overwrite.

Timestamps measure lag and support investigation only.

Versions from different source modules are not comparable.
```

---

## 9. Event and Broker Dependency Rules

Async projection path:

```text
Source module commits truth + OutboxMessage
    -> Worker publishes integration/projection event
    -> RabbitMQ delivers message
    -> Reading consumer receives message
    -> Reading checks MessageId and source-specific version
    -> Reading applies or ignores the snapshot safely
    -> Reading records apply diagnostics
```

Rules:

```text
Outbox represents producer publication intent.

RabbitMQ transports messages.

Reading consumer owns Reading-side idempotent apply.

At-least-once delivery is expected.

Global event ordering is not assumed.

Source-lane version guards are required.

RabbitMQ is not the permanent recovery source.
```

Recovery relies on:

```text
Approved source-owned rebuild input
Reading projection reconciliation
Bounded replay where retained input exists
Deterministic local repair
```

---

## 10. Transaction Boundary Ownership

Source modules own their local truth transactions.

Examples:

```text
Content truth update + Content OutboxMessage
SEO route truth update + SEO OutboxMessage
Media presentation truth update + Media OutboxMessage
Interaction stats update + Interaction OutboxMessage
```

Reading is not part of those source transactions.

A Reading consumer transaction may update:

```text
Reading projection fields
Source-specific applied version
Last applied message metadata
Consumed-message / apply-decision state
Local repair/resync diagnostic state
```

A Reading consumer transaction must not update:

```text
Content truth
SEO truth
Media truth
Interaction truth
Notification truth
Audit truth
```

Rule:

```text
Source modules do not wait for Reading projection completion.
```

---

## 11. Public Visibility Ownership

Content owns publication truth.

Reading owns local serving enforcement based on its applied Content projection.

### Public serving condition

```text
ArticleReadModel.Status = Published
AND ArticleReadModel.IsPublic = true
AND local article visibility is not unsafe / requires-resync
```

### Deny condition

```text
ArticleReadModel missing
OR Status is not Published
OR IsPublic = false
OR local visibility is unsafe / requires-resync
    -> Safe 404 / omission
```

### Eventual-consistency clarification

Because Reading is asynchronously updated:

```text
Content may already have committed a non-public change
while Reading has not yet received or applied that snapshot.
```

V1 accepts this as bounded eventual-consistency lag.

Reading must:

```text
Monitor Content -> Reading projection lag.
Apply non-public snapshots promptly and idempotently.
Reject stale snapshots that could re-expose older public state.
Support repair/reconciliation for drift.
Fail closed once local state is known non-public or unsafe.
```

---

## 12. Route Ownership

SEO owns canonical public route truth.

Reading owns:

```text
ArticleSeoRouteProjection
```

for local public serving.

Slug-based public path:

```text
GET /api/v1/articles/slug/{slug}
    -> Query ArticleSeoRouteProjection by Scope = public and Slug
    -> Require IsActive = true
    -> Require RequiresResync = false
    -> Load ArticleReadModel by ArticlePublicId
    -> Require local article public visibility
    -> Return response or safe 404
```

Rules:

```text
Reading does not synchronously resolve slug through SEO in V1.

SEO route projection decides local slug mapping only.

Route success alone does not grant public visibility.

Missing, inactive or unsafe route fails closed.

A delayed SEO route change may cause temporary bounded route lag.
```

---

## 13. Counter Ownership

Interaction owns counter truth and snapshot publication.

Reading owns only a local serving copy:

```text
ViewCount
LikeCount
VisibleCommentCount
InteractionStatsVersion
```

Counter apply rule:

```text
If IncomingStatsVersion > CurrentInteractionStatsVersion:
    Set ViewCount = incoming ViewCount
    Set LikeCount = incoming LikeCount
    Set VisibleCommentCount = incoming VisibleCommentCount
    Set InteractionStatsVersion = incoming StatsVersion
Else:
    Ignore duplicate or stale snapshot
```

Reading must not:

```text
Increment counters from repeated event delivery.
Infer whether a like/comment operation committed from local counters.
Use counters to determine public visibility.
Synchronously request fresher counters from Interaction during public reads.
```

Safe degradation:

```text
No applied counter snapshot
    -> return documented zero/default counters.

Delayed counter snapshot
    -> return last-known counters.
```

---

## 14. Cache Ownership

Reading may own cache policy for public responses produced from Reading projection state.

Reading cache may store:

```text
Safely public article detail responses
Safely public article list responses
Search/related fragments where adopted
Projection-derived optional enrichment fragments
```

Reading cache must not store or expose:

```text
Draft-only content
Known non-public content
Known unsafe route/visibility responses as public content
Admin/moderation fields
Internal event/message diagnostics
Audit evidence
```

Rules:

```text
Cache is acceleration only.

Cache must not become an upstream fallback.

Locally known route/visibility denial wins over cached public content.

Cache refresh failure must not change source ownership or public visibility rules.
```

---

## 15. Repair and Rebuild Ownership

Reading may run repair/rebuild workflows for Reading-owned derived state only.

Allowed workflows:

```text
Rebuild ArticleReadModel from Content-approved input.
Rebuild ArticleSeoRouteProjection from SEO-approved input.
Repair media enrichment from Media-approved input.
Repair counter enrichment from Interaction-approved stats input.
Reconcile consumed-message/apply diagnostics.
Validate candidate projection state before cutover.
```

Reading must not:

```text
Repair upstream truth.
Recalculate Interaction truth from Reading state.
Reactivate routes from stale Reading assumptions.
Serve partial candidate rebuild output as complete active state.
Treat RabbitMQ as the sole rebuild source.
```

Full rebuild posture:

```text
Generate candidate projection
    -> Validate candidate completeness and safety
    -> Cut over only when acceptable
```

---

## 16. Coordination-Sensitive Workflow Rule

Reading normally relies on:

```text
Idempotent consumer apply
Source-specific version gates
Bounded retry
Rerun-safe repair
Candidate validation before cutover
```

If a future rebuild/cutover workflow requires exclusive ownership, it must define:

```text
Authoritative ownership state
Monotonic generation or fencing token
Resource-side stale-owner rejection
Recovery behavior when ownership is uncertain
```

Forbidden assumptions:

```text
Only one worker currently exists.
An in-memory leader flag proves ownership.
A timeout alone transfers ownership.
Concurrent rebuild publication cannot occur.
```

Safe non-progress is preferred over unsafe dual publication.

---

## 17. Dependency Failure Ownership

## 17.1. Producer / publication failure

Owned by the source/outbox publication path.

Examples:

```text
Source truth committed but Outbox publication delayed.
Producer worker cannot publish yet.
RabbitMQ handoff has not completed.
```

Impact on Reading:

```text
Reading projection may lag.
New content/route/media/counters may not appear yet.
Previously applied local state remains served according to local rules.
```

## 17.2. Reading consumer failure

Owned by Reading consumer processing.

Examples:

```text
Message delivered but Reading DB update fails.
Duplicate or stale input is detected.
Version gap or unsafe projection state is detected.
```

Impact:

```text
Projection may lag.
Retry, resync or repair policy applies.
Metrics and diagnostics must record outcome.
```

## 17.3. Public serving failure

Owned by Reading public API path.

Examples:

```text
Reading projection store unavailable.
Route projection missing or unsafe.
Article projection missing or unsafe.
Cache unavailable.
Optional enrichment missing.
```

Result:

```text
Required safe serving state unavailable -> safe 404 / 503 according to API contract.
Optional enrichment missing -> degrade safely.
```

---

## 18. What Reading May Expect from Other Modules

Reading may expect:

| Module | Expected contract |
|---|---|
| Content | Emits public article projection input and supplies rebuild/reconciliation input |
| SEO | Emits public route/metadata projection input and supplies rebuild/reconciliation input |
| Media | Emits adopted public presentation projection input and supplies rebuild/reconciliation input |
| Interaction | Emits `interaction.article_counters_projection_published` and supplies counter repair input |
| Outbox / RabbitMQ | Provides at-least-once async transport semantics, not exactly-once or permanent replay |
| Platform observability | Exposes lag, failure, retry and repair signals |

---

## 19. What Other Modules May Expect from Reading

Other modules may expect Reading to:

```text
Serve public article responses from Reading-owned projections.

Consume source projection messages idempotently.

Apply newer source-lane snapshots only.

Reject duplicate and stale input safely.

Fail closed for locally missing/unsafe core route or visibility state.

Degrade missing media/counter enrichment safely.

Not synchronously call source modules during ordinary public reads.

Not mutate source truth.

Not block source truth transactions.

Keep important serving projections repairable and rebuildable.

Expose projection lag and apply-decision observability.
```

---

## 20. What Nobody May Assume

No module may assume:

```text
Reading owns publication truth.
Reading owns canonical slug truth.
Reading owns media truth.
Reading owns Interaction counter truth.
Route projection alone is sufficient to expose an article.
Counter presence proves article readability.
Cache presence proves article readability.
Reading projection is instantly consistent with upstream truth.
Exactly-once delivery is guaranteed.
Timestamps are freshness authority.
RabbitMQ is permanent history.
Partial rebuild output is safe to expose.
Idempotency is optional because only one worker currently runs.
```

---

## 21. Evolution Rules

Future Reading evolution may introduce:

```text
Richer search projections
Related-article projection improvements
Explicit ranking / popularity pipeline
Personalized recommendation projections
Projection checkpointing
External search infrastructure
More advanced cache hierarchy
Automated rebuild orchestration
```

Any future evolution must preserve:

```text
Content owns article publication truth.

SEO owns canonical route truth.

Media owns media truth.

Interaction owns engagement and counter truth.

Reading-owned outputs remain derived unless explicitly reclassified.

Ordinary public serving remains projection-based.

Core local route/visibility uncertainty fails closed.

Source-specific version-aware apply remains mandatory.

Important derived outputs remain repairable/rebuildable.
```

---

## 22. Final Dependency Posture

```text
Reading is a fully asynchronous public serving module.

Content supplies article content and visibility projections.

SEO supplies public slug route and metadata projections.

Media supplies optional public presentation projections.

Interaction supplies versioned public counter snapshots through:
    interaction.article_counters_projection_published

Reading serves public article responses from:
    ArticleReadModel
    ArticleSeoRouteProjection
    optional Reading-owned cache derived from safe projection state

Reading does not synchronously call source modules in ordinary public read paths.

Each source lane applies its own version marker.

Reading owns serving behavior and projection safety,
while upstream modules retain truth ownership.
```