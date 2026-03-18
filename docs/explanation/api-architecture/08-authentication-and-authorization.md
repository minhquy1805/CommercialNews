# Authentication & Authorization — CommercialNews (V1)

This document applies Chapter 7 (OAuth2/OIDC, refresh tokens, scopes, enforcement) from *Mastering API Architecture* to CommercialNews.
It defines the **security baseline** for API access: who is calling, how they prove identity, and what they are allowed to do.

Related:
- Threat modeling: `07-security-threat-modeling.md`
- Contracts/standards: `02-contracts-and-standards.md`
- Versioning/compatibility: `04-versioning-and-compatibility.md`
- Arc42 constraints: `../architecture/arc42/02-constraints.md`
- Arc42 governance: `../architecture/arc42/07-architecture-governance.md`

---

## 1) Definitions (avoid common confusion)

### Authentication (AuthN)
Answers: **Who is the caller?**
- End user (browser/mobile)
- Admin user (author/moderator/admin)
- Service/job (worker, scheduled job)

### Authorization (AuthZ)
Answers: **What is the caller allowed to do?**
- Function-level: can the caller perform this action?
- Object-level (BOLA): can the caller perform it on this specific resource?

**Rule:** AuthN is not enough. CommercialNews must enforce AuthZ systematically.

---

## 2) Caller types (V1)

CommercialNews distinguishes these caller identities:

1) **Public anonymous user**
- Can read public content only.

2) **Authenticated end user**
- Can read; may interact (like/comment) depending on verification policy.

3) **Admin/Staff user**
- Can access `/api/v1/admin/*` endpoints based on roles/permissions.

4) **Internal service/job identity (Worker)**
- Consumes events and performs side effects (audit ingestion, email sending, aggregation).

**Rule:** a token does not necessarily represent a human user. It may represent a service identity.

---

## 3) Token strategy (V1 baseline)

### 3.1 Access token (JWT)
- Access tokens are short-lived JWTs.
- Resource servers validate locally.

Minimum validation:
- signature
- `iss` (issuer)
- `aud` (audience)
- `exp` (expiration)
- `nbf` (optional)
- algorithm allowlist

**Rule:** do not put sensitive PII into JWT claims. Keep tokens minimal.

### 3.2 Refresh token (opaque)
- Refresh tokens are opaque strings stored server-side.
- They are long-lived and higher risk.

Required storage metadata (policy-level):
- UserId
- CreatedAt, ExpiryDate
- RevokedAt?, ReplacedByToken?
- IPAddress?, UserAgent? (if collected)
- Token family or chain markers (if implementing reuse detection)

**Rule:** refresh tokens must never be logged.

---

## 4) Refresh token policy (rotation, reuse detection, revocation)

### 4.1 Rotation (recommended default)
- Every refresh call rotates the refresh token.
- The old refresh token becomes revoked and points to the new token.

### 4.2 Reuse detection (policy decision required)
If a revoked/rotated token is presented again, it indicates possible token theft.

Recommended response:
- revoke the token family (or all refresh tokens for that user)
- require re-authentication
- emit a security/audit event
- record anomaly signals

> ADR hook: “Refresh token strategy: rotation + reuse detection + revocation semantics”.

### 4.3 Revocation triggers (V1 baseline)
At minimum, revoke refresh tokens when:
- user logs out (current token or all tokens by mode)
- password is changed or reset
- account is disabled/locked
- reuse detection triggers

---

## 5) Verification gating (Identity policy)

CommercialNews requires explicit rules for what is allowed before email verification.

Recommended V1 posture:
- Allowed before verification:
  - register, resend verification, verify email
  - forgot/reset password
  - login (optional; if allowed, restrict sensitive actions)
- Restricted until verified:
  - admin access (always)
  - interaction actions (like/comment) (optional V1; recommended for V2 or policy-controlled)

> ADR hook: “Verification gating rules”.

---

## 6) Scopes (coarse-grained) vs permissions (fine-grained)

### 6.1 Scope is coarse-grained
Scopes are useful for:
- coarse gating at the edge or API middleware
- separating broad domain access (read vs write)

Scopes are **not** a replacement for permissions or object-level authorization.

### 6.2 Permission/policy is fine-grained (CommercialNews primary)
CommercialNews uses centralized policy enforcement:
- roles/permissions (and potentially ABAC contexts)
- policy checks for every admin endpoint
- object-level checks for resource access

**Rule:** do not rely on scopes alone for admin/governance.

---

## 7) Permission model baseline (V1)

### 7.1 Admin endpoints
All `/api/v1/admin/*` endpoints must enforce:
- explicit policy checks (deny-by-default)
- audit event emission for governance actions (async)

Examples (illustrative):
- `content:write`
- `content:publish`
- `seo:write`
- `media:write`
- `comment:moderate`
- `authz:manage`
- `audit:read`

> Permission naming/versioning is an ADR-level concern (keep consistent and evolvable).

### 7.2 Object-level authorization (BOLA)
Required checks include:
- author editing their own drafts (if supported)
- deleting/editing own comments (public)
- setting primary media for an article (admin)
- reading unpublished content (should be forbidden publicly)

**Rule:** never trust client-supplied IDs without object-level checks.

---

## 8) Enforcement locations (where checks must happen)

### 8.1 Edge/gateway (baseline)
Allowed:
- coarse rate limits
- TLS termination
- basic WAF (if available)
- coarse scope checks (optional)

Not sufficient:
- cannot replace service-level authorization

### 8.2 API component (non-negotiable)
Must enforce:
- authn validation of access tokens
- permission/policy checks for admin endpoints
- object-level checks where needed
- safe error responses (no data leaks)

### 8.3 Worker component
Must enforce:
- service identity verification to consume messages (broker auth)
- idempotency and safe retries
- safe logging and redaction

---

## 9) Abuse controls (mandatory for V1)

### 9.1 Rate limiting required
At minimum, rate limit:
- register/login/refresh
- resend verification
- forgot/reset password
- comments endpoints (anti-spam hooks)

### 9.2 Anti-enumeration required
- forgot password and resend verification must not reveal account existence
- responses must be generic (`{ accepted: true }`)

### 9.3 Safe logging required
- never log: passwords, refresh tokens, verification/reset tokens
- redact sensitive PII in logs and audit payloads
- prefer “minimum necessary” data in events

---

## 10) Operational signals (security telemetry)

Security is validated by signals, not assumptions.

Track at least:
- login failure spikes
- rate-limit triggers on sensitive endpoints
- suspicious repeated resend/forgot requests
- refresh reuse detection events (if implemented)
- 401/403 spikes (potential misconfig or attack)
- admin policy denial rates (unexpected spikes indicate drift)

These map to arc42 measurement guide:
- `../architecture/arc42/06-measurement-guide.md`

---

## 11) Practical rules (common pitfalls to avoid)

- Do not use ID tokens to call APIs (OIDC vs OAuth separation).
- Do not store refresh tokens in insecure client storage (avoid XSS exposure patterns).
- Do not embed sensitive PII into JWT claims.
- Do not let “scope checks” replace policy checks.
- Do not scatter ad-hoc authorization checks across modules; keep it centralized and auditable.

---

## 12) ADR hooks (decisions we must capture)

Create ADRs for:
- refresh token rotation + reuse detection response
- verification gating rules (what actions require verified email)
- permission naming/versioning strategy
- whether/when to introduce OIDC or third-party login (future)