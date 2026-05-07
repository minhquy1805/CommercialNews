# Identity — Idempotency & Consistency (V1)

This document defines Identity-specific idempotency, consistency guarantees, timeout ambiguity handling, token lifecycle safety, ordering-sensitive security state, replication-lag posture, and maintenance/cleanup rules for auth artifacts.

System-wide rules live in:

- `../../../../architecture/arc42/11-replication-v1.md`
- `../../../../architecture/arc42/13-transactions-and-consistency-v1.md`
- `../../../../architecture/arc42/14-distributed-systems-assumptions-v1.md`
- `../../../../architecture/arc42/15-consistency-ordering-and-consensus-v1.md`
- `../../../../architecture/arc42/16-batch-processing-and-derived-data-v1.md`
- `../../../../architecture/arc42/17-dataflow-and-batch-workflows-v1.md`
- `../../../../architecture/arc42/18-stream-processing-and-derived-state-v1.md`
- `../../../../architecture/arc42/19-stream-processing-runtime-v1.md`
- ADR-0013 (Outbox & delivery semantics)
- ADR-0015 (Redis cache policy)
- ADR-0018 (Transaction boundaries & consistency model)
- ADR-0019 (System model and fault assumptions)
- ADR-0020 (Timeout, retry, and failure detection policy)
- ADR-0021 (Clock, time, and ordering policy)
- ADR-0022 (Versioning and fencing strategy)
- ADR-0023 (Consistency, ordering, and consensus boundaries)
- ADR-0024 (Distributed coordination and singleton work policy)
- ADR-0025 (Batch processing and derived state policy)
- ADR-0026 (Batch job orchestration and materialization policy)
- ADR-0027 (Stream processing and derived state policy)
- ADR-0028 (Consumer idempotency, replay, and rebuild policy)

---

## 0) Role of Identity in the system

Identity owns **security-critical truth** for account, credential, token, and session state.

It decides:

- whether an account is verified
- whether a verification/reset token is valid, used, expired, or revoked
- whether a refresh token is active, revoked, or replaced
- whether a refresh-token family remains valid
- whether an account is enabled, disabled, or otherwise security-restricted
- whether replay/reuse has triggered a security response

Identity does **not** depend on:

- email delivery success
- audit persistence success
- cache freshness
- background consumer completion
- notification provider timing
- downstream reporting freshness

Therefore:

- core Identity correctness must come from primary truth
- side effects are downstream and eventual
- client belief is never authority for security decisions

---

## 1) Truth vs derived

### 1.1 Truth (must be primary-consistent)

Identity truth includes:

- user account state
  - `verified / unverified`
  - `enabled / disabled`
  - optional lockout/security flags if modeled
- verification/reset intent lifecycle
  - `issued / used / expired / revoked` as modeled
- refresh-token state
  - `active / revoked / replaced-by / family state`
- password/security transitions
- replay/reuse response truth
- optional login-history truth only if explicitly owned by Identity in this scope

**Rule:** verify/reset/refresh/auth decisions MUST read and write the primary truth store.

Redis is not allowed as the source of truth for:

- token validity
- verified state
- password-reset state
- refresh-token family state
- account enable/disable decisions
- replay/reuse response truth

### 1.2 Derived (allowed but bounded)

Redis is allowed for:

- rate-limit counters
- short-lived dedupe hints where security/business impact is low
- optional non-authoritative acceleration data

Email delivery is a derived side effect:

- email sending is async
- delivery may lag
- correctness must not depend on delivery timing

Audit persistence is also derived with respect to Identity truth.

Maintenance/reporting outputs may also be derived, such as:

- cleanup candidate sets
- expired-token reports
- suspicious-auth summaries
- archival/login-history summaries
- downstream notification delivery summaries

### 1.3 Server truth beats client belief

Identity must not trust client claims such as:

- “this token is still valid”
- “this account is already verified”
- “this request did not succeed because I saw a timeout”
- “this refresh probably failed because I never received the response”
- “this timestamp shows the token is still fresh”

Authority remains server-side:

- server-generated token state
- server-side UTC expiry evaluation
- primary truth state in the authoritative store

### 1.4 Consistency class for Identity

Identity intentionally uses multiple consistency classes.

#### Strong truth-backed consistency

Required for:

- verified/unverified truth
- password and security-state changes
- token issue/revoke/replace truth
- refresh-token family truth
- account enable/disable truth
- replay/reuse response truth
- uniqueness decisions such as email where owned by Identity

#### Ordered / causality-sensitive consistency

Required for:

- token lifecycle transitions
- refresh rotation and replacement chains
- replay/reuse response after a prior revoke/replace action
- single-use token consumption
- stale client / duplicate request rejection where order matters
- cleanup/report publication that must not overtake fresher token truth
- downstream consumers that must not replay older security-derived state over newer truth

#### Eventual consistency

Accepted for:

- email delivery
- audit persistence
- notification pipeline state
- non-authoritative operational accelerators
- cleanup/reporting convergence

---

## 2) Consistency guarantees (non-negotiable)

### 2.1 Read-your-writes (required)

After any successful Identity state change, self reads must reflect it immediately.

Applies to:

- email verification
- password change/reset
- account disable/enable
- refresh-token rotation and revocation effects that change current session validity
- security-state transitions after reuse/replay detection

Policy:

- `/me` and identity self-state endpoints MUST bypass stale caches/replicas
- results should come from primary truth or the just-committed write result

Avoid anomalies such as:

- “verify succeeded but `/me` still shows unverified”
- “password reset succeeded but old session still appears valid”
- “reuse detected but subsequent auth path still treats token family as active”

### 2.2 Monotonic reads (recommended)

For authenticated self reads and security-sensitive state reads:

- do not serve from stale replicas/caches
- avoid “time going backward” on visible security state

### 2.3 Truth-first security posture

Allowed to lag:

- email sending
- email delivery logs
- audit persistence
- notification side effects
- cleanup/reporting jobs

Not allowed to lag:

- verified status
- password hash changes
- token revocation/rotation state
- account enabled/disabled state
- replay/reuse response state

### 2.4 No global ordering assumption

Identity does **not** assume:

- one total global order across all Identity events
- one system-wide sequence across all users or token families

**Rule:** ordering is scoped per account, per token family, per token lifecycle, or per Identity-owned aggregate as needed.

### 2.5 Cause-before-effect rule

Security effects must follow committed truth and must not be made “real” by downstream systems.

Examples:

- account becomes verified when Identity commits verified truth, not when email logs catch up
- reset becomes effective when password/token/session truth commits, not when audit arrives
- reuse response is real when revocation truth commits, not when an alert is sent

---

## 3) Transaction boundary (V1)

### 3.1 Truth boundary

The Identity transaction boundary stops at the Identity-owned truth change.

Typical truth changes include:

- user registration and initial verification state creation
- resend-verification request state updates when persisted
- forgot-password / reset-password intent creation and token consumption
- password hash updates
- refresh token issue / revoke / replace transitions
- account enable/disable state changes
- security-state updates caused by token reuse detection

### 3.2 Atomic commit set

For security-sensitive commands, Identity MUST commit atomically:

- the primary Identity truth change
- token/request lifecycle updates required by that decision
- revocation/rotation markers required by policy
- version/revision or conditional freshness markers if used
- the Outbox record for downstream side effects/signals

Typical examples:

- register:
  - user creation
  - verification intent/token state
  - outbox record(s)
- verify email:
  - verified flag update
  - verification token consumption
  - outbox record(s)
- reset password:
  - reset token consumption
  - password hash update
  - token/session revocation by policy
  - outbox record(s)
- refresh rotation:
  - old token revoke
  - replacement link
  - new token insert
- refresh reuse response:
  - family/user revocation actions
  - outbox/security signal

### 3.3 Outside the transaction

The following MUST NOT be required inside the Identity truth transaction:

- email sending
- broker publish
- audit persistence in downstream stores
- Redis invalidation as a success condition
- external HTTP/API calls
- long-running abuse analysis
- notification workflow completion
- cleanup/reporting workflow completion

These are post-commit async effects.

### 3.4 Transaction duration rule

Identity transactions must be short:

- no waiting for downstream systems
- no cross-request interactive transaction
- no open transaction while performing external network work
- no retry loops over external dependencies inside the transaction

This is especially important for:

- refresh
- verify
- reset
- replay/reuse response
- password/security-state transitions

### 3.5 Shared DB does not widen Identity ownership

Even in a shared DB deployment, Identity must not use the same truth transaction to directly perform:

- Notification business writes
- Audit business writes
- Authorization business writes

