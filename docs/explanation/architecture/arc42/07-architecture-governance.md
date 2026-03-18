# 07 — Architecture Governance (Fitness Functions)

This document defines how CommercialNews will **protect architectural priorities** over time.
It focuses on characteristics that are often **important but not urgent** (e.g., modularity, security, maintainability, rerun safety of derived workflows),
by introducing **automated guardrails** (“fitness functions”) rather than relying on reminders and reviews alone.

> Related:
> - `03-building-blocks-modularity.md` (module boundaries & dependency rules)
> - `04-runtime-view-v1.md` (critical workflows and failure modes)
> - `05-quality-requirements.md` (system-level characteristics)
> - `06-measurement-guide.md` (how we measure characteristics)
> - `13-transactions-and-consistency-v1.md` (truth boundaries, async side effects, read-your-writes)
> - `16-batch-processing-and-derived-data-v1.md` (batch lane, derived state, rebuild/reconciliation posture)
> - `17-dataflow-and-batch-workflows-v1.md` (workflow stages, materialization, publication/cutover, rerun semantics)

---

## 7.1 Governance goals

- Prevent architectural drift (boundary violations, cyclic dependencies, scattered security checks).
- Keep delivery fast while enforcing clear guardrails automatically.
- Provide objective signals for architectural integrity (not subjective “code feels clean”).
- Prevent **derived-state drift**:
  - batch outputs quietly becoming hidden truth
  - partial outputs being exposed as complete
  - important workflows having unclear input/output boundaries
- Keep replay, rebuild, and reconciliation behavior **predictable and safe** over time.

---

## 7.2 Governance scope (what we enforce)

### A) Modularity & boundaries
- Modules follow the dependency rules defined in `03-building-blocks-modularity.md`.
- No “shortcut” dependencies that bypass module ownership.
- The read path must remain protected from non-critical workflows.
- Shared DB must not be treated as permission to widen module ownership or truth boundaries.

### B) Security & governance
- Admin endpoints must enforce authorization policies (systematic coverage).
- Sensitive identity flows must include abuse controls (rate limits).
- Logs and audit records must not leak secrets/tokens/PII (redaction rules).

### C) Async workflow integrity
- Event consumers must be idempotent and retry-safe (at-least-once delivery tolerant).
- Notification and audit must not block core workflows.
- Outbox remains the standard async boundary for side effects that must not be lost.

### D) Operational integrity (read path first)
- Public reading latency, error rate, and degradation behavior must remain within policy targets.
- Non-critical subsystems (notifications, audit ingestion, interaction aggregation) may degrade without taking down reading.
- Truth-backed fallback must remain available when caches/projections are stale or missing.

### E) Batch / rebuild / reconciliation integrity
- Important batch workflows must have bounded input definitions.
- Important workflows must have explainable stage boundaries.
- Reusable derived outputs must be distinct from internal temporary workflow state.
- Partial candidate output must not be exposed as completed active state.
- Derived state must not silently become hidden truth.
- Rerun safety is required for important rebuild/reconciliation/repair workflows.
- Ownership-sensitive or singleton batch work must follow the system-wide coordination policy.

---

## 7.3 Fitness functions (automation plan)

A **fitness function** is any automated mechanism that objectively assesses the integrity of an architecture characteristic.
In CommercialNews, fitness functions are applied across three stages: build-time, deploy-time, and runtime.

---

### 7.3.1 Build-time fitness functions (CI)

**Goal:** fail fast when architectural integrity is violated.

#### 1) Boundary rules (no forbidden dependencies)
- Enforce that modules respect boundary rules (no direct DB/schema bypass, no forbidden references).
- Enforce that the core Content workflow does not directly depend on Notifications or Audit persistence.
- Enforce that batch/rebuild code does not quietly mutate another module’s truth outside approved ownership boundaries.

**Typical implementation options (later)**
- Architecture tests in the test suite (e.g., .NET architecture rule tests)
- Static analysis rules in CI

#### 2) No cyclic dependencies
- Fail the build when cyclic dependencies appear between modules/namespaces.

#### 3) Layering rules (Clean Architecture guardrails)
- Presentation/API does not directly depend on Infrastructure.
- Domain does not depend on Application/Infrastructure.
- Application depends only on abstractions, not concrete infra.

#### 4) Code quality “hotspot” checks (minimal)
- Prevent extreme complexity growth in critical areas (auth flows, publish/unpublish, permission checks, rebuild/cutover workflows).
- Encourage refactoring before complexity dominates.

> Note: thresholds are project-dependent. Start with reporting/warnings first, then enforce hard gates.

