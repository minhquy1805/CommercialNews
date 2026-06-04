# SEO — Idempotency & Consistency (V1)

This document defines SEO-specific rules for idempotency, routing correctness, replication lag, stale-route protection, cache-safe fallback behavior, ordering-sensitive route convergence, and bounded rebuild/reconciliation workflows.

System-wide rules live in:
- `../../../../architecture/arc42/11-replication-v1.md`
- `../../../../architecture/arc42/13-transactions-and-consistency-v1.md`
- `../../../../architecture/arc42/14-distributed-systems-assumptions-v1.md`
- `../../../../architecture/arc42/15-consistency-ordering-and-consensus-v1.md`
- `../../../../architecture/arc42/16-batch-processing-and-derived-data-v1.md`
- `../../../../architecture/arc42/17-dataflow-and-batch-workflows-v1.md`
- `../../../../architecture/arc42/18-stream-processing-and-derived-state-v1.md`
- `../../../../architecture/arc42/19-stream-processing-runtime-v1.md`
- ADR-0014 (Public ID / Slug strategy)
- ADR-0015 (Redis cache policy)
- ADR-0018 (Transaction boundaries & consistency model)
- ADR-0019 (System model and fault assumptions)
- ADR-0020 (Timeout, retry, and failure detection policy)
- ADR-0021 (Clock, time, and ordering policy)
- ADR-0022 (Versioning and fencing strategy)
- ADR-0023 (Consistency, ordering, and consensus boundaries)
- ADR-0024 (Distributed coordination and singleton work policy)
- ADR-0025 (Batch processing and derived state policy)
- ADR-0026 (Batch job orchestration and materialization policy)
- ADR-0027 (Stream processing and derived state policy)
- ADR-0028 (Consumer idempotency, replay, and rebuild policy)

---

## 0) SEO role in the system

SEO is a **routing and discoverability sidecar**.

It owns:
- slug routing truth
- SEO metadata truth

It does **not** own:
- publication lifecycle truth
- public visibility truth
- final authorization to expose content publicly

Therefore:
- SEO may resolve a route
- but Reading must still validate Content truth before returning public content

**Rule:** SEO may accelerate routing, but it must not become the final authority for public visibility.

---

## 1) Truth vs derived (routing sidecar)

### 1.1 Truth (SEO store)
SEO owns the routing truth and SEO metadata:
- slug routing truth: `(Scope, Slug) -> ResourceType + ResourcePublicId` plus status/flags as defined by SEO policy
- optional SEO metadata: canonical, meta title, meta description, social preview fields

Truth invariants:
- `(Scope, Slug)` is unique for **active** routes
- routing truth is authoritative for resolving a slug to a public target identity
- only one active route is valid per slug scope at a time, according to V1 policy

### 1.2 Derived (fast path)
Derived inputs include:
- Redis cache for slug resolution
- cache-backed routing shortcuts
- future search/index pipelines (V2+)
- optional metadata projections
- batch-rebuilt route-serving datasets if introduced later

**Rule:** Redis is never the source of truth for routing.  
DB fallback is required.

### 1.3 Truth limits
Even SEO routing truth is not enough to expose content safely by itself.

SEO truth can answer:
- “what target does this slug map to?”

SEO truth cannot alone answer:
- “is this target publicly visible right now?”

That final question belongs to Content truth.

### 1.4 Consistency class for SEO
SEO intentionally uses multiple consistency classes:

#### Strong truth-backed consistency
Required for:
- active slug ownership
- uniqueness of `(Scope, Slug)` among active routes
- authoritative route target mapping
- SEO-admin truth changes inside the SEO store

#### Ordered / causality-sensitive consistency
Required for:
- route changes per target/resource
- slug switchovers
- stale-route rejection
- metadata convergence where overwrite order matters
- downstream cache/projection application when version-sensitive
- bounded rebuild/reconciliation publication when multiple runs could overlap

#### Eventual consistency
Accepted for:
- Redis cache refresh/invalidation
- metadata propagation to derived systems
- search/index pipelines
- non-authoritative enrichments
- rebuilt derived serving artifacts before cutover

---

## 2) Consistency expectations (V1)

### 2.1 Routing correctness must be strict
`/resolve` and equivalent routing behavior must be correct and must never create a path that leaks drafts or unpublished content.

Strong consistency is required for:
- SEO routing truth within the SEO store
- uniqueness of active slug mappings

