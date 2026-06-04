# Audit — Idempotency & Consistency (V1)

This document defines Audit-specific idempotency, consistency, retry safety, ordering posture, replay posture, and investigation readiness for CommercialNews V1.

Audit V1 is intentionally simple in runtime scope:

* asynchronous audit ingestion
* SQL-backed append-only `AuditLog`
* consumer-side `AuditIngestion`
* durable `MessageId` deduplication
* investigation query APIs
* lightweight dashboard queries directly from `AuditLog`

Future extensions such as alerting, digest generation, materialized summaries, archival, reconciliation, Redis acceleration, partitioning, and notification integration are allowed extension paths, but they are not required for V1 core delivery.

System-wide rules live in:

* `../../../../architecture/arc42/11-replication-v1.md`
* `../../../../architecture/arc42/13-transactions-and-consistency-v1.md`
* `../../../../architecture/arc42/14-distributed-systems-assumptions-v1.md`
* `../../../../architecture/arc42/15-consistency-ordering-and-consensus-v1.md`
* `../../../../architecture/arc42/16-batch-processing-and-derived-data-v1.md`
* `../../../../architecture/arc42/17-dataflow-and-batch-workflows-v1.md`
* `../../../../architecture/arc42/18-stream-processing-and-derived-state-v1.md`
* `../../../../architecture/arc42/19-stream-processing-runtime-v1.md`
* ADR-0011 — Replication Topology
* ADR-0012 — Data Store Placement
* ADR-0013 — Outbox & Delivery Semantics
* ADR-0014 — Public Identifier Strategy
* ADR-0015 — Cache Policy & Invalidation
* ADR-0017 — Partitioning Strategy
* ADR-0018 — Transaction Boundaries & Consistency Model
* ADR-0020 — Timeout, Retry, and Failure Detection Policy
* ADR-0025 — Batch Processing and Derived State Policy
* ADR-0026 — Batch Job Orchestration and Materialization Policy
* ADR-0027 — Stream Processing and Derived State Policy
* ADR-0028 — Consumer Idempotency, Replay, and Rebuild Policy

---

## 1. Role of Audit in the System

Audit is a governance evidence module.

Audit does not own source module business truth.

Audit does not own:

* publication state
* identity security state
* authorization truth
* media attachment truth
* interaction truth
* notification delivery truth
* SEO routing truth

Audit owns:

* durable evidence that an auditable event was observed and persisted according to policy
* consumer-side ingestion status for audit processing
* audit investigation query contracts
* audit evidence redaction policy
* audit evidence idempotency rules

Therefore:

* Audit is not the source of truth for whether a business action is currently valid.
* Audit is the source of truth for whether the system recorded an investigation-ready audit fact.
* Business truth may exist before Audit catches up.
* Audit lag must not block business success.
* Audit evidence is authoritative for investigation only after it has been persisted.

---

## 2. Truth vs Derived

### 2.1 Audit truth

Audit truth consists of:

* `AuditLog`
* `AuditIngestion`

`AuditLog` is SQL-backed append-only evidence truth.

`AuditIngestion` is SQL-backed consumer-side processing state.

AuditLog answers questions such as:

* Which auditable event was recorded?
* Which `MessageId` produced the audit record?
* Which actor/action/resource metadata was captured?
* When did the producer say the event occurred?
* When did Audit persist the evidence?

AuditIngestion answers questions such as:

* Has Audit received this `MessageId`?
* Did Audit process it successfully?
* Was it duplicate-safe?
* Did it fail?
* Is it dead-lettered or awaiting remediation?

### 2.2 Audit truth is not source business truth

Audit does not answer by itself:

* whether the originating resource still exists
* whether the originating resource is currently active
* whether the business action is still the latest state transition
* whether another module’s current truth has since changed

For those questions, investigation must correlate Audit with the owning module’s truth.

Examples:

* Identity owns whether a user is currently locked.
* Audit owns evidence that a `UserLocked` event was recorded.
* Authorization owns current role/permission assignment truth.
* Audit owns evidence that a role/permission change was recorded.
* Content owns current article publication truth.
* Audit owns evidence that an article publication event was recorded.

### 2.3 Derived audit outputs

The following are derived and may lag:

