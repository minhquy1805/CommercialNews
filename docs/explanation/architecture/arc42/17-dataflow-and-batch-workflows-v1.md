# 17 — Dataflow and Batch Workflows (V1)

This section defines how CommercialNews V1 models **batch workflows**, **multi-stage dataflow**, and **materialization boundaries** for rebuild, reconciliation, aggregation, replay, and retention work.

It answers seven architecture-level questions:

- How should multi-stage batch workflows be structured in V1?
- What are the allowed inputs and outputs of each stage?
- Which stages produce reusable datasets versus internal temporary state?
- When should state be materialized versus recomputed?
- How should publication/cutover be handled for derived outputs?
- How should retries, reruns, duplicates, late-arriving inputs, and partial failures behave across stages?
- How does batch orchestration interact with singleton work, ownership, and safe recovery?

This section is a **system-level workflow policy** for V1.  
It does not require a dedicated big-data platform, workflow engine, or cluster scheduler.

### Related

- `02-constraints.md`
- `03-building-blocks-modularity.md`
- `04-runtime-view-v1.md`
- `08-components.md`
- `09-architecture-style.md`
- `10-system-data.md`
- `11-replication-v1.md`
- `12-partitioning-v1.md`
- `13-transactions-and-consistency-v1.md`
- `14-distributed-systems-assumptions-v1.md`
- `15-consistency-ordering-and-consensus-v1.md`
- `16-batch-processing-and-derived-data-v1.md`
- `18-stream-processing-and-derived-state-v1.md`
- `19-stream-processing-runtime-v1.md`
- ADR-0013 (Outbox & delivery semantics)
- ADR-0018 (Transaction boundaries & consistency model)
- ADR-0024 (Distributed coordination and singleton work policy)
- ADR-0025 (Batch processing and derived state policy)
- ADR-0027 (Stream processing and derived state policy)
- ADR-0028 (Consumer idempotency, replay, and rebuild policy)

---

## 17.1 Purpose

CommercialNews V1 uses dataflow-style reasoning for work that:

- is not part of the originating request
- has a bounded input
- may involve multiple transformation stages
- produces derived outputs, repair outcomes, or maintenance effects
- must remain retry-safe, observable, and operationally understandable

The purpose of this section is to standardize how such workflows are described and built.

The architecture must avoid two opposite mistakes:

1. treating every background job as an isolated one-step script with no workflow discipline  
2. over-engineering V1 into a heavyweight orchestration platform

CommercialNews V1 therefore adopts a pragmatic middle ground:

- explicit workflow stages
- explicit input/output boundaries
- selective materialization
- safe publication of derived results
- rerun-safe design
- simple orchestration unless complexity truly justifies more

This section also clarifies how **bounded workflows** relate to **continuous stream-style maintenance**:

- stream-style processing handles normal near-real-time convergence
- bounded workflows handle rebuild, replay, reconciliation, archival, and controlled cutover
- both are valid lanes for derived-state management
- neither lane changes truth ownership

---

## 17.2 Workflow model in V1

A CommercialNews batch workflow is modeled as an ordered sequence of **stages**.

A stage may perform one or more of the following:

- select bounded input
- validate or normalize input
- group/partition data
- aggregate
- join truth with derived or log data
- compare truth against derived state
- generate candidate output
- publish or cut over output
- archive, clean up, or mark completion

The architecture does **not** require every workflow to be implemented as separate deployable jobs.
A workflow may be implemented as:

- one scheduled job with internal stages
- one worker command with explicit sub-steps
- multiple scheduled jobs with explicit handoff
- replay/reconciliation routines with defined stage boundaries

What matters is that the workflow remains explainable in terms of:

- input boundary
- stage purpose
- stage output
- rerun semantics
- publication semantics
- failure semantics

### 17.2.1 Workflow-to-stream relationship

CommercialNews V1 distinguishes:

- **continuous stream-style dataflow** for near-real-time side effects and projection maintenance
- **bounded workflow dataflow** for repair, replay, rematerialization, and summary generation

A bounded workflow may consume inputs that originally came from a stream, such as:

- outbox-derived history
- raw interaction signals
- audit windows
- delivery attempt logs
- mismatch sets produced by reconciliation logic

The existence of a bounded workflow does not imply the system abandons streaming.
It means the system prefers bounded reasoning for selected recovery or summary tasks.

