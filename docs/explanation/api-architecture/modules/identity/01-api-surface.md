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

### Included in V1

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

---

## 2) Endpoints

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

## 3) Versioning

- All endpoints are under `/api/v1`.
- Breaking changes require `/api/v2`.

---

## 4) Response conventions

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

## 5) Rate limit classes (policy-level)

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

## 6) Anti-enumeration (mandatory)

The following endpoints must not reveal account existence:

- `/forgot-password` → always `200 { "accepted": true }`
- `/resend-verification` → always `200 { "accepted": true }`

---

## 7) Idempotency (recommended)

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

## 8) Surface-level consistency rules

- authentication and self-security decisions come from Identity truth, never from notification or audit state
- email delivery is downstream and eventual
- a timeout does not prove that register, verify, reset, logout, or refresh failed
- verification and reset tokens are single-use truth artifacts by policy
- refresh rotation is truth-first and must not allow stale token authority to survive
- cleanup, reporting, and maintenance outputs are operational and derived, not security authority
- a successful synchronous Identity response does not imply downstream notification completion