# Authorization — Business Rules (V1)

This document defines the core business rules for Authorization in V1.  
It focuses on concrete rules that application code, API behavior, policy enforcement, and governance workflows must implement consistently.

Related:

- `01-api-surface.md`
- `02-domain-contracts.md`
- `03-runtime-flows.md`
- `04-errors-status-codes.md`
- `05-security-abuse-controls.md`
- `06-idempotency-consistency.md`
- `08-dependencies-and-ownership.md`

---

## 1) Governance ownership rules

- Authorization owns governance truth for:
  - roles
  - permissions
  - user-role assignments
  - role-permission grants
- Authorization is the authoritative source for the inputs from which effective permissions are computed.
- Authorization does **not** own:
  - identity/account truth
  - notification delivery truth
  - audit evidence truth
  - domain business resource truth in other modules
- Governance truth must not be redefined by cache, materialized views, reports, or downstream side effects.

---

## 2) Role rules

- Role names must be unique under canonical/normalized comparison.
- Recommended normalization rule:
  - trim
  - case-insensitive comparison
  - consistent canonical representation
- Roles support the following V1 lifecycle:
  - create
  - update metadata
  - activate
  - deactivate
- Inactive roles must not contribute to effective permissions.
- System/protected roles may be restricted from rename, deactivate, or delete operations by policy.
- Physical delete is not the normal V1 lifecycle path.

---

## 3) Permission rules

- Permission names must be unique under canonical/normalized comparison.
- Permission names must remain stable by policy.
- Recommended V1 permission naming style is domain-oriented and explicit, for example:
  - `content:publish`
  - `authz:manage`
- Permissions support the following V1 lifecycle:
  - create
  - update metadata
  - activate
  - deactivate
- Inactive permissions must not contribute to effective permissions.
- System/protected permissions may be restricted from rename, deactivate, or delete operations by policy.
- If the system adopts a seed-only permission posture, create/update operations may be restricted by policy.

---

## 4) User-role assignment rules

- Assigning a role to a user creates or maintains one active governance relationship for that `(UserId, RoleId)` pair according to implementation policy.
- Repeating the same active assignment must be idempotent.
- Duplicate assignment must not create duplicate truth rows.
- Revoked assignments must not contribute to effective permissions.
- Repeated revoke must converge safely as no-op or documented deterministic result.
- Assignments to invalid, missing, or policy-blocked targets must fail deterministically.

---

## 5) Role-permission grant rules

- Granting a permission to a role creates or maintains one active governance relationship for that `(RoleId, PermissionId)` pair according to implementation policy.
- Repeating the same active grant must be idempotent.
- Duplicate grant must not create duplicate truth rows.
- Revoked grants must not contribute to effective permissions.
- Repeated revoke must converge safely as no-op or documented deterministic result.
- Grants to invalid, missing, inactive, or policy-blocked targets must fail deterministically.

---

## 6) Effective permission rules

A user’s effective permissions are computed only from:

- active roles assigned to that user
- active permissions granted to those roles
- current authoritative governance truth

The following must **not** grant effective authority:

- inactive roles
- inactive permissions
- revoked assignments
- revoked grants
- stale derived snapshots
- delayed or replayed older governance state

Effective permission reads may be accelerated by cache/materialization, but authoritative truth wins whenever freshness matters.

---

## 7) Admin policy coverage rules

- 100% of `/api/v1/admin/*` endpoints must enforce explicit authorization policies.
- Authorization posture is deny-by-default.
- Bearer authentication alone is not sufficient.
- Client-supplied roles/permissions are never authoritative.
- Object-level checks, when needed, must compose with centralized authorization instead of replacing it.

---

## 8) Governance write rules

Governance-changing operations include at minimum:

- create/update role
- create/update permission
- activate/deactivate role
- activate/deactivate permission
- assign/revoke role
- grant/revoke permission

For all governance-changing writes:

- success means Authorization truth committed
- where async side effects are required, success also means async intent/outbox committed
- success does **not** mean:
  - audit is already queryable
  - cache invalidation already propagated
  - materialized views already rebuilt
  - reports already updated
  - optional downstream notifications already completed

---

## 9) Idempotency rules

The following operations should converge safely under retry:

- assign role
- revoke role
- grant permission
- revoke permission
- activate role
- deactivate role
- activate permission
- deactivate permission

Rules:

- same semantic write repeated must converge to one correct truth state
- logically unchanged truth must not emit duplicate harmful downstream side effects
- timeout does not prove mutation failure
- reconciliation after timeout must come from Authorization truth

The implementation must not mix:

- sometimes no-op success
- sometimes conflict

for the same semantic operation without documenting that rule explicitly.

---

## 10) Conflict and protected-invariant rules

Deterministic conflict handling is required for cases such as:

- duplicate role name
- duplicate permission name
- protected system role mutation
- protected system permission mutation
- invalid lifecycle transition
- optimistic concurrency conflict where used
- aggregate/protected governance invariants where policy blocks mutation

If the system has stronger governance invariants, for example:

- at least one active admin must remain
- protected role must retain minimum permission set

those rules must be explicitly designed and enforced rather than assumed informally.

---

## 11) Policy evaluation rules

- Authorization decisions on security-sensitive/admin paths must be truth-first.
- If evaluation uses cache/materialized inputs:
  - they are acceleration only
  - they are not authority
- If evaluation state is stale, missing, or uncertain on a security-sensitive path:
  - fall back to authoritative truth
  - or fail closed
- Uncertainty must not become implicit allow.

---

## 12) Async event rules

Authorization emits governance events only after truth commit.

Typical V1 events include:

- `Authz.RoleCreated`
- `Authz.RoleUpdated`
- `Authz.RoleActivated`
- `Authz.RoleDeactivated`
- `Authz.PermissionCreated`
- `Authz.PermissionUpdated`
- `Authz.PermissionActivated`
- `Authz.PermissionDeactivated`
- `Authz.UserRoleAssigned`
- `Authz.UserRoleRevoked`
- `Authz.RolePermissionGranted`
- `Authz.RolePermissionRevoked`

Rules:

- emitted events must carry stable `MessageId`
- payloads must be minimal and privacy-aware
- duplicate delivery, replay, and delay are normal downstream conditions
- downstream consumers must be idempotent
- replay of older governance-derived state must not weaken newer truth

---

## 13) Read-after-write and consistency rules

- Admin governance reads must satisfy read-your-writes expectations.
- Immediate post-write confirmation must use authoritative truth.
- Stale cache/materialization must not be trusted blindly for admin confirmation flows.
- Governance truth is strongly authoritative at commit time.
- Audit, reporting, cache propagation, and optional notifications are eventual.

---

## 14) Batch / reconciliation / reporting rules

Authorization may run bounded workflows for:

- mapping reconciliation
- drift detection
- derived permission snapshot rebuild
- governance reporting
- candidate publication/cutover for important derived outputs

These workflows must:

- remain subordinate to current Authorization truth
- be safe under rerun/replay
- not redefine user-role or role-permission truth
- not override fresher truth with stale derived output
- use candidate-before-publication where correctness matters

If rebuild is safer than fragile incremental repair, rebuild is preferred.

---

## 15) Security and abuse rules

- All admin endpoints require Bearer auth.
- All admin endpoints require explicit authorization policy.
- Protected governance objects require stronger policy where applicable.
- Admin mutation endpoints should be protected against:
  - automation loops
  - retry storms
  - repeated equivalent writes
  - bulk mutation mistakes
  - admin token misuse
- Logs must not leak sensitive internals unnecessarily.
- Useful audit/ops signals include:
  - repeated denies
  - repeated protected-object mutation attempts
  - unusual no-op/idempotent-hit rates
  - spikes in `403`, `409`, or `5xx`
  - stale-evaluation fail-closed events

---

## 16) Truth vs derived rules

The following are live governance truth:

- roles
- permissions
- assignments
- grants
- enforced active/inactive state
- revision/version markers where used for freshness

The following are derived only:

- permission snapshots
- governance summaries
- drift reports
- materialized policy views
- reporting outputs
- downstream audit evidence
- optional governance notifications

Derived outputs may be useful, but they must never outrank current Authorization truth.

---

## 17) Business rules summary

1. Authorization owns governance truth for roles, permissions, assignments, and grants.  
2. Role and permission names must be unique under canonical normalization rules.  
3. Roles and permissions use activate/deactivate as the normal V1 lifecycle.  
4. Inactive or revoked governance relationships must not grant effective authority.  
5. Admin APIs must be deny-by-default and fully policy-protected.  
6. Governance writes are truth-first and must not wait for downstream completion.  
7. Assignment, grant, revoke, and lifecycle actions must converge safely under retry.  
8. Timeout ambiguity must be reconciled from Authorization truth.  
9. Policy evaluation on security-sensitive paths must prefer truth or fail closed.  
10. Governance events are post-commit, minimal, and replay-safe.  
11. Replay of stale governance-derived state must not weaken newer truth.  
12. Derived outputs are subordinate to live Authorization truth.