---

## 17.3 Standard stage types

CommercialNews V1 recognizes the following stage types.

### 17.3.1 Source selection stage
Purpose:
- define the bounded input set for the workflow

Examples:
- all events in time window W
- all published articles at snapshot T
- all entities matching a reconciliation predicate
- all pending records selected before scheduler instant S
- all repair candidates identified by mismatch scan M

This stage must make the input boundary explicit.

### 17.3.2 Preparation stage
Purpose:
- parse, normalize, validate, or pre-shape input for later stages

Examples:
- normalize timestamps
- map raw rows into canonical processing shape
- discard clearly invalid records by policy
- enrich with stable reference values needed downstream
- annotate rows with snapshot/version metadata used for replay-safe comparison

This stage should avoid introducing hidden truth writes.

### 17.3.3 Grouping / partitioning stage
Purpose:
- bring related records together by key for efficient processing

Examples:
- group by `ArticleId`
- group by `(ArticleId, Date)`
- group by `UserId`
- group by `Slug`

This reflects the system-wide rule that related data must meet for aggregation, reconciliation, or join-like logic.

### 17.3.4 Aggregation / transformation stage
Purpose:
- derive summaries, counters, rollups, rankings, or normalized outputs

Examples:
- daily totals
- trending inputs
- delivery summaries
- archival bundles
- compacted maintenance outputs

### 17.3.5 Comparison / reconciliation stage
Purpose:
- compare truth and derived state to detect drift or missing effects

Examples:
- published truth exists but derived entry missing
- aggregate differs from recomputed expectation
- lagging output violates freshness policy
- outbox-derived side effect not yet reflected downstream

### 17.3.6 Candidate output stage
Purpose:
- construct a complete or bounded candidate result to be validated before publication

Examples:
- new projection snapshot
- rebuilt derived table
- reconciliation result set
- repair command set
- archival candidate package

### 17.3.7 Publication / cutover stage
Purpose:
- make a validated candidate output active or externally visible

Examples:
- replace current derived dataset
- swap active projection version
- mark a rebuild snapshot as current
- apply an approved reconciliation result

### 17.3.8 Cleanup / retention stage
Purpose:
- remove temporary state, expire obsolete versions, or archive historical artifacts

Examples:
- delete temporary staging rows
- expire superseded output versions by retention policy
- archive aged operational data
- release temporary workflow markers

---

## 17.4 Input and output contracts

Every important workflow must define:

### 17.4.1 Input contract
At minimum:
- source dataset(s)
- ownership of the source
- bounded selection rule
- snapshot or cutoff semantics if relevant
- whether the workflow reads truth, logs, derived state, or a mixture

### 17.4.2 Output contract
At minimum:
- output type
- whether it is reusable or internal-only
- owner of the output
- whether it is publishable or purely temporary
- rerun/replacement semantics
- retention or cleanup expectations

### 17.4.3 Truth safety contract
If a workflow reads or enriches from truth, the workflow must state clearly:

- what truth data it depends on
- whether it reads current truth or a bounded snapshot
- how correctness behaves if truth changes after input selection

CommercialNews V1 prefers explicit cutoff reasoning over hidden assumptions.

### 17.4.4 Replay and duplication contract
If a workflow consumes replayable or retryable input, it must also state:

- whether duplicate input records are possible
- whether the stage deduplicates, tolerates, or rejects duplicates
- whether rerun on the same bounded input is harmless
- whether publication is versioned or otherwise protected from double-apply

This applies especially to workflows built from:

- outbox-derived work
- append-only operational logs
- retry/dead-letter recovery slices
- reconciliation-generated repair candidates

---

## 17.5 Shared datasets vs internal workflow state

This section applies ADR-0025 at workflow level.

### 17.5.1 Shared reusable workflow outputs
A workflow output should be treated as a reusable dataset when it has one or more of the following properties:

- consumed by multiple downstream workflows
- used beyond the lifetime of one run
- meaningful for inspection, reporting, or replay
- worthy of freshness/versioning semantics
- meaningful enough to justify ownership and retention policy

Such outputs require:
- explicit owner
- explicit schema/contract
- explicit publication policy
- explicit retention policy

### 17.5.2 Internal intermediate state
Internal state is appropriate when the output:

- is private to one workflow
- exists only to move to the next stage
- is cheap enough to recompute
- is not used by external readers or downstream jobs
- does not need independent retention or freshness semantics

