# Content — Idempotency & Consistency (V1)

This document defines Content-specific idempotency, consistency, ordering, stale-write protection, visibility-correctness rules, and how Content truth supports replay, rebuild, and reconciliation of downstream derived state.

System-wide rules live in:

- `../../../../architecture/arc42/11-replication-v1.md`
- `../../../../architecture/arc42/13-transactions-and-consistency-v1.md`
- `../../../../architecture/arc42/14-distributed-systems-assumptions-v1.md`
- `../../../../architecture/arc42/15-consistency-ordering-and-consensus-v1.md`
- `../../../../architecture/arc42/16-batch-processing-and-derived-data-v1.md`
- `../../../../architecture/arc42/17-dataflow-and-batch-workflows-v1.md`
- `../../../../architecture/arc42/18-stream-processing-and-derived-state-v1.md`
- `../../../../architecture/arc42/19-stream-processing-runtime-v1.md`
- ADR-0013 (Outbox & delivery semantics)
- ADR-0014 (Public ID / slug strategy)
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

## 0) Truth vs derived

### 0.1 Truth (Content store)

Content owns the source of truth for:

- article lifecycle state
  - `Draft`
  - `Published`
  - `Unpublished`
  - `Archived`
- publication and lifecycle timestamps
- unpublish reason and other lifecycle metadata where required
- revision history *(append-only by intent)*
- visibility invariants *(what is public vs not)*
- per-article monotonic `Version` used for ordered lifecycle and stale-write protection

**Rule:** public visibility correctness MUST be enforceable purely from Content truth.

### 0.2 Derived (allowed to lag)

Derived data may include:

- SEO metadata / indexability projections
- notification delivery workflows and logs
- interaction aggregates such as views/likes/comments counts
- caches *(response caches, feed caches, route caches)*
- read-model / projection materializations
- search / serving artifacts derived from Content truth
- batch-generated summaries, reports, reconciliation outputs, and rebuild candidates

Derived stores may lag. They must be:

- observable
- rebuildable
- safe under duplicate delivery
- safe under stale event arrival
- protected by fallback behavior

**Rule:** correctness must not depend on derived freshness unless explicitly stated.

### 0.3 Content truth vs downstream convenience

Content truth can answer:

- is the article public right now?
- what is the current lifecycle state?
- what is the authoritative version?
- what reason/timestamp belongs to the transition?

Derived systems may answer:

- how the article is presented
- how quickly it is found
- what counters/enrichments are shown
- whether summaries/reports are available

Derived convenience may lag.  
Content truth may not.

### 0.4 Consistency class for Content

Content intentionally uses multiple consistency classes.

#### Strong truth-backed consistency

Required for:

- lifecycle truth
- publication visibility correctness
- revision-aware stale-write rejection
- version advancement
- authoritative public/non-public decision

#### Ordered / causality-sensitive consistency

Required for:

- lifecycle transitions per article
- downstream consumers that apply article-derived state
- stale-event rejection by `(ArticleId, Version)`
- effect ordering where publish truth must exist before downstream meaning
- bounded rebuild/publication workflows where a derived output must not overtake newer Content truth

#### Eventual consistency

Accepted for:

- notifications
- audit persistence
- SEO/reactive enrichments
- caches and projections
- non-authoritative read-side materializations
- replay/reconciliation convergence for downstream derived state

---

## 1) Idempotency for actions

Lifecycle and high-impact action endpoints should be idempotent where feasible.

Examples:

- calling `:publish` on an already published article returns stable success/no-op semantics if that is the chosen V1 rule
- calling `:unpublish` on an already non-public article returns stable success/no-op semantics if that is the chosen V1 rule
- calling `:archive` on an already archived article returns stable success/no-op semantics if that is the chosen V1 rule
- calling `:restore` on an article already restored or not currently archivable/restorable returns stable success/no-op or documented conflict according to policy

If strict conflict semantics are preferred, they must be documented consistently  
(for example `409 CONTENT.INVALID_STATE_TRANSITION`).

**Non-negotiable rule:** idempotent semantics must not emit duplicate harmful downstream side effects.

This means:

- repeated equivalent commands must not create duplicate `Content.ArticlePublished`, `Content.ArticleUnpublished`, `Content.ArticleArchived`, or `Content.ArticleRestored` effects
- no duplicate emails, duplicate audit evidence, or duplicate projection work should be caused by a logically unchanged action result
- if the truth state is unchanged, downstream side effects must either not be re-emitted or must be safely deduplicated downstream

### 1.1 Idempotency is preferred over singleton assumptions

