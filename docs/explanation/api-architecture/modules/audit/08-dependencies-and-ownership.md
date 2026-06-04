# Audit — Dependencies & Ownership (V1)

## 1. Purpose

This document defines the dependency and ownership boundaries for the Audit module in CommercialNews V1.

Audit is an asynchronous, SQL-backed, append-only evidence module. It consumes audit-relevant messages after source module truth has already committed, records investigation-ready evidence, and exposes admin query contracts for governance and operational review.

Audit V1 intentionally keeps the runtime scope simple:

* `AuditLog`
* `AuditIngestion`
* `MessageId` deduplication
* Outbox message consumption
* event normalizer strategies
* investigation query APIs
* lightweight dashboard queries from SQL

Future capabilities such as alerting, digest generation, materialized summaries, replay, reconciliation, archival, notification integration, Redis acceleration, and partitioning are allowed extension paths, but they are not required for V1 core delivery.

Related:

* `../../../../architecture/arc42/03-building-blocks-modularity.md`
* `../../../../architecture/arc42/10-system-data.md`
* `../../../../architecture/arc42/11-replication-v1.md`
* `../../../../architecture/arc42/13-transactions-and-consistency-v1.md`
* `../../../../architecture/arc42/16-batch-processing-and-derived-data-v1.md`
* `../../../../architecture/arc42/17-dataflow-and-batch-workflows-v1.md`
* `../../../../architecture/arc42/18-stream-processing-and-derived-state-v1.md`
* `../../../../architecture/arc42/19-stream-processing-runtime-v1.md`
* `../../../../decisions/adr-0011-replication-topology-v1.md`
* `../../../../decisions/adr-0012-data-store-placement-v1.md`
* `../../../../decisions/adr-0013-outbox-and-delivery-semantics-v1.md`
* `../../../../decisions/adr-0014-public-identifier-strategy-v1.md`
* `../../../../decisions/adr-0015-cache-policy-and-invalidation-v1.md`
* `../../../../decisions/adr-0017-partitioning-strategy-v1.md`
* `../../../../decisions/adr-0018-transaction-boundaries-and-consistency-model-v1.md`
* `../../../../decisions/adr-0020-timeout-retry-and-failure-detection-policy-v1.md`
* `../../../../decisions/adr-0025-batch-processing-and-derived-state-policy-v1.md`
* `../../../../decisions/adr-0026-batch-job-orchestration-and-materialization-policy-v1.md`
* `../../../../decisions/adr-0027-stream-processing-and-derived-state-policy-v1.md`
* `../../../../decisions/adr-0028-consumer-idempotency-replay-and-rebuild-policy-v1.md`

---

## 2. Ownership Boundaries

### 2.1 Audit owns

Audit owns:

* audit evidence persistence
* SQL-backed append-only `AuditLog`
* consumer-side `AuditIngestion`
* `MessageId` deduplication policy
* audit redaction policy
* audit normalizer contracts
* audit investigation query contracts
* audit operational visibility contracts
* lightweight dashboard query contracts

Audit owns evidence about recorded events. It does not own the source modules’ business truth.

### 2.2 Audit does not own

Audit does not own:

* domain action execution in producer modules
* producer module truth state
* producer-side Outbox creation
* producer-side Outbox publishing
* RabbitMQ broker state
* notification delivery
* authorization decisions
* current business state of investigated resources
* source module repair or rebuild logic
* cache correctness for source module reads
* publication visibility truth
* identity security truth
* SEO routing truth

Examples:

* Identity owns whether a user is currently locked.
* Audit owns evidence that a `UserLocked` message was recorded.
* Authorization owns current role and permission assignment truth.
* Audit owns evidence that a role or permission assignment change was recorded.
* Content owns current article lifecycle truth.
* Audit owns evidence that a content lifecycle event was recorded.
* Notifications owns delivery-state truth.
* Audit may record delivery failures as evidence, but does not own delivery execution.

### 2.3 Ownership rule

Audit owns evidence of what was recorded.

Audit does not own whether the source domain action is currently valid.

Current business state must be verified from the owning module’s truth store.

---

## 3. Source Module Dependency

### 3.1 Dependency shape

Audit depends on source modules only through integration messages emitted after source truth commits.

Approved source-to-Audit flow:

