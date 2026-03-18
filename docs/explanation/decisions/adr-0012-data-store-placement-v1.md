# ADR-0012 — Data Store Placement (V1)

**Status:** Accepted  
**Date:** 2026-03-04  
**Decision owners:** Architecture / Platform  
**Scope:** System-wide (module store ownership and usage rules)  
**Related:**
- `../architecture/arc42/10-system-data.md`
- `../architecture/arc42/system-data/` (module data models)
- `../architecture/arc42/11-replication-v1.md`
- `../architecture/arc42/04-runtime-view-v1.md`
- `../architecture/arc42/13-transactions-and-consistency-v1.md`
- `../architecture/arc42/16-batch-processing-and-derived-data-v1.md`
- `../architecture/arc42/17-dataflow-and-batch-workflows-v1.md`
- `../architecture/arc42/18-stream-processing-and-derived-state-v1.md`
- `../api-architecture/01-api-architecture-charter-v1.md`

---

## Context

CommercialNews V1 is read-heavy and must remain operable under burst traffic while preserving:
- visibility correctness (draft/unpublished never leak)
- security-critical identity flows (verification/reset/refresh)
- governance traceability (audit trail)
- non-blocking side effects (notifications, audit ingestion, interaction aggregation)

We use a modular monolith (Public API + Background Worker) with async workflows (Outbox → Broker → Consumers).
We must decide which stores are used for:
- OLTP truth (strong invariants)
- append-only logs (audit/history)
- caches (hot-path)
- aggregates (precomputed counters)
- media/object blobs
- future document/reporting or warehouse-style analytical outputs

We also need the store policy to stay aligned with:
- explicit **module source-of-truth ownership**
- truth vs derived separation
- replay/rebuild/reconciliation workflows
- future polyglot expansion without prematurely widening V1 complexity

---

## Decision

### 1) SQL Server is the primary relational OLTP truth store for V1
SQL Server is the system of record for business-critical mutable state, invariants, and relationships:
- Content core state (draft/published/unpublished/archived) + editorial relationships
- SEO routing truth (slug registry / routing table) + metadata
- Identity security state (users, refresh tokens, verification/reset artifacts)
- Authorization governance state (roles, permissions, assignments)
- Media metadata + attachment/order/primary relationships
- Notifications delivery-state truth
- Audit append-only evidence truth
- Interaction truth where relational invariants or durable event/log storage are required

**Rule:** truth invariants MUST be enforced in SQL via constraints, indexes, and transactional updates.

**Why relational first in V1**
- V1 correctness depends heavily on:
  - uniqueness constraints
  - relationship integrity
  - transactional mutation of multiple related rows
  - predictable OLTP operations for admin/security/governance flows
- CommercialNews is not a document-first V1 architecture.
- The primary V1 problem is not schema flexibility; it is **correctness, ownership, and predictable operational behavior**.

### 2) Source of truth is owned by module, not by physical accessibility
Even with a shared SQL deployment, each module owns its own truth tables/schema and invariants.

Examples:
- Content owns lifecycle/publication truth
- SEO owns slug-routing truth and SEO metadata truth
- Identity owns account/token/security truth
- Authorization owns role/permission/assignment truth
- Media owns metadata + attachment/order/primary truth
- Notifications owns delivery-state truth
- Audit owns append-only evidence truth
- Interaction owns interaction truth and aggregate truth where explicitly defined

**Rule:** a shared SQL database does **not** imply shared truth ownership.

**Consequence:** no module may redefine another module’s truth just because the tables are physically reachable.

### 3) Redis is used only for derived/accelerating state (never as truth)
Redis is allowed for:
- hot-path caches (SEO slug resolution, read path caching where safe)
- rate limiting counters (Identity + abuse-prone endpoints)
- idempotency/dedup caches with TTL (non-critical or supplementary effects)
- counters/aggregates (views/likes) when eventual accuracy is acceptable
- short-lived freshness markers / revision caches where truth fallback exists