However, public visibility is ultimately enforced by Content truth.

### 2.2 Prefix-consistency / cause-before-effect rule
SEO must not cause “effect before cause” anomalies.

Examples:
- a slug resolves to an article that is not yet publicly visible
- an old cached route still points to a target that has been unpublished
- a stale route survives after lifecycle truth has changed
- a rebuilt route-serving dataset publishes older route knowledge after newer truth exists

Therefore:
- resolving `slug -> ResourcePublicId` is only one part of public correctness
- Reading must still validate:
  - the article exists
  - the article is `Published`
  - the article is allowed to be returned on the public path

If that validation fails, return safe `404` or equivalent deny behavior.  
No public leak is allowed.

### 2.3 Metadata freshness may be eventual
SEO metadata (meta title/description/social) may lag behind Content changes under replication lag.

Policy:
- metadata staleness is acceptable within an observable lag budget
- metadata freshness is secondary to visibility correctness
- stale metadata must not cause public exposure of non-public content

### 2.4 Timeout ambiguity does not prove route absence
If cache lookup or truth lookup times out, SEO must not conclude:
- the slug definitely does not exist
- the route definitely does not map
- the target definitely disappeared

Instead:
- follow bounded fallback behavior
- distinguish truth-backed not-found from dependency ambiguity
- prefer safe failure over incorrect exposure
- avoid turning stale cache misses into misleading permanent absence

### 2.5 No global routing order is assumed
SEO does **not** assume:
- one total global order for all slug changes
- one global sequence across all routes in the system

**Rule:** route convergence and stale-write protection are scoped per route target / article / slug decision boundary, not globally across the system.

### 2.6 Stream-style propagation is eventual, not authoritative
When SEO reacts to Content lifecycle events or emits downstream update signals:
- the SEO async path is eventual
- duplicate delivery is normal
- replay is normal
- out-of-order delivery is possible unless explicit versioning is enforced

**Rule:** async propagation supports convergence; it does not redefine truth ownership.

---

## 3) Transaction boundary (V1)

### 3.1 Truth boundary
The SEO transaction boundary stops at the SEO-owned truth change.

Typical truth changes include:
- create/update/remove the active slug mapping for an article within a scope
- upsert SEO metadata owned by SEO
- activate/deactivate route records according to SEO policy
- maintain local revision/version markers used for routing convergence or cache invalidation

### 3.2 Atomic commit set
For SEO-admin writes or SEO-owned routing changes, SEO MUST commit atomically:
- the routing truth change
- any required active/inactive route state change
- any local SEO metadata change that belongs to the same SEO command
- the Outbox record when downstream invalidation/update signals are required
- the version/revision increment when ordering-sensitive state is changed

Typical examples:
- slug change: deactivate old active route + activate new route + version/revision change + Outbox
- metadata update: metadata truth update + version/revision change + Outbox
- route restore/deactivation: routing truth update + version/revision change + Outbox

### 3.3 Outside the transaction
The following MUST NOT be required inside the SEO truth transaction:
- Redis cache update/invalidation as a success condition
- broker publish
- search/index pipeline updates
- public read model refresh
- batch rebuild completion
- external HTTP/API calls
- Content truth changes

These are post-commit async effects or belong to other module boundaries.

### 3.4 Transaction duration rule
SEO transactions must be short:
- no waiting for cache propagation
- no waiting for downstream consumers
- no interactive multi-step transaction across requests
- no retry loops over external dependencies inside the transaction

### 3.5 Shared DB does not widen SEO ownership
Even in a shared DB deployment, SEO must not use the same truth transaction to directly mutate:
- Content lifecycle truth
- Reading read models
- Notification state
- Audit downstream state

SEO may write:
- SEO-owned routing truth
- SEO-owned metadata truth
- approved local replication artifacts such as Outbox

### 3.6 No heterogeneous distributed transaction
SEO does **not** attempt one atomic workflow across:
- SEO truth DB
- Redis
- RabbitMQ
- search/indexing systems
- Content truth
- other module-owned truth stores

Atomicity stops at:
- SEO truth mutation
- required local route-state changes
- local version/revision update
- Outbox intent

### 3.7 Concurrency expectations
SEO write flows must assume:
- concurrent admin slug edits
- retries after timeout
- duplicate event delivery to consumers
- delayed consumer replay
- stale admin form submissions
- overlapping rebuild/reconciliation runs if orchestration is retried

