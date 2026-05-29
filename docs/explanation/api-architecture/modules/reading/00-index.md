# Reading Module — API Architecture (V1 Async Projections)

## Purpose

Reading provides the public article reading experience for CommercialNews.

Reading is a **fully asynchronous public serving projection module**.

Reading owns public serving behavior and Reading-owned projection state for:

```text
Public article listing with paging/filter/sort
Public article detail by ArticlePublicId
Public article detail by slug
Basic public keyword search
Related article composition
Local public visibility enforcement
Local slug-route safety enforcement
Safe response composition
Optional enrichment degradation
Projection apply, repair and rebuild behavior
```

Reading does not own upstream source truth.

Source truth remains owned by:

| Concern | Source owner |
|---|---|
| Article lifecycle, editorial state and public visibility truth | Content |
| Canonical slug, public route and metadata truth | SEO |
| Media asset and public presentation truth | Media |
| Views, likes, comments, moderation and public counter truth | Interaction |

Core serving rule:

```text
Ordinary public requests are served from Reading-owned asynchronous projections only.
```

---

## 1. Reading-Owned Serving State

Reading V1 serves public requests from local projection state including:

```text
ArticleReadModel
ArticleSeoRouteProjection
Reading-owned query/search state where implemented
Reading-owned cache derived from safe projection state where configured
```

### `ArticleReadModel`

Used for:

```text
Article content
Local public visibility
Optional projected media presentation
Optional projected Interaction counters
Source-specific apply metadata
```

### `ArticleSeoRouteProjection`

Used for:

```text
Scope + Slug -> ArticlePublicId local serving lookup
Projected canonical URL and SEO metadata where available
Route activity and route-safety state
SEO source-version tracking
```

### Normal Public Path by Public ID

```text
Public API
    -> ArticleReadModel
    -> Local public visibility check
    -> Response or safe 404
```

### Normal Public Path by Slug

```text
Public API
    -> ArticleSeoRouteProjection by Scope + Slug
    -> ArticleReadModel by ArticlePublicId
    -> Local route safety and article visibility checks
    -> Response or safe 404
```

Reading does not synchronously call Content, SEO, Media or Interaction during ordinary public article response composition.

---

## 2. Why This Module Is Critical

Reading is critical because:

```text
It is the public website read path.

It must remain performant under public traffic and scraping pressure.

It must not intentionally expose locally known draft, unpublished,
archived, soft-deleted or unsafe article state.

It serves from projections that may lag behind source truth.

Slug routing is now served from local async route projection state.

Optional media and counter enrichments may be delayed or missing.

Consumers must be safe under duplicate, stale and replayed delivery.

Projection repair and rebuild must preserve public-serving safety.
```

The most important priority rule is:

```text
Correctness first.
Completeness second.
Freshness third.
```

---

## 3. Primary Consumers

Reading serves:

```text
Public website clients
Mobile clients if introduced
Anonymous users
Authenticated public users where responses remain public-safe
Edge/CDN/cache layers where configured
Public search and crawler traffic
Monitoring and synthetic public checks
```

Reading may support internal operational workflows for:

```text
Projection diagnostics
Projection reconciliation
ArticleReadModel repair/rebuild
ArticleSeoRouteProjection repair/rebuild
Media enrichment repair where adopted
Interaction counter enrichment repair
Search/related projection repair where implemented
Candidate validation and safe cutover for broad rebuilds
```

Reading V1 does not own popularity/trending computation workflows.

---

## 4. V1 Public Capabilities

Reading V1 exposes public query behavior for:

| Capability | Serving projection / state |
|---|---|
| Article list | `ArticleReadModel` |
| Article detail by public id | `ArticleReadModel` |
| Article detail by slug | `ArticleSeoRouteProjection` + `ArticleReadModel` |
| Basic public search | Reading-owned public projection/search state |
| Related articles | Reading-owned public projection/query state |
| Displayed counters | Interaction snapshot fields projected into Reading |
| Optional media presentation | Media-projected fields where adopted |

### Allowed V1 Sort Direction

Baseline list/search sorting:

```text
-publishedAt
publishedAt
```

Not included in V1:

```text
-popularity
popularity
trending ranking
```

