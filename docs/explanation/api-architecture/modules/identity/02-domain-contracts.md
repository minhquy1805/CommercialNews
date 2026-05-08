# Identity — Domain Contracts (V1)

## 1) Ownership

Identity owns:

- `UserAccount`
- `RefreshToken`
- `EmailVerificationToken`
- `PasswordResetToken`
- account identity truth
- credential truth
- email verification truth
- refresh-token/session truth
- password reset token truth
- account security state owned by Identity

Identity is the canonical source of truth for:

- which user accounts exist
- account email and normalized email identity
- password hash / credential state
- account status
- email verification state
- active/revoked refresh tokens
- verification token lifecycle
- password reset token lifecycle
- session revocation truth

Identity does **not** own:

- roles
- permissions
- user-role assignment
- role-permission grants
- effective authorization truth
- audit-log truth
- notification delivery truth
- downstream projections or cache truth

Role and permission governance belongs to the Authorization module.

Audit and Notifications may consume Identity events asynchronously, but they do not define Identity truth.

---

## 2) Domain responsibilities

Identity is responsible for:

- registering user accounts
- authenticating email/password credentials
- issuing and rotating refresh tokens
- revoking refresh tokens
- verifying email ownership through time-bound tokens
- issuing password reset tokens
- resetting passwords through valid reset tokens
- supporting self-service profile/security operations
- supporting admin account/session/security operations over Identity-owned truth
- emitting outbox events for important account/security lifecycle changes

Identity must support:

- strong local consistency for security/auth state changes
- deterministic token validation outcomes
- safe refresh-token rotation and revocation
- anti-enumeration behavior for forgot-password and resend-verification flows
- async side effects through Outbox, not direct synchronous downstream calls
- truth-backed reconciliation after timeout ambiguity

Identity must **not** rely on:

- email delivery state as proof of account truth
- audit ingestion as part of Identity success
- Redis/cache as the source of truth for security decisions
- client-supplied role/permission data as authoritative
- downstream projections as authority for login, refresh, verification, or session revocation

---

## 3) Entities — conceptual contract

### 3.1 UserAccount

A `UserAccount` represents an Identity-owned user account and credential boundary.

**Fields**

- `UserId`
- `Email`
- `EmailNormalized`
- `PasswordHash`
- `FullName?`
- `Status`
- `IsEmailVerified`
- `EmailVerifiedAt?`
- `CreatedAt`
- `UpdatedAt`
- `LastLoginAt?` *(optional / derived depending on implementation)*
- `LockedUntil?` *(conditional if lockout is implemented in V1)*
- `FailedLoginCount?` *(optional / policy-defined)*

**Notes**

- `EmailNormalized` is the canonical comparison key.
- `PasswordHash` is secret-bearing and must never be returned, logged, or emitted.
- `Status` gates authentication according to policy.
- `IsEmailVerified` and `EmailVerifiedAt` are Identity truth.
- Lock fields are conditional until schema support exists.

---

### 3.2 RefreshToken

A `RefreshToken` represents an opaque refresh-token session artifact.

**Fields**

- `RefreshTokenId` or equivalent stable internal identity
- `UserId`
- `TokenHash`
- `CreatedAt`
- `ExpiresAt`
- `RevokedAt?`
- `ReplacedByTokenHash?`
- `CreatedByIp?`
- `RevokedByIp?`
- `UserAgent?`

**Notes**

- raw refresh tokens must never be stored.
- `TokenHash` must never be returned, logged, or emitted.
- refresh-token rotation revokes the old token and issues a successor.
- revoked or expired refresh tokens must not authorize refresh.
- reuse detection behavior is policy-defined and documented in `06-idempotency-consistency.md`.

---

### 3.3 EmailVerificationToken

An `EmailVerificationToken` represents a time-bound, one-time-use verification artifact.

**Fields**

- `EmailVerificationTokenId`
- `UserId`
- `TokenHash`
- `ExpiresAt`
- `UsedAt?`
- `CreatedAt`
- `RevokedAt?` *(optional if policy requires explicit revocation)*

