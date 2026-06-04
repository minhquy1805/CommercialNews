# Audit — Observability & SLO Signals (V1)

## 1. Purpose

This document defines observability signals, SLI groups, interpretation rules, and release gates for the Audit module in CommercialNews V1.

Audit V1 is intentionally simple in runtime scope:

* async audit ingestion
* SQL-backed append-only `AuditLog`
* consumer-side `AuditIngestion`
* durable `MessageId` deduplication
* investigation query APIs
* lightweight dashboard queries directly from `AuditLog`

Future capabilities such as replay, reconciliation, archival, materialized summaries, alerts, digests, Redis cache acceleration, and partitioning are valid extension paths, but they are not required for V1 core delivery.

Related:

* `../../../../architecture/arc42/04-runtime-view-v1.md`
* `../../../../architecture/arc42/06-measurement-guide.md`
* `../../../../architecture/arc42/11-replication-v1.md`
* `../../../../architecture/arc42/13-transactions-and-consistency-v1.md`
* `../../../../architecture/arc42/16-batch-processing-and-derived-data-v1.md`
* `../../../../architecture/arc42/17-dataflow-and-batch-workflows-v1.md`
* `../../../../architecture/arc42/18-stream-processing-and-derived-state-v1.md`
* `../../../../architecture/arc42/19-stream-processing-runtime-v1.md`
* `../../../../decisions/adr-0013-outbox-and-delivery-semantics-v1.md`
* `../../../../decisions/adr-0015-cache-policy-and-invalidation-v1.md`
* `../../../../decisions/adr-0017-partitioning-strategy-v1.md`
* `../../../../decisions/adr-0018-transaction-boundaries-and-consistency-model-v1.md`
* `../../../../decisions/adr-0020-timeout-retry-and-failure-detection-policy-v1.md`
* `../../../../decisions/adr-0025-batch-processing-and-derived-state-policy-v1.md`
* `../../../../decisions/adr-0026-batch-job-orchestration-and-materialization-policy-v1.md`
* `../../../../decisions/adr-0027-stream-processing-and-derived-state-policy-v1.md`
* `../../../../decisions/adr-0028-consumer-idempotency-replay-and-rebuild-policy-v1.md`

---

## 2. Observability Posture

Audit observability must distinguish four different runtime states:

1. Source module truth committed.
2. Outbox publication is pending, failed, or published.
3. Audit consumer has or has not processed the message.
4. Audit evidence has or has not been persisted.

Important rule:

`OutboxMessage.Status = Published` does not mean Audit processed the message.

Audit V1 must observe:

* producer-side backlog signals from Outbox where available
* broker queue/backlog signals
* consumer-side processing signals from `AuditIngestion`
* evidence persistence signals from `AuditLog`
* admin query health signals

Audit lag is acceptable only if it remains:

* visible
* bounded by operational expectation
* recoverable
* investigation-safe

---

## 3. V1 Worker Ingestion SLIs

These are required for Audit V1.

### 3.1 Core consumer health

Measure:

* audit consumer success count
* audit consumer failure count
* audit consumer success/failure rate by `EventType`
* audit consumer success/failure rate by `SourceModule`
* processing latency P50/P95/P99
* consumer retry count
* retry reason classification
* mapping failure count
* redaction failure count
* unsupported event count
* ignored event count
* dead-lettered count

Recommended dimensions:

* `eventType`
* `sourceModule`
* `consumerName`
* `status`
* `errorClass`
* `riskLevel`
* `severity`

### 3.2 Broker and queue health

Measure:

* Audit queue depth
* ready message count
* unacked message count
* consumer count
* consumer lag trend
* DLQ size
* DLQ oldest age
* redelivery count where available

Interpretation:

* growing ready count usually means consumers are not keeping up
* growing unacked count may indicate slow or stuck processing
* growing DLQ oldest age indicates unresolved poison or policy failures

### 3.3 Replication freshness

Measure:

* publish-to-ingest lag
* occurred-to-ingest lag
* oldest unprocessed audit message age where measurable
* Outbox pending count for audit-relevant events where available
* Outbox oldest pending age for audit-relevant events where available
* Outbox publish failure rate where available

Definitions:

```text
publish-to-ingest lag = AuditLog.IngestedAtUtc - OutboxMessage.PublishedAt
occurred-to-ingest lag = AuditLog.IngestedAtUtc - OutboxMessage.OccurredAt
```

If `PublishedAt` is not available in the consumer payload, publish-to-ingest lag may be approximated using available broker/worker timestamps.

### 3.4 Duplicate prevention

Measure:

* `MessageId` dedupe hit count
* `AuditLog.MessageId` unique-key conflict count
* `AuditIngestion.MessageId` duplicate/seen-before count
* ratio of inserted vs deduped outcomes
* duplicate spikes by `EventType` and `SourceModule`

Interpretation:

* some dedupe hits are normal under at-least-once delivery
* sudden dedupe spikes may indicate retry storms, broker redelivery, or publisher ambiguity
* duplicate canonical `AuditLog` records for the same `MessageId` are a correctness incident

### 3.5 AuditIngestion status distribution

Measure count by status:

* `Processing`
* `Succeeded`
* `Duplicate`
* `Ignored`
* `Failed`
* `DeadLettered`

Recommended dashboard panels:

* failed ingestion count
* oldest failed ingestion age
* dead-lettered count
* duplicate count
* processing count
* success rate over time

Interpretation:

* `Failed` means consumer-side Audit processing failed
* `DeadLettered` means terminal consumer-side failure handling
* these statuses do not mean source module truth failed

---

## 4. V1 Audit Evidence SLIs

### 4.1 Evidence persistence

Measure:

* AuditLog insert success count
* AuditLog insert failure count
* AuditLog insert latency P50/P95/P99
* AuditLog unique-key conflict count by `MessageId`
* AuditLog write timeout count
* AuditLog persistence dependency failure count

### 4.2 Evidence volume

Measure:

* total audit records over time
* audit records by `SourceModule`
* audit records by `EventType`
* audit records by `Action`
* audit records by `ActionCategory`
* audit records by `Outcome`
* audit records by `Severity`
* audit records by `RiskLevel`

### 4.3 Evidence freshness

Measure:

* occurred-to-ingest lag P50/P95/P99
* lag by source module
* lag by event type
* lag by risk level
* lag for governance/security-sensitive events

High-risk governance events should have tighter operational expectations than low-risk bulk events.

### 4.4 Evidence integrity signals

Measure or detect:

* duplicate `MessageId` records in `AuditLog`
* forbidden mutation attempts if detectable
* unexpected delete attempts if detectable
* redaction policy violations
* records missing required canonical fields
* records with unsupported enum values
* records with invalid `PublicId` or `MessageId` format

Strong stop condition:

Duplicate canonical evidence for the same `MessageId` is a release blocker.

---

## 5. V1 Admin Read SLIs

Applies to:

* audit search/list
* audit detail by `PublicId`
* lookup by `MessageId`
* lookup by `CorrelationId`
* module logs
* actor timeline
* resource timeline
* ingestion status APIs
* failed ingestion APIs

### 5.1 Latency

Measure:

* endpoint latency P50/P95/P99
* audit search latency
* audit detail lookup latency
* messageId lookup latency
* correlationId lookup latency
* actor timeline latency
* resource timeline latency
* ingestion query latency

### 5.2 Error rate

Measure:

* 4xx rate by endpoint and error code
* 5xx rate by endpoint
* timeout rate
* dependency unavailable count
* rate-limit count

### 5.3 Query pressure

Measure:

* query volume by endpoint
* repeated wide time-range queries
* high page/pageSize usage
* unsupported sort/filter attempts
* slow query count
* database CPU/IO pressure for Audit queries where available

### 5.4 Investigation usability

Optional quality signals:

* time-to-first-result for common investigation queries
* correlationId lookup success rate
* actor/resource timeline usage
* no-result rate while ingestion backlog is high
* detail lookup after dashboard click-through rate

Interpretation:

Admin read health should help operators distinguish:

* true absence of evidence
* evidence delayed by ingestion lag
* evidence present but query/index path slow
* dashboard summary lag versus canonical evidence availability

---

## 6. V1 Dashboard SLIs

V1 dashboard views are lightweight derived views computed directly from SQL-backed `AuditLog` and `AuditIngestion`.