Popularity/trending requires a later dedicated projection design.

---

## 5. Non-Goals in V1

Reading V1 does not own or provide:

```text
Article creation or editing
Publish/unpublish/archive/restore/delete commands
Content lifecycle legality
Canonical slug generation or route truth
Media upload/storage lifecycle
Interaction write endpoints
View/like/comment truth
Comment moderation/report workflow
Counter materialization truth
Popularity/trending calculation
Raw interaction analytics
Audit truth
Notification delivery
Admin preview
Draft preview
Personalized recommendations
User-specific reading history
Permanent event history
Synchronous upstream fallback in ordinary public reads
```

Reading V1 also does not guarantee:

```text
Immediate public appearance after Content publish
Immediate public slug availability after SEO route activation
Immediate disappearance before a newer non-public snapshot reaches Reading
Immediate route deactivation before a newer SEO snapshot reaches Reading
Strongly consistent counters
Exactly-once event delivery
Global ordering across independent source modules
```

---

## 6. Hard Constraints

Reading must follow these constraints:

```text
Only locally safe public article projection state may be intentionally served.

Missing, denied or unsafe local article visibility fails closed.

Missing, inactive or unsafe local slug-route state fails closed.

Safe 404 is preferred over disclosing hidden-resource existence.

Ordinary public paths read Reading-owned projection state only.

No synchronous Reading -> Content visibility lookup in the normal public path.

No synchronous Reading -> SEO route resolution in the normal slug path.

No synchronous Reading -> Media enrichment lookup in the normal public path.

No synchronous Reading -> Interaction counter lookup in the normal public path.

Interaction view contribution must not block Reading responses.

Optional media and counter enrichment may lag or degrade safely.

Cache is acceleration only and must not override local deny/unsafe state.

Internal projection diagnostics must not be exposed publicly.

Projection consumers must be idempotent.

Each source lane must use independent version-aware apply.

Timestamps must not be used as freshness authority.

Repair/rebuild must be bounded, observable and rerun-safe.

Incomplete candidate rebuild output must not be exposed as active complete state.
```

---

## 7. Truth vs Derived Posture

## 7.1. Source Truth

| Concern | Owner |
|---|---|
| Article lifecycle and editorial fields | Content |
| Article public visibility truth | Content |
| Category/tag truth | Content |
| Canonical slug and public route truth | SEO |
| Canonical SEO metadata truth | SEO |
| Media asset and primary/public presentation truth | Media |
| Accepted view counting | Interaction |
| Like and comment truth | Interaction |
| Moderation/report truth | Interaction |
| Public counter materialization truth | Interaction |

## 7.2. Reading-Owned Derived State

Reading may own:

```text
ArticleReadModel
ArticleSeoRouteProjection
Projected cover/media presentation fields
Projected displayed counter fields
Public search state where implemented
Related-article state where implemented
Public response cache derived from safe Reading state
Consumed-message/apply-decision state
Repair/rebuild candidate artifacts
Reconciliation reports
```

Rules:

```text
Reading-owned outputs are derived.

Derived state may lag, replay, repair and rebuild.

Derived state does not transfer ownership of source facts.

Reading must not mutate upstream truth during serving, repair or rebuild.
```

---

## 8. Async Projection Posture

Reading follows upstream source truth asynchronously.

Standard projection flow:

```text
Source module commits truth + OutboxMessage
    -> Outbox worker publishes message
    -> RabbitMQ delivers message
    -> Reading consumer receives message
    -> Reading dedupes MessageId
    -> Reading evaluates source-specific version
    -> Reading applies newer known-value projection snapshot locally
```

### V1 Projection Lanes

| Producer | Reading projection impact | V1 posture |
|---|---|---|
| Content | Article core state and public visibility in `ArticleReadModel` | Required |
| SEO | Route/metadata state in `ArticleSeoRouteProjection` | Required for slug-serving flow |
| Media | Optional public media presentation fields | Adopt when media serving projection is implemented |
| Interaction | Displayed counter snapshot fields | Required for counter-enriched responses once counters are exposed |

Confirmed Interaction event contract:

```text
interaction.article_counters_projection_published
```

Expected Interaction snapshot values:

