# Identity — Business Rules (V1)

This document defines the core business rules for Identity in V1.  
It focuses on concrete rules that application code and API behavior must implement consistently.

Related:

- `01-api-surface.md`
- `02-domain-contracts.md`
- `03-runtime-flows.md`
- `04-errors-status-codes.md`
- `05-security-abuse-controls.md`
- `06-idempotency-consistency.md`
- `08-dependencies-and-ownership.md`
- `09-open-questions.md`

---

## 1) Account rules

- V1 baseline account state model is:
  - `Unverified`
  - `Active`
  - `Inactive`
- Optional / conditional hooks may include:
  - `Locked`
  - `Disabled`
- A newly registered account starts in `Unverified` state.
- An account becomes `Active` only after successful email verification under baseline V1 policy.
- An account may become `Inactive` through an Identity Admin status operation.
- Locked state is conditional in V1 and requires schema support.
- Identity owns:
  - account verification state
  - password state
  - token state
  - session revocation truth
  - account status/security truth
- Authorization rules are owned by the Authorization module, not Identity.
- Identity Admin owns account/session/security truth only.
- Identity Admin must not assign roles, revoke roles, grant permissions, or revoke permissions.

---

## 2) Registration rules

- Registration requires a valid email and password.
- Email must be normalized before uniqueness checks.
- Password must satisfy the password policy.
- Registration success returns `201`.
- Registration must not block on email delivery.
- Registration must commit Identity truth and required outbox intent atomically.
- Successful registration does not imply verification email already delivered.
- Duplicate email behavior is policy-driven:
  - privacy-safe response preferred
  - explicit `409 IDENTITY.EMAIL_EXISTS` allowed if chosen by policy
- `Idempotency-Key` is recommended for safe retries.
- If client-side timeout occurs after registration request submission, the caller must reconcile from Identity truth or deterministic response policy, not from email delivery visibility.

---

## 3) Password rules

- Baseline V1 password policy:
  - minimum length: 12 characters
  - maximum supported length: at least 64 characters
  - printable characters allowed
- Stronger password policy may be layered by configuration or ADR without changing Identity ownership.
- Passwords must never be stored or logged in raw form.
- Passwords must be stored only as strong hashes.
- Password hashes must never be returned, logged, emitted, or copied into audit/event payloads.
- Password change and password reset should revoke refresh tokens.
- Recommended V1 policy: revoke all active refresh tokens after successful password change/reset.
- Successful password change/reset must update credential truth immediately.
- Downstream Audit/Notifications lag must not weaken password-change/reset truth.

---

## 4) Email verification rules

- Verification tokens must be time-bound and single-use.
- Successful user-driven verification must activate the account under baseline V1 policy.
- An already-used verification token must return a deterministic outcome.
- Expired verification tokens must fail with documented token errors.
- After successful verification, self-state reads must reflect the new verified state immediately.
- `/api/v1/auth/resend-verification` must always return `200 { "accepted": true }` where anti-enumeration policy applies.
- `/api/v1/auth/resend-verification` must not reveal whether the account exists.
- `/api/v1/auth/resend-verification` is rate-limited and email-async.
- A valid resend should create a new logical verification intent if policy allows.
- Stale delivery artifacts must not block a valid new verification intent.
- Identity stores only verification token hashes.
- `identity.verification_email_requested` may carry `RawVerificationToken` only because it is an approved Notifications delivery-trigger event.
- Lifecycle, admin, audit-oriented, logging, metrics, tracing, error, support, and reporting payloads must never contain raw verification tokens or verification token hashes.

---

## 5) Login rules

- Login requires valid email and password.
- Invalid credentials return documented authentication failure semantics.
- Login behavior for `Unverified`, `Inactive`, `Locked`, or `Disabled` accounts must follow policy.
- Recommended V1 baseline: deny login for `Unverified`.
- Inactive users must not sign in.
- Locked users must not sign in where lock state is implemented.
- Login success returns access token, refresh token, and current user summary.
- Login must not wait for downstream audit or notification side effects.
- `/api/v1/auth/login` is rate-limited.
- Login decisions must come from Identity truth, not stale cache or downstream projections.

---

## 6) Refresh token and session rules

- Access tokens are short-lived JWTs.
- Refresh tokens are opaque and stored server-side as hash-backed truth.
- Refresh token hashes must never be returned, logged, emitted, or copied into audit/event payloads.
- Refresh token rotation is the default V1 behavior.
- After successful refresh, the old refresh token must no longer remain valid.
- Refresh decisions must come from server-side Identity truth, not cache or client belief.
- Reuse of a revoked/rotated refresh token must trigger a deterministic security response.
- Final reuse response scope must be defined by policy:
  - revoke token family
  - or revoke all refresh tokens for the user
- `/api/v1/auth/refresh` is rate-limited.
- Timeout during refresh does not prove the refresh failed.
- Duplicate/stale refresh attempts must not mint multiple valid successor tokens incorrectly.

---

## 7) Logout rules

