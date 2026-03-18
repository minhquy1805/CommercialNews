# Identity — Runtime Flows (V1)

This module supports arc42 runtime scenarios:
- Scenario 4: registration + verification
- Scenario 5: forgot + reset password

Related:
- `../../../../architecture/arc42/04-runtime-view-v1.md`
- `../../../../architecture/arc42/13-transactions-and-consistency-v1.md`
- `../../../../architecture/arc42/18-stream-processing-and-derived-state-v1.md`
- `../../../../architecture/arc42/19-stream-processing-runtime-v1.md`
- `../../../../architecture/arc42/16-batch-processing-and-derived-data-v1.md`
- `../../../../architecture/arc42/17-dataflow-and-batch-workflows-v1.md`
- `../../../../decisions/adr-0013-outbox-and-delivery-semantics-v1.md`
- `../../../../decisions/adr-0027-stream-processing-and-derived-state-policy-v1.md`
- `../../../../decisions/adr-0028-consumer-idempotency-replay-and-rebuild-policy-v1.md`

---

## Runtime posture in V1

Identity primarily participates in two runtime lanes:

### A) Synchronous truth lane
Used for:
- register
- verify email
- login
- refresh token rotation
- forgot/reset password truth handling
- password change
- token revoke / reuse-detection response

### B) Async side-effect lane
Used for:
- verification email delivery
- reset-password email delivery
- optional audit side effects
- optional security notification signals

Identity may also participate in a lighter third lane:

### C) Batch / cleanup / maintenance lane
Used for:
- cleanup expired verification/reset tokens
- cleanup revoked/expired refresh tokens
- retention cleanup for auth/session artifacts
- optional archival / summarization for login history or auth maintenance outputs
- reconciliation/reporting for stuck or orphan auth artifacts

**Rule:** Identity owns security/account truth.  
Cleanup, reporting, and maintenance workflows may lag, but they do not redefine current security truth.

**Rule:** Identity success is defined by **truth commit**, not by downstream email/audit completion.

**Rule:** Identity-derived async processing is assumed **at-least-once**.  
Duplicates, replay, lag, and worker restart must be tolerated safely.

---

## Flow A — Register → Verification email (async) → Verify

### Goal
Create account truth synchronously, then verify asynchronously without blocking registration success.

### Flow
1. Client calls `POST /auth/register`.
2. Identity creates user in **Unverified** state.
3. Identity creates verification intent/token according to policy.
4. Identity emits `UserRegistered` (or `VerificationEmailRequested`) through Outbox.
5. Notifications worker sends verification email (retry-safe, idempotent).
6. Client calls `POST /auth/verify-email` with token.
7. Identity validates token and marks user verified/active.
8. Identity emits `UserEmailVerified`.

### Runtime stream semantics
- verification email delivery is a **downstream effect**, not part of registration truth
- duplicate notification delivery must not create duplicate harmful sends
- replay of verification-email events is normal under at-least-once delivery
- verification truth is determined by Identity token/account state, not by provider status

### Failure modes
- Email provider down: register still succeeds; email retries; no duplicate emails.
- Token expired: client uses `/resend-verification` (rate-limited).
- Timeout during verify: reconcile from Identity truth, not from email state.
- Duplicate verify attempts: converge safely to verified state or documented no-op/conflict.
- Replay of stale delivery events must not change already-verified truth.

### Batch / cleanup hooks
- expired verification tokens may later be cleaned up by bounded maintenance workflows
- stale or orphan verification intents may be reconciled/reported
- email delivery/reporting artifacts remain derived, not identity truth

### Runtime rules
- account creation and verified-state change are truth-bound operations
- email may lag, but account/security truth must remain authoritative
- read-your-writes applies after successful verify

---

## Flow B — Login → Refresh rotation

### Goal
Issue and rotate session credentials safely with deterministic token-family behavior.

### Flow
1. Client calls `POST /auth/login`.
2. Identity validates credentials and returns access token + refresh token.
3. Refresh token truth is persisted according to policy.
4. Client calls `POST /auth/refresh` to rotate refresh token.
5. Identity validates current refresh token.
6. Identity revokes old token, issues new refresh token, returns new pair.
7. Identity records replacement relation / token-family linkage if modeled.

### Runtime stream semantics
- refresh rotation is a truth-first security transition
- old/new token-family state must converge deterministically
- delayed audit/notification signals do not define session validity
- replayed refresh or reused old token must be evaluated against current token-family truth