```text id="c5yel1"
Source module truth mutation
    ↓
OutboxMessage written in same local transaction
    ↓
Outbox publisher sends message to RabbitMQ
    ↓
CommercialNews.Worker audit consumer receives message
    ↓
Worker sends IngestAuditEventCommand through MediatR
    ↓
Audit.Application selects a registered normalizer and stores evidence
```

### 3.2 Producer modules own

Producer modules own:

* business truth mutation
* event contract correctness
* event payload shape
* event versioning
* `AggregateType`
* `AggregateId`
* `AggregatePublicId`
* `AggregateVersion`
* `OccurredAt`
* `InitiatorUserId`
* Outbox creation in the same transaction as the truth mutation

### 3.3 Audit owns after consumption

After Audit receives the message, Audit owns:

* normalizer abstraction and registry selection
* redaction
* mapping to audit fields
* `MessageId` dedupe
* `AuditLog` persistence
* `AuditIngestion` status
* audit query visibility

Runtime placement:

* `CommercialNews.Worker` owns broker consumption and maps messages into `IngestAuditEventCommand`.
* Audit.Application owns MediatR handlers, FluentValidation validators, pipeline behaviors, use-case orchestration, and normalizer abstractions.
* Audit.Infrastructure owns concrete normalizers and persistence adapters.
* Concrete normalizers are registered in Infrastructure DI.

Expected concrete Infrastructure normalizers:

* `AuthorizationAuditEventNormalizer`
* `IdentityAuditEventNormalizer`
* `ContentAuditEventNormalizer`
* `MediaAuditEventNormalizer`
* `InteractionAuditEventNormalizer`

If no normalizer is registered for a received `EventType`, Audit treats the
event as unsupported and records an ignored ingestion outcome.

### 3.4 Forbidden source-module coupling

Source modules must not:

* call Audit synchronously during command execution
* write directly to Audit tables
* wait for Audit ingestion before returning success
* use Audit success as a condition for source truth success
* use Audit failure as a reason to roll back already-committed source truth
* mutate Audit-owned evidence tables because the database is physically reachable

Audit must not:

* mutate source module truth
* reinterpret source module current state
* write corrective state into source module tables
* perform hidden cross-module truth mutation

---

## 4. Outbox and Broker Dependency

### 4.1 Outbox dependency

Audit consumes messages originating from:

```text id="xp4lq0"
[outbox].[OutboxMessage]
```

Important fields include:

* `MessageId`
* `EventType`
* `AggregateType`
* `AggregateId`
* `AggregatePublicId`
* `AggregateVersion`
* `Payload`
* `Headers`
* `CorrelationId`
* `InitiatorUserId`
* `Priority`
* `OccurredAt`
* `PublishedAt`

### 4.2 MessageId ownership

The system-wide async message identity is:

```text id="suw6mp"
MessageId
```

Audit uses `MessageId` for:

* dedupe
* replay safety
* retry safety
* traceability
* linking AuditLog, AuditIngestion, Outbox, Worker logs, and broker delivery

Audit V1 should not use the old local alias `EventId` for this concept.

### 4.3 Producer-side and consumer-side state separation

`OutboxMessage.Status` is producer-side publication state.

Examples:

* `Pending`
* `Publishing`
* `Published`
* `Failed`
* `Dead`

`AuditIngestion.Status` is consumer-side processing state.

Examples:

* `Processing`
* `Succeeded`
* `Duplicate`
* `Ignored`
* `Failed`
* `DeadLettered`

Rule:

`OutboxMessage.Status = Published` means broker handoff succeeded. It does not mean Audit consumed or processed the message.

### 4.4 Broker dependency

RabbitMQ is a delivery mechanism.

RabbitMQ is not:

* audit evidence truth
* permanent audit history
* universal replay source
* proof that downstream consumers completed their work

Audit recovery and investigation must rely on durable sources such as:

* `AuditLog`
* `AuditIngestion`
* retained Outbox records where policy allows
* source module truth/history where appropriate
* bounded future reconciliation workflows

---

## 5. Audit-Owned Data

### 5.1 AuditLog

Audit owns `AuditLog` as append-only evidence truth.

`AuditLog` records:

* which `MessageId` was recorded
* source module
* event type
* normalized action
* actor
* resource
* outcome
* severity
* risk level
* summary
* correlation identifiers
* occurrence time
* ingestion time
* sanitized payload or metadata

`AuditLog` must not be silently mutated by normal business workflows.

### 5.2 AuditIngestion

