# ADR-0013 — Outbox & Delivery Semantics (V1)

**Status:** Accepted  
**Date:** 2026-03-04  
**Decision owners:** Architecture / Platform  
**Scope:** System-wide (all async workflows)  
**Related:**
- `../architecture/arc42/04-runtime-view-v1.md`
- `../architecture/arc42/06-measurement-guide.md`
- `../architecture/arc42/10-system-data.md`
- `../architecture/arc42/11-replication-v1.md`
- `../architecture/arc42/13-transactions-and-consistency-v1.md`
- `../architecture/arc42/18-stream-processing-and-derived-state-v1.md`
- `../architecture/arc42/19-stream-processing-runtime-v1.md`
- `../api-architecture/01-api-architecture-charter-v1.md`
- `../api-architecture/09-observability-and-slos.md`
- ADR-0011 (Replication topology)
- ADR-0012 (Data store placement)

---

## Context

CommercialNews V1 requires:
- core flows to remain non-blocking (publish/unpublish, register/reset, read path)
- reliable side effects (audit, notifications, interaction aggregation, cache invalidation, derived-state refresh)
- operability under failures (retries, DLQ, replay, rebuild)
- safe behavior under at-least-once delivery and lagging consumers

Direct "DB write then publish to broker" is failure-prone:
- DB commit succeeds but broker publish fails → lost events, inconsistent derived state
- retries cause duplicates → duplicate emails/audit entries without idempotency
- stale or out-of-order consumer processing may overwrite fresher derived state unless versioning/resync rules exist

We need a system-wide, repeatable mechanism to ensure:
- domain state changes and the intent to publish an event are committed atomically
- publishing and consumption are retry-safe and observable
- replay/rebuild/resync can recover lagging derived state without redefining truth
- outbox-backed delivery remains the standard causal bridge between truth and async effects

---

## Decision

### 1) Adopt Outbox pattern as the replication log (required in V1)
For every domain change that triggers async workflows:
- write the domain state change
- write an `OutboxMessage` record
- commit both in **the same DB transaction**

A Background Worker is responsible for:
- polling `OutboxMessage`
- publishing to the broker (RabbitMQ)
- updating the outbox state machine

**Rule:** outbox is the standard V1 causal bridge between:
- committed truth
- async side effects
- derived-state propagation

### 2) Delivery semantics are at-least-once end-to-end
- Broker delivery and consumer processing are **at-least-once**
- Therefore:
  - duplicates are expected
  - retries are expected
  - replay is expected
  - consumers MUST be idempotent
  - effects must be safe under retry and duplicate delivery

We do not attempt "exactly-once" delivery via distributed transactions.

### 3) Consumer idempotency is mandatory (message-level and business-level)
All consumers MUST implement:

- **Message-level dedupe** by `MessageId` (or `EventId`)
- **Business-level idempotency** for side effects where duplicates are harmful

Examples:
- emails: dedupe by `(TemplateKey, Recipient, TokenHash)` or by `MessageId`
- audit: unique `AuditEventId` / `MessageId`
- aggregates: commutative updates and/or dedupe keys
- cache invalidation/update: duplicate processing must be harmless
- projections/materializations: duplicate apply must not corrupt active state

### 4) Ordering model: per-aggregate sequencing, not global order
When ordered transitions exist for an aggregate:
- events include `AggregateId` and `Version` (monotonic per aggregate)
- consumers enforce one of the following, depending on use case:
  - apply only when `Version == expected`
  - reject/ignore if `IncomingVersion <= LastAppliedVersion`
  - if version gaps or out-of-order delivery occur: **resync from truth store**

No global ordering is assumed.

**Rule:** ordering is scoped to the aggregate boundary that actually needs it.

### 5) Stale delivery and replay are first-class conditions
Consumers must distinguish:

#### A) Duplicate delivery
Same message delivered again.

Required protection:
- dedupe by `MessageId`

#### B) Stale delivery
Older aggregate version arrives after a newer version is already applied.

Required protection:
- version-aware reject/ignore logic
- or truth resync when exact freshness matters

#### C) Replay/rebuild input
Previously emitted messages are reprocessed intentionally for recovery or rebuild.

Required protection:
- idempotent consumer behavior
- clear truth-vs-derived rules
- safe publication/cutover if rebuilt output becomes an active derived dataset

### 6) Outbox state machine + retry policy
Outbox records progress through explicit states:

- `Pending` → eligible for publish
- `Published` → successfully published to broker
- `Failed` → transient failure; scheduled for retry
- `Dead` → exceeded retry policy or permanent failure; requires remediation

Retry policy (V1 policy-level):
- exponential backoff with jitter
- `NextRetryAt` is used for scheduling
- permanent errors route to `Dead` (DLQ-equivalent at outbox level)

Broker-level DLQ may also be used for poison messages at consumer side.

