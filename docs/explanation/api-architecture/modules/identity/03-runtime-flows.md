# Identity — Runtime Flows (V1)

This module supports arc42 runtime scenarios and Identity admin runtime operations:

- Scenario 4: registration + verification
- Scenario 5: forgot + reset password
- Identity Admin account/session/security operations

Related:

- `../../../../architecture/arc42/04-runtime-view-v1.md`
- `../../../../architecture/arc42/13-transactions-and-consistency-v1.md`
- `../../../../architecture/arc42/16-batch-processing-and-derived-data-v1.md`
- `../../../../architecture/arc42/17-dataflow-and-batch-workflows-v1.md`
- `../../../../architecture/arc42/18-stream-processing-and-derived-state-v1.md`
- `../../../../architecture/arc42/19-stream-processing-runtime-v1.md`
- `../../../../decisions/adr-0013-outbox-and-delivery-semantics-v1.md`
- `../../../../decisions/adr-0016-authorization-model-rbac-abac-policies-v1.md`
- `../../../../decisions/adr-0018-transaction-boundaries-and-consistency-model-v1.md`
- `../../../../decisions/adr-0020-timeout-retry-and-failure-detection-policy-v1.md`
- `../../../../decisions/adr-0027-stream-processing-and-derived-state-policy-v1.md`
- `../../../../decisions/adr-0028-consumer-idempotency-replay-and-rebuild-policy-v1.md`

---

## Runtime posture in V1

Identity primarily participates in three runtime lanes:

### A) Synchronous truth lane

Used for:

- register
- verify email
- login
- refresh token rotation
- forgot/reset password truth handling
- password change
- token revoke / reuse-detection response
- admin account status changes
- admin email verification override
- admin refresh-token/session revocation
- admin lock/unlock where schema support exists

### B) Async side-effect lane

Used for:

- verification email delivery triggers
- password-reset email delivery triggers
- audit side effects
- optional security notification signals
- optional login-history style derived consumers
- admin account/security audit events
- optional admin security notification signals
- optional cache invalidation for account/session/security changes

### C) Batch / cleanup / maintenance lane

Used for:

- cleanup expired verification/reset tokens
- cleanup revoked/expired refresh tokens
- retention cleanup for auth/session artifacts
- optional archival / summarization for login history or auth maintenance outputs
- reconciliation/reporting for stuck or orphan auth artifacts
- reconciliation between Identity truth and admin/security derived views
- investigation summaries for account/session/security operations

### Core runtime rules

**Rule:** Identity owns account and security truth.  
Cleanup, reporting, and maintenance workflows may lag, but they do not redefine current security truth.

**Rule:** Identity success is defined by **truth commit**.  
When async side effects are required, success means:
- truth committed
- async intent/outbox committed

It does **not** mean:
- broker publish already happened
- Notifications already sent mail
- Audit already persisted
- downstream consumers are already caught up

**Rule:** Identity-derived async processing is assumed **at-least-once**.  
Duplicates, replay, lag, and worker restart must be tolerated safely.

**Rule:** Identity resolves security ambiguity from authoritative truth, not from delivery state, timeout guesswork, or client belief.

**Rule:** Identity Admin operations are policy-protected truth commands.
Every `/api/v1/admin/identity/*` endpoint must enforce an explicit permission policy before mutating Identity truth.

**Rule:** Identity Admin mutations write audit-oriented outbox events.
Audit consumes these events asynchronously and idempotently. Audit visibility does not define whether the Identity mutation succeeded.

**Rule:** Identity Admin transactions remain inside the Identity truth boundary.
Admin commands must not update Authorization, Audit, Notifications, Redis, or derived read-model truth in the same transaction.

### Event type payload rules

Delivery-trigger events:

- `identity.verification_email_requested`
- `identity.password_reset_requested`

Lifecycle/audit-oriented events include:

- `identity.user_registered`
- `identity.user_logged_in`
- `identity.email_verified`
- `identity.password_changed`
- `identity.admin.user_activated`
- `identity.admin.user_deactivated`
- `identity.admin.email_marked_verified`
- `identity.admin.user_sessions_revoked`
- `identity.admin.user_locked`
- `identity.admin.user_unlocked`

Delivery-trigger events may carry raw one-time tokens when required by Notifications.

Delivery-trigger events must be routed only to approved consumers such as Notifications. Audit consumers must not persist raw delivery-token fields.

Lifecycle/audit-oriented events must never carry raw tokens or token hashes.

Secret-bearing delivery-trigger messages must not be:

- logged as raw payload JSON
- attached to exception logs, traces, metrics, or error diagnostics
- copied into `Audit.Data`
- persisted into `EmailDelivery.Data` unless explicitly required and protected by policy
- exposed through admin dashboards, support tools, or operational message viewers

---

## Flow A — Register → Verification email trigger → Verify

### Goal

Create account truth synchronously, then trigger verification delivery asynchronously without blocking registration success.

### Upstream truth boundary

1. Client calls `POST /api/v1/auth/register`.
2. Identity validates request and policy.
3. Identity creates user in `Unverified` state.
4. Identity creates verification intent/token according to policy.
5. Identity writes async intent/outbox in the same truth transaction.

### Async events emitted

Identity may emit:

- `identity.user_registered`
  - business/audit timeline event
- `identity.verification_email_requested`
  - canonical delivery-trigger event for Notifications

### Required event semantics

- each emitted async message carries a stable `MessageId`
- payload must be minimal and privacy-aware
- general lifecycle/audit events must not include raw verification tokens
- `identity.verification_email_requested` is a delivery-trigger event and may include `RawVerificationToken` because Notifications needs it to render the verification link
- `RawVerificationToken` is a secret-bearing payload field and must be redacted from logs, errors, traces, metrics, audit payloads, and diagnostics
- the database stores only `TokenHash`; the raw token is delivery material, not Identity persisted truth

### Downstream flow

6. Notifications consumes `identity.verification_email_requested`.
7. Notifications creates or loads delivery workflow state.
8. Notifications attempts provider send using its own delivery-state rules.
9. Client later calls `POST /api/v1/auth/verify-email` with token.
10. Identity validates token against authoritative token/account truth.
11. Identity marks user verified/active.
12. Identity emits `identity.email_verified`.

### Runtime stream semantics

- verification email delivery is a **downstream effect**, not part of registration truth
- duplicate delivery-trigger message handling is normal and must be safe
- replay of verification-delivery events is expected under at-least-once delivery
- verification truth is determined by Identity token/account state, not by provider status
- a delivered email does not prove verification happened
- a missing/delayed email does not invalidate already-committed registration truth

### Failure modes

- notification provider down:
  - register still succeeds
  - delivery may retry/remediate later
- duplicate delivery-trigger publish:
  - downstream dedupe/business-intent safeguards must converge safely
- token expired:
  - client uses resend-verification flow according to policy
- timeout during verify:
  - reconcile from Identity truth, not from delivery state
- duplicate verify attempts:
  - converge safely to verified state or documented no-op/conflict behavior
- replay of stale verification-delivery events:
  - must not change already-verified truth

### Batch / cleanup hooks

- expired verification tokens may later be cleaned up by bounded maintenance workflows
- stale/orphan verification intents may be reconciled or reported
- notification delivery artifacts remain derived and downstream

### Runtime rules

- account creation and verified-state change are truth-bound operations
- email may lag, but account/security truth remains authoritative
- read-your-writes applies after successful verify
- downstream lag must not weaken current verification truth

---

## Flow B — Login → Refresh rotation

### Goal

Issue and rotate session credentials safely with deterministic token-family behavior.

### Truth flow

1. Client calls `POST /api/v1/auth/login`.
2. Identity validates credentials and policy.
3. Identity returns access token + refresh token according to policy.
4. Refresh-token truth is persisted server-side.
5. Client later calls `POST /api/v1/auth/refresh`.
6. Identity validates current refresh token against authoritative truth.
7. Identity revokes old token, issues new refresh token, and returns new token pair.
8. Identity persists replacement relation / token-family linkage if modeled.