Content correctness must not depend on:

- “only one caller will try this action”
- “only one worker will process this lifecycle effect”
- startup order or local leader belief
- “only one rebuild job will touch downstream state”

Instead, Content prefers:

- idempotent commands
- authoritative truth checks
- optimistic concurrency
- duplicate-effect prevention through outbox + consumer dedupe
- bounded replay/reconciliation over exclusive control

### 1.2 Idempotency for ambiguous client retries

Client retries after timeout or lost responses must be treated as normal.

For high-impact commands such as:

- `:publish`
- `:unpublish`
- `:archive`
- `:restore`

the system should support an idempotent interpretation based on:

- current truth state
- expected version / concurrency token where applicable
- optional `Idempotency-Key` if adopted by API policy

**Rule:** retry safety comes from truth inspection and idempotent command semantics, not from assuming the first request definitely failed.

---

## 2) Consistency expectations

### 2.1 Strong consistency for lifecycle transitions (truth)

On success:

- lifecycle state transitions are strongly consistent at the Content truth boundary
- required lifecycle metadata is committed atomically with the state change
- subsequent admin reads must reflect the new truth immediately *(read-your-writes via primary read or write-return)*

### 2.2 Eventual consistency for side effects (derived)

The following are eventual:

- audit persistence
- SEO reactions and related projections
- notifications
- interaction aggregates
- cache invalidation / cache warm-up
- read-model refreshes
- derived summaries/reports built from Content truth
- search/serving artifact updates

Core Content workflows must not block on those subsystems.

### 2.3 Timeout interpretation

A timeout observed by a caller does **not** prove:

- the Content truth change failed
- the Outbox record was not committed
- no downstream effect will happen later

**Rule:** ambiguous outcomes must be reconciled from Content truth, not inferred from timeout alone.

### 2.4 No global ordering assumption

Content does **not** assume:

- one total global order across all content events in the system
- cross-module total ordering as a correctness requirement

**Rule:** Content ordering is scoped per article / aggregate unless an explicit ADR widens that scope.

### 2.5 Cause-before-effect rule

For Content-derived behavior:

- publish truth must exist before downstream meaning becomes externally relevant
- unpublish truth must win over stale downstream artifacts
- derived systems must not make an article appear public before Content truth says it is public
- delayed rebuild/publication of derived outputs must not overtake newer Content truth

This is a consistency rule, not merely an operational preference.

---

## 3) Transaction boundary (V1)

### 3.1 Truth boundary

The Content transaction boundary stops at the Content-owned truth change.

Typical truth changes include:

- draft creation
- article update
- publish / unpublish
- archive / restore
- revision/history append
- publication reason / timestamp updates
- version increment for ordered state changes

### 3.2 Atomic commit set

For lifecycle-changing commands, Content MUST commit atomically:

- the article truth change
- required lifecycle metadata
- revision/history row when policy requires it
- the new per-article `Version`
- the Outbox record for downstream side effects

This commit happens in one local DB transaction.

### 3.3 Outside the transaction

The following MUST NOT be required inside the Content truth transaction:

- broker publish
- notification sending
- audit persistence in downstream stores
- SEO cache/index/projection updates
- interaction aggregation updates
- Redis invalidation as a success condition
- external HTTP/API calls
- downstream rebuild/reconciliation completion

These are post-commit async effects.

### 3.4 Transaction duration rule

Content transactions must be short:

- no human interaction while transaction is open
- no waiting for downstream systems
- no long-running cross-module workflow inside the same transaction
- no retry loops over external dependencies inside the transaction

### 3.5 Shared DB does not widen Content ownership

Even in a shared DB deployment, Content must not expand its transaction scope into other modules’ truth tables just because they are physically reachable.

Content may write:

- Content-owned truth tables
- Content-owned revision/history tables
- approved local replication artifacts such as Outbox

It must not use the same transaction to directly perform Notification, Audit, SEO, Reading, or Interaction business writes.

### 3.6 No heterogeneous distributed transaction

Content does **not** attempt one atomic workflow across:

- Content truth DB
- RabbitMQ
- Redis
- email providers
- derived SEO/projection stores
- other module-owned truth stores

**Rule:** atomicity stops at Content truth + required local metadata + Outbox.

---

## 4) Concurrency and stale-write protection

### 4.1 Concurrency assumption

Content write flows must assume concurrent admin actions are possible.

Typical risks:

- two admins edit the same draft/article
- a stale publish/unpublish command arrives after a newer state transition
- an old update form overwrites newer content
- delayed async consumers try to apply older article state after newer truth already exists
- a replay/reconciliation workflow reasons from older versioned state after newer truth exists

