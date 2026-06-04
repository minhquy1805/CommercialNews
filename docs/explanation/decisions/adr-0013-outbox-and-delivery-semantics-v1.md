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

- core flows to remain non-blocking
  - publish / unpublish
  - register / verify / reset
  - public read path
- reliable async side effects
  - audit
  - notifications
  - interaction aggregation
  - cache invalidation
  - derived-state refresh
- operability under failures
  - retries
  - dead-letter handling
  - replay
  - rebuild
  - resync
- safe behavior under at-least-once delivery and lagging consumers

Direct "DB write then publish to broker" is failure-prone:

- DB commit succeeds but broker publish fails → lost events, inconsistent derived state
- retries cause duplicates → duplicate emails/audit entries without idempotency
- stale or out-of-order consumer processing may overwrite fresher derived state unless versioning/resync rules exist

We need a system-wide, repeatable mechanism to ensure:

- domain state changes and the intent to publish an event are committed atomically
- publishing and consumption are retry-safe and observable
- replay / rebuild / resync can recover lagging derived state without redefining truth
- outbox-backed delivery remains the standard causal bridge between truth and async effects

---

## Decision

### 1) Adopt Outbox pattern as the standard replication log (required in V1)

For every domain change that triggers async workflows:

- write the domain state change
- write an `OutboxMessage` record
- commit both in the **same DB transaction**

A Background Worker is responsible for:

- polling eligible `OutboxMessage` rows
- claiming work safely
- publishing to the broker (RabbitMQ)
- updating the outbox state machine

**Rule:** outbox is the standard V1 causal bridge between:

- committed truth
- async side effects
- derived-state propagation

### 2) Delivery semantics are at-least-once end-to-end

- broker delivery is **at-least-once**
- consumer processing is **at-least-once**
- therefore:
  - duplicates are expected
  - retries are expected
  - replay is expected
  - consumers MUST be idempotent
  - side effects must be safe under retry and duplicate delivery

CommercialNews V1 does **not** attempt exactly-once delivery via distributed transactions.

### 3) Consumer idempotency is mandatory at two levels

All consumers MUST implement:

#### A) Message-level dedupe
Prevent duplicate processing of the same delivered message.

Primary key:
- `MessageId`

#### B) Business-level idempotency
Protect harmful side effects even when two different messages represent the same underlying intent or when retry/replay races occur.

Examples:

- emails:
  - dedupe by `MessageId`
  - and/or protect by business intent key where required
- audit:
  - unique `MessageId`
- aggregates:
  - commutative updates and/or durable dedupe keys
- cache invalidation/update:
  - duplicate processing must be harmless
- projections/materializations:
  - duplicate apply must not corrupt active state

**Rule:** message-level dedupe alone is not always sufficient for correctness-sensitive side effects.

### 4) Ordering model: per-aggregate sequencing, not global order

When ordered transitions exist for an aggregate:

- events include `AggregateId` and `Version` (monotonic per aggregate)
- consumers enforce one of the following, depending on the use case:
  - apply only when `Version == expected`
  - reject / ignore if `IncomingVersion <= LastAppliedVersion`
  - if version gaps or out-of-order delivery occur: **resync from truth store**

No global ordering is assumed.

**Rule:** ordering is scoped to the aggregate boundary that actually needs it.

### 5) Duplicate, stale, and replay are distinct first-class conditions

Consumers must distinguish:

#### A) Duplicate delivery
The same message is delivered again.

Required protection:
- dedupe by `MessageId`

#### B) Stale delivery
An older aggregate version arrives after a newer version is already applied.

Required protection:
- version-aware reject / ignore logic
- or truth resync when exact freshness matters

#### C) Replay / rebuild input
Previously emitted messages are reprocessed intentionally for recovery, rebuild, or reconciliation.

Required protection:
- idempotent consumer behavior
- clear truth-vs-derived rules
- safe publication / cutover if rebuilt output becomes an active derived dataset

