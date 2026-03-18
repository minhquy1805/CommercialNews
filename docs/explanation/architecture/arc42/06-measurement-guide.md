# 06 — Measurement Guide (Architecture Characteristics)

This guide defines **how we measure** the key architecture characteristics of CommercialNews.
It is intentionally **policy-level** in V1 (no dashboard/alert implementation details yet).

> Related:
> - `05-quality-requirements.md` (what we care about)
> - `quality/` (module characteristic profiles)
> - `04-runtime-view-v1.md` (critical workflows and runtime lanes)
> - `11-replication-v1.md` (replication rules)
> - `16-batch-processing-and-derived-data-v1.md` (batch lane, derived state, rebuild/reconciliation posture)
> - `17-dataflow-and-batch-workflows-v1.md` (workflow stages, materialization, publication/cutover, rerun/recovery)

---

## 6.1 Principles
- Prefer **percentiles (P95/P99)** over averages for latency.
- Measure **read path**, **async side effects**, and **batch/rebuild workflows** separately.
- Treat async processing as **at-least-once**; measure idempotency and duplicate risk.
- Measure batch workflows as **bounded runs**, not as if they were request/response APIs.
- Keep metrics minimal but meaningful: every metric should inform a decision.
- Measure "eventual consistency" explicitly via **freshness/lag signals**.
- Distinguish **truth-path health** from **derived-path freshness**.
- For correctness-sensitive derived outputs, measure both:
  - **candidate build success**
  - **publication/cutover success**

---

## 6.2 System-level measurement plan (V1)

### Security
**SLIs**
- Admin policy coverage (% endpoints protected)
- Rate-limited requests on sensitive endpoints
- PII/token leakage incidents (logs/audit)

**Targets (policy-level)**
- 100% admin endpoints protected
- Zero secrets/tokens/PII in logs/audit

**Signals**
- API logs (structured), auth middleware/policy checks, audit pipeline

---

### Performance (Read Path)
**SLIs**
- P95/P99 latency for list/detail
- TTFB for key read endpoints

**Targets**
- Defined per environment (baseline first, then tighten)

**Signals**
- API metrics, request tracing (optional), DB slow query indicators

---

### Availability (Read Path)
**SLIs**
- Uptime for read endpoints
- Error rate (5xx + timeouts)

**Targets**
- Defined monthly SLO

**Signals**
- API metrics, ingress metrics, health checks

---

### Reliability & Resilience
**SLIs**
- Background handler success/failure rate
- Queue backlog/lag trend
- DLQ rate / poison message rate (if used)

**Targets**
- Backlog remains controlled; failures observable

**Signals**
- Worker metrics, queue metrics, structured logs

---

### Replication freshness & lag (V1)
This section measures whether replicated/derived state is **caught up** enough
to meet user expectations and safety rules.

**SLIs**
- **Outbox pending count** (messages not yet published to broker)
- **Outbox oldest pending age** (seconds since the oldest pending message occurred)
- **Broker queue depth** per consumer (ready/unacked)
- **Consumer processing latency** (P95/P99) per handler
- **Consumer failure rate** and retry rate (transient vs permanent)
- **DLQ age** (oldest message in DLQ), if DLQ is enabled
- **Duplicate-prevention indicators**:
  - dedupe hits / idempotency rejects (audit + notifications + interaction)
- **Derived-store fallback rate** (correctness-first behavior):
  - SEO slug resolve DB fallback rate (%)
  - "truth fallback" rate for public reads when projections/caches are stale
- **Projection freshness (V2+)**:
  - checkpoint age / last processed event time for each projection

**Targets (policy-level)**
- Replication backlog spikes are **visible** and **self-recovering** (via retries/backoff).
- Correctness is preserved under lag:
  - unpublished content is never re-exposed
  - drafts are never leaked through routing
  - admin/self reads remain consistent (primary-only or read-your-writes)
- Duplicate side effects are prevented (emails/audit entries are not duplicated from retries).

**Signals**
- Outbox tables/collections metrics
- Broker metrics (queue depth, unacked)
- Worker handler metrics (latency, error types)
- Structured logs with `correlationId` across publish/register/govern flows
- Selected endpoint counters for fallback behavior (SEO/public query)

---

### Batch / rebuild / reconciliation health (V1)
This section measures whether the **batch lane** is healthy, bounded, and operationally trustworthy.

