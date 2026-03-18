## 16) Batch Processing & Derived Data (V1)

This section defines how CommercialNews V1 treats batch processing, derived state, and rebuild/reconciliation workflows.

It answers six architecture-level questions:

* What kinds of work belong to the batch lane?
* What are the allowed inputs and outputs of batch jobs?
* How does batch relate to synchronous truth writes, asynchronous side effects, and stream-style derived-state maintenance?
* Which derived datasets are shared/reusable versus internal temporary state?
* When should intermediate state be materialized versus recomputed?
* How must batch jobs behave under retry, lag, duplicate input, and partial failure?

This section is a **system-level policy for V1**.
It is **not** a per-job implementation guide and it does not require a separate big-data platform.

### Related

* `02-constraints.md`
* `03-building-blocks-modularity.md`
* `04-runtime-view-v1.md`
* `08-components.md`
* `09-architecture-style.md`
* `10-system-data.md`
* `11-replication-v1.md`
* `12-partitioning-v1.md`
* `13-transactions-and-consistency-v1.md`
* `14-distributed-systems-assumptions-v1.md`
* `15-consistency-ordering-and-consensus-v1.md`
* `18-stream-processing-and-derived-state-v1.md`
* `19-stream-processing-runtime-v1.md`
* ADR-0013 (Outbox & delivery semantics)
* ADR-0015 (Cache policy & invalidation)
* ADR-0018 (Transaction boundaries & consistency model)
* ADR-0024 (Distributed coordination and singleton work policy)
* ADR-0027 (Stream processing and derived state policy)
* ADR-0028 (Consumer idempotency, replay, and rebuild policy)

---

### 16.1 Purpose

CommercialNews V1 distinguishes between three processing lanes:

#### A) Synchronous request/response

* truth writes
* truth-based reads
* immediate user/admin outcomes

#### B) Asynchronous event-driven side effects and stream-style derived-state maintenance

* audit ingestion
* notification delivery
* cache invalidation / reactive updates
* projection updates
* interaction ingestion and selected near-real-time derived-state maintenance

#### C) Batch / rebuild / reconciliation

* bounded-input processing
* derived dataset generation
* repair, cleanup, archival, replay, and recomputation

This section formalizes the third lane and its boundary with the second lane.

The goal is to ensure that batch processing:

* does not block core flows
* does not redefine truth ownership
* produces rebuildable and observable derived state
* remains feasible for V1 operations and team size
* complements stream-style propagation instead of competing with it
* creates a clean path toward richer projections in V2+

---

### 16.2 Batch in the CommercialNews processing model

In CommercialNews V1, **batch processing** means:

* processing a **bounded input**
* outside the originating user request
* to produce **derived output**
* without widening the truth transaction boundary

#### Typical bounded inputs include

* a time-bounded slice of append-only logs
* a bounded replay set from retained outbox or operational history
* a snapshot of truth data
* a queue/backlog slice selected by policy
* a known set of entities requiring rebuild or repair

#### Typical outputs include

* aggregates
* projections
* derived indexes
* archival datasets
* reconciliation reports
* rebuild artifacts
* candidate serving datasets awaiting validation/cutover

Batch is therefore distinct from:

* **A) synchronous truth writes** (authoritative business success at module boundary)
* **B) async side effects and continuous stream-style maintenance** (react to committed truth changes via outbox/broker/consumers)
* **C) public request handling** (low latency, safe correctness under lag)

Batch is the preferred lane when **bounded recompute or bounded repair** is simpler and safer than fragile always-incremental maintenance.

---

### 16.3 Core policy

#### 16.3.1 Batch is first-class, but not truth-owning

Batch is a first-class processing lane in CommercialNews V1, but batch jobs do not become owners of business truth by accident.

Batch **may**:

* read truth
* read append-only logs
* read existing derived datasets
* create new derived datasets
* repair or rebuild derived state
* archive or compact historical data by policy
* reconcile drift between truth and derived state
* regenerate outputs that are too stale, incomplete, or expensive to repair incrementally

Batch **must not**:

* redefine module truth ownership
* treat derived stores as the source of correctness
* widen a truth transaction into a long-running workflow
* make core success depend on a downstream batch result
* silently convert a candidate derived output into active truth-backed serving state without explicit publication/cutover rules

#### 16.3.2 Truth remains authoritative

Truth stores remain the source of record for:

* publication visibility
* identity/security state
* governance state
* uniqueness and transition invariants
* other owning-module correctness rules