### Optional async events

Identity may emit optional operational/audit events such as:

- `identity.user_logged_in`

These are derived side effects and do not define session validity.

### Runtime stream semantics

- refresh rotation is a truth-first security transition
- old/new token-family state must converge deterministically
- delayed audit/notification signals do not define session validity
- replayed refresh or reused old token must be evaluated against current token-family truth
- timeout ambiguity must be resolved from refresh-token truth, not from client belief

### Failure modes

- reuse detection:
  - revoked token used again → apply configured policy
- timeout during refresh:
  - reconcile from refresh-token truth
- duplicate refresh attempt:
  - stale or reused token must not mint multiple valid successor states incorrectly
- stale client retry after successful rotation:
  - must not resurrect old token authority

### Batch / cleanup hooks

- revoked/expired refresh tokens may be cleaned up later
- token-family drift or suspicious reuse may be summarized in derived reports
- cleanup must not weaken current token-validity truth

### Runtime rules

- refresh is security-critical and truth-backed
- old token authority ends at the truth boundary, not when downstream effects catch up
- read-your-writes applies for immediate post-refresh session validity checks

---

## Flow C — Forgot → Reset

### Goal

Allow secure password reset without leaking account existence and without coupling truth success to delivery completion.

### Truth flow

1. Client calls `POST /api/v1/auth/forgot-password`.
2. Identity applies anti-enumeration response policy.
3. If account exists and policy allows, Identity creates password-reset intent/token.
4. Identity writes async intent/outbox in the same truth transaction.

### Async event emitted

Identity emits:

- `identity.password_reset_requested`

### Required event semantics

- message carries stable `MessageId`
- payload must be minimal and privacy-aware
- general lifecycle/audit events must not include raw reset tokens
- `identity.password_reset_requested` is a delivery-trigger event and may include `RawResetToken` because Notifications needs it to render the reset link
- `RawResetToken` is a secret-bearing payload field and must be redacted from logs, errors, traces, metrics, audit payloads, and diagnostics
- the database stores only `TokenHash`; the raw token is delivery material, not Identity persisted truth

### Downstream flow

5. Notifications consumes `identity.password_reset_requested`.
6. Notifications handles delivery workflow asynchronously.
7. Client calls `POST /api/v1/auth/reset-password` with token and new password.
8. Identity validates token against authoritative truth.
9. Identity updates password.
10. Identity revokes relevant refresh tokens by policy.
11. Identity emits `identity.password_changed`.

### Runtime stream semantics

- forgot-password acceptance and reset email delivery are decoupled from reset truth
- duplicate reset-delivery processing must not create duplicate harmful outward effects for the same protected reset intent
- password/security truth is determined by Identity, not by email timing or provider outcome
- replay of old reset-related events must not override fresher token/session truth

### Non-blocking rule

The following must not block on notification delivery:

- register
- forgot-password
- resend-verification

### Failure modes

- duplicate forgot requests:
  - anti-enumeration still holds
- token expired or already consumed:
  - deterministic truth-backed failure
- timeout during reset:
  - reconcile from Identity truth, not email provider outcome
- duplicate reset attempt with same token:
  - must not produce contradictory password truth
- stale refresh/session state:
  - must not survive policy-defined revocation after reset
- replay of stale reset-delivery event:
  - must not cause a newer valid reset intent to be weakened or overridden

### Batch / cleanup hooks

- expired reset tokens may later be cleaned up
- stale reset intents may be reported for maintenance
- delivery artifacts remain derived and downstream

### Runtime rules

- reset consumes truth-owned token lifecycle
- password change and session revocation are truth-first
- downstream lag must not weaken reset security effect

---

## Flow D — Resend verification / auth maintenance trigger

### Goal

