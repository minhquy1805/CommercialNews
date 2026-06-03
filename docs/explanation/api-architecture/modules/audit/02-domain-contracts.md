# Audit — Domain Contracts (V1)

## 1. Purpose

This document defines the domain contracts for the Audit module in CommercialNews V1.

Audit provides SQL-backed, append-only, investigation-ready evidence for important system actions. It consumes audit-relevant messages from the shared Outbox pipeline, normalizes them into a consistent audit model, stores evidence durably, and exposes query contracts for investigation and operations.

Audit V1 intentionally keeps the implementation scope simple:

* `AuditLog`
* `AuditIngestion`
* `MessageId` deduplication
* event normalizer abstractions and registry
* investigation query contracts
* lightweight dashboard queries from `AuditLog`

Future extensions such as alerting, digest generation, materialized summaries, archival, reconciliation, notification integration, Redis acceleration, and partitioning are allowed extension paths, but they are not required for V1 core delivery.

---

## 2. Ownership

### 2.1 Audit owns

Audit owns:

* audit log persistence
* append-only audit evidence model
* audit ingestion tracking
* audit ingestion policy
* audit redaction policy
* audit ingestion idempotency rules
* audit query contracts for investigation and operations
* future audit-derived outputs if introduced, such as summaries, alerts, digests, reports, or archive records

### 2.2 Audit does not own

Audit does not own:

* domain action execution in producer modules such as Content, Authorization, Identity, Media, SEO, Notifications, or Interaction
* producer module truth state
* producer-side outbox creation
* producer-side outbox publishing
* notification delivery
* authorization decisions
* source module lifecycle state
* source module repair/rebuild logic

Examples:

* Identity owns account truth.
* Authorization owns role and permission assignment truth.
* Content owns article lifecycle truth.
* Media owns media metadata and attachment truth.
* Interaction owns comment, like, report, and interaction truth.
* SEO owns slug and routing truth.
* Notifications owns email delivery-state truth.

Audit owns only evidence that important events happened.

### 2.3 Producer modules own

Producer modules own:

* business truth mutations
* emitted event correctness
* event payload shape
* event contract versioning
* producer-side outbox creation in the same local transaction as the truth mutation

Producer modules must not write directly to Audit-owned tables.

Audit must not write back to producer module truth tables.

---

## 3. Source Contract: OutboxMessage

Audit consumes messages that originate from the shared Outbox contract.

The current producer-side Outbox table is:

```text
[outbox].[OutboxMessage]
```

Relevant fields:

| Outbox field        |                      Type | Meaning                                           |
| ------------------- | ------------------------: | ------------------------------------------------- |
| `OutboxMessageId`   |                  `BIGINT` | Internal producer-side outbox row identifier      |
| `MessageId`         |                `CHAR(26)` | System-wide async message identity                |
| `EventType`         |           `NVARCHAR(200)` | Integration event type                            |
| `AggregateType`     |           `NVARCHAR(100)` | Source aggregate type                             |
| `AggregateId`       |           `NVARCHAR(100)` | Source aggregate identifier                       |
| `AggregatePublicId` |       `CHAR(26)` nullable | Stable public aggregate identifier when available |
| `AggregateVersion`  |            `INT` nullable | Monotonic aggregate version when ordering matters |
| `Payload`           |           `NVARCHAR(MAX)` | Event payload JSON                                |
| `Headers`           |  `NVARCHAR(MAX)` nullable | Optional metadata JSON                            |
| `CorrelationId`     |  `NVARCHAR(100)` nullable | Cross-flow correlation identifier                 |
| `InitiatorUserId`   |         `BIGINT` nullable | Producer-side internal actor id when available    |
| `Priority`          |                 `TINYINT` | Producer-side priority                            |
| `Status`            |             `VARCHAR(20)` | Producer-side publication status                  |
| `AttemptCount`      |                     `INT` | Producer-side publish attempt count               |
| `NextRetryAt`       |   `DATETIME2(3)` nullable | Producer-side retry schedule                      |
| `LastAttemptAt`     |   `DATETIME2(3)` nullable | Last producer-side publish attempt                |
| `PublishedAt`       |   `DATETIME2(3)` nullable | Time broker handoff succeeded                     |
| `LastError`         | `NVARCHAR(2000)` nullable | Producer-side publish error                       |
| `LastErrorCode`     |  `NVARCHAR(100)` nullable | Producer-side publish error code                  |
| `LastErrorClass`    |    `VARCHAR(30)` nullable | Producer-side publish error class                 |
| `OccurredAt`        |            `DATETIME2(3)` | Time the producer says the event occurred         |
| `CreatedAt`         |            `DATETIME2(3)` | Outbox row creation time                          |
| `UpdatedAt`         |            `DATETIME2(3)` | Outbox row update time                            |

