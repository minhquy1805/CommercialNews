# ADR-0027 — Stream Processing and Derived State Policy (V1)

**Status:** Accepted  
**Date:** 2026-03-11  
**Decision owners:** Architecture / Platform  
**Scope:** System-wide (all truth-to-derived propagation and stream-style derived-state processing)  
**Related:**
- `../architecture/arc42/04-runtime-view-v1.md`
- `../architecture/arc42/09-architecture-style.md`
- `../architecture/arc42/11-replication-v1.md`
- `../architecture/arc42/13-transactions-and-consistency-v1.md`
- `../architecture/arc42/16-batch-processing-and-derived-data-v1.md`
- `../architecture/arc42/17-dataflow-and-batch-workflows-v1.md`
- `../architecture/arc42/18-stream-processing-and-derived-state-v1.md`
- `../architecture/arc42/19-stream-processing-runtime-v1.md`
- ADR-0013 (Outbox & delivery semantics)
- ADR-0015 (Cache policy & invalidation)
- ADR-0021 (Clock, time, and ordering policy)
- ADR-0022 (Versioning and fencing strategy)
- ADR-0024 (Distributed coordination and singleton work policy)

---

## Context

CommercialNews V1 is built around a strict distinction between:

- **truth state** committed synchronously in the owning module
- **derived state** maintained asynchronously for acceleration, enrichment, summary, and side effects

The system already defines:

- local truth transactions
- outbox atomicity
- at-least-once async delivery
- idempotent consumers
- bounded batch/rebuild/reconciliation workflows

However, without an explicit system-wide policy, teams may drift into unsafe patterns such as:

- ad hoc dual writes from request handlers to DB + broker/cache/search/email
- treating caches or projections as hidden truth
- assuming broker delivery implies durable replay history
- making public correctness depend on lagging derived systems
- using incremental derived-state maintenance without rebuild/reconciliation posture
- over-expanding V1 into a platform-first streaming architecture before justified

We need a clear architectural decision that defines:

- what stream-style processing means in CommercialNews V1
- which derived-state patterns are approved
- which patterns are explicitly out of scope
- how batch and stream relate
- what the runtime and recovery expectations are for truth-following systems

---

## Decision

### 1) Truth is committed once, in the owning module; derived systems follow asynchronously
CommercialNews V1 defines core business success at the **truth boundary** only.

A request succeeds when:

- the owning module commits truth successfully
- and the outbox intent is committed when async propagation is required

It does **not** require downstream completion of:

- audit append
- email send
- cache invalidation
- search/projection update
- counter/summary refresh

Derived systems follow committed truth asynchronously.

---

### 2) Stream-style processing in V1 is built on Outbox → Worker → RabbitMQ → idempotent consumers
CommercialNews V1 adopts the following standard change-propagation model:

1. module commits truth
2. module writes outbox atomically in the same transaction
3. Background Worker publishes to RabbitMQ
4. broker delivers at least once
5. consumers update derived systems or perform side effects idempotently

This is the approved V1 mechanism for:

- async side effects
- projection maintenance
- cache invalidation
- interaction-derived aggregation signals
- delayed consistency convergence behind truth

---

### 3) Derived systems are followers, not co-equal write authorities
The following are classified as **derived systems**:

- Redis caches
- read projections / materialized views
- search/serving artifacts
- counters and summaries
- notification delivery views
- audit reporting summaries
- non-critical dashboards

Derived systems may:

- lag
- be stale
- be rebuilt
- be temporarily unavailable

Derived systems must **not**:

- silently become the only source of publication visibility truth
- become the only reliable source for security-sensitive reads
- be exposed as authoritative before validation/cutover when rebuilt
- override owning truth under disagreement

When correctness matters, truth-safe fallback is required.

---

### 4) RabbitMQ is delivery-oriented in V1, not the system-wide permanent event history
RabbitMQ in V1 is approved for:

- buffering
- decoupling producer and consumer timing
- retry/redelivery semantics
- burst isolation

RabbitMQ is **not** treated as:

- the permanent replay source for every event family
- a replacement for module-owned history/lifecycle storage
- a system-wide event sourcing log

Recovery and rebuild must rely on the approved combination of:

- truth stores
- outbox retention where policy allows
- module-owned history/lifecycle tables
- bounded rebuild/reconciliation workflows
- deterministic regeneration from authoritative inputs

---

### 5) Batch remains the repair/rebuild lane for derived state
CommercialNews V1 explicitly keeps **bounded batch/rebuild/reconciliation** as a first-class lane.

Batch workflows are approved for:

- rebuild
- replay
- reconciliation
- archival
- cleanup
- rematerialization of derived outputs
- bounded summary generation

Batch is the preferred recovery lane when bounded recompute is simpler and safer than fragile incremental repair.

Batch outputs remain derived outputs.
They do not redefine truth ownership.

---

