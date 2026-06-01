# Audit — Runtime Flows (V1)

Audit is a Worker-driven, asynchronous evidence module.

Audit consumes audit-relevant messages after producer module truth has already committed. It does not participate in the originating business transaction and must not block source module commands.

Related:

* `../../../../architecture/arc42/04-runtime-view-v1.md`
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

## 1. Runtime Posture in V1

Audit V1 has one required runtime lane and several future extension lanes.

### 1.1 Required V1 lane — async ingestion lane

Used for:

* consuming audit-relevant messages after originating truth commits
* mapping Outbox integration messages into normalized audit evidence
* applying redaction and minimal-payload enforcement
* durable `MessageId` deduplication
* writing append-only `AuditLog`
* tracking consumer-side processing in `AuditIngestion`
* exposing audit evidence for investigation and operations

### 1.2 Required V1 query lane — investigation reads

Used for:

* audit log list/detail
* lookup by `PublicId`
* lookup by `MessageId`
* lookup by `CorrelationId`
* module-specific logs
* resource timeline
* actor timeline
* recent high-risk events
* lightweight dashboard summaries directly from `AuditLog`

V1 dashboard queries read from SQL-backed `AuditLog` using indexes. V1 does not require materialized dashboard summary tables.

### 1.3 Deferred extension lanes

The following lanes are valid future extension paths but are not required for V1 core delivery:

* replay / reprocess workflow
* DLQ remediation workflow
* audit completeness reconciliation
* archival / retention workflow
* summarization / digest workflow
* alert rule pipeline
* notification integration through Notifications
* Redis acceleration for non-authoritative dashboard panels
* physical data partitioning or worker lane partitioning

These extensions must not change the core ingestion contract.

---

## 2. Global Runtime Rules

The following rules apply to all Audit runtime flows.

### 2.1 Audit is downstream of source truth

A producer command succeeds when:

* the producer module truth change commits
* and the Outbox message commits in the same local transaction when async work is required

A producer command does not wait for:

* RabbitMQ publication
* Audit consumer processing
* `AuditLog` insertion
* dashboard update
* notification delivery
* reporting or summary generation

### 2.2 Audit is eventually consistent

Audit ingestion may lag behind source module truth.

Temporary Audit lag is acceptable only if it remains:

* observable
* retryable
* recoverable
* investigation-safe

Audit APIs do not provide read-your-writes guarantees for just-committed source actions.

### 2.3 Audit owns evidence, not source business truth

Audit owns SQL-backed append-only evidence.

Audit does not own:

* current user account state
* current role/permission assignment state
* current article publication state
* current media attachment truth
* current comment/moderation truth
* current SEO routing truth
* notification delivery truth

Absence of audit evidence must not be used as proof that a source business action did not happen.

### 2.4 MessageId is the canonical runtime identity

Audit uses `MessageId` from `[outbox].[OutboxMessage]` as the canonical async message identity.

`MessageId` is used for:

* dedupe
* retry safety
* replay safety
* outbox-to-audit tracing
* worker and consumer diagnostics

Audit V1 must not use the old local alias `EventId` for this concept.

### 2.5 Producer-side and consumer-side states are separate

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

Important rule:

`OutboxMessage.Status = Published` means broker handoff succeeded. It does not mean Audit processed the message.

---

## 3. Flow A — Source Command Emits Auditable Message

### Goal

Commit producer module truth and record the intent to publish an audit-relevant event without calling Audit synchronously.

### Runtime flow

```text
API/Admin command
    ↓
Producer module validates and authorizes command
    ↓
Producer module loads its own truth state
    ↓
Producer module applies business mutation
    ↓
Producer module writes OutboxMessage in the same local transaction
    ↓
Producer module commits transaction
    ↓
Command returns success based on producer truth commit
```

### Required producer-side behavior

Producer modules must write the Outbox message in the same local transaction as the truth mutation when the event is required for async side effects.

The Outbox message contains:

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

### Runtime rules

* Producer modules must not call Audit synchronously.
* Producer modules must not write directly to Audit tables.
* Producer modules must not wait for Audit ingestion.
* Producer module success is not defined by downstream side-effect completion.
* Producer-side timeout/retry behavior belongs to the producer and Outbox publisher, not to Audit.

### Failure modes

#### Producer truth commit fails

Result:

* no business success
* no valid source truth mutation
* no audit expectation unless the failed attempt itself is intentionally audited by producer policy