- Logout success is based on refresh-token revocation truth, not downstream audit visibility.
- `Current` logout revokes the current session or token family according to policy.
- `All` logout revokes all active refresh tokens for the user if supported.
- Logout semantics must be documented clearly and implemented consistently.
- Logout must not wait for Audit/Notifications/cache completion.
- Timeout during logout must be reconciled from Identity session truth where relevant.

---

## 8) Forgot/reset password rules

- `/api/v1/auth/forgot-password` must always return `200 { "accepted": true }`.
- `/api/v1/auth/forgot-password` must not reveal whether the email exists.
- Reset tokens must be time-bound and single-use.
- Successful password reset must:
  - consume the reset token
  - update the password hash
  - revoke refresh tokens according to policy
- A used or expired reset token must return a deterministic documented outcome.
- `/api/v1/auth/forgot-password` and `/api/v1/auth/reset-password` are rate-limited.
- Email delivery for forgot-password is asynchronous.
- A legitimate new reset intent is distinct from duplicate retry of the same prior request.
- Identity stores only reset token hashes.
- `identity.password_reset_requested` may carry `RawResetToken` only because it is an approved Notifications delivery-trigger event.
- Lifecycle, admin, audit-oriented, logging, metrics, tracing, error, support, and reporting payloads must never contain raw reset tokens or reset token hashes.
- Reset-token delivery failure must not roll back committed Identity truth/outbox intent.

---

## 9) Self-profile rules

- `GET /api/v1/auth/me` returns the authenticated user’s current self profile.
- `GET /api/v1/auth/me` must not serve stale security state after verify, reset, refresh, password change, or revocation-sensitive changes.
- `PUT /api/v1/auth/me` may update only non-sensitive self-service fields.
- Sensitive fields such as role, status, verification flags, and security state must be ignored or denied.
- Self-service profile operations must not become a backdoor for Authorization governance truth changes.

---

## 10) Identity Admin account rules

Identity Admin endpoints manage Identity-owned account/session/security truth only.

All Identity Admin endpoints must require:

- Bearer authentication
- explicit permission policy
- deny-by-default authorization
- sanitized request/response/event payloads
- correlation-aware logging
- audit-oriented outbox events for mutations where truth changes

Required permissions:

- `Identity.User.Read`
- `Identity.Session.Read`
- `Identity.User.ManageStatus`
- `Identity.User.ManageSecurity`
- `Identity.User.VerifyEmail`
- `Identity.User.RevokeSessions`

Optional / recommended:

- `Identity.User.ReadSecurity`

Generic authentication alone is not sufficient for Identity Admin endpoints.

---

## 11) Identity Admin status rules

### Activate user

- `POST /api/v1/admin/identity/users/{userId}:activate` requires `Permission:Identity.User.ManageStatus`.
- Activating a user updates Identity account status truth.
- Activating an already active user should return the current committed state.
- No-op convergence should not emit duplicate state-change events unless audit policy explicitly requires attempt logging.
- Successful activation must commit required admin outbox event where a state change occurs.

### Deactivate user

- `POST /api/v1/admin/identity/users/{userId}:deactivate` requires `Permission:Identity.User.ManageStatus`.
- Deactivation updates Identity account status truth.
- Deactivated users must not login or refresh from Identity truth.
- Deactivation may revoke active refresh tokens according to request/policy.
- Admin cannot deactivate their own account through normal admin APIs.
- Protected system accounts cannot be deactivated through normal admin APIs.
- Deactivating an already inactive user should return the current committed state.
- No-op convergence should not emit duplicate state-change events unless audit policy explicitly requires attempt logging.
- Successful deactivation means Identity truth and required outbox intent committed.
- Successful deactivation does not mean Audit/Notifications/cache/projections have completed.

---

## 12) Identity Admin email verification override rules

- `POST /api/v1/admin/identity/users/{userId}:mark-email-verified` requires `Permission:Identity.User.VerifyEmail`.
- Admin email verification override is high-risk.
- A reason should be required.
- The operation updates Identity verification truth:
  - `IsEmailVerified = true`
  - `EmailVerifiedAt = now`
- Active verification tokens may be revoked or made obsolete according to policy.
- The event `identity.admin.email_marked_verified` is distinct from user-driven `identity.email_verified`.
- The admin event must never contain raw verification tokens or verification token hashes.
- Marking an already verified email should return the current committed state.
- No-op convergence should not emit duplicate state-change events unless audit policy explicitly requires attempt logging.
- Audit must record the operation asynchronously and idempotently.
- Notification, if enabled, is a downstream side effect and must not define operation success.

---

## 13) Identity Admin session revocation rules

- `POST /api/v1/admin/identity/users/{userId}:revoke-sessions` requires `Permission:Identity.User.RevokeSessions`.
- Admin session revocation updates Identity refresh-token truth.
- Revoked refresh tokens must stop being valid immediately from Identity truth.
- The operation must not expose raw refresh tokens or refresh token hashes.
- The event `identity.admin.user_sessions_revoked` must not contain raw refresh tokens or token hashes.
- Admin cannot revoke their own current session through normal admin APIs unless explicitly allowed by policy.
- Revoking sessions when no active sessions remain should return a successful committed state with zero newly revoked sessions.
- Success does not imply Audit/cache/projection completion.
- Access-token invalidation behavior remains policy-defined unless a token-version/security-stamp mechanism is introduced.