Audit owns `AuditIngestion` as consumer-side processing state.

`AuditIngestion` records:

* whether Audit has seen a `MessageId`
* whether processing succeeded
* whether the message was duplicate-safe
* whether it failed
* whether it was ignored
* whether it reached terminal failure handling

`AuditIngestion` is not producer-side Outbox state.

### 5.3 Audit-owned derived outputs

Future Audit-owned derived outputs may include:

* `AuditAlert`
* `AuditDigest`
* `AuditDailySummary`
* `AuditReportRun`
* `AuditArchiveRun`
* completeness reports
* replay candidate sets
* timeline materializations
* cached dashboard panels

These are not required for V1 core delivery.

If introduced, they must remain:

* explicitly documented as derived
* subordinate to `AuditLog`
* rebuildable or reproducible where practical
* observable
* bounded where batch/window-based
* safe under rerun/replay

---

## 6. Truth vs Derived Ownership

### 6.1 Audit truth

Audit truth:

* `AuditLog`
* `AuditIngestion`

### 6.2 Audit-derived outputs

Audit-derived outputs:

* dashboard summaries
* recent-risk panels
* alerts
* digests
* reports
* archive indexes
* reconciliation results
* replay candidate sets
* cached dashboard views

### 6.3 Ownership consequence

A derived Audit output may help operators:

* find gaps
* inspect trends
* prioritize remediation
* browse summaries faster
* detect suspicious patterns
* prepare reports

It does not become:

* canonical evidence truth
* proof that upstream business state is current
* authority to mutate source module truth
* authority to replace canonical `AuditLog`
* authority to silently correct historical evidence

---

## 7. Allowed Dependencies

Audit may depend on:

* SQL Server for Audit-owned persistence
* shared Outbox message contract
* RabbitMQ for message delivery
* Worker infrastructure for async consumption
* module integration event contracts
* platform clock abstraction
* public ID generator for Audit-owned `PublicId`
* logging and metrics infrastructure
* optional future Redis cache for non-authoritative dashboard acceleration
* optional future Notifications integration through Outbox events
* optional future batch scheduler or Worker lane for bounded derived workflows

### 7.1 Allowed dependency shapes

Approved V1 interaction patterns:

* async consume after source truth commit
* append-only evidence persistence
* consumer-side ingestion tracking
* bounded investigation queries
* lightweight dashboard aggregation over `AuditLog`

Approved future extension patterns:

* rule-based alert generation
* notification request through Outbox to Notifications
* bounded replay/backfill/remediation
* completeness reconciliation against bounded expected-event scope
* archival or summary generation over Audit-owned evidence
* Redis acceleration for dashboard panels or metadata
* worker lane partitioning when metrics justify it
* time-range data partitioning when metrics justify it

---

## 8. Forbidden Dependencies

Audit must not depend on:

* synchronous producer module calls for command success
* source module tables for hidden truth mutation
* Redis as audit evidence truth
* Redis-only dedupe for critical `AuditLog` append
* RabbitMQ as permanent audit history
* in-memory singleton ownership for correctness-sensitive workflows
* dashboard summaries as canonical evidence
* alert/digest/report outputs as source business truth
* notification delivery as part of AuditLog persistence
* batch job completion as part of source business success

Audit must not:

* call back into source modules during ingestion to enrich records in a way that creates tight coupling or PII risk
* silently correct or rewrite canonical evidence based on later upstream state
* infer current business truth from Audit evidence alone
* publish partial derived reporting outputs as complete
* treat replay candidate sets as already-applied evidence
* assume one Audit worker instance is a correctness guarantee

---

## 9. Public Identifier Ownership

Audit follows the system public identifier strategy.

### 9.1 Audit-owned identifiers

Audit-owned records may use internal SQL primary keys, but admin APIs should expose stable `PublicId` values.

Examples:

* `AuditLog.PublicId`
* `AuditIngestion.PublicId`

### 9.2 Source resource identifiers

Source resource references should use stable identifiers.

Preferred resource identity:

```text id="rdfdwu"
ResourceId = AggregatePublicId if present, otherwise AggregateId
```

If source events still use internal numeric IDs, Audit may preserve them in sanitized metadata, but API-facing contracts should converge toward public or opaque identifiers.

### 9.3 Actor identifiers

Outbox currently provides:

```text id="8n9pwo"
InitiatorUserId BIGINT NULL
```

Audit may store this as `ActorInternalId`.

