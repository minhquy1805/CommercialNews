# arc42 — CommercialNews Architecture

## 1) Overview
CommercialNews is a content publishing system with:
- Clear article lifecycle (Draft / Published / Archived)
- SEO-focused metadata (slug/canonical/meta)
- Media attachment & ordering
- High-traffic interaction paths (views/likes/comments)
- Secure identity (email verification, refresh tokens)
- Admin governance (roles/permissions, audit trail)
- System notifications (verification/reset and optional new-article emails)

The architecture is designed as a **domain-partitioned modular monolith / service-based system** in V1,
with selective asynchronous workflows for side effects, burst isolation, **stream-style derived-state processing**, and bounded batch/rebuild workflows for repair, replay, and reconciliation.

A major architectural theme in V1 is **distributed-systems correctness under partial failure**:
- truth vs derived state is explicit
- timeout is treated as ambiguity, not proof of failure
- caches, projections, and derived serving artifacts may lag
- retries and duplicate delivery are expected
- idempotency, versioning, fallback, and observability are first-class rules

A second major theme is **derived-state discipline**:
- stream-style processing is used to move committed changes toward derived convergence
- batch is a first-class repair/rebuild lane
- derived outputs must remain rebuildable and observable
- reusable datasets are distinct from internal workflow state
- materialization is selective, not automatic
- candidate output is validated before publication/cutover
- truth correctness never waits for side-effect or rebuild completion

---

## 2) How to navigate arc42

## Current sections

- `01-introduction-goals.md`  
  : what we are building and why it matters

- `02-constraints.md`  
  : non-negotiable constraints shaping the architecture

- `03-building-blocks-modularity.md`  
  : module boundaries, ownership, contracts, and dependency rules

- `04-runtime-view-v1.md`  
  : key V1 workflows (sync vs async), failure modes, and observability notes

- `05-quality-requirements.md`  
  : system-level architecture characteristics, measures, trade-offs, and quality scenarios

- `06-measurement-guide.md`  
  : how we measure key architecture characteristics (policy-level plan for V1)

- `07-architecture-governance.md`  
  : guardrails and fitness functions to prevent architectural drift

- `08-components.md`  
  : deployable components in V1 and module-to-component mapping

- `09-architecture-style.md`  
  : chosen architecture style(s), decision criteria, and key patterns (sync/async boundaries, data ownership, consistency model)

- `10-system-data.md`  
  : system data model entry point (ownership, cross-module references, conventions) with links to module-level data models under `system-data/`

- `11-replication-v1.md`  
  : replication strategy for V1 (truth vs derived, Outbox→Broker→Consumers, lag budgets, idempotency/ordering, repair & observability)

- `12-partitioning-v1.md`  
  : partitioning strategy for V1 (partition-ready posture, hotspot/skew mitigation, secondary-index implications, rebalancing/routing readiness, and metric-driven scale triggers)

- `13-transactions-and-consistency-v1.md`  
  : transaction boundaries and consistency rules for V1 (local truth transactions, outbox atomicity, read-your-writes, eventual consistency, retry/idempotency, cache/truth visibility)

- `14-distributed-systems-assumptions-v1.md`  
  : system model for V1 under partial failure (partially synchronous timing, crash-recovery, faulty-but-honest internal nodes, malicious/malformed external inputs, truth vs derived posture, timeout ambiguity, clock/order limits, and fallback-first correctness rules)

- `15-consistency-ordering-and-consensus-v1.md`  
  : consistency classes, ordering scope, causality boundaries, consensus minimization, and coordination posture for V1 (strong truth-backed correctness, per-aggregate ordering, eventual derived behavior, versioning/fencing over timestamps, and no premature coordination infrastructure)

- `16-batch-processing-and-derived-data-v1.md`  
  : batch as a first-class processing lane in V1 (bounded inputs, derived outputs, rebuild/reconciliation posture, truth vs derived discipline, materialization/recompute rules, and batch observability expectations)

- `17-dataflow-and-batch-workflows-v1.md`  
  : multi-stage workflow model for V1 batch/rebuild/reconciliation work (stage types, input/output contracts, reusable datasets vs internal intermediate state, candidate-before-cutover publication, rerun/recovery semantics, and simple orchestration posture)

- `18-stream-processing-and-derived-state-v1.md`  
  : system-wide policy for stream-style change propagation and derived-state maintenance in V1 (truth vs derived ownership, outbox-driven propagation, projections/side effects, event-time considerations, joins, fault tolerance, rebuildability, and V1 non-goals)

- `19-stream-processing-runtime-v1.md`  
  : runtime shape of stream-style processing in V1 (truth commit → outbox → worker → broker → idempotent consumers → derived convergence, including lag, duplicate delivery, failure paths, and repair/rebuild hooks)

## Reading path (recommended)

For a fast understanding of the architecture, read in this order:

1. `01-introduction-goals.md`  
2. `02-constraints.md`  
3. `03-building-blocks-modularity.md`  
4. `04-runtime-view-v1.md`  
5. `09-architecture-style.md`  
6. `10-system-data.md`  
7. `11-replication-v1.md`  
8. `13-transactions-and-consistency-v1.md`  
9. `18-stream-processing-and-derived-state-v1.md`  
10. `19-stream-processing-runtime-v1.md`  
11. `16-batch-processing-and-derived-data-v1.md`  
12. `17-dataflow-and-batch-workflows-v1.md`  
13. `14-distributed-systems-assumptions-v1.md`  
14. `15-consistency-ordering-and-consensus-v1.md`  

That path gives the minimum architecture story:
- business goals
- constraints
- module ownership
- runtime behavior
- chosen style
- data ownership and truth/derived model
- replication posture
- transaction/consistency rules
- stream-style derived-state policy
- stream-runtime propagation and recovery behavior
- batch/derived-state repair posture
- workflow/materialization policy
- distributed-systems assumptions for correctness under lag, retries, pauses, stale state, and duplicate delivery
- consistency classes, ordering boundaries, and consensus posture for V1

### Planned sections (next)
- Deployment view (if split beyond V1 components)
- Risk register & technical debt backlog
- Operational runbooks (beyond policy-level measurement)
- Batch workflow catalog / operational playbooks (if the number of scheduled/rebuild/reconciliation jobs grows enough to justify a dedicated runbook set)

---

## 3) Key architectural hotspots (from the domain)

1. **Interaction hot path**  
   view/like/comment paths are burst-prone and must not degrade reading

2. **Identity flows**  
   verification/reset/refresh rotation are security-critical and require truth-first consistency

3. **Publish/unpublish**  
   auditability, reasons, and immediate visibility correctness are mandatory

4. **SEO correctness**  
   slug uniqueness, routing safety, and visibility-after-routing must remain correct under lag

5. **Media lifecycle**  
   soft delete/restore, primary media, and ordering must remain deterministic under retries/concurrency

6. **Distributed-systems ambiguity**  
   timeout, retry, duplicate delivery, stale cache, process pause, and lagging derived state must not break truth correctness

7. **Ordering and stale actors**  
   per-aggregate transitions, delayed consumers, and paused/restarted workers must not reintroduce older state or continue acting on expired authority

8. **Coordination boundaries**  
   singleton-style work, ownership transfer, uniqueness, and “one current winner” decisions must be solved at the narrowest authoritative boundary and must not drift into naive homegrown coordination

9. **Derived-state publication discipline**  
   stream/batch/rebuild outputs must not become hidden truth, and partially built outputs must not leak as completed state

10. **Batch workflow operability**  
   aggregation, rebuild, replay, reconciliation, archival, and cleanup workflows must have bounded inputs, rerun-safe behavior, and observable freshness/backlog signals

11. **Stream-runtime operability**  
   outbox backlog, broker lag, duplicate delivery, idempotent apply, stale projections, and repair/rebuild triggers must remain observable and recoverable

---

## 4) Architecture themes to keep in mind

Across all sections, CommercialNews V1 follows these cross-cutting themes:

- **Truth is explicit and owned by module**
- **Derived state may lag**
- **Core flows succeed on truth commit, not side-effect completion**
- **Outbox is the standard async replication boundary**
- **At-least-once delivery is normal**
- **Idempotency is mandatory**
- **Versioning beats timestamp-based freshness**
- **Cache is acceleration only, never hidden truth**
- **Ordering is scoped; global total order is not assumed by default**
- **Cause must be durable before effect becomes meaningful**
- **Stale actors and stale events must be rejectable**
- **Consensus and coordination are minimized by design**
- **If uncertain, prefer safe fallback over stale confidence**
- **Observability must distinguish truth-path health from derived-path lag**
- **Stream-style processing moves committed changes toward derived convergence**
- **RabbitMQ is delivery-oriented, not the permanent history source for all recovery**
- **Every important derived system must have a replay, rebuild, or reconciliation posture**
- **Batch is a first-class processing lane**
- **Bounded input is required for important batch workflows**
- **Reusable datasets are distinct from internal intermediate state**
- **Materialization is selective, not automatic**
- **Candidate output must be validated before publication/cutover**
- **Recompute is acceptable where it is simpler and safer than fragile incremental repair**
- **Rerun safety is mandatory for rebuild/reconciliation workflows**
- **Batch must support repair, replay, and safe recovery without redefining truth**

---

## 5) Links
- Domain capabilities: `../../domain/` (business view)
- Architecture decisions (ADR): `../../decisions/`
- Module-level API architecture: `../../api-architecture/`
- Module-level system data docs: `./system-data/`
- Quality profiles by module: `./quality/`
- ERD diagrams: `./diagrams/erd/`