#### Producer truth commit succeeds but Outbox commit fails

This should not happen if truth and Outbox are in the same local transaction.

If it occurs due to implementation defect, it is a producer-side consistency incident.

#### Outbox row committed but not yet published

Result:

* producer command may already be successful
* Audit has not seen the message yet
* backlog/oldest pending age must make the delay observable

---

## 4. Flow B — Outbox Publishes Message to RabbitMQ

### Goal

Move committed Outbox messages from SQL to RabbitMQ without redefining producer truth or downstream completion.

### Runtime flow

```text
Outbox publisher worker polls eligible OutboxMessage rows
    ↓
Worker claims message
    ↓
Worker publishes message to RabbitMQ
    ↓
Broker handoff succeeds
    ↓
OutboxMessage.Status becomes Published
```

### Runtime rules

* Outbox publishing is producer-side publication work.
* Outbox `Published` means broker handoff succeeded.
* Outbox `Published` does not mean Audit consumed the message.
* Producer-side publish failure must not be confused with Audit consumer failure.
* Worker retry must be bounded and observable.

### Failure modes

#### Publish timeout

Timeout is ambiguous.

The publisher cannot assume the broker did nothing. Producer-side retry policy and `MessageId` stability protect against duplicate downstream delivery.

#### Publish fails before broker handoff

Outbox state may become:

* `Failed`
* eventually `Dead` after retry exhaustion

This is not an Audit consumer failure.

#### Broker receives duplicate publish

Audit consumer must still dedupe by `MessageId`.

---

## 5. Flow C — Audit Ingests Message from RabbitMQ

### Goal

Persist durable audit evidence from an already-committed and broker-published producer message.

### Runtime flow

```text
RabbitMQ delivers message
    ↓
AuditRabbitMqConsumerService receives message
    ↓
Message is deserialized into the Outbox integration envelope
    ↓
AuditEventNormalizerDispatcher selects normalizer by EventType
    ↓
Concrete IAuditEventNormalizer maps envelope + payload to audit command
    ↓
AuditIngestionService starts consumer-side processing
    ↓
Audit checks MessageId dedupe
    ↓
Audit inserts AuditLog if not already present
    ↓
Audit updates AuditIngestion status
    ↓
Consumer ACKs after success or duplicate-safe processing
```

### Runtime components

Recommended V1 components:

* `AuditRabbitMqConsumerService`
* `AuditEventNormalizerDispatcher`
* `IAuditEventNormalizer`
* `AuditIngestionService`
* `IAuditLogRepository`
* `IAuditIngestionRepository`

### Mapping rules

Audit maps Outbox fields into Audit domain fields.

| Outbox field        | Audit usage                                           |
| ------------------- | ----------------------------------------------------- |
| `MessageId`         | `AuditLog.MessageId`, `AuditIngestion.MessageId`      |
| `EventType`         | event type and normalizer selection                   |
| `AggregateType`     | aggregate traceability and default resource type      |
| `AggregateId`       | aggregate traceability                                |
| `AggregatePublicId` | preferred resource identity when available            |
| `AggregateVersion`  | aggregate chronology when available                   |
| `Payload`           | sanitized payload / metadata / mapped business fields |
| `Headers`           | sanitized headers / metadata                          |
| `CorrelationId`     | trace correlation                                     |
| `InitiatorUserId`   | actor internal id when available                      |
| `Priority`          | source priority / metadata                            |
| `OccurredAt`        | source event time                                     |

### Normalizer-derived fields

The selected normalizer derives:

* `SourceModule`
* `Action`
* `ActionCategory`
* `ResourceType`
* `ResourceId`
* `ResourceDisplayName`
* `ActorUserId`
* `ActorEmail`
* `ActorDisplayName`
* `ActorType`
* `Outcome`
* `Severity`
* `RiskLevel`
* `Summary`
* `MetadataJson`
* `BeforeJson`
* `AfterJson`
* `ChangesJson`

### ACK/NACK rules

| Condition                             | Consumer behavior                                         |
| ------------------------------------- | --------------------------------------------------------- |
| AuditLog inserted successfully        | ACK                                                       |
| Same `MessageId` already exists       | Treat as duplicate-safe and ACK                           |
| Event intentionally ignored by policy | Mark `Ignored` and ACK                                    |
| Transient persistence failure         | Retry with bounded backoff                                |
| Timeout during DB write               | Treat as ambiguous; retry safely using `MessageId` dedupe |
| Invalid payload or unsupported schema | Mark failure and route to retry/DLQ policy                |
| Poison message after retry exhaustion | Mark `DeadLettered` or use terminal remediation path      |

