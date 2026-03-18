# 18) Stream Processing & Derived State (V1)

This section defines how CommercialNews V1 uses **stream-style processing** to move, derive, repair, and observe data **after truth has committed**.

It answers these architecture-level questions:

* What counts as a stream in CommercialNews?
* Which stores are truth vs derived?
* How does V1 propagate committed changes safely without dual writes?
* Which derived outputs are maintained continuously vs rebuilt in bounded workflows?
* How do time semantics, joins, retries, duplicates, and rebuilds affect correctness?
* What is in scope for V1, and what is explicitly deferred?

This section is a **system-level policy**.
It does not define a dedicated streaming platform, and it does not turn all workflows into event sourcing.

### Related

* Runtime view: `04-runtime-view-v1.md`
* Architecture style: `09-architecture-style.md`
* System data: `10-system-data.md`
* Replication: `11-replication-v1.md`
* Transactions & consistency: `13-transactions-and-consistency-v1.md`
* Distributed systems assumptions: `14-distributed-systems-assumptions-v1.md`
* Consistency / ordering / consensus: `15-consistency-ordering-and-consensus-v1.md`
* Batch / derived data: `16-batch-processing-and-derived-data-v1.md`
* Dataflow / batch workflows: `17-dataflow-and-batch-workflows-v1.md`
* ADR-0013 (Outbox & delivery semantics)
* ADR-0015 (Cache policy & invalidation)
* ADR-0021 (Clock, time, and ordering policy)
* ADR-0022 (Versioning and fencing strategy)

---

## 18.1 Purpose

CommercialNews V1 treats many important post-commit activities as **continuous change processing**:

* audit ingestion
* notification delivery
* cache invalidation
* projection/read-model updates
* interaction aggregation signals
* selected metadata or routing updates
* replay / repair of delayed derived state

The system does **not** define success by completion of those side effects.
Instead, V1 defines success at the **truth boundary**, and uses stream-style processing to move the rest of the system toward convergence.

This section exists to make five things explicit:

* truth vs derived ownership
* the standard propagation path after truth commit
* the kinds of derived state CommercialNews maintains
* the correctness limits of async and lagging views
* the recovery posture when consumers, caches, or projections fall behind

---

## 18.2 Core principles

### 18.2.1 Truth is owned and committed synchronously

Every business-significant write succeeds only when the owning module’s truth state commits successfully.

Examples:

* Content lifecycle changes
* Identity security state changes
* Authorization governance changes
* SEO truth-owned slug/meta/canonical policy data

### 18.2.2 Derived state follows truth; it does not co-own truth

Search indexes, caches, counters, summaries, projections, and notification/audit side effects are **followers** of committed truth.

They may lag.
They may be rebuilt.
They may be temporarily missing.

They must not silently become hidden truth.

### 18.2.3 Outbox is the standard V1 stream boundary

CommercialNews V1 uses:

* local truth transaction
* atomic outbox write
* Background Worker publish
* at-least-once broker delivery
* idempotent consumer processing

This is the standard mechanism for reliable change propagation across modules and workflows.

### 18.2.4 Stream processing in V1 is selective, not platform-first

CommercialNews V1 is **not** a Kafka-first stream platform.
It uses stream-style processing where it provides clear value:

* reactive side effects
* projection maintenance
* interaction analytics signals
* repair/reconciliation hooks

V1 does not introduce a heavyweight streaming platform as a baseline dependency.

### 18.2.5 Batch and stream are complementary

CommercialNews uses both:

* **continuous stream-style propagation** for near-real-time side effects and projections
* **bounded batch/rebuild/reconciliation** for repair, replay, reconciliation, archival, cleanup, and selective rematerialization

Stream does not replace batch.
Batch does not redefine truth.

### 18.2.6 At-least-once is normal; idempotency is mandatory

Duplicate publication, duplicate delivery, replay, and restart are normal failure semantics in V1.

Therefore:

* idempotent consumers are mandatory
* stable message identity is mandatory where effects matter
* rebuild/reconciliation must exist for important derived outputs

---

## 18.3 What counts as a stream in CommercialNews

In CommercialNews V1, a “stream” is any ordered flow of committed changes or time-ordered operational events that can be consumed incrementally.

Important stream families include:

### 18.3.1 Domain change streams from truth commits

Examples:

* `ArticlePublished`
* `ArticleUnpublished`
* `UserRegistered`
* `UserEmailVerified`
* `PasswordResetRequested`
* `RoleAssigned`
* `PermissionGranted`

These are emitted through the standard outbox contract.

### 18.3.2 Operational/event side-effect streams

