## 13) Transactions & Consistency (V1)

This section defines the transaction model and consistency rules for CommercialNews V1.

It answers five architecture-level questions:

* What must commit atomically?
* Where does a transaction start and stop?
* Which effects are synchronous truth changes vs asynchronous side effects?
* Which read behaviors require strong visibility vs allow eventual consistency?
* How must retries, idempotency, cache fallbacks, and derived-state lag preserve correctness?

This section is a **system-level policy**, not a database textbook and not a per-endpoint implementation guide.

### Related

* Constraints: `02-constraints.md`
* Modularity & ownership: `03-building-blocks-modularity.md`
* Runtime scenarios: `04-runtime-view-v1.md`
* Architecture style: `09-architecture-style.md`
* System data model: `10-system-data.md`
* Replication: `11-replication-v1.md`
* Partitioning: `12-partitioning-v1.md`
* Stream / derived-state policy: `18-stream-processing-and-derived-state-v1.md`
* Stream runtime shape: `19-stream-processing-runtime-v1.md`
* ADR-0012 (Data store placement)
* ADR-0013 (Outbox & delivery semantics)
* ADR-0015 (Cache policy & invalidation)
* ADR-0027 (Stream processing and derived-state policy)
* ADR-0028 (Consumer idempotency, replay, and rebuild policy)

---

### 13.1 Purpose

CommercialNews uses transactions to reduce the number of failure and concurrency cases that application code must reason about.

In V1, transactions are used to protect:

* truth changes inside the owning module
* lifecycle correctness for state transitions
* local atomicity between domain state and outbox intent
* read-your-writes on selected identity and governance flows
* safe degradation when async consumers, broker, cache, or projections are delayed
* explicit separation between committed truth and lagging derived state

CommercialNews does **not** attempt distributed atomic commit across:

* database + broker
* database + Redis
* database + email provider
* database + external systems

Instead, V1 uses **local truth transactions + outbox + idempotent async processing + replay/rebuild posture for derived systems**.

---

### 13.2 Core principles

#### 13.2.1 Truth changes are committed synchronously

A user-facing write succeeds only when the owning module’s source-of-truth state has been committed successfully.

Examples:

* article state change in Content
* account verification state in Identity
* role/permission change in Authorization

#### 13.2.2 Outbox is part of the same local transaction

Whenever a truth change must trigger async work, the module writes:

* the truth change
* the outbox record

…in the same DB transaction.

This is the standard V1 replication contract.

#### 13.2.3 Cross-module side effects are asynchronous

Audit persistence, email sending, aggregation, cache invalidation, and projection updates are post-commit side effects.

They must not determine the success of the core user flow.

#### 13.2.4 Shared DB does not imply shared transactional ownership

CommercialNews may run with a shared database in V1, but module ownership still defines the valid transactional boundary.

A transaction may touch multiple tables owned by the same module or its explicitly approved local replication artifacts (for example, Outbox).
It must not casually join business writes across module boundaries just because the tables are physically reachable.

#### 13.2.5 Transactions must be short

Transactions must:

* complete within a single request/handler execution
* avoid waiting for human input
* avoid external network calls
* avoid cross-process coordination inside the open transaction

This keeps correctness simpler and reduces lock/abort risk.

#### 13.2.6 Truth is primary; derived data may lag

Truth tables/stores define correctness.
Derived data may be stale, rebuilt, replayed, or temporarily unavailable.

Public correctness, governance correctness, and identity security state must never depend solely on lagging derived stores.

#### 13.2.7 Core success is truth commit, not derived convergence

A successful transaction means:

* truth committed
* outbox intent committed when async propagation is required

It does not mean:

* broker publish already happened
* consumers have already applied the event
* cache/projection/search state is already caught up
* side effects have already completed

---

### 13.3 Transaction boundary rules

#### 13.3.1 Allowed inside one local transaction

The following may be committed together when they belong to the same use case boundary:

* owner module truth row/document changes
* owner module lifecycle/history rows
* outbox record for async side effects
* local module metadata required for the state transition
* local durable dedupe/idempotency record, when policy requires it

Examples:

* Content: Article + lifecycle/history + OutboxMessage
* Identity: UserAccount change + verification/reset state + OutboxMessage
* Authorization: UserRole / RolePermission change + OutboxMessage

#### 13.3.2 Not allowed inside the same transaction

The following must not sit inside the core truth transaction:

* publish to broker
* send email
* invalidate/update Redis as a required success condition
* rebuild read models/projections
* call external HTTP/API services
* perform long-running aggregation work
* depend on a downstream consumer having already finished
* wait for a derived system to “confirm” convergence before returning success

#### 13.3.3 Cross-store atomic commit is out of scope

CommercialNews V1 does not use distributed transactions / 2PC to atomically commit across:

* SQL + RabbitMQ
* SQL + Redis
* SQL + external email provider
* multiple independent truth stores

The architecture relies on:

* local atomic commit
* outbox replay
* at-least-once delivery
* idempotent consumers
* safe fallbacks
* rebuild/reconciliation for important derived outputs

---

### 13.4 Standard V1 transaction pattern

For write flows with async side effects, the standard pattern is:

1. Validate input and command semantics
2. Enforce authorization (when required)
3. Load current truth state needed for the decision
4. Apply the truth change
5. Write OutboxMessage in the same transaction
6. Commit
7. Return success based on truth commit only
8. Let Worker/Consumers process side effects asynchronously

This pattern applies to:

* publish/unpublish
* register/verify/reset flows
* governance changes
* interaction ingestion/aggregation signals
* future projection/indexing hooks

#### 13.4.1 Prohibited alternative: ad hoc dual writes

CommercialNews forbids treating application code as the place where multiple independent systems are synchronously “kept in sync” by best effort.

Examples of prohibited patterns:

* DB write + broker publish inline as required success
* DB write + email send inline
* DB write + Redis update as mandatory completion
* DB write + search/projection update inline as authoritative completion

These patterns are failure-prone under partial failure and violate V1 truth/derived discipline.

---

### 13.5 Consistency model by category

CommercialNews intentionally uses different consistency strengths for different categories of work.

#### 13.5.1 Strong local consistency

Required at the truth boundary for:

* article lifecycle changes
* account credential/security state changes
* role/permission assignments
* token rotation/revocation state
* slug uniqueness in the owning truth store
* any state transition whose immediate result must be authoritative

#### 13.5.2 Eventual consistency

Accepted for:

* audit persistence
* notification delivery
* interaction aggregation/counters
* projection/read model updates
* cache invalidation and cache warming
* selected SEO/reactive side effects that do not own publication truth
* other derived serving artifacts that are repairable and truth-fallback-safe

#### 13.5.3 Read-your-writes required

The following flows must reflect the just-committed truth immediately after success:

* self-state after email verification
* self-state after password reset/change
* admin governance reads after role/permission changes
* article visibility checks on truth paths after publish/unpublish
* any security-sensitive state read that could mislead the user/admin immediately after success

These reads must:

* come from the primary truth store, or
* return the committed write result directly

They must not depend on Redis freshness.

#### 13.5.4 Safe stale reads allowed

The following may tolerate bounded lag:

* derived counters
* notification status views
* non-critical admin dashboards
* projection-backed public enrichments, provided truth fallback preserves correctness
* selected serving/read optimizations that are explicitly classified as derived-only

---

### 13.6 Concurrency policy

#### 13.6.1 Default stance for V1

CommercialNews V1 optimizes for:

* correctness at the truth boundary
* short transactions
* explicit database constraints
* targeted application-level concurrency control where needed

The architecture must not assume that a basic transaction alone automatically prevents all race conditions.

#### 13.6.2 Use constraints first for uniqueness/existence invariants

Where an invariant can be expressed as a database constraint, prefer that over application-only checks.

Examples:

* unique slug within scope
* unique email/username
* unique (UserId, RoleId)
* unique (RoleId, PermissionId)
* unique message keys / audit event IDs / email delivery IDs

Application-level “check then insert” is not sufficient by itself under concurrency.

#### 13.6.3 Use optimistic concurrency for stale edit workflows

