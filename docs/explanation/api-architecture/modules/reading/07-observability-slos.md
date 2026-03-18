# Reading — Observability & SLO Signals (V1)

Related:
- `../../../../architecture/arc42/04-runtime-view-v1.md`
- `../../../../architecture/arc42/11-replication-v1.md`
- `../../../../architecture/arc42/18-stream-processing-and-derived-state-v1.md`
- `../../../../architecture/arc42/19-stream-processing-runtime-v1.md`
- `../../../../decisions/adr-0015-cache-policy-and-invalidation-redis-v1.md`
- `../../../../decisions/adr-0027-stream-processing-and-derived-state-policy-v1.md`
- `../../../../decisions/adr-0028-consumer-idempotency-replay-and-rebuild-policy-v1.md`

---

## 1) SLO focus (top priority)

### Endpoints
- `GET /api/v1/articles` (list)
- `GET /api/v1/articles/{slug}` (preferred) or `{id}` variant (detail)

### SLIs (core)
- P95/P99 latency (list vs detail tracked separately)
- error rate (5xx + timeouts)
- degradation success rate (reading works when non-critical deps fail)

### Correctness SLIs (must-have)
- safe visibility enforcement:
  - "draft/unpublished leak" incidents = **0**
- "slug resolved but denied by visibility" rate (internal signal; should not leak info to clients)
- not-found accuracy:
  - ratio of true 404 vs "stale-induced 404" (if detectable)
- truth-vs-derived disagreement rate:
  - derived route/projection/cache suggested readable
  - Content truth denied readability
- truth-fallback success rate:
  - stale/missing derived input occurred
  - public response still served correctly from truth-backed path

### Interpretation rule
Reading observability must distinguish:
- read-path correctness failure
- read-path latency degradation
- enrichment lag only
- routing success followed by truth-safe denial
- safe degraded response versus actual incident

---

## 2) Dependency health signals

### SEO routing (hot path)
- `/resolve` latency and error rate
- cache hit rate (if Redis enabled)
- DB fallback rate (% cache miss/stale fallback)
- resolved-but-not-readable rate (joined internal signal with Content visibility outcome)

### Content truth queries
- Content query latency (list/detail)
- DB slow query indicators for published reads
- truth-read amplification during fallback periods

### Media
- media fetch latency (metadata)
- missing media / placeholder rate (degrade signal)

### Interaction (non-blocking)
- view signal enqueue success/failure (if measured)
- aggregation backlog/lag (queue depth, consumer latency)
- counters freshness lag (if available)
- duplicate/replay signal indicators where measurable

### Replication freshness (system-level impact)
- outbox oldest pending age (seconds)
- worker queue depth for SEO/interaction consumers
- sustained backlog growth correlation with read-path anomalies
- derived freshness lag for counters/summaries where available

### Dependency interpretation rule
Operators should be able to tell whether the problem is:
- SEO routing degradation
- Content truth query degradation
- media partial degradation
- interaction lag only
- derived-state lag causing more truth fallback

---

## 3) Derived-state freshness and fallback signals

Reading consumes derived inputs that may lag:
- counters
- SEO metadata enrichments
- caches
- future summary/projection fragments
- batch-generated trending or summary inputs

Important signals:
- truth fallback rate due to stale/missing derived inputs
- omitted-enrichment rate
- stale-derived suspicion rate (if detectable)
- cache-stale fallback rate
- detail/list served successfully despite missing enrichments
- derived fragment rejected/ignored due to freshness/version uncertainty where measurable
- safe omission count by enrichment type:
  - counters
  - metadata
  - media adjuncts
  - summary/trending fragments

**Rule:** fallback-to-truth is a success-preserving behavior, not automatically an incident.  
But sustained fallback growth is an operational signal that derived workflows are unhealthy.

### Reading-specific freshness rule
A stale enrichment is acceptable.
A stale enrichment that changes readable/not-readable behavior is not acceptable.

---

## 4) Batch / rebuild / reconciliation signals (Reading-specific)

These apply when Reading uses bounded workflows for:
- counters
- trending inputs
- summary enrichments
- future read-model fragments
- repair/rebuild of reading-derived outputs

### Workflow health SLIs
- run success/failure count
- run duration
- records selected / processed / skipped / repaired
- mismatch count from reconciliation
- repair applied count
- rerun count
- candidate output generation success/failure
- publication/cutover success/failure
- freshness age of active derived output

### Replay / recovery indicators
- replay count
- rebuild triggered count
- repeated mismatch on same bounded scope
- candidate-built-but-not-published count
- stale input / stale candidate reject count where measurable

### Ownership-sensitive workflow signals (if introduced later)
- duplicate-run detection count
- stale-owner rejection count
- safe no-owner / degraded intervals

### Policy
Reading correctness must remain acceptable even when these workflows lag or fail,
provided truth-backed fallback still works.

### Recovery interpretation rule
Observability should make it clear whether:
- bounded repair is enough
- full rebuild is needed
- derived output is repeatedly drifting after successful rebuild
- cutover is blocked while truth-safe serving still continues

---

## 5) Degraded-but-acceptable behavior (V1)

Reading is considered "degraded but acceptable" when:
- article list/detail still returns successfully
- counters may be missing/stale
- SEO metadata may be stale
- media may fall back to placeholder
- view tracking may be delayed/dropped
- reading-derived summaries may lag while truth-safe fallback still works
- route resolution may require slower truth-backed fallback instead of fast cache/projection path

Reading is **not acceptable** when:
- drafts/unpublished content is exposed
- slug routing produces misleading empty/404 when truth indicates Published (sustained)
- P99 latency/error rate breaches release gates
- active derived output publication leaks partial or inconsistent reading-side state where that output is meant to be active
- stale derived serving data is trusted over Content visibility truth

### Operator rule
Degraded-but-acceptable means:
- correctness preserved
- latency and completeness may worsen
- recovery work may be needed
- no visibility leak is allowed

---

## 6) Release gates (rollout policy)

During rollout, gate on:
- P99 latency deltas (list/detail)
- error spikes (5xx/timeouts)
- SEO resolve error spikes
- SEO DB fallback rate spikes (cache drift / replication lag indicator)
- worker backlog growth (outbox age, queue depth) for SEO/interaction consumers
- rebuild/reconciliation failure spikes for reading-derived outputs
- candidate publication/cutover failure for important reading-side derived outputs
- sustained growth in truth fallback rate caused by stale/missing derived state
- any visibility-leak signal (must rollback/stop immediately)

### Strong stop conditions
Immediate pause/rollback is recommended if:
- draft/unpublished content is exposed
- route resolution plus composition is producing misleading 404s at meaningful volume
- partial or stale derived output is being published as active reading-side state
- truth fallback is broken when derived state is stale

---

## 7) Operator questions this module must answer

Reading observability should help answer:

1. Is the read path itself unhealthy, or are only enrichments lagging?  
2. Did slug routing fail, or did truth visibility deny the article?  
3. Are counters/summaries stale because async consumers lagged, or because a rebuild/reconciliation workflow failed?  
4. Are we correctly falling back to truth?  
5. Is derived-state lag operationally acceptable, or is repair/rebuild now required?  
6. Are misleading 404s coming from stale route/projection/cache behavior rather than actual truth-backed absence?  
7. Is the system serving a degraded-but-correct response, or drifting into incorrect derived-state behavior?  
8. Did a replay/rebuild workflow safely restore a derived reading output, or is cutover still pending?