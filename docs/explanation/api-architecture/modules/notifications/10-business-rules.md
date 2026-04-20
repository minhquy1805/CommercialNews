# Identity — Business Rules (V1)

This document defines the core business rules for Identity in V1.  
It focuses on concrete rules that application code and API behavior must implement consistently.

Related:

- `02-domain-contracts.md`
- `01-api-surface.md`
- `04-errors-status-codes.md`
- `05-security-abuse-controls.md`
- `06-idempotency-consistency.md`

---

## 1) Account rules

- V1 baseline account state model is:
  - `Unverified`
  - `Active`
- Optional hooks may include:
  - `Locked`
  - `Disabled`
- A newly registered account starts in `Unverified` state.
- An account becomes `Active` only after successful email verification under baseline V1 policy.
- Identity owns account verification state, password state, token state, and session revocation truth.
- Authorization rules are owned by the Authorization module, not Identity.

---

## 2) Registration rules

- Registration requires a valid email and password.
- Email must be normalized before uniqueness checks.
- Password must satisfy the password policy.
- Registration success returns `201`.
- Registration must not block on email delivery.
- Duplicate email behavior is policy-driven:
  - privacy-safe response preferred
  - explicit `409 IDENTITY.EMAIL_EXISTS` allowed for internal/admin policy
- `Idempotency-Key` is recommended for safe retries.
- Successful registration does not imply verification email already delivered.

---

## 3) Password rules

- Baseline V1 password policy:
  - minimum length: 12 characters
  - maximum supported length: at least 64 characters
  - printable characters allowed
- Stronger password policy may be layered by configuration or ADR without changing Identity ownership.
- Passwords must never be stored or logged in raw form.
- Passwords must be stored only as strong hashes.
- Password change and password reset should revoke refresh tokens.
- Recommended V1 policy: revoke all active refresh tokens after successful password change/reset.

---

## 4) Email verification rules

- Verification tokens must be time-bound and single-use.
- Successful verification must activate the account.
- An already-used verification token must return a deterministic outcome.
- Expired verification tokens must fail with documented token errors.
- After successful verification, self-state reads must reflect the new verified state immediately.
- `/resend-verification` must always return `200 { "accepted": true }`.
- `/resend-verification` must not reveal whether the account exists.
- `/resend-verification` is rate-limited and email-async.
- A valid resend should create a new logical verification intent if policy allows.
- Stale delivery artifacts must not block a valid new verification intent.
- Raw verification tokens must never appear in async event payloads or outbox payloads.

---

## 5) Login rules

- Login requires valid email and password.
- Invalid credentials return documented authentication failure semantics.
- Login behavior for `Unverified`, `Locked`, or `Disabled` accounts must follow policy.
- Recommended V1 baseline: deny login for `Unverified`.
- Login success returns access token, refresh token, and current user summary.
- Login must not wait for downstream audit or notification side effects.
- `/login` is rate-limited.

---

## 6) Refresh token and session rules

- Access tokens are short-lived JWTs.
- Refresh tokens are opaque and stored server-side.
- Refresh token rotation is the default V1 behavior.
- After successful refresh, the old refresh token must no longer remain valid.
- Refresh decisions must come from server-side truth, not cache or client belief.
- Reuse of a revoked/rotated refresh token must trigger a deterministic security response.
- Final reuse response scope must be defined by policy:
  - revoke token family
  - or revoke all refresh tokens for the user
- `/refresh` is rate-limited.

---

## 7) Logout rules

- Logout success is based on revocation truth, not downstream audit visibility.
- `Current` logout revokes the current session or token family according to policy.
- `All` logout revokes all active refresh tokens for the user if supported.
- Logout semantics must be documented clearly and implemented consistently.

---

## 8) Forgot/reset password rules

- `/forgot-password` must always return `200 { "accepted": true }`.
- `/forgot-password` must not reveal whether the email exists.
- Reset tokens must be time-bound and single-use.
- Successful password reset must:
  - consume the reset token
  - update the password hash
  - revoke refresh tokens according to policy
- A used or expired reset token must return a deterministic documented outcome.
- `/forgot-password` and `/reset-password` are rate-limited.
- Email delivery for forgot-password is asynchronous.
- A legitimate new reset intent is distinct from duplicate retry of the same prior request.
- Raw reset tokens must never appear in async event payloads or outbox payloads.

---

## 9) Self-profile rules

- `GET /me` returns the authenticated user’s current self profile.
- `GET /me` must not serve stale security state after verify, reset, refresh, or password change.
- `PUT /me` may update only non-sensitive self-service fields.
- Sensitive fields such as role, status, and verification flags must be ignored or denied.

---

## 10) Security and abuse rules

- Tokens must never be logged.
- Tokens must never be sent via query string.
- Passwords must never be logged.
- High-risk and abuse-prone endpoints must be rate-limited:
  - `/login`
  - `/refresh`
  - `/register`
  - `/resend-verification`
  - `/forgot-password`
  - `/reset-password`
- Avoid logging unnecessary raw PII in high-cardinality logs.
- Identity should monitor:
  - login failure spikes
  - rate-limit spikes
  - resend/forgot bursts
  - refresh reuse detection events
  - token invalid / expired / already-used spikes
  - verification/reset delivery lag for auth-critical flows

---

## 11) Error and response rules

- `201` for register success.
- `200` for other successful operations.
- `400` for validation failures and invalid/expired/already-used token errors.
- `401` for invalid credentials and invalid/expired access token.
- `403` for policy-denied cases when applicable.
- `409` may be used for deterministic conflict cases where policy allows.
- `429` for rate-limited requests.
- Email delivery failure should not normally surface as 5xx if Identity truth has already committed.

---

## 12) Consistency rules

- Identity truth is authoritative for security decisions.
- Email, audit, and notifications are downstream eventual side effects.
- Read-your-writes is required after security-sensitive changes.
- Stale cache or derived state must never weaken current security truth.
- Timeouts must be reconciled from Identity truth, not from side effects.
- Detailed idempotency, refresh rotation, replay, and truth-boundary rules are defined in `06-idempotency-consistency.md`.

---

## 13) ADR hooks

The following policy decisions must be recorded explicitly if not already finalized:

- duplicate email register behavior
- login allowed or denied before verification
- refresh reuse response scope
- exact logout semantics
- password policy strength beyond the baseline
- lockout policy
- token TTL values