Batch outputs may lag, be missing, be replaced, or be rebuilt.

#### 16.3.3 Batch must use bounded input

A CommercialNews batch job must define its input boundary explicitly.

Examples:

* “all NewsView events from day D”
* “all published articles as of snapshot time T”
* “all pending records selected before scheduler instant S”
* “all outbox or audit records in retention window W”
* “all entities in candidate mismatch set M produced by reconciliation run R”

A job without an explicit input boundary is not treated as batch-complete architecture.

#### 16.3.4 Batch output is derived state

The standard result of batch work in CommercialNews is derived state, not primary truth.

Examples:

* daily article view totals
* trending projections
* reconciliation reports
* rebuildable SEO/search artifacts
* archived audit partitions
* notification or delivery summaries
* regenerated public-serving or admin-facing derived views

#### 16.3.5 Core flows must not depend on batch completion

Publish, unpublish, register, verify, password reset, role changes, and public reads must not wait for batch completion.

Batch improves:

* efficiency
* reporting
* serving acceleration
* recoverability
* maintenance
* bounded repair after async lag or failure

Batch must not redefine immediate correctness of truth flows.

#### 16.3.6 Batch is the repair lane for stream-derived systems

Where continuous stream-style maintenance falls behind, duplicates, misses work, or becomes too expensive to repair incrementally, batch is the approved recovery lane.

This means:

* stream-style processing handles normal near-real-time convergence
* batch handles bounded replay, recompute, reconciliation, and recovery
* the two lanes are complementary by design

---

### 16.4 Batch job families in V1

CommercialNews V1 recognizes the following job families.

#### 16.4.1 Aggregation jobs

**Purpose**

* compress append-only activity into bounded summaries

**Examples**

* daily view counts
* trending input preparation
* notification delivery summaries
* audit volume summaries

#### 16.4.2 Rebuild jobs

**Purpose**

* regenerate a derived dataset from truth and/or logs

**Examples**

* rebuild a projection
* rebuild a cache-backed routing dataset
* regenerate derived ranking inputs
* regenerate sitemap-like outputs (if introduced)

#### 16.4.3 Reconciliation jobs

**Purpose**

* detect and optionally repair drift between truth and derived state

**Examples**

* published article exists but derived artifact is missing
* cache/projection is stale or incomplete
* delayed side effects created backlog beyond policy
* counters or summaries differ from recomputed expectation

#### 16.4.4 Repair / replay jobs

**Purpose**

* safely reprocess missed or failed work

**Examples**

* replay lagging outbox-consumer effects
* refill missing derived entries
* repair failed aggregation windows
* recover after consumer outage

#### 16.4.5 Retention / archival / cleanup jobs

**Purpose**

* apply retention policy to append-only or operational datasets

**Examples**

* archive audit windows
* purge expired delivery or reset artifacts by policy
* compact or clean temporary processing state
* manage historical partitions

---

### 16.5 Truth stores, logs, and derived state

CommercialNews V1 distinguishes three important categories.

#### 16.5.1 Truth stores

These are authoritative and synchronous at the owning boundary.

Examples:

* content lifecycle state
* identity security/account state
* authorization truth
* SEO truth owned by SEO
* media truth owned by Media

#### 16.5.2 Append-only logs / operational history

These are primarily used for:

* traceability
* replay
* aggregation
* retention-managed history
* bounded rebuild/reconciliation inputs

Examples:

* audit log
* login history
* raw interaction/view signals
* outbox as replication log
* delivery attempt history where policy requires it

#### 16.5.3 Derived state

These exist to accelerate reads, summarize behavior, or expose non-authoritative views.

Examples:

* aggregates
* projections
* cache entries
* ranking/trending inputs
* reconciliation snapshots
* generated serving datasets
* derived search or SEO-serving artifacts

Derived state must be:

* observable
* rebuildable
* safe to lag
* safe to replace
* non-authoritative unless a future ADR explicitly changes that posture

---

### 16.6 Shared datasets vs internal intermediate state

CommercialNews V1 distinguishes between two kinds of batch outputs.

#### 16.6.1 Shared reusable datasets

These are outputs that have independent value and may be consumed by more than one workflow or operator.

Examples:

* daily article aggregates
* archived audit partitions
* a reusable serving projection
* a bounded reconciliation result set
* a reusable derived snapshot for multiple downstream workflows

These should have:

* clear owner
* clear schema/contract
* clear retention/versioning policy
* observability and freshness signals where relevant