---

## 14) Identity Admin lock/unlock rules

Lock/unlock is conditional in V1.

### Availability

- Lock/unlock endpoints are available only if Identity schema supports lock state.
- If schema does not support lock state, endpoints must remain unavailable or return deterministic unsupported-operation behavior.

### Lock user

- `POST /api/v1/admin/identity/users/{userId}:lock` requires `Permission:Identity.User.ManageSecurity`.
- Admin cannot lock their own account through normal admin APIs.
- Protected system accounts cannot be locked through normal admin APIs.
- Locked users must not login according to account-state policy.
- Lock may revoke refresh tokens according to request/policy.
- Locking an already locked user should return the current committed state.

### Unlock user

- `POST /api/v1/admin/identity/users/{userId}:unlock` requires `Permission:Identity.User.ManageSecurity`.
- Unlock clears Identity-owned lock state.
- Unlock does not verify email.
- Unlock does not assign roles or permissions.
- Unlocking an already unlocked user should return the current committed state.

### Events

- Lock emits `identity.admin.user_locked` where a state change occurs.
- Unlock emits `identity.admin.user_unlocked` where a state change occurs.
- Lock/unlock events must never contain raw tokens, token hashes, password hashes, or unnecessary PII.

---

## 15) Protected account and self-action rules

- Protected system accounts are policy-defined.
- Protected account policy may be based on configuration, system marker, or future governance policy.
- Normal admin APIs must block:
  - deactivating protected accounts
  - locking protected accounts
- Normal admin APIs should block dangerous self-actions:
  - self-deactivate
  - self-lock
  - self-current-session revocation through admin endpoint
- Break-glass behavior, if introduced, must be separately documented and strongly audited.
- Last-admin protection remains an ADR/open question because Authorization owns role/permission truth.

---

## 16) Security and abuse rules

- Tokens must never be logged.
- Token hashes must never be logged.
- Passwords must never be logged.
- Password hashes must never be logged.
- High-risk and abuse-prone endpoints must be rate-limited:
  - `/api/v1/auth/login`
  - `/api/v1/auth/refresh`
  - `/api/v1/auth/register`
  - `/api/v1/auth/resend-verification`
  - `/api/v1/auth/forgot-password`
  - `/api/v1/auth/reset-password`
  - `/api/v1/auth/verify-email`
- Admin high-risk operations should have actor-based throttling:
  - deactivate
  - lock
  - mark email verified
  - revoke sessions
- Avoid logging unnecessary raw PII in high-cardinality logs.
- Secret-bearing delivery-trigger event payloads must not be logged as raw JSON.
- Identity should monitor:
  - login failure spikes
  - rate-limit spikes
  - resend/forgot bursts
  - refresh reuse detection events
  - token invalid / expired / already-used spikes
  - verification/reset delivery lag for auth-critical flows
  - admin permission/ABAC denial spikes
  - protected account mutation attempts
  - admin mutation truth/outbox commit failures
  - Audit ingestion lag for `identity.admin.*`

---

## 17) Error and response rules

- `201` for register success.
- `200` for other successful operations.
- `400` for validation failures and invalid/expired/already-used token errors.
- `401` for unauthenticated or invalid authentication situations.
- `403` for policy-denied, ABAC-denied, account-gated, protected-resource, or blocked self-action cases.
- `404` may be used for admin target resources where absence can be safely revealed to authorized callers.
- `409` may be used for deterministic conflict cases where policy allows.
- `429` for rate-limited requests.
- Email delivery failure should not normally surface as 5xx if Identity truth and required outbox intent have already committed.
- Identity Admin mutations must not return success if required Identity truth or required admin outbox intent fails to commit.
- Downstream Audit/Notifications/cache/projection failure after commit must not alter Identity API success.

---

## 18) Consistency rules

- Identity truth is authoritative for security decisions.
- Email, audit, notifications, cache invalidation, and projections are downstream eventual side effects.
- Read-your-writes is required after security-sensitive changes.
- Stale cache or derived state must never weaken current security truth.
- Timeouts must be reconciled from Identity truth, not from side effects.
- Identity mutations requiring async side effects must commit Identity truth and required outbox intent atomically.
- Admin state-setting commands are convergent by default.
- Detailed idempotency, refresh rotation, replay, and truth-boundary rules are defined in `06-idempotency-consistency.md`.

---

## 19) ADR hooks

The following policy decisions must be recorded explicitly if not already finalized:

- duplicate email register behavior
- login allowed or denied before verification
- refresh reuse response scope
- exact logout semantics
- password policy strength beyond the baseline
- lockout policy
- token TTL values
- Identity Admin protected account policy
- Identity Admin self-action policy
- last-admin protection with Authorization
- admin email verification override policy
- admin session revocation semantics
- raw token handling in delivery-trigger events
- notification delivery dedupe policy
- admin command idempotency
- admin audit attempt policy
- access-token invalidation strategy