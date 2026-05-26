# Reading — Idempotency & Consistency (V1 Async Projections)

This document defines Reading-specific consistency guarantees for:

```text
Public read APIs
Async projection consumption
Duplicate / stale / out-of-order event handling
Slug-route serving from Reading-owned projection
Interaction counter snapshot application
Cache safety
Repair and rebuild workflows
```

Related system-wide rules:

```text
../../../architecture/arc42/11-replication-v1.md
../../../architecture/arc42/13-transactions-and-consistency-v1.md
../../../architecture/arc42/14-distributed-systems-assumptions-v1.md
../../../architecture/arc42/15-consistency-ordering-and-consensus-v1.md
../../../architecture/arc42/16-batch-processing-and-derived-data-v1.md
../../../architecture/arc42/17-dataflow-and-batch-workflows-v1.md
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

## 1. Consistency Model

Reading is an asynchronously maintained public serving module.

Reading owns:

```text
ArticleReadModel
ArticleSeoRouteProjection
Public query semantics
Public response composition
Reading-side projection freshness metadata
Reading-side consumer apply/dedupe behavior
Repair and rebuild of Reading-owned projections
```

Reading does not own:

```text
Content publication truth
SEO canonical route truth
Media asset truth
Interaction counter truth
Notification delivery truth
Canonical audit evidence
```

Source ownership remains:

| Concern | Source owner | Reading usage |
|---|---|---|
| Article content and public visibility | Content | Project into `ArticleReadModel` |
| Slug route and SEO metadata | SEO | Project into `ArticleSeoRouteProjection` |
| Cover/media presentation | Media | Project optional media enrichment |
| Views, likes and visible comment totals | Interaction | Project counter snapshot fields |
| Public serving response | Reading | Query local projections only |

Core rule:

```text
Reading serves ordinary public requests from Reading-owned projections only.
```

Reading does not synchronously query Content, SEO, Media or Interaction during the normal public read path.

---

## 2. Truth vs Derived Serving State

### 2.1. Source truth

Source truth is committed in the module that owns it.

Examples:

```text
Content publishes or unpublishes an article.
SEO activates, changes or deactivates a public slug route.
Media changes public cover/media presentation.
Interaction materializes a new public counter snapshot.
```

### 2.2. Reading derived state

Reading state is derived serving state.

It may be:

```text
Delayed
Missing
Stale within accepted async lag
Replayed
Repaired
Rebuilt
Marked unsafe / requires resync
```

Reading projection state must not be treated as upstream truth.

Reading must preserve enough source-version and message metadata to:

```text
Reject stale input
Investigate lag
Repair divergence
Rebuild safely
```

---

## 3. Normal Public Read Path

### 3.1. Article list / detail by public id

```text
Public API
    -> Reading ArticleReadModel
    -> Local visibility check
    -> Response
```

### 3.2. Article detail by slug

```text
Public API
    -> Reading ArticleSeoRouteProjection by Scope + Slug
    -> Reading ArticleReadModel by ArticlePublicId
    -> Local route and visibility checks
    -> Response
```

Rules:

```text
Reading does not synchronously resolve slug through SEO.

Reading does not synchronously load counters from Interaction.

Reading does not synchronously confirm public state through Content.

Missing or unsafe core projection state fails closed.
```

---

## 4. Source-Specific Versioning

Reading consumes independent projection lanes from multiple source owners.

A single shared `SourceVersion` is not sufficient because versions from different modules are not comparable.

### Required version lanes

| Source input | Reading freshness marker |
|---|---|
| Content article projection | `ContentSourceVersion` |
| SEO route projection | `SeoSourceVersion` |
| Media presentation projection | `MediaSourceVersion` |
| Interaction counter snapshot | `InteractionStatsVersion` |

Apply rule per source lane:

```text
If IncomingVersion > CurrentAppliedVersion:
    apply known-value snapshot
Else:
    ignore as duplicate or stale input
```

Forbidden comparison:

```text
Do not compare ContentSourceVersion against SeoSourceVersion.
Do not compare MediaSourceVersion against InteractionStatsVersion.
Do not use timestamps to arbitrate source freshness.
```

---

## 5. Delivery Assumption

CommercialNews V1 uses:

```text
Source transaction + Outbox
    -> RabbitMQ transport
    -> Reading consumer