### 7) Outbox-backed flows must preserve truth-first semantics
A successful user-facing write means:
- truth committed
- outbox intent committed

It does **not** mean:
- broker publish has already happened
- a consumer has already processed the event
- caches are updated
- projections are caught up
- reports/summaries are rebuilt

**Rule:** consumer lag must never redefine business truth.

### 8) Observability requirements (operability contract)
We measure and alert on:
- outbox pending count
- outbox oldest pending age
- publish attempts/failures
- broker queue depth (ready/unacked)
- consumer failure rate + retry rate
- DLQ size/age (if enabled)
- dedupe hits / idempotency rejects
- stale-event reject count where measurable
- version-gap / truth-resync triggers where applicable
- replay/rebuild activity for important derived outputs
- derived-store fallback rates (where applicable)

---

## OutboxMessage contract (V1)

Minimum required fields (contract-level):
- `OutboxId` (monotonic PK)
- `MessageId` (UUID/ULID, unique)
- `EventType`
- `AggregateType`, `AggregateId`
- `Version` (nullable; required when ordering matters)
- `OccurredAt` (UTC)
- `CorrelationId` / `TraceId`
- `Payload` (JSON)
- `Status` (`Pending|Published|Failed|Dead`)
- `AttemptCount`
- `NextRetryAt` (UTC, nullable)
- `LastErrorCode` / `LastErrorMessage` (sanitized)

Recommended additional fields where useful:
- `PartitionKey` (if later needed for partition-aware scanning/routing)
- `Headers` / metadata bag (sanitized)
- `PublishedAt` (UTC, optional)
- `DeadReasonCode` (optional)
- `ProducerModule` (optional, useful for debugging and operations)

Indexing guidance:
- unique index on `MessageId`
- index on `(Status, NextRetryAt, OutboxId)` for efficient polling
- optional index on `(AggregateType, AggregateId, Version)` for debugging
- optional index on `(ProducerModule, Status, NextRetryAt)` for module-scoped operability

---

## Alternatives considered

### 1) Publish to broker inside the same request (no outbox)
- Pros: simpler code.
- Cons: loses events on broker outage; retries cause duplicates; breaks V1 non-blocking + operability goals.

### 2) Two-phase commit / distributed transactions
- Pros: theoretical stronger guarantees.
- Cons: high complexity, operational risk, and performance impact; not aligned with V1.

### 3) DB triggers to generate outbox
- Pros: prevents developers from forgetting to write outbox records.
- Cons: harder debugging/versioning/testing; increases coupling to DB specifics; reserved for exceptional cases only.

### 4) Broker as implicit replay source of truth
- Pros: may look simpler operationally.
- Cons: broker state is not business truth, not stable enough for all recovery reasoning, and not sufficient for truth-safe rebuild of correctness-sensitive derived state.

---

## Consequences

### Positive
- Core flows remain non-blocking; side effects become reliable and retry-safe
- Failures become observable via backlog/age metrics
- Derived stores can be rebuilt and caught up via outbox replay and truth resync
- Clear cross-module contract for events (`MessageId`, `CorrelationId`, `Version`)
- Replay, rebuild, and stale-delivery handling become explicit architecture concerns instead of hidden assumptions

### Negative / Trade-offs
- Requires disciplined consumer idempotency and version handling
- Adds operational components (publisher loop, state machine, DLQ/remediation processes)
- Backlog can grow under outages; must be planned and monitored
- Some consumers need explicit truth-resync logic instead of naive event-only application

---

## Implementation notes (V1)

- Public API writes `OutboxMessage` atomically with domain state changes.
- Background Worker publishes `Pending` messages ordered by `OutboxId` (stable scanning).
- Consumers:
  - store dedupe keys durably when effects are critical
  - implement safe retries and DLQ handling
  - distinguish duplicate delivery from stale delivery
  - resync from truth when version gaps or stale ordering ambiguity matter
- Email workflows:
  - persist `EmailDelivery` with unique `MessageId` and/or business intent key to prevent duplicate sends
  - attempt counts and last error are recorded for investigations
- Projection / cache / derived workflows:
  - must not let stale replay overwrite fresher truth-backed state
  - should use candidate → validate → publish/cutover when rebuilding correctness-sensitive derived outputs

---

## Follow-ups

- ADR-0014: Public Identifier Strategy (slugs/public IDs vs internal keys)
- ADR-0015: Cache Policy & Invalidation (Redis, especially SEO hot path)
- ADR-0016: Authorization Policy Model (governance + policy naming)
- ADR-0027: Stream Processing and Derived State Policy
- ADR-0028: Consumer Idempotency, Replay, and Rebuild Policy
- Module docs: `modules/{module}/06-idempotency-consistency.md` must specify:
  - which events are emitted/consumed
  - idempotency keys
  - ordering requirements and resync behavior
  - stale-delivery handling
  - replay/rebuild posture where derived outputs are important