Internal intermediate state should remain:

- private
- disposable
- clearly non-authoritative
- safe to delete after completion or failure handling

### 17.5.3 Rule: internal state must not silently escape into serving behavior
Temporary stage artifacts must not become de facto serving inputs merely because they are queryable or convenient.

If an output is used outside its workflow boundary, it must be treated as a shared dataset with explicit ownership, publication, and retention semantics.

---

## 17.6 Materialization policy by workflow stage

### 17.6.1 Materialization is allowed, not automatic
CommercialNews V1 explicitly rejects the assumption that every stage output must be durably materialized as a named reusable dataset.

Stage output should be materialized deliberately when:

- it is reused by other workflows
- it serves as a valuable checkpoint
- recomputation is expensive
- publication/cutover requires a durable candidate
- debugging or investigation needs a stable observable artifact

### 17.6.2 Recompute is acceptable for cheap internal stages
If an intermediate stage is:
- cheap to recompute
- private to a single workflow
- not reused elsewhere
- not needed for safe cutover

then recomputation is preferred over heavy persistent stage publication.

### 17.6.3 Materialization must not create hidden truth
A materialized workflow output must not become hidden truth merely because it is stable, queryable, or operationally convenient.

Truth ownership remains with the owning module.

### 17.6.4 Publication-critical stages deserve stronger treatment
Stages that directly produce:
- active serving projections
- externally consumed derived datasets
- repair decisions that will mutate important derived state
- authoritative archival packages by policy

deserve stronger materialization and validation than purely internal stages.

### 17.6.5 Workflow checkpoints are selective, not universal
Checkpointing or durable stage materialization is justified when:

- recomputation cost is operationally painful
- recovery time matters
- stage progress is valuable across failures
- output size or complexity makes repeated reruns too expensive

CommercialNews V1 does not require every workflow to expose durable checkpoint state.

---

## 17.7 Publication and cutover model

### 17.7.1 Candidate-before-publication
For correctness-sensitive derived outputs, the preferred workflow shape is:

1. select bounded input
2. compute candidate output
3. validate candidate output
4. publish / cut over only after validation passes

### 17.7.2 No partial publication as completed result
A workflow must not expose partially built output as though it were complete.

If a run fails before completion:
- the candidate remains non-active, or
- the previous active output remains authoritative for the derived use case

### 17.7.3 Replace rather than mutate, where feasible
For rebuildable derived datasets, CommercialNews prefers:

- replacement of an active derived version
- version-aware cutover
- bounded rollback posture

over uncontrolled incremental mutation of active serving state when the latter weakens rerun or rollback semantics.

### 17.7.4 Publication must preserve truth safety
A failed or delayed publication must not cause:
- exposure of drafts or unpublished content
- stale security truth being trusted
- governance truth being bypassed
- public correctness to depend solely on lagging derived state

### 17.7.5 Repair publication follows the same discipline
Repair and reconciliation outputs that modify important derived state must follow the same publication rules as rebuild outputs:

- candidate first
- validate
- publish or apply safely
- version or scope the applied result where practical
- avoid mixing incomplete repair with active completed state

---

## 17.8 Workflow rerun, replay, and repair rules

### 17.8.1 Rerun-safe by design
A workflow must be safe to rerun on the same bounded input, either because:

- repeated execution is harmless, or
- publication is versioned and explicit, or
- duplicate effect is detectable and rejectable

### 17.8.2 Repair is allowed to be simpler than perfect incremental correctness
CommercialNews V1 prefers operationally understandable repair over clever but fragile mutation logic.

When practical:
- full rebuild is preferred over difficult partial repair
- bounded replay is preferred over hidden “exactly once” assumptions
- explicit reconciliation is preferred over silent divergence

### 17.8.3 Repair outputs must also follow publication discipline
A repair/reconciliation workflow that proposes changes to derived state must follow the same discipline as other derived outputs:

- candidate first
- validate
- apply/publish safely
- never silently mix partial repair with completed state

### 17.8.4 Replay is normal, not exceptional
Workflows that operate on:
- retained outbox slices
- append-only logs
- dead-letter or retry windows
- derived mismatch sets

must assume replay is a normal operational activity, not a rare incident-only behavior.

Therefore:
- replay input must be bounded explicitly
- replay outputs must be idempotent or publication-controlled
- rerun semantics must be documented before implementation