#### 16.6.2 Internal intermediate state

These are stage-local artifacts used only to move one workflow to its next step.

Examples:

* temporary staging rows
* intermediate grouping results
* internal transformation snapshots
* ephemeral work partitions
* stage-local candidate maps or scratch outputs

These do not automatically deserve the same durability, visibility, or governance as shared datasets.

**Rule:** not every internal stage artifact should be promoted to a named long-lived dataset.

---

### 16.7 Materialization policy

#### 16.7.1 Materialize deliberately, not by habit

CommercialNews V1 does not assume every processing stage must write a durable, reusable intermediate dataset.

Materialization is justified when one or more of the following is true:

* the output is reused by multiple downstream consumers
* the output serves as a valuable checkpoint
* recomputation cost is high enough to justify storage
* operational inspection/debuggability requires it
* publication/cutover needs a durable candidate artifact
* a rebuild/reconciliation workflow needs a stable, reviewable candidate set

#### 16.7.2 Prefer lighter internal state for one-workflow-only stages

When an intermediate artifact is:

* private to one workflow
* cheap enough to recompute
* not reused elsewhere
* not needed as a checkpoint

…then lighter temporary state is preferred over heavyweight durable publication.

#### 16.7.3 Materialization must not create hidden truth

A materialized batch output must not quietly become the de facto source of truth for:

* publication visibility
* security state
* governance state
* other truth-sensitive decisions

If a future projection becomes a primary serving source, that must be formalized explicitly in later docs/ADRs.

---

### 16.8 Output publication and cutover rules

#### 16.8.1 Batch output should be published as derived state, not leaked incrementally

For correctness-sensitive derived outputs, CommercialNews prefers:

1. build candidate output
2. validate candidate output
3. publish / cut over after successful completion

This is preferred over exposing partially built output during processing.

#### 16.8.2 Partial output must not be treated as complete

A failed or incomplete batch run must not silently present itself as a complete serving dataset.

#### 16.8.3 Replaceable outputs are preferred

For rebuildable derived state, replacement of a complete derived dataset is preferred over ad hoc uncontrolled mutation where feasible.

This improves:

* rollback posture
* rerun safety
* reasoning about success/failure
* observability of which version is active

#### 16.8.4 Publication must preserve truth safety

Even when derived output publication lags or fails:

* drafts/unpublished content must remain hidden
* security-sensitive truth must remain correct
* governance reads must remain authoritative
* truth fallback must remain available where required

---

### 16.9 Recompute, checkpoints, and replay

#### 16.9.1 Recompute is an accepted recovery strategy

Because derived state is rebuildable by policy, recomputation is an accepted V1 recovery mechanism.

#### 16.9.2 Checkpoints are selective

CommercialNews V1 does not require universal checkpointing for all batch workflows.

Checkpointing is appropriate when:

* the workflow is expensive enough that full recomputation is painful
* recovery time matters operationally
* intermediate progress is meaningful and reusable
* the derived output is large and expensive to rebuild repeatedly

#### 16.9.3 Replay-safe design is preferred

Where jobs consume replayable inputs such as logs or outbox-derived work, the architecture should prefer replay-safe and idempotent application of derived effects.

This means batch replay should be designed under the same realism as stream consumers:

* duplicates may exist
* slices may be rerun
* outputs must not silently double-apply
* publish/cutover rules must remain explicit

#### 16.9.4 Rebuild over repair when simpler

If a derived dataset is cheap enough to rebuild from truth/logs, full rebuild is often preferred over complex incremental repair logic.

This aligns with V1 simplicity goals.

---

### 16.10 Failure handling and rerun rules

#### 16.10.1 Batch failures must not corrupt truth

Batch jobs operate on derived state and auxiliary datasets. Their failure must not invalidate already-committed truth.

#### 16.10.2 Rerun safety is required

A batch job must be designed so that rerunning it on the same bounded input is either:

* harmless, or
* explicitly controlled and validated

#### 16.10.3 Idempotency is not only for consumers

Batch/rebuild/reconciliation jobs must also be designed with replay/rerun safety in mind.

The same distributed-systems realities apply:

* duplicate input slices
* repeated scheduling
* operator-triggered rerun
* recovery after partial completion
* candidate publication retries

#### 16.10.4 Unsafe singleton assumptions are forbidden

If a job requires exclusive ownership or singleton execution, it must follow ADR-0024:

* no naive in-memory leader belief
* no timeout-only authority transfer
* no unsafe lock semantics without stale-owner protection

