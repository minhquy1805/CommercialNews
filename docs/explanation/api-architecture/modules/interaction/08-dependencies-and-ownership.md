# Interaction — Dependencies & Ownership (V1)

## 1) Ownership

Interaction owns:
- views / likes / comments data and rules
- optional raw interaction event truth
- aggregate/reaction counters only as derived outputs
- rebuild/reconciliation/cleanup workflows for Interaction-owned truth and derived aggregates

Interaction does **not** own:
- Content publication truth
- Reading response truth
- routing truth
- notification delivery truth
- audit evidence truth

---

## 2) Allowed dependencies

- Content events for state coupling (optional V1; recommended V2)
- Identity for user identity (`UserId` only)
- Audit via events (optional)
- Reading may call Interaction endpoints or emit signals, but Interaction must remain non-blocking
- bounded aggregation/rebuild workflows may consume:
  - Interaction truth
  - raw events
  - bounded Content visibility inputs where reconciliation requires comparison

---

## 3) Forbidden dependencies

- Interaction must not query Content tables to render article details.
- Interaction must not block Reading responses.
- No synchronous dependency on Notifications/Audit.
- Interaction must not treat counters/summaries as hidden truth.
- Interaction must not mutate another module’s truth because it is physically reachable.
- Interaction must not publish partial aggregate outputs as complete active state.
- Interaction must not rely on naive singleton ownership for aggregation/rebuild workflows.

---

## 4) Truth vs derived ownership

### Truth owned by Interaction
- current like truth
- comment truth
- optional raw view or raw interaction events
- local idempotency markers where policy requires them

### Derived outputs Interaction may own
Interaction may own derived outputs such as:
- `viewsTotal / likesTotal / commentsTotal`
- popularity/trending inputs
- aggregate snapshots
- reaction summaries
- mismatch reports
- cleanup candidate sets

These must remain:
- explicitly documented as derived
- subordinate to Interaction truth
- rebuildable
- observable
- safe under rerun/replay

---

## 5) Batch / replay / reconciliation ownership rules

Interaction batch workflows may:
- rebuild counters from raw events or truth snapshots
- replay missed aggregation inputs
- reconcile aggregate tables against truth/raw input
- generate trending/popularity inputs
- clean up retained raw events or workflow-private temporary state

Interaction batch workflows must not:
- redefine current like/comment truth
- infer truth from counters
- overwrite fresher truth using stale aggregate assumptions
- assume exclusive ownership without explicit coordination semantics

---

## 6) Publication and cutover ownership

If Interaction publishes an important derived aggregate output, Interaction owns:
- candidate generation
- candidate validation
- publication/cutover policy
- freshness signals
- rerun/rebuild policy

But Interaction still does not own:
- Content visibility truth
- Reading response truth

---

## 7) V2 evolution

- Move aggregation and moderation into Worker components/projections as traffic grows.
- Formalize raw-event retention and aggregate rebuild policy.
- Introduce stronger trending/popularity pipelines if needed.

If that happens, Interaction must make explicit:
- which data remains interaction truth
- which outputs are derived aggregates
- how publication/cutover works for important derived outputs
- how replay/reconciliation preserves truth-first correctness

---

## 8) Coordination / ownership-sensitive workflow rule

Interaction normally prefers:
- truth-store authority
- idempotent consumers
- commutative aggregation where possible
- bounded rebuild/reconciliation
- rerun-safe aggregate workflows

If a future workflow truly requires exclusive ownership
(for example one-current-owner hot-partition rebuild or trending-calculation owner),
then it must follow system-wide coordination rules:
- explicit ownership source
- generation/fencing token
- resource-side stale-owner rejection

Naive leader/lock assumptions are forbidden.