**SLIs**
- **Run success/failure count** per important workflow
- **Run duration** per workflow
- **Records selected / scanned / processed / skipped / repaired**
- **Current stage / last completed stage** for multi-stage workflows where relevant
- **Candidate output generation success/failure**
- **Publication/cutover success/failure** for correctness-sensitive derived outputs
- **Freshness age of active derived output**
- **Replay / rebuild backlog age**
- **Rerun count** for workflows with replay or recovery semantics
- **Reconciliation mismatch count**
- **Repair applied count**
- **Checkpoint age**, when checkpoints exist
- **Duplicate-run detection count**, where overlapping execution is possible
- **Stale-owner rejection count**, where singleton or ownership-sensitive work exists

**Targets (policy-level)**
- Important workflows complete successfully within their intended operational window.
- Active derived outputs have freshness appropriate to their purpose.
- Failed runs are visible and recoverable.
- Partial candidate output is not mistaken for completed active output.
- Replay/rebuild/reconciliation workflows remain rerun-safe and operationally understandable.
- Ownership-sensitive workflows prefer safe non-progress over unsafe double-apply.

**Signals**
- Worker/job metrics
- Scheduler/platform job metrics
- Structured workflow logs with workflow/run identifiers
- Candidate/publication state counters
- Reconciliation summaries
- Checkpoint or freshness markers where used

---

### Recoverability
**SLIs**
- Backup success rate
- Restore drill success rate
- RTO/RPO adherence
- Derived-state rebuild success rate for important workflows
- Time to recover a lagging or failed derived output through replay/rebuild

**Targets**
- RTO/RPO defined and tested periodically
- Important derived outputs are rebuildable within an acceptable operational window

**Signals**
- Backup job logs
- Restore drill reports
- Replay/rebuild run reports
- Workflow completion and freshness signals

---

### Maintainability & Evolvability
**SLIs**
- Change coupling (modules touched per feature)
- Boundary violations count (dependency rule breaks)
- Workflow count requiring manual intervention
- Number of important workflows with undocumented input/output boundaries

**Targets**
- Violations rare and visible; coupling controlled
- Important workflows remain explainable and operable
- Manual intervention remains exception, not the normal control plane

**Signals**
- PR review metrics
- Architecture tests/linters (future)
- Runbooks / workflow docs coverage reviews

---

### Observability
**SLIs**
- Time to detect/diagnose (TTD/TTDiag)
- Coverage of correlation IDs in critical flows
- Coverage of workflow/run identifiers in important batch/rebuild jobs
- Ability to distinguish:
  - truth-path failure
  - async consumer lag
  - batch/rebuild freshness lag
  - publication/cutover failure

**Targets**
- Trending improvement over time
- Important flows and workflows are diagnosable without guesswork

**Signals**
- Incident records
- Structured logs
- Log correlation
- Tracing if available
- Workflow stage/status logs

---

## 6.3 Runtime-lane measurement focus (V1)

### Lane A — Synchronous request/response
Focus on:
- latency
- error rate
- truth correctness under partial failure
- read-your-writes for truth-sensitive flows

### Lane B — Async side effects
Focus on:
- backlog/lag
- handler success/failure
- dedupe/idempotency behavior
- queue health
- DLQ behavior

### Lane C — Batch / rebuild / reconciliation
Focus on:
- run success/failure
- run duration
- freshness of active derived outputs
- mismatch / repair rates
- rerun/replay posture
- candidate publication/cutover safety
- ownership-sensitive execution anomalies (if any)

---

## 6.4 Module-level measurement focus (V1)
- **Reading Experience:** P95/P99 latency, error rate, degradation success, SEO fallback rate, aggregate freshness for reading-derived enrichments
- **Interaction:** backlog/throughput, idempotency anomalies, aggregation lag, rebuild/replay success for summaries/counters
- **Identity:** login/refresh success rate, abuse spikes, suspicious patterns, verification/reset email lag, cleanup/replay health for delivery artifacts
- **Authorization/Audit:** policy coverage, audit completeness, ingestion failures, dedupe hits, archival/summarization workflow health
- **Notifications:** send success/failure, duplicates prevented, backlog/lag, DLQ age, replay/cleanup workflow health
- **SEO:** slug resolve latency/error, truth fallback rate, derived serving artifact freshness, rebuild/reconciliation mismatch counts
- **Content:** publish/unpublish success, outbox backlog, visibility correctness signals, rebuild/repair workflow health where derived artifacts depend on content truth

---

## 6.5 What is intentionally out of scope in V1
- Full dashboards and alert rules
- Anomaly detection baselines
- CI quality gates (CC/coverage thresholds) as hard blockers
- A dedicated workflow observability platform
- Automatic SLO enforcement for every batch workflow

These will be added after V1 implementation stabilizes.