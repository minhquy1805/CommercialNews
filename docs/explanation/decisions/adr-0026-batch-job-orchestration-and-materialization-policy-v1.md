# ADR-0026 — Batch Job Orchestration and Materialization Policy (V1)

**Status:** Accepted  
**Date:** 2026-03-10  
**Decision owners:** Architecture / Platform  
**Scope:** System-wide (batch workflow structure, orchestration posture, stage boundaries, materialization, publication/cutover, rerun/recovery semantics)  
**Related:**
- `../architecture/arc42/04-runtime-view-v1.md`
- `../architecture/arc42/08-components.md`
- `../architecture/arc42/09-architecture-style.md`
- `../architecture/arc42/10-system-data.md`
- `../architecture/arc42/13-transactions-and-consistency-v1.md`
- `../architecture/arc42/16-batch-processing-and-derived-data-v1.md`
- `../architecture/arc42/17-dataflow-and-batch-workflows-v1.md`
- ADR-0013 (Outbox & delivery semantics)
- ADR-0018 (Transaction boundaries & consistency model)
- ADR-0024 (Distributed coordination and singleton work policy)
- ADR-0025 (Batch processing and derived state policy)

---

## Context

CommercialNews V1 already accepts:

- local truth transactions for authoritative state
- asynchronous side effects via outbox + broker + consumers
- derived state that may lag but must be observable and rebuildable
- replay/rebuild/reconciliation as valid operational strategies

As the system adds more bounded batch workflows, another architecture risk appears:

- jobs may be designed as opaque scripts with no clear stage boundaries
- stage outputs may be materialized inconsistently or by habit
- temporary intermediate state may be promoted accidentally into quasi-public datasets
- partially built output may leak into active serving state
- rerun/recovery behavior may vary arbitrarily between jobs
- hidden dependencies between stages may make failures hard to reason about
- singleton ownership may be assumed informally for rebuild/maintenance work
- V1 may drift toward either under-disciplined batch jobs or premature orchestration complexity

CommercialNews needs one explicit decision that states:

- how batch workflows should be structured
- how stage input/output boundaries should be described
- when to materialize intermediate state
- when recomputation is preferable
- how publication/cutover should work
- how rerun and failure recovery should behave
- what orchestration posture is appropriate in V1

---

## Decision

### 1) Batch workflows must be modeled as explicit stage sequences
CommercialNews V1 does not treat important batch work as a black-box “one-step background script.”

Each important batch workflow must be explainable as an ordered sequence of stages such as:

- bounded input selection
- preparation / normalization
- grouping / partitioning
- aggregation / transformation
- comparison / reconciliation
- candidate output generation
- publication / cutover
- cleanup / retention

Not every workflow requires every stage.
However, every workflow must make its stage boundaries understandable enough for:

- review
- rerun reasoning
- failure reasoning
- publication safety
- observability

---

### 2) Every important workflow must define input and output contracts
For each important workflow, the architecture must define:

#### Input contract
- source dataset(s)
- ownership of source(s)
- bounded selection rule
- snapshot/cutoff semantics when relevant
- whether source is truth, log, or derived state

#### Output contract
- output type
- owner
- reusable dataset or internal-only artifact
- publication expectations
- retention/cleanup expectations
- rerun/replacement expectations

CommercialNews V1 does not accept “job behavior is obvious from code” as sufficient architecture documentation for important workflows.

---

### 3) Reusable outputs and internal intermediate state must remain distinct
CommercialNews V1 adopts a strict distinction between:

#### A) Shared reusable outputs
Outputs that have independent value, downstream consumers, or explicit freshness/publication meaning.

These require:
- clear ownership
- explicit contract/schema
- explicit publication semantics
- retention/versioning stance where relevant

#### B) Internal intermediate state
Stage-local artifacts used only to advance one workflow.

These do not automatically justify:
- long-lived retention
- broad visibility
- publication semantics
- durable “data product” treatment

Internal workflow state must remain private unless explicitly promoted by design.

---

### 4) Materialization is selective, not mandatory at every stage
CommercialNews V1 explicitly rejects the rule:
> “every workflow stage must materialize a durable named dataset.”

Materialization is justified when one or more of the following is true:

- the output will be reused by multiple downstream workflows
- the output is a valuable checkpoint for recovery
- recomputation is expensive enough to justify storing it
- candidate publication/cutover requires a durable artifact
- operational inspection or debugging benefits justify it

If none of the above applies, lighter internal stage state and recomputation are preferred.

---

### 5) Recompute is preferred over unnecessary durable intermediate state
CommercialNews V1 accepts recomputation as a first-class recovery strategy for workflow stages that are:

- cheap enough to rerun
- private to one workflow
- not reused elsewhere
- not required as safe publication checkpoints

The architecture therefore prefers:

- simple rerun of cheap internal stages
- selective checkpointing for expensive stages
- full rebuild over fragile incremental mutation when rebuild cost is acceptable

This supports V1 simplicity and avoids over-engineered orchestration.

---

### 6) Publication must follow candidate-before-cutover discipline
For correctness-sensitive derived outputs, CommercialNews V1 adopts the following rule:

1. compute candidate output
2. validate candidate output
3. publish / cut over only after validation succeeds

The architecture does not allow partially built output to be treated as a completed active dataset.

If a workflow fails before cutover:
- candidate output remains inactive, or
- previous active output remains authoritative for that derived use case

Where feasible, replacing a completed derived output is preferred over uncontrolled mutation of already-active serving state.

---

### 7) Workflow rerun safety is mandatory
Every important workflow must be safe to rerun on the same bounded input.

This may be achieved by one or more of:

- harmless repeated computation
- explicit versioned publication
- replace-not-mutate semantics
- duplicate-apply rejection
- bounded replay with dedupe-aware application

CommercialNews V1 does not allow important workflows to depend on:
- “this job only ever runs once”
- “the scheduler never retries”
- “two workers will never overlap”
- “partial progress will always be cleaned up manually”

---

### 8) Workflow determinism is preferred where output matters
For important rebuild, reconciliation, replay, and publication workflows, CommercialNews V1 expects stages to be as deterministic as practical relative to their bounded input.

Stages should avoid unnecessary dependence on:

- mutable external state outside the declared input boundary
- unstable iteration order
- uncontrolled random behavior
- wall-clock decisions not included in the stage contract

Perfect determinism is not mandatory for every low-risk maintenance workflow.
However, correctness-sensitive workflows must remain explainable under:
- rerun
- comparison
- rollback/cutover reasoning
- replay/recovery

---

### 9) V1 orchestration remains simple unless complexity is justified
CommercialNews V1 does **not** require a heavyweight orchestration engine as a baseline dependency.

Allowed orchestration approaches include:

- worker-scheduled jobs
- platform scheduler / cron-triggered execution
- explicit maintenance commands
- replay/reconciliation commands
- simple chained stage execution inside one bounded workflow

However, orchestration must still be explicit enough to define:

- trigger
- input boundary
- stage ordering
- completion criteria
- publication rule
- rerun rule
- failure/recovery rule

CommercialNews rejects both extremes:

- under-specified “script-like” workflows
- premature workflow-platform complexity without real need

---

### 10) Stage dependencies must be explicit
If stage B depends on stage A output, the dependency must be explicit in both design and implementation.

CommercialNews V1 does not allow hidden stage coupling such as:

- stage B assuming stage A ran “recently enough” without contract
- stage B reading undocumented temporary output
- stage B relying on side effects of stage A that are not part of its output contract

Stage dependencies must remain reviewable and observable.

---

### 11) Singleton/exclusive workflow ownership follows ADR-0024
CommercialNews V1 prefers:
- idempotent repeated execution
- partitioned work
- DB-enforced winners
- replay-safe workflows

before introducing exclusive workflow ownership.

If a workflow truly requires singleton execution or one current owner, it must define:

- who grants ownership
- where ownership is recorded
- how stale owners are rejected
- whether generation/fencing is required
- what happens if ownership confidence is lost

Naive assumptions such as:
- in-memory `isLeader`
- timeout-only transfer
- local self-belief of ownership

are forbidden for correctness-sensitive publication or repair work.

---

### 12) Safe non-progress is preferred over unsafe dual publication or dual repair
If orchestration confidence is lost for a correctness-sensitive workflow, CommercialNews V1 prefers:

- delayed completion
- explicit retry later
- operator intervention
- rejection of stale runner actions
- retaining previous active output

over:

- two concurrent publishers
- two repair actors mutating derived state blindly
- partial publication under ambiguity
- stale owner continuing after transfer

This aligns with system-wide safety posture:
temporary degraded progress is acceptable; silent corruption is not.

---

### 13) Workflow observability is mandatory
Every important workflow must expose enough signals to answer:

- what bounded input was selected?
- what stage is currently running?
- did candidate output get produced?
- was cutover completed?
- is active output fresh enough?
- is rerun, repair, or operator attention required?

Recommended signals include:

- run started/completed/failed count
- current stage / last completed stage
- records selected / processed / skipped / repaired
- candidate output generated count
- publication success/failure count
- freshness age of active output
- rerun/replay count
- checkpoint age, when checkpoints exist
- stale-owner rejection / duplicate-run detection when ownership matters

