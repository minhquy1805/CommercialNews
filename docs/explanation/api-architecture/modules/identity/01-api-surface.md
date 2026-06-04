# Identity — API Surface (V1)

Base path: `/api/v1/auth`

> Identity V1 is a **truth-owning security module**.  
> It owns account, credential, token, and session truth.  
> Email delivery, audit ingestion, and other downstream side effects are asynchronous and must not define Identity success.

---

## 1) API posture in V1

Identity V1 exposes synchronous APIs for:

- account creation
- verification
- login
- refresh rotation
- logout
- forgot/reset password
- self-state and self-service profile/security actions

### Included public / self-service APIs in V1

- `POST /register`
- `POST /login`
- `POST /refresh`
- `POST /logout`
- `POST /verify-email`
- `POST /resend-verification`
- `POST /forgot-password`
- `POST /reset-password`
- `GET /me`
- `PUT /me`
- `POST /change-password`

### Not part of Identity API success semantics

The following are downstream effects and do **not** define synchronous Identity success:

- verification email sent
- reset email sent
- audit event ingested
- login-history projection updated

**Rule:** a successful Identity write means Identity truth committed, and where required, async intent/outbox committed. It does not mean downstream delivery or ingestion already completed.

## 1.1) Admin API posture in V1

Base path: `/api/v1/admin/identity`

Identity Admin APIs are privileged operational APIs for managing Identity-owned account, verification, session, and security truth.

Identity Admin APIs are synchronous for Identity truth and asynchronous for side effects.

A successful admin write means:

- Identity-owned truth has been committed.
- When async side effects are required, the corresponding `OutboxMessage` has been committed in the same transaction.

A successful admin write does **not** mean:

- the event has already been published to RabbitMQ
- Audit has already ingested the event
- Notifications has already sent an email
- Redis/cache/projections have already been refreshed

### Identity Admin owns

- `UserAccount` status/security state
- email verification state owned by Identity
- refresh-token/session revocation truth
- Identity security summary reads where data exists

### Identity Admin does not own

- roles
- permissions
- user-role assignment
- role-permission grants
- effective authorization truth
- audit-log truth
- notification delivery truth

Role and permission governance belongs to the Authorization module.

### Admin authorization

Every Identity Admin endpoint MUST declare an explicit permission policy.

Policy format:

```txt
Permission:<PermissionKey>
```

Identity Admin endpoints MUST NOT rely only on generic authentication such as `[Authorize]`.

### Admin async event rule

Every Identity Admin command that mutates Identity truth MUST emit an outbox event unless explicitly documented as a no-op/convergent command.

Audit consumes Identity Admin events asynchronously and records investigation logs idempotently.

Notifications may consume selected Identity Admin events when user-facing communication is required by policy.

### Admin timeout and retry posture

Admin client timeouts are ambiguous.

A timeout does not prove that the command failed or that no Identity truth change occurred.

Admin clients must reconcile ambiguous command outcomes by reading Identity truth for the target resource.

Identity Admin truth transactions MUST NOT retry broker publish, audit ingestion, notification delivery, Redis invalidation, or external provider calls.

---

## 2) Public / self-service endpoints

### `POST /register`

Register a new account.

#### Headers

- `Idempotency-Key` *(recommended)*
- `X-Correlation-Id` *(optional)*

#### Request

```json
{
  "email": "user@example.com",
  "password": "********"
}
```

#### Response (201)

```json
{
  "userId": "string",
  "email": "user@example.com",
  "status": "Unverified"
}
```

#### Rules

- must not block on email delivery
- verification email sending is handled asynchronously by downstream processing
- conflict behavior for existing email is policy-driven and defined in `04-errors-status-codes.md`
- if `Idempotency-Key` is reused with the same semantic request, the result should converge safely
- if the client times out, reconciliation must come from Identity truth, not from email delivery visibility
- successful registration does not imply verification email already reached the user

### `POST /login`

Authenticate and return access and refresh tokens.

#### Headers

- `X-Correlation-Id` *(optional)*

#### Request

```json
{
  "email": "user@example.com",
  "password": "********"
}
```

#### Response (200)

```json
{
  "accessToken": "jwt",
  "expiresIn": 900,
  "refreshToken": "opaque",
  "refreshExpiresIn": 2592000,
  "user": {
    "userId": "string",
    "email": "user@example.com",
    "roles": ["User"],
    "status": "Active",
    "verified": true
  }
}
```