### Runtime semantics

* Incoming message is a cause record, not yet audit evidence.
* `AuditLog` becomes audit evidence after durable SQL persistence.
* The same `MessageId` must produce at most one `AuditLog`.
* Replay is allowed and must be safe.
* Duplicate delivery is expected.
* Different `MessageId` values are not collapsed by default.
* Out-of-order delivery may still be recorded as historical evidence.
* Audit does not claim global total ordering.

### Failure modes

#### Consumer crash before ACK

RabbitMQ may redeliver the message.

Audit must dedupe by `MessageId`.

#### Timeout during Audit DB insert

Timeout is ambiguous.

The previous insert may have succeeded.

Retry must rely on SQL uniqueness for `MessageId`.

#### Duplicate delivery

The consumer must not insert a second `AuditLog`.

The duplicate may be recorded as `AuditIngestion.Status = Duplicate` or treated as idempotent success according to implementation policy.

#### Mapping or redaction failure

Failure must be visible.

The message should follow retry or DLQ policy.

#### Audit DB unavailable

Core producer flows remain valid if their truth and Outbox commits already succeeded.

Audit ingestion lag grows and must be observable.

---

## 6. Flow D — AuditIngestion Status Tracking

### Goal

Track consumer-side processing independently from producer-side Outbox publication.

### Runtime flow

```text
Audit receives message
    ↓
Create or load AuditIngestion by MessageId
    ↓
Mark Processing / increment AttemptCount
    ↓
Process normalizer and AuditLog insert
    ↓
Mark Succeeded, Duplicate, Ignored, Failed, or DeadLettered
```

### Status meanings

| Status         | Meaning                                                                       |
| -------------- | ----------------------------------------------------------------------------- |
| `Processing`   | Audit has received and is processing the message                              |
| `Succeeded`    | Audit processed the message and persisted evidence or completed intentionally |
| `Duplicate`    | Message was already represented by existing evidence                          |
| `Ignored`      | Message was intentionally ignored by policy                                   |
| `Failed`       | Audit consumer processing failed and may retry                                |
| `DeadLettered` | Message reached terminal consumer-side failure handling                       |

### Runtime rules

* `AuditIngestion.MessageId` should be unique.
* `AuditIngestion` must not use `Published` as a status.
* `Published` belongs to the Outbox state machine.
* AuditIngestion failures must not mutate Outbox state.
* AuditIngestion allows operators to distinguish producer-side publication from consumer-side processing.

### Operational examples

#### Case 1 — Outbox published, Audit succeeded

```text
OutboxMessage.Status = Published
AuditIngestion.Status = Succeeded
AuditLog exists
```

#### Case 2 — Outbox published, Audit failed

```text
OutboxMessage.Status = Published
AuditIngestion.Status = Failed
AuditLog may not exist
```

#### Case 3 — Outbox published, Audit duplicate-safe

```text
OutboxMessage.Status = Published
AuditIngestion.Status = Duplicate
AuditLog already exists for MessageId
```

#### Case 4 — Outbox pending, Audit has not seen message

```text
OutboxMessage.Status = Pending/Failed
AuditIngestion does not exist
AuditLog does not exist
```

---

## 7. Flow E — Ingest Authorization Governance Change

### Goal

Persist audit evidence for role, permission, and governance-sensitive Authorization changes.

### Authorization events consumed by Audit in V1

Baseline V1 events:

* `authorization.user_role_assigned`
* `authorization.user_role_revoked`
* `authorization.role_permission_granted`
* `authorization.role_permission_revoked`

Future Authorization events may include:

* `authorization.role_created`
* `authorization.role_updated`
* `authorization.role_deleted`
* `authorization.permission_created`
* `authorization.permission_updated`
* `authorization.permission_deleted`

### Runtime flow

```text
Authorization command validates and authorizes request
    ↓
Authorization commits governance truth change
    ↓
Authorization writes OutboxMessage in same local transaction
    ↓
Outbox worker publishes message
    ↓
Audit consumer receives message
    ↓
Authorization audit normalizer maps payload into AuditLog fields
    ↓
AuditIngestionService applies dedupe + redaction
    ↓
AuditLog is inserted
    ↓
AuditIngestion marked Succeeded
    ↓
Consumer ACKs
```

