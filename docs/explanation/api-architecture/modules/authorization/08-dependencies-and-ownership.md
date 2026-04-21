# Authorization — Dependencies & Ownership (V1)

Related:

- `../../../../architecture/arc42/03-building-blocks-modularity.md`
- `../../../../architecture/arc42/13-transactions-and-consistency-v1.md`
- `../../../../architecture/arc42/18-stream-processing-and-derived-state-v1.md`
- `../../../../architecture/arc42/19-stream-processing-runtime-v1.md`
- `../../../../decisions/adr-0013-outbox-and-delivery-semantics-v1.md`
- `../../../../decisions/adr-0020-timeout-retry-and-failure-detection-policy-v1.md`
- `../../../../decisions/adr-0027-stream-processing-and-derived-state-policy-v1.md`
- `../../../../decisions/adr-0028-consumer-idempotency-replay-and-rebuild-policy-v1.md`

---

## 1) Ownership boundaries

Authorization is the single source of truth for:

- roles
- permissions
- user-role assignments
- role-permission grants
- the authoritative inputs from which effective permissions are computed
- the policy enforcement boundary for governance authorization
- derived governance snapshots/reports only as subordinate outputs

Authorization does **not** own:

- user identity truth
- audit evidence truth
- notification delivery truth
- domain business truth in Content / SEO / Media / Reading / Interaction
- downstream cache/materialization truth

**Rule:** Authorization owns **live governance truth**, not downstream evidence, delivery, or reporting truth.

---

## 2) Allowed dependencies

Authorization may rely on the following dependency shapes:

- Identity *(reference to `UserId` and explicit identity contracts only)*
- local/shared Outbox write in the same transaction as governance truth changes
- Audit *(async consumer via events)*
- optional cache/materialization consumers for policy acceleration
- optional governance notification consumers if introduced by policy
- bounded reconciliation/reporting workflows over Authorization-owned truth and terminal artifacts

No synchronous dependency on other domain modules is required for core governance truth.

### 2.1 Identity boundary

Authorization may reference identity subjects through:

- stable `UserId`
- explicit API/contract boundaries where required

Authorization must not treat Identity internal tables as its own domain truth surface.

### 2.2 Async downstream consumers

Approved downstream consumers include:

- Audit
- cache/materialization refreshers
- governance reporting/reconciliation consumers
- optional governance notification consumers

### 2.3 Dependency rule

Authorization commits truth first, then emits async intent.  
It does not synchronously call downstream modules to make governance truth valid.

---

## 3) Forbidden dependencies

- No cross-module DB queries into Content / SEO / Media / Reading / Interaction for core authorization truth.
- Domain modules must not embed their own canonical role/permission storage.
- Authorization must not mutate another module’s truth because it is physically reachable.
- Reconciliation/reporting workflows must not redefine current enforceable governance truth.
- Partial derived governance outputs must not be treated as authoritative policy truth.
- Authorization must not trust stale cache/materialized state as authority on security-sensitive admin/governance paths.
- Optional downstream notifications, if any, must not be treated as prerequisites for governance success.

---

## 4) Contract rule

- All admin endpoints must map to explicit permission/policy names.
- Policy naming/versioning is an ADR concern to prevent drift.
- Authorization evaluation may consume bounded contextual inputs, but governance truth remains Authorization-owned.
- Security-sensitive evaluation must prefer authoritative truth over stale derived state.
- If uncertainty cannot be resolved safely, evaluation must fail closed or fall back to authoritative truth.
- Governance-changing writes should emit async intent with stable `MessageId` through the standard outbox/post-commit path.

---

## 5) Truth vs derived ownership

### 5.1 Truth owned by Authorization

- roles and permissions
- user-role truth
- role-permission truth
- revision/version markers used for freshness where implemented
- protected invariants around enforceable governance state

### 5.2 Derived outputs Authorization may own

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

### 5.3 Ownership consequence

Derived outputs may help:

- performance
- diagnostics
- governance visibility
- repair and reconciliation workflows

They do **not** become:

- live role/permission truth
- assignment/grant truth
- effective authority stronger than current Authorization truth

---

## 6) Async side-effect ownership rule

### 6.1 Authorization owns the cause

Authorization owns:

- role/permission lifecycle truth
- user-role assignment truth
- role-permission grant truth
- governance mutation legality
- policy evaluation inputs derived from committed governance truth

### 6.2 Outbox ownership posture

Authorization writes async intent through Outbox as part of its local truth transaction.

Outbox is the causal bridge between:

- committed governance truth
- downstream audit/materialization/notification propagation

Outbox is not governance truth itself, and it is not audit truth.

### 6.3 Audit owns evidence truth

Audit owns:

- append-only evidence of governance actions
- investigation-ready persistence
- replay/completeness over audit evidence

### 6.4 Optional notification ownership

If governance-related notifications exist, Notifications owns:

- delivery workflow
- send attempts
- retry/remediation/delivery state
- provider-side operational delivery truth

Authorization does not depend on Notifications for governance truth.

### 6.5 Ownership consequence

A successful Authorization action means:

- governance truth committed
- async intent/outbox committed where applicable

It does **not** mean:

- audit evidence is already queryable
- cache/materialization is already current
- reports already reflect the change
- optional downstream notification has completed

---

## 7) Batch / reconciliation / reporting ownership rules

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

### 7.1 Recovery posture

If derived governance outputs are unhealthy:

- Authorization truth remains authoritative
- reconciliation/rebuild is a recovery mechanism
- truth-first evaluation remains live
- safe non-progress is preferable to stale governance weakening

---

## 8) Publication and cutover ownership

If Authorization publishes an important derived governance output, Authorization owns:

- candidate generation
- candidate validation
- publication/cutover policy
- freshness signals
- rerun/rebuild policy

But Authorization still does not own:

- audit evidence truth
- identity truth
- notification delivery truth
- other modules’ business truth

---

## 9) Coordination / ownership-sensitive workflow rule

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

### 9.1 Ownership ambiguity rule

If ownership is ambiguous for a correctness-sensitive maintenance, rebuild, or repair workflow:

- delayed cleanup/rebuild is acceptable
- stale-owner rejection is acceptable
- operator retry is acceptable
- truth-first live evaluation is acceptable

Unsafe stale governance overwrite is not acceptable.

---

## 10) Module dependency posture summary

### 10.1 What Authorization may expect from others

Authorization may expect:

- Identity to provide stable subject identity via explicit contracts
- Audit to consume governance events asynchronously
- optional cache/materialization consumers to follow committed governance truth
- optional governance notification consumers to remain downstream by policy
- reconciliation/reporting to be normal operational tools for derived outputs

### 10.2 What others may expect from Authorization

Other modules may expect:

- authoritative governance truth for roles and permissions
- authoritative assignment/grant truth via explicit contracts
- async event emission after truth commit
- stable `MessageId` on emitted governance events
- truth-first handling of security-sensitive governance evaluation
- no dependence on audit/cache/report/notification completion for governance success

### 10.3 What nobody may assume

No module may assume:

- audit evidence is the same as governance truth
- notification delivery truth is the same as governance truth
- a materialized snapshot is stronger than live Authorization truth
- one current worker/leader is safe without explicit authoritative coordination
- shadow copies of Authorization data are authoritative on security-sensitive paths

---

## 11) V2 evolution

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

### 11.1 V2 constraint that remains unchanged

Even if Authorization becomes richer:

- live governance truth still belongs to Authorization
- identity truth still belongs to Identity
- delivery truth still belongs to Notifications
- evidence truth still belongs to Audit
- derived governance outputs still remain subordinate to live Authorization truth