* dashboard summaries
* recent-risk panels
* module statistics
* actor summaries
* resource timeline materializations
* reports
* digests
* alert outputs
* archive indexes
* completeness reconciliation reports
* replay candidate sets

Rule:

`AuditLog` is evidence truth.
Dashboards, summaries, alerts, reports, digests, archive outputs, and reconciliation outputs are derived.

---

## 3. Idempotency

### 3.1 Primary idempotency key

Audit V1 uses `MessageId` as the canonical idempotency key.

`MessageId` is copied from:

```text
outbox.OutboxMessage.MessageId
```

Audit V1 must not use the old local alias `EventId` for this concept.

The same upstream `MessageId` must not produce duplicate `AuditLog` records.

Recommended enforcement:

* unique constraint/index on `AuditLog.MessageId`
* unique constraint/index on `AuditIngestion.MessageId`
* deterministic consumer dedupe keyed by `MessageId`

Rule:

One canonical triggering message produces at most one canonical audit evidence record.

### 3.2 Message-level idempotency

Message-level idempotency protects against the same message being delivered or processed more than once.

Duplicate sources include:

* Outbox publish ambiguity
* RabbitMQ redelivery
* consumer crash before ACK
* consumer restart after partial processing
* retry after timeout
* replay or bounded reprocess

Required behavior:

* same `MessageId` arrives again
* Audit must not insert a second `AuditLog`
* Audit must treat it as duplicate-safe
* consumer may ACK after verifying duplicate-safe state

### 3.3 Business-level idempotency

Audit V1 does not collapse different `MessageId` values by default.

Reason:

* different messages may represent distinct attempts
* repeated governance attempts may be useful investigation evidence
* business-level dedupe must not hide legitimate audit trails
* raw audit evidence should preserve distinct emitted messages

Future business-level dedupe may be introduced for derived outputs such as:

* alerts
* digests
* summaries
* reports
* noisy derived audit views

Business-level dedupe must be explicitly documented per event family or derived workflow.

### 3.4 Duplicate delivery vs distinct events

Audit must distinguish:

#### Duplicate delivery

Same `MessageId` appears again.

Required behavior:

* dedupe by `MessageId`
* do not insert duplicate `AuditLog`
* mark `AuditIngestion` as `Duplicate` or complete as idempotent success according to implementation policy

#### Distinct event

Different `MessageId` appears.

Default behavior:

* treat as a distinct auditable message
* process normally
* do not collapse by default

### 3.5 Idempotency survives replay

If the same canonical message is seen again due to:

* worker restart
* broker redelivery
* DLQ replay
* failed ingestion remediation
* bounded backfill

the result must still converge to:

* one persisted audit fact
* zero silent history mutation
* one clear processing outcome

Example outcomes:

* `inserted`
* `deduped`
* `ignored`
* `failed`
* `dead-lettered`

### 3.6 Idempotency beats singleton assumptions

Audit correctness must not depend on:

* only one consumer instance running
* one process being “the current audit leader”
* startup order
* local ownership belief
* one replay worker being assumed exclusive without authoritative protection

Audit correctness should rely on:

* stable `MessageId`
* append-only inserts
* durable SQL uniqueness
* replay-safe consumer behavior

---

## 4. Consistency Expectations

### 4.1 Audit is eventually consistent with source truth

Audit ingestion is eventually consistent relative to the originating business action.

Expected behavior:

* source action succeeds at the originating truth boundary
* outbox intent is committed when async work is required
* Audit evidence appears later after publish and consumer processing

Audit may lag because of:

* outbox backlog
* RabbitMQ delay
* consumer outage
* Audit DB outage
* retry backoff
* DLQ/poison handling

### 4.2 Audit must never block core flows

Audit unavailability must not fail:

* Content truth commits
* Identity truth commits
* Authorization truth commits
* Media truth commits
* Interaction truth commits
* SEO truth commits
* Notification delivery-state truth commits

Source module success is based on:

* source truth commit
* source outbox commit when async work is required

It is not based on:

* AuditLog insertion
* Audit dashboard update
* Audit alert creation
* digest generation
* reporting output completion

### 4.3 No read-your-writes guarantee for Audit

Immediately after a source business action succeeds, the caller is not guaranteed that audit evidence is already queryable.

