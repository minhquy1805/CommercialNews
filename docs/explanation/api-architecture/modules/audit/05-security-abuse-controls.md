# Audit — Security & Abuse Controls (V1)

## 1. Purpose

This document defines security, privacy, redaction, access-control, and abuse-control rules for the Audit module in CommercialNews V1.

Audit contains investigation-sensitive evidence about actions across the system. Because Audit may contain security, governance, identity, moderation, and operational records, access must be tightly controlled.

Audit V1 exposes admin read APIs only. Audit ingestion happens asynchronously through the Worker and is not exposed publicly.

---

## 2. Security Posture

Audit V1 must protect:

* audit evidence integrity
* audit query confidentiality
* sensitive actor/resource metadata
* sanitized payload and metadata fields
* consumer-side ingestion diagnostics
* operational failure information
* correlation and trace identifiers

Audit must not become:

* a public API surface
* a raw event payload browser
* a secret/token storage location
* a bypass around source module authorization
* a replacement for source module business truth
* a side channel for extracting sensitive user or system data

---

## 3. Access Control

### 3.1 Admin-only APIs

All endpoints under:

```http
/api/v1/admin/audit/*
```

must require:

* Bearer authentication
* explicit admin authorization policy
* least-privilege permission checks

Audit APIs must not be accessible through public or anonymous routes.

### 3.2 Recommended permissions

Recommended V1 permissions:

* `Audit.Read`
* `Audit.ReadDashboard`
* `Audit.ReadIngestion`
* `Audit.ReadOperationalHealth`
* `Audit.ReadSensitive`

Suggested meaning:

| Permission                    | Meaning                                                       |
| ----------------------------- | ------------------------------------------------------------- |
| `Audit.Read`                  | Read standard audit log list/detail                           |
| `Audit.ReadDashboard`         | Read dashboard summaries and recent risk panels               |
| `Audit.ReadIngestion`         | Read consumer-side ingestion records                          |
| `Audit.ReadOperationalHealth` | Read operational health or lag views                          |
| `Audit.ReadSensitive`         | Read sensitive-but-redacted metadata where explicitly allowed |

Future permissions may include:

* `Audit.Alert.Read`
* `Audit.Alert.Manage`
* `Audit.Replay.Execute`
* `Audit.Retention.Read`
* `Audit.Retention.Manage`
* `Audit.Export.Execute`

### 3.3 Least privilege

Not every admin should automatically see all audit data.

Access should be narrowed by policy where practical:

* security admins may view identity/security events
* governance admins may view authorization changes
* moderators may view moderation audit events
* platform operators may view ingestion failures and operational metrics
* only highly trusted roles should access sensitive metadata

### 3.4 Sensitive read policy

Raw or near-raw payload access is not required for V1.

If sensitive metadata viewing is introduced later, it must require stronger permission such as:

```text
Audit.ReadSensitive
```

Even then, returned data must be redacted.

---

## 4. Ingestion Security

### 4.1 No public ingestion API

Audit ingestion must not be exposed through HTTP public/admin endpoints in V1.

Allowed ingestion path:

```text
Outbox → RabbitMQ → Audit Consumer → AuditLog / AuditIngestion
```

### 4.2 Source trust boundary

Audit consumers should only process messages from approved broker exchanges/queues and known event contracts.

Audit should reject, ignore, or dead-letter messages that are:

* unsupported
* malformed
* from unknown event families
* missing `MessageId`
* missing required event metadata
* violating redaction policy
* violating schema expectations

### 4.3 MessageId requirement

Every ingested message must have a stable `MessageId`.

`MessageId` is required for:

* dedupe
* retry safety
* replay safety
* incident correlation
* outbox-to-audit tracing

Messages without valid `MessageId` should not create `AuditLog` evidence.

### 4.4 Payload size limits

Audit ingestion should enforce payload size limits.

Oversized payloads may indicate:

* accidental raw domain dump
* abuse
* schema mistake
* unsafe event contract
* sensitive content leakage

Oversized payload handling should be:

* visible
* safe
* redacted
* routed to failure/DLQ policy where appropriate

---

## 5. Privacy and Redaction

### 5.1 Non-negotiable redaction rule

Audit must never store or return unsafe secrets.

The following must not be stored in `AuditLog`, `AuditIngestion`, API responses, logs, dashboard payloads, or error messages:

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
* raw provider secrets
* raw connection strings
* secret configuration values

### 5.2 Sensitive PII minimization

Audit should minimize personally identifiable information.

Prefer stable identifiers over full personal data.

Preferred:

```text
ActorUserId
ResourceId
CorrelationId
MessageId
```

Use cautiously:

```text
ActorEmail
ActorDisplayName
IpAddress
UserAgent
```

Avoid unless explicitly justified:

```text
full address
phone number
government identity numbers
full raw profile payload
large unbounded content body
```

### 5.3 Article/content payload policy

Audit should not store full article body content by default.

For content events, prefer:

* article public id
* title snapshot when safe
* slug as metadata
* lifecycle action
* changed field names
* reason
* actor
* timestamps

Full article body storage requires explicit policy approval.