---

## 17.9 Scheduling and orchestration assumptions

### 17.9.1 V1 does not require a heavyweight orchestrator
CommercialNews V1 does not require Airflow-like orchestration or a coordination-heavy workflow platform as a baseline dependency.

Workflows may be scheduled using:
- worker-owned scheduled execution
- platform scheduler / cron-like triggers
- bounded replay commands
- operator-triggered maintenance commands
- simple queue-driven follow-up logic

### 17.9.2 Orchestration must still be explicit
Even without a heavy orchestrator, each important workflow must define:

- trigger type
- input selection rule
- stage ordering
- completion criteria
- publication rule
- rerun rule
- failure/recovery rule

### 17.9.3 Stage coupling should be visible
If stage B depends on stage A output, that dependency must be explicit in docs and implementation shape.
CommercialNews avoids hidden cross-stage assumptions.

### 17.9.4 Workflow-to-stream handoff should be explicit
Where a bounded workflow is triggered by stream lag, replay need, or reconciliation signals, the handoff must be documented explicitly.

Examples:
- consumer lag triggers bounded rebuild
- mismatch detector produces repair candidate set
- outbox backlog window is drained by replay job
- derived serving staleness triggers rematerialization workflow

---

## 17.10 Singleton work and ownership in batch workflows

### 17.10.1 Default posture: avoid exclusive ownership if possible
CommercialNews V1 prefers:

- idempotent repeated execution
- partitioned work
- DB-enforced winner selection
- replay-safe workflows

before introducing exclusive singleton assumptions for batch work.

### 17.10.2 If a workflow truly needs singleton execution, ADR-0024 applies
If a workflow claims:
- only one current scheduler
- only one rebuild owner
- only one maintenance actor
- only one active publisher

then it must define:
- who grants ownership
- how ownership is represented
- how stale owners are rejected
- whether generation/fencing is required
- what happens under ownership ambiguity

### 17.10.3 Safe non-progress beats unsafe double-apply
If ownership confidence is lost for correctness-sensitive publication or repair work, the system must prefer:
- delay
- retry later
- explicit operator intervention
- safe rejection

over unsafe dual publication or unsafe double-apply.

---

## 17.11 Determinism expectations across stages

Batch workflows should be as deterministic as practical relative to their bounded input.

A stage should avoid unnecessary reliance on:
- mutable external state outside the input boundary
- unstable ordering
- uncontrolled randomness
- wall-clock decisions not part of the bounded contract
- silently using “latest available” reference state when snapshot or event-time semantics are required

This is especially important for:
- reconciliation results
- rebuild outputs
- publication candidate validation
- replay/repair workflows
- comparison between runs

CommercialNews does not require perfect determinism in every low-risk maintenance job, but correctness-sensitive workflows must remain explainable under rerun and recovery.

---

## 17.12 Failure model for multi-stage workflows

### 17.12.1 Stage-local failure must not corrupt truth
Failure in a batch stage must not invalidate already committed truth state.

### 17.12.2 Internal stage failure may discard candidate state
If a workflow fails before publication:
- internal temporary state may be discarded
- candidate output may remain unpublished
- rerun may restart from selected input or checkpoint by policy

### 17.12.3 Published outputs require stronger safety
If publication/cutover has already occurred, recovery must respect:
- current active output version
- rollback or replacement rules
- bounded rerun semantics
- truth-safe fallback

### 17.12.4 Observability must reveal incomplete work
A workflow that is:
- stuck
- behind
- failed
- repeatedly rerunning
- producing stale candidate output

must be visible through operational signals.

### 17.12.5 Duplicate or overlapping execution must not silently corrupt output
If the same bounded workflow input is processed twice due to:
- repeated scheduler trigger
- operator rerun
- stale-owner ambiguity
- recovery after partial completion

the design must ensure the result is either:
- harmless
- explicitly deduplicated
- versioned and publish-controlled
- or safely rejected

---

## 17.13 Standard workflow families in CommercialNews

### 17.13.1 Reading analytics workflow family
Typical shape:
1. select bounded interaction input
2. normalize / partition by key
3. aggregate
4. produce candidate summary/projection
5. publish summary
6. clean temporary state

Typical outputs:
- daily counts
- trending inputs
- bounded ranking feeds
- summary enrichments