### 3.1 Producer-side status is not Audit ingestion status

`OutboxMessage.Status` is a producer-side publication state.

Supported producer-side statuses:

* `Pending`
* `Publishing`
* `Published`
* `Failed`
* `Dead`

Important rule:

`OutboxMessage.Status = Published` means broker handoff succeeded.

It does not mean:

* Audit consumed the message
* Audit inserted `AuditLog`
* Audit ingestion succeeded
* downstream side effects completed

Audit must track consumer-side processing separately through `AuditIngestion`.

---

## 4. Identifier Contract

Audit follows the system public identifier strategy.

### 4.1 Internal primary keys

Audit may use internal numeric primary keys for SQL efficiency.

Examples:

* `AuditLogId`
* `AuditIngestionId`

Internal numeric primary keys are database implementation details and should not be exposed as stable API identifiers.

### 4.2 PublicId

Audit-owned records that need stable API identity should use `PublicId`.

Recommended format:

* ULID
* `CHAR(26)` when stored as text

Examples:

* `AuditLog.PublicId`
* `AuditIngestion.PublicId`

Admin APIs should prefer `PublicId` over internal numeric IDs.

Example:

```http
GET /api/v1/admin/audit/logs/{publicId}
```

### 4.3 MessageId

`MessageId` is the system-wide async message identity.

It is copied from:

```text
outbox.OutboxMessage.MessageId
```

Audit uses `MessageId` for:

* message-level deduplication
* replay safety
* retry safety
* cross-system tracing
* linking AuditLog to Outbox, Worker logs, RabbitMQ delivery, and AuditIngestion

`MessageId` must be stable under:

* retry
* broker redelivery
* replay
* worker restart
* consumer restart

Audit V1 uses `MessageId` as the canonical field name.

Audit V1 should not use the old local alias `EventId` for the same concept.

### 4.4 Aggregate identity

Audit preserves source aggregate identity from Outbox:

* `AggregateType`
* `AggregateId`
* `AggregatePublicId`
* `AggregateVersion`

These fields help reconstruct source event context and event ordering for investigation.

### 4.5 Resource identity

Audit normalizes source aggregate identity into an investigation resource identity:

* `ResourceType`
* `ResourceId`

Recommended mapping:

```text
ResourceType = normalized domain resource type
ResourceId   = AggregatePublicId if present, otherwise AggregateId
```

Examples:

* `Article` + `ArticlePublicId`
* `UserAccount` + `UserPublicId`
* `Comment` + `CommentPublicId`
* `MediaAsset` + `MediaAssetPublicId`
* `RolePermission` + `{roleId}:{permissionId}` when no public id exists
* `UserRole` + `{userId}:{roleId}` when no public id exists

If source events still use internal numeric identifiers, Audit may preserve them in sanitized metadata, but API-facing investigation contracts should converge toward stable opaque identifiers.

### 4.6 Actor identity

Outbox currently provides:

```text
InitiatorUserId BIGINT NULL
```

This is a producer-side internal actor identifier.

Audit may store it as `ActorInternalId` for traceability.

If the payload or headers provide a stable actor public identifier, Audit should prefer that value for `ActorUserId` in API-facing contracts.

Recommended actor fields:

* `ActorInternalId`
* `ActorUserId`
* `ActorEmail`
* `ActorDisplayName`
* `ActorType`

### 4.7 Slug

Slug is not a stable resource identity.

Audit may store slug as metadata for investigation, but must not treat slug as the primary resource identifier because slug can change.

Example:

```text
ResourceType = Article
ResourceId = ArticlePublicId
MetadataJson includes Slug
```

---

## 5. Entity: AuditLog

`AuditLog` represents one normalized audit evidence record.

It is SQL-backed and append-only by default.

### 5.1 Conceptual fields

