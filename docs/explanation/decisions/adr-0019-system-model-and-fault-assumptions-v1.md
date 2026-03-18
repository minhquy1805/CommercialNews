# ADR-0019 — System Model and Fault Assumptions (V1)

**Status:** Accepted  
**Date:** 2026-03-09  
**Decision owners:** Architecture / Platform  
**Scope:** System-wide (runtime assumptions, fault model, trust model, truth vs derived posture)  
**Related:**
- `../architecture/arc42/04-runtime-view-v1.md`
- `../architecture/arc42/08-components.md`
- `../architecture/arc42/09-architecture-style.md`
- `../architecture/arc42/11-replication-v1.md`
- `../architecture/arc42/12-partitioning-v1.md`
- `../architecture/arc42/13-transactions-and-consistency-v1.md`
- `../architecture/arc42/14-distributed-systems-assumptions-v1.md`
- ADR-0013 (Outbox & delivery semantics)
- ADR-0015 (Cache policy & invalidation)
- ADR-0018 (Transaction boundaries & consistency model)

---

## Context

CommercialNews V1 is not a single-machine program in the operational sense.

Even though V1 is intentionally kept small in deployable topology
(API + Worker, shared database option, selective event-driven side effects),
the system already has distributed-system behavior because it includes:

- multiple deployable runtime components
- asynchronous propagation through Outbox → Broker → Consumers
- truth stores plus lagging derived stores
- cache-first read paths with truth fallback
- partial failures where one subsystem may degrade while others remain available
- retry, duplicate delivery, and backlog recovery as normal operating conditions

Without an explicit system model, the architecture risks drift in several ways:

- developers implicitly assuming synchronous visibility after writes
- timeout being treated as proof of remote failure
- cross-node timestamps being treated as causality
- derived state being treated as truth
- pause/restart/retry behavior being under-specified
- observability and operational alerts being defined against the wrong assumptions

We need one accepted system-wide decision that states:

- what kind of distributed environment CommercialNews assumes
- which faults are normal and must be tolerated
- which threats are in scope vs out of scope for V1
- how truth, derived state, and uncertainty are interpreted

---

## Decision

### 1) Timing model: partially synchronous

CommercialNews V1 assumes a **partially synchronous** environment.

This means:
- most of the time, network and process behavior stays within expected operational envelopes
- occasionally, latency, queueing, pauses, retries, and clock error may become much larger than normal for a finite period
- the architecture must remain safe even when timing temporarily becomes unpredictable

We explicitly do **not** assume:
- globally bounded network delay at all times
- immediate broker or cache propagation after truth commit
- perfectly synchronized clocks
- hard real-time guarantees for request handling or background work

This model reflects the actual target environment:
- cloud/VM/container execution
- shared infrastructure
- read-heavy bursty traffic
- async consumers and cache layers

---

### 2) Node/process fault model: crash-recovery

CommercialNews V1 assumes **crash-recovery** faults for internal components.

This means:
- API hosts, workers, and processes may crash or restart
- in-memory state may be lost
- durable truth in the primary store is expected to survive restart
- async work may resume later through retry, replay, reconciliation, or rebuild procedures

This decision applies to:
- Public API hosts
- Background Worker hosts
- internal consumers and publishers
- deployment/restart behavior in V1 infrastructure

CommercialNews V1 does **not** assume crash-stop as the dominant model, because:
- processes and containers are expected to restart
- recovery is part of normal operations
- durable state survives longer than process lifetime

---

### 3) Process pauses are normal and in scope

CommercialNews V1 explicitly assumes that processes/threads may pause for non-trivial periods due to:
- runtime pauses
- CPU scheduling and noisy-neighbor effects
- VM or host-level suspend/resume
- I/O stalls
- memory pressure or paging
- operational restart or infrastructure events

Therefore the architecture must not rely on assumptions such as:
- “this code path will definitely finish before the environment changes”
- “I checked validity, so I must still be valid immediately afterward”
- “a healthy process is always making forward progress”

