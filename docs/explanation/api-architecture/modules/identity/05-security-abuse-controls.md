# Identity — Security & Abuse Controls (V1)

## 1) Authentication baseline

- Access token: JWT, short TTL.
- Refresh token: opaque, long TTL, stored server-side as hash-backed truth.
- Verification/reset tokens: one-time token material, stored server-side by hash only.
- Public/self-service Identity flows must be rate-limited and anti-enumeration-aware.
- Identity Admin flows must be protected by explicit permission policies and contextual safety rules.

---

## 2) Token safety rules

### 2.1 General token rules

- Never log raw tokens.
- Never log token hashes.
- Never expose token hashes in API responses.
- Never send tokens via query string where avoidable.
- Reset/verify tokens must be time-bound and single-use by policy.
- Refresh tokens must be opaque and revocable from Identity truth.
- Token-bearing data must be disclosed only at the minimum level required for the user-facing flow.

### 2.2 Verification/reset delivery-token rules

Identity stores only token hashes for verification and reset flows.

Approved delivery-trigger events may carry raw one-time tokens when required by Notifications:

- `identity.verification_email_requested` may carry `RawVerificationToken`.
- `identity.password_reset_requested` may carry `RawResetToken`.

These raw token fields are secret-bearing delivery material.

They must not appear in:

- lifecycle/audit-oriented events
- Identity Admin events
- Audit payloads
- logs
- metrics
- traces
- exception details
- error diagnostics
- support/admin dashboards
- operational message viewers

### 2.3 Delivery-trigger event routing

Delivery-trigger events that contain raw one-time tokens must be routed only to approved consumers such as Notifications.

Audit consumers must not persist raw delivery-token fields.

Notifications must avoid persisting raw token material unless explicitly required and protected by policy.

If raw token material is persisted in delivery workflow state, it must be:

- encrypted or otherwise protected according to secret-handling policy
- redacted from logs and diagnostics
- retained only for the minimum required duration
- inaccessible from general admin/support views

---

## 3) Public/self-service abuse prevention

Rate limit at minimum:

- `POST /api/v1/auth/login`
- `POST /api/v1/auth/refresh`
- `POST /api/v1/auth/register`
- `POST /api/v1/auth/resend-verification`
- `POST /api/v1/auth/forgot-password`
- `POST /api/v1/auth/reset-password`
- `POST /api/v1/auth/verify-email`

Recommended rate-limit dimensions:

- IP address
- normalized email where safe
- user id where authenticated
- device/session hints where available
- operation type

Mitigations:

- progressive delays *(optional)*
- lockout policy *(optional / schema-dependent)*
- refresh-token reuse detection
- idempotency support for retry-prone flows:
  - `POST /api/v1/auth/register`
  - `POST /api/v1/auth/resend-verification`
  - `POST /api/v1/auth/forgot-password`

Rules:

- rate-limit responses must not leak account existence.
- public flows must not depend on Notifications delivery success.
- repeated client retries must not create unsafe duplicate outward effects.

---

## 4) Identity Admin authorization controls

Identity Admin endpoints are privileged security operations.

Every `/api/v1/admin/identity/*` endpoint must require:

- Bearer authentication
- explicit permission policy
- deny-by-default behavior
- correlation-aware logging
- sanitized audit-oriented event emission for mutations

Required permission policies:

- `Permission:Identity.User.Read`
- `Permission:Identity.Session.Read`
- `Permission:Identity.User.ManageStatus`
- `Permission:Identity.User.ManageSecurity`
- `Permission:Identity.User.VerifyEmail`
- `Permission:Identity.User.RevokeSessions`

Optional / recommended if finer-grained security reads are introduced:

- `Permission:Identity.User.ReadSecurity`

Generic `[Authorize]` is not sufficient for Identity Admin endpoints.

---

## 5) Identity Admin ABAC and protected-action controls

High-risk Identity Admin operations require contextual safety checks in addition to permission checks.

### Required contextual controls

- actor cannot deactivate their own account through normal admin APIs.
- actor cannot lock their own account through normal admin APIs.
- actor cannot revoke their own current session through normal admin APIs unless explicitly allowed by policy.
- protected system accounts cannot be deactivated through normal admin APIs.
- protected system accounts cannot be locked through normal admin APIs.
- protected account restrictions may apply to admin email verification override according to policy.

### High-risk operations

- `POST /api/v1/admin/identity/users/{userId}:deactivate`
- `POST /api/v1/admin/identity/users/{userId}:lock`
- `POST /api/v1/admin/identity/users/{userId}:mark-email-verified`
- `POST /api/v1/admin/identity/users/{userId}:revoke-sessions`

### Future controls

Future versions may require:

- MFA for selected admin security operations
- tenant/department constraints
- step-up authorization
- approval workflow for protected account changes
- stronger “last admin” protection coordinated with Authorization

---

## 6) Admin abuse prevention and throttling

Admin endpoints are authenticated but still abuse-sensitive.

Recommended controls:

- actor-based throttling for high-risk admin operations
- anomaly detection on repeated admin mutations
- correlation-id required or generated for every admin action
- reason field required for high-risk admin commands
- strict validation of target user id and operation scope
- alerting for protected-account mutation attempts
- alerting for repeated denied ABAC decisions
- alerting for unusual session revocation bursts

Admin rate limiting should be based primarily on:

- actor user id
- permission/action type
- target user id
- IP/UserAgent context
- time window

Admin abuse controls must not replace permission policy enforcement.

---

## 7) Anti-enumeration and duplicate-safety

The following endpoints must remain anti-enumeration-safe:

- `POST /api/v1/auth/forgot-password`
- `POST /api/v1/auth/resend-verification`

Rules:

- responses must not reveal whether an account exists.
- logs and telemetry must avoid unsafe account-existence disclosure.
- duplicate client submissions must converge safely.
- a valid resend/reset intent must not be blocked by stale delivery artifacts from downstream systems.
- same-intent replay must be protected by idempotency keys, business dedupe keys, or truth-backed reconciliation.

---

## 8) Safe logging, redaction, and audit payloads

### 8.1 Logging and diagnostics

Do not log:

- passwords
- password hashes
- raw refresh tokens
- refresh token hashes
- raw verification tokens
- verification token hashes
- raw reset tokens
- reset token hashes
- raw delivery-trigger event payloads
- sensitive provider responses

Avoid logging raw email in high-cardinality logs where not needed. Masking or hashing is preferred.

### 8.2 Audit payloads

Audit events should include only sanitized investigation metadata, such as:

- actor user id
- target user id
- action
- resource type
- resource id
- previous/new safe state
- reason
- correlation id
- timestamp

Audit events must not include:

- raw tokens
- token hashes
- passwords
- password hashes
- full delivery-trigger payloads containing raw one-time token material

### 8.3 Notification provider details

Provider/downstream notification details must not leak secrets or weaken security posture.

Ambiguous provider outcomes must be handled by delivery state, idempotency, and retry policy.

---

## 9) Security truth vs downstream lag

Identity security truth is authoritative.

The following must not weaken or redefine Identity truth:

- notification delay
- notification provider failure
- audit ingestion delay
- RabbitMQ publish delay after outbox commit
- cache invalidation delay
- derived projection lag
- cleanup/reporting lag

Security-sensitive decisions must use Identity truth, including:

- login account-state checks
- refresh-token validity checks
- email verification state checks
- reset-token validity checks
- admin account status changes
- admin session revocation
- lock/unlock state where implemented

Timeout ambiguity must be reconciled from Identity truth, not from delivery visibility or client belief.

---

## 10) Outbox, consumer, and idempotency controls

Identity mutations that require downstream side effects must commit:

- Identity truth
- required `OutboxMessage`

in the same transaction.

Consumer expectations:

- delivery is at-least-once
- consumers must dedupe by `MessageId`
- Audit must dedupe by canonical `MessageId`
- Notifications must use durable delivery tracking
- Notifications must use message-level and business-level idempotency where duplicate user-visible delivery is harmful
- cache/projection consumers must not let stale events overwrite fresher Identity truth

Rules:

- Identity API success must not depend on consumer completion.
- producer-side publish failure and consumer-side processing failure must be monitored separately.
- duplicate event delivery must not create duplicate audit evidence or harmful duplicate notifications.
- blind same-intent replay is not allowed without idempotency protection or truth reconciliation.

---

## 11) Security signals to monitor

### Public/self-service signals

- login failure spikes
- rate-limit trigger spikes
- register bursts
- resend-verification bursts
- forgot-password bursts
- refresh reuse detection events
- verify/reset token invalid spikes
- verify/reset token expired spikes
- verify/reset token already-used spikes
- unusual refresh/token-rotation failure patterns

### Admin security signals

- denied admin permission checks
- denied admin ABAC checks
- protected account mutation attempts
- self-action denial attempts
- admin deactivate/lock/revoke-session bursts
- admin mark-email-verified operations
- unusual actor/target operation patterns
- admin operations outside expected time/IP/UserAgent patterns

### Async/security pipeline signals

- outbox pending count for `identity.*`
- outbox oldest pending age for `identity.*`
- RabbitMQ publish failures for `identity.*`
- Notifications delivery lag for verification/reset flows
- Notifications provider timeout/failure rate
- Audit ingestion lag for `identity.admin.*`
- Audit dedupe hit count
- Notification business-dedupe reject count
- DLQ/dead-state count and age where enabled

---

## 12) Rules summary

- Identity truth is the source of security authority.
- Redis, Audit, Notifications, and projections are not Identity truth.
- Public flows must remain anti-enumeration-safe.
- Admin flows must be protected by explicit permission policies.
- High-risk admin actions require contextual safety checks.
- Raw verification/reset tokens may appear only in approved Notifications delivery-trigger events.
- Raw token material must never appear in Audit, lifecycle events, logs, metrics, traces, errors, or support/admin views.
- Identity Admin must not mutate Authorization role/permission truth.
- Identity mutations with required async side effects must atomically commit truth and outbox intent.
- Downstream failure after commit must not redefine Identity API success.
