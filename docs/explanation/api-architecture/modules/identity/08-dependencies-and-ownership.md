# Identity — Dependencies & Ownership (V1)

Related:
- `../../../../architecture/arc42/03-building-blocks-modularity.md`
- `../../../../architecture/arc42/13-transactions-and-consistency-v1.md`
- `../../../../architecture/arc42/18-stream-processing-and-derived-state-v1.md`
- `../../../../architecture/arc42/19-stream-processing-runtime-v1.md`
- `../../../../decisions/adr-0013-outbox-and-delivery-semantics-v1.md`
- `../../../../decisions/adr-0027-stream-processing-and-derived-state-policy-v1.md`
- `../../../../decisions/adr-0028-consumer-idempotency-replay-and-rebuild-policy-v1.md`

---

## 1) Ownership

Identity owns:
- account and credential lifecycle
- session management (refresh tokens, logout)
- verification and password reset tokens/policy
- current account security truth
- cleanup/reconciliation/retention workflows for Identity-owned auth artifacts

Identity does **not** own:
- notification delivery truth
- audit evidence truth
- authorization truth
- reporting/dashboard truth in other modules

**Rule:** Identity owns **live security truth**, not downstream delivery/evidence/reporting truth.

---

## 2) Allowed dependencies

- Notifications (async only, via events)
- Audit (async only, via events/policy)
- Authorization (for admin endpoints outside Identity; Identity itself should not embed domain authz logic)
- bounded cleanup/reporting workflows may consume Identity truth and Identity-owned terminal artifacts

### 2.1 Allowed dependency shapes
Approved interaction patterns are:
- sync truth mutation inside Identity boundary
- async event emission after truth commit
- async notification/audit consumers
- bounded cleanup/reconciliation/reporting over Identity-owned truth and terminal artifacts
- read-only use of external identifiers where explicitly allowed by policy

### 2.2 Dependency rule
No synchronous dependency on Notifications or Audit is required for core security truth.

---

## 3) Forbidden dependencies

- Identity must not depend synchronously on Notifications (email delivery).
- Other modules must not query Identity tables directly; they reference `UserId` only.
- Identity must not mutate another module’s truth because it is physically reachable.
- Cleanup/reporting workflows must not redefine current account or token-validity truth.
- Partial maintenance/report outputs must not be treated as authoritative security truth.
- Identity must not trust stale delivery/report/cache state as authority for:
  - verification truth
  - password/reset truth
  - refresh token family truth
  - account enabled/disabled state

---

## 4) Data access rule

Shared DB, not shared schema:
- Identity tables are owned by Identity.
- Read-only access by other modules is forbidden unless explicitly allowed by policy.

### 4.1 Practical consequence
Other modules may:
- reference `UserId`
- consume identity events
- read through explicit APIs/contracts if allowed

Other modules may not:
- directly inspect refresh-token truth
- directly inspect verification/reset-token truth
- infer account security state from raw Identity tables
- maintain shadow copies of canonical identity truth as if they were authoritative

---

## 5) Truth vs derived ownership

### 5.1 Truth owned by Identity
- account enabled/disabled state
- verified/unverified state
- password/security truth
- verification/reset token lifecycle
- refresh token / token-family lifecycle

### 5.2 Derived outputs Identity may own
Identity may own derived outputs such as:
- cleanup candidate sets
- expired-token reports
- suspicious-auth summaries
- login-history summaries or archives
- reconciliation reports for terminal auth artifacts

These must remain:
- explicitly documented as derived
- subordinate to current identity truth
- rebuildable or reproducible where practical
- observable
- safe under rerun

### 5.3 Ownership consequence
A derived suspicious-auth summary, cleanup list, or archival report may help:
- operations
- security hygiene
- reporting
- maintenance planning

It does **not** become:
- live token-validity authority
- verified-state authority
- session-validity authority
- password/security authority

---

## 6) Async side-effect ownership rule

### 6.1 Identity owns the cause
Identity owns:
- registration truth
- verification truth
- reset truth
- refresh/session truth
- reuse/replay response truth

### 6.2 Notifications owns delivery truth
Notifications owns:
- email send attempts
- dedupe outcome for sends
- retry/dead state
- provider-side operational delivery status