```

Delivery is at-least-once.

Reading must assume:

```text
The same message may be delivered more than once.
A consumer may crash and restart.
An older message may arrive after a newer message.
A publisher may retry.
A message may be manually replayed.
Projection repair or rebuild may rerun bounded input.
```

Exactly-once delivery is not assumed.

Reading targets effectively-once projection outcomes through:

```text
Message-level dedupe
Source-specific version-aware apply
Idempotent known-value upsert
Safe stale-message rejection
Repair / rebuild posture
```

---

## 6. Event Identity and Apply Metadata

Inbound messages used to build important Reading projections must carry:

| Field | Purpose |
|---|---|
| `MessageId` | Message-level dedupe identity |
| `EventType` | Handler routing |
| `AggregateType` | Source aggregate/projection classification |
| `AggregateId` / `AggregatePublicId` | Source identity |
| `AggregateVersion` or projection version | Ordered freshness marker for that source lane |
| `Payload` | Known-value snapshot data |
| `CorrelationId` | Cross-module tracing |
| `OccurredAtUtc` | Diagnostics and lag measurement |

Reading consumer diagnostics should capture:

```text
ConsumerName
MessageId
EventType
Source lane
IncomingVersion
CurrentAppliedVersion
ApplyDecision
CorrelationId
ReceivedAtUtc
ProcessedAtUtc
FailureCode where applicable
```

---

## 7. Message-Level Idempotency

Message-level idempotency protects against the same delivered message being processed more than once.

Required invariant:

```text
Unique (ConsumerName, MessageId)
```

Expected behavior for duplicate delivery:

```text
Same MessageId arrives again
    -> do not reapply mutation
    -> acknowledge or record duplicate outcome safely
    -> do not republish downstream side effects
```

Message-level dedupe alone is not enough because a different older message may arrive after a newer message has already been applied.

---

## 8. Projection-Level Idempotency and Stale Input

Projection-level freshness prevents older state from overwriting newer applied state.

### Example: Interaction counter snapshot

```text
CurrentInteractionStatsVersion = 12
IncomingStatsVersion = 11
```

Expected behavior:

```text
Ignore incoming snapshot.
Do not overwrite ViewCount / LikeCount / VisibleCommentCount.
Record stale apply decision if diagnostics require it.
```

### Example: SEO route projection

```text
CurrentSeoSourceVersion = 8
IncomingSeoSourceVersion = 7
```

Expected behavior:

```text
Ignore incoming route state.
Do not reactivate an older route mapping.
```

### Example: Content visibility projection

```text
CurrentContentSourceVersion = 20 with IsPublic = false
IncomingContentSourceVersion = 19 with IsPublic = true
```

Expected behavior:

```text
Ignore stale Content snapshot.
Never re-expose the article from older local input.
```

Version guards should be enforced at the repository/stored-procedure boundary, not only in application memory.

---

## 9. Snapshot vs Delta Rule

Reading V1 should consume known-value snapshot-shaped projection events.

Approved shape:

```text
Incoming snapshot contains the complete values Reading needs to set
for that source lane.
```

Examples:

```text
Content article public projection snapshot
SEO article route snapshot
Media public cover/presentation snapshot
Interaction counter snapshot
```

Approved behavior:

```text
Apply newer snapshot by setting known values.
```

Reading V1 must not build public serving counters from blind delta processing such as:

```text
On each delivered interaction event:
    ViewCount = ViewCount + 1
