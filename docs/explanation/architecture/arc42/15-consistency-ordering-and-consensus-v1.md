# 15) Consistency, Ordering, and Consensus (V1)

This section defines the **system-wide policy** for consistency strength, ordering guarantees, causality boundaries, and consensus use in CommercialNews V1.

It answers six architecture-level questions:

- Which system states require strong, authority-backed correctness?
- Which workflows require ordered transitions, and at what scope?
- Where is causality preserved explicitly, and where is lag acceptable?
- Which parts of the system may remain eventually consistent by design?
- When does a problem become a coordination / consensus problem?
- Which coordination patterns are explicitly out of scope for V1?

This section is a **policy-level architecture reference**, not an algorithm tutorial and not an implementation of Raft/Paxos/2PC.

> Related:
> - `02-constraints.md`
> - `03-building-blocks-modularity.md`
> - `04-runtime-view-v1.md`
> - `09-architecture-style.md`
> - `11-replication-v1.md`
> - `12-partitioning-v1.md`
> - `13-transactions-and-consistency-v1.md`
> - `14-distributed-systems-assumptions-v1.md`
> - `18-stream-processing-and-derived-state-v1.md`
> - `19-stream-processing-runtime-v1.md`
> - `../../decisions/adr-0013-outbox-and-delivery-semantics-v1.md`
> - `../../decisions/adr-0018-transaction-boundaries-and-consistency-model-v1.md`
> - `../../decisions/adr-0019-system-model-and-fault-assumptions-v1.md`
> - `../../decisions/adr-0020-timeout-retry-and-failure-detection-policy-v1.md`
> - `../../decisions/adr-0021-clock-time-and-ordering-policy-v1.md`
> - `../../decisions/adr-0022-versioning-and-fencing-strategy-v1.md`
> - `../../decisions/adr-0023-consistency-ordering-and-consensus-boundaries-v1.md`
> - `../../decisions/adr-0024-distributed-coordination-and-singleton-work-policy-v1.md`
> - `../../decisions/adr-0027-stream-processing-and-derived-state-policy-v1.md`
> - `../../decisions/adr-0028-consumer-idempotency-replay-and-rebuild-policy-v1.md`

---

## 15.1 Purpose and scope

CommercialNews V1 is intentionally designed so that **not all system behavior requires the same consistency strength**.

Some state must be:
- immediately authoritative
- safe against stale overwrite
- protected against duplicate winners
- enforced at the truth boundary

Other state may:
- lag
- replay
- rebuild
- retry
- tolerate temporary disagreement between truth and derived views

This section defines how V1 separates those concerns.

It is the architecture-level bridge between:

- transaction boundaries (`13-transactions-and-consistency-v1.md`)
- distributed system assumptions (`14-distributed-systems-assumptions-v1.md`)
- outbox/event propagation (`11-replication-v1.md`, ADR-0013)
- versioning/fencing (`ADR-0022`)
- timeout ambiguity and failure interpretation (`ADR-0020`)
- stream-derived state maintenance and replay/rebuild posture (`18`, `19`, ADR-0027, ADR-0028)

Its goal is to prevent architectural drift such as:

- treating all reads as if they were equally fresh
- treating timestamps as ordering truth
- treating eventual convergence as sufficient for security- or truth-critical decisions
- widening business workflows into distributed transactions
- building ad hoc coordination or leader logic without explicit correctness guarantees
- allowing replayed or stale stream events to overwrite newer derived state
- confusing processing order with business-time order in stream-derived workflows

---

## 15.2 Core principles

### 15.2.1 Truth-critical decisions must resolve at an authoritative boundary

Whenever the system must decide:

- who wins
- whether a state transition is legal
- whether a resource is visible
- whether an identity/governance state is current
- whether uniqueness holds

…the decision must be made at the authoritative truth boundary of the owning module.

Derived systems may accelerate, enrich, or mirror that truth.
They do not become the final authority for truth-critical correctness unless an explicit ADR says so.

---

### 15.2.2 Not all consistency needs are equal

CommercialNews V1 intentionally uses multiple consistency classes:

- **strong truth-backed consistency** for correctness-critical state
- **ordered / causal consistency** for per-aggregate transitions and async projections
- **eventual consistency** for rebuildable, lag-tolerant derived effects

