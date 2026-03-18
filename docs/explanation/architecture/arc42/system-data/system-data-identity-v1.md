# System Data Model — Identity (V1)

> **Recommended file:** `explanation/architecture/arc42/system-data/system-data-identity-v1.md`  
> **Module:** Identity  
> **Purpose:** Account management (email/password), verification, refresh-token sessions, and password reset — designed for security, resilience, and operations.

---

## 0) Data System fit (V1)

Identity is a **security-critical OLTP module** with bursty and abuse-prone traffic patterns.

- **Truth store:** SQL Server (`UserAccount` + token tables)
- **Async side effects:** Notifications via Outbox + broker (verification email, reset email)
- **Redis:** rate limiting + idempotency/dedup + optional user cache
- **Logging:** minimal PII, correlation-first

**Non-negotiables**
- Abuse prevention for register / resend / forgot / reset
- Non-blocking email delivery (async + retry-safe + no duplicates)
- No secrets, raw tokens, or sensitive PII leakage in logs/events

---

## 1) Capability → Entity mapping

### 1.1 Sign up / Sign in (email + password)

**Entities**
- `UserAccount`
- `LoginHistory` *(recommended for abuse detection and investigations)*

**V2 hooks**
- `ExternalIdentity` (Google / OIDC / SAML)
- `MfaFactor` (TOTP / WebAuthn)

---

### 1.2 Email verification (resend rate-limited)

**Entities**
- `EmailVerificationToken`

**Optional**
- `LoginHistory` or a dedicated attempt log  
  *(rate limit is enforced primarily in Redis)*

---

### 1.3 Session management (refresh tokens / logout / rotate-revoke)

**Entities**
- `RefreshToken`

**V2 optional**
- `UserSession` (device/session tracking)

---

### 1.4 Forgot / reset password

**Entities**
- `PasswordResetToken`

---

### 1.5 Profile update + change password

**Entities**
- `UserAccount`

**V2 optional**
- `PasswordHistory`

---

## 2) Identity entities (V1)

### V1 must-have
1. `UserAccount`
2. `RefreshToken`
3. `EmailVerificationToken`
4. `PasswordResetToken`

### V1 recommended
1. `LoginHistory`

### V2 hooks
- `ExternalIdentity`
- `UserSession`
- `MfaFactor`
- `AccountLockout`
- `RiskFlag`

---

## 3) Relationships (V1)

- `UserAccount (1) → RefreshToken (0..N)`  
  Multi-device sessions

- `UserAccount (1) → EmailVerificationToken (0..N)`  
  Resend may issue a new token

- `UserAccount (1) → PasswordResetToken (0..N)`  
  Policy may revoke prior tokens

- `UserAccount (1) → LoginHistory (0..N)`  
  Optional, minimal PII only

---

## 4) Invariants (V1 rules)

### A) Account identity & state

1. **Email is unique**
   - `EmailNormalized` must be unique.

2. **Account state gates authentication**
   - `Inactive` / `Locked` users cannot sign in.
   - Email verification gating is controlled by policy / ADR.

3. **Sensitive fields must never leak**
   - Never log or emit:
     - `PasswordHash`
     - raw verification tokens
     - raw reset tokens
     - raw refresh tokens

---

### B) Email verification safety

1. **Verification token is time-bound and one-time use**
   - `ExpiresAt` is required.
   - `UsedAt` can be set only once.

2. **Resend is rate-limited**
   - Enforced primarily through Redis.

---

### C) Refresh token semantics

1. **Token value uniqueness**
   - `TokenHash` must be globally unique.

2. **Rotation**
   - On refresh:
     - issue a new token
     - revoke the old token
     - set `ReplacedByTokenHash`

3. **Reuse detection hook**
   - If a revoked token is presented again, treat it as suspicious.
   - V2 may revoke the token family and raise a risk flag.

4. **Logout**
   - Logout current device: revoke one token
   - Logout all devices: revoke all active tokens for the user

---

### D) Password reset safety

1. **Reset token is time-bound and one-time use**

2. **Issuing a new reset token invalidates older active ones**
   - Revoke or expire all previously active reset tokens for that user

---

### E) Profile change rules

- Password change requires current password *(policy-defined)* and must be atomic.

---

## 5) Dataflows (V1) — REST / DB / Broker

> Key principle: core identity flows must still succeed even when email delivery is degraded.

### 5.1 Register → verification email (async)

**Sync (API → DB)**
1. Create `UserAccount` with `IsEmailVerified = 0`
2. Create `EmailVerificationToken`  
   Store `TokenHash`, never raw token

**Async (Outbox / Broker → Notifications)**
3. Emit event via Outbox:
   - `VerificationRequested`
   - or `UserRegistered`
4. Notifications sends verification email containing the **raw token**
   - DB stores only the hash

**Failure behavior**
- Email send failure must not roll back registration
- Retries must be safe and must not send harmful duplicates

---

### 5.2 Verify email

- API receives raw `token`
- Hash the token
- Lookup by `TokenHash` where:
  - `UsedAt IS NULL`
  - `ExpiresAt > now`
- Mark token as used
- Set:
  - `UserAccount.IsEmailVerified = 1`
  - `EmailVerifiedAt = now`
- Do all of the above in one transaction
- Optionally emit `UserEmailVerified`

