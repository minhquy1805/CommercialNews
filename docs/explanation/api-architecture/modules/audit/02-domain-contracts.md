# Audit — Domain Contracts (V1)

## 1) Ownership

Audit owns:
- audit log persistence
- append-only audit evidence model
- audit ingestion policy
- audit redaction policy
- audit ingestion idempotency rules
- audit query contracts for investigation and operations

Audit does not own:
- domain action execution in producer modules such as Content, Authorization, Identity, Media, SEO, or Interaction
- producer module truth state
- notification delivery
- authorization decisions
- producer-side outbox publishing

Producer modules own:
- business truth mutations
- emitted event correctness
- event payload shape
- producer-side outbox creation in the same local transaction as the truth mutation

Audit consumes published integration events and records them as evidence.

Audit must not write back to producer module truth tables.

---

## 2) Entity: AuditLog (conceptual)

Fields (conceptual):

- `AuditId`
  - unique audit record identifier

- `EventId`
  - Audit-local idempotency key
  - stores the upstream async `MessageId`
  - must be unique

- `SourceModule`
  - producer module name
  - examples: `Authorization`, `Identity`, `Content`, `Media`

- `EventType`
  - original integration event type
  - example: `authorization.user_role_assigned`

- `Action`
  - normalized audit action name
  - example: `UserRoleAssigned`

- `ResourceType`
  - audited resource category
  - examples: `UserRole`, `RolePermission`, `Article`, `MediaAsset`

- `ResourceId`
  - audited resource identifier
  - can be a composite string where needed
  - example: `{userId}:{roleId}`

- `ActorUserId?`
  - nullable for system actions
  - identifies the user or system actor that initiated the action

- `Outcome`
  - action outcome from the producer event when available
  - examples: `Success`, `Failure`

- `Summary`
  - short safe human-readable description
  - must not contain secrets or sensitive raw payload data

- `Data`
  - sanitized JSON/map containing a safe subset of the event payload
  - must be minimal and redacted

- `CorrelationId?`
  - used for tracing across API, outbox, worker, RabbitMQ consumer, and audit ingestion

- `OccurredAt`
  - UTC time when the producer says the event occurred

- `IngestedAt`
  - UTC time when Audit persisted the audit record

- `Hash?` / `PrevHash?`
  - V2 tamper-evident hook
  - not required for V1 correctness

---

## 3) Standard event ingestion contract

Audit consumes events through the standard outbox integration envelope.

Conceptual envelope fields:

- `MessageId`
- `EventType`
- `AggregateType`
- `AggregateId`
- `AggregatePublicId?`
- `AggregateVersion?`
- `Payload`
- `Headers?`
- `CorrelationId?`
- `InitiatorUserId?`
- `OccurredAtUtc`

Audit maps:

- `MessageId` → `AuditLog.EventId`
- `EventType` → `AuditLog.EventType`
- `EventType` or handler mapping → `AuditLog.Action`
- `AggregateType` → `AuditLog.ResourceType`
- `AggregateId` → `AuditLog.ResourceId`
- `InitiatorUserId` or safe payload actor field → `AuditLog.ActorUserId`
- `CorrelationId` → `AuditLog.CorrelationId`
- `OccurredAtUtc` → `AuditLog.OccurredAt`
- sanitized payload subset → `AuditLog.Data`

Naming rule:

- `MessageId` is the system-wide async message identity.
- `AuditLog.EventId` is the Audit-local storage name for that same identity.
- If practical in future migrations, Audit may rename `EventId` to `MessageId`, but V1 may keep `EventId` as a local alias.

---

## 4) Invariants

The following invariants must hold:

### 4.1 Append-only evidence

Audit records are append-only.

Once inserted, audit records must not be updated or deleted by normal business workflows.

Exceptions:
- explicit retention policy
- legal/compliance purge
- operator-controlled remediation with separate evidence trail

### 4.2 Message-level idempotency

The same upstream `MessageId` must not produce duplicate audit records.

Implementation rule:

- `AuditLog.EventId` must be unique.
- Duplicate `EventId` during ingestion is treated as already processed.

### 4.3 Replay safety

Replaying the same event with the same `MessageId` must be safe.

Expected behavior:

- existing `EventId` found
- no new audit row inserted
- consumer treats the message as successfully processed

### 4.4 Different MessageIds are not collapsed by default

Audit does not collapse different `MessageId` values by default.

Reason:

- separate events may represent separate attempts
- repeated governance attempts may be useful investigation evidence
- business-level dedupe must not hide legitimate audit trails

Business-level duplicate detection may be added later for noisy or derived audit views, but the raw audit evidence store should preserve distinct emitted events.

### 4.5 Privacy and redaction

`Data` and `Summary` must not include:

- passwords
- password hashes
- access tokens
- refresh tokens
- verification tokens
- reset tokens
- API keys
- session cookies
- sensitive PII beyond minimum necessary
- full article body content unless explicitly approved by audit policy