At minimum, the design must prevent:
- two active rows for the same `(Scope, Slug)`
- silent stale overwrite of a newer slug decision
- out-of-order route convergence corrupting the active mapping
- cache refresh from stale data overwriting fresher route knowledge
- a later rebuild publishing older route knowledge over fresher truth-backed state

### 3.8 Slug conflict rules
Slug ownership/conflict must be decided at the SEO truth boundary, not by application-side “check then insert” logic alone.

Required posture:
- uniqueness is enforced by the database
- conflicting writes fail deterministically
- retries of the same semantic command converge safely
- admin APIs return a clear conflict outcome when the slug is already taken

---

## 4) Versioning and stale-write protection

### 4.1 Version is authoritative, timestamp is not
SEO ordering/freshness must be driven by:
- per-target or per-route version/revision
- explicit uniqueness/invariant checks
- authoritative state in SEO truth

It must **not** be driven primarily by:
- `UpdatedAt`
- `OccurredAt`
- “largest timestamp wins”

Cross-node wall-clock timestamps are informational only.

### 4.2 Stale admin write protection
If admin workflows are load-then-save, SEO should support:
- optimistic concurrency
- rowversion/version checks
- explicit stale-update conflict responses

This prevents:
- an old admin form from silently reverting a newer slug decision
- an old metadata form from overwriting newer metadata truth

### 4.3 Resource-side protection
The SEO truth boundary must verify current freshness itself.

A caller saying:
- “this is still the latest slug decision”
- “I edited the latest metadata”
is not sufficient.

The SEO store or write boundary must check:
- expected version / revision
- uniqueness constraints
- current active mapping state

### 4.4 State legality and versioning are complementary
Version checks alone are not enough.

SEO must also enforce route-state legality, such as:
- whether an active route can be replaced
- whether an old route becomes inactive or remains as history/alias by policy
- whether a restore/deactivation action is legal in current SEO truth

**Rule:** versioning prevents stale overwrite; route policy prevents illegal state transitions.

### 4.5 Downstream stale apply is also a freshness problem
Freshness protection is not only for sync admin writes.

It also applies when:
- a delayed Content-derived event reaches SEO after a newer version is already applied
- a cache refresh signal arrives after fresher route knowledge already exists
- a rebuild workflow computes from an older bounded snapshot and attempts later cutover

**Rule:** downstream apply must be version-aware or truth-resynced before publication.

---

## 5) Events, lag, and fallbacks

### 5.1 Events consumed by SEO (V1)
SEO reacts to Content lifecycle events (via Outbox → Broker → Consumer), such as:
- `content.article_published`
- `content.article_unpublished`
- `content.article_archived`
- `content.article_soft_deleted`
- `content.article_restored`
- `content.article_updated`

### 5.2 Events emitted by SEO (V1)
SEO event emission is optional in V1.

If adopted, SEO may emit downstream signals for:
- cache invalidation/update
- metadata refresh
- route update notifications to derived consumers
- future projection/index synchronization

Possible SEO integration events:
- `seo.slug_route_changed`
- `seo.slug_route_deactivated`
- `seo.metadata_updated`

Where ordering matters, events must include:
- `MessageId`
- `EventType`
- `AggregateId` / `ResourcePublicId`
- `AggregateVersion`
- `OccurredAtUtc`
- `CorrelationId`

### 5.3 Lag budget (policy-level)
- routing cache staleness: expected to be small (seconds) under normal load; spikes are acceptable but must be observable
- metadata staleness: acceptable (seconds to minutes) depending on backlog and consumer health
- rebuild/reconciliation freshness: bounded by workflow policy and must be measurable when important derived outputs exist

### 5.4 Fallback behavior
On `/resolve` or equivalent routing flow:

1. cache-first lookup (Redis), if enabled  
2. on miss or suspicion of staleness, DB lookup from SEO truth  
3. return `ResourcePublicId` as a routing target only
4. caller (Reading) must still enforce `Published` visibility from Content truth

On public detail:
- if slug resolves but detail/projection is stale, Reading falls back to Content truth and re-checks visibility

### 5.5 Safe absence vs ambiguous absence
SEO must distinguish:
- **truth-backed absence**: no active route exists in SEO truth
- **ambiguous absence**: cache miss / timeout / dependency ambiguity without authoritative confirmation

Only truth-backed absence may confidently drive a public “not found” routing conclusion.