**Notes**

- raw verification tokens must never be stored.
- `TokenHash` must never be returned, logged, or emitted.
- a token is usable only when it is not used, not expired, and not revoked if revocation is implemented.
- admin email verification override must not expose or require raw token material.

---

### 3.4 PasswordResetToken

A `PasswordResetToken` represents a time-bound, one-time-use password reset artifact.

**Fields**

- `PasswordResetTokenId`
- `UserId`
- `TokenHash`
- `ExpiresAt`
- `UsedAt?`
- `CreatedAt`
- `RevokedAt?` *(optional if policy requires explicit revocation)*

**Notes**

- raw reset tokens must never be stored.
- `TokenHash` must never be returned, logged, or emitted.
- issuing a new reset token may revoke older active reset tokens according to policy.
- successful password reset updates credential truth atomically.

---

### 3.5 LoginHistory *(optional / recommended)*

`LoginHistory` represents minimal operational login evidence for abuse detection and investigation.

**Fields**

- `LoginHistoryId`
- `UserId?`
- `EmailNormalized?`
- `OccurredAt`
- `Succeeded`
- `FailureReason?`
- `IpAddress?`
- `UserAgent?`
- `CorrelationId?`

**Notes**

- Login history must remain privacy-aware.
- Login history must not store raw credentials or tokens.
- Login history may be written asynchronously depending on implementation.
- Login history must not define authentication success.

---

## 4) Contract DTOs

Contract names should describe the resource and purpose.

Admin context is represented by namespace, route, folder, and policy — not by prefixing every DTO with `Admin`.

### 4.1 Public / self-service contracts

#### RegisterUser

Request:

- `Email`
- `Password`

Response:

- `UserId`
- `Email`
- `Status`

#### LoginUser

Request:

- `Email`
- `Password`

Response:

- `AccessToken`
- `ExpiresIn`
- `RefreshToken`
- `RefreshExpiresIn`
- `User`

#### RefreshToken

Request:

- `RefreshToken`

Response:

- same shape as login response

#### VerifyEmail

Request:

- `Token`

Response:

- `Verified`

#### ResendVerificationEmail

Request:

- `Email`

Response:

- `Accepted`

#### ForgotPassword

Request:

- `Email`

Response:

- `Accepted`

#### ResetPassword

Request:

- `Token`
- `NewPassword`

Response:

- `Reset`

#### GetMyProfile

Response:

- `UserId`
- `Email`
- `FullName?`
- `Status`
- `Verified`
- `Roles[]` *(client-facing snapshot only; Authorization remains the owner)*

#### UpdateMyProfile

Request:

- `FullName`

Response:

- `Updated`

#### ChangePassword

Request:

- `CurrentPassword`
- `NewPassword`

Response:

- `Changed`

---

### 4.2 Admin account contracts

#### UserAccountListItem

Used for admin user list views.

Fields:

- `UserId`
- `Email`
- `FullName?`
- `Status`
- `Verified`
- `CreatedAtUtc`
- `UpdatedAtUtc?`
- `LastLoginAtUtc?`

#### UserAccountDetail

Used for admin account detail views.

Fields:

- `UserId`
- `Email`
- `FullName?`
- `Status`
- `Verified`
- `EmailVerifiedAtUtc?`
- `CreatedAtUtc`
- `UpdatedAtUtc?`
- `ActiveRefreshTokenCount`
- `LastLoginAtUtc?`
- `LockedUntilUtc?`

#### UserSessionItem

Used for admin session/security views.

Fields:

- `RefreshTokenId`
- `CreatedAtUtc`
- `ExpiresAtUtc`
- `RevokedAtUtc?`
- `IpAddress?`
- `UserAgent?`

Rules:

- `RefreshTokenId` must be a public-safe identifier.
- It must not expose or be derived from raw token value or token hash.
- raw refresh tokens and token hashes must never be returned.

#### UserSecuritySummary

