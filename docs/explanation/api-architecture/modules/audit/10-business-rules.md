# Audit Module — Business Rules

## 1. Purpose

This document defines the business rules for the Audit module in CommercialNews V1.

Audit provides investigation-ready evidence for important system actions. It records what happened, who caused it, which resource was affected, which module produced the event, and how the action should be interpreted from governance, security, and operational perspectives.

Audit V1 intentionally keeps the implementation scope simple while remaining open for future business extensions such as alerting, digest generation, reporting, archival, reconciliation, and notification integration.

## 2. Business Positioning

Audit is an evidence module.

Audit owns durable audit evidence about system actions, but it does not own the source modules' business truth.

Examples:

* Identity owns whether a user account is locked.
* Audit owns the evidence that a `UserLocked` action happened.
* Content owns whether an article is published.
* Audit owns the evidence that an `ArticlePublished` action happened.
* Authorization owns role and permission assignments.
* Audit owns the evidence that a permission or role assignment changed.

Audit must not be used as the authority for current business state.

## 3. V1 Business Scope

Audit V1 supports the following business capabilities:

1. Ingest audit-relevant messages from source modules.
2. Deduplicate audit evidence by `MessageId`.
3. Store append-only audit evidence in SQL.
4. Track consumer-side ingestion state.
5. Query audit logs for investigation.
6. Query audit logs by module, actor, resource, message, correlation, action, risk, and time range.
7. Provide lightweight dashboard summaries directly from `AuditLog`.

Audit V1 does not require:

1. Batch jobs.
2. Materialized audit summaries.
3. Audit digest generation.
4. Complex alert rule engine.
5. Direct notification delivery.
6. Archive or retention jobs.
7. Compliance reporting pipeline.
8. Redis-backed audit query cache.

These are future extension candidates.

## 4. Core Business Rules

### BR-AUD-001 — Audit is asynchronous

Source modules must not synchronously write to Audit-owned tables.

Audit records are created through the approved asynchronous pipeline:

```text
Source module truth change
    → Outbox message committed
    → Worker publishes to broker
    → CommercialNews.Worker audit consumer receives message
    → Worker sends IngestAuditEventCommand through MediatR
    → Audit.Application normalizes and stores AuditLog
```

A source module command succeeds when its own truth transaction and outbox intent commit. It does not wait for audit ingestion.

### BR-AUD-002 — AuditLog is append-only evidence truth

`AuditLog` records are append-only by default.

Audit records must not be updated or deleted during normal business operation.

Any purge, archival, masking, or deletion policy must be explicitly defined before being introduced.

### BR-AUD-003 — Audit does not replace source truth

Audit must not be used to determine the current authoritative state of source resources.

Examples:

* Do not use Audit to determine whether a user is currently active.
* Do not use Audit to determine whether an article is currently published.
* Do not use Audit to determine current role or permission assignment.
* Do not use Audit to determine notification delivery truth.

Audit provides evidence and investigation context only.

### BR-AUD-004 — Audit evidence must be deduplicated by MessageId

Audit must not create duplicate `AuditLog` records for the same `MessageId`.

`MessageId` is the system-wide async message identity.

Audit V1 uses durable SQL uniqueness to protect audit append behavior under at-least-once delivery, retry, timeout ambiguity, broker redelivery, and replay.

### BR-AUD-005 — AuditIngestion tracks consumer-side processing

Audit must track consumer-side ingestion separately from producer-side outbox publication.

Outbox `Published` means broker handoff succeeded. It does not mean Audit has processed the message.

`AuditIngestion` records the Audit consumer's processing state.

### BR-AUD-006 — Timeout outcomes are ambiguous

A timeout during audit ingestion does not prove that no audit record was written.

Audit retry behavior must assume the previous attempt may have partially or fully succeeded.

Retry safety is achieved through durable `MessageId` deduplication.

### BR-AUD-007 — Audit is eventually consistent

Audit logs may lag behind source module actions.

Audit APIs do not provide read-your-writes guarantees for just-committed source actions.

Temporary audit lag is acceptable, but it must be observable.

### BR-AUD-008 — Audit must preserve traceability

Audit records should preserve enough metadata to support investigation:

* `MessageId`
* `EventType`
* `AggregateType`
* `AggregateId`
* `AggregateVersion`
* `OccurredAtUtc`
* `IngestedAtUtc`
* `CorrelationId`
* `TraceId`
* `SourceModule`
* `Action`
* `Actor`
* `Resource`
* `Outcome`
* `Severity`
* `RiskLevel`
* `SanitizedPayloadJson`

Raw input payloads from Outbox must be sanitized before storage. Stored or
returned Audit payloads should use `SanitizedPayloadJson` naming.

### BR-AUD-009 — Slug is metadata, not identity

Audit must follow the system public identifier strategy.

Audit-owned records should expose stable `PublicId` values through APIs.

Source resource references should use stable cross-module identifiers, preferably ULID-based `PublicId`.