If payload or headers provide stable actor public identity, Audit should store it as `ActorUserId`.

### 9.4 Slug

Slug is metadata, not resource identity.

Audit may store slug for investigation, but should not use slug as the primary resource identifier.

---

## 10. Correction Ownership

### 10.1 Append-only correction posture

Audit correction must be append-only by default.

If a correction is required:

* append a corrective audit record, or
* use an explicit correction model documented separately

Do not silently mutate historical audit rows in place.

### 10.2 Source event semantics

Audit should not unilaterally redefine what an upstream event means.

If upstream emits unstable, duplicated, or ambiguous event semantics:

* that is an upstream contract issue
* Audit may defend with mapping and dedupe policy
* Audit should not silently invent business meaning

### 10.3 Redaction correction

If redaction policy changes or an unsafe payload is discovered:

* follow an explicit redaction/remediation policy
* preserve evidence of the remediation itself where required
* do not perform silent uncontrolled mutation

---

## 11. Replay, Reconciliation, and Batch Ownership

### 11.1 V1 posture

Replay, reconciliation, archival, and materialized summarization are not required for V1 core delivery.

V1 must still be designed so these workflows can be added later without changing the core ingestion contract.

### 11.2 Future replay ownership

Future replay workflows may:

* reprocess failed messages
* replay DLQ messages after remediation
* reprocess retained Outbox messages where policy allows
* fill missing evidence gaps

They must not:

* create duplicate evidence
* rewrite existing `AuditLog`
* change source module truth
* rely on RabbitMQ as permanent history

### 11.3 Future reconciliation ownership

Future reconciliation workflows may:

* compare expected auditable events against `AuditLog`
* produce mismatch reports
* generate replay candidate sets
* assist operator investigation

They must not:

* treat mismatch reports as evidence truth
* infer source business truth from absence of audit evidence alone
* silently synthesize audit evidence without a real source message or approved correction policy

### 11.4 Future archival and summarization ownership

Future archival and summarization workflows may:

* generate summaries
* prepare archive outputs
* produce reporting views
* create digest inputs

They must:

* use bounded input
* remain rerun-safe
* preserve append-only evidence semantics
* keep derived outputs subordinate to `AuditLog`
* avoid exposing partial output as complete

---

## 12. Publication and Cutover Ownership for Future Derived Outputs

If Audit publishes an important derived report, digest, summary, or archival output, Audit owns:

* candidate generation
* candidate validation
* cutover/publication policy
* freshness/completeness signals
* rerun/rebuild policy

But Audit still does not own:

* originating module truth
* current business state of the resource being investigated
* notification delivery execution

### 12.1 Cutover safety rule

Derived report, digest, summary, or archival publication must ensure:

* partial output is not treated as complete
* stale candidates do not replace fresher output blindly
* canonical `AuditLog` remains queryable or explicitly governed by retention policy
* operators can distinguish evidence truth from derived presentation

---

## 13. Alerting and Notification Ownership

### 13.1 V1 posture

AuditAlert and notification delivery are not required for V1 core delivery.

V1 may show dashboard warning panels directly from SQL queries, such as:

* recent high-risk events
* failed ingestion count
* oldest failed ingestion age
* duplicate ingestion count

### 13.2 Future alert ownership

Future AuditAlert support should be rule-based and idempotent.

Audit may own:

* alert rule evaluation
* `AuditAlert` records
* alert dedupe keys
* alert state such as Open, Acknowledged, Resolved

AuditAlert remains derived from evidence and operational state.

### 13.3 Notification delivery ownership

Audit must not send emails or external notifications directly.

If Audit needs to notify admins:

```text id="0fn83m"
AuditAlert created
    ↓
Audit emits AuditAlertRaised through Outbox
    ↓
Notifications module consumes the event
    ↓
Notifications performs delivery
```

Notifications owns:

* email delivery
* provider retries
* delivery state
* delivery attempts
* notification templates

Audit owns only the alert evidence or alert intent.

---

## 14. Cache Ownership

### 14.1 V1 posture

Audit V1 does not require Redis caching.

SQL remains the source of audit evidence truth.

### 14.2 Future cache usage

Future Redis usage may be introduced only as optional acceleration for:

* dashboard summaries
* recent high-risk event panels
* module/action metadata
* TTL-bound alert dedupe hints

Redis must remain:

* derived-only
* optional
* safely bypassable
* non-authoritative

