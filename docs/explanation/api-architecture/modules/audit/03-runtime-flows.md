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
- `../../../../decisions/adr-0018-transaction-boundaries-and-consistency-model-v1.md`
- `../../../../decisions/adr-0020-timeout-retry-and-failure-detection-policy-v1.md`
- `../../../../decisions/adr-0027-stream-processing-and-derived-state-policy-v1.md`
- `../../../../decisions/adr-0028-consumer-idempotency-replay-and-rebuild-policy-v1.md`

---

## Runtime posture in V1

Audit participates in two runtime lanes.

### A) Async side-effect lane

Used for:
- consuming auditable events after originating truth commits
- mapping integration events into append-only audit records
- durable dedupe and retry-safe persistence
- redaction and minimal-payload enforcement
- non-blocking evidence capture for governance and investigations

### B) Batch / replay / archival / reconciliation lane

Used for:
- replaying missed audit ingestion after lag or failure
- reprocessing DLQ messages after remediation
- reconciling expected governance events against persisted audit evidence
- archival, summarization, and retention workflows
- repairing missing audit evidence where policy allows replay from durable sources

Rules:
- Audit never blocks originating business truth flows.
- Audit owns evidence truth, not originating business truth.
- Audit ingestion is at-least-once.
- Duplicates, replay, worker restarts, and lag are normal.
- Audit success is durable append-only evidence persistence, not immediate completeness of reports or dashboards.

---

## Flow A — Ingest auditable event from RabbitMQ

### Goal

Persist durable audit evidence from an already-committed producer event.

### Runtime flow

1. Producer module commits business truth and writes an outbox message in the same local transaction.
2. Outbox polling worker claims and publishes the outbox message to RabbitMQ.
3. `AuditRabbitMqConsumerService` consumes the RabbitMQ message.
4. The message is deserialized into `OutboxIntegrationEventEnvelope`.
5. `AuditIntegrationEventDispatcher` routes the envelope by `EventType`.
6. A concrete `IAuditIntegrationEventHandler` maps the envelope and payload to an audit ingestion request.
7. `AuditIngestionService` applies redaction and durable idempotency rules.
8. Audit inserts an append-only `AuditLog` record.
9. Consumer ACKs the message after successful insert or duplicate-safe processing.

### Runtime stream semantics

- The incoming event is a cause record, not yet audit evidence.
- The same canonical event identity must produce at most one audit record.
- In V1, canonical event identity is the upstream `MessageId`.
- `AuditLog.EventId` stores the upstream `MessageId`.
- Duplicate delivery must not create duplicate audit rows.
- Replay is allowed and expected after crash, lag, or backlog recovery.
- Audit does not collapse different `MessageId` values by default.

### ACK/NACK rules

- Successful insert: ACK.
- Duplicate `EventId` / `MessageId`: treat as already processed and ACK.
- Transient persistence failure: retry according to consumer retry policy.
- Invalid payload or schema drift: fail visibly and route to retry/DLQ according to policy.
- Poison message after retry exhaustion: DLQ or terminal remediation path.

### Failure modes

- Consumer crash before ACK:
  - message may be redelivered
  - ingestion must dedupe by `EventId` / `MessageId`

- Timeout during DB insert:
  - outcome is ambiguous
  - retry must rely on unique `EventId` / `MessageId`

- DB insert failure:
  - retry with bounded backoff
  - send to DLQ if repeated or poison

- Backlog grows:
  - must be observable
  - core producer flows still succeed if truth + outbox commit succeeded

- Duplicate delivery:
  - must converge to one audit record for the same `MessageId`

- Out-of-order delivery:
  - may still be recorded because Audit is append-only evidence
  - ordering is handled by `OccurredAt` and `IngestedAt`, not by arrival order

### Observability notes

Track:
- ingest success count
- ingest failure count
- duplicate/dedupe hit count
- queue depth
- unacked message count
- DLQ size and age
- consumer retry count
- publish-to-ingest lag
- occurred-to-ingest lag
- mapping/redaction failure count where measurable

---

## Flow B — Ingest Authorization governance change

### Goal

Persist audit evidence for role, permission, and governance-sensitive Authorization changes.

### Authorization events consumed by Audit in V1

Audit consumes:

- `authorization.user_role_assigned`
- `authorization.user_role_revoked`
- `authorization.role_permission_granted`
- `authorization.role_permission_revoked`

Future Authorization events may include:

- `authorization.role_created`
- `authorization.role_updated`
- `authorization.role_deleted`
- `authorization.permission_created`
- `authorization.permission_updated`
- `authorization.permission_deleted`

### Runtime flow

1. Authorization command validates and authorizes the request.
2. Authorization commits governance truth change.
3. Authorization writes the corresponding outbox message in the same local transaction.
4. Outbox worker publishes the event to RabbitMQ.
5. `AuditRabbitMqConsumerService` receives the event through the Audit queue.
6. `AuditIntegrationEventDispatcher` selects the handler by `EventType`.
7. Authorization-specific Audit handler deserializes the payload.
8. Handler maps the event to audit fields:
   - `MessageId` → `AuditLog.EventId`
   - `EventType` → `AuditLog.EventType`
   - event handler mapping → `AuditLog.Action`
   - `AggregateType` → `AuditLog.ResourceType`
   - `AggregateId` → `AuditLog.ResourceId`
   - `InitiatorUserId` or payload actor field → `AuditLog.ActorUserId`
   - `CorrelationId` → `AuditLog.CorrelationId`
   - `OccurredAtUtc` → `AuditLog.OccurredAt`
   - safe payload subset → `AuditLog.Data`
9. `AuditIngestionService` inserts append-only evidence.
10. Consumer ACKs after success or duplicate-safe decision.

### Runtime rules

- Audit ingestion must never block the original admin action.
- Authorization command success is based on Authorization truth commit and outbox commit.
- Audit lag does not roll back or redefine Authorization truth.
- Governance events must preserve stable `MessageId` across retries and replay.
- Evidence persistence must remain append-only.
- Replay of the same `MessageId` must dedupe, not rewrite.
- Different `MessageId` values are not collapsed by default.

### Non-negotiable

- Authorization must not write directly to Audit tables.
- Authorization must not call Audit synchronously.
- Audit consumer failures must not be written back as producer-side outbox failures.
- Missing audit entries are incidents and must be detectable through backlog, lag, failure, and reconciliation signals.
- Duplicate deliveries must not create duplicate audit rows.

### Failure modes

- Consumer restart during processing:
  - safe redelivery and dedupe required

- Mapping schema drift or payload issue:
  - visible failure
  - retry or DLQ according to policy

- Replay after evidence already exists:
  - dedupe cleanly by `EventId` / `MessageId`

- Authorization truth committed but Audit consumer down:
  - Authorization remains valid
  - audit backlog grows
  - recovery occurs through retry, replay, or remediation

---

## Flow C — Replay / reprocess missing audit evidence

### Goal

Recover from lag, outage, or ingestion failure without duplicating already-persisted audit facts.

### Typical workflow shape

1. Select bounded replay input:
   - DLQ window
   - failed message set
   - bounded outbox/event window
   - targeted governance-event comparison set
2. Re-read canonical event identity.
3. Re-apply mapping with the same dedupe rules.
4. Insert missing evidence only.
5. Mark replay outcome and cleanup bounded replay state.

### Runtime rules

- Replay must preserve append-only semantics.
- Replay is for gap filling, not history mutation.
- Canonical event identity must survive replay/remediation.
- If the same bounded replay runs twice, the second run must be safe.
- Replay overlap with live ingestion must remain safe through canonical dedupe.

### Replay outcomes

- `inserted`
- `deduped`
- `invalid_payload`
- `still_failing`
- `escalated`

### Failure modes

- Replay repeatedly hits dedupe:
  - acceptable, but must be observable

- Replay cannot reconstruct canonical event:
  - escalate for operator investigation

- Replay overlaps with live ingestion:
  - must remain safe via `EventId` / `MessageId` dedupe

- Stale replay candidate set:
  - must not be treated as proof that audit is still missing

---

## Flow D — Audit completeness reconciliation workflow

### Goal

Detect possible gaps between expected auditable events and persisted audit evidence.

### Typical workflow shape

1. Select bounded expected-event scope:
   - example: Authorization governance events in time window W
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
- If mismatch detection is uncertain, prefer investigation or replay over unsafe synthetic evidence insertion.
- Reconciliation must not infer business truth from audit absence alone.

