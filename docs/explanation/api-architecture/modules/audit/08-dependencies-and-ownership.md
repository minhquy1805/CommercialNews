# Audit — Dependencies & Ownership (V1)

Related:
- `../../../../architecture/arc42/03-building-blocks-modularity.md`
- `../../../../architecture/arc42/13-transactions-and-consistency-v1.md`
- `../../../../architecture/arc42/18-stream-processing-and-derived-state-v1.md`
- `../../../../architecture/arc42/19-stream-processing-runtime-v1.md`
- `../../../../decisions/adr-0013-outbox-and-delivery-semantics-v1.md`
- `../../../../decisions/adr-0027-stream-processing-and-derived-state-policy-v1.md`
- `../../../../decisions/adr-0028-consumer-idempotency-replay-and-rebuild-policy-v1.md`

---

## 1) Ownership boundaries

Audit owns:
- audit persistence and policy
- canonical evidence truth
- append-only audit record model
- dedupe policy for canonical audit events
- replay/reconciliation/archival workflows for Audit-owned evidence and derived reports
- correction posture for audit evidence (append corrective fact, not silent overwrite)

It does not participate in domain workflows synchronously.

Audit does **not** own:
- publication truth
- authorization truth
- identity truth
- routing truth
- notification truth
- current business-state truth of investigated resources

**Rule:** Audit owns **evidence of what was recorded**, not **whether the domain action was valid**.

---

## 2) Allowed dependencies

- Consumes events from domain modules (Content, Authorization, Identity, Media, Interaction optional)
- Uses shared DB (owned schema) or dedicated store (implementation choice)
- May depend on message broker and worker infrastructure
- May run bounded replay/reconciliation/archival workflows over:
  - canonical audit evidence
  - durable event identity references
  - derived reporting outputs
  - bounded expected-event sets for completeness checking

### 2.1 Allowed dependency shapes

Approved interaction patterns are:

- **async consume after domain truth commit**
- **append-only evidence persistence**
- **bounded replay/backfill/remediation**
- **completeness reconciliation against bounded expected-event scope**
- **archival / summary generation over audit-owned evidence**
- **read-only correlation with upstream identifiers**, when needed for investigations or completeness policy

Not approved:
- synchronous domain dependency for business success
- hidden cross-module truth mutation
- turning derived reports or dashboards into canonical evidence truth

---

## 3) Forbidden dependencies

- No synchronous calls from domain modules into Audit.
- Audit must not call back into domain modules to “enrich” records in a way that adds coupling or PII risk.
- Audit must not mutate another module’s truth because it is physically reachable.
- Audit must not treat dashboards/summaries as evidence truth.
- Audit must not publish partial derived reporting outputs as though they were complete evidence views.
- Audit must not rely on naive singleton ownership for replay/backfill/repair workflows.
- Audit must not silently correct or rewrite canonical evidence based on later upstream state.
- Audit must not infer current business truth from audit evidence alone when the owning module remains authoritative.

---

## 4) Truth vs derived ownership

### 4.1 Truth owned by Audit
- append-only canonical audit records
- local dedupe outcome for canonical event identity
- local ingestion metadata required for operability

### 4.2 Derived outputs Audit may own
Audit may own derived outputs such as:
- governance summaries
- reporting views
- archival indexes
- completeness reconciliation reports
- replay candidate sets
- timeline materializations

These must remain:
- explicitly documented as derived
- subordinate to canonical audit evidence
- rebuildable or reproducible where practical
- observable
- safe under rerun/replay

### 4.3 Ownership consequence

A derived Audit report may help operators or investigators:
- find gaps
- inspect trends
- prioritize replay/remediation
- browse timelines faster

It does **not** become:
- canonical evidence truth
- proof that an upstream business state is still current
- authority to mutate or replace canonical evidence rows

---

## 5) Upstream truth vs evidence ownership rule

### 5.1 Upstream modules create auditable causes

Content, Identity, Authorization, and other modules own the truth that says:
- a publish happened
- a role assignment happened
- a verification action happened
- another auditable action happened

Audit consumes those causes after truth commit.

### 5.2 Audit owns evidence after the cause exists

Once a canonical auditable event exists, Audit owns:
- whether evidence was persisted
- whether it was deduped
- whether ingest failed or was replayed
- whether completeness/reconciliation later found a gap
- whether archival/reporting outputs were generated over that evidence

### 5.3 Audit does not reinterpret upstream validity

Audit may record:
- what event identity was seen
- what actor/resource/action metadata was captured
- when it was stored

Audit must not become owner of:
- whether that action remains valid now
- whether the current domain state still matches that event
- whether another module’s later state supersedes it