```text
ArticlePublicId
ViewCount
LikeCount
VisibleCommentCount
StatsVersion
OccurredAtUtc
```

### Consumer Rules

```text
Delivery is at-least-once.

Duplicate delivery is expected.

Replay is expected.

Older messages may arrive after newer messages.

MessageId protects duplicate processing.

Source-specific version protects stale overwrite.

Known-value snapshot apply is preferred.

Timestamp order is not freshness authority.

RabbitMQ is transport, not permanent replay history.
```

---

## 9. Source-Specific Version Posture

Reading must not use one shared `SourceVersion` for independent upstream modules.

Required source-version lanes:

| Source lane | Reading freshness marker |
|---|---|
| Content article state | `ContentSourceVersion` |
| SEO route state | `SeoSourceVersion` |
| Media presentation state | `MediaSourceVersion` |
| Interaction counter snapshot | `InteractionStatsVersion` |

Approved apply rule per lane:

```text
If IncomingVersion > CurrentAppliedVersion:
    Apply newer known-value snapshot.
Else:
    Ignore as duplicate or stale.
```

Examples:

```text
Older Content state must not re-expose an article locally hidden by newer state.

Older SEO route state must not reactivate a locally deactivated route.

Older Media state must not overwrite newer cover presentation.

Older Interaction StatsVersion must not overwrite newer displayed counters.
```

---

## 10. Public Visibility and Route Safety Posture

Content owns public article visibility truth.

SEO owns canonical public route truth.

Reading owns local serving enforcement using projected state.

### Article Public Condition

```text
ArticleReadModel.Status = Published
AND ArticleReadModel.IsPublic = true
AND local visibility state is not unsafe / requires-resync
```

### Slug Route Condition

```text
ArticleSeoRouteProjection.Scope = public
AND ArticleSeoRouteProjection.Slug matches request
AND ArticleSeoRouteProjection.IsActive = true
AND ArticleSeoRouteProjection.RequiresResync = false
AND target ArticleReadModel satisfies article public condition
```

### Fail-Closed Local Behavior

```text
Missing/denied/unsafe article projection
    -> safe 404 or omission.

Missing/inactive/unsafe route projection
    -> safe 404.

Missing optional media/counter enrichment
    -> safe degraded response where core article serving state is valid.
```

---

## 11. Bounded Eventual-Consistency Posture

Reading is asynchronously synchronized with source owners.

Therefore:

```text
Newly published articles may temporarily be absent from Reading.

Newly activated slugs may temporarily return safe 404.

Articles newly unpublished/archived/deleted upstream may remain represented
by older local public state until newer Content projection input is applied.

Routes newly deactivated upstream may remain represented by older local active
route state until newer SEO projection input is applied.
```

V1 accepts this bounded propagation lag.

Required safeguards:

```text
Measure projection lag by source lane.

Give high operational attention to non-public Content transitions
and route deactivation transitions.

Reject stale input that would overwrite newer local deny state.

Repair/reconcile detected drift.

Fail closed immediately once local state is known denied or unsafe.
```

Reading must not represent async projection serving as instant global consistency.

---

## 12. Interaction Counter and View-Contribution Posture

Interaction owns:

```text
View eligibility and accepted counting
Like truth
Comment truth
Moderation/report workflows
ArticleInteractionStats
Counter snapshot publication
```

Reading owns only the local displayed counter projection:

```text
ViewCount
LikeCount
VisibleCommentCount
InteractionStatsVersion
```

Counter apply contract:

```text
interaction.article_counters_projection_published
    -> Reading applies only newer StatsVersion
    -> Reading sets known counter values
```

Reading must not:

```text
Consume raw per-view messages for public counters.
Blindly increment counters under delivery/replay.
Calculate popularity/trending from raw engagement behavior in V1.
Use counters as visibility authority.
Synchronously query Interaction for fresher public response counters.
```

View contribution flow:

```text
Reading returns public article detail
    -> Client separately sends view contribution to Interaction
    -> Interaction applies policy and updates accepted count where appropriate
    -> Reading later consumes a newer counter snapshot
```

Reading response success does not depend on view contribution success.

---

## 13. Optional Enrichment and Degradation Posture