| Field                 | Description                                                     |
| --------------------- | --------------------------------------------------------------- |
| `AuditLogId`          | Internal SQL primary key                                        |
| `PublicId`            | Stable Audit-owned API identifier                               |
| `MessageId`           | Upstream async message identity from Outbox; unique             |
| `EventType`           | Original integration event type                                 |
| `EventVersion`        | Source event contract version if available                      |
| `SourceModule`        | Producer module name derived by normalizer                      |
| `Action`              | Normalized audit action                                         |
| `ActionCategory`      | Logical action category                                         |
| `AggregateType`       | Source aggregate type from Outbox                               |
| `AggregateId`         | Source aggregate identifier from Outbox                         |
| `AggregatePublicId`   | Source aggregate public id from Outbox when available           |
| `AggregateVersion`    | Source aggregate version when available                         |
| `ResourceType`        | Audited resource category                                       |
| `ResourceId`          | Stable resource identifier used for investigation               |
| `ResourceDisplayName` | Optional display name for investigation                         |
| `ActorInternalId`     | Producer-side internal actor id, usually from `InitiatorUserId` |
| `ActorUserId`         | Stable actor public id when available                           |
| `ActorEmail`          | Optional actor email                                            |
| `ActorDisplayName`    | Optional actor display name                                     |
| `ActorType`           | User, Admin, Moderator, System, Worker, Anonymous, External     |
| `ActorRolesJson`      | Optional sanitized actor role snapshot                          |
| `Outcome`             | Business outcome of the source action                           |
| `Severity`            | Operational seriousness                                         |
| `RiskLevel`           | Security or business sensitivity                                |
| `Summary`             | Short safe human-readable description                           |
| `BeforeJson`          | Optional sanitized previous state snapshot                      |
| `AfterJson`           | Optional sanitized new state snapshot                           |
| `ChangesJson`         | Optional sanitized field-level changes                          |
| `MetadataJson`        | Optional sanitized investigation metadata                       |
| `HeadersJson`         | Optional sanitized Outbox headers                               |
| `SanitizedPayloadJson` | Sanitized original payload or safe event subset                |
| `CorrelationId`       | Cross-flow correlation identifier                               |
| `CausationId`         | Optional causation identifier                                   |
| `TraceId`             | Optional distributed trace identifier                           |
| `IpAddress`           | Optional request IP address                                     |
| `UserAgent`           | Optional request user agent                                     |
| `SourcePriority`      | Optional source priority copied from Outbox                     |
| `OccurredAtUtc`       | Time the producer says the event occurred                       |
| `IngestedAtUtc`       | Time Audit persisted the record                                 |
| `CreatedAtUtc`        | Row creation time                                               |
| `Hash`                | Future tamper-evident hook                                      |
| `PrevHash`            | Future tamper-evident hook                                      |

### 5.2 Required fields in V1

V1 requires:

* `PublicId`
* `MessageId`
* `EventType`
* `SourceModule`
* `Action`
* `ResourceType`
* `ResourceId`
* `Outcome`
* `Severity`
* `RiskLevel`
* `Summary`
* `OccurredAtUtc`
* `IngestedAtUtc`
* `CreatedAtUtc`

V1 should also preserve when available:

* `AggregateType`
* `AggregateId`
* `AggregatePublicId`
* `AggregateVersion`
* `CorrelationId`
* `ActorInternalId`
* `ActorUserId`
* `HeadersJson`
* `SanitizedPayloadJson`

### 5.3 MessageId uniqueness

`AuditLog.MessageId` must be unique.

The same upstream message must not produce duplicate audit evidence.

Recommended SQL constraint:

```text
UQ_AuditLog_MessageId
```

### 5.4 SourceModule derivation

The current Outbox table does not contain a dedicated `ProducerModule` column.

Therefore, `SourceModule` is event/normalizer metadata. It may be derived from
one or more of:

* `EventType` prefix
* `Headers`
* payload convention
* normalizer registration

Audit.Application should not be described as the only place that hard-codes
`SourceModule`. Worker builds `IngestAuditEventCommand`, then the Application use
case resolves a normalizer through `IAuditEventNormalizerRegistry`; the selected
normalizer understands the event schema and produces `AuditNormalizedEvent`,
including `SourceModule`.

Example:

```text
authorization.user_role_assigned → Authorization
identity.user_registered → Identity
content.article_published → Content
```

### 5.5 Action normalization

`Action` is a normalized audit action name.

Examples:

* `UserRegistered`
* `UserLocked`
* `UserRoleAssigned`
* `RolePermissionGranted`
* `ArticlePublished`
* `CommentHiddenByModerator`
* `MediaAttachedToArticle`
* `SeoMetadataUpdated`
* `EmailDeliveryFailed`

`Action` should be stable enough for filtering, dashboard summaries, and future alert rules.

### 5.6 ActionCategory

`ActionCategory` groups related actions.

Examples:

* `Authentication`
* `Authorization`
* `IdentitySecurity`
* `ContentLifecycle`
* `Moderation`
* `MediaGovernance`
* `SeoGovernance`
* `NotificationDelivery`
* `AuditIngestion`

V1 may keep this field simple or nullable if not needed immediately, but the contract allows future dashboard and alerting extension.

### 5.7 Outcome

`Outcome` describes the source business action result.

Supported values:

* `Success`
* `Failure`
* `Denied`
* `Ignored`

Important rule:

`Outcome` is not the same as Audit consumer processing status.

Example:

```text
Outcome = Success
AuditIngestion.Status = Failed
```

This means the source action succeeded, but Audit consumer processing failed.

### 5.8 Severity

`Severity` describes operational seriousness.

Supported values:

* `Info`
* `Warning`
* `Error`
* `Critical`

Examples:

* normal successful event: `Info`
* repeated failed login: `Warning`
* audit ingestion failure: `Error`
* critical consumer pipeline failure: `Critical`

### 5.9 RiskLevel

`RiskLevel` describes security or business sensitivity.

Supported values:

* `Low`
* `Medium`
* `High`
* `Critical`

Examples:

* article published: `Medium`
* user locked: `High`
* role permission granted for sensitive permission: `Critical`
* refresh token reuse detected: `Critical`

### 5.10 Sanitized payload and metadata

Audit may preserve a sanitized event payload for traceability.

However, `SanitizedPayloadJson`, `MetadataJson`, `HeadersJson`, `Summary`,
`BeforeJson`, `AfterJson`, and `ChangesJson` must be sanitized and redacted.

The raw input payload from Outbox is an ingestion input. It must not be confused
with the payload stored or returned from AuditLog detail after redaction.

Audit must not store unrestricted domain data.

---

## 6. Entity: AuditIngestion

`AuditIngestion` represents Audit consumer-side processing state for an incoming message.

It exists to separate downstream Audit processing from producer-side Outbox publication.

### 6.1 Purpose

`AuditIngestion` supports:

* consumer-side processing visibility
* MessageId deduplication tracking
* retry investigation
* failed ingestion diagnostics
* duplicate detection
* poison message handling
* future replay/reconciliation support

### 6.2 Conceptual fields

| Field                  | Description                                         |
| ---------------------- | --------------------------------------------------- |
| `AuditIngestionId`     | Internal SQL primary key                            |
| `PublicId`             | Stable Audit-owned API identifier                   |
| `MessageId`            | Upstream async message identity from Outbox; unique |
| `EventType`            | Original integration event type                     |
| `AggregateType`        | Source aggregate type                               |
| `AggregateId`          | Source aggregate identifier                         |
| `AggregatePublicId`    | Source aggregate public id when available           |
| `AggregateVersion`     | Source aggregate version when available             |
| `CorrelationId`        | Cross-flow correlation identifier                   |
| `SourcePriority`       | Priority copied from Outbox                         |
| `SourceOccurredAtUtc`  | Outbox `OccurredAt`                                 |
| `SourcePublishedAtUtc` | Outbox `PublishedAt` if available                   |
| `ConsumerName`         | Name of the Audit consumer/handler                  |
| `Status`               | Consumer-side processing status                     |
| `AttemptCount`         | Consumer-side attempt count                         |
| `FirstReceivedAtUtc`   | First time Audit saw the message                    |
| `LastAttemptAtUtc`     | Last Audit processing attempt                       |
| `ProcessedAtUtc`       | Time Audit completed processing, if completed       |
| `DeadLetteredAtUtc`    | Time Audit moved the message to terminal failure handling |
| `LastErrorCode`        | Sanitized last consumer-side error code             |
| `LastErrorMessage`     | Sanitized last consumer-side error message          |
| `LastErrorClass`       | Consumer-side error class                           |
| `CreatedAtUtc`         | Row creation time                                   |
| `UpdatedAtUtc`         | Row update time                                     |

### 6.3 Status values

Supported V1 statuses:

* `Processing`
* `Succeeded`
* `Duplicate`
* `Ignored`
* `Failed`
* `DeadLettered`

Meaning:

| Status         | Meaning                                                                          |
| -------------- | -------------------------------------------------------------------------------- |
| `Processing`   | Audit has received the message and is processing it                              |
| `Succeeded`    | Audit processed the message and persisted evidence or intentionally completed it |
| `Duplicate`    | Message was already processed or already represented by an existing `AuditLog`   |
| `Ignored`      | Message was intentionally ignored by policy                                      |
| `Failed`       | Audit consumer processing failed and may retry                                   |
| `DeadLettered` | Message reached terminal consumer-side failure handling                          |

`Published` must not be used as an AuditIngestion status because `Published` belongs to the producer-side Outbox state machine.

### 6.4 MessageId uniqueness

`AuditIngestion.MessageId` should be unique.

Recommended SQL constraint:

```text
UQ_AuditIngestion_MessageId
```

This allows Audit to quickly determine whether a message has already been seen, processed, failed, ignored, or treated as duplicate.

