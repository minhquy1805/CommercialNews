# System Data Model — Audit (V1)

> **Recommended file:** `explanation/architecture/arc42/system-data/system-data-audit-v1.md`
> **Module:** Audit
> **Purpose:** SQL-backed append-only evidence for important actions across modules, designed for investigation, governance, security review, and operational diagnostics.

---

## 0. Data System Fit

Audit is a governance-critical subsystem, but it must remain non-blocking for core business flows.

Audit V1 is intentionally simple:

* SQL-backed `AuditLog`
* SQL-backed `AuditIngestion`
* asynchronous ingestion through Outbox → RabbitMQ → Audit Consumer
* durable `MessageId` deduplication
* redaction before persistence
* investigation-ready query indexes
* lightweight dashboard queries from SQL

Audit V1 is not a full compliance/reporting platform. More advanced capabilities such as alerting, replay, reconciliation, archival, materialized summaries, cache acceleration, and partitioning are future extension paths.

---

## 1. Data Ownership

### 1.1 Audit-owned truth

Audit owns:

* append-only audit evidence
* consumer-side ingestion state
* message-level dedupe outcome
* audit redaction policy
* audit investigation query contracts
* audit operational visibility data

Audit-owned truth tables in V1:

* `audit.AuditLog`
* `audit.AuditIngestion`

### 1.2 Not owned by Audit

Audit does not own:

* Identity account truth
* Authorization role/permission truth
* Content publication truth
* Media attachment truth
* Interaction comment/report truth
* SEO routing truth
* Notifications delivery truth
* producer-side Outbox state
* RabbitMQ broker state

Audit records evidence about source-module events. It does not determine current source-module business state.

---

## 2. Data System Posture

### 2.1 Truth store

Audit truth is stored in SQL Server.

`AuditLog` is append-only evidence truth.

`AuditIngestion` is consumer-side processing truth.

### 2.2 Derived outputs

The following are derived and not required in V1:

* dashboard cache
* recent-risk panels cache
* audit summaries
* digests
* reports
* alerts
* reconciliation outputs
* replay candidate sets
* archive indexes
* search projections

Derived outputs may lag and may be rebuilt. They must not replace `AuditLog`.

### 2.3 Redis posture

Audit V1 does not require Redis.

Future Redis usage may be introduced only as optional acceleration for:

* dashboard summary cache
* recent high-risk panel cache
* module/action metadata cache
* TTL-bound alert dedupe hints

Redis must not be used as:

* AuditLog truth
* AuditIngestion truth
* the only dedupe mechanism for critical audit evidence
* replacement for SQL investigation detail queries

---

## 3. Capability to Entity Mapping

### 3.1 Audit evidence ingestion

Capability:

* consume audit-relevant messages from Outbox/RabbitMQ
* normalize event into audit evidence
* redact unsafe fields
* persist append-only evidence

Entity:

* `AuditLog`

### 3.2 Consumer-side processing tracking

Capability:

* track whether Audit has seen a `MessageId`
* distinguish success, duplicate, ignored, failed, and dead-lettered processing
* support operator diagnostics

Entity:

* `AuditIngestion`

### 3.3 Investigation queries

Capability:

* search by time range
* search by module/action
* search by actor
* search by resource
* search by `MessageId`
* search by `CorrelationId`
* view actor/resource timelines

Entity:

* `AuditLog`

### 3.4 Operational diagnostics

Capability:

* inspect failed ingestions
* inspect duplicate processing
* inspect lag and retry pressure
* support lightweight dashboard panels

Entities:

* `AuditIngestion`
* `AuditLog`

---

## 4. Entities

### 4.1 V1 must-have entities

V1 requires:

1. `AuditLog`
2. `AuditIngestion`

### 4.2 Future entities

Future extension candidates:

* `AuditAlert`
* `AuditDigest`
* `AuditDailySummary`
* `AuditReportRun`
* `AuditArchiveRun`
* `AuditReplayRun`
* `AuditReconciliationRun`
* `AuditCorrection`
* `AuditRedactionRule`

These are not required for V1 core delivery.

---

## 5. Dataflows

## 5.1 Producer side

Source modules commit their own business truth and write Outbox messages in the same local transaction.

Examples of source modules:

* Authorization
* Identity
* Content
* Media
* Interaction
* SEO
* Notifications

Source module success means:

* source truth committed
* Outbox message committed when async work is required

It does not mean:

* RabbitMQ has already received the message
* Audit has already processed the message
* AuditLog is immediately queryable

### Source contract

Audit consumes messages based on the shared Outbox contract:

```text
[outbox].[OutboxMessage]
```

Relevant fields:

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

---

## 5.2 Ingestion side

Audit Worker consumes messages from RabbitMQ.

Runtime shape:

```text
RabbitMQ delivers message
    ↓
Audit consumer receives message
    ↓
Audit creates/loads AuditIngestion by MessageId
    ↓
Audit normalizer maps event to audit fields
    ↓
Audit redacts unsafe payload fields
    ↓
Audit checks MessageId dedupe
    ↓
Audit inserts AuditLog if not already present
    ↓
Audit updates AuditIngestion status
    ↓
Consumer ACKs after success or duplicate-safe processing
```

### Failure behavior

Audit ingestion must tolerate:

* duplicate delivery
* consumer crash before ACK
* worker restart
* timeout during DB insert
* retry after ambiguous failure
* out-of-order delivery
* invalid payload
* unsupported event type
* redaction failure
* DLQ/poison messages

Rules:

* same `MessageId` must not create duplicate `AuditLog`
* timeout is ambiguous
* retry relies on SQL uniqueness
* poison messages must be visible through `AuditIngestion` and/or DLQ
* consumer-side failure must not be written back as producer-side Outbox failure

---

## 6. Relationships

### 6.1 AuditLog references source data by stable identifiers

`AuditLog` references source resources through strings, not hard foreign keys.

Recommended fields:

* `SourceModule`
* `AggregateType`
* `AggregateId`
* `AggregatePublicId`
* `AggregateVersion`
* `ResourceType`
* `ResourceId`

Design intent:

* avoid tight coupling to producer modules’ physical schemas
* support future module split or separate databases
* preserve evidence even if source schema evolves
* support stable investigation APIs

### 6.2 Actor references

Outbox currently provides:

```text
InitiatorUserId BIGINT NULL
```

Audit may store this as:

* `ActorInternalId`

If the payload or headers provide a public actor id, Audit should also store:

* `ActorUserId`

Audit should prefer stable opaque identifiers for API-facing actor filters when available.

### 6.3 No hard cross-module foreign keys

AuditLog should not require hard FK constraints into source module tables.

Reason:

* Audit must survive source schema evolution
* Audit may reference resources that were deleted, archived, or moved
* Audit must remain queryable during partial module failures
* future module extraction or database split should not break evidence history

---

## 7. Invariants

### 7.1 Append-only evidence

`AuditLog` is append-only by default.

Normal business workflows must not update or delete audit evidence.

Exceptions require explicit policy:

* retention policy
* legal/compliance purge
* redaction remediation policy
* append-only correction model

### 7.2 MessageId dedupe

`MessageId` is the canonical idempotency key.

Rules:

* `AuditLog.MessageId` must be unique
* `AuditIngestion.MessageId` should be unique
* same `MessageId` must not create duplicate evidence
* replay of the same `MessageId` must be safe

### 7.3 Different MessageIds are not collapsed by default

Audit V1 does not collapse different `MessageId` values by business intent.

Reason:

* different messages may represent different attempts
* repeated governance actions may be useful evidence
* raw audit evidence should preserve distinct emitted messages

Future business-level dedupe may be introduced only for derived outputs such as alerts, reports, digests, or noisy dashboard views.

### 7.4 Mandatory evidence fields

Each `AuditLog` should capture enough information for investigation:

* who: actor fields where available
* what: action and event type
* where: source module
* which resource: resource type and id
* when: occurred and ingested timestamps
* result: outcome
* sensitivity: severity and risk level
* traceability: message id and correlation id

### 7.5 Privacy and redaction

Audit must never store or return:

* passwords
* password hashes
* access tokens
* refresh tokens
* verification tokens
* reset tokens
* API keys
* session cookies
* raw authorization headers
* private signing keys
* connection strings
* unsafe sensitive PII

Redaction must happen before persistence.

### 7.6 Out-of-order tolerance