### 5.6 Outbox is the causal bridge for SEO-owned async effects
When SEO truth changes trigger cache invalidation or downstream updates, Outbox is the durable bridge between:
- SEO truth mutation
- downstream eventual propagation

This means:
- cache update is never more authoritative than SEO truth
- route update notifications must derive from committed SEO truth
- replay and reconciliation start from SEO truth + Outbox, not timing assumptions

### 5.7 Replay and rebuild are normal recovery paths
If cache, serving artifacts, or downstream consumers drift:
- bounded replay
- bounded rebuild
- reconciliation against SEO truth
are all valid recovery mechanisms.

They must remain:
- bounded
- observable
- rerun-safe
- subordinate to SEO truth and Content visibility truth

---

## 6) Idempotency (must-haves)

### 6.1 Admin APIs
- `PUT /api/v1/admin/seo/articles/{articlePublicId}` is naturally idempotent if it replaces the full SEO state for the requested scope
- slug-specific command endpoints, if introduced later, MUST be idempotent by the command’s business meaning, typically around `(Scope, ResourceType, ResourcePublicId)` plus expected version semantics where applicable

### 6.2 Consumer idempotency
SEO truth-affecting consumers and downstream SEO-event consumers must tolerate at-least-once delivery:
- reprocessing `content.article_published` must not create duplicate routes or metadata rows
- reprocessing `seo.slug_route_changed` must converge to the latest active mapping
- duplicate invalidation/update signals must not corrupt cache or derived route state

Recommended dedupe key:
- `MessageId`

Recommended storage:
- durable processed-message table or durable apply marker for SEO truth-affecting consumers
- Redis TTL may be used only for non-critical cache refresh/invalidation dedupe, not as the sole protection for SEO truth mutation

### 6.3 Duplicate delivery vs stale delivery
SEO must distinguish:
- **duplicate delivery**: same message again
- **stale delivery**: older route/metadata event arrives after newer SEO truth already exists

Protection required:
- dedupe by `MessageId`
- stale rejection or ignore based on route/aggregate version rules

### 6.4 Retry-safe design beats singleton assumptions
SEO correctness must not depend on:
- only one cache updater running
- only one route consumer being active
- only one worker “surely” holding current ownership
- only one rebuild worker implicitly assumed to be current

If future ownership-sensitive processing is introduced, it must use authoritative generation/fencing checks rather than naive singleton belief.

### 6.5 Rebuild/reconciliation rerun safety
Important SEO rebuild/reconciliation workflows must be safe to rerun on the same bounded input.

This means:
- rebuild candidate generation must be deterministic enough to compare/retry
- partial candidate output must not become active automatically
- repair/rebuild workflows must not overwrite fresher truth-backed route knowledge

### 6.6 Externalized cache/update side effects must still be harmless on retry
Even when the side effect is “only cache invalidation” or “only route refresh”:
- retry is normal
- duplicate refresh is normal
- stale refresh is dangerous

Therefore:
- repeated refresh/invalidation should be harmless
- stale refresh must lose to fresher truth/version state

### 6.7 Content event idempotency matrix

| Event | Message-level dedupe | Business-level guard | Version rule | Recovery |
|---|---|---|---|---|
| `content.article_published` | `MessageId` | `(Scope, ResourceType, ResourcePublicId)` and `(Scope, Slug)` | apply only if incoming `AggregateVersion` is newer | resync from Content truth |
| `content.article_unpublished` | `MessageId` | deactivate by `(Scope, ResourceType, ResourcePublicId)` | ignore stale `AggregateVersion` | resync Content visibility |
| `content.article_archived` | `MessageId` | deactivate by `(Scope, ResourceType, ResourcePublicId)` | ignore stale `AggregateVersion` | resync Content visibility |
| `content.article_soft_deleted` | `MessageId` | deactivate by `(Scope, ResourceType, ResourcePublicId)` | ignore stale `AggregateVersion` | resync Content visibility |
| `content.article_restored` | `MessageId` | reactivate only if Content truth says public | apply only if incoming `AggregateVersion` is newer | truth re-check before indexable |
| `content.article_updated` | `MessageId` | do not overwrite manual metadata | apply only if incoming `AggregateVersion` is newer | resync metadata defaults |

### 6.8 Manual override consistency

Automatic Content event sync must not overwrite manual SEO metadata unless explicitly allowed.