Redis is NOT allowed to be the source of truth for:
- identity/authorization decisions
- publication visibility
- slug uniqueness/routing truth
- attachment/order/primary truth
- audit evidence truth
- canonical delivery-state truth

**Rule:** Redis is an acceleration layer only.

### 4) Append-only logs are stored in SQL in V1 (with retention policies)
Append-only, investigation-ready or replay-relevant logs are stored in SQL (schema-owned by modules):
- AuditLog / AuditEvent
- LoginHistory
- Notification delivery history
- OutboxMessage
- interaction raw events/logs where durably stored
- reconciliation or maintenance logs where policy requires durable traceability

**Rule:** logs are append-only by default; deletes require explicit retention/purge policy.

### 5) Aggregates and derived read-serving state are computed asynchronously
Precomputed aggregates or derived serving state are:
- updated asynchronously by the Background Worker and/or bounded workflows
- subordinate to module truth
- rebuildable/replayable
- stored in SQL in V1 unless a stronger reason exists to externalize them later

Examples:
- Interaction counters/trending inputs
- derived governance or delivery summaries
- read-optimized derived tables where introduced
- reconciliation outputs and bounded reporting artifacts

**Rule:** derived state may lag and must not become hidden truth.

### 6) RabbitMQ is required for async workflows; it is not a database
RabbitMQ transports events and commands for async side effects.

**Rule:** the broker is not a source of truth. Durable truth is in SQL (and optional object storage for blobs).

**Consequence:** replay/rebuild/resync should start from:
- truth stores
- append-only logs
- durable outbox/event identity
not from broker state alone.

### 7) Object storage is reserved for media blobs; SQL stores media truth
Binary media objects belong in object storage (future or environment-specific).
SQL stores:
- media metadata
- attachment relationships
- order
- primary selection
- soft-delete/restore truth

**Rule:** object storage/CDN presence is not relationship truth.

### 8) Document database is not a primary V1 truth store
A document database is **not selected as primary truth** for V1.

Reasons:
- V1 core invariants are strongly relational
- strong consistency and transactional relationship updates are central
- module truth boundaries already fit relational ownership well
- introducing document-truth paths now would widen:
  - operational complexity
  - replay/rebuild complexity
  - migration/testing burden
  - cross-store correctness risk

**Allowed V2+ hook**
A document database may be introduced later for:
- document-style derived outputs
- flexible reporting/read models
- large denormalized materializations
- archival or domain-specific read-serving artifacts

But only if:
- truth ownership remains explicit
- contracts remain stable
- rebuild/reconciliation is documented
- a new ADR approves the role

### 9) Data warehouse / analytical store is out of core V1 scope
A warehouse/columnar/analytics-oriented store is **not part of V1 truth**.

Warehouse-like outputs are future candidates for:
- trending analytics
- historical operational reporting
- compliance-style rollups
- long-window interaction analysis
- derived product intelligence

**Rule:** any future warehouse is downstream of truth and append-only inputs.  
It must not become:
- authorization truth
- publication truth
- identity truth
- live routing truth
- live media relationship truth

### 10) Reading remains a facade over upstream truth, not a separate V1 truth store
Reading may use:
- SQL queries
- bounded derived tables
- caches
- future read models

But Reading does not own publication truth, routing truth, or media truth.

**Rule:** Reading is a truth-composing facade, not a separate truth domain in V1.

---

## Store placement matrix (V1)

| Module | Truth store (V1) | Derived/caches | Notes |
|---|---|---|---|
| Content | SQL | optional read caches | lifecycle invariants + publication truth enforced in SQL |
| SEO | SQL (routing truth + metadata) | Redis routing cache | cache allowed; DB fallback required |
| Media | SQL (metadata + ordering + primary + relations) | optional CDN/cache/derivatives | blobs in object storage (optional/future) |
| Reading (public query facade) | upstream SQL truth composition + optional derived SQL views | optional response caching | must enforce visibility correctness from truth |
| Identity | SQL | Redis rate-limit (allowed) | self-state reads must be consistent |
| Authorization | SQL | optional permission cache (careful) | governance correctness first; fail closed if uncertain |
| Interaction | SQL (events/logs + aggregates) | Redis counters (optional) | eventual aggregates ok; truth/derived must stay explicit |
| Notifications | SQL delivery truth + Outbox | Redis dedupe (optional) | at-least-once + idempotent |
| Audit | SQL append-only evidence truth | optional derived reports only | investigation-ready truth |
| Batch/Reports | SQL bounded outputs in V1 | optional future warehouse | derived only; never live truth authority |