Audit does not require ordered arrival to remain correct.

Older source events may arrive after newer events.

Audit may still append them as historical evidence if they have distinct `MessageId` values.

Investigation views may sort by:

* `OccurredAtUtc`
* `IngestedAtUtc`
* `AggregateVersion` where available

### 7.7 Derived outputs are not evidence truth

Dashboards, summaries, reports, alerts, digests, reconciliation outputs, cache entries, and archive indexes are derived.

They must not replace `AuditLog`.

---

## 8. Logical Schema — `audit.AuditLog`

`AuditLog` stores append-only normalized audit evidence.

### 8.1 Recommended V1 columns

| Field                 |              Type | Null | Notes                                                 |
| --------------------- | ----------------: | ---: | ----------------------------------------------------- |
| `AuditLogId`          | `BIGINT IDENTITY` |   NO | Internal primary key                                  |
| `PublicId`            |        `CHAR(26)` |   NO | Audit-owned API identifier                            |
| `MessageId`           |        `CHAR(26)` |   NO | Copied from Outbox; canonical dedupe key              |
| `EventType`           |   `NVARCHAR(200)` |   NO | Original integration event type                       |
| `EventVersion`        |             `INT` |  YES | Source event contract version if available            |
| `SourceModule`        |   `NVARCHAR(100)` |   NO | Derived from event type/header/normalizer             |
| `Action`              |   `NVARCHAR(120)` |   NO | Normalized action                                     |
| `ActionCategory`      |   `NVARCHAR(100)` |  YES | e.g. Authorization, IdentitySecurity, Moderation      |
| `AggregateType`       |   `NVARCHAR(100)` |  YES | From Outbox                                           |
| `AggregateId`         |   `NVARCHAR(100)` |  YES | From Outbox                                           |
| `AggregatePublicId`   |        `CHAR(26)` |  YES | From Outbox when available                            |
| `AggregateVersion`    |             `INT` |  YES | From Outbox when available                            |
| `ResourceType`        |   `NVARCHAR(100)` |   NO | Normalized resource type                              |
| `ResourceId`          |   `NVARCHAR(100)` |   NO | Prefer AggregatePublicId if available                 |
| `ResourceDisplayName` |   `NVARCHAR(300)` |  YES | Safe display value                                    |
| `ActorInternalId`     |          `BIGINT` |  YES | From Outbox InitiatorUserId or payload                |
| `ActorUserId`         |        `CHAR(26)` |  YES | Public actor id when available                        |
| `ActorEmail`          |   `NVARCHAR(320)` |  YES | Optional; use cautiously                              |
| `ActorDisplayName`    |   `NVARCHAR(200)` |  YES | Optional                                              |
| `ActorType`           |     `VARCHAR(30)` |   NO | User/Admin/Moderator/System/Worker/Anonymous/External |
| `Outcome`             |     `VARCHAR(30)` |   NO | Success/Failure/Denied/Ignored                        |
| `Severity`            |     `VARCHAR(30)` |   NO | Info/Warning/Error/Critical                           |
| `RiskLevel`           |     `VARCHAR(30)` |   NO | Low/Medium/High/Critical                              |
| `Summary`             |   `NVARCHAR(500)` |   NO | Safe human-readable summary                           |
| `Reason`              |   `NVARCHAR(500)` |  YES | Optional event-specific reason from normalizer/source payload |
| `CorrelationId`       |   `NVARCHAR(100)` |  YES | Trace correlation                                     |
| `CausationId`         |   `NVARCHAR(100)` |  YES | Optional                                              |
| `TraceId`             |   `NVARCHAR(100)` |  YES | Optional                                              |
| `IpAddress`           |    `NVARCHAR(45)` |  YES | Optional PII                                          |
| `UserAgent`           |   `NVARCHAR(500)` |  YES | Optional PII                                          |
| `SourcePriority`      |         `TINYINT` |  YES | Copied from Outbox priority                           |
| `OccurredAtUtc`       |    `DATETIME2(3)` |   NO | Source event occurrence time                          |
| `IngestedAtUtc`       |    `DATETIME2(3)` |   NO | Audit persistence time                                |
| `MetadataJson`        |   `NVARCHAR(MAX)` |  YES | Sanitized safe metadata                               |
| `HeadersJson`         |   `NVARCHAR(MAX)` |  YES | Sanitized safe headers                                |
| `SanitizedPayloadJson` |   `NVARCHAR(MAX)` |  YES | Sanitized payload subset; avoid raw unsafe data       |
| `BeforeJson`          |   `NVARCHAR(MAX)` |  YES | Sanitized previous state snapshot                     |
| `AfterJson`           |   `NVARCHAR(MAX)` |  YES | Sanitized new state snapshot                          |
| `ChangesJson`         |   `NVARCHAR(MAX)` |  YES | Sanitized field changes                               |
| `Hash`                |        `CHAR(64)` |  YES | Future tamper-evident hook                            |
| `PrevHash`            |        `CHAR(64)` |  YES | Future tamper-evident hook                            |
| `CreatedAtUtc`        |    `DATETIME2(3)` |   NO | Row creation time                                     |

