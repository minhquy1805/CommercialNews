# Media — Observability & SLO Signals (V1)

Related:
- `../../../../architecture/arc42/04-runtime-view-v1.md`
- `../../../../architecture/arc42/11-replication-v1.md`
- `../../../../architecture/arc42/18-stream-processing-and-derived-state-v1.md`
- `../../../../architecture/arc42/19-stream-processing-runtime-v1.md`
- `../../../../architecture/arc42/16-batch-processing-and-derived-data-v1.md`
- `../../../../architecture/arc42/17-dataflow-and-batch-workflows-v1.md`
- `../../../../decisions/adr-0013-outbox-and-delivery-semantics-v1.md`
- `../../../../decisions/adr-0027-stream-processing-and-derived-state-policy-v1.md`
- `../../../../decisions/adr-0018-transaction-boundaries-and-consistency-model-v1.md`
- `../../../../decisions/adr-0020-timeout-retry-and-failure-detection-policy-v1.md`
- `../../../../decisions/adr-0028-consumer-idempotency-replay-and-rebuild-policy-v1.md`

---

## 1) SLIs to track

### Core operations (admin/service APIs)
- register media success/failure and latency (P95/P99)
- attach success/failure and latency
- set-primary success/failure and latency
- reorder success/failure and latency
- soft delete/restore success/failure
- update media metadata success/failure and latency

### Error taxonomy signals (stability)
- error spikes and distribution by code:
  - `409` primary conflicts / invariant violations
  - `400` invalid reorder / validation failures
  - `404` missing media/attachment target
  - `409` idempotency conflicts
  - `409` version conflicts
  - `5xx`/timeouts (infrastructure instability)

### Invariant health (non-negotiable)
- "multiple primary prevented" incidents (should be near 0; any non-zero requires investigation)
- primary-on-deleted attempts
- unexpected detach/restore anomalies (optional)
- duplicate attachment prevention count
- stale-update / optimistic-concurrency reject rate where implemented
- idempotency-key reuse count for `POST /items`
- idempotency-key reuse count for `POST /articles/{articleId}/attachments`
- idempotency conflict count:
  - same key reused with different semantic request
- missing `expectedVersion` count for reorder/set-primary
- `expectedVersion` mismatch count for reorder
- `expectedVersion` mismatch count for set-primary

### Consistency / correctness signals
- reorder mismatch or resync-trigger count where order-sensitive derived views exist
- stale-event reject count for order/primary-sensitive consumers if implemented
- truth-fallback usage when derived media metadata or derivative state is stale/missing
- unexpected "deleted media still active in derived output" incidents
- any derived artifact reintroducing older primary/order state = release blocker

### Replication freshness for Media Outbox events

In V1, Media emits Outbox events and Audit consumes them asynchronously.

Track:
- outbox pending count by Media `eventType`
- outbox oldest pending age by Media `eventType`
- publish success/failure count by Media `eventType`
- publish retry count by Media `eventType`
- dead Media outbox message count by `eventType`
- broker queue depth for Media-related events where measurable
- Media event publish-to-consume lag where measurable

### Interpretation rule
Media observability must help distinguish:
- truth-path health
- downstream lag in cache/CDN/derivative processing
- stale-worker or replay behavior
- cleanup/reconciliation lag
- read-path degradation caused by delivery layers rather than Media truth corruption

### Audit ingestion signals

- Media audit event consumed count by `eventType`
- Media audit ingestion success/failure count
- Media audit ingestion lag:
  - `event occurredAtUtc -> audit insertedAtUtc`
- Audit dedupe hit count by `MessageId`
- Audit consumer retry count for Media events
- Audit DLQ/dead-letter count for Media events where applicable
- Media events published but not audited within target lag

Rules:
- Audit lag must not redefine Media truth.
- Audit consumer failure must not be reported as synchronous Media API failure after Media truth commit.
- Audit ack must happen only after durable append or durable dedupe decision.

---

## 2) Derived media repair / cleanup signals

These apply when Media runs bounded workflows for:
- orphan cleanup
- retention purge
- storage-reference reconciliation
- derived media list rebuild
- derivative/thumbnail repair
- attachment/primary drift detection

### Workflow health SLIs
- run success/failure count
- run duration
- records selected / processed / skipped / repaired / cleaned
- mismatch count from reconciliation
- derived output freshness age where measurable
- candidate output generation success/failure
- publication/cutover success/failure for important derived outputs