This is a deliberate architectural choice, not an inconsistency in design quality.

---

### 15.2.3 Ordering is scoped, not global by default

CommercialNews V1 does **not** require one total global order across all business events in the system.

Instead, V1 primarily requires:

- order **within a truth transaction**
- order **per aggregate / per resource**
- order **within a causal workflow where effect depends on cause**
- order **within an explicitly defined bounded stream/window/join context**, when module semantics require it

Examples:
- article lifecycle order per `ArticleId`
- media ordering / primary selection per aggregate
- role/permission truth transitions per governed entity
- projection freshness order per aggregate
- interaction-derived windowing or enrichment pipelines where event-time semantics matter

Global total order across all modules is not a V1 goal.

---

### 15.2.4 Causality matters more often than global recency

Many workflows do not need “the newest value everywhere immediately.”
They do need:

- effect not visible before cause
- stale event not overwriting newer state
- an async consumer not reintroducing an older view after newer truth exists
- replay or late arrival not changing a correctness-critical outcome silently

Therefore CommercialNews V1 places strong emphasis on:

- outbox as the causal boundary
- aggregate versioning
- stale-event rejection
- truth fallback when freshness is uncertain
- explicit event-time vs processing-time semantics where stream-derived logic depends on time

---

### 15.2.5 Consensus is minimized, not denied

Some problems in distributed systems fundamentally require deciding one winner or one outcome.
Examples include:

- unique claim of a slug or identity key
- ownership transfer for a singleton-style actor
- leader/owner selection in future coordination-heavy workflows
- commit/abort style global decisions

CommercialNews V1 minimizes those cases by design.

Where V1 can safely solve a problem by:
- local DB constraint
- local transaction
- optimistic concurrency
- aggregate versioning
- fencing/generation check
- idempotent replay-safe async processing
- bounded rebuild/reconciliation from truth

…it does so instead of introducing generalized consensus machinery.

---

### 15.2.6 If uncertain, prefer safe refusal over incorrect confidence

Because:
- timeout is ambiguous
- clocks do not define causality
- stale actors may wake up
- derived state may lag
- process pauses are normal
- replay and duplicate delivery are normal

CommercialNews V1 prefers:
- temporary failure
- explicit conflict
- fallback to truth
- safe non-exposure
- rejecting stale writes
- resync/rebuild over silent best-effort overwrite

over:
- silent stale overwrite
- false winner selection
- cache-based overconfidence
- local-belief-based authority

---

## 15.3 Consistency classes in CommercialNews V1

### 15.3.1 Strong truth-backed consistency

Required where stale or ambiguous state would directly break correctness.

This includes:

- publication truth and lifecycle correctness
- identity/security truth (verify/reset/password/lock/revoke/active state)
- authorization truth (role/permission assignment and revocation)
- uniqueness constraints at the owning truth store
- SEO routing truth where slug ownership and routing correctness matter
- media truth invariants such as primary media and authoritative ordering
- token rotation/revocation truth
- any state transition whose result must be immediately authoritative

For these paths:
- correctness is enforced at the owning truth boundary
- writes must commit synchronously at truth
- stale cache or lagging projection must not override the decision
- timestamp-based winner selection is disallowed

---

### 15.3.2 Ordered / causality-sensitive consistency

Required where a later effect depends on an earlier cause, even if the result need not be globally linearizable.

This includes:

- article lifecycle transitions per article
- ordered domain events emitted through outbox
- projection updates per aggregate
- media processing state transitions
- stale event rejection in derived views
- ownership-transfer workflows protected by generation/fencing
- audit/event chains where event sequence matters for interpretation
- async flows where `cause -> effect` must remain visible in correct order
- stream-derived updates where out-of-order delivery or replay could reintroduce older state
- join/enrichment logic where “which version of state” matters to correctness

Typical V1 mechanisms:
- `AggregateId + Version`
- expected-version writes
- `LastAppliedVersion`
- gap detection + resync from truth
- generation/fencing tokens
- state-machine validation

---

### 15.3.3 Eventual consistency

Accepted where lag does not invalidate the truth decision itself.

This includes:

- audit persistence in downstream stores
- notification delivery
- interaction counters and aggregates
- cache invalidation and cache warming
- projections/read models
- search/indexing
- derived dashboards and non-critical admin views
- selected SEO enrichments that do not own publication truth
- bounded batch-generated summaries and rematerialized derived outputs

These paths must still be:
- observable
- replayable
- repairable
- idempotent
- safe under duplicate and delayed delivery

Eventual consistency in V1 is never permission to:
- leak unpublished content
- trust stale security state
- ignore stale-write protection on derived materializations
- replace truth with cache
- hide replay/rebuild uncertainty behind an apparently authoritative read

---

## 15.4 Ordering policy

### 15.4.1 Ordering inside truth boundaries

Within one local truth transaction, the owning module defines the authoritative outcome.

This is the strongest and simplest ordering boundary in V1.

Examples:
- Content publish/unpublish truth mutation + outbox append
- Identity verification truth mutation + outbox append
- Authorization change + local truth durability + outbox append

The transaction boundary is where:
- the state transition becomes authoritative
- the intent for async effects becomes durable
- downstream systems begin from a stable truth point

---

### 15.4.2 Ordering across async propagation

Async propagation follows ADR-0013 and ADR-0028:

- Outbox is the required replication boundary
- broker and consumers are at-least-once
- duplicates are normal
- no global total order is assumed
- ordered processing is per aggregate when required
- replay and rebuild are normal recovery tools

Therefore V1 ordering policy for async flows is:

- preserve causal relation between truth commit and async work via Outbox
- include version/sequence when order matters for an aggregate
- reject stale or out-of-order materialization updates where correctness depends on order
- resync from truth when gaps are detected
- do not assume processing order is business-time order

---

### 15.4.3 No global total order requirement

CommercialNews V1 does not require:

- one globally ordered event stream across all modules
- one system-wide log as correctness authority for all business writes
- a consensus-backed total order broadcast layer for ordinary business workflows

This avoids unnecessary coordination cost and complexity.

Where order is needed, it is scoped to:
- one truth transaction
- one aggregate
- one ownership grant
- one explicitly defined coordination resource
- one explicitly defined stream/window/join context

---

### 15.4.4 Timestamps are not ordering truth

As defined in ADR-0021:

- wall-clock timestamps support chronology, reporting, and expiry
- they do not define causality
- they do not prove freshness
- they do not authorize “last write wins” for correctness-critical state

Therefore:
- `UpdatedAt` is informative, not decisive
- `OccurredAt` supports investigation, not stale-write protection by itself
- cross-node timestamp comparison is not a substitute for versioning
- processing time is not a substitute for event-time ordering in stream-derived business logic

---

## 15.5 Causality policy

### 15.5.1 Effect must not outrun required cause

CommercialNews V1 adopts the rule:

- if one state or effect depends on another, the required cause must be durable before the effect is considered valid

Examples:
- publish notification depends on publish truth
- projection update depends on truth commit
- SEO derived metadata depends on authoritative routing/publication truth
- media “ready” state depends on prior upload/validation/scanning steps if required by that workflow
- stream-derived enrichment depends on the correct reference/version semantics being available

This does not require every side effect to finish synchronously.
It does require that side effects derive from committed truth, not wishful sequencing.

---

### 15.5.2 Outbox is the standard causal bridge

Outbox is not only a delivery pattern.
In V1 it is also the standard **causal boundary** between:

- truth mutation
- durable publication intent
- async downstream processing

This means:
- the event is never “more real” than the truth change
- async consumers should reason from outbox-backed truth, not from local timing assumptions
- missing side effects are recoverable by replay
- ambiguous publish outcomes are reconciled from truth and outbox state, not from timeout guesswork

---

### 15.5.3 Derived state must not become a causality shortcut

Redis, projections, and downstream consumers must not be treated as if their visibility defines truth order.

Examples of disallowed reasoning:
- “cache already shows it, so publish truth must be done”
- “notification sent, so visibility is guaranteed”
- “projection updated, so the consumer order must have been correct”
- “later timestamp in a derived store means causally newer”
- “consumer processed it later, so it must belong to the latest business window”

Truth and version-aware sequencing remain the architectural authorities.

---

## 15.6 Time semantics for stream-derived behavior

