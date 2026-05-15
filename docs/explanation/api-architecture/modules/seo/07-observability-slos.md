# SEO — Observability & SLO Signals (V1)

Related:
- `../../../../architecture/arc42/04-runtime-view-v1.md`
- `../../../../architecture/arc42/11-replication-v1.md`
- `../../../../architecture/arc42/18-stream-processing-and-derived-state-v1.md`
- `../../../../architecture/arc42/19-stream-processing-runtime-v1.md`
- `../../../../decisions/adr-0013-outbox-and-delivery-semantics-v1.md`
- `../../../../decisions/adr-0027-stream-processing-and-derived-state-policy-v1.md`
- `../../../../decisions/adr-0028-consumer-idempotency-replay-and-rebuild-policy-v1.md`

---

## 1) Critical hot path SLIs

### `/seo/resolve` (routing hot path)
Track separately for:
- success: `200`
- safe not-found: `404` — expected in normal usage
- errors: `5xx` / timeouts — incidents

SLIs:
- P95/P99 latency
- error rate (5xx + timeouts) — tracked separately from 404
- 404 rate (expected baseline, watch for abnormal spikes)
- cache hit ratio (if Redis/edge cache enabled)
- **DB fallback rate** (% cache miss/stale fallback to truth routing table)
- cache set/update failures (optional)

Correctness signals (must-have):
- "resolved → denied by visibility" rate (internal signal from Reading/Content checks; should not leak to clients)
- any draft/non-public exposure incidents = **0** (release blocker)
- truth-vs-derived disagreement rate:
  - route/serving state says resolvable
  - Content truth says non-public
- stale-route suspicion signals where measurable:
  - cache route existed but SEO truth or Content truth rejected serve outcome

### Hot-path interpretation rule
Observability must make it clear whether the problem is:
- routing truth unavailable
- cache layer stale/unhealthy
- downstream serving artifact stale
- Content truth correctly denying visibility after a valid route resolution

---

## 2) Metadata SLIs

### `/seo/metadata`
- latency (P95/P99)
- error rate (5xx/timeouts)
- default/fallback usage rate (how often metadata falls back to defaults due to missing/stale values)

If metadata is derived/projection-based (V2+):
- projection freshness (checkpoint age) and mismatch rate vs truth (optional)
- stale metadata overwrite/reject count if version-aware apply is used
- rebuild/candidate publication freshness if metadata-serving artifacts become cutover-based

### Metadata safety rule
Metadata lag is acceptable.
Metadata lag must never be confused with visibility truth or route safety truth.

---

## 3) Async workflow health (SEO updates)

### Consumer pipeline SLIs
- consumer success/failure rate by event type:
  - `content.article_published`
  - `content.article_unpublished`
  - `content.article_archived`
  - `content.article_soft_deleted`
  - `content.article_restored`
  - `content.article_updated`
  - `seo.slug_route_changed`
  - `seo.slug_route_deactivated`
  - `seo.metadata_updated`
- processing latency (P95/P99)
- retry volume and error classification (transient vs permanent)
- broker queue depth (ready/unacked)
- DLQ size and **DLQ oldest age** (if enabled)

### Replication freshness (Outbox → broker → SEO consumer)
- outbox pending count for SEO-relevant events
- outbox oldest pending age (seconds)
- broker publish failure rate for SEO-relevant events where observable
- queue drain rate during backlog recovery

### Time-to-consistency (Content event → SEO apply)
Measure distribution:
- `OccurredAtUtc` on `content.article_*` event → SEO `appliedAt`
Report P50/P95/P99 time-to-consistency.

Operational expectation:
- routing safety holds even if SEO updates lag (correctness enforced by truth checks).

### Duplicate / stale delivery indicators
- dedupe hits by `MessageId`
- stale-event reject count
- version-gap / resync-trigger count
- out-of-order apply attempts rejected
- repeated cache refresh from older SEO version blocked/rejected where measurable

### Async pipeline interpretation rule
Observability must distinguish:
- event not yet published from outbox
- event published but consumer lagging
- consumer processed duplicate safely
- stale event rejected correctly
- consumer could not converge and triggered resync/rebuild posture

---

## 4) Admin SEO write signals

Track admin SEO APIs separately from public hot-path APIs.

### Admin API SLIs
- `PUT /api/v1/admin/seo/articles/{articlePublicId}` latency (P95/P99)
- `PUT /api/v1/admin/seo/articles/{articlePublicId}` error rate by status class
- `409 Conflict` slug conflict rate
- `412 Precondition Failed` stale version / precondition failure rate
- validation failure rate by error code where available
- idempotency replay/retry convergence count where `Idempotency-Key` is used

### Manual override and sync-protection signals
- manual override update count
- manual override protected/skip count for Content-derived metadata sync
- automatic metadata update count
- automatic metadata update rejected by stale `AggregateVersion`
- manual override state mismatch detected by reconciliation

### SEO truth mutation signals
- SEO truth commit success/failure count
- Outbox write failure count for SEO truth mutations
- post-commit SEO integration event backlog/lag
- cache/sitemap/search refresh lag after admin write, tracked as async lag rather than sync API failure

---

## 5) Batch / rebuild / reconciliation signals (SEO-specific)

