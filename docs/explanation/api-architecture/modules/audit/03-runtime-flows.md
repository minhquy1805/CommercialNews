# Audit — Runtime Flows (V1)

Audit is a Worker-driven consumer.

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

Audit primarily participates in two runtime lanes:

### A) Async side-effect lane
Used for:
- ingesting auditable events after originating truth commits
- mapping canonical events into append-only audit records
- durable dedupe and retry-safe persistence
- redaction/minimal-payload enforcement
- non-blocking evidence capture for governance and investigations

### B) Batch / replay / archival / reconciliation lane
Used for:
- replaying missed audit ingestion after lag/failure
- reconciling expected governance events against persisted audit evidence
- archival / summarization / retention workflows
- repair of missing audit evidence where policy allows replay from durable sources

**Rule:** Audit never blocks originating business truth flows.  
Audit owns **evidence truth**, not the originating business truth.

**Rule:** Audit ingestion is assumed **at-least-once**.  
Duplicates, replay, worker restarts, and lag are normal and must converge safely.

**Rule:** Audit success is defined by durable append-only evidence persistence, not by immediate completeness of derived reports or dashboards.

---

## Flow A — Ingest governance event (publish/unpublish)

### Goal
Persist durable governance evidence after the originating business action has already committed.

### Async flow
1. Content publishes/unpublishes (sync path completes).
2. Content emits `ArticlePublished` / `ArticleUnpublished` event with `correlationId` and `actorUserId`.
3. Worker consumes the event.
4. Audit maps event → audit record, applies redaction/minimal payload rules.
5. Audit inserts append-only record.
6. Worker ACKs message.

### Runtime stream semantics
- The incoming event is a **cause record**, not yet audit evidence.
- One canonical auditable fact should converge to one canonical audit record.
- Duplicate delivery must not create duplicate audit rows.
- Replay is allowed and expected after crash, lag, or backlog recovery.

### Failure modes
- Consumer crash before ACK: message redelivered → must be idempotent by `EventId / MessageId`.
- DB insert failure: retry with backoff; send to DLQ if repeated.
- Backlog grows: must be observable; core flows still succeed (non-blocking).
- Duplicate delivery: must converge to one canonical audit fact.
- Stale delivery: may still be recordable if it represents a distinct canonical historical fact.

### Observability notes
- Track:
  - ingest success/failure
  - dedupe hits
  - queue depth / lag
  - time-to-ingest from event occurrence to audit persistence
  - redaction / mapping failures if measurable

---

## Flow B — Ingest authorization governance change

### Goal
Persist audit evidence for role/permission and other governance-sensitive authorization changes.

### Async flow
1. Authorization commits governance truth change.
2. Authorization emits canonical governance event:
   - `UserRoleAssigned`, `RolePermissionGranted`, etc.
3. Worker consumes the event.
4. Audit maps canonical event to append-only evidence record.
5. Audit persists the record and ACKs on success.

### Runtime rules
- Audit ingestion must never block the original admin action.
- Governance events must preserve canonical identity across retries/replay.
- Evidence persistence must remain append-only by policy.
- A later replay of the same canonical event must dedupe, not rewrite.

### Non-negotiable
- Audit ingestion must never block the original admin action.
- Missing audit entries are incidents; detect via backlog/lag and failure metrics.
- Duplicate deliveries must not create duplicate audit rows.

### Failure modes
- Consumer restart during processing: safe redelivery + dedupe required.
- Mapping schema drift or payload issue: must fail visibly and enter retry/DLQ path according to policy.
- Replay after later evidence already exists: must dedupe cleanly.

---

## Flow C — Replay / reprocess missing audit evidence

### Goal
Recover from lag, outage, or ingestion failure without duplicating already-persisted audit facts.

### Typical workflow shape
1. Select bounded replay input:
   - DLQ window
   - failed message set
   - bounded outbox/event window
   - sampled or targeted governance-event comparison set
2. Re-read canonical event identity.
3. Re-apply mapping with the same dedupe rules.
4. Insert missing evidence only.
5. Mark replay outcome and cleanup bounded replay state.

### Runtime rules
- Replay must preserve append-only semantics.
- Replay is for **gap filling**, not history mutation.
- Canonical event identity must survive replay/remediation.
- If the same bounded replay runs twice, the second run must be safe.

### Rules
- Replay must be safe on already-recorded events.
- Replay fills gaps; it does not rewrite historical audit truth.
- Replay must preserve append-only semantics.

### Failure modes
- Replay repeatedly hits dedupe: acceptable, but should be observable.
- Replay cannot reconstruct canonical event: escalate for operator investigation.
- Replay overlap with live ingestion: must remain safe via canonical dedupe.
- Stale replay candidate set: must not be treated as proof that audit is still missing.

---

