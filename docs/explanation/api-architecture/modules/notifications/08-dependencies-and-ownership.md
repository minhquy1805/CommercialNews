# Notifications — Dependencies & Ownership (V1)

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

Notifications owns:
- email delivery orchestration
- template safety and render discipline
- delivery-state truth
- business-intent dedupe rules for each notification type
- retry / dead / ambiguous delivery-state handling
- replay/remediation/cleanup/summarization workflows for Notifications-owned delivery data

Notifications does **not** own:
- account verification truth
- password reset truth
- article publish truth
- authorization/governance truth
- the business validity of whether an email should exist at all

**Rule:** Notifications owns **how delivery is executed and tracked**.  
It does not own **why the underlying business action is valid**.

---

## 2) Allowed dependencies

- Consumes events from Identity and Content.
- Uses message broker and worker infrastructure.
- Uses email provider (external dependency).
- May use Redis as supplementary dedupe/cache support.
- May run bounded workflows over:
  - canonical delivery-state records
  - failed / ambiguous / dead delivery sets
  - derived delivery summaries
  - replay/remediation candidate sets

### 2.1 Allowed dependency shapes
Approved interaction patterns are:

- **async consume after truth commit**
- **local delivery-state transaction**
- **provider call + safe state transition**
- **bounded replay/remediation**
- **derived summary/report generation**
- **supplementary Redis usage**, never as sole harmful-duplicate protection

Not approved:
- synchronous domain truth dependency for core upstream success
- hidden cross-module truth mutation
- treating provider response or dashboard summary as canonical business truth

---

## 3) Forbidden dependencies

- No synchronous calls from Identity/Content to Notifications.
- Notifications must not call back into domain modules to enrich emails in a way that adds coupling or leaks data.
- No access to secrets/tokens beyond what is required to send (handled safely and never logged).
- Notifications must not mutate another module’s truth because it is physically reachable.
- Notifications must not treat summaries/dashboards as canonical delivery truth.
- Notifications must not publish partial derived report output as complete active state.
- Notifications must not rely on naive singleton ownership for replay/remediation/batch-send workflows.
- Notifications must not infer business truth from provider behavior alone.
- Notifications must not resend based only on stale local belief when current delivery-state truth says otherwise.

---

## 4) Truth vs derived ownership

### 4.1 Truth owned by Notifications
- canonical delivery-state records
- attempt/error metadata
- durable dedupe outcome for canonical email intents
- terminal/dead/suppressed/ambiguous state where modeled

### 4.2 Derived outputs Notifications may own
Notifications may own derived outputs such as:
- delivery summaries
- retry/dead reports
- provider error-class reports
- replay/remediation candidate sets
- cleanup candidate sets

These must remain:
- explicitly documented as derived
- subordinate to canonical delivery-state truth
- rebuildable or reproducible where practical
- observable
- safe under rerun/replay

### 4.3 Ownership consequence
A Notifications-owned report or candidate set may help operators decide what to do next.
It does **not** become:
- canonical delivery truth
- proof that provider definitely sent
- proof that business truth upstream is still valid

---

## 5) Upstream cause vs delivery ownership rule

### 5.1 Upstream modules create valid intents
Identity or Content owns the truth that says:
- verification email should exist
- reset email should exist
- publish notification may exist

Notifications consumes those intents after truth commit.

### 5.2 Notifications owns execution after the cause exists
Once a valid event/intention exists, Notifications owns:
- whether a delivery record exists
- whether it is pending, retryable, sent, dead, suppressed, or ambiguous
- whether a replay/remediation workflow is still valid

### 5.3 Cause must remain upstream
Notifications must not reinterpret upstream business validity on its own beyond policy-allowed prerequisite checks.

Examples:
- a resend verification email should rely on a valid new token/intent upstream
- a publish email should rely on publish truth/publish event upstream
- Notifications may validate prerequisites before send, but does not become owner of that domain truth

---

## 6) Batch / replay / remediation ownership rules

Notifications batch workflows may:
- replay failed or ambiguous deliveries
- generate resend/remediation candidate sets
- clean up expired delivery artifacts by policy
- produce derived delivery reports and summaries
- perform controlled burst sending when policy allows