---

## 7. Standard Mapping Contract

Audit consumes the shared Outbox integration envelope.

### 7.1 Outbox to AuditLog mapping

| Outbox field        | AuditLog field                                  |
| ------------------- | ----------------------------------------------- |
| `MessageId`         | `MessageId`                                     |
| `EventType`         | `EventType`                                     |
| `AggregateType`     | `AggregateType`                                 |
| `AggregateId`       | `AggregateId`                                   |
| `AggregatePublicId` | `AggregatePublicId`                             |
| `AggregateVersion`  | `AggregateVersion`                              |
| `Payload`           | `SanitizedPayloadJson` and/or normalized audit fields |
| `Headers`           | `HeadersJson` and/or metadata                   |
| `CorrelationId`     | `CorrelationId`                                 |
| `InitiatorUserId`   | `ActorInternalId`                               |
| `Priority`          | `SourcePriority`                                |
| `OccurredAt`        | `OccurredAtUtc`                                 |

### 7.2 Outbox to AuditIngestion mapping

| Outbox field        | AuditIngestion field   |
| ------------------- | ---------------------- |
| `MessageId`         | `MessageId`            |
| `EventType`         | `EventType`            |
| `AggregateType`     | `AggregateType`        |
| `AggregateId`       | `AggregateId`          |
| `AggregatePublicId` | `AggregatePublicId`    |
| `AggregateVersion`  | `AggregateVersion`     |
| `CorrelationId`     | `CorrelationId`        |
| `Priority`          | `SourcePriority`       |
| `OccurredAt`        | `SourceOccurredAtUtc`  |
| `PublishedAt`       | `SourcePublishedAtUtc` |

### 7.3 Normalizer-derived fields

Audit.Application keeps the abstraction `IAuditEventNormalizer` and the
`IAuditEventNormalizerRegistry`. Concrete normalizers are Infrastructure
components registered by Audit.Infrastructure, for example:

* `AuthorizationAuditEventNormalizer`
* `IdentityAuditEventNormalizer`
* `ContentAuditEventNormalizer`
* `MediaAuditEventNormalizer`
* `InteractionAuditEventNormalizer`

Worker reads the broker/Outbox message, builds `IngestAuditEventCommand`, and
invokes the Application use case through MediatR. The Application service uses
the registry to select a normalizer by `EventType`.

If no normalizer is registered for the `EventType`, the event is treated as
unsupported and is ignored through `AuditIngestion`.

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
* `BeforeJson`
* `AfterJson`
* `ChangesJson`
* `MetadataJson`

The normalizer may use:

* `EventType`
* raw input payload from Outbox
* `Headers`
* `AggregateType`
* `AggregateId`
* `AggregatePublicId`
* `InitiatorUserId`
* module-specific mapping rules

---

## 8. Invariants

### 8.1 Append-only evidence

Audit records are append-only.

Once inserted, `AuditLog` records must not be updated or deleted by normal business workflows.

Exceptions require explicit policy:

* retention policy
* legal/compliance purge
* operator-controlled remediation with separate evidence trail
* future redaction/masking policy

### 8.2 Message-level idempotency

The same upstream `MessageId` must not produce duplicate audit records.

Implementation rule:

* `AuditLog.MessageId` must be unique.
* Duplicate `MessageId` during ingestion is treated as already processed or duplicate-safe.

### 8.3 Replay safety

Replaying the same message with the same `MessageId` must be safe.

Expected behavior:

* existing `MessageId` found
* no new `AuditLog` row inserted
* `AuditIngestion` records duplicate-safe handling
* consumer treats the message as successfully handled or duplicate-safe

### 8.4 Different MessageIds are not collapsed by default

Audit does not collapse different `MessageId` values by default.

Reason:

* separate events may represent separate attempts
* repeated governance attempts may be useful investigation evidence
* business-level dedupe must not hide legitimate audit trails
* raw audit evidence should preserve distinct emitted messages

Business-level duplicate detection may be added later for alerts, digests, reports, or noisy derived audit views.

### 8.5 Privacy and redaction

Audit fields must not include secrets or unsafe sensitive data.

The following must not be stored in `Summary`, `SanitizedPayloadJson`, `HeadersJson`, `MetadataJson`, `BeforeJson`, `AfterJson`, or `ChangesJson`:

* passwords
* password hashes
* access tokens
* refresh tokens
* verification tokens
* reset tokens
* API keys
* session cookies
* raw authorization headers
* raw secret configuration
* sensitive PII beyond minimum necessary
* full article body content unless explicitly approved by audit policy

Audit stores a safe subset of the event payload, not raw unrestricted domain data.