### 15.6.1 Event time and processing time serve different purposes

CommercialNews distinguishes:

- **event time** — when the business event actually occurred
- **processing time** — when the worker/consumer processed it

This distinction matters when:
- building interaction aggregates
- computing trends
- detecting security or abuse patterns
- correlating multiple streams
- replaying delayed events after outage or backlog

### 15.6.2 Business-time correctness should prefer event time where needed

Where module logic depends on when the business event actually happened, event time is the meaningful basis for ordering/windowing.

Examples:
- trending windows
- login failure bursts
- session-style reader behavior
- publish/unpublish analysis
- future notification-open correlations

Processing time may still be used for:
- worker throughput
- queue drain rate
- backlog monitoring
- retry pacing

### 15.6.3 Every windowed or joined pipeline needs explicit semantics

For any important stream-derived pipeline, module/runtime docs must define:

- whether the logic uses event time or processing time
- how late arrivals are handled
- whether correction/retraction is supported
- which version of reference state is joined against
- whether replay must be deterministic

CommercialNews V1 does not allow hidden or accidental time semantics in important pipelines.

---

## 15.7 Consensus and coordination boundary

### 15.7.1 What counts as a coordination / consensus-like problem in V1

A workflow begins to resemble a consensus/coordination problem when it must decide one winner among multiple contenders, such as:

- one unique slug claim
- one unique username/email claim
- one current owner of a critical singleton task
- one accepted generation of ownership
- one authoritative commit/abort outcome across a boundary
- one leader/current owner for a coordination-sensitive resource

Not every such problem requires a generalized consensus system in V1.
It does require explicit handling.

---

### 15.7.2 V1 default: solve locally where possible

CommercialNews V1 prefers to solve “one winner” problems using the narrowest authoritative mechanism available:

- DB unique constraints
- local ACID transaction
- compare-and-set / expected-version logic
- resource-side generation/fencing checks
- version-aware stale-write rejection

This keeps correctness close to the owning truth store and avoids introducing cluster-wide consensus machinery for ordinary business invariants.

---

### 15.7.3 No custom consensus algorithm in V1

CommercialNews V1 explicitly does **not** implement:

- custom Raft/Paxos/Zab/VSR
- custom total-order broadcast service
- homegrown leader election with quorum logic
- application-level distributed lock service pretending to be consensus-safe
- generalized cluster membership subsystem as part of core app logic

These are out of scope for V1.

---

### 15.7.4 Coordination service is not part of the V1 baseline

CommercialNews V1 does not require ZooKeeper/etcd/Consul-style coordination service as part of the baseline architecture.

Reason:
- V1 can keep truth-critical decisions inside local truth boundaries
- many workflows can be made safe through idempotency, partitioning, versioning, truth fallback, and bounded rebuild/reconciliation
- forcing a coordination subsystem too early would add cost without enough architecture value

If future requirements introduce:
- strict singleton scheduling
- leased ownership with transfer
- partition assignment with rebalance
- stateful cluster membership
- leader election for critical processing ownership

then such capability must be introduced by explicit ADR and must use a proven coordination system rather than a homemade one.

---

## 15.8 Distributed transaction and atomic commit policy

### 15.8.1 Heterogeneous distributed atomic commit remains out of scope

CommercialNews V1 does not use XA / heterogeneous 2PC across:

- SQL + RabbitMQ
- SQL + Redis
- SQL + Mongo or independent derived stores
- SQL + email provider
- SQL + object storage
- multiple independent business truth stores as one global commit unit

This remains unchanged from ADR-0018 and is reaffirmed here because the trade-off remains explicit:
- distributed atomic commit is expensive
- coordinator failure is operationally painful
- blocking semantics are a poor fit for V1 application workflows

---

### 15.8.2 Atomicity stops at the local truth boundary

The atomic decision boundary in V1 is:

- truth mutation
- local metadata/history required by the command
- outbox record

Everything beyond that is:
- async
- replayable
- retryable
- observable
- eventually consistent

This is not a weak workaround.
It is the chosen architecture posture for V1.

---

### 15.8.3 Exactly-once is not a global claim in V1

CommercialNews V1 does not claim exactly-once behavior across:
- DB
- broker
- cache
- provider
- projections
- external delivery systems

