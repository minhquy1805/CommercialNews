# 11 — Replication (V1)

This section defines the **replication strategy** for CommercialNews V1.
It turns Chapter 5 (DDIA) concepts into **operational rules**:
- what is replicated (truth vs derived)
- where staleness is acceptable
- how we preserve correctness under replication lag
- how we measure and repair replication

> Related:
> - `04-runtime-view-v1.md`
> - `09-architecture-style.md`
> - `10-system-data.md`
> - `12-partitioning-v1.md`
> - `13-transactions-and-consistency-v1.md`
> - `../../decisions/adr-0013-outbox-and-delivery-semantics-v1.md`
> - `../../decisions/adr-0015-cache-policy-and-invalidation-redis-v1.md`

---

## 11.1 Design intent (V1)

CommercialNews is **read-heavy**. Replication exists to keep reads fast and available,
while protecting invariants (visibility, authorization, identity state).

V1 priorities:
1) **Correctness & safety first** for security-sensitive and public visibility rules.
2) **Fast read path** via caches and derived reads.
3) **Reliable side effects** via async processing (at-least-once + idempotent).

---

## 11.2 Replication topology (V1)

### 11.2.1 Truth stores (system of record)
All business-critical writes go to a **single-leader primary** truth store.
Followers/read replicas may exist, but V1 correctness must not depend on them.

Truth owns invariants:
- publish/unpublish visibility
- slug uniqueness/routing truth (SEO hot path)
- role/permission assignments
- identity state (email verified, password changes, token revocation)

### 11.2.2 Derived stores (replicated state)
Derived stores are allowed to be **eventually consistent**:
- Redis caches (hot-path acceleration)
- projections/read models (V2+)
- delivery logs / aggregates (audit, notifications, interaction counters)

Derived stores must be:
- rebuildable from truth
- protected by fallback behavior when stale

### 11.2.3 Replication transport (application-level)
V1 uses **Outbox -> Broker -> Consumers**:
- The module that performs a write also writes an Outbox record **in the same DB transaction**
- Background Worker publishes Outbox messages to the broker (RabbitMQ)
- Consumers update derived stores and execute side effects

This is the V1 “replication log” for cross-module changes.

---

## 11.3 Consistency guarantees (V1)

Replication lag is expected. V1 explicitly supports these guarantees where required:

### A) Read-your-writes (read-after-write)
After a successful write, the same actor must see the update immediately.

Policy:
- Admin screens and Identity self-state reads must read from **primary** (bypass caches/replicas),
  or return the updated resource from the write result.

### B) Monotonic reads
A user should not see time going backward on repeated reads.

Policy:
- Avoid per-instance in-memory caches for shared read paths unless strictly invalidated.
- Prefer sticky routing (session affinity) for authenticated read-heavy flows
  where “appears then disappears” is unacceptable (comments/notifications lists if exposed).

### C) Consistent prefix reads (causality)
Readers must not see effects before causes.

Policy:
- Enforce cause→effect ordering in async pipelines:
  - materialize/ensure truth exists first
  - then emit effect (notify/index/cache warm)
- Provide safe fallbacks:
  - if routing succeeds but content is not yet available in derived stores, fallback to truth
  - never return “ghost” navigation that leaks drafts/unpublished content

---

## 11.4 Non-negotiable rules (V1)

1) **Truth correctness beats speed** on sensitive routes:
   - identity state, authorization decisions, publish visibility, SEO routing safety.
2) **No reliance on synchronous side effects**:
   - publish/unpublish/register/reset must not block on audit/notifications/interaction.
3) **At-least-once delivery is assumed**:
   - every consumer must be idempotent.
4) **Ordering is per aggregate**:
   - when ordered transitions exist, events carry `AggregateId + Version`.
5) **Derived stores are rebuildable**:
   - must have a backfill/rebuild plan and a fallback plan.

---

## 11.5 Event envelope, idempotency, ordering

### 11.5.1 Required event fields
Every outbox event MUST include:
- `messageId` (unique)
- `eventType`
- `aggregateType`, `aggregateId`
- `version` (monotonic per aggregate when ordering matters)
- `occurredAt`
- `correlationId` / `traceId`

### 11.5.2 Consumer behavior
- **Deduplication**: treat `messageId` as the primary idempotency key.
- **Ordering (if applicable)**:
  - apply only if `version == expected`
  - if gap/out-of-order detected: **resync from truth** (pull state) and continue
- **Retries**:
  - transient failures: retry with backoff
  - poison messages: DLQ + manual/automated remediation
- **Effect safety**:
  - email/send actions require idempotency keys (template + recipient + token/eventId)

---

## 11.6 Replication lag budgets & fallbacks (policy-level)

Lag budgets are module-defined, but V1 provides defaults:

- **Identity self-state + admin governance**: primary-only (no stale allowed)
- **SEO slug routing**: cache-first for speed, DB fallback for correctness; never leak drafts/unpublished
- **Content public reads**: can tolerate small lag (seconds) for non-critical metadata
- **Audit/Notifications/Interaction aggregates**: eventual, must be observable and recoverable

Fallback policy:
- If a derived store is stale/unavailable, prefer:
  - truth fallback (correct but slower), or
  - safe “updating, retry” response
over misleading 404/empty responses.

---

## 11.7 Repair mechanisms

- **Read repair (where applicable)**: refresh caches on read when stale is detected.
- **Reconciliation jobs**: periodic backfill for rarely-read projections.
- **Rebuild playbook**: ability to drop and rebuild derived stores from truth + event replay.

---

## 11.8 Observability requirements (V1)

Minimum replication SLIs (system-level):
- Outbox pending count
- Outbox oldest pending age (seconds)
- Queue depth per consumer (ready/unacked)
- Consumer processing latency (P95/P99)
- Derived-store fallback rate (e.g., SEO slug DB fallback)
- Duplicate prevention rate (dedupe hits) for audit/notifications

Replication incidents must be diagnosable using:
- correlationId propagation across sync + async flows
- structured logs for publish/register/governance actions