### 8.6 Traceability

Audit should store enough metadata to support investigation:

* `MessageId`
* `CorrelationId`
* `TraceId` when available
* `SourceModule`
* `EventType`
* `AggregateType`
* `AggregateId`
* `AggregatePublicId`
* `AggregateVersion`
* `ResourceType`
* `ResourceId`
* `ActorInternalId`
* `ActorUserId`
* `OccurredAtUtc`
* `IngestedAtUtc`

### 8.7 Out-of-order arrival tolerance

Audit does not require ordered application to remain correct.

If older events arrive after newer events, Audit may still append them as historical evidence.

Investigation views may sort by:

* `OccurredAtUtc` for source chronology
* `AggregateVersion` for aggregate chronology where available
* `IngestedAtUtc` for ingestion/runtime chronology

### 8.8 Audit does not claim global ordering

Audit does not provide global total ordering across all events.

Ordering is best-effort and contextual:

* source chronology: `OccurredAtUtc`
* aggregate chronology: `AggregateId` + `AggregateVersion`
* ingestion chronology: `IngestedAtUtc`

### 8.9 Redis is not audit truth

Redis must not be used as the source of audit evidence truth.

Redis must not be the only dedupe mechanism for critical audit append behavior.

Future Redis usage may be introduced only as optional acceleration for non-authoritative dashboard summaries, recent event panels, module/action metadata, or TTL-bound alert hints.

### 8.10 Dashboard outputs are derived

Audit dashboard summaries, counts, recent-risk panels, module statistics, reports, alerts, and digests are derived outputs.

They must not replace `AuditLog` as audit evidence truth.

---

## 9. Audit Coverage Policy — V1 Baseline

At minimum, Audit should cover governance, security-sensitive, moderation-sensitive, and operationally important actions.

### 9.1 Authorization

Authorization events consumed by Audit in V1:

* `authorization.user_role_assigned`
* `authorization.user_role_revoked`
* `authorization.role_permission_granted`
* `authorization.role_permission_revoked`

Recommended future Authorization events:

* `authorization.role_created`
* `authorization.role_updated`
* `authorization.role_deleted`
* `authorization.permission_created`
* `authorization.permission_updated`
* `authorization.permission_deleted`

### 9.2 Identity

Identity actions to audit when enabled:

* user registered
* email verified
* login succeeded
* login failed
* password changed
* password reset requested
* password reset completed
* account locked
* account unlocked
* account disabled/enabled
* refresh token revoked
* all sessions revoked
* suspicious login/security events

Identity audit payloads must never include raw verification/reset tokens, access tokens, refresh tokens, password hashes, or session cookies.

### 9.3 Content

Content actions to audit when enabled:

* article created
* article updated
* article published
* article unpublished
* article archived
* article restored
* article deleted
* sensitive metadata changed
* category created/updated/deleted
* tag created/updated/deleted

### 9.4 Interaction

Interaction actions to audit when enabled:

* comment created
* comment deleted by author
* comment hidden by moderator
* comment restored by moderator
* comment reported
* report dismissed
* reported comment hidden

### 9.5 Media

Media actions to audit when enabled:

* media uploaded
* media attached
* media detached
* media reordered
* primary media changed
* media deleted
* media restored

### 9.6 SEO

SEO actions to audit when enabled:

* slug generated
* slug changed
* slug route activated
* slug route deactivated
* SEO metadata updated

Slug may be stored as metadata, but resource identity should use stable resource identifiers.

### 9.7 Notifications

Notification actions may be audited when operationally important:

* email delivery failed
* email delivery dead-lettered
* delivery retry exhausted
* critical provider failure detected

Routine successful delivery does not always need to be audited unless business policy requires it.

### 9.8 Coverage changes

If V1 narrows or expands audit coverage, the decision should be recorded in the relevant module docs.

---

## 10. Authorization Event Mapping — V1 Baseline

This section defines the baseline Authorization event mapping because Authorization governance changes are security-sensitive.

### 10.1 `authorization.user_role_assigned`

Maps to:

| Audit field       | Value                                                                           |
| ----------------- | ------------------------------------------------------------------------------- |
| `SourceModule`    | `Authorization`                                                                 |
| `Action`          | `UserRoleAssigned`                                                              |
| `ActionCategory`  | `Authorization`                                                                 |
| `ResourceType`    | `UserRole`                                                                      |
| `ResourceId`      | Prefer `AggregatePublicId`; otherwise `{userId}:{roleId}`                       |
| `ActorInternalId` | envelope `InitiatorUserId` or payload `assignedByUserId`                        |
| `ActorUserId`     | payload/header public actor id when available                                   |
| `Outcome`         | `Success` unless payload says otherwise                                         |
| `Severity`        | `Warning`                                                                       |
| `RiskLevel`       | `High`                                                                          |
| `MetadataJson`    | safe subset containing `userId`, `userPublicId`, `roleId`, `roleName`, `reason` |