### 4.2 Required stale-write protection

At minimum, Content should support:

- stale edit detection for load-then-save workflows
- duplicate publish/unpublish prevention
- explicit rejection or safe no-op behavior for stale lifecycle commands
- DB-enforced uniqueness for identifier invariants where applicable

Typical implementation options include:

- version-based optimistic concurrency
- rowversion / concurrency token
- compare-and-set update semantics
- explicit conflict responses for stale updates

### 4.3 Version is authoritative, timestamp is not

Content ordering/freshness must be driven by:

- per-article `Version`
- explicit lifecycle legality
- optimistic concurrency checks

It must **not** be driven primarily by:

- `UpdatedAt`
- `OccurredAt`
- “largest timestamp wins”

**Rule:** cross-node or cross-process wall-clock time is not authority for Content causality.

### 4.4 Resource-side protection beats caller belief

A caller saying “I edited the latest version” is not sufficient.

The Content truth boundary must verify freshness using authoritative state  
(for example expected version / rowversion / compare-and-set semantics).

### 4.5 State machine legality and versioning are complementary

Version checks alone are not enough.

Content must also enforce lifecycle legality, such as:

- what may transition from `Draft`
- what may transition from `Published`
- whether `Archived` content may be restored
- whether a repeated command is a safe no-op or an invalid transition

**Rule:** versioning prevents stale overwrite; lifecycle rules prevent illegal transition.

### 4.6 Downstream stale apply is also a concurrency problem

Concurrency risk is not limited to sync admin writes.

It also exists when:

- a delayed consumer applies an older event after a newer one
- a rebuild job computes from an older snapshot while newer truth already exists
- a replay job republishes old derived assumptions into an active store

**Rule:** `(ArticleId, Version)` freshness rules apply to downstream derived-state maintenance as well as to sync truth mutations.

---

## 5) Replication mechanics (Outbox + events)

### 5.1 Outbox is required for Content transitions

For lifecycle transitions and other changes that trigger side effects, Content MUST:

- write the truth change
- write an Outbox event
- commit both atomically in the same DB transaction

### 5.2 Events emitted by Content (V1)

Typical events:

- `Content.ArticleCreated`
- `Content.ArticleUpdated` *(optional; use with care to avoid noisy streams)*
- `Content.ArticlePublished`
- `Content.ArticleUnpublished`
- `Content.ArticleArchived`
- `Content.ArticleRestored`
- `Content.ArticleDeleted` or `Content.ArticleSoftDeleted` *(only if such lifecycle exists)*
- `Content.CategoryCreated` / `Content.CategoryUpdated` / `Content.CategoryDeleted` *(optional)*
- `Content.TagCreated` / `Content.TagUpdated` / `Content.TagDeleted` *(optional)*

### 5.3 Envelope requirements

Every emitted event must include at least:

- `MessageId`
- `ArticleId` *(or relevant aggregate id)*
- `Version`
- `OccurredAt`
- `CorrelationId`

No global ordering is assumed.  
Ordering is per article.

### 5.4 Outbox is the causal boundary

For Content, Outbox is not only a delivery mechanism.  
It is the durable boundary that preserves:

- truth mutation first
- async effect second

This means:

- a downstream effect must not be considered more authoritative than the truth change it derives from
- replay and reconciliation must start from Content truth + Outbox state, not from timing assumptions

### 5.5 Delivery is at-least-once by design

Content-derived async delivery assumes:

- broker publish may retry
- consumer delivery may duplicate
- handlers may restart
- replay may occur during recovery

Therefore:

- duplicate message handling is normal
- downstream consumers must be idempotent
- stale versions must be rejectable
- rebuild/reconciliation remains part of the design, not an afterthought

---

## 6) Ordering model (per-article Version)

Content lifecycle is ordered per article. Therefore:

- Content maintains `Version` as a monotonic counter per `ArticleId`
- each emitted event includes the new `Version`

### 6.1 Consumer expectations

Consumers SHOULD enforce ordering using `(ArticleId, Version)`.

Depending on consumer type:

- apply only if `IncomingVersion > LastAppliedVersion` for stale-update rejection
- or apply only if `IncomingVersion == ExpectedNextVersion` when strict sequencing is required

### 6.2 Gap handling

If a version gap or out-of-order situation is detected:

- consumers must not blindly apply older or partial assumptions
- consumers should resync current state from Content truth
- reconciliation/rebuild is preferred over silent stale overwrite

### 6.3 Duplicate vs stale delivery

Consumers must distinguish:

- **duplicate delivery**: same message again
- **stale delivery**: older version arrives after newer state is already applied

Protection required:

- dedupe by `MessageId`
- stale rejection by `(ArticleId, Version)` freshness rules

### 6.4 No cross-article ordering guarantee

Content ordering guarantees apply per article.  
They do **not** imply any guaranteed total order between:

- two different articles
- article events and unrelated module events
- publish actions across different aggregates

### 6.5 Rebuild/publication ordering must not outrun truth

If a bounded rebuild or reconciliation workflow generates a candidate derived output:

- it must not publish an older article version over a newer already-known truth state
- cutover must be version-aware or bounded to the intended truth snapshot
- if uncertainty exists, prefer re-read/resync over unsafe publication

---

## 7) Retry safety (sync + async)

### 7.1 Client retries

Clients may retry on timeouts or ambiguous outcomes.

Content actions must not create duplicate effects under retry.

Recommended:

- support `Idempotency-Key` for high-impact actions such as:
  - `:publish`
  - `:unpublish`
  - `:archive`
  - `:restore`

- treat repeated equivalent requests as the same logical outcome where feasible

### 7.2 Timeout ambiguity

A client-side timeout does not prove the article transition did not commit.

Therefore the safe recovery path is:

- inspect Content truth
- inspect current article lifecycle/version
- then decide whether the retry is a safe no-op, a conflict, or a fresh command

### 7.3 Consumer idempotency

Async consumers must be idempotent because delivery is at-least-once:

- dedupe by `MessageId`
- reject stale apply by `(ArticleId, Version)` where ordering matters

### 7.4 Publishing resilience

Broker publish failures must not fail the Content truth transition.

If Outbox publish is delayed:

- article truth remains authoritative
- outbox backlog/age must be observable
- recovery happens through retry/backoff, not by rolling back the Content truth change

### 7.5 Retry-safe design beats exclusive execution assumptions

Content retry safety must not rely on:

- only one worker running a handler
- only one publisher loop being “certainly current”
- one caller believing it still owns the operation
- one replay/rebuild workflow being assumed exclusive without explicit protection

If future ownership-sensitive processing is introduced, it must use authoritative generation/fencing checks rather than caller self-confidence.

### 7.6 Replay is a normal recovery path

Content-supported recovery workflows may:

- replay missed Content-derived effects
- recompute downstream candidate outputs
- rerun reconciliation on the same bounded input

**Rule:** replay/rerun must be harmless, deduplicated, or explicitly versioned at publication/cutover.

---

## 8) Visibility & truth-safe serving guardrails (non-negotiable)

### 8.1 Unpublish must take effect immediately

Even if SEO, notifications, route caches, or read projections lag:

- the public read path MUST filter out non-public articles from Content truth or a truth-backed visibility check
- derived stores must never re-expose unpublished content

### 8.2 Cause → effect ordering

Effects must not appear before causes:

- notifications about a new article must not be sent before the article is `Published`
- SEO routing/indexability must not lead to a public page that is not visible per Content truth
- derived caches/projections must not present public visibility ahead of Content truth
- rebuilt downstream artifacts must not publish older visibility assumptions over newer Content truth

### 8.3 Safe fallback over stale confidence

If a derived store is stale or uncertain:

- fall back to Content truth or a truth-backed visibility check
- or return safe “not found / updating” behavior as policy allows
- prefer safe `404` over incorrect exposure

### 8.4 Routing success does not equal visibility success

Even if a slug or route resolves through a cache or derived path, the final public visibility decision must remain consistent with Content truth.

### 8.5 Derived freshness is an optimization, not a correctness boundary

Fast route resolution, search/index visibility, or cache freshness are valuable.  
They do not become authority for:

- “is this article public?”
- “should this article be hidden now?”
- “which version is current?”

Those questions remain truth-owned by Content.

---

## 9) Edit history integrity (append-only intent)

- revision entries are append-only by intent
- updates create new revisions
- previous revisions are immutable by policy

Retention:

- retention/purge policy is defined at policy level
- any purge must preserve auditability and governance requirements

Revision history must not be rewritten to simulate “latest truth.”  
Current truth and historical revision trail are separate concerns.

### 9.1 History and truth serve different purposes

Current Content truth answers:

- current lifecycle
- current visibility
- current version
- current content state

Revision history answers:

- how the article changed over time
- what previous editorial states existed
- what can be used for investigation or historical reconstruction

Neither should be confused with derived serving convenience.

---

## 10) Rebuild / replay / reconciliation posture (Content-supported)

### 10.1 Content truth is the authoritative rebuild source