#### Rules

- must not leak whether the account exists beyond documented authentication semantics
- rate-limited
- refresh token is opaque and must never be logged
- any downstream audit or login-history work is asynchronous and must not block login success
- returned role information is a client-facing authorization snapshot, not a transfer of ownership of authorization truth from Authorization to Identity

### `POST /refresh`

Rotate the refresh token and return a new access and refresh token pair.

#### Headers

- `X-Correlation-Id` *(optional)*

#### Request

```json
{
  "refreshToken": "opaque"
}
```

#### Response (200)

Same shape as `POST /login`.

#### Rules

- refresh token rotation is the default
- reuse detection behavior is defined in `06-idempotency-consistency.md`
- rate-limited
- this endpoint is truth-first and security-critical
- old token must not remain valid after successful rotation
- stale cache or derived state must not authorize old-token truth
- timeout ambiguity must be reconciled from refresh-token truth, not from client belief
- duplicate/stale retries must not mint multiple valid successor states incorrectly

### `POST /logout`

Revoke refresh token(s).

#### Auth

- Bearer access token

#### Headers

- `X-Correlation-Id` *(optional)*

#### Request

```json
{
  "mode": "Current"
}
```

#### Mode values

- `Current` — revoke the caller’s current session or token family
- `All` — revoke all refresh tokens for the user *(optional in V1; must be explicitly documented if implemented)*

#### Response (200)

```json
{
  "loggedOut": true
}
```

#### Rules

- logout success is based on revocation truth, not on downstream audit visibility
- if `mode = All` is supported, exact revocation semantics must be documented clearly
- timeout ambiguity must be resolved from revocation truth where relevant

### `POST /verify-email`

Verify an email token.

#### Headers

- `X-Correlation-Id` *(optional)*

#### Request

```json
{
  "token": "verification-token"
}
```

#### Response (200)

```json
{
  "verified": true
}
```

#### Rules

- token must be time-bound and single-use by policy
- repeated verification with an already-used token must return a deterministic outcome
- successful verification must support read-your-writes on subsequent `GET /me` and other self-state reads
- verification truth is owned by Identity, not by notification delivery state

### `POST /resend-verification`

Request another verification email.

#### Headers

- `Idempotency-Key` *(recommended)*
- `X-Correlation-Id` *(optional)*

#### Request

```json
{
  "email": "user@example.com"
}
```

#### Response (200)

```json
{
  "accepted": true
}
```

#### Rules

- always returns `{ "accepted": true }` and must not reveal account existence
- rate-limited
- email sending is asynchronous
- a valid resend should create a new logical verification intent if policy allows
- old delivery dedupe state must not block a legitimate new verification intent
- same-intent duplicate retries must converge safely under idempotency policy

### `POST /forgot-password`

Request a password reset email.

#### Headers

- `Idempotency-Key` *(recommended)*
- `X-Correlation-Id` *(optional)*

#### Request

```json
{
  "email": "user@example.com"
}
```

#### Response (200)

```json
{
  "accepted": true
}
```

#### Rules

- always returns `{ "accepted": true }`
- rate-limited
- email sending is asynchronous
- repeated identical requests must remain anti-enumeration-safe
- legitimate new reset-intent policy must be documented separately from duplicate retry handling
- successful acceptance does not imply reset email already delivered

### `POST /reset-password`

Reset a password using a time-bound token.

#### Headers

- `X-Correlation-Id` *(optional)*

#### Request

```json
{
  "token": "reset-token",
  "newPassword": "********"
}
```

#### Response (200)

```json
{
  "reset": true
}
```

#### Rules

- token must be time-bound and single-use by policy
- password reset should revoke refresh tokens by policy
- successful reset must make new password and session/security truth immediately authoritative
- downstream email or audit lag must not weaken revocation or password-change truth
- duplicate use of the same consumed token must return a deterministic failure/no-op policy outcome

### `GET /me`

Return the current user’s basic profile.

#### Auth

- Bearer access token

#### Headers

- `X-Correlation-Id` *(optional)*

#### Response (200)

```json
{
  "userId": "string",
  "email": "user@example.com",
  "roles": ["User"],
  "status": "Active",
  "verified": true
}
```

#### Rules

