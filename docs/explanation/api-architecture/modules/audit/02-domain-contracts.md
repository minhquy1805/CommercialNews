
---

## `docs/explanation/api-architecture/modules/audit/02-domain-contracts.md`

```md id="p7e1kp"
# Audit — Domain Contracts (V1)

## 1) Ownership
Audit owns:
- audit log persistence
- audit policy (what actions are recorded, what fields are redacted)
- ingestion idempotency rules

Audit does not own:
- domain action execution (Content/AuthZ/Identity/Media/etc.)
- notification delivery

---

## 2) Entity: AuditLog (conceptual)
Fields (conceptual):
- `AuditId` (unique)
- `OccurredAt`
- `EventId` (for idempotency)
- `CorrelationId`
- `ActorUserId?` (nullable for system actions)
- `Action` (event type or normalized action name)
- `ResourceType`, `ResourceId`
- `Outcome` (optional: Success/Failure)
- `Summary` (short safe text)
- `Data` (JSON/map; redacted/minimal)
- `Hash?` / `PrevHash?` (V2 tamper-evident hook)

---

## 3) Invariants (must hold)
- Append-only intent: once inserted, records are not updated/deleted (except purge by explicit retention policy).
- Idempotency: the same `EventId` must not produce duplicate audit records.
- Privacy: `Data` must not include:
  - passwords
  - access/refresh tokens
  - verification/reset tokens
  - sensitive PII beyond minimum necessary
- Traceability: must store `CorrelationId` for cross-flow debugging.

---

## 4) Audit coverage policy (V1 baseline)
At minimum, audit these actions:
- Content: publish/unpublish/archive/restore/delete (if enabled)
- Authorization: assign/revoke roles, grant/revoke permissions, role/permission CRUD
- Identity: password changes/resets, email verified (optional), suspicious events (optional)
- Media: attach/detach/reorder/set-primary/delete/restore

Record the policy as ADR if you narrow/expand scope.

---

## 5) Ingestion contract (events → audit records)
Audit consumes domain events using the standard envelope:
- `EventId`, `OccurredAt`, `CorrelationId`, `ActorUserId?`, `EventType`, `Version`, `Payload`

Audit maps:
- `EventType` → `Action`
- payload identifiers → `ResourceType/ResourceId`
- safe subset of payload → `Data`