For admin edit forms and other load-then-save workflows, the design should support explicit stale-write detection.

Typical examples:

* article editing
* SEO metadata editing
* profile editing
* configuration/document-style edits

The project may implement this using:

* version columns
* rowversion/timestamp
* compare-and-set semantics
* conflict responses at the application layer

#### 13.6.4 Use atomic DB operations for counters

Do not implement counters by naive application-side read-modify-write cycles when the database can express the update atomically.

Examples:

* increment view count
* increment retry count
* toggle/aggregate counters when a commutative operation exists

#### 13.6.5 Treat check-then-write invariants as high risk

Patterns like the following are concurrency-sensitive and must be reviewed carefully:

* `IF NOT EXISTS (...) THEN INSERT`
* `IF COUNT(...) >= N THEN UPDATE`
* `IF SUM(...) <= limit THEN INSERT`
* read state → decide → write based on that state

These are candidates for:

* strong DB constraints
* explicit locking
* stronger isolation
* redesign of the invariant boundary

#### 13.6.6 Stronger isolation is selective, not default everywhere

CommercialNews does not require the strongest isolation level for every write path.
However, for invariants spanning multiple rows or search predicates, stronger protection may be needed.

Examples of likely candidates:

* “last admin” style governance rules
* booking/slot overlap rules
* quota/balance-like aggregate guards
* other write-skew / phantom-prone rules

Those cases must be documented at module/use-case level.

---

### 13.7 Retry, idempotency, and replay rules

#### 13.7.1 Core rule

V1 async delivery is at-least-once end-to-end. Therefore:

* duplicates are normal
* retries are normal
* replay is normal
* consumers must be idempotent

#### 13.7.2 Outbox publish retries

If outbox publish fails:

* the truth change remains committed
* backlog grows
* publish retries continue according to policy

Core correctness does not depend on immediate broker publish.

#### 13.7.3 Consumer idempotency

Consumers must implement:

* message-level dedupe by MessageId / EventId
* business-level idempotency where duplicate side effects are harmful
* version-aware apply rules where stale or out-of-order events could corrupt derived state

Examples:

* audit append dedupe by event/message key
* email delivery dedupe by MessageId and/or business key
* aggregate/projection updates by version-aware logic, commutative logic, and/or processed-message tracking

#### 13.7.4 Retry-safe design

A retried operation must not create inconsistent duplicate business effects.

This applies to:

* worker handlers
* consumer handlers
* selected API commands where retries are plausible after timeouts or transient failures

#### 13.7.5 Retry does not imply re-run of external side effects inside the truth transaction

External side effects belong after commit. If retry is needed, it must be applied to:

* outbox publishing
* consumer processing
* explicit idempotent command handling

…not by stretching the original truth transaction across external calls.

#### 13.7.6 Important derived systems require replay/rebuild posture

If an important derived output can lag or be corrupted, the architecture must define at least one of:

* replay from retained operational history
* rebuild from truth
* reconciliation against authoritative source
* bounded recomputation

A derived system without replay/rebuild posture is too close to hidden truth.

---

### 13.8 Cache and visibility rules

#### 13.8.1 Redis is derived-only

Redis accelerates reads and workflows but is never the source of truth for:

* identity security state
* authorization decisions
* publication visibility correctness
* slug uniqueness truth

#### 13.8.2 Cache invalidation is post-commit

Write flow:

1. commit truth
2. emit outbox/event
3. invalidate/update cache asynchronously

TTL is a safety net, not the primary correctness mechanism.

#### 13.8.3 Public visibility is enforced by truth

Delayed cache invalidation or delayed SEO consumers must not re-expose drafts or unpublished content.

The read path must remain safe even when:

* cache is stale
* invalidation lags
* async SEO processing is delayed
* projections are behind

#### 13.8.4 Fallback over false confidence

When cache data is missing or suspected stale:

* fall back to truth store
* prefer safe “not found” over incorrect exposure
* never trust Redis alone for security/visibility-sensitive decisions

---

### 13.9 Module application notes (V1)

#### 13.9.1 Content

