# Authorization — Dependencies & Ownership (V1)

Related:
- `../../../../architecture/arc42/03-building-blocks-modularity.md`
- `../../../../architecture/arc42/13-transactions-and-consistency-v1.md`
- `../../../../architecture/arc42/18-stream-processing-and-derived-state-v1.md`
- `../../../../architecture/arc42/19-stream-processing-runtime-v1.md`
- `../../../../decisions/adr-0013-outbox-and-delivery-semantics-v1.md`
- `../../../../decisions/adr-0016-authorization-model-rbac-abac-policies-v1.md`
- `../../../../decisions/adr-0027-stream-processing-and-derived-state-policy-v1.md`
- `../../../../decisions/adr-0028-consumer-idempotency-replay-and-rebuild-policy-v1.md`

---

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

**Rule:** Authorization owns **who may do what**.  
It does not own the underlying business entities or downstream evidence of governance changes.

---

## 2) Allowed dependencies

- Identity (`UserId` reference only)
- Audit (async consumer via events)
- optional cache/materialization consumers for policy acceleration
- bounded reconciliation/reporting workflows may consume Authorization truth and Authorization-owned terminal governance artifacts

No synchronous dependency on other domain modules is required for core governance truth.

### 2.1 Allowed dependency shapes

Approved interaction patterns are:

- **sync truth mutation inside Authorization boundary**
- **server-side policy evaluation against Authorization-owned truth**
- **async emission of governance events after truth commit**
- **async audit and invalidation/materialization consumers**
- **bounded rebuild/reconciliation/reporting over Authorization truth and Authorization-owned derived artifacts**
- **read-only reference to Identity identifiers**, not Identity business truth mutation

Not approved:
- synchronous dependency on downstream audit/reporting for governance success
- cross-module truth ownership leakage
- hidden reliance on derived permission snapshots as if they were canonical truth

---

## 3) Forbidden dependencies

- No cross-module DB queries into Content/SEO/Media/etc. for core authz truth.
- Domain modules must not embed their own role/permission storage.
- Authorization must not mutate another module’s truth because it is physically reachable.
- Reconciliation/reporting workflows must not redefine current enforceable governance truth.
- Partial derived governance outputs must not be treated as authoritative policy truth.
- Authorization must not trust stale caches/materializations as final authority on security-sensitive paths.
- Authorization must not depend on audit persistence to make governance changes effective.

---

## 4) Contract rule

- All admin endpoints must map to explicit permission/policy names.
- Policy naming/versioning is an ADR concern to prevent drift.
- Authorization evaluation may consume bounded contextual inputs, but governance truth remains Authorization-owned.

### 4.1 Contract ownership consequence

Authorization owns:
- permission naming discipline
- role/permission assignment truth
- evaluation semantics at the policy boundary

Other modules own:
- their own business truth
- the protected resources themselves
- optional contextual attributes provided to policy evaluation

No module may silently redefine Authorization policy semantics in its own local storage.

---

## 5) Truth vs derived ownership

### 5.1 Truth owned by Authorization
- roles and permissions
- user-role truth
- role-permission truth
- revision/version markers used for freshness where implemented

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

A derived permission snapshot or governance report may help:
- accelerate policy evaluation
- inspect governance drift
- support operations
- reduce expensive repeated truth reads

It does **not** become:
- canonical governance truth
- permission authority when stale or uncertain
- justification to override newer Authorization truth

---

## 6) Evaluation ownership rule

### 6.1 Authorization owns final governance evaluation boundary

Authorization owns the final decision logic for:
- role/permission-based evaluation
- policy enforcement naming and mapping
- fail-closed behavior on uncertain governance state

### 6.2 Derived acceleration is subordinate

Permission snapshots, caches, or materialized views may be used only when they remain safe by policy.

If they are:
- stale
- missing
- uncertain
- inconsistent with revision/freshness rules

then Authorization must:
- fall back to truth
- or fail closed on security-sensitive paths

### 6.3 Other modules must not bypass this boundary

Other modules must not:
- locally trust stale copied grants
- embed shadow role/permission truth
- treat historical policy snapshots as active authority
- convert cache uncertainty into access grant

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
- publish stale candidate materialization over fresher live governance truth

### 7.1 Recovery posture

If derived governance outputs are unhealthy:
- Authorization truth remains authoritative
- reconciliation/rebuild is a recovery mechanism
- fail-closed evaluation remains the safety boundary
- safe non-progress is preferable to stale allow behavior

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
- other modules’ business truth

### 8.1 Cutover safety rule

Derived governance output publication must ensure:
- partial output is not treated as complete active policy truth
- stale candidates do not replace fresher derived outputs blindly
- evaluation remains truth-first or fail-closed during uncertainty
- operators can distinguish live truth from derived convenience outputs

---

## 9) Audit and evidence ownership rule

### 9.1 Authorization emits governance causes

Authorization owns:
- governance mutation truth
- canonical governance events after truth commit

### 9.2 Audit owns evidence of those causes

Audit owns:
- append-only evidence of governance events
- investigation-ready persistence
- completeness/replay over evidence truth

Authorization does not own:
- whether audit evidence is already queryable
- audit reporting truth
- audit archival truth

### 9.3 Ownership consequence

A successful governance mutation means:
- Authorization truth committed
- outbox/event intent committed

It does **not** mean:
- audit evidence is already stored
- audit report/dashboard is already current
- derived governance views are already updated

---

## 10) Coordination / ownership-sensitive workflow rule

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

### 10.1 Ownership ambiguity rule

If ownership is ambiguous for a correctness-sensitive materialization/rebuild workflow:
- delayed rebuild is acceptable
- stale-owner rejection is acceptable
- operator retry is acceptable
- truth-first or fail-closed evaluation is acceptable

Unsafe stale allow behavior is not acceptable.

---

## 11) Module dependency posture summary

### 11.1 What Authorization may expect from others

Authorization may expect:
- Identity to provide stable user identifiers, not role truth
- Audit to consume governance events asynchronously
- cache/materialization consumers to treat Authorization truth as authoritative
- replay/rebuild/reconciliation to be normal operational tools for derived outputs

### 11.2 What others may expect from Authorization

Other modules may expect:
- explicit policy names
- authoritative role/permission truth
- immediate governance effect after truth commit
- bounded async event emission after commit
- fail-closed behavior when derived governance state is uncertain

### 11.3 What nobody may assume

No module may assume:
- stale derived permission snapshots are safe authority
- audit truth is the same as governance truth
- a report or snapshot is stronger than live Authorization truth
- one current worker/leader is safe without explicit authoritative coordination
- another module may maintain its own shadow role/permission truth

---

## 12) V2 evolution

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

### 12.1 V2 constraint that remains unchanged

Even if Authorization grows more advanced:

- live governance truth still belongs to Authorization
- audit evidence still belongs to Audit
- derived policy outputs still remain subordinate to truth
- fail-closed behavior still wins over stale convenience