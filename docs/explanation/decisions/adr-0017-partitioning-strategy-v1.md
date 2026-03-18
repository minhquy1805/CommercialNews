# ADR-0017 Partitioning Strategy V1

## Status

**Accepted (V1)**

## Date

**2026-03-05**

## Context

CommercialNews V1 is a domain-partitioned modular monolith with API + Worker topology and selective event-driven side effects (Outbox → Broker → Consumers).

The system is characterized by:

* Read-heavy, bursty public traffic (hot article spikes)
* Security-critical flows in Identity
* Governance-critical flows in Authorization / Audit
* Non-critical but bursty async side effects (Notifications, Interaction aggregation, SEO updates)
* Strong requirement to protect the public read path (P95/P99 and availability)

We need a clear architectural decision for how CommercialNews will apply partitioning concepts (DDIA Chapter 6) in V1 and beyond, without introducing premature distributed complexity.

### Relevant docs

* `arc42/09-architecture-style.md`
* `arc42/10-system-data.md`
* `arc42/11-replication-v1.md`
* `arc42/12-partitioning-v1.md`

## Decision

CommercialNews V1 adopts a **partition-ready strategy**, not full DB sharding by default.

### 1) V1 does not introduce full cross-node DB sharding for core truth modules by default

Core modules (especially Content, Identity, Authorization) remain on current truth-store topology and rely on:

* query-driven indexes
* bounded queries
* Redis caching (derived only)
* async side-effect isolation
* lag/fallback observability

### 2) Partitioning is applied first as workload isolation

Before truth-store sharding, CommercialNews will prioritize:

* worker lane / ownership partitioning (where needed)
* backlog isolation (priority/retry lanes)
* async aggregation lanes (Interaction)
* projection rebuild buckets (V2+)
* search/read-model evolution (V2+) before truth sharding

### 3) Partitioning decisions are module-specific and workload-specific

There is no single partitioning strategy for all modules.

Examples:

* **Interaction / Notifications / Audit:** likely workload partitioning first
* **Audit / append-only logs:** future range/hybrid candidates
* **SEO slug routing:** hash-friendly exact lookup if ever required at larger scale
* **Reading:** read-model/projection evolution before truth sharding
* **Content / Identity / Authorization:** high bar for truth-table partitioning due to correctness complexity

### 4) Rebalancing and routing must use indirection

Future scalable components must prefer:

`key/work-unit -> logical partition/lane -> owner node/worker`

and avoid direct `hash(key) mod N` for scale-critical routing.

### 5) Partitioning changes are signal-driven

Stronger partitioning (workload/data/projection) is introduced only when sustained metrics show V1 tactics are insufficient.

Key signals include:

* read path P95/P99 degradation
* outbox backlog / oldest pending age
* queue lag / consumer latency / retry/DLQ pressure
* fallback-rate increases (SEO / Reading)
* recovery/replay/rebuild windows becoming operationally unsafe

## Rationale

This decision aligns with V1 priorities:

* Protect read path first
* Preserve truth correctness and safety
* Avoid premature distributed complexity
* Keep evolution path open toward projections/read models and stronger partitioning in V2+

It also matches the chosen architecture style:

* modular monolith (service-based, domain-partitioned)
* async side effects for burst isolation
* API + Worker deployable units

This lets CommercialNews apply DDIA partitioning concepts pragmatically:

* use them as a design and operations framework now
* implement stronger partitioning only when justified by real workload signals

## Consequences

### Positive

* Lower operational complexity in V1
* Clear focus on index/caching/query-shape correctness before sharding
* Stronger read-path protection via async isolation and graceful degradation
* Easier maintenance and evolution within module boundaries
* Better observability-driven scaling decisions

### Negative / Trade-offs

* Some high-volume workloads may require earlier optimization in Worker lanes/backpressure before data sharding
* Eventual consistency remains a managed trade-off for async side effects and future projections
* Teams must maintain discipline in:

  * idempotency
  * dedup
  * backlog monitoring
  * fallback correctness

## Risks

* Delaying partitioning too long without watching signals can cause avoidable latency/backlog incidents
* Teams may incorrectly treat “partition-ready” as “no need to model hotspots”
* Workload partitioning can become ad-hoc if ownership/routing metadata is not standardized

## Guardrails (V1)

* Do not shard core truth tables by default without measured evidence and an ADR update.
* Do not use `hash(key) mod N` for future scale-critical partition routing.
* Do not let async side effects block core reads/writes.
* Do not rely on derived stores for correctness-sensitive decisions (visibility, auth, identity state).
* Do measure lag/freshness/fallback before declaring a partitioning problem.

## Follow-up actions

* Maintain **Partitioning Readiness (V1/V2)** sections in module data docs (`system-data-*.md`)
* Prioritize hotspot-readiness reviews for:

  * Interaction
  * Notifications
  * Audit
  * SEO
* Define a standard pattern for future worker lane ownership metadata (when needed)
* Add partition/rebalance-related fitness functions and operational checks in governance docs (V2+)

Revisit this ADR when any of the following occurs:

* sustained public read SLO degradation
* persistent backlog/lag growth in async pipelines
* read-model/search workloads become dominant
* recovery/rebuild windows exceed operational tolerance

## Alternatives considered

### A) Full DB sharding in V1

Rejected for V1 due to:

* high operational complexity
* distributed correctness/rebalancing/routing overhead
* insufficient evidence that current workloads require it immediately

### B) Single global partitioning rule for all modules

Rejected because module workloads differ significantly:

* OLTP truth
* append-only logs
* projections/aggregates
* async delivery workflows

### C) Pure topology-first scaling (microservices first)

Rejected for V1; current style favors modular monolith + selective async side effects, with evolution triggered by measured signals.

## References

* `docs/explanation/architecture/arc42/09-architecture-style.md`
* `docs/explanation/architecture/arc42/10-system-data.md`
* `docs/explanation/architecture/arc42/11-replication-v1.md`
* `docs/explanation/architecture/arc42/12-partitioning-v1.md`