### 8.2 Required V1 query columns

The following should be physical columns rather than JSON-only because they support V1 APIs:

* `PublicId`
* `MessageId`
* `SourceModule`
* `EventType`
* `Action`
* `ActionCategory`
* `ActorInternalId`
* `ActorUserId`
* `ResourceType`
* `ResourceId`
* `Outcome`
* `Severity`
* `RiskLevel`
* `CorrelationId`
* `OccurredAtUtc`
* `IngestedAtUtc`

---

## 9. Logical Schema — `audit.AuditIngestion`

`AuditIngestion` stores consumer-side processing state for incoming messages.

### 9.1 Recommended V1 columns

| Field                  |              Type | Null | Notes                                                             |
| ---------------------- | ----------------: | ---: | ----------------------------------------------------------------- |
| `AuditIngestionId`     | `BIGINT IDENTITY` |   NO | Internal primary key                                              |
| `PublicId`             |        `CHAR(26)` |   NO | Audit-owned API identifier                                        |
| `MessageId`            |        `CHAR(26)` |   NO | Copied from Outbox; consumer-side dedupe key                      |
| `EventType`            |   `NVARCHAR(200)` |   NO | Original event type                                               |
| `AggregateType`        |   `NVARCHAR(100)` |  YES | From Outbox                                                       |
| `AggregateId`          |   `NVARCHAR(100)` |  YES | From Outbox                                                       |
| `AggregatePublicId`    |        `CHAR(26)` |  YES | From Outbox                                                       |
| `AggregateVersion`     |             `INT` |  YES | From Outbox                                                       |
| `CorrelationId`        |   `NVARCHAR(100)` |  YES | Trace correlation                                                 |
| `SourcePriority`       |         `TINYINT` |  YES | From Outbox priority                                              |
| `SourceOccurredAtUtc`  |    `DATETIME2(3)` |   NO | From Outbox OccurredAt                                            |
| `SourcePublishedAtUtc` |    `DATETIME2(3)` |  YES | From Outbox PublishedAt when available                            |
| `ConsumerName`         |   `NVARCHAR(150)` |   NO | Audit consumer/handler name                                       |
| `Status`               |     `VARCHAR(30)` |   NO | Processing/Succeeded/Duplicate/Ignored/Failed/DeadLettered        |
| `AttemptCount`         |             `INT` |   NO | Consumer-side attempt count                                       |
| `FirstReceivedAtUtc`   |    `DATETIME2(3)` |   NO | First time Audit saw the message                                  |
| `LastAttemptAtUtc`     |    `DATETIME2(3)` |  YES | Last processing attempt                                           |
| `ProcessedAtUtc`       |    `DATETIME2(3)` |  YES | Completion time                                                   |
| `DeadLetteredAtUtc`    |    `DATETIME2(3)` |  YES | Terminal dead-letter handling time                                |
| `LastErrorCode`        |   `NVARCHAR(100)` |  YES | Sanitized consumer-side error code                                |
| `LastErrorMessage`     |  `NVARCHAR(2000)` |  YES | Sanitized consumer-side error message                             |
| `LastErrorClass`       |     `VARCHAR(30)` |  YES | Transient/Permanent/Ambiguous/Validation/Policy/Redaction/Unknown |
| `CreatedAtUtc`         |    `DATETIME2(3)` |   NO | Row creation time                                                 |
| `UpdatedAtUtc`         |    `DATETIME2(3)` |   NO | Row update time                                                   |

