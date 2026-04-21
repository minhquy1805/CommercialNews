# Identity — Domain Contracts (V1)

This document defines the **domain contracts** for Identity: entities, states, invariants, and emitted events.  
It is the source of truth for how Identity behaves regardless of implementation details.

Related:

- System-wide auth rules: `../../08-authentication-and-authorization.md`
- Runtime scenarios: `../../../architecture/arc42/04-runtime-view-v1.md`
- Standards: `../../02-contracts-and-standards.md`
- ADR-0013: outbox and delivery semantics
- ADR-0020: timeout, retry, and failure detection policy
- ADR-0028: consumer idempotency, replay, and rebuild policy

---

## 1) Domain scope (what Identity owns)

Identity owns:

- account lifecycle
  - unverified → verified/active
  - optional locked/disabled extensions
- credential lifecycle
  - password hashing
  - password change
  - password reset initiation and completion
- session lifecycle
  - refresh tokens
  - logout
  - token rotation / revocation policy
- verification/reset token policy
  - issuance
  - expiry
  - single-use consumption
  - invalidation policy

Identity does **not** own:

- roles/permissions logic
- governance truth
- audit persistence
- email delivery execution
- notification delivery workflow state

### Boundary rule

Identity is a **truth-owning module**.

When email side effects are needed, Identity commits its own truth first and emits async intent via outbox/event publication.
Identity does not wait for Notifications to complete delivery in order to consider its own truth mutation successful.

---

## 2) Core entities (conceptual model)

### 2.1 UserAccount

Core attributes (conceptual):

- `UserId` *(stable system-wide identity)*
- `Email`
- `EmailNormalized`
- `PasswordHash`
- `Status`
- `CreatedAt`
- `UpdatedAt`
- `VerifiedAt?`

Optional operational fields (policy-driven):

- `LockoutUntil?`
- `FailedLoginCount?`
- `LastLoginAt?`

### 2.2 RefreshToken

Core attributes (conceptual):

- `TokenId`
- `UserId`
- `Token` *(opaque string or equivalent protected representation)*
- `CreatedAt`
- `ExpiryDate`
- `RevokedAt?`
- `ReplacedByToken?`
- `IPAddress?`
- `UserAgent?`
- `FamilyId?` *(optional; token-family revocation support)*

### 2.3 EmailToken (logical concept)

This may be implemented as stored tokens, hashed token records, or signed tokens, but the contract is:

- token has a `Type`
  - `VerifyEmail`
  - `ResetPassword`
- token is **time-bound**
- token is **single-use** by policy
- raw token value is never logged
- raw token value is never emitted in async event payloads

Conceptual fields:

- `TokenId` *(or equivalent token identifier)*
- `UserId`
- `Type`
- `ExpiresAt`
- `ConsumedAt?`

---

## 3) States and lifecycle rules

### 3.1 Account states (V1 baseline)

- `Unverified`
- `Active`

Optional V1/V2 hooks:

- `Locked`
- `Disabled`

### 3.2 State invariants

- a user becomes `Active` only when verification policy is satisfied
- verification completion is an Identity truth transition
- Notifications may deliver verification mail, but delivery success is not itself the verification truth
- admin access always requires authentication plus authorization policy coverage

### 3.3 Verification gating

Allowed baseline actions before verification:

- register
- resend verification
- verify email
- forgot password
- reset password

Policy-driven items:

- whether login before verification is allowed
- whether interaction actions require verified email
- whether some self-service endpoints are accessible before verification

> Final gating decisions should be captured in ADRs and applied consistently.

---

## 4) Security invariants (non-negotiable)

### 4.1 Sensitive data handling

Identity must never store or emit secrets unsafely.

Rules:

- passwords are never stored in plaintext
- password verification uses strong hashing
- access tokens, refresh tokens, verification tokens, and reset tokens must never appear in logs
- raw token values must never appear in async event payloads
- token-bearing templates and delivery flows must use minimum necessary secret exposure only
- upstream truth payloads must remain privacy-aware and minimal

### 4.2 Credential rules

- password hashing uses a strong approved algorithm
- password reset and password change should revoke active refresh tokens by policy
- reset/verification token validity must be bounded and enforceable

### 4.3 Session rules

- refresh tokens are opaque and server-controlled
- refresh token rotation is enabled by default or by recommended policy
- retry/replay of refresh-related flows must not produce inconsistent token-family state

---

## 5) Truth-success and async-side-effect semantics

### 5.1 Identity truth success

A successful Identity write means:

- Identity truth committed
- and, where required, async intent/outbox committed

It does **not** mean:

- broker publish already happened
- Notifications already sent an email
- Audit already persisted a record
- downstream consumers are caught up

### 5.2 Non-blocking requirement

The following must remain non-blocking on Notifications:

- register
- resend verification
- forgot password
- password reset request acceptance
- verification-related truth transitions

### 5.3 Reconciliation rule

If ambiguity exists after client timeout or downstream lag, the source of truth is:

- Identity truth state
- not notification completion state
- not timeout interpretation alone

---

## 6) Idempotency and retry semantics (contract-level)

Identity must be safe under retries and ambiguous client outcomes.

### 6.1 Anti-enumeration

The following flows must remain privacy-safe and anti-enumeration-aware:

- `/forgot-password`
- `/resend-verification`