Create a valid **new** verification intent safely without being blocked by old delivery artifacts or stale dedupe state.

### Truth flow

1. Client calls `POST /api/v1/auth/resend-verification`.
2. Identity enforces anti-abuse / rate-limit policy.
3. Identity determines whether resend is allowed under current verification truth.
4. Identity creates a **new logical verification intent/token** if policy allows.
5. Identity writes a new async intent/outbox record.

### Async event emitted

Identity emits a new delivery-trigger event:

- `identity.verification_email_requested`

### Runtime semantics

- resend is a **new logical intent**, not replay of the old one
- new resend must create:
  - a new protected business intent
  - a new async `MessageId`
- old delivery dedupe state must not suppress a valid new verification intent
- replay of the same resend message must not multiply harmful delivery effects
- downstream dedupe must treat the valid new resend as a new allowed intent, not as replay of the old intent

### Failure modes

- rate-limit or abuse control rejects resend
- old expired token remains in storage but does not define current verification truth
- notification lag delays convenience, not account truth
- retry ambiguity must reconcile from current verification-intent truth

### Runtime rules

- resend validity is determined by Identity truth and policy
- notification completion does not define whether resend truth/intention was created
- old expired intents may remain for cleanup until retention policy removes them

---

## Flow E — Cleanup / retention workflow

### Goal

Control growth of auth/security artifacts without changing current truth incorrectly.

### Typical workflow shape

1. Select bounded expired or terminal auth artifacts such as:
   - verification tokens
   - reset tokens
   - revoked/expired refresh tokens
2. Apply cleanup/archival policy.
3. Record cleanup outcome.

### Rules

- cleanup is bounded
- cleanup must not remove artifacts still required for:
  - current security truth
  - investigation policy
  - compliance/retention policy
- cleanup is maintenance, not a replacement for synchronous validity checks
- rerun on the same bounded scope must remain safe
- if cleanup uncertainty exists, preserving live security truth wins over aggressive removal

### Typical derived outputs

- cleanup candidate sets
- expired-token summaries
- suspicious token-family reports
- maintenance backlog indicators

---

## Flow F — Truth-safe security under derived lag

### Goal

Ensure security-sensitive behavior remains correct even when email, audit, or maintenance workflows lag.

### Typical runtime shape

1. A security-sensitive request arrives, such as:
   - verify email
   - reset password
   - refresh token
   - self-state read after security change
2. Identity reads authoritative account/token truth.
3. Derived systems may still lag:
   - email delivery
   - audit persistence
   - cleanup/reporting views
4. Identity returns outcome based on truth-backed security state only.

### Examples

- verification email delivered late but account already verified
- password reset succeeded but audit not yet visible
- refresh rotation committed but cleanup job has not removed old revoked tokens yet
- missing or delayed email does not imply token/account truth failed
- timeout does not imply side effect did not happen or truth did not commit

### Rules

- Identity truth wins over all derived operational views
- timeout ambiguity must be resolved from truth
- safe deny/reject beats stale or guessed security behavior
- downstream delivery/reporting lag must not weaken security correctness

---

## Flow G — Admin activates or deactivates user

### Goal

Change account access state immediately from Identity truth and emit an auditable admin security event without waiting for downstream consumers.

### Truth flow

1. Admin calls one of:
   - `POST /api/v1/admin/identity/users/{userId}:activate`
   - `POST /api/v1/admin/identity/users/{userId}:deactivate`
2. API enforces `Permission:Identity.User.ManageStatus`.
3. ABAC/context rules are evaluated:
   - actor cannot deactivate themselves through normal admin APIs
   - protected system accounts cannot be deactivated through normal admin APIs
4. Identity loads target `UserAccount`.
5. Identity applies the account status transition.
6. Identity revokes active refresh tokens when policy or request requires it.
7. Identity writes the corresponding outbox event in the same truth transaction.
8. Transaction commits.
9. API returns committed Identity result.

### Async events emitted

Identity emits one of:

- `identity.admin.user_activated`
- `identity.admin.user_deactivated`