- this endpoint is subject to read-your-writes expectations after security-sensitive truth changes
- it must not serve stale cached state after verify, reset, change-password, or revocation-sensitive flows when that would misrepresent current security truth

### `PUT /me`

Update profile fields owned by self-service policy.

#### Auth

- Bearer access token

#### Headers

- `X-Correlation-Id` *(optional)*

#### Request

```json
{
  "fullName": "New Name"
}
```

#### Response (200)

```json
{
  "updated": true
}
```

#### Rules

- sensitive fields such as role, status, verification flags, and security state must be ignored or denied
- if stale-write protection is implemented, conflict behavior must be deterministic
- this endpoint must not become a backdoor for authorization or governance truth changes

### `POST /change-password`

Change password using the current password.

#### Auth

- Bearer access token

#### Headers

- `X-Correlation-Id` *(optional)*

#### Request

```json
{
  "currentPassword": "********",
  "newPassword": "********"
}
```

#### Response (200)

```json
{
  "changed": true
}
```

#### Rules

- refresh tokens should be revoked by policy *(recommended: revoke all)*
- successful password change must update live security truth immediately
- subsequent self-state and security checks must reflect the new truth without stale lag
- downstream audit or notification lag must not weaken password-change truth

---

## 3) Admin endpoints

Base path: `/api/v1/admin/identity`

Admin endpoints manage Identity-owned truth only.

All endpoints require:

- Bearer access token
- explicit permission policy
- correlation-aware logging
- audit event emission for mutations
- sanitized request/response/event payloads

### `GET /users`

List user accounts for admin operations.

#### Auth

- Bearer access token

#### Policy

```txt
Permission:Identity.User.Read
```

#### Headers

- `X-Correlation-Id` *(optional)*

#### Query

- `page`
- `pageSize`
- `q`
- `status`
- `verified`
- `sort`

#### Response (200)

```json
{
  "items": [
    {
      "userId": "string",
      "email": "user@example.com",
      "fullName": "string",
      "status": "Active",
      "verified": true,
      "createdAtUtc": "2026-05-07T00:00:00Z",
      "updatedAtUtc": "2026-05-07T00:00:00Z",
      "lastLoginAtUtc": "2026-05-07T00:00:00Z"
    }
  ],
  "pageInfo": {
    "page": 1,
    "pageSize": 20,
    "totalItems": 1,
    "totalPages": 1
  }
}
```

#### Rules

- must not expose password hashes, token hashes, raw tokens, or sensitive operational secrets
- role/permission information is not owned by Identity
- sorting and filtering fields must be allow-listed
- reads should come from Identity truth or approved read model that does not misrepresent security state

### `GET /users/{userId}`

Get Identity-owned account detail for admin review.

#### Auth

- Bearer access token

#### Policy

```txt
Permission:Identity.User.Read
```

#### Headers

- `X-Correlation-Id` *(optional)*

#### Response (200)

```json
{
  "userId": "string",
  "email": "user@example.com",
  "fullName": "string",
  "status": "Active",
  "verified": true,
  "emailVerifiedAtUtc": "2026-05-07T00:00:00Z",
  "createdAtUtc": "2026-05-07T00:00:00Z",
  "updatedAtUtc": "2026-05-07T00:00:00Z",
  "activeRefreshTokenCount": 2,
  "lastLoginAtUtc": "2026-05-07T00:00:00Z"
}
```

#### Rules

- response contains Identity-owned account/security state only
- must not expose credential or token secrets
- effective roles/permissions are owned by Authorization and must not be treated as Identity truth

### `GET /users/{userId}/sessions`

List refresh-token/session records or session summaries for a user.

#### Auth

- Bearer access token

#### Policy

```txt
Permission:Identity.Session.Read
```

#### Headers

- `X-Correlation-Id` *(optional)*

#### Response (200)

```json
{
  "items": [
    {
      "refreshTokenId": "string",
      "createdAtUtc": "2026-05-07T00:00:00Z",
      "expiresAtUtc": "2026-06-06T00:00:00Z",
      "revokedAtUtc": null,
      "ipAddress": "masked-or-truncated",
      "userAgent": "sanitized"
    }
  ]
}
```

#### Rules

- must never expose raw refresh tokens or token hashes
- session/security reads must not depend on stale cache when used for security decisions
- IP/UserAgent fields must be sanitized according to logging and privacy policy

### `GET /users/{userId}/security-summary`