Optional enrichment in Reading V1 may include:

```text
Media presentation fields
Optional SEO response metadata beyond route lookup requirements
Interaction displayed counters
Related article output
Optional search presentation fields
Cache acceleration
```

Safe degradation examples:

| Missing or delayed data | Safe response behavior |
|---|---|
| Cover/media | Null or omitted public fields |
| Optional SEO metadata | Null or omitted metadata fields |
| Counter snapshot missing | Documented zero/default counters |
| Counter snapshot delayed | Last-known counters |
| Related output unavailable | Empty or deterministic fallback |
| Cache unavailable | Serve from Reading projection when safe |

Not included in V1:

```text
Popularity/trending score
Popularity/trending sorting
```

Optional enrichment failure must never make visibility more permissive.

---

## 14. Batch / Repair / Rebuild Posture

Reading projections are derived and must be recoverable.

Reading may run bounded workflows for:

```text
Repair/rebuild of ArticleReadModel from Content-approved input
Repair/rebuild of ArticleSeoRouteProjection from SEO-approved input
Repair of projected media presentation from Media-approved input
Repair of displayed counters from Interaction-approved snapshot input
Search/related state repair where those states are implemented
Consumed-message/apply-decision investigation
```

Reading V1 does not own:

```text
Interaction counter truth rebuild
Raw interaction analytics rebuild
Popularity/trending generation
Moderation/report repair
```

Rules:

```text
Repair/rebuild updates Reading-owned state only.

Repair/rebuild must remain rerun-safe.

RabbitMQ is not permanent recovery storage.

Important broad rebuilds should use candidate validation before cutover.

Partial or unsafe candidate output must not become active public-serving state.
```

---

## 15. Cache Posture

Cache is acceleration only.

Reading cache may improve latency for safely public projection-backed responses.

Rules:

```text
Cache must contain public-safe response data only.

Cache hit must not bypass local article visibility checks.

Cache hit must not bypass local route-safety checks.

Locally known deny or unsafe state wins over cached public output.

Cache misses must not trigger hidden synchronous source calls.

Cache refresh failures must not break safe projection-backed responses.

Cache values must not expose internal projection diagnostics.
```

---

## 16. Primary Correctness Posture

Reading correctness is based on local safely applied serving projection state.

The following are not sufficient to expose public content:

```text
Slug match alone
Route projection alone
Cache hit alone
Search match alone
Related candidate alone
Media presence alone
Counter presence alone
Interaction activity alone
```

A public article response requires local article visibility checks to pass.

For slug requests, local route safety checks must also pass.

---

## 17. Failure and Recovery Posture

Reading must handle:

```text
Projection propagation lag
Duplicate delivery
Stale message arrival
Consumer retry and restart
Broker backlog
Cache miss or stale cache
Optional enrichment delay
Projection-store failure
Repair/rebuild failure
Timeout ambiguity
```

Failure rules:

```text
Timeout does not prove source absence.

Cache miss does not prove source absence.

Route presence does not prove article visibility.

Counter presence does not prove article visibility.

Missing/unsafe local core serving state returns safe denial.

Required Reading serving-store outage may return 503.

Optional enrichment failure degrades safely.

Public reads do not wait for async catch-up.

Public reads do not synchronously call source modules as hidden fallback.
```

---

## 18. Documentation Map

| Document | Purpose |
|---|---|
| [01-api-surface.md](01-api-surface.md) | Public endpoint and response contract |
| [02-domain-contracts.md](02-domain-contracts.md) | Reading projections, source lanes and ownership contracts |
| [03-runtime-flows.md](03-runtime-flows.md) | Public serving and async projection runtime flows |
| [04-errors-status-codes.md](04-errors-status-codes.md) | Public-safe errors and degraded-success behavior |
| [05-security-abuse-controls.md](05-security-abuse-controls.md) | Exposure, scraping, cache and logging controls |
| [06-idempotency-consistency.md](06-idempotency-consistency.md) | Delivery, replay, versions and consistency posture |
| [07-observability-slos.md](07-observability-slos.md) | Metrics, source-lane lag signals and rollout gates |
| [08-dependencies-and-ownership.md](08-dependencies-and-ownership.md) | Module boundaries and permitted dependencies |
| [09-open-questions.md](09-open-questions.md) | Deferred ADR hooks and operational design decisions |
| [10-business-rules.md](10-business-rules.md) | Reading V1 business rules |