When downstream systems need replay or rebuild, authoritative input should come from:

- current Content truth
- Content history/revision data where policy requires historical reconstruction
- versioned Outbox event streams where applicable

### 10.2 Derived outputs remain derived

SEO-serving state, Reading projections, summaries, notification candidate sets, and reporting outputs built from Content remain derived outputs.

They may be:

- rebuilt
- replaced
- reconciled
- delayed

But they do not become Content truth.

### 10.3 Candidate-before-publication

If a downstream workflow builds a correctness-sensitive derived output from Content truth:

- build candidate first
- validate candidate
- publish/cut over explicitly
- do not treat partial candidate state as complete active output

### 10.4 Rerun safety

Important rebuild/reconciliation workflows that depend on Content truth must be safe to rerun on the same bounded input without corrupting downstream state.

### 10.5 Full rebuild is acceptable when simpler than partial repair

If a derived dataset is cheap enough to regenerate from Content truth and logs:

- full rebuild is preferred over fragile incremental patching
- bounded recompute is preferred over hidden exactly-once assumptions
- explicit reconciliation is preferred over silent divergence

### 10.6 Rebuild inputs and outputs must stay bounded and explainable

A Content-supported rebuild/reconciliation workflow should define:

- bounded input selection
- whether it reads truth, history, logs, or existing derived state
- candidate output shape
- publication/cutover rule
- rerun semantics
- stale-version protection if output is ordering-sensitive

---

## 11) Coordination and singleton posture (Content)

### 11.1 Content does not require global singleton processing by default

Ordinary Content correctness must not depend on:

- one global content worker leader
- one process being “the only publisher”
- one instance being trusted purely because it started first

Content correctness should instead be achieved through:

- truth-store authority
- version-aware mutations
- outbox durability
- idempotent consumers
- stale-event rejection

### 11.2 If future exclusive ownership is introduced

If a future Content workflow truly requires one current owner for a task  
(for example strict scheduled publication ownership or one-current rebuild owner),
that workflow must define:

- ownership source of truth
- monotonic generation/fencing token
- resource-side rejection of stale owner actions

Naive leader/lock patterns are not acceptable.

### 11.3 Safe non-progress beats unsafe stale apply

If ownership is ambiguous for a correctness-sensitive replay/rebuild/publication workflow, the system must prefer:

- delayed rebuild
- stale-owner rejection
- operator retry
- truth-first fallback

over unsafe dual publication of derived state or stale replay overwrite.

---

## 12) Observability signals (Content-specific)

Minimum signals:

- action success/failure
  - `publish`
  - `unpublish`
  - `archive`
  - `restore`
  - `update`
- outbox backlog/age for Content events
- downstream consumer lag
  - SEO
  - Notifications
  - Audit
  - future Reading/search projections
- optimistic concurrency conflicts / stale update rejects
- duplicate-delivery dedupe hits
- stale-event rejection count by consumer where measurable
- public read anomalies:
  - slug resolved but not visible
  - derived route existed but truth rejected visibility
- truth fallback rate when derived state is stale or unavailable
- version-gap / resync-trigger count where applicable
- replay/reconciliation mismatch count for important derived outputs
- candidate publication/cutover failures in downstream rebuild workflows where surfaced
- ownership-generation mismatch count if future ownership-sensitive workflows are introduced

Log requirements:

- propagate `correlationId` across sync action → Outbox event → consumers
- include `ArticleId` and `Version` in key logs
- log replay/rebuild scope when bounded recovery runs are triggered
- do not log sensitive content payloads unnecessarily

---

## 13) Summary

Content correctness in V1 rests on the following rules:

1. Content truth owns visibility and lifecycle legality.  
2. Lifecycle changes commit atomically with version + Outbox intent.  
3. Side effects are eventual and must be idempotent.  
4. Ordering is per article and protected by explicit versioning, not timestamps.  
5. Stale confidence is rejected at the truth/resource boundary; safe fallback beats incorrect exposure.  
6. Cause must be durable before effect becomes externally meaningful.  
7. No global ordering or heterogeneous distributed transaction is assumed for Content workflows.  
8. Content history is append-only by intent.  
9. Content truth is the authoritative rebuild source for downstream derived systems.  
10. Candidate derived output must be validated before publication when output correctness matters.  
11. Replay, rerun, and duplicate delivery are normal and must remain safe under version-aware or deduplicated handling.  
12. Singleton/ownership semantics are not relied on unless explicitly protected by authoritative generation/fencing rules.  
13. Derived freshness is useful, but never the authority for public visibility correctness.