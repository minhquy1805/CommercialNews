# ADR-0018 — Transaction Boundaries & Consistency Model (V1)

**Status:** Accepted  
**Date:** 2026-03-06  
**Decision owners:** Architecture / Platform  
**Scope:** System-wide (truth writes, transaction boundaries, consistency expectations, retry/idempotency posture)  
**Related:**
- `../architecture/arc42/02-constraints.md`
- `../architecture/arc42/03-building-blocks-modularity.md`
- `../architecture/arc42/04-runtime-view-v1.md`
- `../architecture/arc42/09-architecture-style.md`
- `../architecture/arc42/10-system-data.md`
- `../architecture/arc42/11-replication-v1.md`
- `../architecture/arc42/12-partitioning-v1.md`
- `../architecture/arc42/13-transactions-and-consistency-v1.md`
- ADR-0012 (Data store placement)
- ADR-0013 (Outbox & delivery semantics)
- ADR-0015 (Cache policy & invalidation)
- ADR-0016 (Authorization model)

---

## Context

CommercialNews V1 is a domain-partitioned modular monolith / service-based system with:

- synchronous truth writes for core user and admin flows
- asynchronous side effects for audit, notifications, aggregation, and invalidation
- a shared database deployment option, but explicit module ownership
- read-path priority and a strong requirement that non-critical subsystems must not block core flows

Without an explicit transaction policy, the architecture risks drift in several ways:

- developers stretching transactions across module boundaries because tables are physically reachable
- core flows waiting on broker publish, email delivery, cache invalidation, or external APIs
- application-level "check then write" logic being mistaken for safe concurrency control
- derived stores/caches being treated as if they were truth
- retries causing duplicate side effects or inconsistent state transitions

We need one system-wide rule set that defines:

- where a transaction starts and stops
- what must commit atomically
- what is allowed to lag
- what must be immediately visible after success
- how retries and at-least-once delivery interact with correctness

---

## Decision

### 1) Use local truth transactions as the core consistency boundary
Each command commits synchronously at the **owning module’s truth boundary**.

A successful write means:
- the module’s truth change committed
- and, when async side effects are required, the corresponding Outbox record committed in the same transaction

We do not define system success as “all side effects completed”.

---

### 2) Outbox is part of the same local transaction as the truth change
For commands that trigger async work, the system MUST commit atomically:

- truth change
- local lifecycle/history metadata required by the command
- Outbox record

This is the standard V1 write pattern.

Examples:
- Content publish/unpublish + outbox
- Identity register/verify/reset + outbox
- Authorization role/permission mutation + outbox
- Media attach/reorder/set-primary + outbox
- Interaction action write + outbox (when emitted)

---

### 3) Cross-module side effects are asynchronous and post-commit
The following are not part of the originating truth transaction:

- broker publish
- email sending
- audit ingestion in downstream stores
- Redis invalidation as a success condition
- projection/read-model refresh
- search/index updates
- external HTTP/API calls

These are handled after commit through:
- Outbox
- broker
- background worker / consumers
- at-least-once processing
- idempotent handlers

---

### 4) Shared DB does not imply shared transactional ownership
CommercialNews may run with a shared DB in V1, but transaction boundaries still follow **module ownership**, not physical reachability.

A module may write:
- its own truth tables/documents
- its own local metadata/history
- approved local replication artifacts such as Outbox
- local durable idempotency markers where needed

A module must not widen its transaction boundary into another module’s business truth just because both live in the same database.

---

### 5) Transactions must be short and bounded
Transactions MUST:
- complete within one request/handler execution
- avoid human interaction while open
- avoid external network calls
- avoid waiting for async consumers
- avoid long-running cross-module workflows

This reduces:
- lock duration
- contention risk
- retry complexity
- correctness ambiguity under failure

---

### 6) Truth is authoritative; derived state may lag
The architecture distinguishes between:

#### Strong local consistency
Required for:
- lifecycle truth changes
- security/auth state changes
- governance truth changes
- uniqueness/invariant enforcement at the owning store
- token rotation/revocation state
- attachment/primary/order invariants in Media truth

#### Eventual consistency
Accepted for:
- audit persistence
- notification delivery
- interaction counters/aggregates
- projections/read models
- cache invalidation
- selected metadata enrichments

Derived state may be stale, missing, or rebuildable.

---

### 7) Read-your-writes is selective and explicit
Immediate post-success visibility is required for:
- Identity self-state after verify/reset/password change
- Authorization governance reads after changes
- truth-based publication visibility after publish/unpublish
- other security-sensitive flows where stale reads would mislead the user or admin

