# Interaction — Runtime Flows (V1)

Supports arc42 Scenario 3 (read + view tracking) and Interaction requirements.

Related:
- `../../../../architecture/arc42/04-runtime-view-v1.md`
- `../../../../architecture/arc42/13-transactions-and-consistency-v1.md`
- `../../../../architecture/arc42/18-stream-processing-and-derived-state-v1.md`
- `../../../../architecture/arc42/19-stream-processing-runtime-v1.md`
- `../../../../architecture/arc42/16-batch-processing-and-derived-data-v1.md`
- `../../../../architecture/arc42/17-dataflow-and-batch-workflows-v1.md`
- `../../../../decisions/adr-0013-outbox-and-delivery-semantics-v1.md`
- `../../../../decisions/adr-0027-stream-processing-and-derived-state-policy-v1.md`
- `../../../../decisions/adr-0028-consumer-idempotency-replay-and-rebuild-policy-v1.md`

---

## Runtime posture in V1

Interaction participates in all three runtime lanes:

### A) Synchronous truth lane
Used for:
- like / unlike
- comments CRUD
- optional durable raw interaction event write
- deterministic per-user/per-comment truth changes

### B) Async side-effect and derived-state lane
Used for:
- view ingestion / queueing
- counter aggregation
- popularity/trending updates
- optional downstream notifications/audit hooks
- future read-facing summary propagation

### C) Batch / replay / reconciliation lane
Used for:
- rebuilding counters from raw interaction truth
- replaying missed interaction events
- reconciling aggregate counters against truth/raw logs
- generating trending inputs and derived summaries
- cleanup / retention workflows for raw events or derived aggregates

**Rule:** Interaction owns user interaction truth.  
Counters, scores, and summaries are derived and may lag.

**Rule:** Interaction success is defined by **interaction truth commit** or **accepted async ingestion**, not by downstream aggregate freshness.

**Rule:** Interaction-derived async processing is assumed **at-least-once**.  
Duplicates, retries, replay, late arrivals, and out-of-order delivery must be tolerated safely.

---

## Flow A — View tracking (non-blocking)

### Goal
Capture view intent without degrading reading latency.

### Flow
1. User reads article detail (Reading).
2. Client (or Reading internally) calls `POST /articles/{id}/views`.
3. Interaction records view quickly:
   - write minimal log
   - or enqueue for async aggregation
4. Return `202 Accepted` immediately.

### Runtime stream semantics
- View capture is a **signal ingestion path**, not an immediate counter update path.
- Accepted view signals may be:
  - delayed
  - duplicated
  - replayed
  - sampled/dropped by explicit policy
- Aggregate counters/trending inputs converge later.

### Failure modes
- Interaction down: reading must still succeed; view counting lags.
- Under spike: may apply sampling/dropping by policy; must be observable.
- Duplicate submissions: acceptable in V1 unless stricter dedupe policy is explicitly enabled.
- Aggregation lag: counters may trail reality, but read response remains valid.
- Late or replayed view event: must not corrupt aggregate state.

### Observability notes
- Track:
  - accepted rate
  - enqueue success/failure
  - queue lag
  - aggregation freshness lag
  - duplicate/replay indicators where measurable

---

## Flow B — Like / unlike (idempotent)

### Goal
Maintain deterministic per-user like truth for an article.

### Flow
1. Authenticated user calls `POST /articles/{id}/likes`.
2. Interaction ensures unique `(ArticleId, UserId)` like record.
3. Returns `liked=true`.
4. Unlike uses delete (or soft delete) and returns `liked=false`.
5. If configured, emit outbox event for downstream aggregate update.

### Runtime rules
- Like truth is authoritative.
- Aggregate like totals are derived.
- Repeated equivalent command should converge as safe no-op or documented idempotent success.
- Truth change + outbox intent (if used) should commit atomically.

### Failure modes
- Retried requests must not create duplicates.
- Concurrent like/unlike attempts must converge deterministically.
- Totals may lag if aggregated asynchronously.
- Timeout ambiguity requires reconciliation from Interaction truth, not from counters.
- Duplicate or stale aggregate update must not corrupt derived totals.

### Rules
- current like truth is authoritative
- aggregate like totals are derived
- repeated equivalent command should be safe no-op or documented conflict

---

## Flow C — Comments CRUD

### Goal
Persist comment truth deterministically while allowing later moderation/aggregation.

### Flow
1. User creates comment `POST /articles/{id}/comments`.
2. Interaction persists comment and returns id.
3. Edit/delete enforce object-level auth (author or policy).
4. If configured, emit outbox events for downstream aggregates or moderation/reporting pipelines.
5. V2 introduces moderation states and anti-spam controls.

### Runtime rules
- Comment truth is authoritative.
- Comment counts and moderation/reporting summaries are derived.
- Edit/delete behavior should remain deterministic under stale-write or repeated-command conditions.
- Outbox/event propagation is downstream and non-blocking for truth success.

### Failure modes
- Timeout during create may lead to duplicate submission risk unless idempotency is used.
- Stale edit/delete attempt must be rejected or handled deterministically.
- Comment counters may lag aggregate updates.
- Replay of older comment-derived events must not overwrite newer derived state incorrectly.