### Event semantics

- carries stable `MessageId`
- includes target user id, actor user id, previous/new status, reason, and correlation id where available
- must not contain password hashes, raw tokens, token hashes, or unnecessary PII
- safe under duplicate delivery, retry, and replay

### Downstream flow

10. Outbox worker publishes the event to RabbitMQ.
11. Audit consumes the event idempotently.
12. Notifications may consume selected events if user-facing security notification is enabled.
13. Cache/projection invalidation may happen asynchronously.

### Failure modes

- audit ingestion delayed:
  - account status truth still applies immediately
- notification delivery failed:
  - account status truth still applies immediately
- RabbitMQ publish timeout:
  - outbox retry handles producer-side ambiguity
- client timeout:
  - admin reconciles by reading Identity truth
- duplicate request:
  - command converges to current target state
- duplicate event delivery:
  - Audit dedupes by `MessageId` or equivalent `AuditEventId`

### Runtime rules

- Identity truth defines account status.
- Deactivated users must not be able to login or refresh from Identity truth.
- Audit is evidence, not account truth.
- Notifications are side effects, not success criteria.

---

## Flow H — Admin marks email as verified

### Goal

Allow a privileged admin override of email verification state while preserving auditability and avoiding token leakage.

### Truth flow

1. Admin calls `POST /api/v1/admin/identity/users/{userId}:mark-email-verified`.
2. API enforces `Permission:Identity.User.VerifyEmail`.
3. ABAC/context rules are evaluated where required:
   - protected system-account restrictions apply where policy requires
   - actor, target, IP/UserAgent, and correlation context are captured for audit
4. Identity loads target `UserAccount`.
5. Identity sets `IsEmailVerified = true` and `EmailVerifiedAt`.
6. Identity may revoke or obsolete active verification tokens according to policy.
7. Identity writes `identity.admin.email_marked_verified` outbox event in the same truth transaction.
8. Transaction commits.
9. API returns committed verification state.

### Async event emitted

Identity emits:

- `identity.admin.email_marked_verified`

### Event semantics

- this event is distinct from user-driven `identity.email_verified`
- carries stable `MessageId`
- includes actor user id, target user id, reason, and correlation id where available
- must not contain raw verification tokens or token hashes

### Downstream flow

10. Outbox worker publishes the event to RabbitMQ.
11. Audit consumes the event idempotently.
12. Notifications may consume the event if user-facing verification notification is enabled.
13. Cache/projection invalidation may happen asynchronously.

### Failure modes

- audit ingestion delayed:
  - email verification truth still applies immediately
- duplicate request:
  - command converges safely if already verified
- stale verification email later delivered:
  - delivery does not change already-verified truth
- replay of old verification delivery event:
  - must not override fresher verification truth

### Runtime rules

- admin verification override is high-risk and must be audited.
- verification truth is owned by Identity.
- notification delivery state does not define verification state.

---

## Flow I — Admin revokes user sessions

### Goal

Invalidate active refresh-token sessions for a target user immediately from Identity truth.

### Truth flow

1. Admin calls `POST /api/v1/admin/identity/users/{userId}:revoke-sessions`.
2. API enforces `Permission:Identity.User.RevokeSessions`.
3. ABAC/context rules are evaluated:
   - actor cannot revoke their own current session through normal admin APIs unless explicitly allowed
   - protected system-account restrictions apply where policy requires
4. Identity loads active refresh tokens for target user.
5. Identity revokes matching active refresh tokens.
6. Identity writes `identity.admin.user_sessions_revoked` outbox event in the same truth transaction.
7. Transaction commits.
8. API returns committed revocation result.

### Async event emitted

Identity emits:

- `identity.admin.user_sessions_revoked`

### Event semantics

- carries stable `MessageId`
- includes actor user id, target user id, revoked session count, reason, and correlation id where available
- must not contain raw refresh tokens or token hashes

### Downstream flow