Identity may write only:

- Identity-owned truth tables
- approved local replication artifacts such as Outbox
- durable idempotency records when policy requires them

It must not couple auth success to downstream module completion.

### 3.6 No heterogeneous distributed transaction

Identity does **not** attempt one atomic workflow across:

- Identity truth DB
- RabbitMQ
- Redis
- email providers
- audit stores
- other module-owned truth stores

Atomicity stops at:

- Identity truth mutation
- required token/request lifecycle state
- local freshness/version markers where used
- Outbox intent

---

## 4) Concurrency and stale-action posture

### 4.1 Concurrency assumption

Identity must assume:

- duplicate client retries
- concurrent requests
- replay of already-used tokens
- refresh requests racing each other
- stale clients acting on already-changed truth
- timeout ambiguity during sensitive operations
- cleanup/report workflows running later on already-changed artifacts
- downstream replay of old Identity-derived events

### 4.2 Required protections

At minimum, the design should prevent:

- duplicate user creation for the same semantic request
- double-consumption of verification/reset tokens
- inconsistent refresh-token family state
- contradictory outcomes from racing refresh/revoke actions
- stale self-state reads after successful security transitions

Typical implementation options include:

- DB uniqueness constraints
- durable idempotency records
- conditional updates / compare-and-set semantics
- explicit conflict/already-used responses
- family-level or token-level atomic revocation rules

### 4.3 Timestamp is not freshness authority

Identity must not use:

- `UpdatedAt`
- client-provided time
- “latest timestamp wins”

as the primary authority for token/state freshness.

Identity freshness/validity must instead come from:

- token lifecycle truth
- explicit used/revoked flags
- version/conditional update semantics where needed
- server-side expiry evaluation

### 4.4 Resource-side protection beats caller belief

A caller saying:

- “this refresh token should still be current”
- “this verify/reset token should still be unused”
- “this request surely failed because I timed out”

is not sufficient.

The Identity truth boundary must verify:

- current token/request lifecycle state
- current token family state
- current account security state
- expected freshness/version semantics where used

### 4.5 State legality and freshness are complementary

Identity must enforce both:

- lifecycle legality
- stale/replayed action rejection

Examples:

- a token may be structurally valid but already used
- a refresh token may be syntactically valid but already replaced
- an account may exist but current policy forbids the requested transition

**Rule:** freshness protection prevents stale/replayed actions; lifecycle rules prevent illegal security transitions.

### 4.6 Stale derived state must not weaken security truth

If a derived view, cache, or delayed downstream consumer still reflects older security state:

- it must not re-authorize old token/session truth
- it must not re-open already-consumed verification/reset intent
- it must not weaken a reuse-triggered revocation response
- it must be ignored, rebuilt, or bypassed according to truth-first policy

---

## 5) Idempotency keys (recommended)

### 5.1 Endpoints that should support idempotent retry posture

Support `Idempotency-Key` for endpoints likely to be retried:

- `POST /register`
- `POST /resend-verification`
- `POST /forgot-password`

Goal:

- prevent duplicate harmful side effects
- support safe client retries under timeout ambiguity
- avoid duplicate token issuance or duplicate delivery intent where harmful

### 5.2 Semantics

Rules:

- same `Idempotency-Key` + same semantic request → same or safely convergent outcome
- same `Idempotency-Key` + different semantic payload → deterministic conflict/error by policy

### 5.3 Storage posture

Preferred:

- durable idempotency records for requests where duplicates are harmful or security-relevant

Redis-only dedupe is acceptable only when:

- business impact is low
- correctness/security does not depend on it
- TTL safely covers the retry window

For higher-impact security flows, durable records are preferred.

### 5.4 Replay-safe intent handling

A replayed request/event must not be confused with a legitimately new intent.

Examples:

- resend-verification should create a new logical verification intent
- forgot-password may create a new reset intent by policy
- duplicate retry of the same old request must not create multiple conflicting truth states

## 5.5 Admin command idempotency and convergence

Identity Admin state-setting commands are convergent by default.

Applies to:

- `POST /api/v1/admin/identity/users/{userId}:activate`
- `POST /api/v1/admin/identity/users/{userId}:deactivate`
- `POST /api/v1/admin/identity/users/{userId}:lock` *(where schema supports lock state)*
- `POST /api/v1/admin/identity/users/{userId}:unlock` *(where schema supports lock state)*
- `POST /api/v1/admin/identity/users/{userId}:mark-email-verified`
- `POST /api/v1/admin/identity/users/{userId}:revoke-sessions`