Slugs may be stored as metadata for investigation, but they must not be treated as stable resource identity.

### BR-AUD-010 — Redis is not audit truth

Redis must not be used as audit evidence truth.

Redis must not be the only dedupe mechanism for critical audit append behavior.

Future Redis usage is allowed only as an optional acceleration layer for non-authoritative dashboard summaries, recent event panels, module/action metadata, or TTL-bound alert hints.

## 5. Auditable Event Rules

### BR-AUD-011 — Security-sensitive actions must be audited

Identity and Authorization events that affect security, access, authentication, or governance must be audited.

Examples:

* User registered
* Email verified
* Login succeeded
* Login failed
* Password changed
* Password reset requested
* Password reset completed
* User locked
* User unlocked
* User deactivated
* Refresh token revoked
* All sessions revoked
* Role created
* Role updated
* Role deleted
* Permission created
* Role permission granted
* Role permission revoked
* User role granted
* User role revoked

### BR-AUD-012 — Governance-sensitive content actions must be audited

Content and SEO events that affect publication, editorial governance, or public routing should be audited.

Examples:

* Article created
* Article updated
* Article published
* Article unpublished
* Article archived
* Article deleted
* Category created
* Tag created
* Slug changed
* SEO metadata updated

### BR-AUD-013 — Moderation-sensitive interaction actions must be audited

Interaction events that affect moderation, public comments, reports, or abuse handling should be audited.

Examples:

* Comment created
* Comment deleted by author
* Comment hidden by moderator
* Comment restored by moderator
* Comment reported
* Report dismissed
* Reported comment hidden

### BR-AUD-014 — Media governance actions should be audited

Media actions that affect article attachments, primary selection, ordering, deletion, or file governance should be audited.

Examples:

* Media uploaded
* Media deleted
* Media attached to article
* Media detached from article
* Primary media changed
* Media reordered

### BR-AUD-015 — Notification delivery failures may be audited

Notification events may be audited when they are operationally or governance relevant.

Examples:

* Email delivery failed
* Email delivery dead-lettered
* Critical delivery retry exhausted

Routine successful delivery does not always need to be audited unless the business requires it.

## 6. Actor Rules

### BR-AUD-016 — Actor must be captured when available

Audit should capture the actor that caused the action when available.

Supported actor types:

* User
* Admin
* Moderator
* System
* Worker
* Anonymous
* External

### BR-AUD-017 — System actions must be distinguishable from user actions

System-generated and worker-generated actions must not be misrepresented as human user actions.

If the actor is a system process, `ActorType` should indicate `System` or `Worker`.

### BR-AUD-018 — Actor identity must be stable

Actor identifiers exposed across modules or APIs should use stable opaque identifiers.

Audit should not depend on internal numeric primary keys for cross-module actor identity.

## 7. Resource Rules

### BR-AUD-019 — Resource must identify the affected domain object

An audit record should identify the affected resource through:

* `ResourceType`
* `ResourceId`
* optional `ResourceDisplayName`

Examples of resource types:

* UserAccount
* Role
* Permission
* Article
* Comment
* MediaAsset
* SeoRoute
* EmailDelivery
* AuditIngestion

### BR-AUD-020 — ResourceId should be stable

`ResourceId` should be a stable source-module identifier, preferably a `PublicId` or other opaque cross-module identifier.

Internal numeric IDs should not be required for audit investigation APIs.

## 8. Outcome Rules

### BR-AUD-021 — Every audit record must have an outcome

Supported outcomes:

* Success
* Failure
* Denied
* Ignored

Outcome meaning:

* `Success`: action completed successfully.
* `Failure`: action failed.
* `Denied`: action was rejected by authorization or business policy.
* `Ignored`: event was intentionally ignored.

### BR-AUD-022 — Consumer processing result is not the same as business outcome

The business outcome describes the source action.

The ingestion status describes whether the Audit consumer processed the message.

Example:

* Business outcome: `Success`
* AuditIngestion status: `Failed`

This means the source action succeeded, but Audit consumer processing failed.

## 9. Severity and Risk Rules

### BR-AUD-023 — Severity and RiskLevel must be separated

Severity describes operational seriousness.

RiskLevel describes security or business sensitivity.

They must not be treated as the same concept.

### BR-AUD-024 — Supported severity values

Supported severity values:

* Info
* Warning
* Error
* Critical

Examples:

* Normal successful login: `Info`
* Repeated failed login: `Warning`
* Audit ingestion failure: `Error`
* Critical consumer pipeline failure: `Critical`

### BR-AUD-025 — Supported risk levels

Supported risk levels:

* Low
* Medium
* High
* Critical

Examples:

* Article published: `Medium`
* User locked: `High`
* Role permission granted for sensitive permission: `Critical`
* Refresh token reuse detected: `Critical`

### BR-AUD-026 — High-risk actions must be easily discoverable

Audit APIs and dashboard queries must support filtering by `RiskLevel`.