9. Outbox worker publishes the event to RabbitMQ.
10. Audit consumes the event idempotently.
11. Cache/projection invalidation may happen asynchronously.

### Failure modes

- audit ingestion delayed:
  - revoked sessions remain invalid immediately
- duplicate request:
  - command converges to zero or current revoked session count according to documented result policy
- client timeout:
  - reconcile from Identity session truth
- duplicate event delivery:
  - Audit dedupes by `MessageId` or equivalent `AuditEventId`

### Runtime rules

- refresh-token validity is determined by Identity truth.
- revoked refresh tokens must not authorize refresh.
- cache/projection lag must not preserve old token authority.

---

## Flow J — Admin locks or unlocks user *(conditional in V1)*

### Goal

Set or clear account lock state where Identity schema supports lock fields.

### Availability

This flow is available only if Identity V1 schema supports lock state such as `LockedUntil` or equivalent account-security fields.

### Truth flow

1. Admin calls one of:
   - `POST /api/v1/admin/identity/users/{userId}:lock`
   - `POST /api/v1/admin/identity/users/{userId}:unlock`
2. API enforces `Permission:Identity.User.ManageSecurity`.
3. ABAC/context rules are evaluated:
   - actor cannot lock themselves through normal admin APIs
   - protected system accounts cannot be locked through normal admin APIs
4. Identity loads target `UserAccount`.
5. Identity applies or clears lock state.
6. Identity revokes active refresh tokens if policy or request requires it.
7. Identity writes the corresponding outbox event in the same truth transaction.
8. Transaction commits.
9. API returns committed lock state.

### Async events emitted

Identity emits one of:

- `identity.admin.user_locked`
- `identity.admin.user_unlocked`

### Failure modes

- schema does not support lock state:
  - flow remains unavailable/out of scope
- audit ingestion delayed:
  - lock/unlock truth still applies immediately
- duplicate request:
  - command converges to current lock state
- client timeout:
  - reconcile from Identity account/security truth
- duplicate event delivery:
  - Audit dedupes by `MessageId` or equivalent `AuditEventId`

### Runtime rules

- lock state is Identity security truth.
- locked users cannot sign in according to account state policy.
- unlocking does not verify email.
- unlocking does not assign roles or permissions.
- Audit/Notifications lag does not define lock truth.

---

## Summary

Identity runtime in V1 is governed by the following rules:

1. Identity owns account and security truth synchronously.  
2. Email delivery is downstream and non-blocking.  
3. Successful Identity writes mean truth committed, and where needed, async intent/outbox committed.  
4. Identity-derived async processing is at-least-once; duplicates and replay are normal.  
5. Verification/reset flows depend on committed identity intents, not provider success.  
6. `identity.verification_email_requested` and `identity.password_reset_requested` are canonical delivery-trigger events for Notifications.
7. Async events use stable `MessageId` and minimal privacy-aware payloads.  
8. Raw verification/reset tokens may appear only in approved Notifications delivery-trigger events and must be treated as secret-bearing payload fields. They must never appear in audit, lifecycle, logging, metrics, traces, errors, or general reporting payloads.
9. Refresh rotation and reuse detection must converge deterministically from authoritative token truth.  
10. Read-your-writes is required after security-sensitive truth changes.  
11. Expired/revoked artifacts may be cleaned up later without redefining current truth.  
12. Timeout ambiguity must be resolved from Identity truth, not from client belief or delivery state.
13. Identity Admin endpoints must enforce explicit permission policies before mutating truth.
14. Identity Admin mutations commit Identity truth and admin outbox events atomically.
15. Audit consumes Identity Admin events asynchronously and idempotently.
16. Admin command success does not imply Audit ingestion, Notification delivery, cache invalidation, or projection refresh.
17. Admin state-setting commands should converge safely under repeated requests.
18. Identity Admin must not mutate Authorization role/permission truth.
19. Session revocation and account deactivation must take effect immediately from Identity truth.
20. Lock/unlock flows are conditional until Identity schema supports lock state.