Whenever correctness depends on one current owner, ownership must be explicit and stale owners must be rejectable.

#### 16.10.5 Prefer safe non-progress over unsafe double-apply

If job ownership or publication safety is ambiguous, the system must prefer delayed completion over silent corruption.

---

### 16.11 Determinism expectations

CommercialNews batch workflows should be as deterministic as practical for a given bounded input.

Avoid unnecessary dependence on:

* wall-clock timing during processing
* unstable iteration order
* random behavior without controlled seed/policy
* external mutable state not included in the input boundary
* silently using the latest mutable reference data when event-time or snapshot-time semantics are required

Deterministic behavior improves:

* replay safety
* debugging
* comparison between runs
* reconciliation confidence
* selective recomputation

Perfect determinism is not required for every low-risk derived dataset, but non-deterministic behavior must not undermine correctness-sensitive publication or repair workflows.

---

### 16.12 Batch observability requirements

Every important batch/rebuild/reconciliation workflow should expose enough signals to answer:

* Is it running?
* Is it succeeding?
* How far behind is it?
* Is output trustworthy?
* Is recovery required?

Recommended signals:

* run success/failure count
* run duration
* records scanned / processed / skipped / repaired
* lag or freshness age
* backlog age/count for replay-oriented jobs
* stale-output or missing-output detection counts
* checkpoint age when checkpoints exist
* candidate publication success/failure
* stale-owner rejection / duplicate-run detection where coordination matters

Batch observability should also remain distinguishable from live stream-style lag:

* continuous consumer lag
* outbox backlog
* bounded rebuild/reconciliation backlog
* active derived dataset freshness

---

### 16.13 Module application notes (V1)

#### 16.13.1 Reading Experience

Reading may consume derived aggregates and enrichments, but public correctness remains truth-safe under lag.
Trending and aggregate inputs are valid batch candidates.
Batch remains the approved repair lane when stream-maintained enrichments or summaries drift.

#### 16.13.2 SEO

SEO may use rebuild or reconciliation jobs for derived artifacts, but publication visibility remains governed by truth.
Slug uniqueness remains truth-owned.
Batch may regenerate serving artifacts, but must not silently replace truth-owned routing rules.

#### 16.13.3 Interaction

Raw activity and aggregation are natural batch/async inputs.
Counters and summaries are derived, lag-tolerant, and rebuildable.
Where stream-style maintenance exists, batch remains the recovery lane for recompute and reconciliation.

#### 16.13.4 Audit

Audit archival, compaction, and reporting are batch candidates.
Append-only audit truth must remain traceable even when summaries lag.
Batch may repair or summarize views, not redefine append-only truth.

#### 16.13.5 Notifications

Delivery summaries, replay support, and cleanup jobs are batch candidates.
Email delivery itself remains async side effect, not batch-owned truth.
Batch may repair derived delivery views or summaries after lag/failure.

#### 16.13.6 Content / Identity / Authorization

These modules remain truth-first.
Batch may summarize, repair, or support them, but must not replace their authoritative state transitions.

---

### 16.14 What batch does not mean in CommercialNews

Batch in V1 does not imply:

* Hadoop, Spark, or a dedicated big-data cluster
* a separate analytics platform as a required baseline
* that every derived dataset needs a complex orchestration system
* that every async side effect becomes a scheduled batch job
* that truth correctness waits for rebuilds or projections
* that batch replaces continuous stream-style propagation
* that derived output may bypass publication/cutover discipline because it was produced offline

CommercialNews V1 batch is intentionally pragmatic:

* bounded
* rebuild-oriented
* observable
* safe under retry and rerun
* simple enough for a small team

---

### 16.15 V1 boundaries and V2+ hooks

#### V1

CommercialNews V1 supports:

* bounded batch jobs
* rebuilds
* reconciliation
* repair/replay
* archival/cleanup
* selective materialization
* observable derived state
* a complementary relationship between stream-style maintenance and bounded repair/recompute

#### V2+

Future versions may add:

* richer projection checkpoints
* stronger freshness SLIs
* dedicated read-model components
* more formalized serving datasets
* more advanced scheduling/orchestration
* domain-specific ranking/recommendation workflows
* richer event-time-aware rebuild/recompute logic where justified

**The V1 rule remains unchanged:**

* truth first
* derived state second
* batch never silently becomes truth
* batch repairs what stream-style maintenance cannot keep healthy enough
* safe fallback always wins over false freshness