Admin users should be able to quickly find high-risk and critical actions.

## 10. Query and Investigation Rules

### BR-AUD-027 — Audit queries must be bounded

Audit list and timeline queries should support time-range filtering and pagination.

Unbounded full-table queries are not acceptable for production operations.

### BR-AUD-028 — Resource timeline must be supported

Audit should support investigation by resource.

Example questions:

* Who changed this article?
* Who locked this user?
* Who hid this comment?
* Who changed this permission?

### BR-AUD-029 — Actor timeline must be supported

Audit should support investigation by actor.

Example questions:

* What did this admin do today?
* Which moderator hid comments?
* Which user repeatedly triggered denied actions?

### BR-AUD-030 — Correlation-based investigation must be supported

Audit should support investigation by `CorrelationId`.

This helps trace actions produced by the same request or workflow.

### BR-AUD-031 — Message-based investigation must be supported

Audit should support lookup by `MessageId`.

This helps connect Audit records to Outbox, worker logs, broker delivery, retries, and ingestion records.

## 11. Dashboard Rules

### BR-AUD-032 — V1 dashboard summaries are lightweight

Audit V1 dashboard summaries may be computed directly from `AuditLog` using indexed SQL queries.

V1 does not require materialized dashboard summary tables.

### BR-AUD-033 — Dashboard summaries are not evidence truth

Dashboard summaries, counts, recent-risk panels, and module statistics are derived views.

They must not replace `AuditLog` as audit evidence.

### BR-AUD-034 — Dashboard staleness must be acceptable

If future caching is introduced for dashboard summaries, cached dashboard values may be stale for a short time.

Investigation-critical detail views must still read from SQL-backed audit evidence.

## 12. Partitioning Readiness Rules

### BR-AUD-035 — Audit V1 is partition-ready, not physically partitioned

Audit V1 does not introduce physical table partitioning or full sharding by default.

Audit V1 relies on:

* SQL indexes
* bounded time-range queries
* source module metadata
* risk metadata
* consumer lag observability

### BR-AUD-036 — Future partitioning must be signal-driven

Future Audit partitioning may be introduced only when sustained metrics justify it.

Relevant signals include:

* query P95/P99 degradation
* growing AuditLog table/index pressure
* audit consumer lag
* oldest uningested message age
* retry or DLQ pressure
* security audit delayed by high-volume non-security events
* archive or report windows becoming too slow

### BR-AUD-037 — Future partitioning must preserve semantics

Future partitioning must not change:

* append-only evidence semantics
* `MessageId` dedupe
* source module truth ownership
* audit investigation correctness
* eventual consistency expectations

## 13. Future Extension Rules

### BR-AUD-038 — Future alerting must be rule-based and idempotent

Future AuditAlert support should use rule-based extension points.

Alert creation must use durable business dedupe keys to avoid duplicate alerts under retry, replay, or batch rerun.

### BR-AUD-039 — Future notification delivery must go through Notifications

Audit must not send emails or external notifications directly.

If Audit needs to notify admins, it should emit an audit alert event through Outbox and let the Notifications module handle delivery.

### BR-AUD-040 — Future batch workflows must use bounded inputs

Future batch workflows such as summary, digest, archive, or reconciliation jobs must define bounded input windows.

Examples:

* daily audit summary
* hourly risk scan
* monthly archive window
* failed ingestion reconciliation window

### BR-AUD-041 — Future batch outputs must be rerun-safe

Reprocessing the same bounded input must not create duplicate alerts, duplicate digests, or double-counted summaries.

### BR-AUD-042 — Future materialized outputs must be derived

Future materialized summaries, digests, reports, and archive outputs are derived state.

They must remain rebuildable from approved durable sources such as `AuditLog`, retained Outbox records, source module truth/history, or bounded reconciliation inputs.

### BR-AUD-043 — Future cache usage must remain derived-only

Future Redis usage for Audit must remain optional and derived-only.

Redis may accelerate dashboard summaries or metadata lookups, but SQL remains the source of audit evidence truth.

## 14. Out of Scope for V1

The following are out of scope for Audit V1:

* Full audit rule engine
* Realtime notification delivery
* Batch-based digest generation
* Materialized audit summary tables
* Archive and retention jobs
* Warehouse or analytics pipeline
* Physical table partitioning
* Full DB sharding
* Redis-backed audit evidence
* Exactly-once delivery claims

## 15. Summary

Audit V1 is intentionally simple but extension-ready.

The required V1 business capability is to ingest audit-relevant messages asynchronously, deduplicate by `MessageId`, store append-only SQL-backed audit evidence, track consumer-side ingestion, and expose investigation-oriented admin queries.

Future capabilities such as alerting, digest generation, reporting, archival, cache acceleration, partitioning, and notification integration may be added later through explicit extension points. These extensions must not change the core ingestion contract, must not redefine source module business truth, and must remain idempotent, observable, and rebuildable where applicable.
