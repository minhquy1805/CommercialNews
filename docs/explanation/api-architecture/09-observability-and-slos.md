# Observability & SLOs — CommercialNews (V1)

This document defines the **observability baseline** for CommercialNews APIs and async workflows.
It turns the arc42 quality requirements and measurement guide into **operational rules** that teams implement and verify.

CommercialNews is read-heavy and bursty. Observability is mandatory to:
- detect regressions quickly (especially on read path)
- validate non-blocking side effects (audit/notifications/interaction)
- diagnose issues with correlation IDs across sync + async boundaries
- support governance investigations without leaking sensitive data
- quantify "eventual consistency" via **replication freshness/lag**

Related:
- Quality requirements: `../architecture/arc42/05-quality-requirements.md`
- Measurement guide: `../architecture/arc42/06-measurement-guide.md`
- Runtime scenarios: `../architecture/arc42/04-runtime-view-v1.md`
- Replication rules: `../architecture/arc42/11-replication-v1.md`
- Governance: `../architecture/arc42/07-architecture-governance.md`
- Release/rollouts: `06-release-rollout-strategies.md`
- Threat modeling: `07-security-threat-modeling.md`

---

## 1) Observability goals (V1)

1) **Protect the read path**
- track P95/P99 latency and error rates for list/detail
- detect degradation when subsystems fail (interaction/audit/notifications)
- measure correctness-first fallbacks (e.g., SEO DB fallback)

2) **Make async pipelines operable**
- track worker success/failure, retry volume, backlog/lag, DLQ growth
- detect duplicate/idempotency anomalies (email duplicates, duplicate audit writes)
- measure outbox-to-consumer freshness ("how behind are we?")

3) **Support security and governance**
- track auth anomalies (401/403 spikes, login failures, rate-limit triggers)
- ensure audit traceability for governance actions (publish/unpublish, role changes)

4) **Enable safe releases**
- use observability gates to promote/pause/rollback canaries

---

## 2) Required telemetry types (V1 baseline)

### 2.1 Logs (structured)
- Structured logs with stable fields (no free-text-only logging).
- Two intents:
  - **Journal logs**: operational, stable schema
  - **Diagnostic logs**: debug-level, controlled and safe

**Non-negotiable:** no tokens, secrets, or sensitive PII in logs.

### 2.2 Metrics
- Percentiles (P95/P99) over averages for latency.
- Error rates for availability and release gates.
- Async backlog/lag metrics to detect drift and failure accumulation.
- Replication freshness metrics to quantify eventual consistency.

### 2.3 Traces (optional in V1, recommended path)
- Distributed tracing becomes important once multiple services exist.
- In V1 (API + Worker), correlation IDs provide most of the value.

---

## 3) Correlation rules (connect sync and async)

### 3.1 Correlation ID
- Client MAY send: `X-Correlation-Id`
- API SHOULD propagate correlation ID through:
  - API logs
  - emitted domain events (event envelope `CorrelationId`)
  - outbox records (recommended)
  - worker logs

### 3.2 Trace ID
- API errors MUST include `traceId` in response body (standard error envelope).
- `traceId` is used for support and debugging.

### 3.3 Required fields (minimum)
For API request logs:
- timestamp
- correlationId (if present)
- traceId
- method/path
- status code
- duration (ms)
- module (or route group)
- actorUserId (if authenticated; avoid PII)
- client IP (optional; privacy policy applies)

For worker handler logs:
- timestamp
- correlationId
- messageId / eventId
- eventType + version
- handler name
- outcome (success/fail)
- duration (ms)
- retry attempt (if known)
- DLQ flag (if applicable)

For outbox publish logs (Worker publishing outbox -> broker):
- timestamp
- correlationId
- messageId
- eventType
- publish outcome
- duration (ms)
- attempt count (if applicable)

---

## 4) SLIs and what to measure (V1)

This section translates arc42 measurement into API-operational SLIs.

### 4.1 Read path (top priority)
Endpoints:
- Article listing
- Article detail by slug

SLIs:
- P95/P99 latency
- error rate (5xx + timeouts)
- degradation success (reading still works when non-critical subsystems fail)
- SEO slug resolve fallback rate (cache miss/stale -> DB fallback), if applicable

### 4.2 Identity flows (security-critical)
Endpoints:
- register/login/refresh/verify/reset

SLIs:
- success/failure rate
- rate-limit trigger rate
- suspicious spikes (failed logins, reset requests)
- email workflow freshness (verification/reset emails not stuck in backlog)

### 4.3 Governance actions (admin)
Actions:
- publish/unpublish
- role/permission changes
- delete/restore actions

SLIs:
- policy deny rate (unexpected spikes indicate drift)
- action success/failure
- audit ingestion lag/backlog (eventual consistency is allowed, but must be observable)

### 4.4 Async pipelines (Worker)
Handlers:
- notifications
- audit ingestion
- interaction aggregation