These reads must:
- return from truth directly, or
- return the committed write result

They must not depend on Redis freshness.

---

### 8) Constraints first, stronger concurrency controls only where needed
The architecture does not assume that “using a transaction” alone prevents all concurrency anomalies.

The system posture is:

#### Prefer DB constraints first
For invariants such as:
- unique slug
- unique email
- unique `(UserId, RoleId)`
- unique `(RoleId, PermissionId)`
- unique attachment identity
- unique primary media scope

#### Use optimistic concurrency where stale edit is plausible
Examples:
- admin article edit
- SEO metadata edit
- profile/config/document edits
- reorder/set-final-state admin workflows

#### Treat check-then-write invariants as high risk
Examples:
- “if no row exists, insert”
- “if still at least one admin exists, revoke”
- “if quota remains, consume”
- “if no slot overlaps, book”

These require explicit design and may need:
- stronger constraints
- stronger isolation
- locking
- invariant-specific transaction handling

---

### 9) At-least-once delivery is normal; idempotency is mandatory
Since V1 uses Outbox + broker + consumers with at-least-once delivery:

- duplicates are expected
- retries are expected
- consumers MUST be idempotent
- side effects must be safe under replay

This applies to:
- Notifications
- Audit
- Interaction aggregation
- cache invalidation/update consumers
- future projections/read models

---

### 10) Public correctness is enforced by truth, not by cache freshness
Reading and SEO hot paths may use cache-first patterns, but:

- routing success does not imply visibility success
- stale cache must not leak drafts/unpublished content
- fallback to truth is required when freshness is uncertain
- safe `404` is preferred over incorrect exposure

---

## Decision summary

CommercialNews V1 adopts the following consistency model:

- **local truth transaction** as the primary correctness boundary
- **Outbox in the same transaction** as the truth change
- **async side effects** outside the transaction
- **strong local consistency** for owning-module truth
- **eventual consistency** for derived stores and delivery side effects
- **selective read-your-writes** for identity, governance, and truth-sensitive flows
- **DB constraints first**, then targeted concurrency control where needed
- **no distributed transactions / 2PC** across DB + broker + cache + external providers

---

## Alternatives considered

### 1) Treat all workflows as one end-to-end transaction
- Pros: superficially simple mental model.
- Cons: false in practice across broker, Redis, email, and external systems; creates long fragile transactions; blocks core flows on non-critical work.

Rejected.

---

### 2) Use distributed transactions / 2PC across DB and async infrastructure
- Pros: theoretical stronger end-to-end atomicity.
- Cons: high operational complexity, higher latency, difficult failure handling, poor fit for V1 team size and architecture style.

Rejected.

---

### 3) Let each module choose its own transaction semantics independently
- Pros: local freedom.
- Cons: guarantees drift, inconsistent behavior, and weakens system-wide operability and reasoning.

Rejected.

---

### 4) Pure eventual consistency for all writes
- Pros: simpler async-first posture in some areas.
- Cons: unacceptable for security state, governance truth, publication visibility, and ownership invariants.

Rejected.

---

## Consequences

### Positive
- Core flows remain fast and decoupled from non-critical subsystems
- Truth boundaries stay aligned with module ownership
- Outbox prevents “DB committed but event lost” failures
- Derived systems become rebuildable and observable
- Retry/idempotency posture is consistent across modules
- Public visibility and security correctness are preserved even under lag

### Negative / Trade-offs
- End-to-end workflows are not atomically complete at request return time
- Derived data can lag and requires fallback logic
- Consumers must be disciplined about dedupe and replay safety
- Some complex invariants still require explicit concurrency design beyond “basic transaction”
- Operational maturity is required for backlog, retry, DLQ, and replay handling

---

## Implementation notes (V1)

- Every module-level `06-idempotency-consistency.md` must align with this ADR.
- Write handlers should follow the standard shape:
  1. validate
  2. authorize (if required)
  3. load truth state
  4. apply truth mutation
  5. write outbox in same transaction
  6. commit
  7. return success based on truth commit only
- Consumers must implement durable or policy-approved dedupe.
- Redis remains derived-only and must not become hidden truth.
- Public read correctness must favor truth-backed fallback over stale derived failure.

---

## Follow-ups

- Keep `13-transactions-and-consistency-v1.md` as the architecture-level reference.
- Ensure all module docs reference this ADR and the arc42 section.
- Add stronger invariant-specific ADRs later if needed for:
  - booking/slot rules
  - “last admin” governance protection
  - quota/balance style checks
  - projection checkpointing as primary read source