Rules:
- manual `MetaTitle` blocks automatic title-derived meta title overwrite
- manual `MetaDescription` blocks automatic summary-derived meta description overwrite
- manual social preview fields block automatic social-preview overwrites for those fields
- lifecycle-derived route/indexability updates may still apply
- manual override decisions must be persisted in SEO truth, not inferred from timestamps

---

## 7) Ordering & conflict posture

### 7.1 Ordered transitions
For a given resource/route target, the following transitions are ordered:
- publish / unpublish / archive / soft-delete / restore reactions
- slug updates
- SEO metadata changes where overwrite ordering matters
- rebuild/publication of derived route-serving outputs where overlapping runs are possible

Events SHOULD include:
- `AggregateId = ResourcePublicId`
- `AggregateVersion` (monotonic per target/aggregate)

### 7.2 Consumer rule
If version gaps or stale deliveries are detected:
- do not apply out-of-order updates blindly
- resync routing/meta from authoritative SEO truth (or Content truth where Content is the source event)
- prefer convergence from truth over inferred event order

### 7.3 Conflict posture
SEO avoids multi-writer conflict by design in V1:
- Content remains single-writer for publication state
- SEO routing truth enforces unique `(Scope, Slug)` for active routes
- if a conflict occurs (slug already taken), admin operation fails with a clear conflict response

### 7.4 No timestamp-based conflict resolution
SEO must not resolve route or metadata conflicts by:
- “newest `UpdatedAt` wins”
- “later event time wins”

Versioning and truth-boundary constraints are authoritative.

### 7.5 No global total order across SEO workflows
SEO ordering guarantees do not imply:
- one global order between all slug changes
- one total order between route updates and unrelated module events
- one cluster-wide sequence that all SEO actions must share

Ordering is scoped to the route/aggregate boundary that actually needs it.

### 7.6 Rebuild/cutover ordering must not outrun fresher truth
If a candidate derived route-serving dataset is built from bounded input:
- cutover must not happen as though that candidate were fresher than current SEO truth
- stale candidates must be rejected, rerun, or explicitly scoped by snapshot semantics
- safe non-progress is preferred over unsafe rollback of route knowledge

---

## 8) Publication coupling (visibility safety)

- indexability and routing behavior must reflect Content state by policy
- if SEO processing is delayed, Content/Reading must still enforce visibility correctness
- non-public lifecycle changes must never be undone by stale SEO routing:
  - even if slug still resolves
  - even if cache still contains a target
  - even if an old derived route-serving candidate exists
  - the public detail must remain hidden after Content truth says non-public

**Rule:** stale SEO may delay convenience, but must not break visibility safety.

### 8.1 Routing success is not visibility success
A successful route resolution means:
- SEO found a target

It does not mean:
- the target is public
- the target is safe to return
- the target has not since become non-public

Reading must still apply truth-backed visibility enforcement.

### 8.2 Derived serving artifacts remain subordinate
Any SEO-serving artifact, cache, or rebuilt dataset remains:
- derived
- laggable
- replaceable
- subordinate to truth

None of them may override Content truth on public visibility.

---

## 9) Redis cache rules (SEO hot path)

### 9.1 Example key posture
Illustrative key:
- `cn:seo:slug:{scope}:{slug}` → `{ resourceType, resourcePublicId, version, updatedAt }`

Where available:
- `version` is preferred over `updatedAt` for freshness comparison

### 9.2 Invalidation triggers
Typical invalidation/update triggers:
- `content.article_published`
- `content.article_unpublished`
- `content.article_archived`
- `content.article_soft_deleted`
- `content.article_restored`
- `content.article_updated`
- `seo.slug_route_changed`
- `seo.slug_route_deactivated`
- `seo.metadata_updated`

### 9.3 Cache rules
- event-driven invalidation/update is preferred
- TTL is a safety net, not the correctness mechanism
- cache hits must still participate in truth-safe public read flow
- stale cache must never bypass Content visibility truth

### 9.4 Cache drift signals
Monitor:
- DB fallback rate
- stale-route rejection indicators where measurable
- cache hit ratio by routing key group
- visibility-denied-after-route-resolve patterns

### 9.5 Cache update idempotency
Cache update logic must be safe under:
- duplicate signals
- replay
- stale deliveries
- overlapping rebuild and refresh activity

A stale cache write must not overwrite fresher routing truth-derived data.

---