Applies to:

* `/dashboard/summary`
* `/dashboard/recent-risk-events`

### 6.1 Dashboard latency and reliability

Measure:

* dashboard summary latency P50/P95/P99
* recent-risk-events latency P50/P95/P99
* dashboard query error rate
* dashboard query timeout count
* dashboard time-window size distribution

### 6.2 Dashboard content signals

Measure:

* total audit event count in selected window
* high-risk event count
* critical event count
* failed ingestion count
* duplicate ingestion count
* events by module
* events by severity
* events by risk level
* oldest failed ingestion age

### 6.3 Interpretation rule

Dashboard output is derived.

Dashboard failure or lag does not mean canonical evidence is lost.

Dashboard output must not be treated as stronger truth than `AuditLog`.

If dashboard query load becomes expensive, future versions may introduce cache or materialized summaries, but those outputs remain derived.

---

## 7. Security and Abuse Signals

### 7.1 Audit API access signals

Measure:

* audit endpoint request volume
* audit endpoint access denied count
* repeated 403s
* repeated 401s
* rate-limit triggers
* unusual query volume by actor
* broad time-range query attempts
* high-frequency pagination
* repeated lookups of sensitive resource types
* repeated raw/sensitive field access attempts if such access is introduced

### 7.2 Pipeline integrity signals

Measure:

* sudden drop in audit ingestion volume for governance-critical events
* sudden spike in ingestion failures
* repeated failures with same error signature
* DLQ growth
* DLQ oldest age growth
* mapping failure spikes after deployment
* redaction failure spikes after deployment
* unexpected unsupported event type spikes

### 7.3 Evidence integrity signals

Measure or detect:

* unexpected duplicate canonical audit rows
* unexpected absence of high-risk governance events
* append-only integrity violations
* forbidden update/delete attempts if detectable
* redaction policy violations
* unsafe payload detection count

### 7.4 Interpretation rule

Security/anomaly observability should help distinguish:

* suspicious audit browsing
* broken upstream event production
* broken Outbox publication
* broken broker delivery
* broken Audit consumer mapping
* broken redaction
* replay/remediation instability in future workflows
* evidence integrity risk versus dashboard lag

---

## 8. V1 Release Gates

During rollout, gate on the following Audit signals.

### 8.1 Warning gates

Investigate before continuing rollout if:

* ingestion failure rate increases after deployment
* mapping failure count increases after deployment
* redaction failure count increases after deployment
* queue depth grows continuously
* unacked message count grows continuously
* occurred-to-ingest P99 regresses materially
* duplicate/dedupe hit rate spikes unexpectedly
* dashboard query latency regresses materially
* admin read API 5xx rate rises

### 8.2 Strong stop conditions

Pause or rollback is recommended if:

* duplicate canonical `AuditLog` records are created for the same `MessageId`
* append-only evidence integrity is violated
* Audit stores or returns raw secrets/tokens
* redaction policy is bypassed
* governance-critical audit ingestion fails broadly
* DLQ is non-zero and growing without remediation path
* Audit API exposes data to unauthorized users
* Audit ingestion failure is incorrectly written back as producer-side Outbox failure
* source module truth flow starts depending on Audit completion

---

## 9. Future Extension Signals

The following signals are not required for V1 core delivery, but should be introduced if the corresponding feature is added.

---

## 9.1 Replay / remediation signals

Applies if replay or remediation controls are introduced.

Measure:

* replay run count
* replay success/failure count
* replay duration
* replay scope size
* replay inserted count
* replay deduped count
* replay still-failing count
* replay escalated count
* repeated replay of same message
* replay overlap with live ingestion
* unsafe replay rejection count

Interpretation:

Replay should close evidence gaps without duplicating already-recorded evidence.

---

## 9.2 Completeness / reconciliation signals

Applies if reconciliation workflows are introduced.

Measure:

* reconciliation run count
* reconciliation success/failure count
* reconciliation duration
* expected event count
* persisted evidence count
* mismatch count
* mismatch rate
* replay candidate count
* stale candidate rejection count
* repeated mismatch on same bounded scope

Interpretation:

Reconciliation output is derived. It helps detect possible gaps but does not replace `AuditLog` evidence truth.

---

