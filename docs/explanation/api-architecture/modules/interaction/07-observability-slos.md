# Interaction — Observability & SLO Signals (V1)

Related:
- `../../../../architecture/arc42/04-runtime-view-v1.md`
- `../../../../architecture/arc42/11-replication-v1.md`
- `../../../../architecture/arc42/18-stream-processing-and-derived-state-v1.md`
- `../../../../architecture/arc42/19-stream-processing-runtime-v1.md`
- `../../../../decisions/adr-0013-outbox-and-delivery-semantics-v1.md`
- `../../../../decisions/adr-0027-stream-processing-and-derived-state-policy-v1.md`
- `../../../../decisions/adr-0028-consumer-idempotency-replay-and-rebuild-policy-v1.md`

---

## 1) Critical SLIs

### Views (non-blocking)
- view accepted rate
- view signal enqueue success/failure rate (if measured)
- view endpoint latency (P95/P99) — must not regress read path

Notes:
- duplicates are acceptable in V1 unless a dedupe policy is enabled.
- accepted view signal does **not** imply counter freshness.
- observability should distinguish:
  - accepted but not yet aggregated
  - accepted and aggregated
  - dropped/sampled by policy
  - rejected due to overload or abuse controls if such policy exists

### Likes / unlikes
- success rate and latency (P95/P99)
- uniqueness / conflict anomalies:
  - unique constraint violations (should map to idempotent success or expected no-op)
  - unexpected 409/5xx spikes
- like/unlike churn rate (can indicate bots or UI retries)
- stale-write / concurrency conflict rate if implemented
- timeout-then-retry ambiguity rate where observable

### Comments
- create/edit/delete success rate and latency (P95/P99)
- moderation/validation failure rate (4xx)
- rate-limit trigger rate (spam/bot signals)
- idempotency-key hit/conflict rate for comment create, if implemented
- duplicate-comment suspicion rate where observable

### Interpretation rule
Interaction observability must separate:
- truth-write health
- async aggregation lag
- replay/rebuild activity
- abuse/bot patterns
- safe degraded behavior versus actual correctness risk

---

## 2) Backlog / lag (if async aggregation exists)

### Replication freshness (Outbox → broker → consumers)
- outbox pending count (interaction events)
- outbox oldest pending age (seconds)
- broker queue depth (ready/unacked) for aggregation consumers
- consumer processing latency (P95/P99)
- consumer failure rate + retry volume (with error classification)
- DLQ size and DLQ oldest age (if enabled)

### Aggregation correctness signals (eventual)
- counter freshness lag (if measurable): time since last counter update per article/sample
- reconciliation/backfill job success (if implemented)
- stale-event reject/resync count (if implemented)
- dedupe hit rate by consumer/message type
- replay count / replay-trigger count
- late-arrival count where time/window-sensitive aggregation exists

### Stream-health interpretation
Operators should be able to tell whether:
- interaction truth committed successfully but aggregation is behind
- outbox is blocked before broker publish
- broker is healthy but consumers are lagging
- duplicates are being handled safely
- stale or replayed events are triggering resync/rebuild instead of corrupting aggregates

---

## 3) Derived aggregate rebuild / reconciliation signals

These apply when Interaction runs bounded workflows for:
- counter rebuild
- popularity/trending recompute
- replay of raw events
- reconciliation of aggregate tables vs truth/raw input
- cleanup / retention jobs

### Workflow health SLIs
- run success/failure count
- run duration
- records selected / processed / skipped / repaired
- mismatch count from reconciliation
- aggregate rebuild count
- candidate output generation success/failure
- publication/cutover success/failure for important derived aggregates
- freshness age of active derived aggregate output where measurable

### Replay / recovery indicators
- replay count
- rerun count
- repeated mismatch on same bounded scope
- candidate-built-but-not-published count
- stale snapshot / stale candidate reject count where measurable
- full rebuild chosen vs partial repair count

