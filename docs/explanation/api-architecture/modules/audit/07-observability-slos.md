# Audit — Observability & SLO Signals (V1)

Related:
- `../../../../architecture/arc42/04-runtime-view-v1.md`
- `../../../../architecture/arc42/11-replication-v1.md`
- `../../../../architecture/arc42/18-stream-processing-and-derived-state-v1.md`
- `../../../../architecture/arc42/19-stream-processing-runtime-v1.md`
- `../../../../decisions/adr-0013-outbox-and-delivery-semantics-v1.md`
- `../../../../decisions/adr-0027-stream-processing-and-derived-state-policy-v1.md`
- `../../../../decisions/adr-0028-consumer-idempotency-replay-and-rebuild-policy-v1.md`

---

## 1) Worker ingestion SLIs (must measure)

### Core pipeline health
- handler success/failure rate (by eventType)
- processing latency (P95/P99)
- retry volume (and retry reasons classification)
- queue backlog/lag:
  - broker queue depth (ready/unacked)
  - consumer lag trend
- DLQ size and growth
- **DLQ oldest age** (time since oldest DLQ message)

### Replication freshness (outbox-to-audit)
- outbox oldest pending age (seconds) for audit-relevant events (system-level)
- outbox pending count (trend)
- publish failure rate (outbox → broker), if tracked

### Duplicate prevention
- dedupe hits / unique-key conflicts rate (expected under at-least-once)
- ratio of `inserted` vs `deduped` outcomes (unexpected spikes can indicate retry storms)

### Interpretation rule
Observability must distinguish:
- upstream truth is healthy but audit ingestion is lagging
- broker/backlog is the bottleneck
- consumer processing is failing
- dedupe pressure is rising because of replay/retry storms
- canonical evidence truth is intact even while derived reporting is behind

---

## 2) Audit completeness signals

### Time-to-ingest (freshness distribution)
Measure distribution:
- `event.occurredAt → audit.storedAt`

Report:
- P50/P95/P99 time-to-ingest
- tail growth detection (P99 drift)

### Missing audit detection (optional but recommended for governance)
For governance-critical actions (publish/unpublish, role/permission changes):
- compare count of governance events emitted vs count of audit inserts (by correlationId/eventType) over a window
- track mismatch rate (%)

Sampling option (V1-friendly):
- sample 1–5% of governance actions and verify audit presence within a bounded time window.

### Canonical mapping correctness
- ensure "one event -> one audit record"
- alert if a single correlationId produces unexpected duplicate audit records (beyond dedupe expectations)

### Completeness interpretation rule
Operators should be able to tell whether:
- evidence is truly missing
- evidence is only delayed
- reconciliation mismatch is coming from backlog
- the issue is canonical ingest failure versus lagging summary/report generation

---

## 3) Derived reporting / archival workflow signals

These apply when Audit runs bounded workflows for:
- archival
- summarization
- completeness reconciliation
- replay candidate generation
- derived reporting outputs

### Workflow health SLIs
- run success/failure count
- run duration
- records selected / processed / skipped / archived
- mismatch count from completeness reconciliation
- replay candidate count
- archival lag / pending archival window age
- summary freshness age
- candidate output generation success/failure
- publication/cutover success/failure for important derived report outputs

### Replay / recovery indicators
- replay count
- rerun count
- repeated mismatch on same bounded scope
- candidate-built-but-not-applied count
- full bounded reprocess chosen vs targeted replay count
- stale candidate / stale snapshot reject count where measurable

### Ownership-sensitive workflow signals (if introduced later)
- duplicate-run detection count
- stale-owner rejection count
- safe no-owner / degraded intervals

### Policy
Canonical audit evidence remains authoritative even when these workflows lag or fail.
Derived reports and summaries may lag, but must not be mistaken for evidence truth.

### Recovery interpretation rule
Observability should make it clear whether:
- live ingestion is healthy but archival/reporting is behind
- completeness reconciliation is finding real evidence gaps
- replay is actually closing gaps
- derived report cutover is failing while append-only evidence truth remains intact

---

## 4) Admin read SLIs

Endpoints / actions:
- audit search/list
- audit detail lookup by id/correlationId

SLIs:
- search latency (P95/P99)
- error rates (5xx/timeouts)
- query volume and rate-limit triggers (if admin search is protected)
- slow query indicators (DB-level) for common filters (actor, resource, time range)

Correctness/UX signals:
- "empty search results while ingestion backlog is high" rate (helps distinguish lag vs true absence)
- dashboard hint: surface ingestion lag status to admins when backlog is elevated
- stale summary/report usage rate if derived reporting outputs are behind

### Investigation usability signals
- time-to-first-result for common investigation queries
- correlationId lookup success rate
- actor/resource filter hit usefulness (optional quality signal)
- rate of queries that fall back from derived report view to canonical evidence search, if such dual path exists

### Interpretation rule
Admin read health should let operators distinguish:
- true absence of evidence
- evidence delayed by ingestion lag
- evidence present but reporting/index path lagging
- slow admin query due to storage/indexing issue rather than missing audit facts

---

## 5) Security anomaly signals

Access and investigation signals:
- sudden spikes in audit search queries
- repeated access denials (403) to audit endpoints
- unusual query patterns:
  - very broad time ranges
  - high-frequency pagination
  - repeated lookups of sensitive resource types (policy-defined)

Pipeline integrity signals:
- spikes in failed ingestion with the same error signature
- DLQ oldest age growth (indicates stuck remediation)
- unexpected drop in audit ingestion volume for governance events (possible broken event pipeline)
- repeated reconciliation mismatch spikes (possible systemic completeness issue)

### Evidence integrity signals
- unexpected duplicate canonical audit rows
- unexpected drop in dedupe-hit rate during known replay/retry incident
- append-only integrity violations or forbidden mutation attempts, if such checks exist
- correction-model usage spike, if explicit correction records are supported later

### Interpretation rule
Security/anomaly observability should help distinguish:
- operator misuse or suspicious audit browsing
- broken upstream event production
- broken ingest mapping or consumer deployment
- replay/remediation instability
- evidence-integrity risk versus mere reporting lag

---

## 6) Release gates (recommended)

During rollout, gate on:
- ingestion failure spikes
- sustained backlog/lag growth (queue depth + outbox age)
- DLQ non-zero and growing / DLQ oldest age breach
- time-to-ingest P99 regression
- reconciliation mismatch spikes for governance-critical audit evidence
- candidate publication/cutover failure for important derived audit report outputs
- unexpected duplicate canonical audit records (release blocker)

### Strong stop conditions
Immediate pause/rollback is recommended if:
- append-only evidence integrity is violated
- duplicate canonical evidence is being created for the same canonical event
- governance-critical audit completeness drops materially without clear replay recovery
- remediation/replay is mutating or contradicting evidence instead of safely filling gaps

---

## 7) Operator questions this module must answer

Audit observability should help answer:

1. Did the originating business event happen, but audit is merely lagging?  
2. Are duplicates coming from retry storms, replay, or upstream event duplication?  
3. Is there canonical evidence missing, or only a lagging summary/dashboard?  
4. Is the problem in live ingestion, replay/remediation, or archival/reporting workflow?  
5. Are we preserving append-only evidence truth while derived reporting outputs recover?  
6. Is replay actually closing completeness gaps, or just reprocessing already-deduped events?  
7. Is the current incident an evidence-truth problem, a backlog problem, or only a derived-reporting problem?