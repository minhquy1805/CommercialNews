# Release & Rollout Strategies — CommercialNews (V1)

This document applies Chapter 5 (*Effective Software Releases*) from *Mastering API Architecture* to CommercialNews.
It defines how we **deploy** and **release** API changes safely, using traffic management and observability gates.

Related:
- Quality requirements: `../architecture/arc42/05-quality-requirements.md`
- Measurement guide: `../architecture/arc42/06-measurement-guide.md`
- Governance/fitness functions: `../architecture/arc42/07-architecture-governance.md`
- Components: `../architecture/arc42/08-components.md`
- Edge traffic management: `05-edge-gateway-and-traffic-management.md`
- Versioning/compatibility: `04-versioning-and-compatibility.md`

---

## 1) Core idea (non-negotiable)

### 1.1 Deploy ≠ Release
- **Deploy**: ship a new version to the runtime environment.
- **Release**: expose real user traffic to that version in a controlled way.

**Rule:** CommercialNews must treat deployment and release as separate concerns.

### 1.2 Why this matters for CommercialNews
CommercialNews has:
- a read-heavy bursty public workload
- governance-sensitive admin workflows
- async pipelines (audit/notifications/interaction aggregation)

A safe release process is required to:
- protect read-path performance/availability
- detect regressions early
- roll back quickly without data corruption or consumer breakage

---

## 2) Release objectives (V1)

A rollout is “successful” when:
- public read endpoints remain within latency/error budgets (P95/P99)
- no spike in 5xx/timeouts
- no spike in security signals (401/403)
- async pipelines remain healthy (no runaway backlog/lag)
- no breaking contract changes are introduced

---

## 3) Release types and recommended strategies

### 3.1 Patch/Minor releases (backward compatible)
**Default strategy:** Canary / Progressive delivery

Use when:
- additive changes (new optional fields, new endpoints)
- bug fixes
- performance improvements
- internal refactors behind stable contracts

### 3.2 Major releases (breaking changes)
**Default strategy:** Parallel run (v1/v2) + routed traffic

Use when:
- contract changes are breaking (see versioning rules)
- consumers need time to migrate

Routing:
- `/api/v1/...` stays stable
- `/api/v2/...` introduced with migration guide
- gradual consumer migration + sunset plan

### 3.3 Coupled changes (multiple modules must move together)
**Default strategy:** Blue–Green

Use when:
- a change spans components tightly (e.g., shared schema change requiring lockstep)
- you want fast rollback with minimal decision points

**Caution:** blue–green can hide data migration risks; pair with migration strategy.

---

## 4) Rollout strategies (how we release)

### 4.1 Canary (recommended default for API component)
- Route a small percentage of traffic to the new version.
- Increase gradually through steps.
- Pause and evaluate at each step.

**Best for:** read-path regressions, latency changes, error spikes.

### 4.2 Blue–Green (fast rollback)
- Run two environments (blue=current, green=new).
- Flip traffic when ready.
- Roll back by flipping back.

**Best for:** tightly coupled changes requiring fast cutover.

### 4.3 Mirroring (dark launch)
- Duplicate traffic to the new version without impacting users.
- Compare responses, latency, and resource usage.

**Rules:**
- mirrored traffic must not produce side effects (or must be isolated)
- careful with costs and privacy

---

## 5) Feature flags (separating release from deploy)

Feature flags can separate code deployment from feature exposure.

### 5.1 Where feature flags help
- gradual enablement of new logic while keeping old behavior available
- migration steps in strangler patterns
- risk reduction for high-impact workflows

### 5.2 Feature flag discipline (avoid becoming a hazard)
- flags must have:
  - clear owner
  - expiration/cleanup date
  - unique naming
  - documented default state
- flags must not become a SPOF:
  - avoid external flag dependencies that can break core flows
  - ensure safe defaults if flag system fails

---

## 6) Observability gates (release is driven by signals)

### 6.1 Read-path gates (must monitor)
For:
- article list
- article detail by slug

Signals:
- P95/P99 latency
- error rate (5xx + timeouts)
- saturation (CPU/memory)
- edge routing failures (502/503)

### 6.2 Security gates (must monitor)
Signals:
- spikes in 401/403
- spikes in rate-limit triggers on sensitive endpoints
- spikes in login failures or suspicious reset/verification requests

### 6.3 Async pipeline gates (must monitor)
For Worker:
- success/failure rate of handlers
- backlog/lag trend in queues
- retry volume trend
- DLQ growth

**Rule:** a healthy API release must not create a runaway backlog in non-critical pipelines.

---

## 7) Application-level pitfalls (common causes of failed rollouts)

### 7.1 Caching effects
Caching can hide canary regressions:
- the canary may not be seeing real traffic patterns
- caches may serve stale responses masking errors

Guidelines:
- define caching policy per endpoint
- during canary, monitor cache hit ratio and bypass patterns if needed

### 7.2 Header propagation
Missing headers can break:
- tracing/correlation
- auth context
- routing logic

Guideline:
- define allowlist of headers that must propagate end-to-end
- test in component/integration tests

### 7.3 Logging quality
Two log intents must be separated:
- **journal logs** for operations (structured, stable fields)
- **diagnostic logs** for debugging (controlled verbosity)

**Rule:** never log tokens/PII in either.

---

## 8) Rollback and recovery rules

### 8.1 Rollback triggers (examples)
- P95/P99 latency breaches sustained
- 5xx/timeouts spike above threshold
- security signal anomalies (401/403 spikes)
- worker backlog/lag grows rapidly
- unexpected contract issues (consumer errors)

### 8.2 Rollback playbook (policy-level)
- pause rollout
- roll back traffic to stable version
- keep investigation artifacts:
  - correlation IDs
  - release version and config snapshot
  - key metrics windows
- document incident and update guardrails/tests if needed

---

## 9) “Golden path” service template (recommended)

To make releases repeatable, CommercialNews should have a baseline template:
- health endpoints and readiness/liveness probes
- structured logs with correlation IDs
- metrics for critical endpoints and handlers
- standard error envelope
- standard timeouts and retry guidance

This reduces drift and makes new modules production-ready faster.

---

## 10) V1 minimum release policy (starter set)

In V1, enforce at least:
- progressive rollout for API component changes (canary or staged)
- rollback readiness (known-good version always available)
- observability gates for read path + auth + async pipelines
- OpenAPI breaking change detection in CI
- safe logging checks (no token/PII leakage)