```

Reason:

```text
At-least-once delivery and replay would make blind increments unsafe.
```

If a future lane introduces delta events, it must define strict-order or resync behavior before adoption.

---

## 10. Timestamp Policy

Reading must not use wall-clock timestamps as ordering authority.

Do not use:

```text
Largest UpdatedAtUtc wins
Largest OccurredAtUtc wins
Latest ProcessedAtUtc wins
```

Use:

```text
Source lane identity + source-specific version
```

Timestamps remain useful for:

```text
Public display
Projection lag measurement
Diagnostics
Audit investigation
Operational reporting
Scheduling repair/rebuild workflows
```

---

## 11. Content Visibility Consistency

Content owns public article visibility truth.

Reading applies Content-owned projection snapshots into `ArticleReadModel`.

### Locally known safe-public condition

A Reading row may be served publicly only when local state confirms:

```text
Status = Published
AND IsPublic = true
AND visibility state is not unsafe / resync-required
```

### Locally known deny condition

Reading must deny exposure when local state says:

```text
IsPublic = false
OR Status is not Published
OR visibility state is unsafe / requires resync
OR ArticleReadModel is missing
```

### Important bounded-lag clarification

Reading is asynchronous. Therefore, if Content has committed an unpublish/archive/soft-delete but the corresponding projection update has **not yet reached Reading**, Reading may temporarily still hold the previous public projection.

V1 accepts this as bounded eventual-consistency lag.

Required controls:

```text
Monitor Content -> Reading projection lag.
Keep lag within defined SLO.
Apply non-public snapshots urgently and idempotently.
Fail closed immediately once local state is known unsafe or non-public.
Support reconciliation / repair when drift is detected.
```

Reading must not claim that local projections instantly reflect newly committed source truth.

---

## 12. SEO Route Consistency

SEO owns canonical slug and routing truth.

Reading consumes SEO route projection state into:

```text
ArticleSeoRouteProjection
```

Slug-based reads require local route state:

```text
Scope = public
AND Slug matches request
AND IsActive = true
AND RequiresResync = false
```

Then Reading must also confirm article public visibility in `ArticleReadModel`.

### Async route lag behavior

Because route projection is asynchronous:

| Situation | Public behavior |
|---|---|
| New SEO route committed but not yet projected to Reading | New slug may temporarily return safe 404 |
| Route deactivated in SEO but deactivation not yet projected | Old local route may remain usable within accepted bounded lag |
| Route locally marked inactive | Return safe 404 |
| Route locally marked `RequiresResync` | Return safe 404 |
| Route exists but article projection is non-public | Return safe 404 |

Required controls:

```text
Monitor SEO -> Reading route projection lag.
Use source-version guards.
Reject stale route reactivation.
Repair route projection drift when detected.
```

Reading does not synchronously call SEO as a hidden correctness fallback.

---

## 13. Media Enrichment Consistency

Media owns public media truth.

Reading may consume media presentation snapshots into optional fields such as:

```text
CoverMediaPublicId
CoverMediaUrl
CoverAlt
MediaSourceVersion
```

Rules:

```text
Media projection updates must not change article visibility.
Media lag must not block safely public article content.
Older MediaSourceVersion must not overwrite newer presentation state.
```

Safe degradation:

```text
Missing media snapshot
    -> return null/omitted media fields.

Delayed media snapshot
    -> return last-known or absent media fields according to API contract.
```

---

## 14. Interaction Counter Consistency

Interaction owns:

```text
ArticleViewCount
ArticleLike truth
Comment truth
ArticleInteractionStats
```

Reading consumes:

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

### Reading apply rule

```text
If IncomingStatsVersion > CurrentInteractionStatsVersion:
    ViewCount = Incoming.ViewCount
    LikeCount = Incoming.LikeCount
    VisibleCommentCount = Incoming.VisibleCommentCount
    InteractionStatsVersion = Incoming.StatsVersion
Else:
    ignore duplicate or stale snapshot
```

### Counter guarantees

```text
Counters are derived enrichment.
Counters may lag.
Counters may be missing before first snapshot.
Counters do not determine article visibility.
Counters must not be incremented blindly by Reading.
```

### Safe response behavior

When no Interaction snapshot has been applied:

```text
Return zero/default counters according to public response contract.
```

When an older snapshot is available:

```text
Return last-known counters.
```

Reading must not synchronously query Interaction for fresher values during ordinary public requests.

---

## 15. View Tracking Consistency

Reading does not own view acceptance or view persistence.

Runtime flow:

```text
Reading returns public article detail
    -> Client separately sends view contribution to Interaction
    -> Interaction applies eligibility and abuse policy
    -> Interaction durably updates ArticleViewCount when accepted
    -> Interaction eventually publishes a newer counter snapshot
    -> Reading eventually applies that snapshot