Examples:

* notification send attempts/results
* audit ingestion results
* retry/DLQ events
* cache invalidation requests
* delayed consumer recovery signals

### 18.3.3 Interaction/event telemetry streams

Examples:

* article view signals
* like/comment activity
* trending inputs
* future anti-abuse/security signals

These may be raw, aggregated, or summarized depending on module policy.

### 18.3.4 Repair/replay/reconciliation streams

Examples:

* replay of missing outbox events
* bounded rebuild input sets
* mismatches between truth and derived outputs
* repair candidates generated by reconciliation workflows

These are still derived-state workflows.
They do not redefine truth ownership.

---

## 18.4 Truth vs derived systems

### 18.4.1 Truth systems

Truth systems are module-owned stores where authoritative state lives.

Examples:

* Content truth for article lifecycle
* Identity truth for user/account/security state
* Authorization truth for role/permission assignments
* SEO truth for slug uniqueness and policy-owned metadata

Truth systems define correctness for:

* lifecycle visibility
* governance state
* security-sensitive state
* uniqueness/invariant enforcement

### 18.4.2 Derived systems

Derived systems exist to accelerate, summarize, enrich, or operationalize data already committed as truth.

Examples:

* Redis caches
* public read projections
* search documents
* SEO-serving artifacts that can be regenerated
* interaction counters and summaries
* notification delivery views
* audit reporting summaries
* admin dashboards

Derived systems may be:

* stale
* rebuilt
* incomplete during recovery
* temporarily unavailable

### 18.4.3 Rule: derived systems must not become hidden truth

CommercialNews forbids designs in which:

* a cache becomes the only reliable visibility source
* a projection becomes the only source for security-sensitive reads
* a lagging read model becomes the sole judge of publication correctness
* a batch output is exposed as authoritative before validation/cutover

If correctness matters and derived state is uncertain, the system must fall back safely to truth.

---

## 18.5 Standard V1 stream-processing model

The standard V1 pattern is:

1. Validate command semantics
2. Enforce authorization when required
3. Load current truth state needed for the decision
4. Apply truth change
5. Write outbox record in the same local transaction
6. Commit
7. Return success based on truth commit only
8. Publish outbox asynchronously
9. Let downstream consumers update derived systems or perform side effects

This applies to:

* publish/unpublish
* register/verify/reset
* governance changes
* selected interaction workflows
* future indexing/projection hooks

### 18.5.1 Prohibited alternative: ad hoc dual writes

CommercialNews V1 must not implement cross-system propagation by directly writing:

* DB + broker inline
* DB + Redis as one required success path
* DB + email provider inside the same truth flow
* DB + search/projection as mandatory synchronous success criteria

Those patterns are failure-prone and violate V1 truth/derived discipline.

---

## 18.6 Derived state categories in V1

CommercialNews V1 maintains several classes of derived state.

### 18.6.1 Reactive side effects

These are not authoritative state, but operational or user-facing consequences of committed truth.

Examples:

* audit append after publish/unpublish
* verification/reset emails
* notification sends
* cache invalidation requests

Rules:

* must be retry-safe
* must be idempotent where duplicates are harmful
* must not determine core flow success

### 18.6.2 Projections / read models

These are query-oriented representations maintained from truth changes.

Examples:

* future public article read models
* governance/read dashboards
* derived SEO-serving state
* authorization-effective views

Rules:

* projection lag is acceptable within policy
* projections must be rebuildable
* projection correctness must never override truth correctness

### 18.6.3 Search / serving artifacts

These are optimized forms for retrieval or routing support.

Examples:

* search documents
* SEO lookup-serving artifacts
* future denormalized listing artifacts

Rules:

* they are derived
* rebuild/reconciliation must be possible
* public correctness must survive serving lag through safe fallback

### 18.6.4 Interaction counters and summaries

These are derived summaries from raw or semi-raw interaction signals.

Examples:

* article counters
* trending inputs
* hourly/daily summaries
* future engagement rollups

Rules:

* eventual consistency is acceptable
* replay and recompute must be possible where business value warrants it
* burst isolation is preferred over synchronous update pressure on the read path

### 18.6.5 Repair and reconciliation outputs

These are candidate outputs used to restore health after delay, failure, or drift.

Examples:

* missing audit/reporting repair
* delayed SEO/projected state correction
* counter recomputation
* replayed notification artifacts
* mismatch reports between truth and derived state

Rules:

* bounded input required
* candidate-before-cutover where correctness matters
* partial repair output must not silently become active truth

---

## 18.7 Delivery-oriented messaging vs replay/history-oriented logs

