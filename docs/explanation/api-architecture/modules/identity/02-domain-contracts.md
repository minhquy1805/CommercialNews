# Identity — Domain Contracts (V1)

This document defines the **domain contracts** for Identity: entities, states, invariants, and emitted events.
It is the source of truth for how Identity behaves regardless of implementation details.

Related:
- System-wide auth rules: `../../08-authentication-and-authorization.md`
- Runtime scenarios: `../../../architecture/arc42/04-runtime-view-v1.md` (Scenario 4–5)
- Standards: `../../02-contracts-and-standards.md` (errors, conventions)

---

## 1) Domain scope (what Identity owns)

Identity owns:
- account lifecycle (unverified → verified/active; optional locked/disabled)
- credential lifecycle (password hashing, password change/reset)
- session lifecycle (refresh tokens, logout)
- verification/reset token policy (time-bound, single-use)

Identity does **not** own:
- roles/permissions logic (Authorization module owns that)
- audit persistence (Audit module ingests events)
- email delivery (Notifications module sends emails)

---

## 2) Entities (conceptual model)

### 2.1 UserAccount
Core attributes (conceptual):
- `UserId` (stable system-wide)
- `Email` (unique)
- `PasswordHash`
- `Status` (account state)
- `CreatedAt`, `UpdatedAt`
- `VerifiedAt?`

Optional operational fields (policy-driven):
- `LockoutUntil?`
- `FailedLoginCount?`
- `LastLoginAt?`

### 2.2 RefreshToken
- `TokenId`
- `UserId`
- `Token` (opaque string)
- `CreatedAt`
- `ExpiryDate`
- `RevokedAt?`
- `ReplacedByToken?`
- `IPAddress?`
- `UserAgent?`
- `FamilyId?` (optional, to support token-family revocation)

### 2.3 EmailToken (logical concept)
This can be implemented as stored tokens or signed tokens, but the **contract** is:
- Token has a `Type`:
  - `VerifyEmail`
  - `ResetPassword`
- Token is **time-bound**
- Token is **single-use** (consumed) by policy
- Token is never logged or emitted in events/templates beyond what is required

Fields (conceptual):
- `TokenId` (or token identifier)
- `UserId`
- `Type`
- `ExpiresAt`
- `ConsumedAt?`

---

## 3) States and lifecycle rules

### 3.1 Account states (V1 baseline)
- `Unverified`
- `Active`

Optional (V1/V2 hooks):
- `Locked` (temporary lockout)
- `Disabled` (admin action)

### 3.2 State invariants
- A user is **Active** only when email verification is completed (policy-level baseline).
- Public read access does not require authentication.
- Admin access **always** requires Active + authorization policies.

### 3.3 Verification gating (ADR hook)
Define what is allowed before verification:
- Allowed (baseline):
  - register, resend verification, verify email
  - forgot/reset password
- Policy-driven:
  - login before verification (allowed or denied)
  - interaction actions (like/comment) requiring verified email

> Record the final gating decision in an ADR and apply consistently.

---

## 4) Security invariants (non-negotiable)

### 4.1 Sensitive data handling
Never store or emit secrets unsafely:
- Passwords are never stored (only strong hashes)
- Access tokens, refresh tokens, verification tokens, reset tokens must never appear in logs
- Token values must not be included in domain events
- Templates must avoid leaking tokens/PII beyond minimum necessary

### 4.2 Credential rules
- Password hashing uses a strong algorithm (implementation choice).
- Password reset and change must trigger session revocation by policy (recommended: revoke all refresh tokens).

### 4.3 Session rules (baseline)
- Refresh tokens are opaque and stored server-side.
- Refresh token rotation is enabled (recommended default).

---

## 5) Idempotency and retry semantics (contract-level)

Identity must be safe under retries:
- `/forgot-password` and `/resend-verification` are **anti-enumeration** and return accepted.
- Idempotency keys are recommended for:
  - register, resend verification, forgot password
- Refresh rotation must tolerate retries without issuing inconsistent token state.

---

## 6) Domain events (for async side effects)

Identity emits events to support:
- Notifications (email sending)
- Audit ingestion
- (Optional) Login history

### 6.1 Event envelope
Events follow the system envelope in `arc42/03`:
- `EventId`, `OccurredAt`, `CorrelationId`, `ActorUserId?`
- `EventType`, `Version`, `Payload`

### 6.2 Event types (V1)
1) `UserRegistered`
Payload:
- `UserId`
- `Email` (consider redaction policy; use minimal necessary)
- `OccurredAt`

2) `VerificationEmailRequested` (optional alternative to UserRegistered)
Payload:
- `UserId`
- `Email` (minimal)
- `RequestId` (idempotency/dedupe key)

3) `UserEmailVerified`
Payload:
- `UserId`
- `VerifiedAt`

4) `PasswordResetRequested`
Payload:
- `UserId`
- `Email` (minimal)
- `ResetTokenId` (identifier only; do not emit raw token)
- `RequestedAt`

5) `UserPasswordChanged`
Payload:
- `UserId`
- `ChangedAt`
- `Reason` (e.g., `Reset` | `Change`)

6) `UserLoggedIn` (optional)
Payload:
- `UserId`
- `LoggedInAt`
- `IPAddress?`, `UserAgent?` (privacy policy applies)

**Rule:** event payloads must be minimal and privacy-aware.

---

## 7) ADR hooks (must be explicit)

- Verification gating rules (login before verify? interactions require verify?)
- Refresh token reuse detection response (revoke family vs revoke all)
- Logout semantics (`Current` vs `All`)
- Register conflict behavior (privacy-safe vs explicit `EMAIL_EXISTS`)
- Lockout policy (if added)