### 10.2 `authorization.user_role_revoked`

Maps to:

| Audit field       | Value                                                                           |
| ----------------- | ------------------------------------------------------------------------------- |
| `SourceModule`    | `Authorization`                                                                 |
| `Action`          | `UserRoleRevoked`                                                               |
| `ActionCategory`  | `Authorization`                                                                 |
| `ResourceType`    | `UserRole`                                                                      |
| `ResourceId`      | Prefer `AggregatePublicId`; otherwise `{userId}:{roleId}`                       |
| `ActorInternalId` | envelope `InitiatorUserId` or payload `revokedByUserId`                         |
| `ActorUserId`     | payload/header public actor id when available                                   |
| `Outcome`         | `Success` unless payload says otherwise                                         |
| `Severity`        | `Warning`                                                                       |
| `RiskLevel`       | `High`                                                                          |
| `MetadataJson`    | safe subset containing `userId`, `userPublicId`, `roleId`, `roleName`, `reason` |

### 10.3 `authorization.role_permission_granted`

Maps to:

| Audit field       | Value                                                                                  |
| ----------------- | -------------------------------------------------------------------------------------- |
| `SourceModule`    | `Authorization`                                                                        |
| `Action`          | `RolePermissionGranted`                                                                |
| `ActionCategory`  | `Authorization`                                                                        |
| `ResourceType`    | `RolePermission`                                                                       |
| `ResourceId`      | Prefer `AggregatePublicId`; otherwise `{roleId}:{permissionId}`                        |
| `ActorInternalId` | envelope `InitiatorUserId` or payload `grantedByUserId`                                |
| `ActorUserId`     | payload/header public actor id when available                                          |
| `Outcome`         | `Success` unless payload says otherwise                                                |
| `Severity`        | `Warning`                                                                              |
| `RiskLevel`       | `Critical` for sensitive permissions, otherwise `High`                                 |
| `MetadataJson`    | safe subset containing `roleId`, `roleName`, `permissionId`, `permissionKey`, `reason` |

### 10.4 `authorization.role_permission_revoked`

Maps to:

| Audit field       | Value                                                                                  |
| ----------------- | -------------------------------------------------------------------------------------- |
| `SourceModule`    | `Authorization`                                                                        |
| `Action`          | `RolePermissionRevoked`                                                                |
| `ActionCategory`  | `Authorization`                                                                        |
| `ResourceType`    | `RolePermission`                                                                       |
| `ResourceId`      | Prefer `AggregatePublicId`; otherwise `{roleId}:{permissionId}`                        |
| `ActorInternalId` | envelope `InitiatorUserId` or payload `revokedByUserId`                                |
| `ActorUserId`     | payload/header public actor id when available                                          |
| `Outcome`         | `Success` unless payload says otherwise                                                |
| `Severity`        | `Warning`                                                                              |
| `RiskLevel`       | `Critical` for sensitive permissions, otherwise `High`                                 |
| `MetadataJson`    | safe subset containing `roleId`, `roleName`, `permissionId`, `permissionKey`, `reason` |

---

## 11. Consistency Contract

Audit ingestion is eventually consistent with producer module truth.

A successful producer command means:

* producer truth committed
* producer outbox message committed when async work is required

It does not mean:

* RabbitMQ publication already completed
* Audit consumer already processed the message
* AuditLog is immediately queryable
* Audit dashboard is already updated
* Notifications have been sent

Audit consumer lag must be observable, but it must not redefine producer module truth.

Audit APIs do not provide read-your-writes guarantees for just-committed source actions.

---

## 12. Failure and Retry Contract

Audit ingestion must be safe under:

* broker redelivery
* worker restart
* consumer crash after partial work
* timeout during Audit DB insert
* retry after ambiguous failure
* replay of retained messages
* duplicate delivery from producer-side publication ambiguity

Rules:

* timeout is ambiguous
* retry must rely on `MessageId` dedupe
* duplicate insert is treated as already processed
* poison messages should be routed to consumer-side terminal handling or DLQ
* consumer-side failures must not be written back as producer-side outbox failures
* `AuditIngestion` tracks consumer-side status
* `OutboxMessage.Status` remains producer-side publication status

---

## 13. Query Contract

Audit query contracts support investigation and operations.

V1 query contracts should support:

* list audit logs with pagination
* filter by source module
* filter by action
* filter by actor
* filter by resource
* filter by outcome
* filter by severity
* filter by risk level
* filter by correlation id
* lookup by message id
* lookup by public id
* resource timeline
* actor timeline
* correlation timeline
* recent high-risk events
* lightweight dashboard summary

### 13.1 Bounded queries

Audit queries must be bounded.

Production list/timeline queries should support:

* `fromUtc`
* `toUtc`
* `page`
* `pageSize`

Unbounded full-table queries are not acceptable for production operations.

### 13.2 Timeline ordering

Resource and actor timelines may support multiple ordering modes:

* source chronology by `OccurredAtUtc`
* ingestion chronology by `IngestedAtUtc`
* aggregate chronology by `AggregateVersion` when available

---

## 14. Partitioning Readiness Contract

Audit V1 is partition-ready but not physically partitioned by default.

V1 relies on:

* SQL indexes
* bounded time-range queries
* source module metadata
* risk metadata
* consumer lag observability

Future partitioning may include:

* time-range partitioning for `AuditLog`
* worker lane partitioning by source module or risk priority
* queue partitioning by module, category, or priority
* batch/window partitioning for summary, digest, archive, or reconciliation workflows
* logical lane routing through indirection rather than direct `hash(key) mod N`

Future partitioning must not change:

* append-only evidence semantics
* `MessageId` dedupe requirement
* source module truth ownership
* audit investigation correctness
* eventual consistency expectations

---

## 15. Cache Extension Contract

Audit V1 does not require Redis caching.

SQL remains the source of audit evidence truth.

Future Redis usage may be introduced only as optional acceleration for:

* dashboard summaries
* recent high-risk event panels
* module/action metadata
* TTL-bound alert dedupe hints

Redis must not be used as:

* audit evidence truth
* the only dedupe mechanism for critical AuditLog append behavior
* a replacement for SQL investigation detail queries

Investigation-critical detail APIs must be able to read from SQL directly.

---

## 16. Future Extension Contract

Audit V1 keeps runtime scope simple but defines extension paths.

### 16.1 Future alerting

Future AuditAlert support should be rule-based and idempotent.

Alert creation must use durable business dedupe keys to avoid duplicate alerts under retry, replay, or batch rerun.

Possible extension contracts:

* `AuditAlert`
* `IAuditRealtimeRule`
* `IAuditWindowRule`
* `AuditAlertRaised` event

### 16.2 Future notification integration

Audit must not send emails or external notifications directly.

If Audit needs to notify admins, it should emit an audit alert event through Outbox and let the Notifications module handle delivery.

### 16.3 Future batch workflows

Future batch workflows such as summaries, digests, archive jobs, or reconciliation jobs must define:

* bounded input
* explicit stage sequence
* output contract
* rerun behavior
* publication/cutover behavior when correctness-sensitive
* observability signals

### 16.4 Future materialized summaries

Future materialized summaries, digests, reports, and archive outputs are derived state.

They must remain rebuildable from approved durable sources such as:

* `AuditLog`
* retained Outbox records
* source module truth/history
* bounded reconciliation inputs

They must not replace `AuditLog` as evidence truth.

### 16.5 Future tamper-evident evidence

`Hash` and `PrevHash` are reserved V2 hooks.

Tamper-evident hash chain enforcement is not required for V1 correctness.

---

## 17. Out of Scope for V1

The following are out of scope for V1 unless explicitly added later:

* global total ordering of audit events
* tamper-evident hash chain enforcement
* exactly-once broker delivery
* synchronous audit writes inside producer module transactions
* cross-module transaction between producer modules and Audit
* automatic reconstruction of missing audit events without authoritative producer history
* full audit rule engine
* realtime notification delivery
* batch-based digest generation
* materialized audit summary tables
* archive and retention jobs
* warehouse or analytics pipeline
* physical table partitioning
* full DB sharding
* Redis-backed audit evidence
* Redis-only dedupe for critical audit evidence

---

## 18. Summary

Audit V1 is intentionally simple but extension-ready.

The required V1 domain contract is:

* consume audit-relevant messages asynchronously
* use the shared Outbox `MessageId` as the canonical idempotency key
* store append-only SQL-backed `AuditLog` evidence
* track consumer-side processing through `AuditIngestion`
* expose investigation-oriented query contracts
* keep dashboard summaries lightweight and SQL-backed

Future capabilities such as alerting, digest generation, reporting, archival, cache acceleration, partitioning, and notification integration may be added later through explicit extension contracts.

These extensions must not change the core ingestion contract, must not redefine source module business truth, and must remain idempotent, observable, bounded, and rebuildable where applicable.
