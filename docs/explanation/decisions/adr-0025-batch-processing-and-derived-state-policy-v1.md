# ADR-0025 — Batch Processing and Derived State Policy (V1)

## Status

Accepted

## Date

2026-03-10

## Decision owners

Architecture / Platform

## Scope

System-wide (batch lane, derived state, rebuild/reconciliation posture, bounded inputs, publication rules)

## Related

- `../architecture/arc42/04-runtime-view-v1.md`
- `../architecture/arc42/08-components.md`
- `../architecture/arc42/09-architecture-style.md`
- `../architecture/arc42/10-system-data.md`
- `../architecture/arc42/13-transactions-and-consistency-v1.md`
- `../architecture/arc42/16-batch-processing-and-derived-data-v1.md`
- ADR-0013 (Outbox & delivery semantics)
- ADR-0015 (Cache policy & invalidation)
- ADR-0018 (Transaction boundaries & consistency model)
- ADR-0024 (Distributed coordination and singleton work policy)

---

## Context

CommercialNews V1 already distinguishes between:

- synchronous truth writes for core user/admin flows
- asynchronous side effects for audit, notifications, aggregation, invalidation
- derived stores that may lag, but must remain observable and recoverable

However, without one explicit system-wide decision for batch processing and derived state, the architecture risks drifting in several ways:

- derived datasets quietly becoming de facto truth without explicit approval
- rebuild/reconciliation logic being implemented inconsistently across modules
- long-running jobs widening truth boundaries or blocking core flows
- every intermediate processing stage being materialized by habit rather than by policy
- partial batch outputs being exposed as if they were complete and trustworthy
- replay/rebuild/reconciliation jobs using unclear input boundaries and unsafe rerun semantics
- teams treating batch as “background implementation detail” instead of a first-class processing lane

CommercialNews needs one accepted decision that states:

- what batch processing means in V1
- which outputs count as derived state
- how batch relates to truth writes and async side effects
- when materialization is justified
- how outputs are published safely
- how recomputation, rerun, and repair are expected to work

---

## Decision

### 1) Batch processing is a first-class processing lane in V1

CommercialNews V1 formally recognizes three processing lanes:

- synchronous request/response for truth writes and truth-sensitive reads
- asynchronous event-driven side effects for post-commit processing
- batch / rebuild / reconciliation for bounded-input offline or scheduled processing

Batch is therefore an architecture concern, not merely an implementation detail.

### 2) Batch operates on bounded inputs and produces derived outputs

A CommercialNews batch job **MUST** define a bounded input boundary explicitly.

Allowed examples:

- a time-bounded slice of append-only events
- a snapshot of truth data at a known time
- a selected backlog window
- a bounded set of entities to rebuild, repair, or reconcile

A batch output is treated as **derived state** by default.

Typical outputs include:

- aggregates
- projections
- derived indexes
- archival datasets
- reconciliation reports
- rebuild artifacts
- serving accelerators

Batch does not redefine truth ownership.

### 3) Truth remains authoritative; derived state may lag

The architecture formally distinguishes:

#### Truth

Authoritative, synchronous, owning-module state.

#### Derived state

Rebuildable, observable, lag-tolerant outputs used for:

- acceleration
- reporting
- enrichment
- maintenance
- recoverability

Derived state may be:

- stale
- missing
- recomputed
- replaced
- invalidated
- rebuilt after failure

Derived state must not quietly become correctness authority for:

- publication visibility
- identity/security state
- governance state
- owning-module invariants
- other truth-sensitive decisions

Any future elevation of a projection or derived dataset into a primary serving authority must be made explicit by separate docs/ADR changes.

### 4) Batch must not block core truth flows

CommercialNews V1 does not define business success as “batch completed successfully.”

Core flows such as:

- publish / unpublish
- register / verify / reset
- role / permission changes
- public truth-safe reading

must not wait for batch completion.

Batch exists to support:

- derived serving data
- summarization
- replay
- repair
- reconciliation
- cleanup / archival
- rebuild after lag or outage

It must not be inserted into the immediate success condition of truth flows.

### 5) Batch must not widen truth transaction boundaries

Batch jobs are not allowed to stretch or redefine the truth transaction model already established by ADR-0018.

CommercialNews V1 continues to use:

- local truth transaction
- outbox in the same transaction when async side effects are needed
- post-commit async propagation
- replay/retry/idempotent consumers

Batch jobs may read truth and produce derived state, but they must not:

- hold long-running truth transactions
- introduce cross-system atomic commit requirements
- require broker/cache/email/external systems to finish before truth success
- redefine module ownership because data is physically reachable

### 6) Shared reusable datasets and internal intermediate state are distinct

CommercialNews distinguishes between:

#### A) Shared reusable datasets

Outputs with independent value that may be consumed by multiple workflows or operators.

These require:

- clear ownership
- clear contract/schema
- explicit retention/versioning stance
- observability/freshness signals where relevant

#### B) Internal intermediate state

Workflow-private artifacts used only to move processing from one stage to the next.

These do not automatically justify:

- long-lived publication
- durable replication-level treatment
- broad reuse assumptions
- system-wide visibility

Not every internal stage artifact deserves promotion to a named reusable dataset.

### 7) Materialization is selective and policy-driven

CommercialNews V1 does not require materializing every intermediate processing step.

Materialization is justified when one or more of the following applies:

- the output is reused by multiple downstream workflows
- the output serves as an operationally valuable checkpoint
- recomputation cost is high enough to justify storing it
- publication/cutover requires a durable candidate artifact
- observability/debugging benefits are significant

When an intermediate result is private to one workflow, cheap enough to recompute, and not reused elsewhere, lighter temporary state is preferred.

This avoids accidental over-engineering and unnecessary operational cost in V1.

### 8) Batch outputs should be published safely, not leaked incrementally

For correctness-sensitive derived outputs, CommercialNews prefers the following publication model:

1. build candidate output
2. validate candidate output
3. publish / cut over after successful completion

The architecture does not treat a partially built batch result as a valid finished dataset.

Where practical, replacement of a completed derived output is preferred over uncontrolled incremental mutation of active serving state.

This improves:

- rerun safety
- rollback posture
- reasoning about success/failure
- clear visibility of what output version is active

### 9) Recompute is an accepted recovery mechanism

Because derived state is rebuildable by policy, recomputation is a normal and accepted recovery strategy in V1.

CommercialNews prefers:

- full rebuild over overly clever incremental repair when rebuild cost is acceptable
- replay-safe repair over brittle “exactly once” assumptions
- simpler rebuild logic over coordination-heavy state mutation where feasible

Checkpointing is allowed, but selective.  
It is used when:

- recovery time matters
- recomputation is expensive
- the intermediate state is meaningful and reusable
- operational recovery needs a durable handoff point

### 10) Batch jobs must be rerun-safe

Batch, rebuild, replay, and reconciliation jobs must be designed so that rerunning them on the same bounded input is either:

- harmless, or
- explicitly controlled and validated

This means V1 requires:

- replay-safe behavior
- avoidance of hidden side effects
- explicit publication/cutover discipline
- no dependence on “this job will only ever run once”

Rerun safety is treated as a policy-level requirement, not a nice-to-have.

### 11) Determinism is preferred where correctness-sensitive

CommercialNews expects batch workflows to be as deterministic as practical for a given bounded input.

The system should avoid unnecessary dependence on:

- unstable iteration order
- uncontrolled random behavior
- mutable external state outside the declared input boundary
- wall-clock decisions made during processing when not part of the input contract

Perfect determinism is not mandatory for every low-risk derived output, but non-deterministic behavior must not undermine:

- publication confidence
- reconciliation confidence
- replay safety
- correctness-sensitive rebuild flows

### 12) Batch must follow the singleton/ownership policy from ADR-0024

If a batch or rebuild workflow claims exclusive ownership or singleton execution semantics, then:

- ownership must be explicit
- stale owners must be rejectable
- naive in-memory leadership is forbidden
- timeout-only ownership transfer is forbidden
- safe non-progress is preferred over unsafe double-apply

CommercialNews V1 prefers idempotent and partitioned designs before exclusive singleton ownership.

### 13) Batch observability is mandatory

Batch/rebuild/reconciliation workflows must expose enough operational signals to answer:

- is the workflow running?
- is it succeeding?
- how far behind is it?
- is output fresh enough for its purpose?
- is repair or rerun required?

At minimum, relevant workflows should expose some combination of:

- run success/failure count
- run duration
- records scanned / processed / skipped / repaired
- lag or freshness age
- backlog age/count
- stale-output detection count
- checkpoint age, where checkpoints exist
- duplicate-run or stale-owner rejection signals when coordination matters

---

## Decision summary

CommercialNews V1 adopts the following batch and derived-state posture:

- batch is a first-class processing lane
- batch uses bounded inputs
- batch outputs are derived state by default
- truth remains authoritative
- core truth flows do not wait for batch completion
- batch does not widen truth transaction boundaries
- reusable datasets and internal intermediate state are distinct
- materialization is selective, not automatic
- batch outputs should be published safely after completion
- recomputation is an accepted recovery strategy
- rerun safety is mandatory
- determinism is preferred where correctness-sensitive
- singleton/exclusive batch ownership must follow ADR-0024
- batch observability is mandatory

---

## Consequences

### Positive

- Clarifies that batch/rebuild/reconciliation are architectural concerns, not ad hoc background code
- Protects truth ownership from accidental drift into derived stores
- Gives a consistent posture for rebuilds, repair, replay, and archival workflows
- Prevents accidental over-materialization of internal workflow state
- Improves rerun/recovery safety for projections, aggregates, and derived outputs
- Supports future read-model evolution without prematurely treating projections as truth
- Keeps V1 aligned with small-team operational simplicity

### Negative / Trade-offs

- Some derived workflows may temporarily remain simpler but less optimized until stronger orchestration/checkpointing is justified
- Teams must think explicitly about input boundaries and output publication instead of treating jobs as “best effort scripts”
- Derived datasets may lag and require fallback or reconciliation logic
- A few workflows may require additional design effort to become rerun-safe and deterministic enough
- More explicit distinction between truth and derived state may slow some shortcut implementations

---

## Alternatives considered

### 1) Treat batch as just another async implementation detail

**Pros:** fewer explicit docs and fewer policy rules.

**Cons:** encourages inconsistent rebuild/replay/materialization behavior; hides architectural significance of derived state.

**Rejected.**

### 2) Let derived outputs evolve organically without system-wide policy

**Pros:** short-term flexibility.

**Cons:** invites drift, hidden truth, unsafe publication, and inconsistent recovery behavior.

**Rejected.**

### 3) Materialize every stage of every workflow

**Pros:** simple recovery model in some cases.

**Cons:** unnecessary I/O, unnecessary operational weight, poor fit for V1 simplicity, promotes internal artifacts to false first-class data products.

**Rejected.**

### 4) Require sophisticated orchestration and checkpointing for all batch work in V1

**Pros:** future-ready in theory.

**Cons:** over-engineered for V1 scale and team size; adds complexity before justified by real workloads.

**Rejected.**

### 5) Allow batch outputs to mutate live serving state incrementally without publication discipline

**Pros:** simpler to implement in some cases.

**Cons:** risks exposing partial or inconsistent derived results; weak rollback and rerun posture.

**Rejected.**

---

## Implementation notes (V1)

- `16-batch-processing-and-derived-data-v1.md` is the architecture-level narrative reference for this ADR.
- Component docs should continue treating batch/rebuild/reconciliation work as primarily Worker-owned or Worker-assisted unless explicitly justified otherwise.
- Module docs should identify:
  - which datasets are truth
  - which datasets are derived
  - which jobs are aggregation / rebuild / reconciliation / archival
  - whether a batch output is reusable or internal-only

Reviews should reject designs that:

- treat cache/projection state as hidden truth
- expose partial batch output as complete
- omit input boundary definition
- rely on unsafe singleton assumptions
- cannot explain rerun safety

---

## Recommended application by area (V1)

### Reading Experience

- treat counters, trending inputs, and summary enrichments as derived
- preserve truth-safe fallback when derived outputs lag

### SEO

- allow rebuild/reconciliation of derived SEO-serving artifacts
- keep slug uniqueness and visibility correctness at truth boundaries

### Interaction

- treat raw activity and counters/aggregates as natural batch/async inputs and outputs
- prefer rebuildable summaries over fragile one-shot counters

### Audit

- allow archival, summarization, and reporting jobs
- preserve append-only traceability of authoritative audit records

### Notifications

- allow replay, cleanup, and summary jobs
- keep delivery itself outside truth transactions

### Content / Identity / Authorization

- remain truth-first
- use batch only for support, summary, repair, or derived acceleration

---

## Follow-ups

- add this ADR to `docs/explanation/decisions/README.md`
- add section 16 to `docs/explanation/architecture/arc42/00-index.md`
- create or update `17-dataflow-and-batch-workflows-v1.md` in Phase 2
- update relevant module docs in Phase 4:
  - `03-runtime-flows.md`
  - `06-idempotency-consistency.md`
  - `08-dependencies-and-ownership.md`