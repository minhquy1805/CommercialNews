# Identity — Errors & Status Codes (V1)

## 1) Standard error envelope

See: `../../02-contracts-and-standards.md`

Identity errors must use the standard API error envelope.

Error responses should be deterministic, privacy-aware, and safe for security-sensitive flows.

---

## 2) Status code mapping

### Success

- `201 Created` — register success
- `200 OK` — successful command/query operations
- `204 No Content` — optional for selected no-body operations if explicitly documented

### Client errors

- `400 Bad Request`
  - validation errors
  - malformed request
  - invalid token input
  - expired token
  - already-used token
  - unsupported admin operation where the feature is not available in V1 schema

- `401 Unauthorized`
  - unauthenticated caller
  - missing access token
  - invalid access token
  - expired access token
  - invalid credentials on login

- `403 Forbidden`
  - authenticated caller denied by permission policy
  - authenticated caller denied by ABAC/context rules
  - account status gating prevents operation
  - admin self-action is blocked
  - protected system account mutation is blocked

- `404 Not Found`
  - admin target user not found
  - admin target session/resource not found where the endpoint is allowed to reveal absence

- `409 Conflict`
  - deterministic conflict cases
  - stale/concurrent mutation conflicts where optimistic concurrency is used
  - optional `EMAIL_EXISTS` behavior if chosen by policy for register
  - state transition conflict where convergence is not allowed by policy

- `429 Too Many Requests`
  - rate-limited public/self-service flow
  - actor-based throttling for admin/security operation where implemented

### Server/dependency errors

- `500 Internal Server Error`
  - unexpected failures
  - unhandled persistence/runtime errors

- `503 Service Unavailable`
  - dependency-level service failure where the Identity truth operation cannot safely complete
  - database unavailable
  - outbox write unavailable when outbox intent is required for the operation

---

## 3) Success semantics and async failure rules

A successful Identity write means:

- Identity truth committed successfully
- when async side effects are required, the corresponding `OutboxMessage` committed in the same transaction

A successful Identity write does **not** mean:

- RabbitMQ publish has completed
- Notifications has sent email
- Audit has ingested the event
- Redis/cache invalidation has completed
- derived projections or reports are caught up

### Downstream failure rule

Downstream failures must not be surfaced as Identity API failures after Identity truth and outbox intent have committed.

Examples:

- verification email provider failure must not fail `POST /api/v1/auth/register`
- reset email provider failure must not fail `POST /api/v1/auth/forgot-password`
- Audit ingestion delay must not fail an Identity Admin mutation
- cache invalidation delay must not fail account status/session security truth

### Outbox write failure rule

If an operation requires async side effects and Identity cannot commit the required `OutboxMessage` together with the truth change, the operation must fail before returning success.

Reason:

- truth change and async intent must be atomic
- returning success after truth commit without required outbox intent would violate V1 delivery semantics

---

## 4) Canonical error codes

### 4.1 Common errors

- `IDENTITY.VALIDATION_FAILED`
- `IDENTITY.UNAUTHENTICATED`
- `IDENTITY.POLICY_DENIED`
- `IDENTITY.FORBIDDEN`
- `IDENTITY.NOT_FOUND`
- `IDENTITY.CONFLICT`
- `IDENTITY.RATE_LIMITED`
- `IDENTITY.UNEXPECTED_ERROR`
- `IDENTITY.SERVICE_UNAVAILABLE`

### 4.2 Authentication and account state errors

- `IDENTITY.INVALID_CREDENTIALS`
- `IDENTITY.ACCOUNT_INACTIVE`
- `IDENTITY.ACCOUNT_LOCKED` *(if lock state is implemented)*
- `IDENTITY.ACCOUNT_NOT_VERIFIED` *(if verification gating is enforced)*
- `IDENTITY.ACCOUNT_DISABLED`

### 4.3 Token errors