### Rules
- comment truth is authoritative
- comment counts are derived
- moderation/reporting outputs must not redefine comment truth

---

## Flow D — Async counter aggregation

### Goal
Materialize read-friendly counters and summaries without slowing interaction truth writes.

### Typical flow
1. Interaction truth changes commit successfully.
2. Outbox/event is published asynchronously.
3. Worker consumes interaction events.
4. Aggregate counters are updated:
   - `viewsTotal`
   - `likesTotal`
   - `commentsTotal`
5. Optional popularity/trending inputs are recomputed or updated incrementally.

### Runtime stream semantics
- Aggregation is a **derived-state convergence flow**.
- Consumers must handle:
  - duplicate delivery
  - stale delivery
  - replay after outage
  - late-arriving view events
- Where ordering matters, consumers should prefer explicit aggregate/version logic or bounded recompute over timestamp-only trust.

### Failure modes
- consumer lag: counters stale
- duplicate delivery: aggregation must dedupe or converge safely
- stale delivery: must not corrupt aggregate state
- worker outage: truth still exists; counters can be replayed/rebuilt later
- partial derived update: must not be mistaken for complete aggregate truth

### Rules
- aggregation is derived
- read path must tolerate stale/missing counters
- replay/rebuild must be possible from truth/raw inputs
- full rebuild is acceptable when safer than fragile incremental repair

---

## Flow E — Rebuild / reconciliation of aggregates

### Goal
Repair derived counters and summaries when async aggregation lagged, failed, or drifted.

### Typical workflow shape
1. Select bounded interaction input:
   - raw view window
   - likes/comments truth snapshot
   - suspect article set
2. Aggregate or recompute candidate counters/summaries.
3. Compare against active derived aggregate state.
4. Produce repair candidate set.
5. Validate candidate output.
6. Publish / replace repaired aggregate state safely.
7. Cleanup temporary workflow state.

### Typical outputs
- repaired counters
- trending inputs
- mismatch reports
- aggregate snapshots

### Rules
- candidate output is derived, not truth
- partial rebuild output must not be treated as completed active state
- rerun on the same bounded input must be safe
- rebuild/cutover must not publish older aggregate knowledge over fresher truth-backed input scope
- if uncertainty exists, bounded recompute is preferred over unsafe patching

### Failure modes
- rebuild failure: interaction truth remains authoritative
- candidate publication failure: previous active derived aggregate remains valid if one exists
- duplicate run / overlapping execution: must remain safe under rerun and ownership rules
- stale snapshot used too late: candidate must be rejected, rerun, or explicitly scoped

---

## Flow F — Cleanup / retention workflow

### Goal
Control storage growth for raw interaction events and derived artifacts by policy.

### Typical flow
1. Select bounded eligible window/state.
2. Archive or purge according to retention rules.
3. Preserve any data still required for replay/reconciliation policy.
4. Record cleanup outcome.

### Rules
- cleanup must be bounded
- cleanup must not destroy data still required for correctness or replay policy
- cleanup is maintenance, not truth mutation of current like/comment state
- retention policy must distinguish:
  - current interaction truth
  - replayable raw inputs
  - replaceable derived outputs

### Failure modes
- cleanup too aggressive: replay/rebuild ability may be damaged
- cleanup lag: storage growth increases but correctness remains intact
- overlapping cleanup and rebuild: must be safe under ownership and bounded-input policy

---

## Flow G — Truth-safe serving under aggregate lag

### Goal
Ensure stale counters, popularity signals, or summaries never affect core read correctness.

### Typical runtime shape
1. Reading requests counters or interaction-derived enrichments.
2. Interaction or derived stores may respond with:
   - fresh values
   - stale values
   - no values
3. Reading may omit or degrade these enrichments safely.
4. Public article visibility remains governed by Content truth, not Interaction state.

### Examples
- like/comment totals lag after bursts
- view totals trail accepted view signals
- trending inputs not yet rebuilt after consumer outage
- rebuilt summary candidate not yet cut over

### Rules
- Interaction-derived state may shape experience, not visibility truth.
- Missing or stale counters are acceptable.
- Safe omission beats stale invented precision.
- Derived interaction lag must never block article list/detail correctness.

---

## Summary

Interaction runtime in V1 is governed by ten rules:

1. Likes, comments, and raw interaction events are Interaction truth.  
2. Views must remain non-blocking.  
3. Counters, popularity signals, and summaries are derived and may lag.  
4. Like/unlike and comment truth must stay deterministic under retry and concurrency.  
5. Async aggregation is at-least-once; duplicates, replay, and late arrivals are normal.  
6. Read paths must tolerate stale or missing interaction-derived enrichments.  
7. Batch workflows support aggregation, replay, rebuild, and cleanup — not truth ownership replacement.  
8. Candidate aggregate output must be validated before publication when correctness matters.  
9. Full rebuild is acceptable when safer than fragile partial repair.  
10. Reading must keep working even when Interaction aggregation is delayed.