### 9.2 Status values

Allowed values:

* `Processing`
* `Succeeded`
* `Duplicate`
* `Ignored`
* `Failed`
* `DeadLettered`

Rule:

Do not use `Published` as an AuditIngestion status.

`Published` belongs to producer-side `OutboxMessage.Status`.

---

## 10. Constraints and Indexes

## 10.1 Primary keys

Recommended primary keys:

```text
PK_AuditLog(AuditLogId)
PK_AuditIngestion(AuditIngestionId)
```

### 10.2 PublicId uniqueness

Recommended unique constraints:

```text
UQ_AuditLog_PublicId(PublicId)
UQ_AuditIngestion_PublicId(PublicId)
```

### 10.3 MessageId uniqueness

Recommended unique constraints:

```text
UQ_AuditLog_MessageId(MessageId)
UQ_AuditIngestion_MessageId(MessageId)
```

Purpose:

* prevent duplicate audit evidence under retries
* support idempotent ingestion
* support lookup by message id
* support replay safety

### 10.4 Check constraints

Recommended checks for `AuditLog` matching the current table script:

```text
CK_AuditLog_PublicId_NotBlank
CK_AuditLog_MessageId_NotBlank
CK_AuditLog_EventType_NotBlank
CK_AuditLog_SourceModule_NotBlank
CK_AuditLog_Action_NotBlank
CK_AuditLog_ResourceType_NotBlank
CK_AuditLog_ResourceId_NotBlank
CK_AuditLog_Summary_NotBlank
CK_AuditLog_Reason_NotBlank

CK_AuditLog_EventVersion
  EventVersion IS NULL OR EventVersion >= 1

CK_AuditLog_AggregateVersion
  AggregateVersion IS NULL OR AggregateVersion >= 1

CK_AuditLog_SourcePriority
  SourcePriority IS NULL OR SourcePriority BETWEEN 1 AND 9

CK_AuditLog_Outcome
  Outcome IN ('Success', 'Failure', 'Denied', 'Ignored')

CK_AuditLog_Severity
  Severity IN ('Info', 'Warning', 'Error', 'Critical')

CK_AuditLog_RiskLevel
  RiskLevel IN ('Low', 'Medium', 'High', 'Critical')

CK_AuditLog_ActorType
  ActorType IN ('User', 'Admin', 'Moderator', 'System', 'Worker', 'Anonymous', 'External')

CK_AuditLog_MetadataJson_IsJson
CK_AuditLog_HeadersJson_IsJson
CK_AuditLog_SanitizedPayloadJson_IsJson
CK_AuditLog_BeforeJson_IsJson
CK_AuditLog_AfterJson_IsJson
CK_AuditLog_ChangesJson_IsJson
```

Recommended checks for `AuditIngestion` matching the current table script:

```text
CK_AuditIngestion_PublicId_NotBlank
CK_AuditIngestion_MessageId_NotBlank
CK_AuditIngestion_EventType_NotBlank
CK_AuditIngestion_ConsumerName_NotBlank

CK_AuditIngestion_AggregateVersion
  AggregateVersion IS NULL OR AggregateVersion >= 1

CK_AuditIngestion_SourcePriority
  SourcePriority IS NULL OR SourcePriority BETWEEN 1 AND 9

CK_AuditIngestion_Status
  Status IN ('Processing', 'Succeeded', 'Duplicate', 'Ignored', 'Failed', 'DeadLettered')

CK_AuditIngestion_AttemptCount
  AttemptCount >= 0

CK_AuditIngestion_LastErrorClass
  LastErrorClass IS NULL OR LastErrorClass IN
  ('Transient', 'Permanent', 'Ambiguous', 'Validation', 'Policy', 'Redaction', 'Unknown')

CK_AuditIngestion_SourcePublishedAtUtc
  SourcePublishedAtUtc IS NULL OR SourcePublishedAtUtc >= SourceOccurredAtUtc

CK_AuditIngestion_LastAttemptAtUtc
  LastAttemptAtUtc IS NULL OR LastAttemptAtUtc >= FirstReceivedAtUtc

CK_AuditIngestion_ProcessedAtUtc
  ProcessedAtUtc IS NULL OR ProcessedAtUtc >= FirstReceivedAtUtc

CK_AuditIngestion_DeadLetteredAtUtc
  DeadLetteredAtUtc IS NULL OR DeadLetteredAtUtc >= FirstReceivedAtUtc
```