Rules:

- same state-setting command against an already matching state should return the current committed state
- convergent no-op commands should not emit duplicate state-change events unless audit policy explicitly requires attempt logging
- client timeout after an admin mutation must be reconciled from Identity truth
- repeated admin commands must not create duplicate harmful downstream effects
- required admin outbox events must use stable `MessageId`
- downstream Audit must dedupe admin events by `MessageId` or equivalent `AuditEventId`

Examples:

- activating an already active user returns current active state
- deactivating an already inactive user returns current inactive state
- marking an already verified email returns current verified state
- revoking sessions when no active sessions remain returns a successful committed state with zero newly revoked sessions

---

## 6) Outbox & downstream side effects

### 6.1 Email sending is asynchronous

Email sending must not block core Identity flows.

Identity endpoints may return success or accepted-style outcomes before email delivery completes.

Examples:

- registration may commit before verification email is delivered
- forgot-password may commit before reset email is delivered
- resend-verification may commit before verification email is delivered

### 6.2 Canonical async event categories

Identity emits two broad categories of async events.

#### Lifecycle / audit-oriented events

Examples:

- `identity.user_registered`
- `identity.email_verified`
- `identity.password_changed`
- `identity.user_logged_in` *(optional)*
- `identity.admin.user_activated`
- `identity.admin.user_deactivated`
- `identity.admin.email_marked_verified`
- `identity.admin.user_sessions_revoked`
- `identity.admin.user_locked`
- `identity.admin.user_unlocked`

Lifecycle/audit-oriented events must never contain:

- raw verification tokens
- raw reset tokens
- raw refresh tokens
- token hashes
- password hashes
- unnecessary PII

#### Delivery-trigger events for Notifications

Examples:

- `identity.verification_email_requested`
- `identity.password_reset_requested`

These are canonical notification-trigger events for downstream delivery workflows.

Delivery-trigger events may carry raw one-time token material when required by Notifications:

- `identity.verification_email_requested` may carry `RawVerificationToken`
- `identity.password_reset_requested` may carry `RawResetToken`

These fields are secret-bearing delivery material.

They must not be:

- logged as raw payload JSON
- attached to exception logs
- copied into `Audit.Data`
- exposed through metrics, traces, diagnostics, dashboards, or support tools
- routed to consumers that are not approved to handle secret-bearing delivery messages

### 6.3 Producer-side contract

Identity guarantees that emitted async messages:

- carry stable `MessageId`
- are derived from committed Identity truth
- contain minimal privacy-aware payloads
- use `BusinessDedupeKey` where duplicate outward effects may be harmful
- distinguish lifecycle/audit events from delivery-trigger events

For delivery-trigger events, raw verification/reset token fields are delivery material only.

Identity database truth stores only token hashes.

### 6.4 Consumer requirements

Downstream consumers must tolerate:

- at-least-once delivery
- duplicate message delivery
- provider timeout ambiguity
- delayed or replayed delivery after newer truth already exists

Audit consumers must:

- dedupe by `MessageId` or equivalent `AuditEventId`
- never persist raw delivery-token fields
- store only sanitized investigation metadata

Notifications consumers must apply:

- message-level dedupe by `MessageId`
- business-level dedupe by `BusinessDedupeKey` where duplicate user-visible delivery is harmful
- durable delivery state before/around provider calls
- redaction of secret-bearing token fields from logs and diagnostics

Cache/projection consumers must:

- treat Identity truth as authoritative
- avoid stale event apply over fresher Identity truth
- resync from truth where freshness/order matters

### 6.5 Timeout ambiguity rule

A timeout in the notification path does **not** prove:

- no email was sent
- no delivery attempt happened
- the originating Identity truth failed

Therefore Identity treats:

- email as eventual
- delivery as downstream operational truth
- security truth as independent from notification timing

### 6.6 Outbox is the causal bridge

For Identity, Outbox is the durable bridge between:

- truth mutation
- security decision completion
- downstream notification/audit propagation

This means:

- side effects derive from committed Identity truth
- missing or delayed side effects are recoverable by retry/replay
- side-effect timing does not redefine security truth