Notifications batch workflows must not:
- override current delivery truth blindly
- resend without canonical dedupe/state validation
- treat remediation candidate sets as already-applied delivery truth
- assume exclusive ownership without explicit coordination semantics
- publish stale candidate resend/report state over fresher canonical delivery-state truth

### 6.1 Recovery posture
If notification delivery is lagging or ambiguous:
- canonical delivery-state truth remains the authority
- replay/remediation is a recovery mechanism
- summaries/reports remain operational views only
- safe non-progress is preferable to unsafe duplicate send

---

## 7) Publication and cutover ownership

If Notifications publishes an important derived report/summary, Notifications owns:
- candidate generation
- candidate validation
- cutover/publication policy
- freshness signals
- rerun/rebuild policy

But Notifications still does not own:
- the originating domain truth that created the email intent

### 7.1 Cutover safety rule
Derived report/candidate publication must ensure:
- partial output is not treated as complete active state
- stale candidates do not replace fresher report truth or fresher canonical delivery-state knowledge
- operator-facing outputs remain clearly separate from canonical delivery-state truth

---

## 8) Dedupe and resend ownership rule

### 8.1 Notifications owns dedupe enforcement
Notifications owns:
- message-level dedupe handling
- business-intent dedupe handling
- safe suppression/no-op outcomes for duplicate intent processing

### 8.2 Upstream owns creation of legitimate new intent
A legitimate resend must come from:
- new token
- new intent identity
- new upstream business cause

Notifications must not guess that on its own from stale retry pressure.

### 8.3 Redis is supplementary only
If Redis is used for hot dedupe suppression, Notifications still owns:
- durable final harmful-duplicate protection in canonical delivery-state storage
- the policy that says which duplicates are dangerous
- the safe state transitions around resend and replay

Redis support does not transfer canonical dedupe ownership away from Notifications truth.

---

## 9) Coordination / ownership-sensitive workflow rule

Notifications normally prefers:
- idempotent consumer execution
- durable dedupe
- bounded replay/remediation
- safe delivery-state transitions
- rerun-safe summary/cleanup workflows

If a future workflow truly requires exclusive ownership
(for example one-current-owner batch-send orchestrator or one-current repair worker),
then it must follow system-wide coordination rules:
- explicit ownership source
- generation/fencing token
- resource-side stale-owner rejection

Naive lock/leader assumptions are forbidden.

### 9.1 Ownership ambiguity rule
If ownership is ambiguous for a correctness-sensitive resend/remediation workflow:
- delay is acceptable
- stale-owner rejection is acceptable
- operator retry is acceptable
- preserving current delivery-state truth is acceptable

Unsafe dual send or stale retry overwrite is not acceptable.

---

## 10) Module dependency posture summary

### 10.1 What Notifications may expect from others
Notifications may expect:
- upstream modules to emit valid post-commit intents
- broker/worker infrastructure to redeliver at least once
- provider to be fallible, slow, and sometimes ambiguous
- replay/remediation workflows to rely on canonical delivery-state truth

### 10.2 What others may expect from Notifications
Other modules may expect:
- non-blocking side-effect execution
- canonical delivery-state records
- durable dedupe for harmful duplicate prevention
- bounded replay/remediation workflows
- clear separation between canonical delivery truth and derived summaries/reports

### 10.3 What nobody may assume
No module may assume:
- Notifications proves upstream business truth
- provider success equals canonical business truth
- Redis alone is enough for harmful duplicate prevention
- replay candidate sets equal actual resend truth
- summaries/reports are authoritative delivery-state truth

---

## 11) V2 evolution

Notifications may evolve toward:
- richer subscription-driven publish notifications
- provider abstraction changes
- stronger remediation tooling
- multi-provider routing
- more formalized delivery summaries or campaign-like batching

If that happens, Notifications must make explicit:
- which datasets are canonical delivery truth
- which outputs are derived summaries/reports
- how publication/cutover works for important derived outputs
- how replay/remediation preserves dedupe and delivery-state integrity
- which workflows are operationally critical

### 11.1 V2 constraint that remains unchanged
Even if Notifications becomes more advanced:

- upstream business truth still remains upstream
- canonical delivery-state truth still belongs to Notifications
- derived reports/candidate sets still remain subordinate to canonical delivery truth
- replay/remediation remains a recovery mechanism, not authority to resend blindly