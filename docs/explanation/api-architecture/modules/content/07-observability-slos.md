# Content — Observability & SLO Signals (V1)

Related:
- `../../../../architecture/arc42/04-runtime-view-v1.md`
- `../../../../architecture/arc42/13-transactions-and-consistency-v1.md`
- `../../../../architecture/arc42/18-stream-processing-and-derived-state-v1.md`
- `../../../../architecture/arc42/19-stream-processing-runtime-v1.md`
- `../../../../decisions/adr-0013-outbox-and-delivery-semantics-v1.md`
- `../../../../decisions/adr-0027-stream-processing-and-derived-state-policy-v1.md`
- `../../../../decisions/adr-0028-consumer-idempotency-replay-and-rebuild-policy-v1.md`

---

## 1) Critical admin actions to measure

- create draft, update draft
- publish/unpublish (highest impact)
- archive/restore (if enabled)
- slug/title/seo-related edits (if they affect routing/indexability)
- stale-write / optimistic-concurrency rejects on editorial actions
- replay/reconciliation-triggering lifecycle anomalies

---

## 2) SLIs (minimum)

### Core action health
- success/failure rates for publish/unpublish (by outcome category)
- policy deny rate (403) on governance endpoints
- latency percentiles (P95/P99) for publish/unpublish
- error rate spikes:
  - 409 (invalid transition / conflict / stale edit)
  - 5xx/timeouts (system instability)

### Correctness & safety signals (must-have)
- visibility correctness incidents:
  - draft/unpublished leak signals = **0** (release blocker)
- "published but not readable" anomaly rate (if detectable):
  - publish succeeded but public detail returns 404/empty due to stale derived paths
- truth-vs-derived disagreement signals:
  - derived route/projection says visible, Content truth says not visible
  - derived route/projection missing while Content truth says published
- content state distribution drift:
  - sudden spikes in unpublished/archived transitions (possible abuse/automation bug)
- stale-write protection signals:
  - edit conflict rate
  - stale lifecycle command reject/no-op rate

### Ordering & replay safety signals
- stale-event rejection count by downstream consumer where measurable
- version-gap / resync-trigger count where downstream consumers detect missing or out-of-order versions
- duplicate publish/unpublish effect attempts detected by downstream dedupe logic

---

## 3) Async workflow signals (must monitor)

### Replication freshness for content events
- outbox pending count (content events)
- outbox oldest pending age (seconds)
- publish attempts / failures for content outbox events
- broker queue depth (ready/unacked) for:
  - audit ingestion consumer
  - SEO consumer
  - notifications consumer (if enabled)
  - future reading/search projection consumers (if introduced)

### Downstream pipelines
- audit ingestion success/failure + retries + DLQ size/oldest age
- SEO update lag (if measurable) and/or SEO DB fallback rate spikes (indirect indicator)
- notification send backlog/lag + dedupe hits (if enabled)
- projection freshness lag for important Content-derived outputs where measurable

### Duplicate-prevention indicators
- dedupe hits / unique-key conflicts in:
  - audit store (AuditEventId/MessageId unique)
  - notifications delivery log (MessageKey unique)
  - downstream projection/version apply logic where tracked

High spikes often indicate retry storms, broker redeliveries, replay activity, or out-of-order/stale delivery.

### Content-derived stream health questions
Observability should make it possible to tell:
- truth committed, but outbox not yet drained
- outbox drained, but one downstream consumer is behind
- consumer is receiving duplicates safely
- consumer is rejecting stale versions correctly
- derived-state lag is operational, not a truth failure

---

## 4) Derived-state rebuild / reconciliation signals

These apply when downstream systems depend on Content truth for:
- SEO route/materialization repair
- reading projection repair
- search/index repair
- notifications recovery candidate generation
- reporting / summary rebuilds
- bounded replay/reconciliation workflows