This affects:
- lease/ownership logic
- stale-worker protection
- retry safety
- idempotent handler design
- resource-side version/fencing checks where applicable

---

### 4) Internal trust model: faulty but honest

CommercialNews V1 assumes internal nodes are **faulty but honest**.

Internal services may:
- be slow
- restart
- retry
- lag behind truth
- process duplicates
- act on incomplete knowledge

But they are not assumed Byzantine by default.

Therefore CommercialNews V1 focuses on:
- crash-recovery resilience
- idempotency
- replay safety
- stale-write protection
- observability and recovery

It does **not** attempt full Byzantine fault tolerance for internal services.

This decision applies to:
- Public API
- Background Worker
- internal broker publishing/consumption
- Redis
- SQL truth stores
- internal coordination assumptions in V1

---

### 5) External boundary model: clients may be malicious or malformed

CommercialNews V1 assumes external clients and public inputs are untrusted unless verified server-side.

This includes:
- request payloads
- headers
- claims
- timestamps
- retry behavior
- malformed inputs
- abusive traffic patterns

Therefore:
- public input must be validated and bounded
- authority must come from server truth, not client assertions
- client-supplied time must not become authority for correctness or security decisions
- public/security-sensitive paths must use defensive validation and safe logging

This is the primary “malicious actor” assumption in V1.
It replaces any need for full internal Byzantine tolerance.

---

### 6) Truth model: explicit source of truth per module

CommercialNews V1 adopts a strict truth-vs-derived model.

Each module must define one authoritative source of truth for its business rules.

Examples:
- Content truth owns publication state and lifecycle correctness
- Identity truth owns account/security state
- Authorization truth owns role/permission assignment
- SEO truth owns slug uniqueness/routing truth where policy requires it
- Media truth owns attachment/ordering/primary-media rules

Derived state may exist in:
- Redis caches
- async projections
- interaction aggregates
- notification delivery views
- audit persistence views
- read-side enrichments

Derived state is allowed to lag, but must be:
- rebuildable
- observable
- recoverable
- protected by fallback rules

Correctness must not depend on derived freshness unless explicitly stated.

---

### 7) Timeout is an operational heuristic, not proof of failure

CommercialNews V1 adopts the rule:

- a timeout means the caller stopped waiting
- a timeout does **not** prove that no remote state change occurred
- a timeout does **not** prove broker publish did not happen
- a timeout does **not** prove an external side effect did not happen

Therefore:
- retries must be treated as potentially duplicate
- async handlers must be idempotent
- write flows must expose or preserve truth state that allows reconciliation after ambiguous outcomes

This rule is foundational for:
- Outbox semantics
- at-least-once delivery
- notification safety
- audit/event replay
- operational debugging

---

### 8) Wall-clock time is not authority for causality

CommercialNews V1 adopts the rule that cross-node wall-clock timestamps are useful for:
- audit and reporting
- operator investigation
- log reading
- rough chronology support

But they are not sufficient authority for:
- stale-write resolution
- cross-node causal ordering
- logical conflict resolution
- proving “which write is newer” across distributed actors

Therefore ordering-sensitive logic must rely on:
- explicit version numbers
- sequence/revision counters
- optimistic concurrency tokens
- rowversion/compare-and-set semantics
- fencing/generation-style monotonic tokens where required

This decision protects the system from:
- clock skew
- delayed messages
- stale workers
- false “last write wins” reasoning based on physical time

---

### 9) Core correctness boundary: local truth transaction + outbox

CommercialNews V1 defines the primary correctness boundary as:

- local truth transaction inside the owning module
- plus outbox intent in the same transaction when async side effects are required

A successful write means:
- truth committed
- and, when required, the outbox record committed

It does **not** mean:
- broker publish completed
- notification was delivered
- audit was already persisted
- cache invalidation already propagated
- projections already caught up

