# Identity — Open Questions & ADR Hooks (V1)

This document tracks unresolved Identity design decisions, policy hooks, and future ADR candidates.

Identity V1 already follows the baseline posture:

- Identity owns account/session/security truth.
- Authorization owns role/permission governance.
- Notifications and Audit are asynchronous downstream consumers.
- Identity mutations that require side effects commit truth and outbox intent atomically.
- Identity Admin manages Identity-owned truth only.

---

## ADR-01: Verification gating rules

Questions:

- What actions require verified email in V1?
- Do we allow login before verification?
- If login before verification is allowed, what is restricted?
- Can unverified users refresh tokens?
- Can unverified users access public profile/self-service endpoints?
- Should admin-created or admin-verified accounts bypass normal verification gating?

Current posture:

- Verification truth is owned by Identity.
- Email delivery state does not define verification truth.
- Admin verification override is possible only through explicit policy.

---

## ADR-02: Refresh token reuse detection response

Questions:

- If a revoked/rotated refresh token is presented again, do we revoke only the token family or all active tokens for the user?
- Do we force re-login immediately?
- Do we notify the user?
- Do we emit a dedicated security event?
- Should suspected reuse trigger admin/security dashboard alerts?
- Should reuse detection lock the account or only revoke sessions?

Current posture:

- Refresh-token truth is owned by Identity.
- Reuse detection must be truth-first.
- Notification/Audit side effects must be asynchronous.

---

## ADR-03: Logout semantics

Questions:

- Support `mode = Current` only or also `mode = All`?
- If `mode = All` is supported, does it revoke all active refresh tokens or only the current token family?
- Should logout emit an audit/security event?
- Should logout-all notify the user?
- Should admin session revocation share the same internal revocation logic as logout-all?

Current posture:

- Logout success is based on refresh-token revocation truth.
- Downstream audit visibility does not define logout success.

---

## ADR-04: Register conflict behavior

Questions:

- Should register return anti-enumeration-safe response when email already exists?
- Or should register return explicit `409 IDENTITY.EMAIL_EXISTS`?
- Should behavior differ between public registration and admin-created accounts if admin creation is introduced later?
- How should idempotent retry with the same `Idempotency-Key` behave when the account already exists?

Current posture:

- Forgot-password and resend-verification are always anti-enumeration-safe.
- Register conflict behavior is still policy-defined.

---

## ADR-05: Lockout policy

Questions:

- Do we lock accounts on repeated login failures?
- What threshold triggers lockout?
- How long is the lockout?
- Is lockout temporary or admin-cleared?
- Does lockout revoke refresh tokens?
- Does lockout require schema support such as `LockedUntil`?
- Should lockout emit `identity.admin.user_locked` or a separate system/security event?
- Should lockout be visible in admin security summary?

Current posture:

- Lock/unlock APIs are conditional in V1 until Identity schema supports lock state.
- Locked users must not sign in where lock state is implemented.

---

## ADR-06: Identity Admin protected account policy

Questions:

- Which accounts are protected system accounts?
- Is the default admin account protected by email, user id, role, configuration, or another marker?
- Which operations are blocked for protected accounts?
  - deactivate
  - lock
  - revoke sessions
  - mark email verified
  - profile/security mutation
- Can protected account restrictions be bypassed by break-glass operation?
- If break-glass exists, how is it audited and approved?

Current posture:

- Identity Admin must not deactivate or lock protected system accounts through normal admin APIs.
- Identity should not query or mutate Authorization truth inside its transaction to determine role ownership.

---

## ADR-07: Identity Admin self-action policy

Questions:

- Can an admin deactivate themselves?
- Can an admin lock themselves?
- Can an admin revoke their own current session from the admin endpoint?
- Should self-revoke be allowed only through normal logout?
- Are there special rules for revoking all sessions except the current one?
- Should self-action denial return `403 IDENTITY.ADMIN.SELF_ACTION_DENIED`?

Current posture:

- Normal admin APIs should block dangerous self-actions.
- Self-service logout/change-password flows remain separate from admin flows.

---

## ADR-08: Last-admin protection

Questions:

- Should the system prevent deactivating/locking the last effective admin?
- Since Authorization owns roles and permissions, where should the last-admin invariant be enforced?
- Should this be enforced by Authorization policy/ABAC before Identity mutation?
- Should Identity only protect configured system accounts in V1?
- Should a future cross-module governance guard be introduced?

Current posture:

- Identity does not own role/permission truth.
- V1 can protect configured system accounts without cross-module transactional ownership.
- Strong last-admin protection likely requires Authorization-owned policy or a dedicated ADR.

---

## ADR-09: Admin email verification override policy

Questions:

- When is admin `mark-email-verified` allowed?
- Is a reason required?
- Should this require higher permission than normal status management?
- Should this require MFA or step-up authorization in the future?
- Should active verification tokens be revoked/obsolete after admin verification?
- Should the user be notified when an admin marks their email as verified?

Current posture:

- Admin email verification override is high-risk.
- It emits `identity.admin.email_marked_verified`.
- It must not expose raw verification token or token hash.

---

## ADR-10: Admin session revocation semantics

Questions:

- Does admin `revoke-sessions` always revoke all active refresh tokens?
- Should V1 support revoking one specific session?
- If revoking one session, what public-safe session identifier should be exposed?
- Should admin session revocation notify the user?
- Should revoking sessions also invalidate access tokens immediately, or only prevent refresh?
- Should there be a grace period?

Current posture:

- Refresh-token revocation truth is owned by Identity.
- Revoked refresh tokens must stop being valid immediately from Identity truth.
- Access-token invalidation strategy remains policy-defined.

---

