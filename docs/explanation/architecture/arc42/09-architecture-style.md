# 09 — Architecture Style (System and Module Patterns)

This section documents the chosen **architecture style** for CommercialNews (V1), including:
- system-level topology (overall style)
- integration style (sync vs async boundaries)
- stream/derived-state posture (truth-following propagation, projection maintenance, repair/rebuild discipline)
- batch/dataflow posture (bounded workflows, derived-state publication, rebuild/reconciliation)
- component alignment (deployable units)
- module-level patterns (how each capability applies the style)
- alternatives considered and key trade-offs

This decision uses the criteria defined in:
- `docs/reference/architecture-style-decision-criteria.md`

> Related:
> - Modules & dependency rules: `03-building-blocks-modularity.md`
> - Runtime scenarios: `04-runtime-view-v1.md`
> - Quality requirements: `05-quality-requirements.md`
> - Measurement & governance: `06-measurement-guide.md`, `07-architecture-governance.md`
> - Components: `08-components.md`
> - Transactions & consistency: `13-transactions-and-consistency-v1.md`
> - Batch / derived state: `16-batch-processing-and-derived-data-v1.md`
> - Dataflow / workflow structure: `17-dataflow-and-batch-workflows-v1.md`
> - Stream / derived-state policy: `18-stream-processing-and-derived-state-v1.md`
> - Stream runtime shape: `19-stream-processing-runtime-v1.md`

---

## 9.1 Decision summary (V1)

### Chosen system topology style
**Service-Based Architecture (Domain-partitioned Modular Monolith)**

### Chosen integration style
**Selective Event-Driven Architecture for side effects and derived-state convergence**  
(sync for truth-sensitive user flows, async for non-critical and bursty work)

### Chosen stream/derived-state posture
**Truth-first, Outbox-driven, delivery-at-least-once, consumer-idempotent derived-state processing**  
(derived systems follow committed truth; they may lag, be rebuilt, and must not become hidden truth)

### Chosen batch/dataflow posture
**Bounded batch / rebuild / reconciliation workflows for derived state**  
(selective materialization, candidate-before-cutover publication, rerun-safe recovery)

**In short:** CommercialNews is a modular, domain-partitioned system with a small number of deployable components (API + Worker), using:
- synchronous truth handling for user/admin flows
- events to decouple side effects and maintain selected derived outputs
- bounded batch/rebuild workflows to generate, repair, reconcile, and maintain derived state
- safe fallback to truth when derived systems are stale or uncertain

---

## 9.2 Why this style fits CommercialNews

### Domain fit
CommercialNews is:
- **read-heavy** with **bursty spikes** during “hot articles”
- **write-light** but governance-sensitive (publish/unpublish reasons, edit history)
- security-critical in **Identity & Access**
- operationally sensitive in **Interaction** (views/likes/comments) and **Notifications**
- increasingly dependent on **derived state** for counters, enrichments, routing/serving support, and future read-model evolution

This favors a style that:
- keeps the read path fast and available
- avoids unnecessary distributed complexity in V1
- supports asynchronous side effects and burst buffering
- supports stream-style convergence of derived state behind committed truth
- supports rebuild/reconciliation of derived outputs without redefining truth
- keeps operational recovery practical for a small team

### Quality drivers fit (V1 priorities)
From `05-quality-requirements.md`, V1 prioritizes:
- Security
- Read path performance and availability
- Reliability and resilience (especially async workflows)
- Recoverability
- Maintainability and evolvability
- Observability

A service-based modular monolith minimizes operational cost and complexity while still enabling strong modular boundaries.
Selective event-driven integration supports reliability and protects the read path from non-critical work.
Stream-style derived-state processing supports:
- non-blocking truth flows
- asynchronous convergence of projections and side effects
- explicit lag/freshness observability
- replay/rebuild posture when continuous propagation falls behind

Bounded batch/dataflow workflows support:
- derived-state rebuildability
- replay/recovery after lag or failure
- summary generation
- reconciliation and repair
- retention/cleanup operations

---

## 9.3 Topology and components (V1)

CommercialNews V1 uses two deployable components:

1) **Public API Component**
- Hosts public read endpoints and admin endpoints.
- Enforces authorization policies.
- Primary objective: protect **read path performance and availability** and handle truth-bound request flows.

2) **Background Worker Component**
- Publishes outbox records to the broker.
- Consumes async events and executes stream-style derived-state updates and side effects.
- Runs bounded batch/rebuild/reconciliation workflows.
- Primary objective: ensure **reliable async and background processing** (retry-safe, observable, replay/rebuild-capable, rerun-safe where applicable).

External dependencies (runtime infrastructure) include:
- database (shared DB with owned schema boundaries)
- message broker (RabbitMQ; delivery-oriented)
- cache / Redis (derived acceleration only)
- object storage/CDN (optional later)
- scheduler / platform trigger (implicit operational dependency for recurring workflow execution)

See: `08-components.md`

---

## 9.4 Processing-style rules (sync, async, stream, batch)

CommercialNews follows a pragmatic processing rule:

### Synchronous by default for truth-sensitive user flows
Use synchronous request/response when the caller needs an immediate answer to complete the flow, e.g.:
- public read (list/detail)
- slug routing (slug → articleId)
- login/refresh token
- admin actions (publish/unpublish) with immediate result
- governance changes with immediate authoritative effect

### Asynchronous for side effects and derived-state convergence
Use events for non-critical and/or bursty processing, e.g.:
- audit ingestion
- notification emails
- view tracking aggregation
- cache invalidation
- reactive metadata/projection updates
- selective serving/search/read-model maintenance

Async integration requires discipline:
- idempotent consumers
- retry + DLQ policy
- schema/versioning compatibility
- lag/backlog monitoring
- stable message identity
- version-aware apply rules when ordering matters

### Stream-style derived-state processing behind committed truth
CommercialNews V1 treats selected post-commit work as continuous change processing:

- truth commits once in the owner module
- outbox captures async intent atomically
- Worker publishes to RabbitMQ
- consumers update derived systems or produce side effects
- important derived systems must have replay/rebuild/reconciliation posture

This posture explicitly means:
- derived systems are followers, not co-equal write authorities
- RabbitMQ is used for delivery, not as permanent history for all recovery
- fallback to truth is preferred over stale derived confidence when correctness matters

### Batch / rebuild / reconciliation for bounded derived-state work
Use bounded workflows when the work is:
- not part of the originating request
- defined over a bounded input
- intended to produce or repair derived outputs
- better handled as summary, replay, rebuild, archival, or reconciliation work

Examples:
- daily aggregates
- trending input generation
- reconciliation of truth vs derived state
- replay/repair after consumer lag
- cleanup / archival / retention jobs
- future projection rebuilds

Batch/dataflow discipline requires:
- explicit bounded input
- explicit stage boundaries for important workflows
- selective materialization
- candidate-before-cutover publication where output correctness matters
- rerun-safe behavior
- no silent promotion of derived output into truth

Reference:
- `docs/reference/connascence-and-contract-coupling.md`

---

## 9.5 Style consequences for truth vs derived state

CommercialNews V1 explicitly separates:

### Truth
- authoritative, module-owned state
- committed synchronously
- correctness boundary for business success

### Derived state
- may lag
- may be rebuilt
- may be missing temporarily
- may be maintained continuously or regenerated in bounded workflows
- exists to accelerate reads, summarize behavior, support recovery, or operationalize side effects

This style choice implies:
- core flow success is defined by truth commit, not by side-effect completion
- caches/projections/aggregates are not hidden truth
- batch workflows support the health of derived state but do not redefine truth ownership
- stream-style propagation keeps derived systems converging behind truth, but does not override truth
- fallback to truth is preferred over stale derived confidence in correctness-sensitive scenarios
- every important derived system must have a replay, rebuild, or reconciliation posture

---

## 9.6 Module-level patterns (how the style is applied)

This section does not assign a new “architecture style” per module.  
Instead, it describes the **dominant pattern choices** within the overall system style.

### Content Management
- Transactional core with strict lifecycle invariants.
- Emits domain events for side effects and derived-state convergence (audit, notification, SEO updates, future search/read-model updates).
- Prioritizes correctness and auditability over throughput.
- Batch role (support only): reconciliation, repair, and derived-output support must not replace authoritative publication truth.

### SEO & Discoverability
- Policy-driven slug/canonical/meta rules.
- Fast slug lookup for routing; async updates from events where serving artifacts are derived.
- Emphasizes correctness and stability (avoid link breakage).
- Batch role: rebuild/reconciliation of derived serving artifacts is allowed; slug uniqueness remains truth-owned.

### Media
- CRUD + attachment policy (primary media, ordering).
- Lifecycle rules with soft delete/restore policy.
- Optional async processing later for heavy tasks.
- Batch role: cleanup, repair, or maintenance support is allowed; truth ownership remains in Media.

### Reading Experience (Public)
- **Query Facade** in V1 (bounded queries + caching policy).
- Evolution path: **Read Model / projections (CQRS-style)** in V2 if burst traffic demands it.
- Strict publication-state filtering (never expose drafts/unpublished).
- Batch role: aggregates, trending inputs, and summary enrichments are natural derived outputs.
- Runtime rule: if derived serving data is stale or uncertain, fall back to truth-safe reads.

### Interaction (Views / Likes / Comments)
- Non-blocking tracking.
- Event-driven aggregation (eventual consistency for counters).
- Time-sensitive stream semantics may matter for trending or anomaly logic.
- Anti-abuse hooks (rate limits, moderation expansion in V2).
- Batch role: aggregation, replay, rebuild, and summary generation are first-class support patterns.

### Identity & Access
- Security-first flows (verification/reset/session rules).
- Rate limiting for sensitive endpoints.
- Emits events for notifications/audit without blocking core auth flows.
- Batch role: cleanup, replay, and operational summaries are allowed; security truth remains synchronous and authoritative.