## 9.3 Archival / retention signals

Applies if archival or retention workflows are introduced.

Measure:

* archive run count
* archive success/failure count
* archival lag
* archived-through timestamp
* archive candidate count
* archive validation failure count
* purge count if policy allows purge
* retention policy violation count
* archive read fallback count

Interpretation:

Archive lag does not mean canonical evidence is lost unless retention policy explicitly moved evidence to an archive tier.

---

## 9.4 Summary / digest / reporting signals

Applies if materialized summaries, digests, or reports are introduced.

Measure:

* summary build count
* digest generation count
* report generation count
* run duration
* records selected
* records processed
* records skipped
* candidate output generated count
* publication/cutover success/failure count
* active output freshness age
* rerun count
* stale candidate rejection count

Interpretation:

Derived reports and summaries may lag. They must not be mistaken for canonical evidence truth.

---

## 9.5 Alerting and notification signals

Applies if `AuditAlert` or notification integration is introduced.

Measure:

* alert created count
* alert dedupe hit count
* alert rule evaluation failure count
* alert acknowledgement count
* alert resolved count
* AuditAlertRaised outbox publish count
* notification delivery success/failure from Notifications
* duplicate alert reject count
* alert backlog count

Interpretation:

Audit may own alert state, but Notifications owns delivery.

Audit must not send email directly.

---

## 9.6 Cache signals

Applies if Redis acceleration is introduced.

Measure:

* audit cache hit rate
* dashboard cache hit rate
* cache miss rate
* cache fallback-to-SQL count
* cache latency
* cache error rate
* cache stale detected count where measurable
* cache invalidation/update count if event-driven invalidation is used

Interpretation:

Cache is derived-only. SQL remains audit evidence truth.

---

## 9.7 Partitioning and worker lane signals

Applies if worker lane, queue, or data partitioning is introduced.

Measure:

* lane backlog by partition/lane
* lane oldest message age
* lane processing latency
* high-risk lane delay
* security audit lane delay
* partition ownership changes
* stale owner rejection count
* duplicate-run detection count
* partition rebalance count

Interpretation:

Partitioning must not change `MessageId` dedupe or append-only evidence semantics.

---

## 10. Operator Questions

Audit observability should help operators answer:

1. Did the source business action commit, while Audit is merely lagging?
2. Is the delay in Outbox publication, broker delivery, or Audit consumer processing?
3. Was the message published but not ingested?
4. Was the message ingested but failed before evidence insertion?
5. Was the message already deduped?
6. Are duplicates coming from retry storms, replay, or publisher ambiguity?
7. Is there canonical evidence missing, or only a lagging dashboard summary?
8. Is the problem in live ingestion, query/index performance, or a future derived workflow?
9. Are we preserving append-only evidence truth while operational views recover?
10. Is the current incident an evidence-truth problem, a backlog problem, or only a derived-view problem?
11. Are high-risk/security audit events being delayed by lower-risk/high-volume traffic?
12. Is any audit API access pattern suspicious or abusive?

---

## 11. Summary

Audit Observability V1 is governed by these rules:

1. Separate producer-side Outbox state from Audit consumer-side ingestion state.
2. Track `MessageId` from Outbox through AuditIngestion and AuditLog.
3. Measure ingestion success, failure, duplicate, ignored, and dead-lettered outcomes.
4. Measure occurred-to-ingest and publish-to-ingest lag where possible.
5. Measure queue depth, unacked count, DLQ size, and DLQ oldest age.
6. Measure AuditLog persistence integrity and unique-key conflict behavior.
7. Measure admin read latency, errors, and query pressure.
8. Measure dashboard query health as derived-view health, not evidence truth health.
9. Monitor access-denied spikes, broad searches, and suspicious audit browsing.
10. Treat duplicate canonical AuditLog records for the same `MessageId` as a release blocker.
11. Treat redaction failure or secret leakage as a strong stop condition.
12. Keep replay, reconciliation, archival, alerting, cache, and partitioning signals as future extensions until those capabilities are introduced.
13. Observability must help operators distinguish lag, failure, dedupe, missing evidence, and derived-view delay.
14. A green process is not sufficient if backlog, DLQ, failed ingestion, or evidence integrity signals are unhealthy.