### Mapping rules

Common Authorization mapping:

| Audit field         | Source                                                   |
| ------------------- | -------------------------------------------------------- |
| `MessageId`         | Outbox `MessageId`                                       |
| `EventType`         | Outbox `EventType`                                       |
| `SourceModule`      | `Authorization`                                          |
| `Action`            | Event-specific normalizer                                |
| `ActionCategory`    | `Authorization`                                          |
| `AggregateType`     | Outbox `AggregateType`                                   |
| `AggregateId`       | Outbox `AggregateId`                                     |
| `AggregatePublicId` | Outbox `AggregatePublicId`                               |
| `AggregateVersion`  | Outbox `AggregateVersion`                                |
| `ResourceType`      | `UserRole` or `RolePermission`                           |
| `ResourceId`        | Prefer `AggregatePublicId`; otherwise composite id       |
| `ActorInternalId`   | Outbox `InitiatorUserId` or payload actor id             |
| `ActorUserId`       | actor public id from payload/headers when available      |
| `CorrelationId`     | Outbox `CorrelationId`                                   |
| `OccurredAtUtc`     | Outbox `OccurredAt`                                      |
| `Outcome`           | `Success` unless payload says otherwise                  |
| `Severity`          | usually `Warning`                                        |
| `RiskLevel`         | `High` or `Critical` depending on permission sensitivity |
| `MetadataJson`      | safe subset of payload                                   |

### Runtime rules

* Authorization must not write directly to Audit tables.
* Authorization must not call Audit synchronously.
* Authorization command success is based on Authorization truth commit and Outbox commit.
* Audit lag does not roll back or redefine Authorization truth.
* Governance audit evidence must dedupe by `MessageId`.
* Different `MessageId` values are not collapsed by default.

### Failure modes

#### Consumer restart during processing

Redelivery is expected.

Audit dedupes by `MessageId`.

#### Mapping schema drift

Failure must be visible and routed through retry/DLQ policy.

#### Authorization truth committed but Audit consumer down

Authorization truth remains valid.

Audit ingestion lag grows and must be visible through operational metrics.

---

## 8. Flow F — Query Audit Logs

### Goal

Support investigation and operational visibility from SQL-backed audit evidence.

### Runtime flow

```text
Admin calls Audit API
    ↓
API validates authorization and query parameters
    ↓
Use case builds bounded query
    ↓
Repository reads AuditLog / AuditIngestion from SQL
    ↓
API returns paged or detailed result
```

### Supported V1 query patterns

V1 should support:

* list audit logs with pagination
* get audit log by `PublicId`
* get audit log by `MessageId`
* get audit logs by `CorrelationId`
* filter by `SourceModule`
* filter by `Action`
* filter by `ActorUserId`
* filter by `ResourceType`
* filter by `ResourceId`
* filter by `Outcome`
* filter by `Severity`
* filter by `RiskLevel`
* resource timeline
* actor timeline
* recent high-risk events
* lightweight dashboard summary
* failed ingestion list

### Query rules

* Production queries must be bounded.
* List and timeline queries should support `fromUtc`, `toUtc`, `page`, and `pageSize`.
* Internal numeric primary keys should not be exposed as stable API identifiers.
* `PublicId` should be used for Audit-owned record detail APIs.
* `MessageId` should be used for outbox-to-audit tracing.
* Investigation-critical detail reads must read from SQL-backed evidence.

### Timeline ordering

Timelines may support:

* source chronology by `OccurredAtUtc`
* ingestion chronology by `IngestedAtUtc`
* aggregate chronology by `AggregateVersion` when available

Audit does not provide global total ordering.

---

## 9. Flow G — Lightweight Dashboard Summary

### Goal

Provide V1 operational visibility without materialized summary tables or batch jobs.

### Runtime flow

```text
Admin opens Audit dashboard
    ↓
API requests summary data
    ↓
Use case queries AuditLog / AuditIngestion with bounded time window
    ↓
SQL aggregates counts by module, action, severity, risk level, or ingestion status
    ↓
Dashboard renders summary
```

### V1 dashboard examples

* total audit events in selected window
* events by module
* events by severity
* events by risk level
* recent high-risk events
* failed ingestion count
* duplicate ingestion count
* oldest failed ingestion age