---

## 6) Batch / replay / reconciliation ownership rules

Audit batch workflows may:
- replay failed or missing evidence ingestion
- reconcile expected events against persisted evidence
- archive historical windows by retention policy
- generate summaries and reporting outputs
- clean up workflow-private temporary state

Audit batch workflows must not:
- rewrite historical canonical evidence in place
- redefine originating business truth
- bypass append-only policy
- treat replay candidate sets as already-applied evidence
- assume exclusive ownership without system-approved ownership semantics
- publish stale completeness/reporting output over fresher canonical evidence understanding

### 6.1 Recovery posture

If Audit completeness is uncertain:
- canonical evidence remains the authority for what is known
- replay/reconciliation is a recovery mechanism
- summaries/reports remain operational aids
- safe non-progress is preferable to unsafe synthetic evidence mutation

---

## 7) Publication and cutover ownership

If Audit publishes an important derived report or archival output, Audit owns:
- candidate generation
- candidate validation
- cutover/publication policy
- freshness/completeness signals
- rerun/rebuild policy

But Audit still does not own:
- originating module truth
- current business state of the resource being investigated

### 7.1 Cutover safety rule

Derived report/archival publication must ensure:
- partial output is not treated as complete evidence view
- stale candidates do not replace fresher derived output blindly
- canonical append-only evidence remains queryable or explicitly policy-governed through archival strategy
- operators can still distinguish evidence truth from derived presentation layer

---

## 8) Canonical identity and correction ownership rule

### 8.1 Audit owns canonical evidence identity

Audit owns how a canonical upstream event maps to:
- one canonical audit record identity
- one dedupe decision
- one append-only persistence outcome

### 8.2 Upstream owns event semantics

Audit should not unilaterally redefine what an upstream event means.
If upstream emits unstable or duplicated semantics:
- that is an upstream contract/governance issue
- Audit may defend with dedupe/mapping policy
- but Audit should not silently invent business meaning

### 8.3 Correction remains append-only

If a correction model exists, Audit owns:
- how corrective facts are appended
- how linkage between original and corrective records is represented
- how investigations distinguish original evidence from later corrective evidence

Audit does not own silent in-place mutation of history.

---

## 9) Coordination / ownership-sensitive workflow rule

Audit normally prefers:
- idempotent ingestion
- bounded replay
- completeness reconciliation
- append-only correction by new facts
- rerun-safe archival/reporting workflows

If a future workflow truly requires exclusive ownership
(for example one-current-owner backfill of a partition),
then it must follow system-wide coordination rules:
- explicit ownership source
- generation/fencing token
- resource-side stale-owner rejection

Naive lock/leader assumptions are forbidden.

### 9.1 Ownership ambiguity rule

If ownership is ambiguous for a correctness-sensitive replay/reconciliation/publication workflow:
- delay is acceptable
- stale-owner rejection is acceptable
- operator retry is acceptable
- preserving append-only evidence integrity is acceptable

Unsafe dual replay, duplicate evidence creation, or contradictory derived publication is not acceptable.

---

## 10) Module dependency posture summary

### 10.1 What Audit may expect from others

Audit may expect:
- upstream modules to emit canonical post-commit auditable events
- broker/worker infrastructure to redeliver at least once
- replay/backfill/remediation to be normal operational recovery tools
- derived reporting workflows to remain subordinate to canonical evidence

### 10.2 What others may expect from Audit

Other modules may expect:
- non-blocking evidence capture
- append-only investigation-ready persistence
- durable dedupe for canonical event identity
- bounded replay/reconciliation workflows
- clear distinction between canonical evidence and derived summaries/reports

### 10.3 What nobody may assume

No module may assume:
- Audit proves current business truth
- a dashboard/report is stronger truth than canonical evidence
- replay candidate sets mean evidence has already been repaired
- stale archival/reporting output is authoritative
- singleton ownership is safe without explicit authoritative coordination

---

## 11) V2 evolution

Audit may evolve toward:
- richer compliance/reporting outputs
- stronger completeness-check workflows
- archive tiers and archive indexes
- more formalized reconciliation/backfill tooling

If that happens, Audit must make explicit:
- which datasets are canonical evidence truth
- which outputs are derived reports/summaries
- how publication/cutover works for important derived outputs
- how replay/reconciliation preserves append-only integrity
- which workflows are operationally critical for governance confidence

### 11.1 V2 constraint that remains unchanged

Even if Audit becomes more advanced:

- upstream business truth still remains upstream
- canonical evidence truth still belongs to Audit
- derived reports/summaries still remain subordinate to canonical evidence
- replay/reconciliation remains a recovery mechanism, not permission to rewrite history