```

Rules:

```text
Article read success does not depend on view recording success.

Reading does not consume one raw event per view.

Client retry or Interaction suppression does not alter Reading visibility.

Counter freshness is eventually consistent.
```

---

## 16. Public Read Idempotency

Public GET endpoints are naturally idempotent:

```text
GET /api/v1/articles
GET /api/v1/articles/{articlePublicId}
GET /api/v1/articles/slug/{slug}
GET /api/v1/articles/search
GET /api/v1/articles/{articlePublicId}/related
```

Repeated reads:

```text
Must not mutate upstream truth.
May return newer local projection data if consumers have applied newer snapshots between requests.
```

This change in returned freshness is expected and correct.

View contribution is separate from the Reading GET response path.

---

## 17. Projection Lag and Safe Degradation

Projection lag is expected in V1.

### Core projection lag

| Missing / unsafe state | Result |
|---|---|
| `ArticleReadModel` missing | Safe 404 / omission |
| Article locally non-public | Safe 404 / omission |
| Article visibility marked unsafe | Safe 404 / omission |
| `ArticleSeoRouteProjection` missing for slug request | Safe 404 |
| Route inactive | Safe 404 |
| Route `RequiresResync = true` | Safe 404 |

### Optional enrichment lag

| Missing / stale enrichment | Result |
|---|---|
| Interaction counters missing | Zero/default counters |
| Interaction counters delayed | Last-known counters |
| Cover media missing | Null/omitted cover |
| Related content delayed | Empty/deterministic fallback |

Principle:

```text
Core visibility and route uncertainty fails closed.

Optional enrichment lag degrades safely.
```

---

## 18. Cache Consistency

Cache is acceleration only.

Cache must not become hidden source truth or hidden upstream fallback.

Rules:

```text
Cache stores responses or projection-derived data only according to cache policy.

Cache must not bypass local Reading visibility checks.

Cache must not cause a locally denied or unsafe article/route to be served.

Cache must not query upstream modules implicitly on miss.

Cached media/counters may lag according to optional-enrichment policy.
```

Conflict rule:

```text
Reading local deny/unsafe state wins over cached public response.
```

When local projection state itself has not yet consumed an upstream visibility/route change, the same bounded eventual-consistency limitation applies and must be monitored.

---

## 19. Consumer Transaction Boundary

Reading projection apply uses bounded local transactions.

A consumer apply transaction may update:

```text
Reading projection row
Source-specific version metadata
Source message metadata
Consumed-message / apply-decision state
Local diagnostics fields
```

It must not update:

```text
Content truth
SEO truth
Media truth
Interaction truth
Notification truth
Audit truth
```

Conceptual consumer transaction:

```text
Begin Reading local transaction
    -> Reserve MessageId for ConsumerName
    -> Compare source-specific version
    -> Apply newer snapshot or record stale decision
    -> Complete consumed-message apply metadata
Commit
```

If processing cannot complete safely:

```text
Rollback or mark failure according to consumer policy.
Retry / resync / repair as required.
```

Source producers do not wait for Reading projection completion as part of their own truth transaction.

---

## 20. Producer-Side vs Consumer-Side Failure

### Producer-side failure

Examples:

```text
Content/SEO/Media/Interaction truth committed but Outbox publish is delayed.
Outbox publisher cannot hand message to RabbitMQ yet.
```

These are upstream publication/delivery issues, not Reading apply failures.

### Reading consumer-side failure

Examples:

```text
Reading receives message but cannot update projection.
Reading database is temporarily unavailable.
Reading detects stale or unsafe source input.
Reading detects a version gap requiring resync.
```

These belong to Reading consumer retry, apply-decision and repair handling.

Metrics and logs must distinguish producer publication lag from Reading consumer apply lag.

---

## 21. Timeout and Ambiguity

Timeouts are ambiguous.

A timeout does not prove:

```text
A projection event was not published.
A Reading consumer apply did not commit.
An article does not exist in source truth.
A slug route does not exist in SEO truth.
An Interaction counter snapshot is absent at source.
```

Public request behavior remains local-projection based:

```text
No safe local public article/route projection
    -> safe deny / 404.

