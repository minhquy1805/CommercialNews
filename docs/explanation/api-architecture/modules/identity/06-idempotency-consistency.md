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

Identity owns **security-critical truth** for user and session state.

It decides:
- whether an account is verified
- whether a reset/verification token is valid, used, or expired
- whether a refresh token is active, revoked, or replaced
- whether an account is enabled or disabled
- whether reuse/replay has triggered a security response

Identity does **not** depend on:
- email delivery success
- audit persistence success
- cache freshness
- background consumer completion

Therefore:
- core Identity correctness must come from primary truth
- side effects are downstream and eventual
- client belief is never authority for security decisions

---

## 1) Truth vs derived

### 1.1 Truth (must be primary-consistent)
Identity truth includes:
- user account state (`enabled / disabled`, `email verified`)
- verification/reset requests and their lifecycle (`issued / used / expired / revoked` as modeled)
- refresh token state (`active / revoked / replaced-by / family state`)
- security-state transitions caused by replay/reuse detection
- login history if modeled as Identity-owned truth in this scope

**Rule:** verify/reset/refresh/auth decisions MUST read/write the primary truth store.

Redis is not allowed as the source of truth for:
- token validity
- verified state
- password-reset state
- refresh token family state
- account enable/disable security decisions

### 1.2 Derived (allowed but bounded)
Redis is allowed for:
- rate-limit counters
- short-lived dedupe hints where business impact is low
- optional non-authoritative acceleration data

Email delivery is a derived side effect:
- email send is async
- delivery may lag
- correctness must not depend on delivery timing

Audit persistence is also derived with respect to Identity truth.

Maintenance/reporting outputs may also be derived, such as:
- cleanup candidate sets
- expired-token reports
- suspicious-auth summaries
- archival/login-history summaries

### 1.3 Server truth beats client belief
Identity must not trust client claims such as:
- “this token is still valid”
- “this account is already verified”
- “this request did not succeed because I saw a timeout”
- “this timestamp shows the token is still fresh”

Authority remains server-side:
- server-generated token state
- server-side UTC expiry evaluation
- server truth read from primary store

### 1.4 Consistency class for Identity
Identity intentionally uses multiple consistency classes:

#### Strong truth-backed consistency
Required for:
- verified/unverified truth
- password and security-state changes
- token issue/revoke/replace truth
- refresh-token family truth
- account enable/disable truth
- replay/reuse response truth
- uniqueness decisions such as email/username where owned by Identity

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
- notification state
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
- refresh token rotation and revocation effects that change current session validity
- security-state transitions after reuse/replay detection

Policy:
- `/me` and identity self-state endpoints MUST bypass stale caches/replicas
- return from primary truth or from the just-committed write result

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
- one total global order across all identity events
- one system-wide sequence across all users or token families

**Rule:** ordering is scoped per account, per token family, per token lifecycle, or per identity-owned aggregate as needed.

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
- forgot-password / reset-password request creation and consumption
- password hash updates
- refresh token issue / revoke / replace transitions
- account enable/disable state changes
- security-state updates caused by token reuse detection

### 3.2 Atomic commit set
For security-sensitive commands, Identity MUST commit atomically:
- the primary identity truth change
- token/request lifecycle updates required by that decision
- revocation/rotation markers required by policy
- version/revision or conditional freshness markers if used by the implementation
- the Outbox record for downstream side effects/signals

Typical examples:
- register: user creation + verification request/token state + Outbox
- verify email: verified flag update + verification token consumption + Outbox
- reset password: reset token consumption + password hash update + token/session revocation (by policy) + Outbox
- refresh rotation: old token revoke + replacement link + new token insert
- refresh reuse response: family/user revocation actions + Outbox/security signal

### 3.3 Outside the transaction
The following MUST NOT be required inside the Identity truth transaction:
- email sending
- broker publish
- audit persistence in downstream stores
- Redis invalidation as a success condition
- external HTTP/API calls
- long-running abuse analysis or notification workflows
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

Identity may write:
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
- downstream replay of old identity-derived events

### 4.2 Required protections
At minimum, the design should prevent:
- duplicate user creation for the same semantic request
- double-consumption of verification/reset tokens
- inconsistent refresh token family state
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
- prevent duplicate side effects
- support safe client retries under timeout ambiguity
- avoid duplicate token issuance or duplicate email intent where harmful

### 5.2 Semantics
Rules:
- if the same `Idempotency-Key` is reused with the **same semantic request**, return the same outcome
- if reused with a **different** request payload, return a conflict response (policy-defined)

### 5.3 Storage posture
Preferred:
- durable idempotency records for requests where duplicates are harmful

Redis-only dedupe is acceptable only when:
- business impact is low
- correctness/security does not depend on it
- TTL safely covers the retry window

For high-impact security flows, durable records are preferred.

### 5.4 Replay-safe intent handling
A replayed request/event must not be confused with a legitimately new intent.

