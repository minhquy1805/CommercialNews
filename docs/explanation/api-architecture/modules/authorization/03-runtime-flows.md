# Authorization — Runtime Flows (V1)

This module supports arc42 Scenario 6: governance action + audit recorded.

Related:

- `../../../../architecture/arc42/04-runtime-view-v1.md`
- `../../../../architecture/arc42/13-transactions-and-consistency-v1.md`
- `../../../../architecture/arc42/16-batch-processing-and-derived-data-v1.md`
- `../../../../architecture/arc42/17-dataflow-and-batch-workflows-v1.md`
- `../../../../architecture/arc42/18-stream-processing-and-derived-state-v1.md`
- `../../../../architecture/arc42/19-stream-processing-runtime-v1.md`
- `../../../../decisions/adr-0013-outbox-and-delivery-semantics-v1.md`
- `../../../../decisions/adr-0020-timeout-retry-and-failure-detection-policy-v1.md`
- `../../../../decisions/adr-0027-stream-processing-and-derived-state-policy-v1.md`
- `../../../../decisions/adr-0028-consumer-idempotency-replay-and-rebuild-policy-v1.md`

---

## Runtime posture in V1

Authorization primarily participates in three runtime lanes:

### A) Synchronous truth lane

Used for:

- assign/revoke role
- grant/revoke permission
- create/update governance reference data where supported
- activate/deactivate roles and permissions
- evaluate policy against current authoritative governance truth

### B) Async side-effect lane

Used for:

- audit emission and ingestion
- optional cache invalidation / policy materialization updates
- optional downstream governance reporting/update signals
- optional governance-related notification consumers if introduced by policy

### C) Batch / reconciliation / reporting lane

Used for:

- reconcile user-role / role-permission mappings
- detect orphan or inconsistent governance artifacts
- produce governance summaries and reports
- rebuild derived authorization materializations or snapshots if introduced
- cleanup workflow-private temporary state

### Core runtime rules

**Rule:** Authorization owns governance truth.  
Audit, cache/materialization, notifications, and governance reports are downstream or derived.

**Rule:** Authorization success is defined by **truth commit**.  
When async side effects are required, success means:
- governance truth committed
- async intent/outbox committed

It does **not** mean:
- audit is already queryable
- cache invalidation already propagated
- materialized views already rebuilt
- optional downstream notifications already completed

**Rule:** Authorization-derived async processing is assumed **at-least-once**.  
Duplicates, replay, lag, and worker restart must be tolerated safely.

**Rule:** Security-sensitive governance decisions must reconcile from authoritative truth, not from stale derived views or downstream visibility.

---

## Flow A — Admin assigns role to user

### Goal

Apply governance truth immediately and make it enforceable without waiting for downstream audit or materialization.

### Truth flow

1. Admin calls `POST /api/v1/admin/authz/users/{userId}/roles`.
2. API enforces required policy.
3. Authorization validates assignment legality.
4. Authorization persists assignment idempotently.
5. Authorization writes async intent/outbox in the same truth transaction.

### Async event emitted

Authorization emits:

- `Authz.UserRoleAssigned`

### Event semantics

- emitted only after truth commit
- carries stable `MessageId`
- minimal and privacy-aware
- safe under duplicate delivery, replay, and worker restart

### Downstream flow

6. Audit worker ingests the governance event.
7. Optional downstream cache/materialization refreshers ingest it.
8. Optional downstream governance notifications, if policy introduces them, remain subordinate side effects.

### Runtime stream semantics

- `Authz.UserRoleAssigned` is a truth-following governance event
- duplicate delivery must not create duplicate audit evidence
- replay after worker crash or backlog recovery is normal
- governance truth is already active before downstream audit or materialization catches up

### Failure modes

- audit ingestion delayed:
  - assignment still applies immediately
- cache/materialization lag:
  - truth-backed evaluation remains authoritative
- timeout during assignment:
  - reconcile from Authorization truth, not from audit visibility
- duplicate request:
  - converge to one final truth outcome
- duplicate event delivery:
  - must not create duplicate audit rows or duplicate harmful derived outputs

### Batch / reconciliation hooks

Governance reconciliation jobs may later verify:

- expected assignment presence
- orphan or inconsistent mappings
- drift between truth and derived governance views

### Runtime rules

- Authorization truth defines the effective assignment
- audit is evidence, not enforcement
- derived policy views may lag, but protected evaluation paths must remain truth-safe

---

## Flow B — Admin grants permission to role

### Goal

Change role-permission truth safely and make policy evaluation observe it immediately.

### Truth flow

1. Admin calls `POST /api/v1/admin/authz/roles/{roleId}/permissions`.
2. Required policy is enforced.
3. Authorization validates legality and protection rules.
4. Grant is persisted idempotently.
5. Authorization writes async intent/outbox in the same truth transaction.

### Async event emitted

Authorization emits:

- `Authz.RolePermissionGranted`

### Runtime stream semantics

- `Authz.RolePermissionGranted` is a canonical governance event after truth commit
- duplicate grant requests and duplicate downstream deliveries must converge safely
- logically unchanged truth must not create duplicate harmful downstream effects
- replay is normal under at-least-once delivery

### Failure modes

- audit ingestion delayed:
  - grant still applies immediately
- duplicate grant request:
  - must not create duplicate truth rows or duplicate meaningful downstream effects
- timeout ambiguity:
  - reconcile from Authorization truth