Optional media/counter enrichment missing
    -> degrade safely.
```

Consumer/rebuild behavior handles ambiguity through:

```text
Bounded retry
Message dedupe
Version-aware apply
Resync
Repair
Rebuild
Operator investigation
```

Reading must not introduce synchronous upstream calls in the public hot path to resolve timeout ambiguity.

---

## 22. Reconciliation and Rebuild

Reading projections must have recovery paths.

| Reading state | Approved recovery input |
|---|---|
| Article core and visibility | Content-owned projection/rebuild input |
| Slug route and SEO metadata | SEO-owned projection/rebuild input |
| Media enrichment | Media-owned presentation input |
| Interaction counters | Interaction-owned stats snapshot/reconciliation input |

Rules:

```text
Reading rebuild updates Reading-owned projections only.

Reading must not recalculate Interaction truth.

RabbitMQ is transport, not permanent replay storage.

Rebuild must preserve source-specific freshness semantics.

Full rebuild must not expose incomplete candidate state as fully current serving state.
```

### Rerun safety

Repair/rebuild rerun must not:

```text
Duplicate article projections
Reactivate stale routes
Double-count counters
Expose locally known non-public content
Overwrite newer projected state with older source input
Publish partial candidate output as complete
```

---

## 23. Safe Non-Progress Rule

When Reading cannot prove that applying or serving state is safe, it must prefer:

```text
Ignore stale input
Reject unsafe input
Fail closed
Retry
Resync
Repair
Rebuild
Require operator intervention
```

over:

```text
Applying uncertain projection changes
Serving locally known unsafe content
Inventing missing enrichment values beyond documented defaults
Synchronously coupling to source modules as an undocumented fallback
```

---

## 24. Observability Signals

Reading should expose or log metrics for:

```text
Projection apply count by source lane and event type
Projection apply failure count
Duplicate message count
Stale version ignore count
Version gap / resync-required count
Content projection lag
SEO route projection lag
Media enrichment lag
Interaction counter snapshot lag
Visibility deny / unsafe deny count
Route missing / inactive / unsafe deny count
Counter defaulted / last-known response count
Repair and rebuild execution count
Repair and rebuild failure count
Cache hit / miss / invalidation count
```

Apply-decision logs should include:

```text
ConsumerName
MessageId
EventType
SourceLane
AggregateId / ArticlePublicId
IncomingVersion
CurrentAppliedVersion
ApplyDecision
CorrelationId
OccurredAtUtc
ReceivedAtUtc
ProcessedAtUtc
FailureCode where applicable
```

---

## 25. Non-Goals

Reading V1 does not provide:

```text
Synchronous SEO slug resolution on the public read path
Synchronous Content visibility verification on ordinary public requests
Synchronous Interaction counter fallback
Raw view-event processing
Blind counter increment processing
Reading-owned engagement truth
Reading-owned moderation truth
Popularity/trending ranking without a separately designed projection contract
Instant global consistency after upstream source commits
```

---

## 26. Summary

Reading consistency in V1 rests on these rules:

1. Reading serves public requests from Reading-owned asynchronous projections only.
2. Content owns article visibility truth; Reading applies Content projection snapshots locally.
3. SEO owns canonical route truth; Reading serves slug requests from `ArticleSeoRouteProjection`.
4. Media enrichment is optional and may lag safely.
5. Interaction owns counter truth; Reading applies `interaction.article_counters_projection_published` by `StatsVersion`.
6. Every source lane uses an independent version marker.
7. `MessageId` protects duplicate processing; source-specific version protects stale overwrite.
8. Reading applies known-value snapshots, not blind counter deltas.
9. Visibility and route state fail closed once local state is missing, denied or unsafe.
10. Because propagation is asynchronous, a bounded lag may exist before Reading learns upstream visibility or route changes; that lag must be monitored and repaired.
11. Cache accelerates Reading-owned state only and never overrides local deny/unsafe state.
12. Repair and rebuild update Reading projections only; upstream modules retain truth ownership.
13. RabbitMQ is transport, not permanent replay history.
14. Safe non-progress is preferred over unsafe stale apply or hidden synchronous coupling.