### Authorization
- Centralized, policy-based authorization enforcement.
- Avoid scattered ad-hoc checks (prevent drift).
- Permission naming/versioning strategy governs long-term maintainability.
- Batch role: audit/reporting/reconciliation support is allowed; governance truth remains synchronous.
- Derived effective-permission views/caches must never outrank truth-sensitive governance reads.

### Audit Trail
- Event-driven ingestion (append-only mindset).
- Idempotent processing, privacy-aware payload rules.
- High value for incident investigation and governance.
- Batch role: archival, summarization, replay, and retention windows are natural workflow families.

### Notifications
- Event-driven email workflows (retry-safe, idempotent).
- Observability for success/failure/backlog.
- Non-blocking requirement for core flows.
- Batch role: replay, cleanup, delivery summaries, and retention jobs are natural workflow families.

Module characteristic profiles are documented under:
- `arc42/quality/` (e.g., `quality/content-management.md`, `quality/seo.md`, etc.)

---

## 9.7 Alternatives considered (and why they were not chosen as system topology)

### Layered Architecture (as system topology)
- Good technical separation, but tends to smear domain workflows across layers.
- We still use layered concepts **inside components/modules** (Clean Architecture),
  but not as the primary system partitioning strategy.

### Microservices Architecture (as V1 starting point)
- Provides independent deployability and scaling, but significantly increases:
  - operational overhead (service sprawl)
  - contract coupling/versioning complexity
  - distributed data challenges (sagas/outbox/idempotency everywhere)
  - coordination pressure for derived-state workflows and rebuilds
- Not justified for V1 given team size and delivery goals.

### Pipeline Architecture (as primary topology)
- Best suited for transformation pipelines/ETL as the dominant shape of the whole system.
- CommercialNews has pipelines and bounded workflows (notifications, aggregation, rebuild, replay),
  but they are **supporting processing styles**, not the primary topology.

### Log-based streaming platform as baseline architecture
- Strong for high-throughput replay-heavy event platforms.
- But making a dedicated log-retained streaming system the default in V1 would add operational cost and complexity before justified.
- CommercialNews V1 instead uses Outbox + RabbitMQ + rebuild/reconciliation posture as the practical truth-following model.

### Microkernel Architecture
- Best for plugin/customization platforms.
- Not a primary requirement in CommercialNews V1.

### Space-Based Architecture
- Best for extreme scale and contention.
- Too ops-heavy for V1 and unnecessary unless traffic/contention signals demand it.

### Pure Event-Driven Architecture (as full system style)
- Event-driven is used selectively for side effects and derived-state convergence.
- A fully event-driven system for all flows would increase complexity and debugging burden in V1.

### Full Event Sourcing (as default system model)
- Valuable for history-first domains, but too heavy as the default truth model for CommercialNews V1.
- V1 instead keeps current-state truth stores and uses outbox/events for propagation behind those truth boundaries.

### Workflow-Orchestrator-First / Dataflow-Platform-First Architecture
- Strong explicit workflow orchestration can be useful at larger scale.
- But making a heavyweight workflow engine a baseline dependency in V1 would add cost and complexity before justified.
- CommercialNews V1 instead uses explicit workflow discipline without requiring a heavyweight orchestration platform.

---

## 9.8 Trade-offs (what we gain vs what we accept)

### Gains
- Low operational overhead in V1 (fewer deployable units).
- Strong modular boundaries support maintainability and evolution.
- Read path protection via async side effects.
- Clear path to introduce read models/projections if needed.
- Derived state is explicitly rebuildable and observable.
- Stream-style convergence lets side effects and projections follow truth without distributed commit.
- Batch/reconciliation/replay work has a place in the architecture without becoming hidden truth.

### Costs / risks
- Shared DB increases coupling risk if ownership rules are violated.
- Event-driven side effects require discipline (idempotency, DLQ, observability).
- Some counters and projections may be eventually consistent.
- Important batch workflows require bounded input, rerun-safety, and publication discipline.
- Derived-state lag and rebuild operations introduce operational responsibilities that must be monitored and governed.
- RabbitMQ is delivery-oriented, so rebuild/recovery cannot rely on broker history alone.

---

## 9.9 Evolution path (when to change topology)

We consider introducing additional components (or microservices) when signals justify it:
- sustained read bursts require dedicated read models/projections
- Interaction becomes a dominant hot path needing independent scaling
- coordinated deploys become frequent due to coupling
- operational risk demands smaller blast radius
- bounded rebuild/reconciliation workflows become numerous enough to justify more explicit orchestration or dedicated derived-data components
- stream-style analytics/replay needs outgrow the practical limits of the V1 Outbox + RabbitMQ posture

When topology changes are made, we record:
- an ADR in `docs/explanation/decisions/`
- updated components mapping (`08-components.md`)
- updated runtime scenarios (`04-runtime-view-v1.md`)
- updated governance/fitness functions (`07-architecture-governance.md`)
- updated batch/dataflow posture if reusable serving datasets or dedicated workflow components become first-class
- updated stream/derived-state policy if a richer streaming platform or event-history model is adopted