The architecture guarantees:

* originating truth committed
* auditable outbox intent committed where required
* Audit consumer will attempt ingestion asynchronously

Audit APIs do not provide read-your-writes guarantees for just-committed source actions.

### 4.4 Timeout ambiguity

Timeout is ambiguous.

A timeout or missing acknowledgment in the Audit pipeline does not prove:

* the source action did not happen
* the Outbox message was not published
* the Audit insert did not happen
* the message will not be retried
* the previous attempt had no effect

Audit retry must assume the previous attempt may have partially or fully succeeded.

Retry safety is achieved through durable `MessageId` dedupe.

### 4.5 Cause-before-evidence rule

Audit evidence must follow a committed domain cause.

Runtime order:

```text
Source truth commit
    ↓
Outbox message commit
    ↓
Outbox publish
    ↓
Audit consume
    ↓
AuditLog persist
```

Audit may lag behind the source cause.

Audit must not invent evidence ahead of a committed canonical event.

---

## 5. Transaction Boundaries

### 5.1 Source transaction boundary

The source module transaction owns:

* source truth mutation
* source local metadata/history if required
* Outbox message creation

Audit is not part of this transaction.

Source modules must not:

* write directly to Audit tables
* call Audit synchronously
* wait for Audit consumer completion
* include Audit success as part of business success

### 5.2 Audit transaction boundary

The Audit transaction owns:

* `AuditIngestion` create/update
* `AuditLog` insert
* `MessageId` dedupe outcome
* local processing metadata required for observability

A typical Audit local transaction should atomically commit:

* append-only `AuditLog` if not duplicate
* ingestion status update
* dedupe decision

This local transaction is separate from the source module transaction.

In the refactored Application architecture, handlers do not manually open
transactions. `AuditTransactionBehavior` wraps commands that need durable writes.
Commands opt into this behavior by implementing `IAuditTransactionalRequest`.

Queries do not require this transaction behavior.

### 5.3 Outside the Audit transaction

The following are not part of the Audit persistence transaction:

* source module truth commit
* RabbitMQ broker state
* producer-side Outbox state transition
* dashboard refresh
* alert notification delivery
* external email/provider calls
* reporting workflow completion
* archive workflow completion

Distributed atomic commit across source truth + broker + Audit store is out of scope for V1.

### 5.4 Transaction duration rule

Audit transactions must be short.

Audit must not hold an open transaction while:

* waiting for downstream systems
* running alert rules
* sending notifications
* generating summaries
* performing long-running enrichment
* running retry loops
* doing cross-module calls

Audit should persist the audit fact quickly and defer all non-essential follow-up work.

---

## 6. Audit Immutability and Correction Model

### 6.1 Append-only rule

Audit history is append-only by default.

Previously written `AuditLog` records must not be silently overwritten.

### 6.2 Correction rule

If correction is required by policy:

* append a new corrective record, or
* use an explicit correction model documented separately

Audit must not silently mutate historical records in place.

### 6.3 Correction does not erase prior evidence

If an operator or workflow discovers a mapping, classification, or redaction issue:

* the fix must remain auditable
* prior evidence must remain visible or explicitly superseded by policy
* replay/remediation must not silently erase the original record trail

### 6.4 Retention and purge

Deletes or purges are not normal business operations.

They require explicit policy such as:

* retention policy
* legal/compliance purge
* security redaction policy
* operator-controlled remediation policy

Without such policy, AuditLog is append-only.

---

## 7. Consumer Processing Posture

Audit is a pure asynchronous consumer.

Consumer runtime lives outside Audit.Application. `CommercialNews.Worker`
consumes Outbox/Broker messages, maps them to `IngestAuditEventCommand`, and
invokes Audit.Application through MediatR.

Audit.Application owns validation, transaction behavior, ingestion use cases,
normalizer abstractions, and registry selection. It does not read queues or
brokers directly.

Audit consumes Outbox-published messages from modules such as:

* Identity
* Authorization
* Content
* Media
* Interaction
* SEO
* Notifications

Delivery is at-least-once.

Therefore, Audit must assume:

* duplicate delivery
* replay
* consumer restart
* partial processing
* out-of-order arrival
* retry after timeout
* overlapping remediation attempts in future workflows