Return an admin security summary for investigation and support.

#### Auth

- Bearer access token

#### Policy

```txt
Permission:Identity.User.ManageSecurity
```

#### Headers

- `X-Correlation-Id` *(optional)*

#### Response (200)

```json
{
  "userId": "string",
  "status": "Active",
  "verified": true,
  "activeRefreshTokenCount": 2,
  "lastLoginAtUtc": "2026-05-07T00:00:00Z",
  "lockedUntilUtc": null,
  "riskFlags": []
}
```

#### Rules

- fields are truth-backed where available
- V1 may return partial security summary if optional data such as login history is not implemented
- this endpoint must not expose secrets or raw token material

### `POST /users/{userId}:activate`

Activate an inactive user account.

#### Auth

- Bearer access token

#### Policy

```txt
Permission:Identity.User.ManageStatus
```

#### Headers

- `X-Correlation-Id` *(optional)*

#### Request

```json
{
  "reason": "AdminOperation"
}
```

#### Response (200)

```json
{
  "userId": "string",
  "status": "Active",
  "occurredAtUtc": "2026-05-07T00:00:00Z"
}
```

#### Rules

- updates `UserAccount` truth
- writes `identity.admin.user_activated` outbox event in the same transaction
- Audit consumes the event asynchronously
- if the user is already active, the command should converge safely and must not emit a duplicate state-change event unless explicitly required by audit policy
- success does not mean Audit has already ingested the event

### `POST /users/{userId}:deactivate`

Deactivate a user account.

#### Auth

- Bearer access token

#### Policy

```txt
Permission:Identity.User.ManageStatus
```

#### Headers

- `X-Correlation-Id` *(optional)*

#### Request

```json
{
  "reason": "AdminOperation",
  "revokeSessions": true
}
```

#### Response (200)

```json
{
  "userId": "string",
  "status": "Inactive",
  "sessionsRevoked": true,
  "occurredAtUtc": "2026-05-07T00:00:00Z"
}
```

#### Rules

- updates `UserAccount` truth
- revokes active refresh tokens when required by policy
- writes `identity.admin.user_deactivated` outbox event in the same transaction
- Audit consumes the event asynchronously and idempotently
- Notifications may consume the event if user-facing notification is enabled
- admin cannot deactivate their own account through normal admin APIs
- protected system accounts cannot be deactivated through normal admin APIs
- success is based on Identity truth and outbox intent commit, not Audit/Notifications completion

### `POST /users/{userId}:lock`

Lock a user account or set security lock state.

#### Auth

- Bearer access token

#### Policy

```txt
Permission:Identity.User.ManageSecurity
```

#### Headers

- `X-Correlation-Id` *(optional)*

#### Request

```json
{
  "reason": "SuspiciousActivity",
  "lockedUntilUtc": "2026-05-08T00:00:00Z",
  "revokeSessions": true
}
```

#### Response (200)

```json
{
  "userId": "string",
  "locked": true,
  "lockedUntilUtc": "2026-05-08T00:00:00Z",
  "sessionsRevoked": true
}
```

#### Rules

- lock state is Identity security truth
- locked users cannot sign in according to account state policy
- writes `identity.admin.user_locked` outbox event in the same transaction
- admin cannot lock their own account through normal admin APIs
- protected system accounts cannot be locked through normal admin APIs
- if lock fields are not implemented in V1 schema, this endpoint must remain out of scope until schema support exists

### `POST /users/{userId}:unlock`

Unlock a user account or clear security lock state.

#### Auth

- Bearer access token

#### Policy

```txt
Permission:Identity.User.ManageSecurity
```

#### Headers

- `X-Correlation-Id` *(optional)*

#### Request

```json
{
  "reason": "AdminReviewCompleted"
}
```

#### Response (200)

```json
{
  "userId": "string",
  "locked": false,
  "lockedUntilUtc": null
}
```

#### Rules

- clears Identity-owned lock state
- writes `identity.admin.user_unlocked` outbox event in the same transaction
- unlocking does not automatically verify email
- unlocking does not assign roles or permissions
- if the user is already unlocked, the command should converge safely

### `POST /users/{userId}:mark-email-verified`

Mark a user email as verified by admin override.

#### Auth

- Bearer access token

#### Policy

```txt
Permission:Identity.User.VerifyEmail
```

#### Headers