Audit stores a safe subset of the event payload, not raw unrestricted domain data.

### 4.6 Traceability

Audit should store:

- `EventId`
- `CorrelationId`
- `SourceModule`
- `EventType`
- `ResourceType`
- `ResourceId`
- `ActorUserId`
- `OccurredAt`
- `IngestedAt`

This allows cross-flow debugging and incident investigation.

### 4.7 Out-of-order arrival tolerance

Audit does not require ordered application to remain correct.

If older events arrive after newer events, Audit still appends them as historical evidence.

Investigation views may sort by:
- `OccurredAt` for domain chronology
- `IngestedAt` for ingestion/runtime chronology

---

## 5) Audit coverage policy — V1 baseline

At minimum, Audit should cover governance and security-sensitive actions.

### 5.1 Authorization

Authorization events consumed by Audit in V1:

- `authorization.user_role_assigned`
- `authorization.user_role_revoked`
- `authorization.role_permission_granted`
- `authorization.role_permission_revoked`

Recommended future Authorization events:

- `authorization.role_created`
- `authorization.role_updated`
- `authorization.role_deleted`
- `authorization.permission_created`
- `authorization.permission_updated`
- `authorization.permission_deleted`

### 5.2 Content

Content actions to audit when enabled:

- publish
- unpublish
- archive
- restore
- delete
- sensitive metadata changes

### 5.3 Identity

Identity actions to audit when enabled:

- password changed
- password reset completed
- email verified
- suspicious login/security events
- account disabled/enabled

Identity audit payloads must never include raw verification/reset tokens.

### 5.4 Media

Media actions to audit when enabled:

- attach
- detach
- reorder
- set primary
- delete
- restore

### 5.5 Policy changes

If V1 narrows or expands audit coverage, record the decision in the relevant module docs or ADR.

---

## 6) Authorization event mapping — V1 baseline

### 6.1 `authorization.user_role_assigned`

Maps to:

- `SourceModule`: `Authorization`
- `Action`: `UserRoleAssigned`
- `ResourceType`: `UserRole`
- `ResourceId`: `{userId}:{roleId}`
- `ActorUserId`: `assignedByUserId` or envelope `InitiatorUserId`
- `Data`: safe subset containing `userId`, `userPublicId?`, `roleId`, `roleName?`, `reason?`

### 6.2 `authorization.user_role_revoked`

Maps to:

- `SourceModule`: `Authorization`
- `Action`: `UserRoleRevoked`
- `ResourceType`: `UserRole`
- `ResourceId`: `{userId}:{roleId}`
- `ActorUserId`: `revokedByUserId` or envelope `InitiatorUserId`
- `Data`: safe subset containing `userId`, `userPublicId?`, `roleId`, `roleName?`, `reason?`

### 6.3 `authorization.role_permission_granted`

Maps to:

- `SourceModule`: `Authorization`
- `Action`: `RolePermissionGranted`
- `ResourceType`: `RolePermission`
- `ResourceId`: `{roleId}:{permissionId}`
- `ActorUserId`: `grantedByUserId` or envelope `InitiatorUserId`
- `Data`: safe subset containing `roleId`, `roleName?`, `permissionId`, `permissionKey?`, `reason?`

### 6.4 `authorization.role_permission_revoked`

Maps to:

- `SourceModule`: `Authorization`
- `Action`: `RolePermissionRevoked`
- `ResourceType`: `RolePermission`
- `ResourceId`: `{roleId}:{permissionId}`
- `ActorUserId`: `revokedByUserId` or envelope `InitiatorUserId`
- `Data`: safe subset containing `roleId`, `roleName?`, `permissionId`, `permissionKey?`, `reason?`

---

## 7) Consistency contract

Audit ingestion is eventually consistent with producer module truth.

A successful producer command means:

- producer truth committed
- producer outbox message committed when async work is required

It does not mean:

- RabbitMQ publication already completed
- Audit consumer already processed the event
- audit record is immediately queryable

Audit consumer lag must be observable, but it must not redefine producer module truth.

---

## 8) Failure and retry contract

Audit ingestion must be safe under:

- broker redelivery
- worker restart
- consumer crash after partial work
- timeout during Audit DB insert
- replay of retained messages

Rules:

- timeout is ambiguous
- retry must rely on `EventId` / `MessageId` dedupe
- duplicate insert is treated as already processed
- poison messages should be routed to consumer-side terminal handling or DLQ
- consumer-side failures must not be written back as producer-side outbox failures

---

## 9) Out of scope for V1

The following are out of scope for V1 unless explicitly added later:

- global total ordering of audit events
- tamper-evident hash chain enforcement
- exactly-once broker delivery
- synchronous audit writes inside producer module transactions
- cross-module transaction between Authorization and Audit
- automatic reconstruction of missing audit events without authoritative producer history