### 6.7 Required outbox atomicity

When an Identity operation requires downstream side effects, Identity must commit atomically:

- Identity truth change
- required lifecycle/token updates
- required `OutboxMessage`

If the required outbox intent cannot be committed, the Identity command must not return success.

After commit, producer-side publish failures and consumer-side processing failures are handled by worker/consumer retry, DLQ/dead-state, observability, and remediation.

---

## 7) Verification and reset token consistency

### 7.1 Token lifecycle truth

Verification/reset tokens must be modeled as authoritative truth with explicit lifecycle such as:

- issued
- used
- expired
- revoked *(if applicable)*

### 7.2 Single-use guarantee

Reset and verification token consumption must be single-use by policy.

Consumption must be atomic with the corresponding truth transition.

Examples:

- verify email:
  - mark token used
  - set user verified
- reset password:
  - mark token used
  - update password hash
  - revoke sessions/tokens by policy

### 7.3 Expiry rule

Expiry is evaluated from server-side UTC truth.

Client-side time must not be used as authority.

### 7.4 Replay / already-used posture

If an already-used token is presented:

- return a deterministic, policy-defined outcome
- do not silently reapply the state change
- do not issue conflicting truth transitions

### 7.5 No token-winner decision by timestamp

Token validity or freshness must not be decided by:

- which token record has the latest timestamp
- which request arrived with later client time
- “newest-looking wall-clock value wins”

Lifecycle truth and explicit consumption/revocation state are authoritative.

### 7.6 Rebuild / replay posture for token artifacts

Cleanup, reporting, or replay workflows may reason over token artifacts later, but:

- they must not resurrect used/expired/revoked token truth
- they must not publish stale candidate output over fresher token state
- they must treat current Identity truth as authoritative rebuild source

---

## 8) Refresh token rotation rules (truth consistency)

### 8.1 Refresh is security-critical

`POST /refresh` is security-critical and MUST not be served from stale replicas/caches.

### 8.2 Rotation contract

Refresh-token rotation must be atomic in the truth store:

- old token revoked
- replacement/new token inserted
- replacement link/family metadata recorded
- timestamps/client metadata recorded as policy requires

### 8.3 Racing refresh protection

Concurrent or replayed refresh requests must not produce contradictory family state.

The implementation should use:

- conditional updates
- token-family invariants
- explicit already-revoked/replaced handling
- deterministic conflict outcomes where races are detected

### 8.4 Read-your-writes after refresh

Once rotation commits successfully:

- subsequent auth decisions must reflect the new token family truth immediately
- stale caches must not re-authorize the old token

### 8.5 Ordering scope for refresh families

Refresh ordering is per token family / security aggregate, not globally across all users.

### 8.6 Replay of older refresh-derived state must not weaken newer truth

If a delayed or replayed downstream view still reflects pre-rotation state:

- it must not be treated as valid security authority
- it must not mint or validate stale successor assumptions
- it must be bypassed or repaired from truth

---

## 9) Reuse detection (policy)

### 9.1 Reuse detection meaning

If a revoked/rotated token is presented again, treat it as possible token theft or replay.

### 9.2 Recommended response

Recommended response:

- revoke token family *(or all tokens for the user, by policy)*
- force re-login
- emit security/audit event asynchronously
- record telemetry for suspicious reuse

### 9.3 Consistency requirement

Once reuse is detected and revocation is applied:

- subsequent auth decisions must reflect revocation immediately from primary truth
- delayed downstream side effects must not weaken the revocation decision

### 9.4 Cause-before-effect rule

Security effect must not lag behind its cause in a way that weakens protection.

Examples:

- if reuse is detected, revocation truth must commit before downstream alerts/audit
- delayed email/audit path must not be mistaken for the authority that makes revocation “real”

---

## 10) Password reset/change consistency (session safety)

### 10.1 Reset/change must cut off old session authority

When password is reset/changed:

- revoke refresh tokens by policy *(recommended: revoke all tokens for the user)*
- ensure old sessions cannot continue indefinitely

### 10.2 Atomicity rule

Token consumption and password update must be atomic in truth:

- mark reset token used
- update password hash
- revoke token/session family according to policy

### 10.3 Eventual side effects

Allowed to lag:

- email delivery
- downstream audit persistence
- notification of password change if implemented

Not allowed to lag:

- password truth
- token revocation truth
- account security-state truth