### 14.3 Forbidden cache usage

Redis must not be used as:

* AuditLog truth
* AuditIngestion truth
* the only dedupe mechanism for critical audit evidence
* a replacement for SQL investigation detail queries
* source module business truth

---

## 15. Partitioning Ownership

### 15.1 V1 posture

Audit V1 is partition-ready but not physically partitioned by default.

V1 relies on:

* SQL indexes
* bounded time-range queries
* source module metadata
* risk metadata
* consumer lag observability
* query latency observability

### 15.2 Future partitioning

Future partitioning may include:

* time-range partitioning for `AuditLog`
* worker lane partitioning by source module
* worker lane partitioning by risk priority
* queue partitioning by module, category, or priority
* batch/window partitioning for summary, digest, archive, or reconciliation workflows
* logical lane routing through indirection rather than direct `hash(key) mod N`

### 15.3 Partitioning guardrails

Future partitioning must not change:

* append-only evidence semantics
* `MessageId` dedupe
* source module truth ownership
* investigation correctness
* eventual consistency expectations

Partitioning must be signal-driven.

---

## 16. Coordination and Ownership-Sensitive Workflows

### 16.1 No singleton requirement for V1 ingestion

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

### 16.2 Future ownership-sensitive workflows

If a future Audit workflow truly requires one current owner, such as exclusive partition backfill or one-current audit repair owner, it must define:

* ownership source of truth
* monotonic generation/fencing token
* stale owner rejection
* failure/remediation behavior

Naive leader/lock patterns are not acceptable for correctness-sensitive workflows.

### 16.3 Ownership ambiguity

If ownership is ambiguous for a correctness-sensitive future workflow, Audit must prefer:

* delayed remediation
* retry later
* operator intervention
* stale-owner rejection
* continued evidence-truth preservation

over:

* unsafe dual replay
* duplicate evidence creation
* unsafe double-publish of derived reports
* contradictory repair mutation

---

## 17. Module Dependency Posture Summary

### 17.1 What Audit may expect from others

Audit may expect:

* upstream modules to emit stable post-commit auditable messages
* Outbox messages to contain stable `MessageId`
* Outbox messages to contain enough source context for mapping
* RabbitMQ to deliver at least once
* Worker infrastructure to retry within policy
* source modules to retain their own truth ownership
* Notifications to own actual notification delivery if alert delivery is introduced later

### 17.2 What others may expect from Audit

Other modules may expect:

* non-blocking evidence capture
* append-only investigation-ready persistence
* durable dedupe by `MessageId`
* consumer-side processing visibility
* bounded investigation queries
* clear distinction between evidence truth and derived summaries/reports
* future extension path for replay/reconciliation/alerting without changing source truth semantics

### 17.3 What nobody may assume

No module may assume:

* Audit proves current business truth
* Audit ingestion is complete immediately after source command success
* `OutboxMessage.Status = Published` means Audit processed the message
* a dashboard/report is stronger truth than canonical evidence
* replay candidate sets mean evidence has already been repaired
* stale archival/reporting output is authoritative
* Redis cache is Audit evidence truth
* singleton ownership is safe without explicit authoritative coordination
* RabbitMQ is permanent audit history
* exactly-once delivery exists across heterogeneous systems

---

## 18. V2 Evolution

Audit may evolve toward:

* richer compliance/reporting outputs
* `AuditAlert`
* digest generation
* stronger completeness-check workflows
* archive tiers and archive indexes
* formalized reconciliation/backfill tooling
* Redis acceleration for non-authoritative panels
* worker lane partitioning
* time-range data partitioning

If that happens, Audit must make explicit:

* which datasets are canonical evidence truth
* which outputs are derived reports/summaries
* how publication/cutover works for important derived outputs
* how replay/reconciliation preserves append-only integrity
* which workflows are operationally critical for governance confidence
* which ownership or fencing mechanisms protect exclusive workflows if needed

### 18.1 V2 constraints that remain unchanged

Even if Audit becomes more advanced:

* upstream business truth still remains upstream
* canonical evidence truth still belongs to Audit
* `MessageId` remains the canonical message identity unless a future ADR changes it
* derived reports/summaries still remain subordinate to canonical evidence
* replay/reconciliation remains a recovery mechanism, not permission to rewrite history
* Redis remains derived-only
* Notifications owns actual delivery
* source modules must not write directly into Audit tables