## Flow D — Audit completeness reconciliation workflow

### Goal
Detect possible gaps between expected auditable events and persisted audit evidence.

### Typical workflow shape
1. Select bounded expected-event scope:
   - e.g. governance events in time window W
2. Compare expected canonical events against persisted audit records.
3. Produce mismatch candidate set.
4. Validate candidate set.
5. Trigger replay or operator-visible investigation output.
6. Record reconciliation outcome.

### Typical outputs
- mismatch reports
- replay candidate sets
- completeness summaries
- investigation handoff reports

### Rules
- Reconciliation output is derived, not evidence truth.
- Missing-evidence detection must not silently mutate historical audit rows.
- Replay or corrective handling must remain bounded and observable.
- If mismatch detection is uncertain, prefer investigation/replay over unsafe synthetic evidence insertion.

### Failure modes
- Expected-event source incomplete: reconciliation may under-report or over-report gaps; must remain visibly derived.
- Reconciliation rerun on same scope: must not create duplicate evidence.
- Stale comparison snapshot: candidate mismatch output must not override fresher completeness understanding blindly.

---

## Flow E — Audit archival / summarization workflow

### Goal
Apply retention policy and generate derived reporting outputs without weakening evidence integrity.

### Typical workflow shape
1. Select bounded historical audit window.
2. Partition / normalize data for archival or summarization.
3. Generate candidate archive or summary output.
4. Validate candidate output.
5. Publish archive / summary according to policy.
6. Cleanup or mark completion.

### Typical outputs
- archive partitions
- governance summaries
- reporting views
- derived timeline/reporting datasets

### Rules
- Canonical audit store remains append-only evidence truth.
- Dashboards and summaries are derived and may lag.
- Archival/cutover must follow explicit policy; do not silently destroy evidence required by retention rules.
- Full recompute of derived summaries is acceptable when safer than fragile patch repair.

### Failure modes
- Summary generation failure: evidence truth remains intact.
- Archive workflow failure: archival lag is acceptable if visible and recoverable.
- Partial derived output must not be mistaken for complete report state.
- Cleanup/archival overlap with replay: must remain safe under bounded scope and retention rules.

---

## Flow F — DLQ / dead-letter remediation

### Goal
Provide operationally safe recovery path for audit ingestion failures that exceeded normal retry policy.

### Flow
1. Failed audit ingestion lands in DLQ / Dead state.
2. Operator or automated bounded workflow selects remediation scope.
3. Root cause is corrected if possible.
4. Replay is attempted safely.
5. Outcome is recorded:
   - inserted
   - deduped
   - still failing
   - escalated

### Rules
- DLQ remediation must remain bounded.
- Remediation must preserve canonical event identity and append-only posture.
- Manual investigation must be possible through `correlationId`, `eventId / messageId`, and failure metadata.
- Safe non-progress is preferable to unsafe synthetic reconstruction of evidence.

### Failure modes
- Same dead item replayed repeatedly: acceptable only if clearly observable and bounded.
- Root cause unresolved: must remain escalated, not silently loop forever.
- Overlapping remediation runs: must remain safe through dedupe and current-state checks.

---

## Flow G — Truth-safe audit evidence under lag

### Goal
Ensure audit lag never rewrites the meaning of already committed business truth while still preserving investigation integrity.

### Typical runtime shape
1. Originating module commits truth and emits event.
2. Audit may lag due to queue backlog, worker outage, or DB failure.
3. Business flow still succeeds.
4. Audit eventually converges through normal ingestion, replay, or remediation.

### Rules
- Missing audit evidence temporarily does **not** undo upstream business truth.
- Audit completeness is operationally critical, but not part of upstream synchronous success.
- Investigation tooling must distinguish:
  - truth committed but audit lagged
  - truth committed and audit later recovered
  - true missing evidence still unresolved

### Examples
- publish succeeded but audit row appears later
- role assignment succeeded while audit queue was backlogged
- replay filled an earlier evidence gap after outage recovery

---

## Summary

Audit runtime in V1 is governed by ten rules:

1. Audit is downstream of truth and never blocks the originating business action.  
2. One canonical event should produce at most one canonical audit record.  
3. Duplicate delivery must dedupe; replay must fill gaps without rewriting history.  
4. Audit evidence is append-only truth for investigations.  
5. Async ingestion is at-least-once; replay, lag, and worker restart are normal.  
6. Dashboards, summaries, and archival derivatives are not the canonical evidence store.  
7. Batch workflows support replay, completeness checking, archival, and reporting — not business truth ownership.  
8. Partial repair/report outputs must not be mistaken for complete authoritative evidence.  
9. Safe non-progress is preferable to unsafe synthetic evidence mutation.  
10. Audit lag is acceptable temporarily only if it remains observable, recoverable, and investigation-safe.