---

### 5.3 Forgot password → reset email (async)

- Create `PasswordResetToken`  
  Store hash only
- Revoke prior active reset tokens according to policy
- Emit `PasswordResetRequested` via Outbox
- Notifications sends reset email
- Retries must be safe and deduplicated

---

### 5.4 Reset password

- Validate token by hash lookup
- Require token to be:
  - unused
  - not expired
- Mark token as used
- Update `PasswordHash` atomically
- Optionally emit `UserPasswordChanged`

---

### 5.5 Refresh token rotation

- Validate refresh token by `TokenHash`
- Ensure token is:
  - not revoked
  - not expired
- Issue new refresh token
- Revoke old token
- Set `ReplacedByTokenHash`
- Reuse detection hook applies if a revoked token is presented

---

## 6) Redis plan (Identity V1)

### 6.1 Rate limiting (required)

Use Redis `INCR + EXPIRE` per flow and identifier.

**Recommended keys**
- `cn:rl:register:ip:{ip}:{window}`
- `cn:rl:register:email:{emailNorm}:{window}`
- `cn:rl:resend:email:{emailNorm}:{window}`
- `cn:rl:forgot:ip:{ip}:{window}`
- `cn:rl:forgot:email:{emailNorm}:{window}`
- `cn:rl:login:ip:{ip}:{window}` *(optional)*

**Policy notes**
- Do not leak account existence
- Keep rate-limit windows configurable

---

### 6.2 Idempotency for email-triggering commands (optional but recommended)

To prevent double issuance under retries/timeouts:

- `cn:idem:{operation}:{idempotencyKey}` → cached result with TTL (for example 10–60 minutes)

**Operations**
- register
- resend-verification
- forgot-password

---

### 6.3 Dedup for async consumers (required)

- `cn:msg:processed:{messageId}` with TTL 7–30 days  
  Align with replay window

---

### 6.4 Optional user cache

- `cn:user:{userId}`
- `cn:user:email:{emailNorm}`

TTL: 5–30 minutes

Invalidate on:
- profile update
- email verification
- status change

---

## 7) Fields (logical schema) — SQL Server (V1)

### 7.1 `UserAccount`
Keep the current table definition, but ensure it covers:
- unique normalized email
- password hash
- account status
- email verification state
- created/updated timestamps
- optional lock / operational fields if policy requires them

---

### 7.2 `EmailVerificationToken`
Keep the hash-only token design.

Must support:
- `UserId`
- `TokenHash`
- `ExpiresAt`
- `UsedAt`
- `CreatedAt`

---

### 7.3 `PasswordResetToken`
Keep the same pattern as verification token.

Must support:
- `UserId`
- `TokenHash`
- `ExpiresAt`
- `UsedAt`
- `CreatedAt`

Optional:
- explicit revocation fields if policy requires them

---

### 7.4 `RefreshToken`
Keep the current rotation-friendly design.

Should support:
- `UserId`
- `TokenHash`
- `CreatedAt`
- `ExpiresAt`
- `RevokedAt`
- `ReplacedByTokenHash`
- optional IP / UserAgent metadata

---

### 7.5 `LoginHistory` *(optional but recommended)*
Keep it minimal and privacy-aware.

Should store only the minimum needed for:
- investigations
- abuse detection
- operational review

---

## 8) Constraints & indexes — Identity (V1)

### 8.1 PK / FK / UNIQUE / CHECK

Keep the current constraint set if it already covers:
- PKs for all truth tables
- FKs from token/history tables to `UserAccount`
- unique `EmailNormalized`
- unique `TokenHash` where required
- validity checks on status / dates where appropriate

---

### 8.2 Indexes (performance & ops)

Recommended baseline:
- unique index on `UserAccount.EmailNormalized`
- unique index on `RefreshToken.TokenHash`
- lookup indexes for active verification/reset token validation
- FK-supporting indexes on `UserId`
- optional:
  - `IX_UserAccount_Status_LockedUntil` on `(Status, LockedUntil)`

This last index helps with:
- lockout checks
- operational queries
- account status filtering

---

## 9) Retention & operational jobs (V1 policy)

### 9.1 Cleanup jobs

Scheduled cleanup should handle:
- expired verification tokens
- expired reset tokens
- refresh tokens that are both expired and retained past policy threshold
- old login history beyond retention policy

---

### 9.2 Safety

Cleanup must never remove data needed for:
- security investigations
- audit correlation
- active incident response

Align retention with Audit module policy.

---

## 10) Security integration notes

- Never store or log raw tokens
- Audit events should contain identifiers and action metadata only
- Avoid leaking sensitive PII into logs/events
- Consider stronger secret handling / encryption in V2 (for MFA or future factors)

---

## 11) V2 hooks (upgrade path)

Possible future extensions:
- `ExternalIdentity`
- `UserSession`
- `MfaFactor`
- `PasswordHistory`
- `AccountLockout`
- `RiskFlag`

These should be introduced only when:
- policy is defined
- ownership remains explicit
- replay / audit / security semantics are documented

---

## 12) ERD (dbdiagram.io)

See: `../diagrams/erd/identity-v1.dbml`

How to render:
1. Open dbdiagram.io
2. Copy DBML content from the file above
3. Paste into dbdiagram.io to view or export