## ADR-11: Raw token handling in delivery-trigger events

Questions:

- Should `identity.verification_email_requested` continue to carry `RawVerificationToken`?
- Should `identity.password_reset_requested` continue to carry `RawResetToken`?
- Should tokens be carried as raw token, prebuilt link, encrypted payload, or secure reference?
- Should Notifications persist raw token material in delivery workflow state?
- If persisted, should it be encrypted and retained only for a short TTL?
- How do we prevent raw token leakage in logs, traces, metrics, dashboards, support tools, and Audit payloads?

Current posture:

- Identity stores only token hashes.
- Approved Notifications delivery-trigger events may carry raw one-time token material.
- Lifecycle, admin, audit-oriented, logging, metrics, tracing, error, and reporting payloads must never contain raw tokens or token hashes.

---

## ADR-12: Notification delivery dedupe policy

Questions:

- What is the exact `BusinessDedupeKey` format for verification email?
- What is the exact `BusinessDedupeKey` format for password reset email?
- Does resend-verification always create a new allowed business intent?
- Does forgot-password always create a new allowed business intent, or suppress repeated requests within a window?
- When is explicit resend allowed?
- How do we distinguish duplicate retry from legitimate new intent?

Current posture:

- Notifications must dedupe by `MessageId`.
- Notifications must also use business-level idempotency where duplicate user-visible delivery is harmful.
- Resend-verification is a new logical intent when policy allows.

---

## ADR-13: Admin command idempotency

Questions:

- Should Identity Admin mutations support `Idempotency-Key`?
- Or is convergent state-setting behavior sufficient for V1?
- Should repeated no-op admin commands emit attempt audit events?
- Should repeated no-op admin commands suppress duplicate state-change events?
- Should the API return the original result for repeated identical admin command attempts?

Current posture:

- Admin state-setting commands are convergent by default.
- No-op convergence should not emit duplicate state-change events unless audit policy explicitly requires attempt logging.

---

## ADR-14: Identity Admin event payload contract

Questions:

- Should admin events include `RequiredPermission`?
- Should admin events include previous/new state snapshots?
- Should admin events include `Reason` as required field?
- Should admin events include actor IP/UserAgent or keep those only in headers/audit context?
- Should admin event payloads include public ids, internal ids, or both?
- Should admin events carry aggregate version for ordering-sensitive projections?

Current posture:

- Admin events must be minimal, sanitized, and audit-oriented.
- Admin events must never contain raw tokens, token hashes, password hashes, or unnecessary PII.
- Audit consumes admin events idempotently.

---

## ADR-15: Login history ownership and write model

Questions:

- Is `LoginHistory` owned by Identity in V1?
- Is login history written synchronously or asynchronously?
- If async, which event triggers it?
- Is failed login history stored?
- How much IP/UserAgent data should be retained?
- What is the retention period?
- Is login history used for lockout/risk decisions or only investigation?

Current posture:

- Login history is recommended but optional in V1.
- It must remain privacy-aware.
- It must not define authentication success unless explicitly promoted to Identity truth.

---

## ADR-16: Admin security summary source

Questions:

- Which fields are returned by `GET /api/v1/admin/identity/users/{userId}/security-summary` in V1?
- Are `lastLoginAtUtc`, active session count, lock state, and risk flags truth-backed or derived?
- If optional data is unavailable, should the endpoint return partial summary or omit fields?
- Should security summary read from Identity truth only, or include downstream projections?

Current posture:

- V1 may return partial security summary.
- Security-sensitive state must be truth-backed.
- Derived summaries must not misrepresent live security truth.

---

## ADR-17: Access-token invalidation strategy

Questions:

- When account is deactivated, locked, password-reset, or sessions are revoked, what happens to existing access tokens?
- Do we rely on short access-token TTL?
- Do we introduce token version/security stamp?
- Do we check account status on every protected request?
- Should admin deactivation force immediate access-token rejection?

Current posture:

- Refresh-token revocation is immediate from Identity truth.
- Access-token invalidation strategy remains policy-defined.
- Protected endpoints should not allow stale security assumptions to persist where correctness matters.

---

## ADR-18: Admin operation notification policy

Questions:

- Which admin actions notify the user?
  - deactivate
  - activate
  - lock
  - unlock
  - mark email verified
  - revoke sessions
- Which notifications are security-critical versus optional?
- Should notification failure ever affect admin command success?
- Should notification content include reason?
- How do we prevent duplicate notification delivery?

Current posture:

- Admin notifications, if enabled, are downstream side effects.
- Admin command success does not depend on notification delivery.
- Notifications must use message-level and business-level idempotency.

---

## ADR-19: Admin audit attempt policy

Questions:

- Should denied admin attempts be audited?
- Should no-op convergent admin attempts be audited?
- Should repeated attempts against protected accounts be audited separately from successful mutations?
- Should failed validation attempts be audited?
- Which audit events are emitted from Identity truth mutations versus API/security middleware?

Current posture:

- Successful admin mutations emit audit-oriented outbox events.
- Audit consumes events asynchronously and idempotently.
- Attempt-level auditing may require separate policy.

---

## ADR-20: Identity Admin lock/unlock schema support

Questions:

- Does V1 schema include `LockedUntil`, `LockReason`, `LockedByUserId`, or equivalent fields?
- Is lock represented by `Status = Locked` or separate lock fields?
- Does unlock clear failed login count?
- Does lock revoke sessions?
- Does unlock restore previous status?
- Should lock/unlock remain out of scope until schema support exists?

Current posture:

- Lock/unlock flows are conditional in V1.
- If schema does not support lock state, endpoints must remain unavailable or return deterministic unsupported-operation behavior.