Instead V1 claims:
- local atomic truth commit
- outbox durability
- at-least-once downstream delivery
- idempotent consumers
- stale-event rejection where needed
- replay/reconciliation from truth

This is the correctness model operators and developers must reason with.

---

## 15.9 Membership, singleton work, and ownership policy

### 15.9.1 Singleton semantics are not free

Any claim of the form:
- “only one worker may do this”
- “this node is the current owner”
- “this task has exactly one active executor”

is a coordination-sensitive claim.

CommercialNews V1 does not allow such semantics to be assumed informally by:
- local memory flags
- startup order
- wall-clock-based freshness guesses
- naive “whoever starts first wins”
- timeout-only confidence without resource-side validation

---

### 15.9.2 Prefer design that reduces singleton dependence

Before introducing strict singleton coordination, V1 should prefer:

- idempotent handlers
- partitioned work ownership
- aggregate-scoped serialization where necessary
- DB-enforced winner selection
- replay-safe processing
- explicit state machine transitions
- generation/fencing where ownership transfer exists
- rebuild/reconciliation rather than fragile in-place repair where safer

The best way to survive coordination complexity is often to need less of it.

---

### 15.9.3 If ownership transfer exists, use fencing/generation semantics

If a workflow truly requires “current owner” semantics, the ownership must be protected by:

- monotonic generation/fencing token
- authoritative resource-side validation
- rejection of lower-generation actions

This follows ADR-0022 and protects against:
- stale actors waking after pause
- former owners acting after transfer
- delayed commands reapplying obsolete authority

---

## 15.10 Module mapping (V1)

### 15.10.1 Content
- Publication lifecycle truth is strong and authoritative.
- Lifecycle transitions are ordered per article.
- Publish/unpublish success is based on truth commit, not on notification or projection completion.
- Public visibility correctness must be protected against stale cache and lagging derived systems.
- Outbox events should carry aggregate version when ordered downstream effects matter.

### 15.10.2 Identity
- Verification/reset/password/account-state truth is strong and immediate.
- Read-your-writes is required for selected self-state and security-sensitive follow-up reads.
- Email delivery is eventual and must not be mistaken for the truth transition itself.
- Expiry uses UTC time, but ordering/freshness of state must not rely on timestamp alone.
- Security or anomaly pipelines that use time windows must define event-time vs processing-time semantics explicitly.

### 15.10.3 Authorization
- Role/permission truth is strong and authoritative.
- Governance reads immediately after mutation must reflect current truth.
- Stale admin actions should be protectable by explicit concurrency/version logic where applicable.
- Audit remains downstream and must not block governance truth commit.
- Effective-permission projections or caches must not override truth when freshness is uncertain.

### 15.10.4 SEO
- Slug ownership/routing truth is correctness-sensitive.
- Slug uniqueness is enforced at truth, not via app-side race-prone checks.
- Public routing may use cache-first lookup, but truth still defines safety and visibility.
- Metadata enrichments may lag; routing truth may not become ambiguous.
- Derived serving artifacts must reject stale overwrite or be rebuilt from truth safely.

### 15.10.5 Media
- Authoritative ordering/primary-media invariants are strong truth concerns.
- Async processing (scan/thumbnail/etc.) is causal and replayable, not one global transaction.
- Reorder and final-state mutation paths require stale-write protection where admin workflows can race.
- “Ready” or equivalent externally meaningful state must not outrun required prior causes.

### 15.10.6 Interaction
- Counters and aggregates are eventual by policy.
- Duplicate and delayed deliveries are normal.
- If any ordered materialization exists, projection freshness markers must reject stale updates.
- Public reading must not depend on exact aggregate freshness for correctness-critical exposure decisions.
- Windowed analytics and future stream joins must state event-time and late-arrival policy explicitly.

### 15.10.7 Audit
- Append-only ingestion is async and lag-tolerant for core flow success.
- Message-level idempotency is mandatory.
- If materialized audit views are introduced, stale-event rejection rules still apply to those views.
- Audit chronology is useful for investigation, but timestamps alone do not reconstruct causality.