These workflows often complement continuous interaction pipelines and act as:
- bounded recompute
- summary generation
- drift repair

### 17.13.2 SEO rebuild / reconciliation workflow family
Typical shape:
1. select bounded truth input from published content + SEO truth
2. compare truth and derived serving state where relevant
3. generate candidate rebuild or missing-entry set
4. validate candidate output
5. publish / cut over or apply repair
6. record completion / cleanup

### 17.13.3 Notification replay / cleanup workflow family
Typical shape:
1. select bounded pending/failed delivery or summary input
2. classify by retry/replay/retention policy
3. generate replay or cleanup candidates
4. apply retry-safe actions or produce bounded summaries
5. mark completion / archive

### 17.13.4 Audit archival / summarization workflow family
Typical shape:
1. select bounded historical audit window
2. normalize / partition for archival or reporting
3. generate archival artifact or summary candidate
4. validate candidate
5. publish archive / summary
6. apply cleanup by policy

### 17.13.5 Repair / reconciliation workflow family
Typical shape:
1. detect divergence between truth and derived state
2. produce bounded repair candidate set
3. validate repair candidate
4. apply repair safely
5. mark repaired scope and observe outcome

### 17.13.6 Stream-recovery workflow family
Typical shape:
1. detect backlog, gap, or stale derived output
2. select bounded replay/rebuild scope
3. regenerate candidate result from truth/logs
4. validate
5. publish/apply safely
6. record recovery outcome and cleanup temporary state

This family exists to bridge:
- continuous stream-style maintenance
- bounded recovery and rematerialization

---

## 17.14 Module application notes (V1)

### 17.14.1 Reading Experience
Reading workflows are the clearest candidates for bounded aggregation and derived serving data.
Truth-safe fallback remains mandatory when derived outputs lag.
Bounded workflows may regenerate summary or ranking inputs when stream-maintained views drift.

### 17.14.2 SEO
SEO workflows may rebuild or reconcile serving-facing derived artifacts, but slug uniqueness and visibility correctness remain truth-owned.

### 17.14.3 Interaction
Interaction naturally produces append-only input for aggregation workflows.
Counters and summaries are derived outputs and are expected to be lag-tolerant and rebuildable.
Batch is the approved repair lane when stream-style aggregation falls behind or must be recomputed.

### 17.14.4 Audit
Audit workflows may archive, summarize, or compact historical windows.
Append-only traceability remains authoritative even if summaries lag.

### 17.14.5 Notifications
Replay, cleanup, and delivery summarization are valid workflow families.
Delivery itself remains outside truth transactions and must stay idempotent.

### 17.14.6 Content / Identity / Authorization
These modules remain truth-first.
Dataflow/batch workflows may support them through summary, replay, repair, or derived acceleration, but must not replace authoritative transitions.

---

## 17.15 Workflow observability requirements

Every important workflow should expose enough signals to answer:

- what input boundary was selected?
- what stage is currently running?
- did the workflow complete?
- was candidate output published?
- how stale is the current active output?
- is repair or operator attention required?

Recommended workflow-level signals:

- run started / completed / failed count
- current stage or last completed stage
- records selected / scanned / processed / skipped / repaired
- candidate generated count
- publication success/failure count
- freshness age of active output
- rerun count / replay count
- stale-owner rejection count when ownership matters
- reconciliation mismatch count

Workflow observability should also distinguish:
- live stream lag/backlog
- bounded rebuild/replay lag
- candidate publication state
- active output freshness

---

## 17.16 V1 boundaries and V2+ hooks

### V1
CommercialNews V1 supports:
- explicit multi-stage workflow reasoning
- bounded input selection
- selective materialization
- candidate-before-publication
- rerun-safe design
- simple orchestration
- replay/rebuild/reconciliation discipline
- explicit bridging between continuous stream maintenance and bounded recovery workflows

### V2+
Later versions may add:
- richer workflow metadata
- explicit workflow catalogs
- stronger checkpoint semantics
- more formalized dependency graphs
- additional serving projection families
- more advanced scheduling/orchestration tooling
- stronger event-time-aware rebuild/recompute semantics where justified

The V1 rule remains unchanged:

- workflow stages must stay explainable
- truth remains authoritative
- derived outputs must be publish-safe
- rerun safety and observability are mandatory
- bounded workflows repair what continuous pipelines cannot safely keep healthy enough