### 7.1 Consumer requirements

Audit consumers must provide:

* idempotent inserts
* retry-safe processing
* append-only persistence
* durable uniqueness protection by `MessageId`
* safe logging
* redaction before persistence
* observable failure handling

Audit consumers must not rely on:

* perfect upstream ordering
* perfect clock alignment
* absence of replay
* immediate consumer success
* a single consumer instance as a correctness guarantee

### 7.2 Duplicate delivery vs stale delivery

Audit must distinguish:

#### Duplicate delivery

Same `MessageId` is delivered again.

Required behavior:

* dedupe by `MessageId`
* do not create duplicate `AuditLog`

#### Stale delivery

An older source event arrives after newer source truth exists elsewhere.

Audit behavior:

* if it has a distinct `MessageId`, it is usually still valid historical evidence
* do not reject solely because newer truth exists
* do not reinterpret current business state from Audit alone

Audit should preserve chronology fields for investigation:

* `OccurredAtUtc`
* `IngestedAtUtc`
* `AggregateId`
* `AggregateVersion`

### 7.3 Out-of-order arrival

Audit does not require ordered application to remain correct.

Out-of-order events may still be recorded as historical evidence if they are distinct canonical messages.

Investigation views may sort by:

* source chronology: `OccurredAtUtc`
* aggregate chronology: `AggregateVersion` where available
* ingestion chronology: `IngestedAtUtc`

### 7.4 No global ordering claim

Audit does not claim:

* one strict total global order across all module events
* one perfect cluster-wide timeline
* one perfect chronology reconstructed from timestamps alone

Audit claims:

* durable evidence persistence
* stable `MessageId`
* append-only integrity
* correlation-assisted reconstruction
* source-module truth correlation where current state matters

---

## 8. Retry, Failure, DLQ, and Replay

### 8.1 Retry strategy

Retries must be:

* bounded
* backoff-based
* safe under duplicate delivery
* safe under replay
* observable

Typical posture:

* transient error → retry with backoff
* timeout → treat as ambiguous and retry safely using `MessageId`
* permanent error → fail visibly
* poison message → terminal handling or DLQ

### 8.2 DLQ or dead-letter handling

DLQ or terminal failure records must be:

* inspectable
* correlated with `MessageId`
* correlated with `CorrelationId` when available
* replayable after fix where policy allows
* visible to operators

DLQ handling is consumer-side failure handling.

It must not be written back as producer-side Outbox failure.

### 8.3 Replay safety

Replay/reprocess operations must not create duplicate audit facts.

Replay should:

* preserve original `MessageId`
* re-attempt missing evidence
* not duplicate already-recorded evidence
* preserve append-only semantics
* record outcome where applicable

Possible replay outcomes:

* `inserted`
* `deduped`
* `ignored`
* `invalid_payload`
* `still_failing`
* `escalated`

### 8.4 Same-message replay

Same `MessageId` appears again.

Required behavior:

* no duplicate `AuditLog`
* duplicate-safe handling
* consumer may ACK after verifying existing evidence

### 8.5 Same-intent replay

Different `MessageId` may represent a similar or repeated business intent.

Audit V1 does not collapse these by default.

Future business-level dedupe may be applied only for:

* alerts
* digests
* reports
* derived views
* event families where duplicated evidence would be misleading or harmful

### 8.6 Safe non-progress beats unsafe evidence mutation

If Audit cannot safely determine whether a retry/replay/remediation action should apply, prefer:

* retry later
* defer
* mark failed
* dead-letter
* escalate
* require operator remediation

over:

* synthetic evidence insertion
* mutating prior audit facts
* double-applying a derived output
* silently ignoring ambiguous failure without visibility

---

## 9. Reconciliation and Completeness

### 9.1 V1 posture

Completeness matters, but automated reconciliation is not required for V1 core delivery.

V1 must make ingestion lag and failures observable.

Future reconciliation workflows may compare expected auditable events against persisted Audit evidence.

### 9.2 Completeness is a governance signal

Potential gaps must be detectable through:

* outbox pending count
* oldest outbox pending age
* audit queue depth
* audit consumer failure rate
* failed ingestion count
* DLQ entries
* dedupe hits
* replay counts
* missing expected canonical events during investigation
* future reconciliation mismatch reports

