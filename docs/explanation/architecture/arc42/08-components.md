# 08 — Components (V1)

This section describes the **physical deployment units** (components) of CommercialNews in V1
and how logical **modules** map into those components.

> Related:
> - `03-building-blocks-modularity.md` (modules and boundaries)
> - `04-runtime-view-v1.md` (runtime scenarios and runtime lanes)
> - `07-architecture-governance.md` (guardrails and fitness functions)
> - `11-replication-v1.md` (replication rules)
> - `16-batch-processing-and-derived-data-v1.md` (batch lane, derived state, rebuild/reconciliation posture)
> - `17-dataflow-and-batch-workflows-v1.md` (workflow stages, materialization, publication/cutover)

---

## 8.1 Module vs Component

- **Module**: a logical boundary in the codebase (cohesion, low coupling, ownership).
- **Component**: a physical deployment artifact (build/version/deploy/scan/rollback/observe).

A system can have many modules but only a few components (modular monolith), or map modules to separate components (microservices).

CommercialNews V1 intentionally keeps:
- **module boundaries rich**
- **component count small**

This allows:
- lower operational overhead in V1
- clear ownership and evolution paths
- selective scaling or component extraction in V2+ when justified

---

## 8.2 V1 component inventory

### Component A — Public API
- Hosts public read endpoints (listing/detail/search) and admin endpoints for content management.
- Primary responsibility: **read path performance and availability** and **truth-bound request handling**.
- Replication responsibility (V1):
  - Executes **truth writes** (single-leader primary).
  - Writes **Outbox** records atomically with domain changes (replication log for async workflows).
  - Propagates `correlationId` on sync requests for end-to-end tracing.
- Batch/dataflow posture (V1):
  - does **not** own long-running rebuild/reconciliation as a normal runtime responsibility
  - may trigger or expose admin/operator commands that initiate bounded background workflows
  - must not wait for batch/rebuild completion as a user-facing success condition

### Component B — Background Worker
- Executes asynchronous workflows and bounded batch/rebuild/reconciliation work.
- Primary responsibility: **reliable async and background processing** (retry-safe, observable, rerun-safe where applicable).
- Replication responsibility (V1):
  - Publishes Outbox messages to the broker (retry + backoff).
  - Consumes events **at-least-once**; handlers must be **idempotent** and (when needed) **ordering-aware**.
  - Drives repair mechanisms (retries, DLQ handling, replay, reconciliation jobs where applicable).
- Batch/dataflow responsibility (V1):
  - runs bounded aggregation jobs
  - runs rebuild/reconciliation workflows
  - runs replay/repair workflows for derived state
  - runs archival/cleanup/retention jobs by policy
  - performs candidate generation and publication/cutover of derived outputs where applicable

### External dependencies (runtime infrastructure)
- Database (shared DB with owned schema boundaries; primary is the source of truth)
- Message broker (event delivery for async workflows)
- Cache (Redis) for hot-path acceleration (e.g., SEO slug routing) (optional but recommended)
- Object storage/CDN (optional, future)
- Scheduler / platform trigger (implicit operational dependency for recurring batch/rebuild jobs; implementation may be app-owned or platform-owned)

---

## 8.3 Runtime-lane to component mapping (V1)

CommercialNews V1 uses three runtime lanes:

### Lane A — Synchronous request/response
Primary component:
- **Public API**

Responsibilities:
- truth writes
- truth-sensitive reads
- authorization checks
- low-latency public/admin request handling

### Lane B — Asynchronous event-driven side effects
Primary component:
- **Background Worker**

Responsibilities:
- outbox publishing
- broker consumption
- idempotent side-effect handling
- delayed but non-blocking downstream processing

### Lane C — Batch / rebuild / reconciliation
Primary component:
- **Background Worker**

Responsibilities:
- bounded aggregation
- rebuild / replay / repair
- archival / cleanup
- candidate generation and cutover of important derived outputs
- workflow-level observability and recovery

**Rule:** Lane C must not silently redefine Lane A truth ownership or Lane B delivery semantics.

---

## 8.4 Module → Component mapping (V1)