---

## 19. Key Links

### System-Wide

```text
../../01-api-architecture-charter-v1.md
../../02-contracts-and-standards.md
../../09-observability-and-slos.md
```

### Arc42

```text
../../../architecture/arc42/03-building-blocks-modularity.md
../../../architecture/arc42/04-runtime-view-v1.md
../../../architecture/arc42/05-quality-requirements.md
../../../architecture/arc42/13-transactions-and-consistency-v1.md
../../../architecture/arc42/16-batch-processing-and-derived-data-v1.md
../../../architecture/arc42/17-dataflow-and-batch-workflows-v1.md
../../../architecture/arc42/18-stream-processing-and-derived-state-v1.md
../../../architecture/arc42/19-stream-processing-runtime-v1.md
```

### Relevant ADRs

```text
ADR-0013 Outbox & Delivery Semantics
ADR-0015 Cache Policy & Invalidation
ADR-0018 Transaction Boundaries & Consistency
ADR-0020 Timeout, Retry, and Failure Detection
ADR-0021 Clock, Time, and Ordering
ADR-0022 Versioning and Fencing
ADR-0025 Batch Processing and Derived State
ADR-0026 Batch Job Orchestration and Materialization
ADR-0027 Stream Processing and Derived State
ADR-0028 Consumer Idempotency, Replay, and Rebuild
```

### Upstream Modules

```text
Content
SEO
Media
Interaction
```

---

## 20. Closed V1 Decisions

The following decisions are already closed for Reading V1:

| Topic | V1 decision |
|---|---|
| Public serving state | Reading-owned async projections |
| Detail by public id | `ArticleReadModel` |
| Detail by slug | `ArticleSeoRouteProjection` + `ArticleReadModel` |
| Synchronous SEO route resolution | Not used in ordinary public serving |
| Synchronous source fallback | Not used in ordinary public serving |
| Article visibility owner | Content; projected into Reading |
| Canonical route owner | SEO; projected into Reading |
| Counter owner | Interaction |
| Counter inbound contract | `interaction.article_counters_projection_published` |
| Counter apply style | Known-value snapshot apply by `StatsVersion` |
| Counter visibility effect | None; counters never make an article public |
| Source freshness model | Independent version per source lane |
| Popularity/trending | Deferred beyond V1 |
| Core local missing/unsafe state | Fail closed |
| Optional enrichment lag | Degrade safely |
| Event delivery | At-least-once with idempotent/version-aware apply |
| Async propagation lag | Bounded lag accepted and observable |

---

## 21. Remaining ADR Hooks

Important deferred decisions remain tracked in:

```text
09-open-questions.md
```

Remaining high-value hooks include:

```text
Canonical redirect and historical slug behavior
Basic search implementation strategy
Read cache TTL and invalidation policy
Counter response/freshness exposure policy
Exact Content / SEO / Media projection message schemas
Reading consumed-message persistence and retention
Version gap and resync behavior
Repair/rebuild/cutover strategy
Related article implementation strategy
Projection freshness SLO thresholds
Media projection initial scope
Public preview boundary
Personalization boundary
External search/recommendation integration
Future popularity/trending projection design
```

---

## 22. Final V1 Posture

```text
Reading is a fully asynchronous public serving projection module.

Content provides article content and visibility projection input.

SEO provides local slug-route and metadata projection input.

Media may provide optional public presentation projection input
according to adopted V1 media scope.

Interaction provides versioned displayed counter snapshots through:
    interaction.article_counters_projection_published

Reading serves ordinary public requests only from:
    ArticleReadModel
    ArticleSeoRouteProjection
    Reading-owned safe query/cache state where configured

Reading does not synchronously call upstream source modules
during ordinary public article serving.

Each upstream lane uses its own version marker.

Missing or unsafe local core state fails closed.

Optional media/counter lag degrades safely.

Bounded async propagation lag is accepted and must be observable.

Popularity/trending is deliberately deferred beyond V1.
```