* CreateDraft, UpdateDraft, Publish, Unpublish commit at the Content truth boundary.
* Publish/unpublish writes the lifecycle change and outbox record atomically.
* Notification, audit persistence, and SEO reactions are asynchronous.
* Public visibility correctness is owned by Content truth, not by derived caches or lagging consumers.
* If downstream serving/projection artifacts drift, repair/rebuild must not redefine publication truth.

#### 13.9.2 Identity

* Register, verify, reset, and password change commit the identity/security state synchronously.
* Verification and reset emails are asynchronous side effects only.
* After successful verification/password change/reset, self-state reads must reflect the new truth immediately.
* Delivery retries and replay must not produce duplicate harmful sends.

#### 13.9.3 Authorization

* Role/permission changes commit synchronously at Authorization truth.
* Audit is downstream and must not block governance success.
* Governance reads immediately after change must observe current truth.
* Derived effective-permission views/caches must not outrank truth-sensitive governance reads.

#### 13.9.4 SEO

* SEO owns slug/meta/canonical policy data.
* Slug uniqueness must be enforced at the SEO truth boundary, not by app checks alone.
* Public routing may use cache-first lookup, but final visibility correctness must still be validated against truth.
* If derived serving artifacts are used, they remain derived and rebuildable.

#### 13.9.5 Interaction

* Interaction ingestion must not slow public reads.
* Aggregation and counters are eventual-consistency concerns by policy.
* Duplicate delivery and replay must be tolerated safely.
* If time-windowed analytics are introduced, event-time vs processing-time semantics must be explicit.

#### 13.9.6 Audit

* Audit ingestion is append-only and asynchronous by default.
* Audit lag is acceptable for core flow success, but not invisible; it must be observable and repairable.
* Sensitive actions must be representable by stable audit event IDs / message keys.

#### 13.9.7 Notifications

* Email delivery is never part of the core truth transaction.
* Delivery must be idempotent and retry-safe.
* Failures are operationally important but must not roll back the originating business success.
* Durable delivery records or equivalent dedupe posture are required where duplicate sends are harmful.

#### 13.9.8 Reading Experience

* Public reads may use cache and query-facade optimizations.
* Publication visibility must be correct even during cache lag or projection lag.
* Reading flows must degrade safely when non-critical derived systems are behind.
* Safe fallback to truth is preferred over stale derived confidence.

---

### 13.10 What transactions do not guarantee in CommercialNews

A successful local transaction means:

* the truth change committed
* and the outbox intent committed if required

It does **not** mean that:

* the broker publish has already happened
* audit has already been persisted
* email has already been sent
* caches are already invalidated
* projections are already caught up
* all consumers have already applied the event
* a cross-system distributed workflow finished atomically

CommercialNews correctness relies on:

* strong local truth transactions
* eventual async completion
* idempotency
* observability
* safe fallback behavior
* replay/rebuild/reconciliation for important derived systems

---

### 13.11 Failure semantics (V1)

#### 13.11.1 If truth commit fails

The user-facing operation fails.

#### 13.11.2 If truth commit succeeds but async propagation fails

The user-facing operation still succeeds. Recovery happens through:

* outbox retry
* consumer retry
* DLQ/remediation flow
* replay/reconciliation if needed

#### 13.11.3 If cache is stale or unavailable

Core flows must degrade safely to truth-store reads where required.

#### 13.11.4 If a non-critical consumer is down

Core flows remain available.
Operational signals must show backlog, lag, and failure rate.

#### 13.11.5 If a derived store is missing or corrupted

Truth remains authoritative.
Recovery happens through:

* replay
* rebuild
* reconciliation
* bounded recomputation

The system must not silently treat the broken derived state as authoritative.

---

### 13.12 Evolution notes (V2+)

As CommercialNews evolves toward richer projections and read models:

* the truth transaction boundary remains stable
* the eventual-consistency surface may grow
* additional checkpoints/reconciliation may be introduced
* stronger isolation may be applied selectively to multi-row invariants
* richer stream-style joins, event-time windows, and derived-state maintenance may appear
* distributed commit remains out of scope unless introduced by explicit ADR

**The V1 rule remains unchanged:** truth correctness first, side effects second, observability always.