---

## Decision summary

CommercialNews V1 adopts the following batch orchestration and materialization policy:

- important workflows must be modeled as explicit stage sequences
- every important workflow must define bounded input and output contracts
- reusable outputs and internal intermediate state remain distinct
- materialization is selective, not automatic
- recomputation is preferred when cheaper and simpler than durable stage storage
- correctness-sensitive publication follows candidate-before-cutover discipline
- rerun safety is mandatory
- determinism is preferred where output trust matters
- orchestration remains simple in V1 unless complexity is justified
- stage dependencies must be explicit
- exclusive ownership follows ADR-0024
- safe non-progress beats unsafe dual publication or repair
- workflow observability is mandatory

---

## Consequences

### Positive
- Makes batch workflows reviewable and explainable rather than opaque scripts
- Prevents accidental promotion of temporary workflow state into pseudo-public datasets
- Improves rerun, recovery, and publication safety for rebuild/reconciliation workflows
- Avoids unnecessary durable materialization and operational cost
- Keeps V1 orchestration simple while still disciplined
- Aligns workflow ownership and singleton concerns with the broader coordination policy
- Improves confidence in cutover of correctness-sensitive derived outputs

### Negative / Trade-offs
- Teams must document stage boundaries and contracts more explicitly
- Some quick “just run a script” approaches become non-compliant for important workflows
- Validation and cutover steps may add implementation work for derived outputs
- A few workflows may need stronger output versioning/checkpointing than initially expected
- More explicit stage thinking may initially feel heavier than ad hoc job design

---

## Alternatives considered

### 1) Treat every batch job as a one-step opaque command
- Pros: simplest to start.
- Cons: weak rerun reasoning, weak recovery model, poor publication discipline, hidden dependencies.

Rejected.

### 2) Materialize every stage output durably
- Pros: simpler recovery story in some cases.
- Cons: too much I/O, too much operational weight, too many quasi-public artifacts, poor fit for V1 simplicity.

Rejected.

### 3) Require a heavyweight workflow orchestrator in V1 baseline
- Pros: strong explicit workflow modeling in theory.
- Cons: over-engineered for current V1 scale and team size; introduces complexity before justified.

Rejected.

### 4) Allow partially built outputs to become visible incrementally
- Pros: simpler for some incremental implementations.
- Cons: weak trust model, weak rollback posture, risk of exposing incomplete derived state.

Rejected.

### 5) Assume singleton execution without explicit ownership semantics
- Pros: appears easy for maintenance/rebuild jobs.
- Cons: unsafe under retry, overlap, pause, restart, and ownership ambiguity.

Rejected.

---

## Implementation notes (V1)

- `17-dataflow-and-batch-workflows-v1.md` is the architecture-level narrative reference for this ADR.
- `16-batch-processing-and-derived-data-v1.md` remains the system-wide policy reference for batch and derived-state posture.
- Reviews should reject important workflow designs that:
  - omit bounded input definition
  - omit stage boundaries
  - expose partial candidate output as active
  - treat internal temporary state as implicit public dataset
  - cannot explain rerun semantics
  - rely on naive singleton assumptions
- Worker-owned batch/rebuild/reconciliation logic should remain aligned with:
  - ADR-0018 for transaction boundaries
  - ADR-0024 for singleton/ownership semantics
  - ADR-0025 for batch/derived-state posture

---

## Recommended application by workflow family (V1)

### Reading analytics workflows
- use explicit input windows
- aggregate into candidate summaries
- publish only validated derived outputs
- keep truth-safe fallback available

### SEO rebuild/reconciliation workflows
- compare bounded truth against derived serving artifacts
- generate candidate rebuild or repair set
- publish or apply repair only after validation

### Notification replay/cleanup workflows
- keep replay/cleanup stages explicit
- avoid hidden mutation of active delivery state
- remain rerun-safe and dedupe-aware

### Audit archival/summarization workflows
- stage bounded historical windows
- produce archival/summarization candidates
- publish/archive with explicit completion rules

### Repair/reconciliation workflows
- make mismatch detection explicit
- produce candidate repair sets
- apply repair safely, not as uncontrolled immediate mutation

---

## Follow-ups

- add this ADR to `docs/explanation/decisions/README.md`
- add section 17 to `docs/explanation/architecture/arc42/00-index.md`
- use Phase 3 to update core arc42 docs with:
  - batch lane visibility
  - workflow/materialization references
  - observability/governance hooks
- use Phase 4 to update module docs where workflow families are relevant:
  - `03-runtime-flows.md`
  - `06-idempotency-consistency.md`
  - `08-dependencies-and-ownership.md`