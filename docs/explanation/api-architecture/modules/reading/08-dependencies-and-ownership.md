# Reading — Dependencies & Ownership (V1)

Related:

* `../../../../architecture/arc42/03-building-blocks-modularity.md`
* `../../../../architecture/arc42/13-transactions-and-consistency-v1.md`
* `../../../../architecture/arc42/18-stream-processing-and-derived-state-v1.md`
* `../../../../architecture/arc42/19-stream-processing-runtime-v1.md`
* `../../../../decisions/adr-0015-cache-policy-and-invalidation-redis-v1.md`
* `../../../../decisions/adr-0018-transaction-boundaries-and-consistency-model-v1.md`
* `../../../../decisions/adr-0021-clock-time-and-ordering-policy-v1.md`
* `../../../../decisions/adr-0022-versioning-and-fencing-strategy-v1.md`
* `../../../../decisions/adr-0025-batch-processing-and-derived-state-policy-v1.md`
* `../../../../decisions/adr-0026-batch-job-orchestration-and-materialization-policy-v1.md`
* `../../../../decisions/adr-0027-stream-processing-and-derived-state-policy-v1.md`
* `../../../../decisions/adr-0028-consumer-idempotency-replay-and-rebuild-policy-v1.md`

---

## 1) Module ownership

Reading owns:

* public read model ownership
* public query semantics
* response composition
* source-derived public visibility evaluation inside Reading projection
* safe degradation behavior
* cache usage policy for Reading public APIs
* Reading-owned projection freshness metadata
* Reading-owned rebuild/reconciliation posture
* Reading-owned read-facing derived outputs

Reading does not own:

* article lifecycle truth
* article editorial truth
* publication truth
* slug generation truth
* canonical routing truth
* media asset truth
* interaction raw event truth
* counter aggregation truth
* audit truth
* notification delivery truth

**Rule:** Reading owns the public serving projection. It does not own the source facts copied into that projection.

---

## 2) Source ownership

| Concern | Owner | Reading usage |
|---|---|---|
| Article lifecycle | Content | Projected status and visibility |
| Article title/body/summary | Content | Projected public article fields |
| Category/tag data | Content | Projected display/filter fields |
| Slug and canonical metadata | SEO | Projected slug and SEO metadata |
| Media assets and primary media | Media | Projected cover/media fields |
| Views, likes, comments | Interaction | Projected counters/summaries |
| Public read projection | Reading | Owned derived serving data |

Copied data does not transfer ownership.

If source data changes, the source owner emits events or provides rebuild/reconciliation input.

---

## 3) Normal dependency posture

Normal public read path:

```text
Public API
    ↓
Reading projection
    ↓
Response
```

Reading public APIs should normally consume Reading-owned projections, not synchronously compose from Content/SEO/Media/Interaction on every request.

Policy-controlled fallback may read source truth when correctness requires it, but fallback must be:

* explicit
* observable
* bounded
* not the normal hot path
* not hidden cross-module ownership

---

## 4) Approved dependency shapes

Approved dependency shapes in V1:

### 4.1 Async event consumption

Reading may consume source events from:

* Content
* SEO
* Media
* Interaction

Purpose:

* maintain `reading.ArticleReadModel`
* update projected slug/metadata
* update projected cover/media fields
* update projected counters or summaries

Rules:

* delivery is at-least-once
* handlers must be idempotent
* projection apply must be version-aware
* stale events must not overwrite newer projection state
* RabbitMQ is delivery infrastructure, not permanent replay history

### 4.2 Reading-owned projection reads

Reading public APIs may read:

* `reading.ArticleReadModel`
* Reading-owned search/materialized query data
* Reading-owned related/trending summaries
* Reading-owned cache entries where configured

Rules:

* projection visibility must fail closed when uncertain
* optional enrichments may degrade
* projection diagnostics must not leak publicly

### 4.3 Cache-aside acceleration

Reading may use Redis/cache for:

* public article detail cache
* public list cache
* related/search cache where safe
* projected enrichment cache

Rules:

* cache is acceleration only
* cache must not become hidden truth
* stale cache must not override Reading projection visibility
* cache refresh failure must not break a safe projection-backed response

### 4.4 Policy-controlled source fallback

Reading may use source fallback only where explicitly defined.

Possible fallback sources:

* Content truth
* SEO truth
* Media truth
* Interaction aggregate truth

Rules:

* fallback must be logged and measured
* fallback must not become normal hot path
* fallback must not mutate source truth
* fallback must not create hidden ownership

### 4.5 Batch / rebuild / reconciliation input

Reading may read bounded source inputs to:

* rebuild Reading projections
* reconcile projection drift
* repair missing derived rows
* regenerate counters/summaries/trending inputs
* validate candidate output before cutover

Rules:

* input boundary must be explicit
* output remains derived
* rebuild must be rerun-safe
* partial candidate output must not be exposed as complete

---

## 5) Forbidden dependencies

Reading must not:

* write into Content truth tables
* write into SEO truth tables
* write into Media truth tables
* write into Interaction truth tables
* mutate another module's truth as "repair"
* require audit completion to respond
* require notification completion to respond
* require interaction aggregation completion to respond
* require Reading projection update completion inside source module transaction
* treat cache/projection/summary presence as publication truth
* infer public visibility from slug route resolution alone
* publish partial rebuild output as active complete state
* rely on RabbitMQ as permanent replay history
* rely on naive singleton ownership for rebuild/repair workflows
* use timestamp last-write-wins as freshness authority
* let stale events overwrite newer projection state

---

## 6) Source module expectations

### 6.1 Content

Reading expects Content to own:

* article lifecycle truth
* editorial truth
* article status transitions
* article source version/revision
* lifecycle legality

Reading may consume Content events such as:

```text
content.article_published
content.article_updated
content.article_unpublished
content.article_archived
content.article_soft_deleted
```

Reading must not decide whether a Content lifecycle transition is legal.

Reading only projects the committed source result.

### 6.2 SEO

Reading expects SEO to own:

* slug generation
* canonical routing rules
* redirect rules
* metadata truth

Reading may project:

* slug
* canonical URL
* meta title
* meta description

Reading may resolve slug from its projected read model in the normal public path.

SEO fallback may be used only according to explicit policy.

### 6.3 Media

Reading expects Media to own:

* media lifecycle
* storage location
* media metadata
* primary media rules
* attachment/order truth

Reading may project:

* cover media id
* cover media URL
* cover alt text
* ordered media/gallery fields

Missing or delayed media projection should degrade safely.

### 6.4 Interaction

Reading expects Interaction to own:

* view signals
* like signals
* comment counters
* dedupe/abuse policy
* aggregation/recompute policy

Reading may project counters or summaries.

Reading must not blindly increment counters under replay unless raw events are deduped or the operation is otherwise replay-safe.

Preferred counter shape:

```text
Set counter to known aggregate value.
```

---

## 7) Reading-owned derived outputs

Reading may own derived outputs such as:

* `ArticleReadModel`
* read-facing search projection
* related article projection
* trending input projection
* summary fragments
* response-composition accelerators
* public cache entries
* rebuild candidate artifacts
* reconciliation reports

These outputs must remain:

* derived
* observable
* rebuildable
* version-aware where ordering matters
* subordinate to source ownership

Reading-owned derived outputs may improve:

* latency
* query simplicity
* ranking
* response completeness
* website experience

They must not become:

* Content truth
* SEO truth
* Media truth
* Interaction truth
* permanent event history

---

## 8) Projection ownership rules

Reading owns projection state and projection freshness markers.

Examples:

* `SourceVersion`
* `LastEventMessageId`
* `LastSourceOccurredAtUtc`
* `LastSyncedAtUtc`
* projection visibility state
* cache freshness metadata
* rebuild/reconciliation metadata

Rules:

* `SourceVersion` protects against stale overwrite
* `MessageId` supports duplicate detection and traceability
* timestamps support investigation and lag measurement
* timestamps are not freshness authority
* projection freshness metadata must not be exposed publicly

---

## 9) Event and broker dependency rules

Reading consumes events through the system async path:

```text
Source truth commit
    ↓
Outbox
    ↓
Worker publisher
    ↓
RabbitMQ
    ↓
Reading consumer
    ↓
Reading projection update
```

Rules:

* outbox is the source module's publication intent
* RabbitMQ is delivery infrastructure
* Reading consumer is responsible for idempotent apply
* Reading must not assume exactly-once delivery
* Reading must not assume global ordering
* Reading must not assume RabbitMQ is permanent replay history

Recovery must rely on:

* source truth
* retained operational history where policy allows
* outbox retention where policy allows
* bounded rebuild/reconciliation
* deterministic regeneration from authoritative inputs

---

## 10) Transaction boundary ownership

Source modules own their local truth transactions.

Reading does not participate in source truth transactions.

A source module transaction may commit:

* source truth change
* source lifecycle/history metadata
* outbox message

Reading projection update happens later.

Reading projection transaction may update:

* Reading projection fields
* `SourceVersion`
* `LastEventMessageId`
* `LastSourceOccurredAtUtc`
* `LastSyncedAtUtc`

Reading projection transaction must not update source module truth.

Source modules must not wait for Reading projection completion as part of their truth transaction.

---

## 11) Public visibility ownership

Content owns publication truth.

Reading owns source-derived public visibility state inside its projection.

Public response rule:

```text
Projection says public + visibility not uncertain
    ↓
Reading may serve publicly
```

Fail-closed rule:

```text
Projection missing or visibility uncertain
    ↓
Safe 404 or explicit fallback policy
```

Reading must not expose content when:

* source-derived status is not `Published`
* projection `IsPublic` is false
* article is archived
* article is soft-deleted
* slug is inactive
* visibility is uncertain

Safe `404` is preferred over incorrect public exposure.

---

## 12) Route ownership

SEO owns canonical slug/routing truth.

Reading may own projected slug lookup for public serving.

Normal path:

```text
GET /api/v1/articles/slug/{slug}
    ↓
Reading projection slug lookup
    ↓
Reading projection visibility check
    ↓
Response
```

Policy fallback path:

```text
Projection missing or canonical behavior needed
    ↓
SEO resolve according to explicit policy
    ↓
Reading visibility check / fallback policy
    ↓
Response or safe 404
```

Rules:

* route success is not serve authority
* projected slug match is not enough without public visibility
* SEO route resolve is not enough without public visibility
* redirect/canonical behavior must be defined explicitly

---

## 13) Cache ownership

Reading may own cache policy for Reading public APIs.

Reading cache may store:

* public article list responses
* public article detail responses
* related/search fragments
* projected enrichment fragments

Reading cache must not store:

* draft content
* unpublished content
* archived-only content
* soft-deleted content
* internal event/message ids
* projection diagnostics
* admin-only fields
* audit fields

Rules:

* cache hit must not bypass projection visibility
* stale cache must not override projection visibility
* cache write must be skipped if projection visibility is uncertain
* cache refresh failure must not break safe projection-backed response

---

## 14) Batch / rebuild / reconciliation ownership

Reading may run batch/rebuild/reconciliation workflows for Reading-owned derived outputs.

Allowed workflows:

* rebuild `ArticleReadModel`
* reconcile projection drift
* rebuild counters/summaries
* regenerate trending inputs
* repair missing projected enrichments
* validate active projection against source inputs

Rules:

* input boundary must be bounded
* source ownership remains unchanged
* output remains derived
* rerun must be safe
* candidate-before-cutover is preferred where correctness matters
* partial rebuild output must not be exposed as complete
* RabbitMQ must not be the only recovery source

---

## 15) Publication and cutover ownership

If Reading publishes a correctness-sensitive derived output, Reading owns:

* candidate generation
* candidate validation
* cutover/publication rule
* rollback/retry posture
* freshness metrics
* rerun/rebuild policy

Cutover safety rules:

* candidate output must be bounded and validated
* stale candidate must not replace fresher projection state
* partial candidate must not become active complete output
* active output must not contradict public visibility rules
* previous safe active output should remain active if candidate fails

---

## 16) Coordination / ownership-sensitive workflow rule

Reading normally prefers:

* idempotent execution
* version-aware apply
* bounded rerun
* safe cache behavior
* rebuild/reconciliation over exclusive control

If a future workflow truly requires exclusive ownership, it must define:

* ownership source of truth
* monotonic generation/fencing token
* resource-side stale-owner rejection
* behavior when ownership confidence is lost

Naive assumptions are forbidden:

* in-memory `isLeader`
* timeout-only transfer
* local self-belief of ownership
* "only one worker exists today"

If ownership is ambiguous, Reading must prefer safe non-progress over unsafe dual publication.

---

## 17) Dependency failure ownership

Reading must distinguish failure domains.

### Producer-side failure

Owned by source/outbox path.

Examples:

* outbox row not published yet
* RabbitMQ handoff failed
* producer worker is down
* source event not yet delivered

Impact:

* Reading projection may lag
* public API may miss newly published content
* rebuild/reconciliation may be needed later

### Consumer-side failure

Owned by Reading consumer path.

Examples:

* event delivered but Reading failed to apply
* repository unavailable
* stale version rejected
* duplicate message ignored
* projection update failed

Impact:

* Reading projection may lag
* queue backlog may grow
* retry/DLQ/rebuild policy applies

### Public API dependency failure

Owned by Reading public serving path.

Examples:

* projection store unavailable
* cache unavailable
* optional enrichment unavailable
* explicit fallback dependency unavailable

Impact:

* degraded response
* safe `404`
* `503` when required safe serving dependency is unavailable

---

## 18) What Reading may expect from others

Reading may expect:

* Content emits source events for lifecycle/editorial changes
* Content provides source truth for rebuild/reconciliation input
* SEO emits source events for slug/metadata changes
* SEO provides source truth for rebuild/reconciliation input
* Media emits source events for media/primary changes
* Media provides source truth for rebuild/reconciliation input
* Interaction owns view/counter dedupe and aggregation policy
* Outbox/RabbitMQ deliver at-least-once, not exactly-once
* platform observability exposes outbox/broker lag signals

---

## 19) What others may expect from Reading

Other modules may expect Reading to:

* serve public APIs from Reading-owned projections in the normal path
* fail closed when public visibility is uncertain
* consume events idempotently
* reject stale source versions
* not mutate source truth
* not block source transactions
* keep derived outputs rebuildable
* expose projection lag/freshness metrics
* degrade optional enrichments safely
* avoid leaking internal metadata publicly

---

## 20) What nobody may assume

No module may assume:

* Reading owns publication truth
* Reading owns slug truth
* Reading owns media truth
* Reading owns interaction counter truth
* route resolution alone is enough to serve content
* cache/projection presence proves public readability
* Reading projection is always fresh
* RabbitMQ is permanent event history
* exactly-once delivery is guaranteed
* timestamps define freshness correctness
* rebuild output may silently replace source truth
* partial candidate output is safe to expose
* a single worker means idempotency is optional

---

## 21) Evolution rules

Future Reading evolution may add:

* richer search projections
* related article projections
* personalized recommendations
* cache hierarchy
* rebuild orchestration
* projection checkpointing
* external search backend
* analytics/trending pipeline

Any evolution must preserve:

* Content owns publication truth
* SEO owns routing truth
* Media owns media truth
* Interaction owns engagement truth
* Reading-owned outputs remain derived unless explicitly reclassified by ADR
* public visibility remains fail-closed
* important derived outputs remain rebuildable
* idempotency and version-aware apply remain mandatory