Used for admin security investigation views.

Fields:

- `UserId`
- `Status`
- `Verified`
- `ActiveRefreshTokenCount`
- `LastLoginAtUtc?`
- `LockedUntilUtc?`
- `RiskFlags[]`

Rules:

- V1 may return partial security summary if optional data is not implemented.
- The summary must not expose secrets or raw token material.

---

### 4.3 Admin command contracts

#### ActivateUserRequest

Fields:

- `Reason`

#### ActivateUserResult

Fields:

- `UserId`
- `Status`
- `OccurredAtUtc`

#### DeactivateUserRequest

Fields:

- `Reason`
- `RevokeSessions`

#### DeactivateUserResult

Fields:

- `UserId`
- `Status`
- `SessionsRevoked`
- `OccurredAtUtc`

#### LockUserRequest

Fields:

- `Reason`
- `LockedUntilUtc?`
- `RevokeSessions`

#### LockUserResult

Fields:

- `UserId`
- `Locked`
- `LockedUntilUtc?`
- `SessionsRevoked`

#### UnlockUserRequest

Fields:

- `Reason`

#### UnlockUserResult

Fields:

- `UserId`
- `Locked`
- `LockedUntilUtc?`

#### MarkEmailVerifiedRequest

Fields:

- `Reason`

#### MarkEmailVerifiedResult

Fields:

- `UserId`
- `Verified`
- `EmailVerifiedAtUtc`

#### RevokeUserSessionsRequest

Fields:

- `Reason`
- `Scope`

Allowed `Scope` values:

- `All`

#### RevokeUserSessionsResult

Fields:

- `UserId`
- `RevokedSessionCount`
- `OccurredAtUtc`

---

## 5) Lifecycle rules

### 5.1 UserAccount lifecycle

User accounts support lifecycle transitions through:

- register
- verify email
- update profile
- change password
- activate
- deactivate
- lock *(conditional if schema supports lock state)*
- unlock *(conditional if schema supports lock state)*

**V1 posture**

- physical delete is not part of normal Identity account management.
- deactivate is preferred for disabling account access.
- protected system accounts may be blocked from unsafe admin mutation.
- repeated state-setting commands must converge safely.

---

### 5.2 RefreshToken lifecycle

Refresh tokens support lifecycle transitions through:

- issue
- rotate
- revoke current token
- revoke all active tokens for a user
- expire naturally
- mark replaced by successor token

**V1 posture**

- refresh-token rotation is the default refresh model.
- successful rotation must revoke the old token.
- revoke-sessions must take effect immediately from Identity truth.
- token reuse after revocation is suspicious and policy-defined.

---

### 5.3 EmailVerificationToken lifecycle

Verification tokens support lifecycle transitions through:

- issue
- use once
- expire
- revoke/obsolete active token when policy requires

**V1 posture**

- tokens are time-bound and single-use.
- admin email verification override may obsolete active verification tokens according to policy.
- verification email delivery is async and does not define verification truth.

---

### 5.4 PasswordResetToken lifecycle

Password reset tokens support lifecycle transitions through:

- issue
- use once
- expire
- revoke/obsolete prior active tokens

**V1 posture**

- tokens are time-bound and single-use.
- issuing a new reset token should invalidate older active reset tokens according to policy.
- successful password reset updates credential truth atomically.

---

## 6) Invariants

### 6.1 Account identity invariants

- `EmailNormalized` must be unique.
- email normalization must be deterministic.
- one account identity maps to one canonical normalized email.
- account lookup for login and anti-enumeration flows must use normalized email.

### 6.2 Secret-handling invariants

Identity must never return, log, or emit:

- `PasswordHash`
- raw verification token
- verification `TokenHash`
- raw reset token
- reset `TokenHash`
- raw refresh token
- refresh `TokenHash`

### 6.3 Authentication gate invariants

- inactive users cannot sign in.
- locked users cannot sign in where lock state exists.
- email verification gating is policy-defined.
- stale cache or derived state must not authorize authentication when Identity truth would deny it.