### 10.4 Replay / duplicate attempt rule

A repeated reset/change request must not:

- reapply the same single-use reset token
- create contradictory password truth
- leave old sessions active because a downstream cleanup job has not caught up

## 10.5 Identity Admin consistency

### 10.5.1 Admin account status changes

Admin activate/deactivate operations must be truth-first.

For deactivate:

- account status must update in Identity truth
- active refresh tokens should be revoked when policy/request requires it
- outbox event must be committed atomically with the truth change
- deactivated users must not login or refresh from Identity truth

Allowed to lag:

- Audit ingestion
- Notifications delivery
- cache invalidation
- projections/security summaries

Not allowed to lag:

- account status truth
- refresh-token revocation truth where applied

### 10.5.2 Admin email verification override

Admin email verification override must update Identity verification truth directly.

Rules:

- `IsEmailVerified` and `EmailVerifiedAt` are Identity truth
- active verification tokens may be revoked or made obsolete according to policy
- admin event `identity.admin.email_marked_verified` must not contain raw verification tokens or token hashes
- this event is distinct from user-driven `identity.email_verified`
- repeated override against an already verified user should converge safely

### 10.5.3 Admin session revocation

Admin session revocation must invalidate refresh-token truth immediately.

Rules:

- revoked refresh tokens must not authorize refresh
- cache/projection lag must not preserve old token authority
- duplicate revoke command should converge safely
- if no active sessions remain, the command should return current committed revocation state according to response policy
- admin event `identity.admin.user_sessions_revoked` must not contain raw refresh tokens or token hashes

### 10.5.4 Admin lock/unlock consistency

Where schema supports lock state:

- lock/unlock state is Identity security truth
- locked users cannot sign in according to account state policy
- unlock does not verify email
- unlock does not assign roles or permissions
- repeated lock/unlock commands should converge safely

Where schema does not support lock state:

- lock/unlock commands remain unavailable or out of scope
- callers must receive deterministic unsupported-operation behavior

### 10.5.5 Admin transaction boundary

Identity Admin commands must not update the following inside the Identity truth transaction:

- Authorization role/permission truth
- Audit business truth
- Notifications delivery truth
- Redis/cache state as success condition
- derived read-model truth

Admin success means:

- Identity truth committed
- required admin outbox intent committed

Admin success does not mean:

- RabbitMQ publish completed
- Audit ingested the event
- Notifications sent email
- cache invalidation completed
- projections caught up

---

## 11) Safe reconciliation after ambiguous outcomes

### 11.1 Timeout is not proof of failure

If a client times out during:

- register
- verify email
- forgot password
- reset password
- refresh

that timeout does **not** prove the truth change did not commit.

### 11.2 Reconciliation posture

Safe reconciliation must rely on:

- primary truth state
- token lifecycle state
- account verified status
- refresh-token family truth
- durable idempotency records where used

### 11.3 Do not infer from side effects

Do not use:

- “email received / not received”
- “audit row visible / not visible”
- “cache says X”

as authority for whether Identity truth committed.

Identity truth is authoritative.

### 11.4 Maintenance and cleanup are subordinate

Cleanup/reporting workflows may act on expired or terminal auth artifacts later, but:

- they do not decide current token validity
- they do not override account security truth
- they must not race with live truth decisions in a way that weakens security

### 11.5 Replay-safe recovery

If replay/rebuild/remediation is needed for downstream effects or maintenance outputs:

- current Identity truth remains the rebuild source
- repeated processing on the same bounded input must be safe
- derived recovery outputs must not be mistaken for current live security truth

---

## 12) Eventual consistency notes (what is allowed to lag)

Allowed to lag:

- email delivery and delivery logs
- audit trails of Identity actions
- notification pipeline state
- cleanup/reporting and archival outputs

Not allowed to lag:

- verified status
- password hash changes
- token revocation/rotation state
- account enabled/disabled state
- replay/reuse response state

### 12.1 Derived outputs remain derived

Derived outputs such as:

- suspicious-auth summaries
- cleanup candidate sets
- maintenance dashboards
- token-family reports
- replay/remediation candidate outputs

may be:

- rebuilt
- replaced
- delayed
- replayed

But they do not become live security truth.

---

## 13) Coordination and ownership posture (Identity)

### 13.1 Identity correctness does not rely on singleton workers by default