### Runtime rules

* V1 dashboard summaries are derived views.
* Dashboard summaries do not replace `AuditLog` evidence.
* V1 does not require materialized summary tables.
* V1 does not require batch jobs for dashboard summary.
* Dashboard queries should be bounded by time range.

### Future extension

If dashboard queries become expensive, future versions may introduce:

* Redis cache for non-authoritative dashboard panels
* materialized audit summaries
* time-window summary jobs
* dashboard invalidation/version keys
* archive-aware summaries

Future dashboard acceleration must remain derived-only and must not replace SQL-backed `AuditLog` as evidence truth.

---

## 10. Deferred Flow H — Replay / Reprocess Failed Audit Message

### Status

Deferred extension.

Not required for V1 core delivery.

### Goal

Recover from failed, lagged, or dead-lettered audit ingestion without duplicating already-persisted audit evidence.

### Typical workflow shape

```text
Select bounded replay input
    ↓
Read failed/DLQ/retained message set
    ↓
Preserve original MessageId
    ↓
Run same normalizer and ingestion logic
    ↓
Insert missing evidence only
    ↓
Record replay outcome
```

### Possible input scopes

* failed ingestion records
* DLQ window
* retained Outbox window
* targeted message id list
* bounded governance-event comparison set

### Runtime rules

* Replay must preserve `MessageId`.
* Replay must not mutate existing `AuditLog`.
* Replay must not create duplicate evidence.
* Replay overlap with live ingestion must remain safe through `MessageId` dedupe.
* Replay must be bounded and observable.

### Replay outcomes

* `inserted`
* `deduped`
* `ignored`
* `invalid_payload`
* `still_failing`
* `escalated`

---

## 11. Deferred Flow I — Audit Completeness Reconciliation

### Status

Deferred extension.

Not required for V1 core delivery.

### Goal

Detect possible gaps between expected auditable events and persisted audit evidence.

### Typical workflow shape

```text
Select bounded expected-event scope
    ↓
Compare expected MessageIds against AuditLog / AuditIngestion
    ↓
Produce mismatch candidate set
    ↓
Validate candidate set
    ↓
Trigger replay or operator-visible investigation output
    ↓
Record reconciliation outcome
```

### Typical outputs

* mismatch reports
* replay candidate sets
* completeness summaries
* investigation handoff reports

### Rules

* Reconciliation output is derived, not evidence truth.
* Missing-evidence detection must not silently mutate historical audit rows.
* Absence of audit evidence must not be treated as proof that source business truth did not happen.
* Replay or corrective handling must remain bounded and observable.
* If mismatch detection is uncertain, prefer investigation or replay over unsafe synthetic evidence insertion.

---

## 12. Deferred Flow J — Audit Archival / Summarization

### Status

Deferred extension.

Not required for V1 core delivery.

### Goal

Apply retention policy and generate derived reporting outputs without weakening evidence integrity.

### Typical workflow shape

```text
Select bounded historical audit window
    ↓
Generate candidate archive or summary output
    ↓
Validate candidate output
    ↓
Publish archive or summary according to policy
    ↓
Record completion
```

### Typical outputs

* archive partitions
* governance summaries
* reporting views
* derived timeline/reporting datasets

### Rules

* `AuditLog` remains canonical append-only evidence truth.
* Dashboards, summaries, reports, and archives are derived outputs.
* Partial derived output must not be treated as complete report state.
* Archival or purge must follow explicit retention policy.
* Do not silently destroy evidence required by retention rules.
* Full recompute of derived summaries is acceptable when safer than fragile patch repair.

---

## 13. Deferred Flow K — Audit Alert and Notification Extension

### Status

Deferred extension.

Not required for V1 core delivery.

### Goal

Allow Audit to produce admin-facing alerts or notification requests without sending notifications directly.

### Realtime alert extension shape

```text
AuditLog inserted
    ↓
Optional realtime audit rule evaluator
    ↓
Create AuditAlert if rule matches
    ↓
Optional AuditAlertRaised outbox event
    ↓
Notifications module performs delivery
```

### Window-based alert extension shape

```text
Select bounded AuditLog window
    ↓
Run window-based audit rules
    ↓
Create AuditAlert or AuditDigest
    ↓
Optional AuditAlertRaised event
    ↓
Notifications module performs delivery
```