- `IDENTITY.TOKEN_INVALID`
- `IDENTITY.TOKEN_EXPIRED`
- `IDENTITY.TOKEN_ALREADY_USED`
- `IDENTITY.TOKEN_REVOKED`
- `IDENTITY.REFRESH_TOKEN_INVALID`
- `IDENTITY.REFRESH_TOKEN_EXPIRED`
- `IDENTITY.REFRESH_TOKEN_REVOKED`
- `IDENTITY.REFRESH_REUSE_DETECTED`

### 4.4 Password errors

- `IDENTITY.PASSWORD_POLICY_VIOLATION`
- `IDENTITY.CURRENT_PASSWORD_INVALID`
- `IDENTITY.PASSWORD_RESET_TOKEN_INVALID`
- `IDENTITY.PASSWORD_RESET_TOKEN_EXPIRED`
- `IDENTITY.PASSWORD_RESET_TOKEN_ALREADY_USED`

### 4.5 Email verification errors

- `IDENTITY.EMAIL_VERIFICATION_TOKEN_INVALID`
- `IDENTITY.EMAIL_VERIFICATION_TOKEN_EXPIRED`
- `IDENTITY.EMAIL_VERIFICATION_TOKEN_ALREADY_USED`
- `IDENTITY.EMAIL_ALREADY_VERIFIED`
- `IDENTITY.EMAIL_EXISTS` *(only if chosen by policy for that surface)*

### 4.6 Admin errors

- `IDENTITY.ADMIN.USER_NOT_FOUND`
- `IDENTITY.ADMIN.SESSION_NOT_FOUND`
- `IDENTITY.ADMIN.PERMISSION_DENIED`
- `IDENTITY.ADMIN.ABAC_DENIED`
- `IDENTITY.ADMIN.SELF_ACTION_DENIED`
- `IDENTITY.ADMIN.PROTECTED_ACCOUNT`
- `IDENTITY.ADMIN.OPERATION_NOT_ALLOWED`
- `IDENTITY.ADMIN.LOCK_NOT_SUPPORTED`
- `IDENTITY.ADMIN.USER_ALREADY_ACTIVE`
- `IDENTITY.ADMIN.USER_ALREADY_INACTIVE`
- `IDENTITY.ADMIN.USER_ALREADY_LOCKED`
- `IDENTITY.ADMIN.USER_ALREADY_UNLOCKED`
- `IDENTITY.ADMIN.EMAIL_ALREADY_VERIFIED`
- `IDENTITY.ADMIN.NO_ACTIVE_SESSIONS`
- `IDENTITY.ADMIN.SESSION_REVOCATION_FAILED`

### 4.7 Outbox / async-intent errors

These errors apply only before the Identity transaction has successfully committed.

- `IDENTITY.OUTBOX_WRITE_FAILED`
- `IDENTITY.ASYNC_INTENT_REQUIRED`
- `IDENTITY.EVENT_PAYLOAD_INVALID`

After commit, producer-side publish failures and consumer-side processing failures are handled by outbox/worker/consumer retry and observability, not by changing the already-returned Identity API result.

---

## 5) Public / self-service endpoint error behavior

### `POST /api/v1/auth/register`

Possible outcomes:

- `201 Created`
- `400 IDENTITY.VALIDATION_FAILED`
- `400 IDENTITY.PASSWORD_POLICY_VIOLATION`
- `409 IDENTITY.EMAIL_EXISTS` *(only if explicit conflict policy is chosen)*
- `429 IDENTITY.RATE_LIMITED`
- `500/503` for unexpected truth/outbox failure

Rules:

- must not block on verification email delivery
- if outbox intent is required and cannot be committed, registration must not return success
- email delivery failure after commit is downstream and must not change registration success

---

### `POST /api/v1/auth/login`

Possible outcomes:

- `200 OK`
- `400 IDENTITY.VALIDATION_FAILED`
- `401 IDENTITY.INVALID_CREDENTIALS`
- `403 IDENTITY.ACCOUNT_INACTIVE`
- `403 IDENTITY.ACCOUNT_LOCKED`
- `403 IDENTITY.ACCOUNT_NOT_VERIFIED` *(if verification gating is enforced)*
- `429 IDENTITY.RATE_LIMITED`

Rules:

- invalid email/password should use deterministic authentication failure behavior
- must not leak sensitive account existence details beyond documented login semantics
- refresh token is secret-bearing and must not be logged

---

### `POST /api/v1/auth/refresh`

Possible outcomes:

- `200 OK`
- `400 IDENTITY.VALIDATION_FAILED`
- `401 IDENTITY.REFRESH_TOKEN_INVALID`
- `401 IDENTITY.REFRESH_TOKEN_EXPIRED`
- `401 IDENTITY.REFRESH_TOKEN_REVOKED`
- `403 IDENTITY.REFRESH_REUSE_DETECTED`
- `403 IDENTITY.ACCOUNT_INACTIVE`
- `403 IDENTITY.ACCOUNT_LOCKED`
- `429 IDENTITY.RATE_LIMITED`

Rules:

- refresh-token rotation is truth-first
- old token must not remain valid after successful rotation
- timeout ambiguity must be reconciled from refresh-token truth
- duplicate/stale refresh attempts must not mint multiple valid successor tokens incorrectly

---

### `POST /api/v1/auth/logout`

Possible outcomes:

- `200 OK`
- `400 IDENTITY.VALIDATION_FAILED`
- `401 IDENTITY.UNAUTHENTICATED`
- `401 IDENTITY.REFRESH_TOKEN_INVALID`
- `401 IDENTITY.REFRESH_TOKEN_REVOKED`

Rules:

- logout success is based on refresh-token revocation truth
- downstream audit visibility is not part of logout success

---

### `POST /api/v1/auth/verify-email`

Possible outcomes:

- `200 OK`
- `400 IDENTITY.EMAIL_VERIFICATION_TOKEN_INVALID`
- `400 IDENTITY.EMAIL_VERIFICATION_TOKEN_EXPIRED`
- `400 IDENTITY.EMAIL_VERIFICATION_TOKEN_ALREADY_USED`
- `200 OK` or `400` for already-verified state depending on documented token policy

Rules:

- token outcome must be deterministic
- successful verification must support read-your-writes
- email delivery state does not define verification truth

---

### `POST /api/v1/auth/resend-verification`

Possible outcomes:

- `200 OK`
- `400 IDENTITY.VALIDATION_FAILED`
- `429 IDENTITY.RATE_LIMITED`
- `500/503` for unexpected truth/outbox failure

Rules:

- always returns `200 { "accepted": true }` where anti-enumeration policy applies
- must not reveal account existence
- email sending is asynchronous
- raw verification token may appear only in approved Notifications delivery-trigger event payload and must be redacted from logs/diagnostics

---

### `POST /api/v1/auth/forgot-password`

Possible outcomes:

- `200 OK`
- `400 IDENTITY.VALIDATION_FAILED`
- `429 IDENTITY.RATE_LIMITED`
- `500/503` for unexpected truth/outbox failure

Rules:

- always returns `200 { "accepted": true }`
- must not reveal account existence
- email sending is asynchronous
- raw reset token may appear only in approved Notifications delivery-trigger event payload and must be redacted from logs/diagnostics

---

### `POST /api/v1/auth/reset-password`

Possible outcomes:

- `200 OK`
- `400 IDENTITY.PASSWORD_RESET_TOKEN_INVALID`
- `400 IDENTITY.PASSWORD_RESET_TOKEN_EXPIRED`
- `400 IDENTITY.PASSWORD_RESET_TOKEN_ALREADY_USED`
- `400 IDENTITY.PASSWORD_POLICY_VIOLATION`

Rules:

- token must be time-bound and single-use
- password/security truth must update atomically
- refresh tokens should be revoked according to policy
- downstream audit/notification lag must not weaken reset security effect

---

### `GET /api/v1/auth/me`

Possible outcomes:

- `200 OK`
- `401 IDENTITY.UNAUTHENTICATED`
- `403 IDENTITY.ACCOUNT_INACTIVE`
- `403 IDENTITY.ACCOUNT_LOCKED`

Rules:

- must reflect security-sensitive truth after verify/reset/change-password/revocation-sensitive changes
- must not depend on stale cache when security truth would be misrepresented

---

### `PUT /api/v1/auth/me`

Possible outcomes:

- `200 OK`
- `400 IDENTITY.VALIDATION_FAILED`
- `401 IDENTITY.UNAUTHENTICATED`
- `403 IDENTITY.POLICY_DENIED`
- `409 IDENTITY.CONFLICT` where stale-write protection is implemented

Rules:

- must not allow self-service updates to role, status, verification, or security truth
- stale-write protection behavior must be deterministic where implemented

---

### `POST /api/v1/auth/change-password`

Possible outcomes:

- `200 OK`
- `400 IDENTITY.VALIDATION_FAILED`
- `400 IDENTITY.CURRENT_PASSWORD_INVALID`
- `400 IDENTITY.PASSWORD_POLICY_VIOLATION`
- `401 IDENTITY.UNAUTHENTICATED`
- `403 IDENTITY.ACCOUNT_INACTIVE`
- `403 IDENTITY.ACCOUNT_LOCKED`

Rules:

- successful password change must update credential truth immediately
- refresh token revocation policy must be deterministic
- downstream notification/audit lag must not weaken password-change truth

---

## 6) Identity Admin endpoint error behavior

All Identity Admin endpoints require:

- Bearer access token
- explicit permission policy
- deny-by-default authorization
- sanitized errors
- audit-oriented outbox event for mutations where truth changes

### `GET /api/v1/admin/identity/users`

Possible outcomes:

- `200 OK`
- `401 IDENTITY.UNAUTHENTICATED`
- `403 IDENTITY.ADMIN.PERMISSION_DENIED`
- `400 IDENTITY.VALIDATION_FAILED`

Rules:

- invalid filter/sort fields should return deterministic validation errors
- response must not expose credentials, raw tokens, or token hashes

---

### `GET /api/v1/admin/identity/users/{userId}`

Possible outcomes:

- `200 OK`
- `401 IDENTITY.UNAUTHENTICATED`
- `403 IDENTITY.ADMIN.PERMISSION_DENIED`
- `404 IDENTITY.ADMIN.USER_NOT_FOUND`

Rules:

- returns Identity-owned account/security state only
- roles/permissions are not Identity truth

---

### `GET /api/v1/admin/identity/users/{userId}/sessions`

Possible outcomes:

- `200 OK`
- `401 IDENTITY.UNAUTHENTICATED`
- `403 IDENTITY.ADMIN.PERMISSION_DENIED`
- `404 IDENTITY.ADMIN.USER_NOT_FOUND`

Rules:

- must not expose raw refresh tokens or token hashes
- session identifiers must be public-safe identifiers

---

### `GET /api/v1/admin/identity/users/{userId}/security-summary`

Possible outcomes:

- `200 OK`
- `401 IDENTITY.UNAUTHENTICATED`
- `403 IDENTITY.ADMIN.PERMISSION_DENIED`
- `404 IDENTITY.ADMIN.USER_NOT_FOUND`

Rules:

- may return partial V1 summary if optional data such as login history is not implemented
- must not expose secrets or token material

---

### `POST /api/v1/admin/identity/users/{userId}:activate`

Possible outcomes:

- `200 OK`
- `401 IDENTITY.UNAUTHENTICATED`
- `403 IDENTITY.ADMIN.PERMISSION_DENIED`
- `403 IDENTITY.ADMIN.ABAC_DENIED`
- `403 IDENTITY.ADMIN.PROTECTED_ACCOUNT`
- `404 IDENTITY.ADMIN.USER_NOT_FOUND`
- `200 OK` for already-active convergent result
- `500/503` if Identity truth or required outbox intent cannot be committed

Rules:

- activating an already active user should converge safely
- should not emit duplicate state-change event for no-op convergence unless audit policy explicitly requires attempt logging
- success means Identity truth and required outbox intent committed

---

### `POST /api/v1/admin/identity/users/{userId}:deactivate`

Possible outcomes:

- `200 OK`
- `401 IDENTITY.UNAUTHENTICATED`
- `403 IDENTITY.ADMIN.PERMISSION_DENIED`
- `403 IDENTITY.ADMIN.ABAC_DENIED`
- `403 IDENTITY.ADMIN.SELF_ACTION_DENIED`
- `403 IDENTITY.ADMIN.PROTECTED_ACCOUNT`
- `404 IDENTITY.ADMIN.USER_NOT_FOUND`
- `200 OK` for already-inactive convergent result
- `500/503` if Identity truth or required outbox intent cannot be committed

Rules:

- admin cannot deactivate themselves through normal admin APIs
- protected system accounts cannot be deactivated through normal admin APIs
- deactivation may revoke refresh sessions according to policy/request
- deactivated users must not login or refresh from Identity truth
- success does not imply Audit/Notifications/cache completion

---

### `POST /api/v1/admin/identity/users/{userId}:lock`

Possible outcomes:

- `200 OK`
- `400 IDENTITY.ADMIN.LOCK_NOT_SUPPORTED`
- `401 IDENTITY.UNAUTHENTICATED`
- `403 IDENTITY.ADMIN.PERMISSION_DENIED`
- `403 IDENTITY.ADMIN.ABAC_DENIED`
- `403 IDENTITY.ADMIN.SELF_ACTION_DENIED`
- `403 IDENTITY.ADMIN.PROTECTED_ACCOUNT`
- `404 IDENTITY.ADMIN.USER_NOT_FOUND`
- `200 OK` for already-locked convergent result
- `500/503` if Identity truth or required outbox intent cannot be committed

Rules:

- this endpoint is available only where Identity schema supports lock state
- admin cannot lock themselves through normal admin APIs
- protected system accounts cannot be locked through normal admin APIs
- locked users cannot login according to account state policy
- lock state is Identity truth

---

### `POST /api/v1/admin/identity/users/{userId}:unlock`

Possible outcomes:

- `200 OK`
- `400 IDENTITY.ADMIN.LOCK_NOT_SUPPORTED`
- `401 IDENTITY.UNAUTHENTICATED`
- `403 IDENTITY.ADMIN.PERMISSION_DENIED`
- `403 IDENTITY.ADMIN.ABAC_DENIED`
- `404 IDENTITY.ADMIN.USER_NOT_FOUND`
- `200 OK` for already-unlocked convergent result
- `500/503` if Identity truth or required outbox intent cannot be committed

Rules:

- unlocking does not verify email
- unlocking does not assign roles or permissions
- success does not imply Audit/cache/projection completion

---

### `POST /api/v1/admin/identity/users/{userId}:mark-email-verified`

Possible outcomes:

- `200 OK`
- `401 IDENTITY.UNAUTHENTICATED`
- `403 IDENTITY.ADMIN.PERMISSION_DENIED`
- `403 IDENTITY.ADMIN.ABAC_DENIED`
- `403 IDENTITY.ADMIN.PROTECTED_ACCOUNT` where policy requires
- `404 IDENTITY.ADMIN.USER_NOT_FOUND`
- `200 OK` for already-verified convergent result
- `500/503` if Identity truth or required outbox intent cannot be committed

Rules:

- this is a high-risk admin override
- verification token hashes and raw tokens must not be returned or emitted in this admin event
- admin verification event is distinct from user-driven `identity.email_verified`
- success does not imply Audit/Notifications completion

---

### `POST /api/v1/admin/identity/users/{userId}:revoke-sessions`

Possible outcomes:

- `200 OK`
- `401 IDENTITY.UNAUTHENTICATED`
- `403 IDENTITY.ADMIN.PERMISSION_DENIED`
- `403 IDENTITY.ADMIN.ABAC_DENIED`
- `403 IDENTITY.ADMIN.SELF_ACTION_DENIED` where policy blocks self-current-session revocation
- `404 IDENTITY.ADMIN.USER_NOT_FOUND`
- `200 OK` with `revokedSessionCount = 0` if no active sessions remain
- `500/503` if Identity truth or required outbox intent cannot be committed

Rules:

- revoked refresh tokens must stop being valid immediately from Identity truth
- command should converge safely if no active sessions remain
- success does not imply Audit/cache/projection completion

---

## 7) Anti-enumeration rules

The following endpoints must not reveal account existence:

- `POST /api/v1/auth/forgot-password` → always `200 { "accepted": true }`
- `POST /api/v1/auth/resend-verification` → always `200 { "accepted": true }`

Anti-enumeration applies to response shape and user-facing error semantics.

Logs and telemetry must also avoid leaking account existence in unsafe ways.

---

## 8) Email already exists on register

Two acceptable approaches:

### A) Privacy-first

Return a safe response that does not confirm account existence.

### B) Explicit conflict

Return:

- `409 Conflict`
- `IDENTITY.EMAIL_EXISTS`

Whichever approach is chosen must be recorded and implemented consistently across:

- controller behavior
- service logic
- logs/telemetry
- documentation
- tests

---

## 9) Convergent admin command behavior

State-setting admin commands are convergent by default.

Examples:

- activating an already active user returns the current committed state
- deactivating an already inactive user returns the current committed state
- locking an already locked user returns the current committed state
- unlocking an already unlocked user returns the current committed state
- marking an already verified email returns the current committed state
- revoking sessions when no active sessions remain returns a successful committed state with zero newly revoked sessions

Convergent no-op commands should not emit duplicate state-change events unless audit policy explicitly requires attempt logging.

---

## 10) Timeout and retry behavior

Timeout is ambiguous.

A timeout does **not** prove that:

- the Identity command failed
- no truth state changed
- no outbox message was committed
- no email delivery-trigger event was created

### Caller reconciliation

After client-side timeout, callers should reconcile from Identity truth.

Examples:

- after admin deactivate timeout, read `GET /api/v1/admin/identity/users/{userId}`
- after admin revoke-sessions timeout, read `GET /api/v1/admin/identity/users/{userId}/sessions`
- after verify/reset ambiguity, use truth-backed user state or deterministic token outcome

### Retry rules

Retries are safe only when protected by:

- idempotent/convergent command semantics
- durable idempotency records
- message-level dedupe by `MessageId`
- business-level idempotency for user-visible delivery effects
- truth-backed reconciliation

Blind retry of external side effects is not allowed.

---

## 11) Rules summary

- `401` is for unauthenticated/invalid-auth situations.
- `403` is for authenticated callers denied by permission, ABAC, account gating, or protected-resource rules.
- `404` may be used for admin target resources where absence can be safely revealed to authorized callers.
- `409` is reserved for deterministic conflicts where convergence is not the chosen behavior.
- Token failures should be deterministic where possible:
  - invalid
  - expired
  - already used
  - revoked
- Rate-limited endpoints must return stable rate-limit behavior.
- Identity error results must be based on Identity truth, not downstream notification or audit completion.
- Identity Admin mutations must not return success if required Identity truth or required outbox intent fails to commit.
- Downstream Audit/Notifications/cache failure after commit must not alter Identity API success.