### 9.3 Reconciliation output is derived

Future reconciliation outputs may include:

* mismatch reports
* replay candidate sets
* completeness summaries
* investigation handoff reports

These outputs are derived.

They must not:

* rewrite canonical audit truth
* treat dashboard or summary data as evidence truth
* silently auto-correct historical evidence without explicit replay/correction policy
* infer source business truth from Audit absence alone

### 9.4 Bounded reprocess

A bounded reprocess is acceptable when safer than fragile selective repair.

Conditions:

* original `MessageId` is preserved
* dedupe remains enforced
* existing evidence is not mutated
* input scope is bounded
* outcome is observable

---

## 10. Derived Outputs and Future Workflows

### 10.1 V1 derived outputs

V1 may compute lightweight dashboard summaries directly from `AuditLog`.

Examples:

* count by module
* count by action
* count by severity
* count by risk level
* recent high-risk events
* failed ingestion count

These are derived views.

They do not replace `AuditLog`.

### 10.2 Deferred derived workflows

The following workflows are future extensions:

* alerting
* digest generation
* materialized summaries
* reporting
* archival
* retention jobs
* reconciliation
* batch replay
* Redis dashboard caching
* partitioned worker lanes
* physical table partitioning

### 10.3 Rerun safety for future derived workflows

Future derived workflows must be safe to rerun on the same bounded input.

This means:

* summaries must be replaceable or versioned
* digests must not duplicate
* alert creation must use durable dedupe keys
* partial candidate output must not masquerade as completed report state
* rerun must not corrupt canonical append-only evidence

### 10.4 Candidate-before-cutover

Future correctness-sensitive derived outputs should follow:

```text
build candidate output
    ↓
validate candidate
    ↓
publish/cut over
```

Partial output must not be exposed as complete.

### 10.5 Batch workflow requirements

Future batch workflows must define:

* bounded input
* stage sequence
* output contract
* rerun behavior
* publication/cutover behavior where needed
* observability signals

---

## 11. Cache Posture

Audit V1 does not require Redis caching.

SQL remains the source of audit evidence truth.

Redis must not be used as:

* audit evidence truth
* the only dedupe mechanism for critical `AuditLog` append
* replacement for SQL investigation detail queries

Future Redis usage may be introduced only as optional acceleration for:

* dashboard summaries
* recent high-risk event panels
* module/action metadata
* TTL-bound alert dedupe hints

Future cache entries must remain:

* derived-only
* optional
* safely bypassable
* short-lived or policy-defined by TTL

---

## 12. Partitioning Readiness

Audit V1 is partition-ready but not physically partitioned by default.

V1 relies on:

* SQL indexes
* bounded time-range queries
* source module metadata
* risk metadata
* consumer lag observability
* query latency observability

Future partitioning may include:

* time-range partitioning for `AuditLog`
* worker lane partitioning by source module
* worker lane partitioning by risk priority
* queue partitioning by module/category/priority
* batch-window partitioning for summary, digest, archive, or reconciliation workflows
* logical lane routing through indirection rather than direct `hash(key) mod N`

Future partitioning must not change:

* append-only evidence semantics
* `MessageId` dedupe
* source module truth ownership
* investigation correctness
* eventual consistency expectations

Partitioning must be signal-driven.

Signals include:

* query P95/P99 degradation
* growing `AuditLog` table/index pressure
* audit consumer lag
* oldest uningested message age
* retry/DLQ pressure
* security audit delayed by high-volume non-security events
* archive/report windows becoming too slow

---

## 13. Coordination and Ownership

### 13.1 No global singleton by default

Ordinary Audit ingestion must not require:

* one global audit leader
* one process being the only ingestor
* startup order deciding authority
* timeout-only ownership assumptions

Audit correctness should come from:

* `MessageId`
* durable dedupe
* append-only inserts
* replay-safe consumer behavior

### 13.2 Future ownership-sensitive workflows

If a future Audit workflow truly requires one current owner, such as exclusive partition backfill or one-current audit repair owner, it must define:

* ownership source of truth
* monotonic generation/fencing token
* stale owner rejection
* failure/remediation behavior

Naive leader/lock patterns are not acceptable for correctness-sensitive workflows.