### 5.4 Identity payload policy

Identity audit payloads must never include:

* password
* password hash
* verification token
* reset token
* refresh token
* access token
* session cookie
* raw security stamp if treated as secret

For Identity events, prefer:

* user public id
* actor public id
* action
* outcome
* reason code
* safe email domain or masked email where needed
* correlation id

### 5.5 Authorization payload policy

Authorization audit payloads may include:

* role id
* role public id
* role name
* permission id
* permission key
* target user public id
* reason

Authorization audit payloads must not include:

* unrelated user profile data
* full permission cache snapshots unless explicitly approved
* secret policy internals not intended for admin review

### 5.6 Redaction must happen before persistence

Redaction must happen before writing:

* `AuditLog.RawPayloadJson`
* `AuditLog.MetadataJson`
* `AuditLog.HeadersJson`
* `AuditLog.BeforeJson`
* `AuditLog.AfterJson`
* `AuditLog.ChangesJson`
* application logs

Audit must not persist unsafe raw payload first and redact later.

### 5.7 Redaction failure

If redaction cannot be completed safely:

* do not persist unsafe payload
* mark ingestion as failed or dead-lettered according to policy
* record sanitized failure metadata only
* expose operational failure without leaking unsafe data

---

## 6. API Response Safety

### 6.1 Default response shape

Audit API responses should return normalized, safe fields by default:

* `publicId`
* `messageId`
* `sourceModule`
* `eventType`
* `action`
* `actionCategory`
* `actor`
* `resource`
* `outcome`
* `severity`
* `riskLevel`
* `summary`
* `correlationId`
* `occurredAtUtc`
* `ingestedAtUtc`

### 6.2 Avoid raw payload exposure in V1

V1 should not expose raw event payload by default.

If a future endpoint exposes raw or near-raw payload:

* require `Audit.ReadSensitive`
* return redacted payload only
* never return blocked secret fields
* log access to sensitive audit details
* consider audit-of-audit for sensitive payload reads

### 6.3 Ingestion error response safety

AuditIngestion APIs may expose:

* `messageId`
* `eventType`
* `status`
* `attemptCount`
* `lastErrorCode`
* `lastErrorClass`
* sanitized `lastErrorMessage`

They must not expose:

* raw payload
* raw headers
* full stack traces
* SQL text with values
* provider secrets
* tokens or credentials

### 6.4 Dashboard response safety

Dashboard APIs must not return raw payloads.

Allowed dashboard fields include:

* counts
* grouped statistics
* recent event summaries
* high-risk event summaries
* failed ingestion counts
* duplicate counts
* lag/freshness metadata

Dashboard output is derived and must not expose more sensitive detail than the underlying detail API would allow.

---

## 7. Abuse Controls for Query APIs

Audit search can become expensive and sensitive.

### 7.1 Bounded queries

Audit list and timeline queries must be bounded.

Recommended controls:

* require pagination
* enforce maximum `pageSize`
* enforce allowlisted sort fields
* require time range for high-volume queries where policy requires
* cap maximum time-window size
* cap maximum export size if export is introduced later

### 7.2 Sort allowlist

Sort must be allowlisted.

Recommended V1 sort fields:

* `occurredAtUtc`
* `ingestedAtUtc`
* `riskLevel`
* `severity`
* `sourceModule`

Disallow arbitrary SQL sort fields.

### 7.3 Filter allowlist

Filter fields must be allowlisted.

Recommended V1 filters:

* `sourceModule`
* `eventType`
* `action`
* `actionCategory`
* `actorUserId`
* `actorInternalId`
* `resourceType`
* `resourceId`
* `outcome`
* `severity`
* `riskLevel`
* `correlationId`
* `messageId`
* `fromUtc`
* `toUtc`

Disallow arbitrary JSON/path filtering unless explicitly designed and indexed.

### 7.4 Time range controls

High-volume audit queries should use time ranges.

Policy options:

* require `fromUtc` and `toUtc` for `/logs`
* default to a recent window when no range is supplied
* cap the maximum window size
* require stronger permission for long historical queries

### 7.5 Rate limiting

Audit admin endpoints may be rate-limited.

Especially:

* `/logs`
* `/logs/by-correlation/{correlationId}`
* `/resources/{resourceType}/{resourceId}/timeline`
* `/actors/{actorUserId}/timeline`
* `/dashboard/summary`
* future export endpoints

Rate limiting is an abuse-control layer, not the primary authorization boundary.

### 7.6 Expensive query protection

The API should reject or constrain queries that would cause unsafe load.

Examples:

* no time range + large page size
* unsupported sort
* unindexed free-text search
* unbounded JSON payload search
* excessive dashboard window
* repeated wide historical scans

---

## 8. Safe Logging

### 8.1 Safe log fields

Audit system logs may include:

* `MessageId`
* `CorrelationId`
* `EventType`
* `SourceModule`
* `Action`
* `ResourceType`
* `ResourceId`
* `ActorUserId`
* `ActorInternalId`
* processing outcome
* retry attempt
* error code
* error class

### 8.2 Unsafe log fields