### 6.4 Token lifecycle invariants

- verification tokens are time-bound and single-use.
- password reset tokens are time-bound and single-use.
- refresh tokens are time-bound and revocable.
- revoked refresh tokens must not authorize refresh.
- expired refresh tokens must not authorize refresh.
- successful refresh rotation must not leave multiple active successor tokens for the same rotation intent.

### 6.5 Admin mutation invariants

- admin account/security mutations must be protected by explicit permission policies.
- admin status/security mutations must emit outbox events unless documented as no-op/convergent.
- admin cannot deactivate or lock their own account through normal admin APIs.
- protected system accounts cannot be deactivated or locked through normal admin APIs.
- Identity Admin must not assign or revoke roles.
- Identity Admin must not grant or revoke permissions.

### 6.6 Convergent command invariants

State-setting admin commands are convergent by default.

Examples:

- activating an already active user should return the current committed state.
- deactivating an already inactive user should return the current committed state.
- unlocking an already unlocked user should return the current committed state.
- marking an already verified email should return the current committed state.

Convergent no-op commands should not emit duplicate state-change events unless explicitly required by audit policy.

### 6.7 Replay-safety invariant

Replayed or delayed downstream effects must not:

- re-enable a revoked refresh token.
- re-open a used verification/reset token.
- override fresher account security truth.
- create duplicate audit records.
- send duplicate harmful notifications without business-level idempotency.

---

## 7) Policy coverage invariant

- 100% of `/api/v1/admin/identity/*` endpoints must enforce explicit authorization policies.
- policy format is `Permission:<PermissionKey>`.
- authorization checks must be deny-by-default.
- generic authentication alone is not sufficient for admin endpoints.
- object-level ABAC checks must be composed with permission checks where required.

Required V1 permissions:

- `Identity.User.Read`
- `Identity.Session.Read`
- `Identity.User.ManageStatus`
- `Identity.User.ManageSecurity`
- `Identity.User.VerifyEmail`
- `Identity.User.RevokeSessions`

Optional / recommended if finer-grained read security is desired:

- `Identity.User.ReadSecurity`

---

## 8) ABAC context contract

Identity Admin may require ABAC checks for high-risk operations.

### 8.1 SubjectAttributes

- `UserId`
- `Email`
- `Roles[]`
- `TenantId?`
- `Groups[]?`
- `Department?`
- `IsMfaOn?`

### 8.2 ResourceAttributes

For Identity user resources:

- `ResourceType = "Identity.User"`
- `Id = target UserId`
- `OwnerId = target UserId`
- `Status = target account status`
- `Sensitivity = "Protected"` when target is a protected system account
- `TenantId?`

### 8.3 EnvironmentAttributes

- `Now`
- `Ip?`
- `UserAgent?`
- `CorrelationId?`
- `TenantId?`

### 8.4 Required ABAC rules

High-risk Identity Admin actions must enforce contextual rules such as:

- actor cannot deactivate themselves.
- actor cannot lock themselves.
- actor cannot revoke their own current session through normal admin APIs unless explicitly allowed.
- protected system accounts cannot be deactivated or locked through normal admin APIs.
- future: MFA may be required for selected high-risk operations.
- future: tenant/department constraints may apply.

---

## 9) Domain events

Identity emits minimal, privacy-aware events through the standard outbox path.

### 9.1 Event identity and transport posture

Identity events must:

- be emitted only with committed Identity truth.
- carry stable `MessageId`.
- include `EventType`.
- include `AggregateType`.
- include `AggregateId`.
- include `Version` where ordered transitions matter.
- include `OccurredAt`.
- include `CorrelationId` where available.
- be safe under retry, duplicate delivery, and replay.
- remain minimal and privacy-aware.

### 9.2 Public / self-service events

Examples:

- `identity.user_registered`
- `identity.verification_requested`
- `identity.email_verified`
- `identity.password_reset_requested`
- `identity.password_changed`
- `identity.refresh_token_rotated`
- `identity.user_logged_out`