### Failure modes

- Expected-event source incomplete:
  - reconciliation may under-report or over-report gaps
  - output must remain visibly derived

- Reconciliation rerun on same scope:
  - must not create duplicate evidence

- Stale comparison snapshot:
  - candidate mismatch output must not override fresher completeness understanding blindly

---

## Flow E — Audit archival / summarization workflow

### Goal

Apply retention policy and generate derived reporting outputs without weakening evidence integrity.

### Typical workflow shape

1. Select bounded historical audit window.
2. Partition or normalize data for archival or summarization.
3. Generate candidate archive or summary output.
4. Validate candidate output.
5. Publish archive or summary according to policy.
6. Cleanup or mark completion.

### Typical outputs

- archive partitions
- governance summaries
- reporting views
- derived timeline/reporting datasets

### Rules

- Canonical audit store remains append-only evidence truth.
- Dashboards and summaries are derived and may lag.
- Archival/cutover must follow explicit policy.
- Do not silently destroy evidence required by retention rules.
- Full recompute of derived summaries is acceptable when safer than fragile patch repair.

### Failure modes

- Summary generation failure:
  - evidence truth remains intact

- Archive workflow failure:
  - archival lag is acceptable if visible and recoverable

- Partial derived output:
  - must not be mistaken for complete report state

- Cleanup/archival overlap with replay:
  - must remain safe under bounded scope and retention rules

---

## Flow F — DLQ / dead-letter remediation

### Goal

Provide operationally safe recovery path for audit ingestion failures that exceeded normal retry policy.

### Runtime flow

1. Failed audit ingestion lands in DLQ or terminal failure state.
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
- Manual investigation must be possible through:
  - `CorrelationId`
  - `EventId` / `MessageId`
  - `EventType`
  - `AggregateId`
  - failure metadata
- Safe non-progress is preferable to unsafe synthetic reconstruction of evidence.

### Failure modes

- Same dead item replayed repeatedly:
  - acceptable only if clearly observable and bounded

- Root cause unresolved:
  - must remain escalated, not silently loop forever

- Overlapping remediation runs:
  - must remain safe through dedupe and current-state checks

---

## Flow G — Truth-safe audit evidence under lag

### Goal

Ensure audit lag never rewrites the meaning of already committed business truth while still preserving investigation integrity.

### Runtime shape

1. Originating module commits truth and writes outbox.
2. Outbox worker may lag before publishing.
3. RabbitMQ may deliver later or redeliver.
4. Audit may lag due to queue backlog, worker outage, or DB failure.
5. Business truth remains valid.
6. Audit eventually converges through normal ingestion, replay, or remediation.

### Rules

- Missing audit evidence temporarily does not undo upstream business truth.
- Audit completeness is operationally critical, but not part of upstream synchronous success.
- Investigation tooling must distinguish:
  - truth committed but outbox not yet published
  - event published but audit consumer lagged
  - truth committed and audit later recovered
  - true missing evidence still unresolved

### Examples

- role assignment succeeded while audit queue was backlogged
- permission grant succeeded but audit row appeared later
- replay filled an earlier evidence gap after outage recovery
- content publish succeeded but audit row appeared after consumer recovery

---

## Summary

Audit runtime in V1 is governed by these rules:

1. Audit is downstream of truth and never blocks the originating business action.
2. Producer modules commit truth and outbox locally; Audit consumes later.
3. The same `MessageId` should produce at most one audit record.
4. Duplicate delivery must dedupe; replay must fill gaps without rewriting history.
5. Audit evidence is append-only truth for investigations.
6. Async ingestion is at-least-once; replay, lag, and worker restart are normal.
7. Audit accepts out-of-order delivery as historical evidence.
8. Dashboards, summaries, and archival derivatives are not the canonical evidence store.
9. Batch workflows support replay, completeness checking, archival, and reporting — not business truth ownership.
10. Partial repair/report outputs must not be mistaken for complete authoritative evidence.
11. Safe non-progress is preferable to unsafe synthetic evidence mutation.
12. Audit lag is acceptable temporarily only if it remains observable, recoverable, and investigation-safe.