| Module | Runs in Public API | Runs in Background Worker | Notes |
|---|---:|---:|---|
| Content Management | ✅ | (limited/optional hooks) | Core write workflows; writes outbox for publish/unpublish; truth remains API-owned |
| SEO & Discoverability | ✅ | ✅ | slug routing in API; async SEO updates, rebuild, and reconciliation may run in worker |
| Media | ✅ | (optional later) | attach/reorder/primary in API; heavy or maintenance workflows may move to worker later |
| Reading Experience (Public) | ✅ | (derived support) | read path facade in API; aggregates/trending/rebuild support may run in worker |
| Interaction | ✅ (sync entry) | ✅ | non-blocking tracking in API; aggregation/replay/rebuild in worker |
| Identity & Access | ✅ | (operational support only) | auth endpoints in API; cleanup/replay/reporting jobs may run in worker if introduced |
| Authorization | ✅ | (operational support only) | policy enforcement in API; audit/reporting/reconciliation support may run in worker |
| Audit Trail |  | ✅ | event-driven ingestion; idempotent append-only logging; archival/summarization in worker |
| Notifications |  | ✅ | email workflows; dedupe/idempotency required; replay/cleanup/summaries in worker |

---

## 8.5 Component responsibilities by processing style

### 8.5.1 Public API responsibilities
The Public API is the component that:
- commits truth
- enforces immediate request correctness
- applies read-your-writes where required
- performs truth-safe fallback when caches or projections lag

It must **not** become the place where:
- long-running aggregation is performed inline
- rebuild/reconciliation logic blocks core request flows
- active user/admin success depends on batch completion

### 8.5.2 Background Worker responsibilities
The Background Worker is the component that:
- absorbs retryable side effects
- processes outbox-driven work
- owns bounded rebuild/reconciliation/replay workflows
- applies cleanup and retention jobs
- generates and publishes candidate derived outputs when relevant

It must preserve:
- idempotent consumer behavior
- rerun-safe workflow behavior
- safe publication discipline
- safe ownership/coordination semantics where exclusivity matters

---

## 8.6 Replication and batch boundary notes (V1)

- **Sync truth boundary**: Public API commits truth + outbox in one transaction.
- **Async boundary**: Background Worker publishes/consumes events and updates derived state.
- **Batch boundary**: Background Worker selects bounded input and produces derived outputs, replay effects, or reconciliation results outside the originating request.
- **Derived stores** (Redis/projections/logs/aggregates) are allowed to lag, but must be observable and recoverable.
- **Correctness guardrails**:
  - Unpublished/draft content must never be exposed by any component.
  - Admin/self flows requiring read-your-writes must bypass stale replicas/caches.
  - Derived outputs must not silently become hidden truth.
  - Partial candidate output must not be treated as completed active state.

---

## 8.7 Operational posture for batch/rebuild work (V1)

CommercialNews V1 does not require a separate dedicated “batch platform” component.
Instead, bounded batch/rebuild/reconciliation work is hosted primarily in the **Background Worker**.

This posture is intentional because V1 prioritizes:
- small-team feasibility
- low operational overhead
- clear ownership
- replay/rebuild safety over infrastructure sprawl

Where workflows become more numerous or more operationally significant, the architecture may later introduce:
- dedicated job runners
- projection-focused workers
- stronger scheduling/orchestration components
- projection/read-model serving components

Those changes require explicit evolution decisions, not silent drift.

---

## 8.8 Ownership-sensitive work and singleton posture

CommercialNews V1 does **not** assume that batch/rebuild work is automatically safe just because it runs in one worker process.

If a workflow truly requires:
- one current publisher
- one active rebuild owner
- one current maintenance actor
- one current repair authority

then it must follow the system-wide singleton/ownership policy:
- ownership must be explicit
- stale owners must be rejectable
- naive in-memory leadership is forbidden
- safe non-progress is preferred over unsafe dual apply

Most V1 background work should instead prefer:
- idempotent repeated execution
- bounded replay
- partitioned work
- DB-enforced winners where feasible

---

## 8.9 Evolution notes (V2+)

- If interaction or read traffic requires independent scaling, consider splitting into additional components:
  - Read Model / Projection component
  - Interaction service component
  - Derived-data / aggregation worker component
- Apply connascence analysis before splitting (avoid distributed monolith).
- If projections become first-class read sources, introduce explicit:
  - projection checkpoints
  - freshness SLIs
  - publication/cutover contracts
  - workflow ownership and recovery rules
- If workflow complexity grows substantially, consider introducing more explicit orchestration support, but only with a clear ADR-backed justification.