### Workflow health SLIs
- run success/failure count
- run duration
- records selected / processed / skipped / repaired
- mismatch count from reconciliation
- version-gap / resync-trigger count
- candidate output generation success/failure
- publication/cutover success/failure for important derived outputs
- freshness age of active derived outputs where measurable
- rerun count / replay count for bounded recovery workflows
- stale-owner rejection count if a workflow uses ownership-sensitive execution
- candidate-built-but-not-published count (useful for detecting stuck cutover or validation failures)

### Policy
Content truth remains authoritative even when these workflows lag or fail.
Derived outputs may lag, but must not be mistaken for visibility truth.

### Recovery posture signals
Operators should be able to see whether:
- a bounded rebuild is enough
- a full rebuild is being preferred over partial repair
- reconciliation is finding repeated drift for the same output family
- a derived output is repeatedly falling behind after successful rebuild

---

## 5) Correlation requirements

Propagate `correlationId` across:
- publish/unpublish request logs
- outbox records / emitted events (`messageId`, `eventType`, `articleId`, `version`)
- worker consumer logs (audit/seo/notifications/projections)

Required logging fields for investigations:
- actorUserId (no PII)
- articleId/publicId
- action and state transition (from -> to)
- reason (for unpublish)
- correlationId + messageId + version
- consumer/apply decision where relevant:
  - applied
  - duplicate ignored
  - stale version rejected
  - resync triggered

### Additional recommended log fields
For operational investigations, include where relevant:
- current truth version at decision time
- expected version / rowversion for stale-write rejects
- downstream handler name
- retry attempt / redelivery marker
- rebuild/reconciliation run id when a workflow is involved

---

## 6) Investigations readiness

Audit entries for content governance must include:
- actorUserId
- action
- articleId
- timestamps (`occurredAt`, `storedAt`)
- reason (for unpublish)
- correlationId
- source (API/worker) and environment (optional)

Operational expectations:
- investigators can trace:
  - request → outbox event → consumer handling → audit record
- backlog/lag status must be visible during investigations to avoid false assumptions
- operators can distinguish:
  - truth committed but downstream lagged
  - truth committed and downstream rebuilt later
  - true visibility problem vs derived-state freshness problem
  - duplicate delivery vs stale-version arrival
  - safe fallback behavior vs genuine serving failure

### Investigation-ready questions
Content observability should make it possible to answer:
- what truth version is authoritative right now?
- which version was emitted to outbox?
- which downstream consumers have applied that version?
- was an older version rejected or did it incorrectly win somewhere?
- was a missing derived effect later recovered by replay/rebuild?

---

## 7) Release gates (recommended)

During rollout, gate on:
- publish/unpublish 5xx/timeouts spike
- sustained P99 latency regression for publish/unpublish
- outbox oldest pending age sustained growth (content events)
- downstream DLQ growth or DLQ oldest age breach (audit/notifications)
- replay/reconciliation failure spikes for important derived outputs
- sustained growth in stale-event rejection or version-gap/resync-trigger counts
- abnormal increase in truth fallback rate due to stale/missing derived state
- any visibility-leak signal (immediate stop/rollback)

### Strong stop conditions
Immediate pause/rollback is recommended if any of the following is observed:
- draft/unpublished content leak
- repeated publish/unpublish disagreement between truth and serving path
- rebuild/cutover publishing older visibility assumptions over newer Content truth
- runaway duplicate downstream side effects from Content events

---

## 8) Operator questions this module must answer

Content observability should help answer:

1. Did Content truth commit successfully?  
2. Was the correct version emitted to Outbox?  
3. Are downstream systems simply lagging, or is a replay/reconciliation workflow failing?  
4. Is public invisibility caused by Content truth, or by stale/missing derived state?  
5. Are rebuild/reconciliation workflows preserving truth-first behavior correctly?  
6. Are downstream consumers rejecting stale versions and duplicates correctly?  
7. Is truth fallback being used because derived state is behind, or because a serving artifact is broken?  
8. Has a bounded rebuild/replay recovered the intended derived output safely?