CommercialNews V1 primarily uses RabbitMQ and outbox-driven delivery semantics.

### 18.7.1 Delivery-oriented messaging in V1

RabbitMQ is used for:

* async side effects
* burst isolation
* retry-safe consumer processing
* decoupled downstream work

Its role in V1 is **delivery-oriented**, not permanent event history.

V1 does not assume RabbitMQ alone is the long-term replay source for all important derived-state recovery.

### 18.7.2 Replay/history posture in V1

Where replay or rebuild matters, CommercialNews relies on some combination of:

* truth stores
* module-owned history/lifecycle tables
* retained outbox records as operational recovery source where policy allows
* bounded rebuild/reconciliation workflows
* deterministic regeneration from authoritative inputs

### 18.7.3 Non-goal in V1

CommercialNews V1 does not promise:

* permanent broker-retained event history for all modules
* global event replay platform semantics
* full event sourcing as a system-wide truth model

---

## 18.8 Time semantics in stream processing

### 18.8.1 Event time vs processing time

CommercialNews distinguishes:

* **event time** — when the business event actually occurred
* **processing time** — when the worker/consumer handled the event

These must not be conflated.

### 18.8.2 Business metrics generally prefer event time

Event time should be used where meaning depends on when the business event actually happened.

Examples:

* interaction/trending windows
* security anomaly windows
* publish/unpublish timelines
* user behavior/session analysis

### 18.8.3 Operational metrics may use processing time

Processing time is appropriate for observability of the pipeline itself.

Examples:

* worker throughput
* queue drain rate
* consumer backlog catch-up speed
* retry pacing
* DLQ growth over processing time

### 18.8.4 Rule: every time-windowed pipeline needs an explicit lateness policy

For pipelines that window event-time data, module/runtime policy must decide whether late events are:

* ignored after a cutoff
* applied with correction
* delayed until watermark-like confidence is sufficient

V1 favors simple, explicit policies over hidden complexity.

### 18.8.5 Timestamp requirements

Where stream timing matters, events should carry enough metadata to reason about:

* `OccurredAt`
* `CorrelationId`
* aggregate `Version` when ordering matters
* optionally processing timestamps in worker logs/telemetry

---

## 18.9 Join semantics in V1

CommercialNews recognizes three important join styles in stream-oriented processing.

### 18.9.1 Stream-stream joins

Used when correlating two event flows over time.

Examples:

* future search → click correlation
* notification sent → notification opened
* suspicious auth pattern detection

Rules:

* join window must be explicit
* unmatched-event behavior must be explicit
* ordering and lateness assumptions must be documented where correctness matters

### 18.9.2 Stream-table joins

Used when enriching events with current or versioned reference state.

Examples:

* interaction event + article metadata
* auth event + user/role state
* notification event + recipient preferences

Rules:

* prefer locally maintained derived lookup state over per-event remote truth queries where latency matters
* if correctness depends on time-specific reference state, the “as-of” semantics must be explicit

### 18.9.3 Table-table joins as maintained materialized views

Used when continuously maintaining a joined read model from changing truth/reference state.

Examples:

* article + SEO + media → public-serving view
* user + role + permission → effective authorization view
* content + category/tag mappings → listing summaries

Rules:

* these are derived views
* rebuild strategy must exist
* update semantics must be version-aware where stale overwrite is possible

### 18.9.4 Rule: joins are time-dependent, not only key-dependent

For nontrivial joins, module docs must be clear about whether the join uses:

* current state
* event-time state
* latest available derived state

If replay determinism matters, time/version semantics must be stable under reprocessing.

---

## 18.10 Fault tolerance and recovery posture

### 18.10.1 V1 assumption: at-least-once delivery

CommercialNews assumes that:

* outbox publish may retry
* broker delivery may duplicate
* consumer processing may restart
* delayed application of events is normal

Exactly-once claims are out of scope unless explicitly proven within a narrow, bounded mechanism.

### 18.10.2 Effective correctness comes from idempotent effects

CommercialNews aims for **effectively-once outcomes** by combining:

* stable `MessageId` / `EventId`
* aggregate `Version` where ordering matters
* idempotent consumer logic
* rebuild/reconciliation capability

### 18.10.3 External side effects require explicit dedupe

Checkpointing inside a worker does not make external side effects magically safe.

Therefore, for effects like:

* sending email
* appending audit
* applying projection updates
* emitting downstream requests

the system must use:

* durable dedupe keys
* delivery logs where appropriate
* monotonic version checks where appropriate

### 18.10.4 Derived state must have a recovery path

Important derived systems must have a defined recovery posture:

* rebuild from truth
* replay from retained operational history
* reconciliation against authoritative source
* bounded recomputation

If a derived system cannot be replayed, rebuilt, or reconciled, it is too close to hidden truth.

### 18.10.5 Safe non-progress beats unsafe dual-apply

When ownership, ordering, or stale-writer ambiguity exists:

* reject stale work
* resync from truth
* rebuild safely
* prefer temporary lag over silent corruption

This is consistent with V1 stale-actor and fencing posture.

---

## 18.11 Batch vs stream boundary

### 18.11.1 Use stream-style processing when:

* reaction should be near-real-time
* the output is a side effect or continuously maintained derived view
* buffering and retry are useful
* bounded lag is acceptable

### 18.11.2 Use bounded batch/rebuild workflows when:

* input can be bounded explicitly
* repair/reconciliation is needed
* a summary or rematerialized output is needed
* replay/recompute is simpler and safer than incremental repair
* candidate validation or cutover is required

### 18.11.3 Rule: batch remains the repair lane for stream-derived systems

CommercialNews V1 does not force every problem into incremental stream repair.
Where bounded recompute is safer, batch is preferred.

---

## 18.12 V1 module posture

### 18.12.1 Content

* emits lifecycle change events through outbox
* defines truth for publication visibility
* downstream SEO/audit/notification behavior follows asynchronously

### 18.12.2 SEO

* owns slug/meta/canonical truth policy
* may maintain derived serving artifacts
* correctness must survive cache/projection lag through truth-backed fallback

### 18.12.3 Reading Experience

* read path may use caches and derived enrichments
* correctness of visibility must remain truth-safe
* interaction-derived counters and enrichments may lag

### 18.12.4 Identity

* security truth commits synchronously
* notifications and audit are downstream effects
* self-state reads after critical changes must remain truth-strong

### 18.12.5 Authorization

* governance truth commits synchronously
* effective permissions may be represented through derived views/caches
* post-change reads for governance correctness must remain truth-strong

### 18.12.6 Interaction

* ingestion must not slow public reads
* analytics and counters are derived/eventual
* event-time semantics matter for meaningful windows and anomaly logic

### 18.12.7 Audit

* append-only async ingestion is normal
* lag is acceptable only if visible and repairable
* event identity must support replay-safe ingestion

### 18.12.8 Notifications

* delivery is event-driven and retry-safe
* duplicate prevention is mandatory
* delivery views/status are derived, not truth for core business success

---

## 18.13 Non-goals in V1

CommercialNews V1 does not adopt the following as system-wide defaults:

* full event sourcing
* permanent log-retained replay for every event family
* a dedicated log-based streaming platform as a baseline dependency
* distributed transactions / 2PC across DB, broker, cache, and external systems
* global total ordering of all events
* “exactly-once” guarantees across heterogeneous systems without explicit bounded proof
* synchronous projection completion as a requirement for truth-flow success

---

## 18.14 Observability expectations

The health of stream-driven derived state must be observable separately from truth-path health.

Minimum signals include:

* outbox pending count
* outbox oldest pending age
* publish success/failure
* broker queue depth
* consumer success/failure and retry rate
* DLQ size/age where enabled
* dedupe hits / idempotency rejects
* projection lag/freshness where meaningful
* fallback-to-truth rates where derived views may be stale
* bounded rebuild/reconciliation backlog and age

Rule:

* dashboards and alerts must distinguish **truth-path success** from **derived-path lag**
* a healthy truth path with lagging derived state is degraded, not identical to fully healthy

---

## 18.15 What stream processing does not mean in CommercialNews

In CommercialNews V1, “stream processing” does **not** mean:

* every queue consumer is a sophisticated analytics engine
* every workflow is an event-sourced aggregate
* every derived dataset is incrementally maintained forever
* every late event must produce correction semantics
* the broker is the only or permanent history store
* application code may avoid truth ownership discipline because “the stream will fix it later”

Truth remains explicit.
Streams move consequences.
Batch repairs what streams do not complete cleanly.

---

## 18.16 Runtime summary (V1)

CommercialNews V1 uses stream-style processing to keep derived systems converging behind committed truth.

The architecture is therefore:

* **truth-first**
* **outbox-based**
* **delivery-at-least-once**
* **consumer-idempotent**
* **fallback-safe**
* **rebuild-capable**
* **batch-assisted for repair and reconciliation**

This gives CommercialNews a practical V1 posture:

* minimal distributed coordination
* strong local correctness
* observable lag
* safe recovery under retries, duplicates, and partial failure
* room to evolve into richer projections or stream analytics later without redefining truth ownership