---

## Alternatives considered

### 1) MongoDB as primary truth store
- Pros: flexible schema, convenient document reads, denormalized aggregates.
- Cons: V1 needs strong invariants (slug uniqueness, governance, security flows, media relationships) and relational SQL patterns are a better fit.

### 2) Polyglot truth (SQL for some modules, document DB for others)
- Pros: “best tool per job”.
- Cons: adds operational complexity; cross-store invariants become harder; increases failure modes in V1; weakens consistency of module-boundary reasoning.

### 3) Redis as truth for hot paths
- Pros: performance.
- Cons: unacceptable risk for correctness and security; cache invalidation and durability are not suitable for truth.

### 4) Warehouse-first reporting/aggregate posture
- Pros: scalable analytics.
- Cons: premature for V1; introduces more moving parts before truth boundaries, replay policy, and read-model promotion rules are mature enough.

### 5) Reading-owned separate truth store in V1
- Pros: potentially faster public reads.
- Cons: risks hidden truth drift; makes visibility correctness harder; premature promotion of derived state to serving authority.

---

## Consequences

### Positive
- Clear source of truth for invariants (SQL)
- Simpler operations for V1 (one primary OLTP truth technology)
- Predictable replication strategy (Outbox + async consumers)
- Explicit truth ownership by module, even in shared DB deployment
- Easy to evolve: add projections/caches/warehouse/reporting layers without breaking truth
- Media, audit, notification, and interaction placement all align with truth-vs-derived posture

### Negative / Trade-offs
- Some read paths may require careful indexing to meet latency targets
- Redis caches require explicit invalidation/fallback rules
- SQL will carry both OLTP truth and some append-only/derived workloads in V1, so schema and indexing discipline matter
- Later migration to additional stores (search, document store, warehouse) must be planned and ADR-governed

---

## Implementation notes (V1)

- Use schema-per-module boundaries in SQL (ownership clarity).
- Enforce invariants with constraints and indexes:
  - SEO slug uniqueness `(Scope, Slug)` unique index
  - authorization assignment uniqueness `(UserId, RoleId)`, `(RoleId, PermissionId)`
  - media attachment uniqueness `(ArticleId, MediaId)` where policy requires
  - filtered uniqueness for primary media where needed
  - notification business dedupe keys where harmful duplicates must be prevented
  - audit canonical event identity uniqueness where required
- Treat Redis as derived:
  - cache-aside patterns with DB fallback
  - TTL + event-driven invalidation when feasible
- Treat SQL append-only logs as:
  - replayable where needed
  - retention-managed
  - subordinate to explicit module ownership
- Do not let derived SQL tables silently become live truth authority.
- Any future document DB or warehouse introduction requires:
  - explicit ownership rule
  - truth/derived classification
  - rebuild/reconciliation posture
  - promotion/cutover rules if it ever serves user traffic

---

## Follow-ups

- ADR-0013: Outbox & Delivery Semantics (at-least-once, retry/DLQ, dedupe)
- ADR-0014: Public Identifier Strategy (slug/public IDs vs internal keys)
- ADR-0015: Cache Policy & Invalidation (Redis)
- ADR-0016: Authorization Policy Model (policy naming, RBAC/ABAC boundaries)
- Future ADR candidate: search / index store placement
- Future ADR candidate: document-store placement for derived read models
- Future ADR candidate: warehouse / analytical store policy
- Future ADR candidate: read-model promotion rules when a derived serving source becomes primary