### 6) Stream processing in V1 is selective, not platform-first
CommercialNews V1 does **not** adopt a platform-first streaming architecture.

Approved V1 stream-style use cases include:

- reliable side effects after truth commit
- projection/read-model maintenance
- cache invalidation/update requests
- interaction-derived counters/summaries
- selected near-real-time derived serving updates
- bounded repair/replay hooks

CommercialNews V1 does **not** require as baseline dependencies:

- a dedicated log-based streaming platform
- full event sourcing
- global total-order infrastructure
- heavyweight workflow-engine orchestration for all async work

---

### 7) Time semantics and join semantics must be explicit where meaning depends on them
For stream-style analytics, aggregation, or enrichment pipelines, teams must define:

- whether logic uses **event time** or **processing time**
- how late arrivals are handled
- whether joins use:
  - current state
  - event-time state
  - latest available derived state

No hidden time semantics are allowed in important pipelines.

This is especially relevant for:

- interaction analytics
- trending
- security/anomaly detection
- future search/click correlations
- enrichment flows where reference state changes over time

---

### 8) Every important derived system must have a recovery path
Any derived system important enough to affect user/admin experience must have at least one approved recovery strategy:

- rebuild from truth
- replay from retained operational history
- reconciliation against authoritative source
- bounded recomputation

A derived system without a recovery path is too close to hidden truth and must be redesigned or reclassified.

---

### 9) Safe fallback beats stale confidence
When derived state is missing, stale, delayed, or suspected inconsistent:

- prefer truth fallback
- prefer degraded-but-correct behavior
- prefer safe non-progress over unsafe stale apply
- do not expose hidden inconsistency as authoritative business state

This rule is mandatory for:

- publication visibility
- security-sensitive state
- governance-sensitive reads
- slug/routing safety where visibility correctness is at stake

---

## Approved V1 usage patterns

### A) Truth commit → outbox → downstream side effects
Examples:
- publish → audit + notifications + SEO reaction
- register/reset → notification delivery
- governance change → audit append

### B) Truth commit → outbox → derived projection maintenance
Examples:
- content changes → reading/search/SEO-serving artifacts
- auth/governance changes → derived effective-permission views/caches

### C) Interaction event ingestion → derived summaries/counters
Examples:
- article views → counters/trending inputs
- future engagement summaries

### D) Bounded repair/reconciliation after async lag or failure
Examples:
- rebuild stale derived SEO-serving artifacts
- reconcile counters against bounded authoritative input
- replay delayed notification or audit-derived views

---

## Disallowed or deferred patterns in V1

### 1) Ad hoc dual writes from request handlers
Disallowed:
- DB + broker publish inline as required success
- DB + Redis update as required success
- DB + email send inline
- DB + projection/search update inline as mandatory completion

### 2) Treating a cache or projection as hidden truth
Disallowed:
- publication visibility correctness depending only on cache/projection freshness
- governance/security correctness depending only on lagging derived views

### 3) Full event sourcing as the default system model
Deferred unless adopted explicitly for a narrow subdomain by future ADR.

### 4) Permanent broker-retained replay as the only recovery model
Disallowed as a system-wide assumption in V1.

### 5) Global exactly-once claims across heterogeneous systems
Disallowed unless proven within an explicit narrow mechanism.
V1 defaults to:
- at-least-once delivery
- effectively-once outcomes through idempotency and rebuildability

---

## Consequences

### Positive
- Keeps truth ownership explicit
- Prevents hidden truth from emerging in caches/projections
- Aligns async runtime with Outbox and idempotent-consumer posture already chosen
- Gives teams a safe pattern for projections and side effects
- Preserves batch/rebuild as a valid recovery lane
- Avoids premature platform complexity in V1

### Negative / Trade-offs
- Derived systems may visibly lag
- Teams must implement consumer idempotency and rebuild discipline
- Recovery depends on good observability and repair workflows
- Some serving paths must implement truth fallback for correctness-sensitive cases
- RabbitMQ alone cannot be treated as universal replay history

---

## Implementation notes (V1)

- Outbox remains the standard change-propagation boundary.
- Module docs must specify which outputs are truth vs derived.
- Derived systems that affect UX or admin experience should document:
  - freshness expectations
  - fallback behavior
  - rebuild/reconciliation posture
- Interaction and analytics flows should explicitly document time semantics when windowed logic matters.
- Projection/update consumers should prefer:
  - idempotent upsert
  - version-aware apply rules
  - safe resync from truth when gaps/out-of-order are detected

---

## Follow-ups

- ADR-0028: Consumer Idempotency, Replay, and Rebuild Policy (V1)
- Update arc42 `00-index.md` to include sections 18 and 19
- Update module docs, especially:
  - Content
  - SEO
  - Reading
  - Interaction
  - Notifications
  - Audit
  - Authorization
  - Identity
- Ensure module-level docs explicitly state:
  - truth ownership
  - derived outputs
  - fallback rules
  - replay/rebuild posture