Async side effects are therefore modeled as:
- at-least-once
- laggable
- retryable
- potentially duplicating unless deduped or idempotent

---

### 10) Read model posture: fallback to truth beats stale confidence

CommercialNews V1 assumes that caches and derived reads may be stale or unavailable.

Therefore the architecture prefers:
- truth fallback
- or safe degradation
over stale confidence

Examples:
- stale slug cache must not leak unpublished content
- admin and identity self-state reads must bypass stale caches when read-your-writes is required
- public correctness must be enforced by truth boundaries, not cache freshness alone

This is a non-negotiable consequence of the chosen system model.

---

## Consequences

### Positive

- Gives CommercialNews one explicit system model for reasoning about correctness
- Aligns runtime behavior, retries, caching, and consistency expectations across modules
- Prevents accidental assumptions such as “timeout means failure” or “timestamp proves ordering”
- Clarifies that truth is owned and derived state may lag
- Supports safe architecture choices:
  - local truth transactions
  - outbox
  - idempotent consumers
  - truth-backed fallback
  - version-aware stale-write protection
- Improves observability design by separating:
  - truth-path health
  - derived-path lag
  - component liveness
  - business-flow health

### Negative / Trade-offs

- The architecture must accept ambiguity in some runtime outcomes
- Side effects are not complete at request-return time
- Derived data can lag and requires fallback logic
- Versioning/idempotency/resource-side protection increase design discipline
- Some intuitive but unsafe shortcuts are explicitly disallowed:
  - retrying blindly after timeout
  - using wall-clock timestamps as ordering truth
  - trusting local node belief as system authority
- Operators and developers must learn to reason in terms of:
  - truth vs derived
  - safety vs liveness
  - lag vs corruption
  - stale belief vs actual authority

---

## Alternatives considered

### 1) Implicit assumptions only (no explicit system model ADR)
- Pros: faster short-term documentation.
- Cons: guarantees drift, hidden assumptions spread through code and docs, hard to reason about failure semantics.

Rejected.

### 2) Synchronous model assumptions
- Pros: simpler mental model for timing.
- Cons: unrealistic for cloud/web runtime behavior; encourages unsafe timeout and ordering assumptions.

Rejected.

### 3) Crash-stop-only model
- Pros: simpler distributed theory model.
- Cons: unrealistic for containerized/service-based V1 runtime where restart/recovery is normal.

Rejected.

### 4) Full Byzantine fault-tolerant internal model
- Pros: stronger theoretical protection against arbitrary malicious internal behavior.
- Cons: excessive complexity and cost; poor fit for V1 scope; not justified for internal services under one operator.

Rejected.

### 5) Cache-first truth model for hot paths
- Pros: lower latency under ideal conditions.
- Cons: unsafe for identity, governance, and publication visibility correctness.

Rejected.

---

## Implementation notes (V1)

- `14-distributed-systems-assumptions-v1.md` is the architecture-level narrative reference for this ADR.
- Module docs must align with this ADR, especially:
  - `03-runtime-flows.md`
  - `06-idempotency-consistency.md`
  - `07-observability-slos.md`
  - `08-dependencies-and-ownership.md`
- New flows must explicitly identify:
  - truth boundary
  - derived state allowed to lag
  - timeout ambiguity handling
  - retry/idempotency expectations
  - stale-write protection mechanism if ordering matters
- Observability should separate:
  - truth write failures
  - outbox backlog
  - consumer lag/failure
  - cache fallback rates
  - business-flow symptoms

---

## Follow-ups

- Create ADR-0020: Timeout, Retry, and Failure Detection Policy (V1)
- Create ADR-0021: Clock, Time, and Ordering Policy (V1)
- Create ADR-0022: Versioning and Fencing Strategy (V1)
- Update module docs to reflect:
  - truth vs derived posture
  - timeout ambiguity
  - idempotent handler expectations
  - version-aware stale-write rejection where applicable