### 6) Outbox state machine is explicit and producer-side

Outbox state is a **producer-side publication state machine**, not a consumer-processing state machine.

Recommended states:

- `Pending`
  - eligible for claim/publish
- `Publishing`
  - temporarily claimed by a worker or equivalent lease/claim state
- `Published`
  - successfully handed off to the broker
- `Failed`
  - transient producer-side publish failure; scheduled for retry
- `Dead`
  - exceeded retry policy or hit a permanent producer-side failure; requires remediation

#### Important meaning of `Published`
`Published` means:

- broker handoff succeeded

It does **not** mean:

- a consumer has processed the message
- downstream side effects completed
- caches/projections/audit/notifications are caught up

#### Producer-side vs consumer-side failure separation

**Producer-side failure**
- happens before broker handoff completes
- tracked in outbox state (`Failed`, `Dead`)

**Consumer-side failure**
- happens after broker handoff succeeds
- handled by consumer retry / DLQ / idempotent reprocessing policy
- not represented as outbox publication failure

**Rule:** producer-side publication failure and consumer-side processing failure must not be conflated.

### 7) Polling and claim semantics are eligibility-based, not naive FIFO

Workers poll **eligible** outbox rows based on scheduling state, not just insertion order.

Eligibility is based on:

- `Status`
- `NextRetryAt`
- optional `Priority`
- stable tie-breaker such as `OutboxId`

Recommended behavior:

- select rows eligible for publish now
- claim them atomically
- avoid multiple workers repeatedly publishing the same row
- use `OutboxId` as a stable scan/pagination tie-breaker, not as the only scheduling rule

**Rule:** retry scheduling and priority must take precedence over naive strict FIFO.

### 8) Retry policy is explicit

Retry policy in V1 is policy-level but mandatory in behavior:

- exponential backoff
- jitter recommended
- `NextRetryAt` drives future eligibility
- permanent producer-side errors route to `Dead`

Broker-level DLQ may also be used for poison messages at the consumer side.

### 9) Outbox-backed flows preserve truth-first semantics

A successful user-facing write means:

- truth committed
- outbox intent committed

It does **not** mean:

- broker publish already happened
- a consumer already processed the event
- caches are updated
- projections are caught up
- notifications were sent
- reports/summaries were rebuilt

**Rule:** consumer lag must never redefine business truth.

### 10) Observability is part of the contract

We measure and alert on:

- outbox pending count
- outbox oldest pending age
- publish attempts / failures
- publish success rate
- broker queue depth (ready / unacked)
- consumer failure rate + retry rate
- DLQ size / age where enabled
- dedupe hits / idempotency rejects
- stale-event reject count where measurable
- version-gap / truth-resync triggers where applicable
- replay / rebuild activity for important derived outputs
- derived-store fallback rates where applicable

---

## OutboxMessage contract (V1)

Minimum required fields (contract-level):

- `OutboxId` (monotonic PK)
- `MessageId` (UUID/ULID, unique)
- `EventType`
- `AggregateType`
- `AggregateId`
- `Version` (nullable; required when ordering matters)
- `OccurredAt` (UTC)
- `CorrelationId` / `TraceId`
- `Payload` (JSON, sanitized)
- `Status` (`Pending|Publishing|Published|Failed|Dead`)
- `AttemptCount`
- `NextRetryAt` (UTC, nullable)
- `LastErrorCode` / `LastErrorMessage` (sanitized)

Recommended additional fields where useful:

- `Priority`
- `PartitionKey`
- `Headers` / metadata bag (sanitized)
- `PublishedAt` (UTC, optional)
- `DeadReasonCode` (optional)
- `ProducerModule` (optional, useful for debugging and operations)
- claim/lease metadata if needed by implementation

### Naming rule

CommercialNews uses **`MessageId`** as the system-wide contract name for outbox/event identity.

