# ADR-0023 — Consistency, Ordering, and Consensus Boundaries (V1)

**Status:** Accepted  
**Date:** 2026-03-09  
**Decision owners:** Architecture / Platform  
**Scope:** System-wide (consistency classes, ordering scope, causality boundaries, consensus minimization, coordination posture)  
**Related:**
- `../architecture/arc42/11-replication-v1.md`
- `../architecture/arc42/12-partitioning-v1.md`
- `../architecture/arc42/13-transactions-and-consistency-v1.md`
- `../architecture/arc42/14-distributed-systems-assumptions-v1.md`
- `../architecture/arc42/15-consistency-ordering-and-consensus-v1.md`
- ADR-0013 (Outbox & delivery semantics)
- ADR-0018 (Transaction boundaries & consistency model)
- ADR-0019 (System model and fault assumptions)
- ADR-0020 (Timeout, retry, and failure detection policy)
- ADR-0021 (Clock, time, and ordering policy)
- ADR-0022 (Versioning and fencing strategy)

---

## Context

CommercialNews V1 already defines:

- local truth transactions as the primary correctness boundary
- outbox as the required async replication boundary
- a partially synchronous, crash-recovery system model
- timeout as ambiguity, not proof
- wall-clock time as useful operationally but insufficient for causality/order authority
- versioning and fencing as required protection against stale writes and stale actors

However, without one explicit ADR for **consistency, ordering, and consensus boundaries**, the architecture still risks drift in several ways:

- treating all reads as if they require the same freshness guarantees
- assuming that a lagging derived view is acceptable even for truth-critical decisions
- treating timestamps as if they prove causal order
- assuming global ordering where only per-aggregate ordering exists
- introducing ad hoc singleton/leader patterns without explicit coordination semantics
- widening business workflows into distributed coordination problems unnecessarily
- treating eventual convergence as sufficient for uniqueness, visibility, or ownership decisions

CommercialNews needs one accepted system-wide decision that states:

- which categories of state require strong truth-backed correctness
- where ordering is required and at what scope
- where causal correctness matters more than global recency
- where eventual consistency is explicitly acceptable
- when a problem should be considered a coordination/consensus problem
- which consensus/coordination approaches are intentionally out of scope for V1

---

## Decision

### 1) CommercialNews V1 uses three consistency classes, intentionally

CommercialNews V1 does **not** use a single uniform consistency strength for all state and all workflows.

Instead, V1 adopts three explicit consistency classes:

#### A) Strong truth-backed consistency
Used where correctness requires one authoritative answer at the owning truth boundary.

#### B) Ordered / causality-sensitive consistency
Used where later effects depend on earlier causes, especially per aggregate or per owned workflow.

#### C) Eventual consistency
Used where lag is acceptable because the state is derived, rebuildable, and not the primary correctness authority.

This is a deliberate system design decision, not a compromise by accident.

---

### 2) Strong truth-backed consistency is required for correctness-critical state

The following categories require strong, authoritative correctness at the owning truth boundary:

- publication state and lifecycle truth
- identity/security truth
- authorization/governance truth
- uniqueness constraints in the owning truth store
- SEO routing truth where slug ownership and routing safety matter
- media truth invariants such as primary media and authoritative ordering
- token rotation/revocation truth
- any state transition whose immediate result must be authoritative to the user/admin/system

For these paths:

- correctness is enforced at truth
- success is based on local truth commit
- stale cache or lagging projections must not override the decision
- timestamp-based “latest wins” logic is disallowed
- if freshness is uncertain, fallback must prefer truth or safe refusal

---

### 3) Ordered / causality-sensitive consistency is required per aggregate or per owned workflow

CommercialNews V1 requires explicit ordering or causality protection where:

- one transition depends on a prior transition
- a delayed consumer could revert newer derived state
- an async effect depends on an earlier truth commit
- a stale admin/process could overwrite newer state
- ownership or execution rights may transfer over time

Typical examples include:

- article lifecycle transitions per `ArticleId`
- projection updates per aggregate
- media processing state chains
- stale-event rejection in derived views
- ownership transfer protected by fencing/generation
- version-sensitive async consumers
- workflow chains where effect must not be externally meaningful before cause is durable

The approved V1 posture is:

- ordering is primarily **per aggregate / per resource**
- events carry `AggregateId + Version` where ordering matters
- consumers reject stale events and resync from truth if gaps matter
- no global system-wide event order is assumed by default

---

### 4) Eventual consistency is explicitly accepted for lag-tolerant derived behavior

CommercialNews V1 accepts eventual consistency for state that is:

- derived from truth
- rebuildable or replayable
- observable when lagging
- not the sole authority for security, visibility, or ownership-critical correctness

Examples include:

- audit persistence in downstream stores
- notification delivery
- interaction counters and aggregates
- cache invalidation and cache warming
- search/indexing
- projections/read models
- non-critical dashboards and derived enrichments

Acceptance of eventual consistency does **not** allow:

- drafts/unpublished content to be exposed because of stale cache
- stale identity/governance state to override truth
- stale consumers to overwrite newer derived state
- derived freshness to become hidden truth

---

### 5) Ordering in V1 is scoped, not global

CommercialNews V1 explicitly rejects a default requirement for:

- one total global order across all business events
- one globally linearizable event stream across all modules
- one consensus-backed ordering layer for all application workflows

Instead, V1 adopts the following ordering model:

- **inside one truth transaction**: ordering is authoritative at the local truth boundary
- **per aggregate / per resource**: ordering is enforced where business correctness needs it
- **across unrelated aggregates/modules**: no global total order is assumed unless explicitly introduced by future ADR

This keeps coordination cost bounded and aligned with actual business need.

---

### 6) Causality matters more often than global recency

CommercialNews V1 adopts the rule that many workflows do not require the globally newest value everywhere immediately, but they do require that:

- effect does not outrun required cause
- stale events do not overwrite newer derived state
- externally meaningful state does not appear before required prior truth is durable

Therefore:

- Outbox is the standard causal boundary between truth commit and async propagation
- derived systems must preserve required cause-effect relationships
- truth commit remains the authority for “what happened”
- downstream completion timing does not redefine truth

This means causality protection in V1 is provided primarily by:

- local truth transaction
- outbox durability
- per-aggregate versioning
- stale-event rejection
- resync from truth
- fencing/generation checks where ownership transfers

---

### 7) Timestamp ordering is not correctness authority

CommercialNews V1 reaffirms ADR-0021:

- wall-clock timestamps are useful for audit, reporting, scheduling, and investigation
- they do not prove causality
- they do not define authoritative freshness
- they do not justify naive “latest timestamp wins” for correctness-critical data

Therefore V1 requires:

- versioning over timestamp for ordered/conflict-sensitive state
- generation/fencing over timestamp for ownership transfer
- truth-backed reconciliation over timestamp-based guesswork after ambiguity

Any design relying primarily on:
- `UpdatedAt`
- `OccurredAt`
- `ProcessedAt`
- or “highest timestamp wins”

for correctness-critical state is non-compliant unless justified by explicit ADR.

---

### 8) Consensus and coordination problems must be minimized by design

CommercialNews V1 treats a workflow as a coordination/consensus-like problem when it must decide one winner or one current authority, such as:

- unique slug claim
- unique username/email claim
- one current owner of a singleton-style task
- one accepted generation of ownership
- one authoritative commit/abort outcome across a wider boundary
- one leader/current owner for a coordination-sensitive resource

The V1 default posture is:

- solve such problems locally where possible
- use the narrowest authoritative primitive available
- avoid widening them into generalized cluster coordination unless necessary

Preferred V1 tools are:

- DB unique constraints
- local ACID transactions
- compare-and-set / expected-version logic
- resource-side generation/fencing checks
- version-aware stale-write rejection
- idempotent replay-safe async processing

---

### 9) No custom consensus or generalized coordination subsystem in V1

CommercialNews V1 explicitly does **not** implement:

- custom Raft/Paxos/Zab/VSR
- custom total-order broadcast
- custom quorum-based leader election
- application-level distributed lock service pretending to be consensus-safe
- generalized cluster membership subsystem as part of baseline application runtime

CommercialNews V1 also does not require a ZooKeeper/etcd/Consul-style coordination service in the baseline architecture.

If future requirements truly justify:

- strict singleton scheduling
- leased ownership with transfer
- partition assignment and rebalance
- stateful cluster membership
- critical leader election for processing ownership

then such capability must be introduced by explicit ADR and must use a proven coordination system, not a homemade one.

---

### 10) Heterogeneous distributed atomic commit remains out of scope

CommercialNews V1 explicitly rejects using heterogeneous distributed transactions / XA / 2PC across:

- SQL + RabbitMQ
- SQL + Redis
- SQL + Mongo or other derived stores
- SQL + email provider
- SQL + object storage
- multiple independent truth stores as one global commit unit

The architecture remains:

- local truth transaction
- outbox in the same transaction when async side effects are required
- at-least-once downstream delivery
- idempotent consumers
- stale-event rejection where needed
- replay/reconciliation from truth

Atomicity stops at the local truth boundary.

This decision exists because chapter-9-style coordination/atomic-commit costs are real:
- blocking risk
- operational pain
- failure amplification
- coordination complexity
- poor fit for V1 application workflows

---

### 11) Singleton/ownership claims are coordination-sensitive and must not be informal

CommercialNews V1 rejects informal claims such as:

- “only one worker will do this”
- “this process is obviously still the owner”
- “whoever started first keeps control”
- “timeout means the old owner is definitely gone”

Where singleton or ownership semantics truly matter, the system must use:

- authoritative state
- generation/fencing semantics
- resource-side validation
- explicit ownership transfer rules

Before introducing strict singleton coordination, V1 should first prefer:

- idempotent processing
- partitioned work
- per-aggregate serialization
- DB-enforced winner selection
- replay-safe state-machine-driven workflows

The architecture prefers reducing coordination need over increasing coordination machinery.

---

## Decision summary

CommercialNews V1 adopts the following system-wide boundaries:

- **strong truth-backed consistency** for correctness-critical truth state
- **per-aggregate / causality-sensitive ordering** where effect depends on prior cause
- **eventual consistency** for lag-tolerant, rebuildable derived behavior
- **no default global total order**
- **timestamp is not ordering truth**
- **consensus/coordination problems are minimized by design**
- **no custom consensus subsystem in V1**
- **no heterogeneous distributed transactions / XA / 2PC**
- **singleton/ownership semantics require explicit authoritative protection, not local belief**

---

## Consequences

### Positive

- Aligns consistency strength with business need instead of over-coordinating everything
- Makes strong correctness boundaries explicit and reviewable
- Prevents accidental reliance on stale cache, derived views, or timestamp ordering
- Encourages per-aggregate ordering and version-aware async design
- Keeps V1 operable without introducing premature coordination infrastructure
- Reinforces the chosen architecture posture:
  - truth first
  - outbox as causal boundary
  - versioning/fencing over timestamps
  - replayable eventual side effects

### Negative / Trade-offs

- Some flows must document and implement explicit versioning/generation logic
- Developers cannot assume “one consistency model fits all”
- Derived views may lag and require fallback or reconciliation logic
- Certain future singleton or ownership-heavy workflows may require new infrastructure decisions
- Some users/admins may see eventual lag on non-critical derived surfaces even though truth already committed

---

## Alternatives considered

### 1) Treat all state as requiring strong global consistency
- Pros: simple mental model in theory.
- Cons: excessive coordination cost, poor fit for V1, unnecessary latency and complexity for derived paths.

Rejected.

### 2) Treat eventual consistency as sufficient for all workflows
- Pros: simpler async-first posture.
- Cons: unacceptable for publication truth, security/governance state, uniqueness decisions, and routing correctness.

Rejected.

### 3) Introduce a global total-order / consensus layer as V1 baseline
- Pros: strong ordering model.
- Cons: over-engineered for V1 scope; high complexity and operational cost; unjustified for current requirements.

Rejected.

### 4) Solve freshness/ordering by wall-clock timestamp comparison
- Pros: simple implementation.
- Cons: unsafe under skew, delay, pause, and retry; contrary to ADR-0021 and system model assumptions.

Rejected.

### 5) Use heterogeneous distributed transactions to “avoid eventual consistency”
- Pros: superficially stronger end-to-end atomicity.
- Cons: blocking and operational pain; poor fit for app-level workflow boundaries in CommercialNews V1.

Rejected.

---

## Implementation notes (V1)

- `15-consistency-ordering-and-consensus-v1.md` is the architecture-level narrative reference for this ADR.
- Module docs must reflect which consistency class each important workflow belongs to.
- Module-level `06-idempotency-consistency.md` files should specify:
  - truth-critical decisions
  - ordered/causal flows
  - eventual derived behavior
  - versioning/freshness mechanism
  - stale-write/stale-event rejection behavior
- New features must explicitly identify:
  - truth boundary
  - ordering scope
  - whether causality must be preserved
  - whether any winner/ownership decision exists
  - whether the problem can be solved locally without coordination infrastructure
- Reviews should reject designs that:
  - rely on timestamps as freshness authority
  - assume cache freshness for truth-critical decisions
  - introduce hidden global ordering assumptions
  - introduce singleton semantics without authoritative protection

---

## Follow-ups

- Create ADR-0024: Distributed Coordination and Singleton Work Policy (V1)
- Update `docs/explanation/architecture/arc42/00-index.md` to add section 15
- Update `docs/explanation/decisions/README.md` to include ADR-0023 and ADR-0024
- Update module docs, especially:
  - `modules/content/06-idempotency-consistency.md`
  - `modules/seo/06-idempotency-consistency.md`
  - `modules/identity/06-idempotency-consistency.md`
  - `modules/authorization/06-idempotency-consistency.md`
  - `modules/media/06-idempotency-consistency.md`
- If future cluster coordination is introduced, document:
  - ownership model
  - quorum/majority assumptions
  - fencing strategy
  - membership source of authority
  - safe behavior under loss of coordination confidence