### 6.3 Audit owns evidence truth
Audit owns:
- append-only evidence of identity actions
- investigation-ready persistence
- completeness/replay over audit evidence

### 6.4 Ownership consequence
A successful Identity action means:
- Identity truth committed
- outbox/event intent committed where applicable

It does **not** mean:
- email has already been delivered
- audit evidence is already queryable
- derived maintenance/report outputs are already current

---

## 7) Batch / cleanup / reconciliation ownership rules

Identity maintenance workflows may:
- clean expired verification/reset artifacts
- clean revoked/expired refresh tokens
- produce auth hygiene reports
- reconcile orphan or terminal auth artifacts
- archive old auth-maintenance outputs where policy allows

Identity maintenance workflows must not:
- decide live token validity ahead of truth checks
- override verified/password/session truth
- weaken security by deleting artifacts still required by policy
- assume exclusive ownership without explicit coordination semantics
- publish stale maintenance output over fresher live security truth

### 7.1 Recovery posture
If derived maintenance outputs are unhealthy:
- Identity truth remains authoritative
- reconciliation/rebuild is a recovery mechanism
- live security decisions remain truth-first
- safe non-progress is preferable to stale security weakening

---

## 8) Evaluation ownership rule

### 8.1 Identity owns final security-state evaluation
Identity owns final truth for:
- whether a token is valid
- whether a reset/verification intent is usable
- whether a refresh token has been replaced/revoked
- whether an account is verified/enabled
- whether a reuse/replay response has already invalidated session truth

### 8.2 Derived acceleration is subordinate
Rate-limit state, caches, summaries, and maintenance outputs may assist operations, but they must not become hidden authority for live security decisions.

### 8.3 Security-sensitive uncertainty rule
If a derived view is:
- stale
- missing
- ambiguous
- inconsistent with current truth markers

then Identity must:
- read from authoritative truth
- or reject/fail safely on the security-sensitive path

---

## 9) Coordination / ownership-sensitive workflow rule

Identity normally prefers:
- truth-store authority
- conditional updates
- durable idempotency
- replay-safe async side effects
- bounded cleanup/reconciliation

If a future workflow truly requires exclusive ownership
(for example one-current-owner token-family repair or security maintenance worker),
then it must follow system-wide coordination rules:
- explicit ownership source
- generation/fencing token
- resource-side stale-owner rejection

Naive leader/lock assumptions are forbidden.

### 9.1 Ownership ambiguity rule
If ownership is ambiguous for a correctness-sensitive maintenance or repair workflow:
- delayed cleanup is acceptable
- stale-owner rejection is acceptable
- operator retry is acceptable
- truth-first live security evaluation is acceptable

Unsafe stale security overwrite is not acceptable.

---

## 10) Module dependency posture summary

### 10.1 What Identity may expect from others
Identity may expect:
- Notifications to consume events asynchronously
- Audit to consume events asynchronously
- Authorization to remain a separate governance truth owner
- cleanup/reconciliation/reporting to be normal operational tools for derived outputs

### 10.2 What others may expect from Identity
Other modules may expect:
- stable `UserId` identity
- authoritative account/security truth via explicit contracts
- async event emission after truth commit
- truth-first handling of security-sensitive state
- no dependence on delivery/audit/report completion for security success

### 10.3 What nobody may assume
No module may assume:
- email delivery truth is the same as account/security truth
- audit evidence is the same as identity truth
- a maintenance report is stronger than live token/account truth
- one current worker/leader is safe without explicit authoritative coordination
- shadow copies of Identity data are authoritative on security-sensitive paths

---

## 11) V2 evolution

Identity may later evolve toward:
- stronger session/device management
- richer suspicious-activity detection
- more formalized login-history archival/reporting
- stronger security maintenance workflows

If that happens, the architecture must keep explicit:
- what remains live identity truth
- what is derived/maintenance output
- how cleanup/reconciliation preserves truth-first security
- how operational summaries avoid becoming hidden security authority

### 11.1 V2 constraint that remains unchanged
Even if Identity becomes richer:
- live account/token/security truth still belongs to Identity
- delivery truth still belongs to Notifications
- evidence truth still belongs to Audit
- derived maintenance outputs still remain subordinate to live security truth