SLIs:
- handler success/failure rate
- retry volume
- queue backlog/lag trend
- DLQ growth
- duplicate/idempotency anomaly indicators (where measurable)
- processing latency (P95/P99)

### 4.5 Replication freshness & lag (system-wide)
CommercialNews uses Outbox → Broker → Consumers. This section measures **how far behind** replicated/derived state is.

SLIs:
- outbox pending count
- outbox oldest pending age (seconds)
- broker queue depth per consumer (ready/unacked)
- consumer processing latency (P95/P99)
- consumer failure rate + retry rate
- DLQ age (oldest DLQ message), if enabled
- dedupe hits / idempotency rejects (audit + notifications + interaction)
- derived-store fallback rate:
  - SEO slug resolve DB fallback rate (%)
  - truth fallback rate for public reads when projections/caches are stale (if applicable)

---

## 5) SLO policy (V1 approach)

V1 uses **policy-level SLOs**:
- establish baseline first
- tighten over time once real traffic exists

### 5.1 Read path SLOs (must exist)
- monthly uptime target for read endpoints
- latency targets as percentiles (P95/P99)
- error budget framing (policy-level)

### 5.2 Async workflow SLOs (must exist)
- backlog/lag remains controlled
- failures are observable
- retries do not cause duplicate side effects

### 5.3 Replication freshness SLOs (must exist)
Policy-level targets:
- outbox + queues do not accumulate indefinitely (oldest age stays bounded under normal load)
- spikes are visible and recover automatically (retries/backoff)
- correctness-first fallbacks work under lag:
  - no draft/unpublished leak through routing/read paths
  - admin/self reads remain consistent (primary-only / read-your-writes)

### 5.4 Security posture targets (must exist)
- 100% admin endpoint policy coverage
- zero token/PII leakage in logs/audit
- rate-limited endpoints show controlled trigger patterns (no unbounded spikes)

---

## 6) Dashboards (policy-level layout)

V1 recommended dashboards (names are illustrative):

1) **Read Path Dashboard**
- request rate
- P95/P99 latency
- error rate (5xx/timeouts)
- top slow endpoints
- saturation (CPU/mem)
- SEO fallback rate (if applicable)

2) **Identity Security Dashboard**
- login failures
- rate-limit triggers
- refresh failures
- suspicious reset/resend patterns
- 401/403 trends
- verification/reset email backlog indicators

3) **Async Workflows Dashboard**
- handler success/failure
- retries
- backlog/lag
- DLQ size
- processing latency
- dedupe hits / idempotency rejects

4) **Governance Dashboard**
- publish/unpublish frequency
- policy denies
- audit ingestion lag/completeness signals
- outbox-to-audit freshness

5) **Replication Freshness Dashboard**
- outbox oldest pending age
- outbox pending count
- queue depth per consumer
- consumer latency + failure rate
- DLQ age
- derived-store fallback rates

---

## 7) Alerting stance (V1)

V1 alerting should be:
- minimal but meaningful
- calibrated after baseline data exists

Recommended initial alerts:
- read path error rate spike
- read path P99 latency sustained breach
- worker backlog/lag sustained growth
- outbox oldest pending age sustained growth
- DLQ non-zero and growing
- 401/403 spike (security signal)
- sudden rate-limit trigger spike on auth endpoints

---

## 8) Safe logging and privacy constraints

### 8.1 Prohibited logging content
Never log:
- passwords
- access tokens
- refresh tokens
- verification/reset tokens
- sensitive PII beyond minimum necessary (per privacy policy)

### 8.2 Audit redaction rule (policy)
Audit entries must be investigation-ready but privacy-aware:
- include actor, action, target identifiers, timestamps, correlationId
- redact sensitive fields (tokens, raw emails if not needed, etc.)

---

## 9) Observability gates for releases (Chapter 5)

Rollouts must be driven by signals:
- read path P95/P99 latency
- 5xx/timeouts
- worker backlog/lag
- outbox oldest pending age
- security signals (401/403 spikes)

See:
- `06-release-rollout-strategies.md`

---

## 10) Module-level responsibility (how to apply)

Each module must document in `modules/{module}/07-observability-slos.md`:
- its critical endpoints and SLIs
- key logs (what fields are required)
- key metrics (success/failure, latency, backlog, freshness)
- failure modes and what “degraded but acceptable” means
- links to runtime scenarios it supports (arc42/04)
- replication signals (outbox/queue lag, dedupe hits, fallback rate) when applicable

---

## 11) Done definition (V1)

Observability is “done” for a module when:
- critical flows emit structured logs with correlation IDs
- metrics exist for success/failure and latency/backlog/freshness as relevant
- dashboards can answer “is it healthy?” within minutes
- safe logging rules are enforced (no token/PII leakage)
- release gates can use the module’s signals