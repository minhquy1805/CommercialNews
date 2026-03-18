# Authorization — Dependencies & Ownership (V1)

## 1) Ownership boundaries

Authorization is the single source of truth for:
- roles
- permissions
- user-role assignments
- role-permission grants
- policy enforcement boundary
- derived governance snapshots/reports only as subordinate outputs

Authorization does **not** own:
- user identity truth
- audit evidence truth
- notification truth
- domain business truth in Content/SEO/Media/Reading/Interaction

---

## 2) Allowed dependencies

- Identity (`UserId` reference only)
- Audit (async consumer via events)
- optional cache/materialization consumers for policy acceleration
- bounded reconciliation/reporting workflows may consume Authorization truth and Authorization-owned terminal governance artifacts

No synchronous dependency on other domain modules is required for core governance truth.

---

## 3) Forbidden dependencies

- No cross-module DB queries into Content/SEO/Media/etc. for core authz truth.
- Domain modules must not embed their own role/permission storage.
- Authorization must not mutate another module’s truth because it is physically reachable.
- Reconciliation/reporting workflows must not redefine current enforceable governance truth.
- Partial derived governance outputs must not be treated as authoritative policy truth.

---

## 4) Contract rule

- All admin endpoints must map to explicit permission/policy names.
- Policy naming/versioning is an ADR concern to prevent drift.
- Authorization evaluation may consume bounded contextual inputs, but governance truth remains Authorization-owned.

---

## 5) Truth vs derived ownership

### Truth owned by Authorization
- roles and permissions
- user-role truth
- role-permission truth
- revision/version markers used for freshness where implemented

### Derived outputs Authorization may own
Authorization may own derived outputs such as:
- permission snapshots
- governance summaries
- drift reports
- orphan mapping reports
- reconciliation candidate outputs
- cache/materialization artifacts

These must remain:
- explicitly documented as derived
- subordinate to Authorization truth
- rebuildable or reproducible where practical
- observable
- safe under rerun/replay

---

## 6) Batch / reconciliation / reporting ownership rules

Authorization batch-light workflows may:
- reconcile mapping integrity
- rebuild derived permission snapshots/materializations
- detect drift between truth and derived outputs
- generate governance reports
- clean workflow-private temporary state

Authorization batch-light workflows must not:
- redefine user-role or role-permission truth
- grant access based on stale reports/snapshots
- override fresher truth with stale derived output
- assume exclusive ownership without explicit coordination semantics

---

## 7) Publication and cutover ownership

If Authorization publishes an important derived governance output, Authorization owns:
- candidate generation
- candidate validation
- publication/cutover policy
- freshness signals
- rerun/rebuild policy

But Authorization still does not own:
- audit evidence truth
- identity truth
- other modules’ business truth

---

## 8) Coordination / ownership-sensitive workflow rule

Authorization normally prefers:
- truth-store authority
- uniqueness constraints
- deterministic idempotent command handling
- fail-closed evaluation
- bounded reconciliation/reporting
- rerun-safe derived workflows

If a future workflow truly requires exclusive ownership
(for example one-current-owner permission materialization rebuild or governance repair worker),
then it must follow system-wide coordination rules:
- explicit ownership source
- generation/fencing token
- resource-side stale-owner rejection

Naive leader/lock assumptions are forbidden.

---

## 9) V2 evolution

Authorization may later evolve toward:
- richer ABAC materializations
- derived permission snapshots at larger scale
- stronger governance drift detection
- more formalized reporting and repair workflows

If that happens, the architecture must keep explicit:
- what remains live governance truth
- what is derived policy output
- how publication/cutover works for important derived outputs
- how reconciliation preserves truth-first and fail-closed behavior