Audit system logs must not echo raw event payloads blindly.

Do not log:

* raw payload
* raw headers
* tokens
* passwords
* password hashes
* session cookies
* raw authorization headers
* API keys
* stack traces with sensitive values
* raw SQL command values containing sensitive data

### 8.3 Error logging

Internal logs may contain more diagnostic detail than API responses, but they must still be secret-safe.

For redaction violations:

* log sanitized event identity
* log `MessageId`
* log `EventType`
* log failure class
* do not log unsafe field values

---

## 9. Audit-of-Audit

### 9.1 V1 posture

V1 does not require a full audit-of-audit workflow.

However, sensitive Audit API access should be considered for future audit-of-audit coverage.

Examples:

* reading sensitive audit detail
* accessing redacted payload metadata
* exporting audit logs
* replaying failed audit messages
* changing retention policy
* resolving future AuditAlert records

### 9.2 Future audit-of-audit

If introduced, audit-of-audit must be append-only and must not create recursive uncontrolled ingestion loops.

A future policy should define:

* which Audit API actions are audited
* how to prevent infinite audit recursion
* what metadata is safe to store
* who can view audit-of-audit records

---

## 10. Future Replay / Remediation Security

Replay and remediation APIs are not required in V1.

If introduced later, they must require stronger controls:

* explicit permission such as `Audit.Replay.Execute`
* bounded replay scope
* original `MessageId` preservation
* idempotency
* audit-of-audit record
* operator identity capture
* reason field
* dry-run option where useful
* approval workflow for sensitive scopes where needed

Replay must not:

* create duplicate `AuditLog`
* rewrite existing evidence
* synthesize evidence without approved source message/correction policy
* use RabbitMQ as permanent history by assumption

---

## 11. Future Alert / Notification Security

AuditAlert and notification integration are not required for V1.

If introduced later:

* Audit may own alert state.
* Notifications owns actual delivery.
* Audit must not send email directly.
* Alert creation must be idempotent.
* Alert dedupe keys must be durable.
* Alert acknowledgement/resolution must require explicit permission.
* Alert notification payloads must be minimal and redacted.

Example future flow:

```text
AuditAlert created
    ↓
Audit emits AuditAlertRaised through Outbox
    ↓
Notifications module performs delivery
```

---

## 12. Cache Security

### 12.1 V1 posture

Audit V1 does not require Redis caching.

SQL remains audit evidence truth.

### 12.2 Future cache controls

If Redis cache is introduced later for Audit:

Allowed:

* dashboard summary cache
* recent high-risk event panel cache
* module/action metadata cache
* TTL-bound alert dedupe hints

Forbidden:

* Redis as `AuditLog` truth
* Redis as `AuditIngestion` truth
* Redis-only dedupe for critical audit evidence
* Redis as replacement for SQL detail queries
* Redis as source module business truth

### 12.3 Cache privacy

Cached Audit values must follow the same redaction rules as API responses.

Do not cache unsafe payloads or sensitive metadata.

---

## 13. Partitioning and Data Exposure

### 13.1 V1 posture

Audit V1 is partition-ready but not physically partitioned by default.

V1 relies on:

* SQL indexes
* bounded queries
* query limits
* source module metadata
* risk metadata
* observability

### 13.2 Future partitioning security

If future partitioning is introduced:

* partition boundaries must not bypass authorization
* worker lanes must preserve `MessageId` dedupe
* high-risk/security audit lanes must remain observable
* no partition should expose broader data than the caller is authorized to view
* partition routing metadata must not leak sensitive information unnecessarily

---

## 14. Security Invariants

Audit security depends on these invariants:

1. Audit APIs are admin-only.
2. Audit read access is least-privilege.
3. Audit ingestion is not publicly exposed.
4. `MessageId` is required for ingestion dedupe.
5. Audit evidence is SQL-backed and append-only.
6. Redis is never audit evidence truth.
7. Raw payload exposure is avoided in V1.
8. Secrets and tokens are never stored or returned.
9. Redaction happens before persistence.
10. Audit logs do not replace source module authorization or truth.
11. Queries are bounded and rate-limited where needed.
12. Error responses do not leak unsafe internals.
13. Safe logging uses identifiers and outcomes, not raw payload dumps.
14. Future replay/remediation requires strong authorization and audit-of-audit.
15. Future alert/notification delivery must go through Notifications, not direct Audit email sending.

---

## 15. Summary

Audit V1 security focuses on:

* admin-only access
* least privilege
* strict redaction
* safe payload minimization
* bounded query behavior
* safe logging
* consumer-side ingestion diagnostics without secret leakage
* SQL-backed evidence integrity

Audit V1 deliberately does not expose:

* public ingestion endpoints
* manual evidence mutation APIs
* raw payload browsing
* replay execution APIs
* archive execution APIs
* alert management APIs
* cache control APIs
* partition management APIs

Future extensions may add replay, audit-of-audit, alerts, notifications, cache acceleration, exports, archival, or partitioning, but they must preserve the same rule:

```text
Audit is evidence-sensitive. Expose less by default, redact before persistence, and never let operational convenience weaken investigation trust.
```