- derived permission snapshots lag:
  - truth remains authoritative
- replay/remediation:
  - must not create duplicate derived permission materialization facts

### Batch / reconciliation hooks

Bounded jobs may verify:

- mapping integrity
- expected permission coverage
- drift between truth and derived snapshots/materializations

### Runtime rules

- role-permission truth is effective at commit time
- audit/reporting lag does not weaken the grant
- derived governance outputs remain subordinate to Authorization truth

---

## Flow C — Revoke role / revoke permission

### Goal

Remove governance authority deterministically and have evaluation reflect the change immediately.

### Truth flow

1. Admin calls revoke endpoint.
2. Required policy is enforced.
3. Authorization validates legality and persists revoke.
4. Authorization writes async intent/outbox in the same truth transaction.

### Async event emitted

Authorization emits one of:

- `Authz.UserRoleRevoked`
- `Authz.RolePermissionRevoked`

### Runtime stream semantics

- revocation is ordering-sensitive for enforcement, but not globally ordered across the whole system
- downstream consumers must not let older grant views override fresher revoke truth
- replay of older governance-derived state must not reintroduce already-revoked authority in derived outputs

### Failure modes

- delayed audit:
  - must not delay governance truth
- repeated revoke:
  - should converge safely as no-op or stable documented result
- stale cache/materialization:
  - must not continue granting dangerous access on critical paths
- old replayed grant-derived state:
  - must not resurrect revoked authority in derived views

### Runtime rules

- revoke is truth-first
- fail-closed posture applies if derived state is stale or uncertain
- downstream consumers must treat governance truth as authoritative

---

## Flow D — Policy evaluation

### Goal

Make authorization decisions against current enforceable governance truth.

### Evaluation flow

1. API receives a protected request.
2. Authorization evaluates policy using current truth and allowed evaluation inputs.
3. If caches/materializations are available and trustworthy by policy, they may accelerate evaluation.
4. If evaluation state is stale or uncertain on a security-sensitive path:
   - fall back to authoritative truth
   - or fail closed
5. Decision is returned to the API layer.

### Failure modes

- cache stale or unavailable:
  - do not grant by guess
  - fail closed or read authoritative truth
- timeout/ambiguity in derived path:
  - do not convert uncertainty into allow
- derived materialization lag:
  - must not weaken protected admin/governance paths
- replay lag in derived authorization snapshots:
  - must not be treated as proof of current permission truth

### Runtime rules

- evaluation correctness is truth-first
- derived policy views are acceleration only
- client-supplied roles/permissions are never authoritative truth
- security-sensitive uncertainty resolves to deny/fail-closed, not optimistic allow

---

## Flow E — Governance reconciliation / reporting workflow

### Goal

Detect drift, produce reports, and repair derived governance outputs without redefining truth.

### Typical workflow shape

1. Select bounded governance truth input:
   - users/roles/permissions window
   - changed-since checkpoint
   - suspect aggregate set
2. Re-read authoritative Authorization truth.
3. Build reconciliation or reporting candidate output.
4. Compare candidate with derived state if present.
5. Publish or store derived report/materialization according to policy.
6. Record completion and cleanup.

### Typical outputs

- governance summaries
- drift reports
- orphan mapping reports
- permission snapshot rebuilds
- policy materialization repair candidates

### Rules

- truth remains in Authorization store
- reports/snapshots are derived
- partial candidate output must not masquerade as authoritative governance truth
- rerun on the same bounded input must remain safe
- if a full rebuild is safer than fragile incremental repair, rebuild is preferred

### Failure modes

- report/materialization lag:
  - governance truth remains correct
- candidate publication failure:
  - previous active derived output remains if one exists
- overlapping replay/reconciliation runs:
  - must remain safe under dedupe, bounded input, and cutover rules

---

## Flow F — Truth-safe governance under derived lag

### Goal

Ensure security-sensitive decisions remain correct even when derived authorization outputs lag.

### Typical runtime shape

1. Protected request arrives.
2. Evaluation may consult a cache/materialized view first if policy allows.
3. If derived state is missing, stale, or uncertain:
   - system falls back to authoritative Authorization truth
   - or fails closed on security-sensitive paths
4. Final decision is returned only after truth-safe evaluation.

### Examples

- stale role snapshot still shows revoked role
- delayed permission materialization misses a recent grant
- replay/backfill has not yet repaired a derived governance view
- cache invalidation lag leaves old role memberships visible in a convenience layer

### Rules

- Authorization truth wins over derived governance views
- security-sensitive uncertainty must not become implicit allow
- safe deny/fail-closed beats stale convenience every time

---

## Summary

Authorization runtime in V1 is governed by the following rules:

1. Governance mutations commit Authorization truth first.  
2. Successful writes mean truth committed, and where needed, async intent/outbox committed.  
3. Audit is downstream evidence and must not block governance success.  
4. Assignment/grant/revoke commands must converge deterministically.  
5. Authorization-derived async processing is at-least-once; duplicates and replay are normal.  
6. Governance events use stable `MessageId` and are emitted only after truth commit.  
7. Policy evaluation must prefer authoritative truth over stale convenience.  
8. If uncertain on a security-sensitive path, fail closed.  
9. Replay of older governance-derived state must not weaken newer truth.  
10. Reconciliation/reporting workflows may depend on governance truth, but do not redefine it.  
11. Derived governance outputs must be bounded, observable, and safe under rerun.  
12. Safe deny and truth fallback are preferable to stale allow.