### Replay / rerun indicators
- rerun count on same bounded scope
- repeated mismatch on same scope
- stale candidate rejection count
- truth-resync trigger count where workflows detect stale or out-of-order derived state

### Ownership-sensitive workflow signals (if introduced later)
- duplicate-run detection count
- stale-owner rejection count
- safe no-owner / degraded intervals

### Policy
Media truth remains authoritative even when cleanup/repair workflows lag or fail.
Derived outputs may lag, but must not be mistaken for attachment/order/primary truth.

### Strong stop conditions
Treat as high severity:
- a repair workflow publishing stale derived output over fresher truth
- a cleanup workflow removing still-needed artifacts against policy
- a derivative rebuild causing active article rendering failures at scale

---

## 3) Abuse/security signals

- spikes in upload/register attempts (by IP/user)
- repeated failures due to type/size constraints
- unusual rate-limit triggers (if enabled)
- abnormal attach/reorder churn (rapid repeated operations on same article)
- repeated primary-toggle storms on the same article
- repeated duplicate-attach attempts (possible UX loop or automation bug)

### Storage/provider signals (if applicable)
- object storage error rate / timeout rate
- CDN/presigned URL generation failures (if used)
- derivative-generation failure rate by media type
- storage/object-reference mismatch growth over time

### Safety posture
Storage/CDN/provider degradation is operationally serious, but it must be distinguishable from:
- broken Media truth
- stale derived outputs
- bad admin workflows
- replay/reconciliation drift

---

## 4) Read-path impact signals

Media must not break Reading.

Signals:
- frequency of missing/invalid cover media in Reading responses (placeholder rate)
- media metadata fetch latency contribution to article detail
- time-to-recover after storage incidents:
  - incident start -> placeholder rate back to normal
  - incident start -> error rate back to baseline
- truth-backed article success despite media delivery failure
- degraded-but-successful read rate (article served with placeholder/omitted media)

### Derived-state impact signals
- stale derivative fallback rate
- missing-thumbnail or missing-variant rate
- route/detail success when derivative state is absent but base media truth is valid
- "truth valid, derivative missing" rate

### Degraded-but-acceptable behavior
- Reading returns successfully with placeholders when media is unavailable.
- Missing derivative variants do not break article truth-backed rendering.

### Not acceptable
- media issues causing article list/detail to error out systematically
- stale derived media state overriding fresher Media truth
- large-scale placeholder spikes caused by Media truth corruption rather than delivery-layer failure

---

## 5) Release gates (recommended)

During rollout, gate on:
- spikes in `5xx`/timeouts for media endpoints
- sustained P99 latency regression for attach/set-primary/reorder
- surge in invariant-related `409`s (may indicate concurrency/transaction regression)
- sustained placeholder rate increase in Reading (read-path impact)
- cleanup/reconciliation failure spikes for important derived media outputs
- sustained outbox oldest pending age growth for media-derived side effects
- derivative/thumbnail pipeline failure spike if enabled
- any signal that stale derived media output is overriding fresher truth-backed state (release blocker)
- any dead Media outbox message under normal operation
- Audit DLQ/dead-letter growth for Media events
- Media events published but not audited within target lag

---

## 6) Initial SLO targets (V1)

These are initial operating targets and should be refined after measurement.

### Truth-path latency

- register metadata excluding binary upload: P95 < 500ms, P99 < 1500ms
- update metadata: P95 < 300ms, P99 < 1000ms
- attach/detach: P95 < 300ms, P99 < 1000ms
- reorder/set-primary: P95 < 500ms, P99 < 1500ms
- delete/restore: P95 < 500ms, P99 < 1500ms

### Async freshness

- Media outbox oldest pending age: normally < 60s
- Media audit ingestion lag: P95 < 120s
- dead Media outbox messages: 0 under normal operation
- Audit DLQ/dead-letter count for Media events: 0 under normal operation

### Correctness targets

- duplicate active attachment incidents: 0
- multiple active primary incidents: 0
- stale derived output overriding fresher truth: 0
- article read failures caused by missing media: 0 where graceful degradation policy applies

---

## 7) Operator questions this module must answer

Media observability should help answer:

1. Did Media truth commit successfully?  
2. Is the problem in attachment/order/primary truth, or only in derived delivery layers?  
3. Are placeholder spikes caused by storage/CDN issues or by truth drift?  
4. Is cleanup/reconciliation only lagging operationally, or is it drifting away from truth?  
5. Are retries caused by admin UX, network ambiguity, or invariant conflicts?  
6. Is the issue in live media truth, in downstream async convergence, or in a delayed rebuild/publication path?