### 9.3 Admin events

- `identity.admin.user_activated`
- `identity.admin.user_deactivated`
- `identity.admin.user_locked`
- `identity.admin.user_unlocked`
- `identity.admin.email_marked_verified`
- `identity.admin.user_sessions_revoked`

### 9.4 Event payload rules

Events should include only the minimum necessary data.

Allowed payload examples:

- `ActorUserId?`
- `TargetUserId`
- `PreviousStatus?`
- `NewStatus?`
- `Reason?`
- `RevokedSessionCount?`
- `OccurredAtUtc`
- `CorrelationId?`
- `RequiredPermission?`

Events must not include:

- password hash
- raw tokens
- token hashes
- unnecessary PII
- full authorization role/permission snapshots

### 9.5 Downstream posture

Downstream consumers such as Audit, Notifications, cache invalidation handlers, or projections must tolerate:

- at-least-once delivery
- duplicate Identity events
- delayed Identity events
- replayed Identity events
- stale event arrival

These downstream effects do not define Identity truth.

Audit must dedupe by `MessageId` or equivalent `AuditEventId`.

Notifications must use durable delivery tracking and business-level idempotency where duplicate user-visible delivery is harmful.

---

## 10) Read/write truth contract

### 10.1 Write success means

For Identity-changing operations, success means:

- Identity truth committed successfully.
- async intent/outbox committed where applicable.

Write success does **not** guarantee that:

- RabbitMQ publish has completed.
- an audit record is already queryable.
- an email has already been sent.
- cache invalidation has already propagated.
- derived projections already reflect the change.
- downstream reporting already reflects the change.

### 10.2 Read-after-write requirement

For security-sensitive Identity flows:

- post-write reads must reflect authoritative current truth.
- immediate reconciliation must use Identity truth, not downstream audit/cache/materialization visibility.
- successful verify/reset/change-password/revoke-session operations must be visible to subsequent truth-backed reads.
- stale cache must not misrepresent current security truth.

### 10.3 Timeout ambiguity

A timeout does not prove that an Identity command failed.

After client-side timeout, the caller should reconcile by reading Identity truth where possible.

Examples:

- after admin deactivate timeout, read `GET /api/v1/admin/identity/users/{userId}`.
- after admin revoke-sessions timeout, read session/security truth.
- after self-service verify/reset ambiguity, use truth-backed status or deterministic token outcome.

---

## 11) Abuse and safety rules

Identity must support safe handling of:

- repeated client retries.
- timeout ambiguity.
- duplicate requests.
- repeated equivalent admin commands.
- password reset/resend loops.
- verification resend abuse.
- login brute force attempts.
- refresh-token replay or reuse attempts.
- protected account mutation attempts.

The model must produce deterministic outcomes for:

- duplicate email registration attempts.
- invalid credentials.
- invalid/expired/used tokens.
- revoked refresh tokens.
- repeated equivalent admin state-setting commands.
- protected system account mutation attempts.
- unauthorized admin operations.

---

## 12) V1 boundaries and exclusions

V1 includes:

- account registration
- email/password login
- refresh-token rotation
- logout/session revocation
- email verification
- resend verification
- forgot/reset password
- self profile/security actions
- Identity Admin account reads
- Identity Admin status/security mutations where schema supports them
- Identity Admin session revocation
- Identity Admin email verification override
- outbox-backed async events for important account/security changes

V1 may conditionally include:

- lock/unlock user if schema supports lock state
- login-history-backed security summary
- actor-based throttling for admin security operations
- `Identity.User.ReadSecurity` permission for sensitive security reads

V1 does not include:

- role assignment
- permission grants
- effective authorization ownership
- external identity providers
- MFA factors
- password history
- risk engine as authoritative truth
- direct synchronous audit insertion
- direct synchronous email delivery inside Identity write transactions

Future ABAC, MFA, external identity, and risk-based flows must compose with — not replace — Identity-owned account/session/security truth.