If a module currently uses a name such as `MessageKey`, that field should be treated as the module-local representation of the same concept and should be converged to `MessageId` where practical.

### Indexing guidance

Required:

- unique index on `MessageId`
- index on `(Status, NextRetryAt, OutboxId)` for efficient eligible polling

Recommended where useful:

- index on `(Status, Priority, NextRetryAt, OutboxId)`
- index on `(AggregateType, AggregateId, Version)` for debugging/order analysis
- index on `(ProducerModule, Status, NextRetryAt)` for module-scoped operability

---

## Alternatives considered

### 1) Publish to broker inside the same request (no outbox)

**Pros**
- simpler code path

**Cons**
- loses events on broker outage
- retries cause duplicates
- breaks V1 non-blocking + operability goals

### 2) Two-phase commit / distributed transactions

**Pros**
- theoretical stronger guarantees

**Cons**
- high complexity
- operational risk
- performance impact
- not aligned with V1

### 3) DB triggers to generate outbox

**Pros**
- may prevent developers from forgetting to write outbox records

**Cons**
- harder debugging/versioning/testing
- tighter DB coupling
- reserved for exceptional cases only

### 4) Broker as implicit replay source of truth

**Pros**
- may appear operationally simpler

**Cons**
- broker state is not business truth
- not stable enough for all recovery reasoning
- insufficient for truth-safe rebuild of correctness-sensitive derived state

---

## Consequences

### Positive

- core flows remain non-blocking
- side effects become reliable and retry-safe
- failures become observable via backlog/age metrics
- derived stores can be rebuilt and caught up via replay and truth resync
- clear cross-module event contract:
  - `MessageId`
  - `CorrelationId`
  - `AggregateId`
  - `Version` where needed
- replay, rebuild, duplicate, and stale-delivery handling become explicit architecture concerns

### Negative / Trade-offs

- requires disciplined consumer idempotency and version handling
- adds operational components:
  - publisher loop
  - claim/publish state machine
  - remediation / DLQ handling
- backlog can grow under outages and must be planned and monitored
- some consumers need explicit truth-resync logic instead of naive event-only application

---

## Implementation notes (V1)

### Producer side

- Public API / admin API writes `OutboxMessage` atomically with domain state changes.
- Workers publish **eligible** outbox rows, not merely the oldest inserted row.
- Workers should use atomic claim/update semantics or an equivalent lease mechanism to reduce duplicate broker publishes.

### Consumer side

Consumers:

- store dedupe keys durably when effects are critical
- implement safe retries and DLQ handling
- distinguish duplicate delivery from stale delivery
- resync from truth when version gaps or stale ordering ambiguity matter

### Email workflows

- persist `EmailDelivery` with unique `MessageId` and/or a business intent guard when harmful duplicates are possible
- record attempt counts and last error for investigations
- do not treat broker handoff as equivalent to successful delivery

### Projection / cache / derived workflows

- must not let stale replay overwrite fresher truth-backed state
- should use candidate → validate → publish/cutover when rebuilding correctness-sensitive derived outputs

### Shared outbox posture

CommercialNews V1 uses a **shared system outbox contract**.

Physical implementation may be:

- one shared outbox table in a shared database deployment
- or an equivalent system-level outbox implementation that preserves the same contract and semantics

Module docs must describe:

- which events a module emits
- which events it consumes
- what idempotency/business guards apply
- what ordering/resync rules apply

---

## Follow-ups

- ADR-0014: Public Identifier Strategy
- ADR-0015: Cache Policy & Invalidation
- ADR-0016: Authorization Policy Model
- ADR-0027: Stream Processing and Derived State Policy
- ADR-0028: Consumer Idempotency, Replay, and Rebuild Policy

Module docs such as `modules/{module}/06-idempotency-consistency.md` must specify:

- emitted / consumed events
- idempotency keys
- business-level duplicate protections where relevant
- ordering requirements and resync behavior
- stale-delivery handling
- replay / rebuild posture where derived outputs are important