Recommended response posture:

- accepted-style response regardless of account existence when policy requires it

### 6.2 Idempotency support

Idempotency keys are recommended for operations such as:

- register
- resend verification
- forgot password

### 6.3 Token and session consistency

Refresh rotation and token consumption flows must tolerate retries without producing inconsistent truth.

Examples:

- duplicate client retry must not create contradictory refresh-token family state
- consumed token must not become reusable because of retry ambiguity

---

## 7) Domain events (for async side effects)

Identity emits events for:

- Notifications
- Audit
- optional login-history or derived operational consumers

### 7.1 Event envelope

Identity async events should use the system-wide async envelope:

- `MessageId`
- `OccurredAt`
- `CorrelationId`
- `ActorUserId?`
- `EventType`
- `AggregateId`
- `Version` *(where ordered transitions matter)*
- `Payload`

**Rule:** use `MessageId` as the stable async identity name in V1 docs.

### 7.2 Event categories

Identity emits two broad categories of events:

#### A) Business/audit timeline events

Examples:

- `Auth.UserRegistered`
- `Auth.UserEmailVerified`
- `Auth.UserPasswordChanged`
- `Auth.UserLoggedIn` *(optional)*

These are useful for:

- audit ingestion
- business/ops reporting
- timeline reconstruction

#### B) Delivery-trigger events for Notifications

Examples:

- `Auth.EmailVerificationRequested`
- `Auth.PasswordResetRequested`

These are the canonical async triggers for Notifications mail delivery flows.

**Rule:** Notifications should consume delivery-trigger events, not infer mail behavior indirectly from unrelated business events unless explicitly documented.

---

## 8) Event types (V1)

### 8.1 `Auth.UserRegistered`

Purpose:

- business/audit timeline event after registration truth commits

Payload (minimal, privacy-aware):

- `UserId`
- `OccurredAt`

Optional:
- privacy-safe registration context if truly needed by downstream consumers

### 8.2 `Auth.EmailVerificationRequested`

Purpose:

- canonical delivery-trigger event for verification email

Payload (minimal, delivery-safe):

- `UserId`
- delivery-safe recipient context where necessary
- token identifier or equivalent safe verification context if needed
- `RequestedAt`

Rules:

- do **not** emit raw verification token value
- do **not** emit more recipient/context data than necessary
- payload must support safe notification delivery without leaking secret-bearing truth

### 8.3 `Auth.UserEmailVerified`

Purpose:

- business/audit event that verification truth completed

Payload:

- `UserId`
- `VerifiedAt`

### 8.4 `Auth.PasswordResetRequested`

Purpose:

- canonical delivery-trigger event for reset email

Payload (minimal, delivery-safe):

- `UserId`
- delivery-safe recipient context where necessary
- `ResetTokenId` or equivalent identifier only
- `RequestedAt`

Rules:

- do **not** emit raw reset token value
- do **not** let notification payload become a carrier of unsafe secret-bearing truth

### 8.5 `Auth.UserPasswordChanged`

Purpose:

- business/audit event that password truth changed

Payload:

- `UserId`
- `ChangedAt`
- `Reason` *(for example: `Reset` | `Change`)*

### 8.6 `Auth.UserLoggedIn` *(optional)*

Purpose:

- optional operational/audit event

Payload:

- `UserId`
- `LoggedInAt`
- optional privacy-governed metadata such as:
  - `IPAddress?`
  - `UserAgent?`

---

## 9) Payload minimization and privacy rules

### 9.1 Minimum necessary principle

Identity event payloads must be:

- minimal
- privacy-aware
- safe under logging/tracing/transport
- sufficient for the intended downstream consumer, but no more

### 9.2 Email-related events

For delivery-trigger events:

- email/recipient context may be included only when operationally necessary for delivery
- logs and admin views must still mask/redact recipient data by policy
- raw secrets/tokens are forbidden in payloads
- prefer identifiers plus safe delivery context over broad truth snapshots

### 9.3 No notification-owned truth in Identity events

Identity should emit:

- truth changes
- delivery-trigger intents

Identity should not emit events that imply:

- email already sent
- provider already accepted delivery
- notification workflow already succeeded

Those belong to Notifications delivery truth.

---

## 10) Cross-module contract with Notifications

### 10.1 What Identity guarantees

For delivery-trigger events such as:

- `Auth.EmailVerificationRequested`
- `Auth.PasswordResetRequested`

Identity guarantees:

- upstream truth/intended operation committed according to Identity rules
- async intent committed with stable `MessageId`
- payload is minimal and safe for downstream processing

### 10.2 What Identity does not guarantee

Identity does **not** guarantee:

- broker publication already completed
- mail already sent
- provider accepted the message
- delivery ambiguity already resolved

### 10.3 What Notifications owns

Notifications owns:

- delivery workflow creation
- delivery attempt history
- provider result classification
- retry/remediation state
- operational send truth

Identity must not model those concerns as its own truth.

---

## 11) ADR hooks (must be explicit)

The following decisions should remain explicit in ADRs or closely related module docs:

- verification gating rules
- login-before-verify policy
- refresh-token reuse detection response
- logout semantics (`Current` vs `All`)
- register conflict behavior
- lockout policy
- minimal payload rules for email-trigger events
- whether delivery-trigger events include direct email value or only identifier + safe delivery context