#### 5) Batch workflow declaration checks (recommended)
For important rebuild/reconciliation/aggregation workflows, require explicit declaration or documentation of:
- bounded input
- output type
- whether output is reusable or internal-only
- publication/cutover semantics if output becomes active
- rerun expectations

This can start as PR checklist or doc review and later become stronger CI validation.

#### 6) Hidden-truth checks (recommended)
Prevent designs where:
- cache/projection/temporary output is treated as authoritative for truth-sensitive decisions
- public visibility or security state depends solely on derived state
- a batch artifact is used as truth without explicit architecture approval

Start as review/architecture-test rules; harden later where feasible.

---

### 7.3.2 Deploy-time fitness functions (CD)

**Goal:** prevent unsafe releases and configuration drift.

#### 1) Baseline release safety
- Reject deployments that violate baseline release policies (e.g., unsafe image/config patterns).
- Ensure configuration changes are controlled and reviewable.

#### 2) Security posture checks (baseline)
- Block releases with critical security misconfigurations (policy-driven).
- Ensure secrets are not hard-coded and are handled via approved mechanisms.

#### 3) Derived-output publication safety checks (recommended)
For workflows that publish/cut over correctness-sensitive derived outputs:
- deployment/release automation should not silently activate half-built or unverifiable candidate data
- publication steps must remain explicit and reviewable
- rollback path should be clear where versioned outputs are used

> Deploy-time checks are intentionally minimal in V1 and can be expanded in V2+.

---

### 7.3.3 Runtime fitness functions (SRE monitors)

**Goal:** validate architecture characteristics in production through measurable signals.

#### 1) Read path SLO monitors
- Track P95/P99 latency and error rate for:
  - Article listing
  - Article detail by slug

#### 2) Async workflow health
- Track notification and audit ingestion:
  - success/failure rate
  - backlog/lag trends
  - retry volume

#### 3) Security anomaly signals
- Track spikes in:
  - login failures
  - rate-limit triggers on sensitive endpoints
  - suspicious repeated reset/verification requests

#### 4) Batch / rebuild workflow health
Track important workflows for:
- run success/failure
- run duration
- freshness age of active derived outputs
- replay/rebuild backlog age
- reconciliation mismatch counts
- candidate publication/cutover failures
- repeated reruns or overlapping execution anomalies

#### 5) Stale-owner / duplicate-run safety signals
For any ownership-sensitive workflow introduced in V1 or later, track:
- stale-owner rejection count
- duplicate-run detection count
- safe no-owner / degraded intervals
- repair/recovery after ambiguous ownership

> Runtime fitness functions should start after baseline data is collected; avoid overly strict alerts before calibration.

---

## 7.4 Governance operating model (how we use guardrails)

### A) “Steering”, not policing
Governance exists to **steer** the system toward long-term health while maintaining delivery speed.
Guardrails should be:
- explainable
- consistent
- proportional to risk

### B) Progressive enforcement
- Start with **report-only** (warnings, dashboards, PR comments).
- Graduate to **hard gates** (CI/CD failures) once thresholds and rules are accepted.

### C) ADR-driven changes
If a team needs to break a guardrail for a justified reason (e.g., performance, migration, staged rollout),
the change must be captured as an **ADR** explaining the trade-off and mitigation plan.

### D) Important workflows require architecture-level visibility
If a workflow:
- produces a reusable derived dataset
- performs publication/cutover
- does reconciliation/repair of important state
- relies on singleton/ownership-sensitive execution

then it should not remain an undocumented background script.
It must have architecture-visible rules and observability.

---

## 7.5 V1 minimal guardrails (recommended starter set)

These guardrails provide the highest value with the lowest friction:

1) **No cyclic dependencies** between modules  
2) **No boundary violations** (respect ownership and dependency rules)  
3) **100% policy coverage** for admin endpoints (authorization required)  
4) **Non-blocking read path** (no sync dependency on notifications/audit/interaction aggregation)  
5) **Safe logging** (no secrets/tokens/PII in logs and audit payloads)  
6) **Bounded-input rule for important batch workflows**  
7) **No partial publication of correctness-sensitive derived outputs**  
8) **No hidden truth in cache/projection/batch artifacts**  
9) **Rerun safety for important rebuild/reconciliation workflows**  
10) **No naive singleton batch ownership** (must follow system coordination policy if exclusivity matters)

---

## 7.6 What is intentionally out of scope (V1)

- Full anomaly detection and automated baselining
- Advanced chaos experiments in production
- Extensive quality gates that slow iteration (beyond the minimal guardrails)
- Mandatory heavyweight workflow orchestration tooling
- Full static verification of every batch workflow contract

These can be introduced after V1 stabilizes and baseline measurements are available.

---