## 10) Rebuild / reconciliation posture (SEO)

### 10.1 Rebuildable derived SEO outputs
SEO-derived serving artifacts such as:
- cache-backed routing views
- non-authoritative metadata projections
- future search/index-supporting route artifacts

should be treated as rebuildable by policy.

### 10.2 Candidate-before-cutover
If an SEO workflow produces a correctness-sensitive derived output:
- build candidate first
- validate candidate
- publish/cut over explicitly
- never treat partial candidate state as active output

### 10.3 Reconciliation over blind trust
If SEO truth and derived serving state diverge, prefer:
- truth-backed fallback
- bounded reconciliation
- safe repair/rebuild
over blind trust in stale derived output.

### 10.4 Recompute is acceptable
If a derived routing-serving output is cheap enough to rebuild from SEO truth, full rebuild may be preferred over fragile incremental repair logic.

### 10.5 Recovery outputs remain derived
Mismatch reports, repair candidates, rebuilt route maps, and rematerialized serving datasets remain derived outputs.

They may support operability and serving quality.
They do not become SEO truth automatically.

---

## 11) Coordination and ownership posture (SEO)

### 11.1 SEO does not require global singleton coordination by default
Ordinary SEO correctness must not depend on:
- one global SEO leader
- one process being “the resolver owner”
- startup order deciding who may update route caches
- timeout-only assumptions about which updater is current

SEO correctness should instead be achieved through:
- SEO truth-store authority
- uniqueness constraints
- version-aware route convergence
- idempotent invalidation/update consumers
- truth-backed fallback

### 11.2 If future SEO ownership transfer is introduced
If a future SEO workflow truly requires one current owner
(for example exclusive rebuild of a route partition or one-current cache maintenance owner),
that workflow must define:
- ownership source of truth
- monotonic generation/fencing token
- resource-side rejection of stale owner actions

Naive lock/leader patterns are not acceptable.

### 11.3 Safe non-progress beats unsafe double-apply
If ownership is ambiguous for a correctness-sensitive rebuild/publication workflow, SEO must prefer:
- delayed rebuild
- stale-owner rejection
- operator retry
- truth fallback

over unsafe dual publication or stale-owner overwrite.

---

## 12) Observability signals (SEO-specific)

Minimum signals:
- `/resolve` latency and error rate
- Redis routing cache hit rate
- Redis → DB fallback rate
- route conflict rate on admin writes
- stale-write / optimistic-concurrency reject rate (if implemented)
- stale-event reject count
- version-gap / resync count for ordered consumers
- “route resolved but content not publicly visible” rate for debugging
- metadata lag indicators where measurable
- rebuild/reconciliation mismatch count
- active derived output freshness age where applicable
- candidate publication/cutover failure count
- ownership-generation mismatch count if future ownership-sensitive workflows are introduced

Logs should include:
- `correlationId / traceId`
- scope + slug (when safe)
- resolved target public id
- route source (`cache` vs `DB`)
- fallback path taken
- visibility-safe handoff outcome when available
- version/revision markers where ordering matters
- rebuild/reconciliation workflow identifiers where relevant

---

## 13) Slug change semantics (ADR hook)

- V1 default: one active slug per scope and article
- V2 option: keep old slugs as aliases with redirect policy (`301`) and an explicit slug-history model

See ADR-0014 (Public ID strategy) for the system rule.

---

## 14) Summary

SEO correctness in V1 rests on thirteen rules:

1. SEO owns routing truth, but not publication visibility truth.  
2. Redis accelerates routing but never becomes routing authority.  
3. Route resolution must still be followed by Content truth visibility validation.  
4. Version/revision is authoritative for stale-write protection; timestamp is not.  
5. Duplicate delivery and stale delivery are separate problems and both must be handled.  
6. Stale SEO may delay freshness, but it must never create public exposure that Content truth would deny.  
7. No global ordering or distributed transaction is assumed for SEO workflows.  
8. Rebuild/reconciliation supports SEO serving quality, but does not redefine route truth.  
9. Candidate derived output must be validated before publication when output correctness matters.  
10. Rebuild/reconciliation workflows must be rerun-safe.  
11. Async propagation is eventual and at-least-once; replay is normal.  
12. Rebuild/cutover must not publish older route knowledge over fresher truth.  
13. Singleton/ownership semantics are not relied on unless explicitly protected by authoritative generation/fencing rules.