Examples:
- resend-verification should create a new logical verification intent
- forgot-password may create a new reset intent by policy
- duplicate retry of the same old request must not create multiple conflicting truth states

---

## 6) Outbox & side effects (email is async)

### 6.1 Email sending is asynchronous
Email sending must not block core Identity flows:
- endpoints return success/accepted before email delivery completes

### 6.2 Typical events
Events emitted through Outbox may include:
- `UserRegistered`
- `VerificationRequested`
- `PasswordResetRequested`
- `UserEmailVerified`
- `UserPasswordChanged`
- security signals such as replay/reuse responses where policy requires them

### 6.3 Consumer requirements
Downstream consumers must tolerate:
- at-least-once delivery
- duplicate message delivery
- provider timeout ambiguity

Email send dedupe should be by:
- `MessageId`
- or business key such as `(TemplateKey, RecipientUserId, TokenHash/TokenId)`

### 6.4 Timeout ambiguity rule
A timeout in the notification path does **not** prove:
- no email was sent
- no delivery attempt happened
- the originating Identity truth failed

Therefore Identity must treat:
- email as eventual
- delivery as investigable operational truth
- security truth as independent from notification timing

### 6.5 Outbox is the causal bridge
For Identity, Outbox is the durable bridge between:
- truth mutation
- security decision completion
- downstream notification/audit propagation

This means:
- side effects must derive from committed identity truth
- missing or delayed side effects are recoverable by replay
- side-effect timing does not redefine the security truth

### 6.6 At-least-once downstream posture
Identity assumes downstream events may be:
- duplicated
- delayed
- replayed
- reprocessed after crash
- consumed after newer truth already exists

Therefore downstream consumers must:
- dedupe
- avoid trusting arrival order alone
- resync from truth when freshness/order matters

---

## 7) Verification and reset token consistency

### 7.1 Token lifecycle truth
Verification/reset tokens must be modeled as authoritative truth with explicit lifecycle such as:
- issued
- used
- expired
- revoked (if applicable)

### 7.2 Single-use guarantee
Reset and verification token consumption must be single-use by policy.

Consumption must be atomic with the corresponding truth transition, for example:
- verify email: mark token used + set user verified
- reset password: mark token used + update password hash + revoke sessions/tokens by policy

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
- which request arrived with the later client time
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
Refresh token rotation must be atomic in the truth store:
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
- revoke token family (or all tokens for the user, by policy)
- force re-login
- emit security/audit event asynchronously
- record telemetry signal for suspicious reuse

### 9.3 Consistency requirement
Once reuse is detected and revocation is applied:
- subsequent auth decisions must reflect revocation immediately from primary truth
- delayed downstream side effects must not weaken the revocation decision

### 9.4 Cause-before-effect rule
Security effect must not lag behind its cause in a way that weakens protection.

Examples:
- if reuse is detected, revocation truth must commit before downstream alerts/audit
- a delayed email/audit path must not be mistaken for the authority that makes the revocation “real”

---

## 10) Password reset/change consistency (session safety)

### 10.1 Reset/change must cut off old session authority
When password is reset/changed:
- revoke refresh tokens by policy (recommended: revoke all tokens for the user)
- ensure old sessions cannot continue indefinitely

### 10.2 Atomicity rule
Token consumption and password update must be atomic in truth:
- mark reset token as used
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
- refresh token family truth
- durable idempotency records where used

### 11.3 Do not infer from side effects
Do not use:
- “email received / not received”
- “audit row visible / not visible”
- “cache says X”

as authority for whether the Identity truth committed.

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
- audit trails of identity actions
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
- email workflow backlog/lag (outbox age, queue depth)
- suspicious signals:
  - refresh reuse detections
  - token family revocations
  - spikes in reset requests
- durable idempotency hits/conflicts
- dedupe hits for downstream email sends
- token rotation conflicts / replay rejects / already-used token rejects
- timeout/ambiguity rates on security-sensitive endpoints where measurable
- stale/replayed security action rejection count
- cleanup/reconciliation activity for auth artifacts where used
- ownership-generation mismatch count if future ownership-sensitive workflows are introduced

Logs should include:
- `correlationId`
- account/user identity in safe form
- token identifiers only in hashed/redacted form
- outcome classification (`success`, `already-used`, `replayed`, `revoked`, `conflict`, `rate-limited`)
- no raw secrets/tokens in logs

---

## 15) Summary

Identity correctness in V1 rests on fifteen rules:

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
11. No global ordering or distributed transaction is assumed for Identity workflows.  
12. Stale derived state must never weaken fresher security truth.  
13. Replay/rebuild/remediation workflows must remain rerun-safe.  
14. Candidate derived maintenance/reporting output must be validated before publication when correctness matters.  
15. Singleton/ownership semantics are not relied on unless explicitly protected by authoritative generation/fencing rules.