### Rules

* Audit must not send emails directly.
* Notifications module owns notification delivery.
* Alert creation must be idempotent.
* Window-based alerting must be bounded and rerun-safe.
* Alert/digest outputs are derived and must not replace `AuditLog`.

---

## 14. Deferred Flow L — Cache Acceleration Extension

### Status

Deferred extension.

Not required for V1 core delivery.

### Goal

Improve dashboard or metadata query performance without making Redis part of audit correctness.

### Possible cached views

* dashboard summary
* recent high-risk event panel
* module/action metadata
* TTL-bound alert dedupe hints

### Rules

* Redis must not be audit evidence truth.
* Redis must not be the only dedupe mechanism for critical AuditLog append behavior.
* Investigation-critical detail APIs must be able to read directly from SQL.
* Cache entries must be safely bypassable.
* Cache TTLs must be short and policy-defined.

---

## 15. Deferred Flow M — Partitioning and Worker Lane Extension

### Status

Deferred extension.

Not required for V1 core delivery.

### Goal

Scale Audit processing or data management when metrics justify stronger partitioning.

### Future partitioning candidates

* worker lane by source module
* worker lane by risk priority
* separate queue for high-risk events
* separate queue for interaction-heavy audit events
* time-range partitioning for `AuditLog`
* batch/window partitioning for summaries, archive, digest, or reconciliation

### Rules

* Audit V1 is partition-ready, not physically partitioned by default.
* Future partitioning must be signal-driven.
* Future partitioning must not change `MessageId` dedupe semantics.
* Future partitioning must not change append-only evidence semantics.
* Future routing should use logical lane indirection rather than direct `hash(key) mod N`.

### Signal examples

* audit query P95/P99 degradation
* growing AuditLog table/index pressure
* audit consumer lag
* oldest uningested message age
* retry or DLQ pressure
* security audit delayed by high-volume non-security events
* archive/report windows becoming too slow

---

## 16. Truth-Safe Audit Evidence Under Lag

### Goal

Ensure audit lag never rewrites the meaning of already committed business truth while still preserving investigation integrity.

### Runtime shape

```text
Originating module commits truth and writes Outbox
    ↓
Outbox worker may lag before publishing
    ↓
RabbitMQ may deliver later or redeliver
    ↓
Audit may lag due to queue backlog, worker outage, or DB failure
    ↓
Business truth remains valid
    ↓
Audit eventually converges through ingestion, retry, replay, or remediation
```

### Rules

* Missing audit evidence temporarily does not undo upstream business truth.
* Audit completeness is operationally important but not part of upstream synchronous success.
* Investigation tooling should distinguish:

  * truth committed but Outbox not yet published
  * Outbox published but Audit consumer lagged
  * Audit processing failed after broker handoff
  * Audit later recovered through retry or replay
  * true unresolved missing evidence

### Examples

* Role assignment succeeded while Audit queue was backlogged.
* Permission grant succeeded but Audit row appeared later.
* Content publish succeeded but Audit row appeared after consumer recovery.
* Replay filled an earlier evidence gap after outage recovery.

---

## 17. Observability Notes

Audit runtime must expose enough signals to distinguish producer-side publication, broker delivery, and consumer-side processing.

V1 should track:

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

---

## 18. Summary

Audit runtime in V1 is governed by these rules:

1. Audit is downstream of source truth and never blocks the originating business action.
2. Producer modules commit truth and Outbox locally; Audit consumes later.
3. `MessageId` is the canonical dedupe and trace identity.
4. The same `MessageId` should produce at most one `AuditLog`.
5. Duplicate delivery must dedupe; replay must fill gaps without rewriting history.
6. Audit evidence is SQL-backed, append-only evidence truth.
7. Async ingestion is at-least-once; retry, replay, lag, and worker restart are normal.
8. Audit accepts out-of-order delivery as historical evidence.
9. `OutboxMessage.Status` and `AuditIngestion.Status` are separate state machines.
10. Dashboard summaries are lightweight derived views in V1.
11. Batch, replay, reconciliation, archival, alerts, notifications, cache acceleration, and partitioning are future extension paths.
12. Partial repair/report/cache outputs must not be mistaken for canonical evidence.
13. Safe non-progress is preferable to unsafe synthetic evidence mutation.
14. Audit lag is acceptable temporarily only if it remains observable, recoverable, and investigation-safe.