Ordinary Identity correctness must not depend on:

- one global auth leader
- one process being “the only refresh handler”
- one notification worker being “surely current”
- startup order or timeout belief deciding current authority

Identity correctness should instead be achieved through:

- truth-store authority
- lifecycle truth
- unique constraints
- conditional updates
- replay-safe async side effects
- stale/replayed action rejection

### 13.2 If future ownership-sensitive security workflows are introduced

If a future Identity workflow truly requires one current owner
(for example one-current security maintenance owner or one-current token-family repair worker),
that workflow must define:

- ownership source of truth
- monotonic generation/fencing token
- resource-side rejection of stale owner actions

Naive leader/lock patterns are not acceptable.

### 13.3 Safe non-progress beats unsafe stale security change

If ownership is ambiguous for a correctness-sensitive maintenance or repair workflow, Identity must prefer:

- delayed maintenance
- operator retry
- stale-owner rejection
- continued truth-first security decisions

over unsafe dual repair or stale overwrite of security artifacts.

---

## 14) Observability signals (Identity-specific)

Minimum signals:

- register/resend/forgot/reset success vs failure rate
- rate-limit trigger rate
- verification/reset delivery lag for auth-critical flows
- outbox oldest pending age
- broker queue depth for downstream delivery path where measured
- suspicious signals:
  - refresh reuse detections
  - token family revocations
  - spikes in reset requests
- durable idempotency hits/conflicts
- downstream delivery dedupe hits where measurable
- token invalid / expired / already-used reject counts
- token rotation conflicts / replay rejects
- timeout/ambiguity rates on security-sensitive endpoints where measurable
- stale/replayed security action rejection count
- cleanup/reconciliation activity for auth artifacts where used
- ownership-generation mismatch count if future ownership-sensitive workflows are introduced
- admin identity mutation success/failure rate
- admin deactivate/activate count
- admin mark-email-verified count
- admin revoke-sessions count
- admin lock/unlock count where supported
- admin protected-account denial count
- admin self-action denial count
- admin ABAC denial count
- Audit ingestion lag for `identity.admin.*`
- Audit dedupe hits for `identity.admin.*`
- Notification business-dedupe rejects for Identity delivery-trigger events
- raw-token redaction/secret-field logging violations where detectable

Logs should include:

- `correlationId`
- account/user identity in safe form
- token identifiers only in hashed/redacted form
- outcome classification such as:
  - `success`
  - `already-used`
  - `replayed`
  - `revoked`
  - `conflict`
  - `rate-limited`
- no raw secrets/tokens in logs

---

## 15) Summary

Identity correctness in V1 rests on the following rules:

1. Identity security decisions come only from primary server truth.  
2. Read-your-writes is mandatory after security-sensitive state changes.  
3. Email and audit are downstream eventual effects, not part of core auth success.  
4. Async downstream processing is at-least-once; duplicates, replay, and lag are normal.  
5. Timeout does not prove a token/state change failed.  
6. Verification/reset tokens are authoritative lifecycle truth and must be single-use.  
7. Refresh rotation and reuse response must be atomic and truth-first.  
8. Client time and client belief are never authority for security decisions.  
9. Safe reconciliation after ambiguity must read primary truth, not infer from side effects.  
10. Cleanup/reporting workflows are derived and subordinate to current security truth.  
11. No global ordering or heterogeneous distributed transaction is assumed for Identity workflows.  
12. Stale derived state must never weaken fresher security truth.  
13. Replay/rebuild/remediation workflows must remain rerun-safe.  
14. Candidate derived maintenance/reporting output must be validated before publication when correctness matters.  
15. Singleton/ownership semantics are not relied on unless explicitly protected by authoritative generation/fencing rules.  
16. Raw verification/reset tokens may appear only in approved Notifications delivery-trigger events and must be treated as secret-bearing payload fields.  
17. Lifecycle, audit-oriented, admin, logging, metrics, trace, error, and reporting payloads must never contain raw tokens or token hashes.  
18. Identity Admin state-setting commands are convergent by default.  
19. Identity Admin mutations must commit Identity truth and required admin outbox events atomically.  
20. Audit consumes Identity Admin events asynchronously and idempotently.  
21. Identity Admin must not mutate Authorization role/permission truth.  
22. Admin account deactivation and session revocation must take effect immediately from Identity truth.