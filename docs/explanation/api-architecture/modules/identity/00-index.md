# Identity Module — API Architecture (V1)

**Purpose**
- Provide secure authentication and account lifecycle flows:
  - register/login/refresh/logout
  - email verification (resend + verify)
  - forgot/reset password
  - profile updates + change password
- Keep **identity and session truth** explicit and authoritative.
- Support async email/audit side effects, login-history pipelines, and bounded replay/reconciliation workflows without turning those outputs into hidden security truth.

**Why this module is critical**
- Identity flows are common attack targets (brute force, credential stuffing, token theft).
- Identity must remain stable under burst traffic (major events, bot spikes).
- Sensitive data must never leak (tokens, PII).
- Security-sensitive state must remain truth-first even when notifications, audit, or caches lag.
- Derived operational outputs may exist, but they must never become authority for auth decisions.
- At-least-once async delivery, timeout ambiguity, and replay are normal operational conditions and must not weaken security truth.

**Primary consumers**
- Public clients (web/mobile)
- Admin UI (same auth surface, stricter permissions in Authorization)
- Worker (async consumers for emails/audit/login history)
- Future reporting/reconciliation workflows for token/session or notification pipelines

**Non-goals (V1)**
- Social login / federated SSO (OIDC federation)
- MFA
- Device/session UI management
- Advanced account recovery (support-based workflows)
- Treating notification status, audit trails, or caches as security truth
- Global coordination-heavy auth infrastructure

**Hard constraints**
- Anti-enumeration for resend/forgot flows.
- Email delivery must not block register/reset flows (async notifications).
- Refresh tokens are opaque and never logged; rotation policy is required.
- Identity truth is **primary and authoritative** for verification state, password state, token validity, and revocation state.
- Security-sensitive reads must support **read-your-writes** after successful truth changes.
- Derived outputs must remain **observable**, **rebuildable where applicable**, and **subordinate to Identity truth**.
- Replay/reconciliation workflows must be **bounded**, **observable**, and **rerun-safe**.
- Partial or candidate maintenance/reporting outputs must not be treated as final security truth.
- No raw secrets, tokens, or sensitive identifiers may appear in logs, events, or derived outputs beyond approved safe forms.

**Truth vs derived posture**
- **Truth**:
  - user account state
  - verification/reset token lifecycle
  - password/security-state changes
  - refresh token family truth
  - revocation/replacement state
  - account enable/disable truth
- **Derived**:
  - email delivery state
  - audit persistence
  - login-history summaries or materializations
  - rate-limit counters and operational caches
  - reporting/reconciliation outputs for session/security pipelines
- **Rule**: auth decisions come only from primary Identity truth; derived outputs may lag and be rebuilt, but they do not become security authority.

**Batch / replay / reconciliation posture**
- Identity may participate in bounded workflows for:
  - login-history rebuild or reconciliation
  - notification pipeline replay/reporting
  - token/session anomaly reporting
  - cleanup of expired token/request artifacts by policy
  - bounded reconciliation between durable identity truth and derived operational outputs
- These workflows must:
  - start from authoritative Identity truth or approved durable evidence
  - remain rerun-safe
  - never weaken current token/session/account security state
  - validate important candidate outputs before publication/cutover where correctness matters

**Primary security posture**
- Server truth beats client belief.
- Caches may accelerate low-risk operational behavior, but never decide token validity or account security truth.
- Timeout, missing email, or delayed audit entry do not prove auth truth failed.
- If security-sensitive state is uncertain, prefer truth-backed evaluation over stale confidence.
- Cause must be durable before effect becomes meaningful:
  - verification/reset/session truth commits first
  - notifications/audit follow later

**Key links**
- System-wide rules:
  - `../../01-api-architecture-charter-v1.md`
  - `../../02-contracts-and-standards.md`
  - `../../04-versioning-and-compatibility.md`
  - `../../08-authentication-and-authorization.md`
  - `../../09-observability-and-slos.md`
- Arc42 runtime scenarios:
  - `../../../architecture/arc42/04-runtime-view-v1.md` (Scenario 4–5)
  - `../../../architecture/arc42/13-transactions-and-consistency-v1.md`
  - `../../../architecture/arc42/18-stream-processing-and-derived-state-v1.md`
  - `../../../architecture/arc42/19-stream-processing-runtime-v1.md`
  - `../../../architecture/arc42/16-batch-processing-and-derived-data-v1.md`
  - `../../../architecture/arc42/17-dataflow-and-batch-workflows-v1.md`
- System data model:
  - `../../../architecture/arc42/system-data/system-data-identity-v1.md` (if present)
- Quality profile:
  - `../../../architecture/arc42/quality/identity.md` (if present)

**ADR hooks**
- Verification gating rules (what is allowed before verification)
- Refresh token strategy (rotation + reuse detection response)
- Logout semantics (revoke current vs revoke all)
- Cleanup/retention policy for expired verification/reset/session artifacts
- Replay/reconciliation policy for login history and identity-derived operational outputs
- Whether reset/change-password revokes current session only or all sessions