### 13.3 Safe non-progress

If ownership is ambiguous for a correctness-sensitive future workflow, Audit must prefer:

* delayed remediation
* retry later
* operator intervention
* stale-owner rejection
* continued evidence-truth preservation

over unsafe dual replay, unsafe double-publish of derived reports, or contradictory repair mutation.

---

## 14. Observability Signals

### 14.1 Minimum V1 signals

Audit V1 should expose:

* audit ingest success count
* audit ingest failure count
* audit duplicate/dedupe hit count
* audit ignored message count
* audit dead-lettered count
* audit consumer retry count
* audit mapping failure count
* audit redaction failure count
* audit queue depth
* audit unacked message count
* audit DLQ size and age
* publish-to-ingest lag
* occurred-to-ingest lag
* oldest failed ingestion age
* failed ingestion count
* audit query latency
* dashboard query latency

### 14.2 Logging requirements

Logs should include:

* `MessageId`
* `CorrelationId`
* `EventType`
* `SourceModule`
* `Action`
* `ResourceType`
* `ResourceId`
* processing outcome
* retry attempt
* error class where applicable

Processing outcomes may include:

* `inserted`
* `deduped`
* `ignored`
* `failed`
* `dead-lettered`
* `replayed`

Logs must remain:

* privacy-aware
* secret-safe
* usable for investigations

### 14.3 Health layering

Audit observability should distinguish:

* consumer liveness
* queue/backlog health
* ingestion success/failure
* dedupe pressure
* DLQ pressure
* audit completeness risk
* query health
* future derived-reporting or archival workflow lag

A green process is not enough if:

* backlog is growing
* DLQ is aging
* failed ingestions are increasing
* expected audit facts are not appearing

### 14.4 Future extension signals

Future extensions may track:

* alert created count
* alert dedupe hit count
* digest generation duration
* batch run duration
* archive lag
* reconciliation mismatch count
* cache hit rate
* cache fallback-to-SQL count
* partition lane lag
* stale owner rejection count

---

## 15. Canonical Mapping Fields

Recommended canonical mapping fields include:

* `MessageId`
* `CorrelationId`
* `EventType`
* `SourceModule`
* `ActorInternalId`
* `ActorUserId`
* `Action`
* `ResourceType`
* `ResourceId`
* `AggregateType`
* `AggregateId`
* `AggregatePublicId`
* `AggregateVersion`
* `OccurredAtUtc`
* `IngestedAtUtc`

These fields support:

* dedupe
* investigation
* trace reconstruction
* governance review
* source truth correlation

They do not replace originating module truth where current business state must be confirmed.

### 15.1 Mapping stability rule

Canonical mapping should remain stable across:

* retry
* replay
* backfill
* remediation

A replay of the same canonical message should map to the same canonical audit identity.

---

## 16. Summary

Audit correctness in V1 rests on these rules:

1. Audit owns evidence truth, not originating business truth.
2. Audit is append-only and must never block the originating truth flow.
3. `MessageId` is the canonical idempotency key.
4. Duplicate delivery must not create duplicate audit facts.
5. Audit immutability is the default; corrections append new facts instead of rewriting old ones.
6. Replay must fill gaps without duplicating already-recorded evidence.
7. Timestamps help investigations, but correlation and source module truth remain necessary.
8. Audit lag is acceptable only if it is visible, recoverable, and governable.
9. No global ordering or distributed transaction is assumed for Audit workflows.
10. Business truth and audit evidence are linked causally through Outbox-backed messages.
11. `OutboxMessage.Status` and `AuditIngestion.Status` are separate state machines.
12. Dashboards, summaries, reports, alerts, digests, and completeness views are derived, not evidence truth.
13. Reconciliation and replay support audit completeness, but do not rewrite canonical evidence.
14. Future derived audit workflows must be bounded and rerun-safe.
15. Retry, duplicate delivery, replay, and bounded backfill are normal and must remain safe.
16. Redis is optional and derived-only; SQL remains audit evidence truth.
17. Audit V1 is partition-ready, not physically partitioned by default.
18. Safe non-progress is preferable to unsafe synthetic evidence mutation.
19. Singleton/ownership semantics are not relied on unless explicitly protected by authoritative generation/fencing rules.