### Ownership-sensitive workflow signals (if introduced later)
- duplicate-run detection count
- stale-owner rejection count
- safe no-owner / degraded intervals

### Policy
Interaction truth remains authoritative even when aggregate workflows lag or fail.
Counters and scores may lag, but must not be mistaken for correctness truth.

### Recovery posture signals
Observability should make it clear whether:
- bounded repair is enough
- full rebuild is being chosen because it is safer
- derived aggregate output keeps drifting after successful rebuild
- cutover is blocked while truth remains healthy

---

## 4) Read-path protection signals

Interaction must not degrade reading.

Signals:
- correlation between read traffic spikes and interaction degradation:
  - view enqueue failures vs read error rate
  - interaction queue lag vs read latency
- "reading degradation did not increase reading errors/latency" checks:
  - read path P99 latency and error rate remain stable when interaction backlog grows
- placeholder counters rate (counters missing/stale) vs user-facing complaints (optional)
- truth-fallback / omit-enrichment growth on Reading side during interaction lag
- sustained stale-counter exposure rate where measurable

Guardrail:
- if interaction degradation causes reading failures, treat as a release blocker.

### Protection rule
Degraded Interaction is acceptable when:
- reads still succeed
- counters may be stale or omitted
- no truth-sensitive behavior depends on aggregate freshness

It is not acceptable when:
- reading latency/error rate regresses materially because Interaction is unhealthy
- read path blocks on view/counter processing
- stale aggregates are treated as correctness truth

---

## 5) Security anomaly signals

### Abuse patterns
- bursts of comment creation from few IPs/users
- unusual like/unlike churn (toggle storms)
- spikes in rate-limit triggers (by endpoint and clientKey)
- repeated attempts that result in no-op (idempotent duplicates) at high volume (possible bot retries)
- replay-like bursts on the same interaction keys/message ids
- suspicious retry storms after timeouts

### Suspicious distribution
- top-N actors by volume (users/IPs) for comments/likes/views
- sudden shifts in geo/user-agent (if tracked per privacy policy)
- outlier articles with abnormal interaction velocity
- skew toward one endpoint or one article indicating attack or automation

### Operational interpretation
Security/anomaly signals should help distinguish:
- legitimate traffic spikes
- UI retry bugs
- broker replay storms
- bot or abuse campaigns
- scraper/view-spam patterns if view signals are externally exposed

---

## 6) Release gates (recommended)

During rollout, gate on:
- interaction endpoints 5xx/timeouts spikes
- sustained P99 latency regression for like/comment endpoints
- sustained outbox oldest pending age growth (interaction events)
- DLQ growth / DLQ oldest age breach for aggregation consumers
- rebuild/reconciliation failure spikes for important derived aggregates
- sustained dedupe-hit or stale-event-reject spikes beyond expected baseline
- candidate publication/cutover failure for important aggregate or trending outputs
- any measurable impact on read path error rate/latency due to interaction degradation

### Strong stop conditions
Immediate pause/rollback is recommended if:
- interaction degradation materially harms read-path latency or availability
- like/comment truth becomes inconsistent or duplicate-active-like anomalies appear
- aggregate cutover publishes stale or partial output as active state
- replay/rebuild logic is corrupting instead of repairing derived aggregates

---

## 7) Operator questions this module must answer

Interaction observability should help answer:

1. Did interaction truth commit successfully?  
2. Are counters merely stale, or is truth itself inconsistent?  
3. Is the problem in live ingestion, async aggregation, or rebuild/reconciliation?  
4. Are duplicate spikes coming from retries, bots, or replay storms?  
5. Is the read path still protected while Interaction degrades?  
6. Are stale or replayed events being handled safely, or are they corrupting aggregates?  
7. Is bounded repair enough, or is a full rebuild now the safer recovery path?  
8. Is Interaction currently degraded-but-acceptable, or has it crossed into read-path or truth-path risk?