These apply when SEO uses bounded workflows for:
- route-serving rebuild
- cache/state reconciliation
- metadata repair
- search/index-supporting derived artifacts (future)

### Workflow health SLIs
- run success/failure count
- run duration
- records selected / processed / skipped / repaired
- route mismatch count from reconciliation
- stale-route repair count
- candidate output generation success/failure
- publication/cutover success/failure
- freshness age of active derived SEO-serving output

### Ordering / ownership-sensitive workflow signals
- stale-event reject count
- version-gap / resync count
- duplicate-run detection count
- stale-owner rejection count (if exclusive workflows are introduced later)
- candidate-built-but-not-published count
- rebuild attempted against stale snapshot count, if detectable

### Policy
Routing truth remains authoritative even when these workflows lag or fail.
Derived serving outputs may lag, but fallback to SEO truth must remain available.

### Recovery posture signals
Operators should be able to see whether:
- bounded repair is enough
- full rebuild is being chosen over partial repair
- repeated drift is occurring for the same scope/slug population
- derived serving cutover is lagging behind healthy SEO truth

---

## 6) Incident signals

### Hot path regressions
- sudden spikes in resolve failures (5xx/timeouts)
- sustained P99 latency regressions on `/seo/resolve`
- DB fallback rate spike (cache drift, invalidation lag, or Redis issues)

### Admin workflow issues
- slug conflict error spikes (unique constraint violations or repeated collisions)
- `412 Precondition Failed` spikes (stale forms or stale clients)
- unusual rate of slug changes/rewrites (possible editorial tooling issue)
- stale-update conflict spikes if optimistic concurrency is used
- manual override protected/skip spikes after Content update bursts
- Outbox write failures on SEO truth mutations

### Safety regressions (highest severity)
- any signal of draft/non-public leak (immediate stop/rollback)
- abnormal rise in "resolved but denied by visibility" (may indicate routing truth drift, stale serving state, or content state regressions)
- publication/cutover of incorrect derived route output (highest severity if such outputs become active)
- stale route surviving after unpublish/archive/soft-delete beyond accepted lag budget
- replay/rebuild publishing older route knowledge over fresher truth

### Duplicate/replay storm indicators
- dedupe hit spikes
- retry spikes
- broker backlog growth combined with stale-event reject spikes
- repeated resync/rebuild triggers for the same route scope

---

## 7) Release gates (recommended)

During rollout, gate on:
- `/seo/resolve` P99 latency sustained regression
- spikes in 5xx/timeouts on resolve/metadata
- sustained DB fallback rate increase (cache/invalidation regression)
- admin SEO write error or latency sustained regression
- SEO consumer backlog/lag sustained growth (outbox age, queue depth)
- DLQ non-zero and growing / DLQ oldest age breach
- rebuild/reconciliation failure spikes for SEO-derived outputs
- candidate publication/cutover failure for important SEO-derived serving outputs
- sustained stale-event reject or version-gap/resync growth beyond expected baseline
- any visibility leak signal (release blocker)

### Strong stop conditions
Immediate pause/rollback is recommended if:
- draft/non-public exposure is detected
- active route-serving artifact contradicts current truth in a way that can expose content incorrectly
- rebuild/cutover publishes stale route knowledge over fresher SEO or Content truth
- routing truth itself becomes inconsistent or conflict errors spike abnormally after rollout

---

## 8) Suggested SLO posture

Baseline targets are policy-level and must be tuned after measurement.

- `/seo/resolve` availability: high availability target, excluding valid `404`.
- `/seo/resolve` latency: track P95/P99; budget should be tighter than metadata/admin APIs.
- visibility leak: zero tolerated incidents.
- SEO async consistency lag: track P95/P99 from Content event occurred time to SEO applied time.
- DLQ/dead-state: should be zero during normal operation; any growth requires investigation.
- rebuild/cutover correctness: zero stale cutover over fresher truth.

Correctness SLOs such as visibility leak and stale cutover are stricter than latency/freshness SLOs.

---

## 9) Sync vs async interpretation rule

A successful admin SEO write means SEO truth committed.
It does not mean cache invalidation, sitemap refresh, search/index refresh, Audit ingestion, or downstream projections have completed.

Async lag must be measured separately through outbox, broker, consumer, cache, and rebuild signals.

---

## 10) Metric cardinality guidance

Metrics must not use raw `slug` or `resourcePublicId` as high-cardinality labels by default.

Allowed labels:
- endpoint
- status class
- eventType
- scope
- dependency
- outcome
- errorCode

Use logs/traces for specific slug/resource investigations.

---

## 11) Operator questions this module must answer

SEO observability should help answer:

1. Is routing truth healthy, or is only cache/derived serving state unhealthy?  
2. Did `/resolve` fail because the slug truly does not exist, or because cache/DB access degraded?  
3. Is stale routing coming from async consumer lag, cache drift, or failed rebuild/reconciliation?  
4. Are stale events or stale writes being rejected correctly?  
5. Is the issue in SEO truth, in async convergence, or in a delayed rebuild/publication path?  
6. Did a duplicate or replayed event converge safely, or did it trigger repeated recovery work?  
7. Is the active route-serving output fresher than the candidate trying to replace it?  
8. Is Content truth correctly protecting visibility even while SEO-derived paths are degraded?