### 15.10.8 Notifications
- Delivery is eventual and retry-safe, not part of the originating truth commit.
- Message-level and business-level idempotency are mandatory.
- Delivery state is derived.
- If future ordered notification materializations exist, they must use explicit freshness markers rather than timestamp ordering.
- Future sent→open correlation pipelines must define join window and time semantics explicitly.

### 15.10.9 Reading Experience
- Public reads may use cache and derived enrichments.
- Truth still defines visibility correctness.
- Safe fallback or safe negative response is preferred over stale confidence.
- Reading flows must remain correct even if non-critical derived systems lag behind truth.
- Future read-side projections or search-serving views remain derived and rebuildable, not hidden truth.

---

## 15.11 What V1 does not promise

CommercialNews V1 does **not** promise:

- global linearizability across all stores and side effects
- one total order across all system events
- exactly-once semantics across DB + broker + providers
- cluster-wide consensus for ordinary business workflows
- custom leader election or cluster membership as part of baseline app runtime
- that timestamps alone prove ordering, recency, or causality
- that request success implies all downstream effects already completed
- that cache freshness equals truth freshness
- that stream processing order automatically equals business order
- that replayed events can safely reapply without version/idempotency controls

Instead V1 promises:

- strong local truth commit at the owning boundary
- durable outbox intent where async work is required
- explicit versioning/fencing where stale actors or stale writes matter
- eventual completion and observability for derived effects
- safe fallback over stale overconfidence
- replay/rebuild/reconciliation posture for important derived systems

---

## 15.12 Failure semantics under this policy

### 15.12.1 If truth commit succeeds
The user-facing mutation is successful even if:
- broker publish has not yet occurred
- notification has not yet been sent
- projection has not yet caught up
- cache is not yet invalidated

### 15.12.2 If derived order is ambiguous
The system must prefer:
- version-aware rejection
- resync from truth
- safe non-exposure
- explicit conflict
- rebuild/reconciliation where appropriate

over:
- best-effort blind overwrite
- timestamp-based guesswork
- stale actor optimism

### 15.12.3 If future coordination loses confidence
For any future coordination-sensitive subsystem, CommercialNews must prefer:
- no progress
- safe blocking
- ownership rejection
- leader/lease invalidation

over:
- split brain
- dual owners
- stale owner writes
- silent correctness loss

### 15.12.4 If stream-derived logic sees late or replayed events
The system must apply the declared policy for that pipeline, which may include:
- ignore after lateness cutoff
- apply with correction
- resync/rebuild
- defer until state/order confidence is restored

Hidden semantics are not acceptable.

---

## 15.13 Evolution notes (V2+)

As CommercialNews evolves, consistency and coordination may become more explicit in areas such as:

- dedicated read-model components
- projection checkpointing
- stricter scheduler ownership
- partition assignment and rebalance
- dedicated coordination service
- stronger aggregate-level or stream-level sequencing
- explicit cluster membership for stateful processing components
- richer event-time analytics and late-event correction
- more formal stream-window/join semantics for interaction and security pipelines

Even then, the V1 principles remain the baseline:

- truth first
- derived may lag
- ordering is scoped unless explicitly widened
- timestamp is not ordering truth
- stale actors and stale events must be rejectable
- do not introduce consensus machinery casually
- use proven infrastructure for coordination if it becomes necessary

---

## 15.14 Summary

CommercialNews V1 treats consistency, ordering, and consensus as **different classes of architectural problems**, not one giant switch called “strong consistency.”

The system posture is:

- use **strong truth-backed correctness** where the business needs one authoritative answer
- use **per-aggregate ordering and causality protection** where later effects depend on earlier causes
- use **eventual consistency** for lag-tolerant, rebuildable, observable derived behavior
- use **versioning and fencing** instead of timestamp-based freshness guesses
- use **explicit time semantics** where stream-derived windows, joins, or analytics depend on them
- avoid **heterogeneous distributed transactions**
- avoid **homegrown consensus and coordination**
- introduce coordination infrastructure only when a real ownership/membership problem justifies it

CommercialNews V1 therefore optimizes for:

- correctness at truth boundaries
- explicit handling of stale actors and stale events
- reliable async propagation through Outbox
- safe fallback under lag and ambiguity
- minimal coordination surface consistent with business correctness
- replay/rebuild-safe derived-state maintenance