---

## 10.5 Investigation indexes — AuditLog

Recommended V1 indexes:

```text
IX_AuditLog_OccurredAtUtc
  (OccurredAtUtc DESC)

IX_AuditLog_IngestedAtUtc
  (IngestedAtUtc DESC)

IX_AuditLog_SourceModule_OccurredAtUtc
  (SourceModule, OccurredAtUtc DESC)

IX_AuditLog_EventType_OccurredAtUtc
  (EventType, OccurredAtUtc DESC)

IX_AuditLog_Action_OccurredAtUtc
  (Action, OccurredAtUtc DESC)

IX_AuditLog_ActionCategory_OccurredAtUtc
  (ActionCategory, OccurredAtUtc DESC)

IX_AuditLog_ActorUserId_OccurredAtUtc
  (ActorUserId, OccurredAtUtc DESC)

IX_AuditLog_ActorInternalId_OccurredAtUtc
  (ActorInternalId, OccurredAtUtc DESC)

IX_AuditLog_Resource_OccurredAtUtc
  (ResourceType, ResourceId, OccurredAtUtc DESC)

IX_AuditLog_CorrelationId_OccurredAtUtc
  (CorrelationId, OccurredAtUtc DESC)

IX_AuditLog_RiskLevel_OccurredAtUtc
  (RiskLevel, OccurredAtUtc DESC)

IX_AuditLog_Severity_OccurredAtUtc
  (Severity, OccurredAtUtc DESC)

IX_AuditLog_Outcome_OccurredAtUtc
  (Outcome, OccurredAtUtc DESC)

IX_AuditLog_Aggregate_Version
  (AggregateType, AggregateId, AggregateVersion)
```

### Notes

* `MessageId` lookup is covered by the unique constraint in `001_tables.sql`.
* Query indexes should follow actual API access patterns.
* Avoid over-indexing before real query pressure is measured.
* JSON fields should not be used for high-volume filters in V1 unless a computed/indexed strategy is explicitly designed.

---

## 10.6 Operational indexes — AuditIngestion

Recommended V1 indexes:

```text
IX_AuditIngestion_Status_FirstReceivedAtUtc
  (Status, FirstReceivedAtUtc DESC)

IX_AuditIngestion_Status_LastAttemptAtUtc
  (Status, LastAttemptAtUtc DESC)

IX_AuditIngestion_EventType_FirstReceivedAtUtc
  (EventType, FirstReceivedAtUtc DESC)

IX_AuditIngestion_CorrelationId_FirstReceivedAtUtc
  (CorrelationId, FirstReceivedAtUtc DESC)

IX_AuditIngestion_ConsumerName_Status
  (ConsumerName, Status)

IX_AuditIngestion_SourceOccurredAtUtc
  (SourceOccurredAtUtc DESC)

IX_AuditIngestion_SourcePublishedAtUtc
  (SourcePublishedAtUtc DESC)

IX_AuditIngestion_ProcessedAtUtc
  (ProcessedAtUtc DESC)
```

---

## 11. Append-only Enforcement

AuditLog is append-only by policy.

Possible enforcement options:

1. DB permissions:

   * application role may INSERT and SELECT
   * UPDATE/DELETE denied for normal application path

2. Trigger-based protection:

   * block UPDATE/DELETE on `audit.AuditLog`

3. Service-level policy:

   * repositories expose insert/read only
   * no update/delete methods for AuditLog

Recommended V1 approach:

* service/repository-level insert-only contract
* SQL constraints for identity and dedupe
* optional DB permission hardening later

If retention, purge, redaction remediation, or correction is introduced, it must be documented separately.

---

## 12. Retention and Operational Jobs

### 12.1 V1 posture

V1 does not require archive or purge jobs.

Early V1 may keep all audit evidence in primary SQL.

### 12.2 Future retention questions

Future policy must define:

* how long `AuditLog` stays in primary SQL
* how long `AuditIngestion` stays in primary SQL
* whether older evidence is archived
* whether archived evidence is queryable
* whether purge is allowed
* who can approve purge
* whether purge creates audit-of-audit evidence

### 12.3 Future jobs

Future jobs may include:

* archive job
* purge job
* redaction remediation job
* summary build job
* digest generation job
* replay/remediation job
* reconciliation job

These jobs must be bounded, observable, and rerun-safe.

---

## 13. Partitioning Readiness

Audit V1 is partition-ready but not physically partitioned by default.

V1 priority:

* reliable ingestion
* investigation-ready indexes
* bounded queries
* observable lag/failure
* non-blocking source flows

### 13.1 Why Audit is a partitioning-risk module

Audit is append-only and may grow quickly.

Risk sources:

* admin bulk actions
* authorization policy migrations
* content moderation bursts
* interaction/moderation event spikes
* replay/retry bursts after outage
* incident investigations repeatedly querying recent windows

### 13.2 V1 access patterns

Worker ingestion:

* insert `AuditLog`
* upsert/update `AuditIngestion`
* dedupe by `MessageId`

Admin investigation reads:

* time range
* source module + time
* actor + time
* resource + time
* action + time
* risk/severity + time
* correlation id + time
* message id lookup

### 13.3 V1 mitigations

V1 mitigations before partitioning:

* async ingestion
* durable MessageId dedupe
* focused SQL indexes
* bounded query windows
* allowlisted sort/filter fields
* consumer retry/backoff
* DLQ visibility
* query latency monitoring
* queue lag monitoring

### 13.4 Future partitioning options

Future partitioning may include:

#### Option A — Worker lane partitioning

Partition ingestion by:

* source module
* risk priority
* event category
* queue/lane

Example lanes:

* security/governance lane
* content/media lane
* interaction/moderation lane
* retry/dead-letter lane

#### Option B — Time-range partitioning for AuditLog

Useful when:

* `AuditLog` grows large
* retention/archive becomes expensive
* recent-window query performance degrades
* purge/archive windows need operational isolation

#### Option C — Hybrid partitioning

Possible future strategy:

* time range + source module
* time range + risk priority
* time range + hash bucket

Use only when metrics justify it.

#### Option D — Search/investigation projection

If investigation queries become too complex, introduce a derived search/read projection.

Guardrail:

`AuditLog` remains append-only evidence truth.

### 13.5 Partitioning signals

Consider stronger partitioning when sustained signals show:

* audit query P95/P99 degradation
* growing AuditLog table/index pressure
* audit consumer lag does not self-recover
* oldest uningested message age grows
* retry/DLQ pressure grows
* security audit delayed by high-volume non-security events
* archive/report windows become too slow

---

## 14. ADR Candidates

Future ADR candidates:

* Audit payload shape and redaction policy per event type
* Audit retention, archive, and purge policy
* Audit tamper-evident evidence strategy
* Audit replay and remediation control policy
* Audit completeness reconciliation policy
* Audit alerting and admin notification policy
* Audit digest and summary materialization policy
* Audit storage and indexing evolution
* Audit Redis dashboard cache policy
* Audit partitioning and worker lane strategy
* Audit correction and redaction remediation policy
* Audit export policy
* Audit-of-audit policy

---

## 15. ERD

See:

```text
../diagrams/erd/audit-v1.dbml
```

How to render:

1. Open dbdiagram.io.
2. Copy DBML content from the file above.
3. Paste into dbdiagram.io to view/export.

---

## 16. Summary

Audit V1 system data model is based on two core tables:

```text
AuditLog
AuditIngestion
```

The most important data rules are:

1. `AuditLog` is append-only evidence truth.
2. `AuditIngestion` is consumer-side processing state.
3. `MessageId` is the canonical idempotency key.
4. `PublicId` is the API-facing Audit-owned identifier.
5. Source resources are referenced by stable string identifiers, preferably source PublicIds.
6. Source module truth remains with source modules.
7. SQL is the source of Audit evidence truth.
8. Redis, dashboards, alerts, reports, digests, archives, and summaries are derived.
9. V1 is not physically partitioned, but it is partition-ready.
10. Future workflows must be bounded, observable, idempotent, and rerun-safe.