### Failure modes
- Reuse detection: revoked token used again → apply policy (revoke family, force re-login).
- Timeout during refresh: reconcile from refresh-token truth, not from client belief.
- Duplicate refresh attempt: stale or reused token must not mint multiple valid successor states incorrectly.
- Stale client retry after successful rotation must not resurrect old token authority.

### Batch / cleanup hooks
- revoked/expired refresh tokens may be cleaned up later
- token-family drift or suspicious reuse may be summarized in derived reports
- cleanup must not weaken current token-validity truth

### Runtime rules
- refresh is security-critical and truth-backed
- old token authority must end at truth boundary, not when a downstream effect catches up
- read-your-writes applies for immediate post-refresh session validity checks

---

## Flow C — Forgot → Reset

### Goal
Allow secure password reset without leaking account existence.

### Flow
1. Client calls `POST /auth/forgot-password` (always returns accepted).
2. If email exists, Identity creates password-reset intent/token.
3. Identity emits `PasswordResetRequested`.
4. Notifications worker sends reset email (async).
5. Client calls `POST /auth/reset-password` with token and new password.
6. Identity validates token and updates password.
7. Identity revokes relevant refresh tokens by policy.
8. Identity emits `UserPasswordChanged`.

### Runtime stream semantics
- forgot-password request and reset email delivery are decoupled from reset truth
- duplicate reset-email delivery must not create duplicate harmful business outcomes
- password/security truth is determined by Identity, not by email delivery timing
- replay of old reset-related events must not override fresher token/session truth

### Non-blocking rule
- forgot/register must not block on email delivery.

### Failure modes
- Duplicate forgot requests: anti-enumeration still holds.
- Token expired/used: deterministic failure.
- Timeout during reset: reconcile from Identity truth, not from email provider outcome.
- Duplicate reset attempt with same token: must not produce contradictory password truth.
- Stale refresh/session state must not survive policy-defined revocation after reset.

### Batch / cleanup hooks
- expired reset tokens may later be cleaned up
- stale reset intents may be reported for maintenance
- delivery artifacts remain derived

### Runtime rules
- reset consumes truth-owned token lifecycle
- password change and session revocation are truth-first
- downstream lag must not weaken the reset security effect

---

## Flow D — Resend verification / auth maintenance

### Goal
Create a valid new verification intent safely without being blocked by old delivery artifacts.

### Flow
1. Client calls `POST /auth/resend-verification`.
2. Identity enforces anti-abuse / rate-limit policy.
3. Identity creates a new verification intent/token if policy allows.
4. Identity emits a new delivery-trigger event.
5. Notifications handles async delivery.

### Runtime stream semantics
- resend is a **new logical intent**, not a replay of the old one
- old business dedupe state must not suppress a valid new verification intent
- replay of the same resend event must not multiply harmful delivery effects

### Rules
- new resend must be a new logical intent
- old delivery dedupe state must not block a valid new token
- old expired intents may remain for cleanup until retention policy removes them

### Failure modes
- rate-limit or abuse control rejects resend
- old expired token remains in storage but does not define current verification truth
- notification lag delays convenience, not account truth
- retry ambiguity must reconcile from current verification-intent truth

---

## Flow E — Cleanup / retention workflow

### Goal
Control growth of auth/security artifacts without changing current truth incorrectly.

### Typical workflow shape
1. Select bounded expired or terminal auth artifacts:
   - verification tokens
   - reset tokens
   - revoked/expired refresh tokens
2. Apply cleanup/archival policy.
3. Record cleanup outcome.

### Rules
- cleanup is bounded
- cleanup must not remove artifacts still required for current security truth or investigation policy
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
1. A security-sensitive request arrives:
   - verify
   - reset
   - refresh
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
- email missing does not imply token or account truth failed

### Rules
- Identity truth wins over all derived operational views
- timeout ambiguity must be resolved from truth
- safe deny/reject beats stale or guessed security behavior

---

## Summary

Identity runtime in V1 is governed by ten rules:

1. Identity owns account and security truth synchronously.  
2. Email delivery is downstream and non-blocking.  
3. Identity-derived async processing is at-least-once; duplicates and replay are normal.  
4. Verification/reset flows depend on committed identity intents, not provider success.  
5. Refresh rotation and reuse detection must converge deterministically.  
6. Read-your-writes is required after security-sensitive truth changes.  
7. Expired/revoked artifacts may be cleaned up later without redefining current truth.  
8. Maintenance/reporting workflows are derived and subordinate to identity truth.  
9. Timeout ambiguity must be resolved from Identity truth, not from client belief or delivery state.  
10. Truth-backed security decisions must remain correct even while downstream systems lag.