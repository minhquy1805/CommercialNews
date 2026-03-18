**Response (201)**

```json
{
  "userId": "string",
  "email": "user@example.com",
  "status": "Unverified"
}
```

**Rules**

* Must not block on email delivery (Notifications handles async sending).
* Conflict behavior (`email exists`) is policy-driven (see `04-errors-status-codes.md`).
* If `Idempotency-Key` is reused with the same semantic request, the result should converge safely.
* If the client times out, reconciliation must come from Identity truth, not from email delivery visibility.

### POST `/login`

Authenticate and return access and refresh tokens.

**Headers**

* `X-Correlation-Id` (optional)

**Request**

```json
{
  "email": "user@example.com",
  "password": "********"
}
```

**Response (200)**

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

**Rules**

* Do not leak whether the account exists beyond normal authentication semantics.
* Rate-limited.
* Refresh token is opaque and must never be logged.
* Any downstream audit or login-history work is asynchronous and must not block login success.

### POST `/refresh`

Rotate the refresh token and return a new access and refresh token pair.

**Headers**

* `X-Correlation-Id` (optional)

**Request**

```json
{
  "refreshToken": "opaque"
}
```

**Response (200)**

Same shape as `POST /login`.

**Rules**

* Refresh token rotation is the default.
* Reuse detection behavior is defined in `06-idempotency-consistency.md`.
* Rate-limited.
* This endpoint is truth-first and security-critical:

  * old token must not remain valid after successful rotation
  * stale cache or derived state must not authorize old-token truth
* Timeout ambiguity must be reconciled from refresh-token truth, not from client belief.

### POST `/logout`

Revoke refresh token(s).

**Auth**

* Bearer access token

**Headers**

* `X-Correlation-Id` (optional)

**Request**

```json
{
  "mode": "Current"
}
```

**Mode values**

* `Current` — revoke the caller’s current session or token family
* `All` — revoke all refresh tokens for the user (optional in V1)

**Response (200)**

```json
{
  "loggedOut": true
}
```

**Rules**

* Logout success is based on revocation truth, not on downstream audit visibility.
* If `mode = All` is supported, exact revocation semantics must be documented clearly.

### POST `/verify-email`

Verify an email token.

**Headers**

* `X-Correlation-Id` (optional)

**Request**

```json
{
  "token": "verification-token"
}
```

**Response (200)**

```json
{
  "verified": true
}
```

**Rules**

* Token must be time-bound and single-use by policy.
* Repeated verification with an already-used token must return a deterministic outcome.
* Successful verification must support read-your-writes on subsequent `GET /me` and other self-state reads.

### POST `/resend-verification`

Request another verification email (rate-limited, anti-enumeration).

**Headers**

* `Idempotency-Key` (recommended)
* `X-Correlation-Id` (optional)

**Request**

```json
{
  "email": "user@example.com"
}
```

**Response (200)**

```json
{
  "accepted": true
}
```

**Rules**

* Always returns `{ "accepted": true }` and must not reveal account existence.
* Email sending is asynchronous.
* A valid resend should create a new logical intent if policy allows.
* Old delivery dedupe state must not block a legitimate new verification intent.

### POST `/forgot-password`

Request a password reset email (rate-limited, anti-enumeration).

**Headers**

* `Idempotency-Key` (recommended)
* `X-Correlation-Id` (optional)

**Request**

```json
{
  "email": "user@example.com"
}
```

**Response (200)**

```json
{
  "accepted": true
}
```

**Rules**

* Always returns `{ "accepted": true }`.
* Email sending is asynchronous.
* Repeated identical requests should remain anti-enumeration-safe.
* Legitimate new reset-intent policy must be documented separately from duplicate retry handling.

### POST `/reset-password`

Reset a password using a time-bound token.

**Headers**

* `X-Correlation-Id` (optional)

**Request**

```json
{
  "token": "reset-token",
  "newPassword": "********"
}
```

**Response (200)**

```json
{
  "reset": true
}
```

**Rules**

* Token must be time-bound and single-use by policy.
* Password reset should revoke refresh tokens by policy (see `06-idempotency-consistency.md`).
* Successful reset must make new password and security truth immediately authoritative.
* Downstream email or audit lag must not weaken revocation or password-change truth.

### GET `/me`

Return the current user’s basic profile.

**Auth**

* Bearer access token

**Headers**

* `X-Correlation-Id` (optional)

**Response (200)**

```json
{
  "userId": "string",
  "email": "user@example.com",
  "roles": ["User"],
  "status": "Active",
  "verified": true
}
```

**Rules**

* This endpoint is subject to read-your-writes expectations after security-sensitive truth changes.
* It must not serve stale cached state after verify, reset, or change-password when that would misrepresent current security truth.

### PUT `/me`

Update profile (non-sensitive fields only).

**Auth**

* Bearer access token

**Headers**

* `X-Correlation-Id` (optional)

**Request**

```json
{
  "fullName": "New Name"
}
```

**Response (200)**

```json
{
  "updated": true
}
```

**Rules**

* Sensitive fields such as role, status, and verification flags must be ignored or denied.
* If stale-write protection is implemented, conflict behavior should be deterministic.

### POST `/change-password`

Change password (requires the current password).

**Auth**

* Bearer access token

**Headers**

* `X-Correlation-Id` (optional)

**Request**

```json
{
  "currentPassword": "********",
  "newPassword": "********"
}
```

**Response (200)**

```json
{
  "changed": true
}
```

**Rules**

* Refresh tokens should be revoked by policy (recommended: revoke all).
* Successful password change must update live security truth immediately.
* Subsequent self-state and security checks must reflect the new truth without stale lag.

---

## 2) Versioning

* All endpoints are under `/api/v1`.
* Breaking changes require `/api/v2` (see system docs).

---

## 3) Response conventions

* Success responses are JSON.
* Errors follow the standard error envelope defined in `../../02-contracts-and-standards.md`.
* Security-sensitive failures should use deterministic, documented outcomes, such as:

  * invalid token
  * expired token
  * already-used token
  * revoked or replayed refresh token
  * rate-limited
  * invalid credentials

---

## 4) Rate limit classes (policy-level)

Identity endpoints must be rate-limited:

* **High risk:** `/login`, `/refresh`
* **Abuse-prone:** `/register`, `/resend-verification`, `/forgot-password`
* **Token validation:** `/verify-email`, `/reset-password`

Thresholds are implementation and configuration level concerns and may evolve without contract changes.

---

## 5) Anti-enumeration (mandatory)

* `/forgot-password` always returns `200 { "accepted": true }`
* `/resend-verification` always returns `200 { "accepted": true }`

---

## 6) Idempotency (recommended)

Support `Idempotency-Key` for:

* `/register`
* `/resend-verification`
* `/forgot-password`

**Goal**

Prevent duplicated side effects under retries, such as duplicate emails or duplicate requests.

**Rules**

* same key + same semantic payload -> same outcome
* same key + different semantic payload -> deterministic conflict or error
* durable idempotency records are preferred for higher-impact security flows

---

## 7) Surface-level consistency rules

* Authentication and authorization decisions come from Identity truth, never from notification or audit state.
* Email delivery is downstream and eventual.
* A timeout does not prove that register, verify, reset, or refresh failed.
* Verification and reset tokens are single-use truth artifacts by policy.
* Refresh rotation is truth-first and must not allow stale token authority to survive.
* Cleanup, reporting, and maintenance outputs are operational and derived, not security authority.