- `X-Correlation-Id` *(optional)*

#### Request

```json
{
  "reason": "ManualVerification"
}
```

#### Response (200)

```json
{
  "userId": "string",
  "verified": true,
  "emailVerifiedAtUtc": "2026-05-07T00:00:00Z"
}
```

#### Rules

- this is a high-risk admin override
- updates `UserAccount.IsEmailVerified` and `EmailVerifiedAt`
- may revoke/expire active verification tokens according to policy
- writes `identity.admin.email_marked_verified` outbox event in the same transaction
- Audit consumes the event asynchronously
- Notifications may consume the event if user-facing notification is enabled
- must not expose verification token hashes or raw tokens
- if already verified, the command should converge safely

### `POST /users/{userId}:revoke-sessions`

Revoke active refresh-token sessions for a user.

#### Auth

- Bearer access token

#### Policy

```txt
Permission:Identity.User.RevokeSessions
```

#### Headers

- `X-Correlation-Id` *(optional)*

#### Request

```json
{
  "reason": "AdminSecurityAction",
  "scope": "All"
}
```

#### Response (200)

```json
{
  "userId": "string",
  "revokedSessionCount": 2,
  "occurredAtUtc": "2026-05-07T00:00:00Z"
}
```

#### Rules

- revokes Identity-owned refresh-token truth
- revoked sessions must stop being valid immediately from Identity truth
- writes `identity.admin.user_sessions_revoked` outbox event in the same transaction
- Audit consumes the event asynchronously and idempotently
- admin cannot revoke their own current session through normal admin APIs unless explicitly allowed by policy
- success does not depend on Audit/Notifications/Redis completion

---

## 4) Versioning

- All endpoints are under `/api/v1`.
- Breaking changes require `/api/v2`.

---

## 5) Response conventions

- success responses are JSON
- errors follow the standard error envelope defined in `../../02-contracts-and-standards.md`

Security-sensitive failures should use deterministic, documented outcomes, such as:

- invalid token
- expired token
- already-used token
- revoked or replayed refresh token
- rate-limited
- invalid credentials

---

## 6) Rate limit classes (policy-level)

Identity endpoints must be rate-limited according to risk class.

### High risk

- `/login`
- `/refresh`

### Abuse-prone

- `/register`
- `/resend-verification`
- `/forgot-password`

### Token validation

- `/verify-email`
- `/reset-password`

Thresholds are implementation/configuration concerns and may evolve without contract changes.

---

## 7) Anti-enumeration (mandatory)

The following endpoints must not reveal account existence:

- `/forgot-password` → always `200 { "accepted": true }`
- `/resend-verification` → always `200 { "accepted": true }`

---

## 8) Idempotency (recommended)

Support `Idempotency-Key` for:

- `/register`
- `/resend-verification`
- `/forgot-password`

### Goal

Prevent duplicated side effects and ambiguous repeated client requests from producing unsafe behavior.

### Rules

- same key + same semantic payload → same/convergent outcome
- same key + different semantic payload → deterministic conflict or error
- durable idempotency records are preferred for higher-impact security flows

---

## 9) Surface-level consistency rules

- authentication and self-security decisions come from Identity truth, never from notification or audit state
- email delivery is downstream and eventual
- a timeout does not prove that register, verify, reset, logout, or refresh failed
- verification and reset tokens are single-use truth artifacts by policy
- refresh rotation is truth-first and must not allow stale token authority to survive
- cleanup, reporting, and maintenance outputs are operational and derived, not security authority
- a successful synchronous Identity response does not imply downstream notification completion

### Admin surface consistency rules

- Identity Admin commands mutate only Identity-owned truth.
- Identity Admin commands must not update Authorization, Audit, Notifications, Redis, or derived read-model truth in the same transaction.
- Identity Admin mutations that require side effects must commit an `OutboxMessage` atomically with the Identity truth change.
- Audit consumes Identity Admin events asynchronously and must dedupe by canonical `MessageId`.
- Notifications may consume selected Identity Admin events, but user-visible delivery must be protected by message-level and business-level idempotency.
- Client-side timeout after an admin mutation must be reconciled by reading Identity truth for the target user.
- Admin command success does not imply outbox publish, RabbitMQ delivery, Audit ingestion, Notification delivery, cache invalidation, or projection refresh has completed.
- Role and permission governance remains owned by Authorization.
