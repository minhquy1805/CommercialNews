# 14 — Distributed Systems Assumptions (V1)

This section defines the **system model assumptions** for CommercialNews V1.

Its purpose is to make the architecture explicit about:
- what kind of distributed environment CommercialNews assumes
- what is considered normal vs exceptional in production
- what the system can and cannot safely infer from time, network, and local node state
- which design consequences follow from those assumptions

This section turns the DDIA Chapter 8 mindset into **architecture rules**, not book notes.

> Related:
> - `02-constraints.md`
> - `03-building-blocks-modularity.md`
> - `04-runtime-view-v1.md`
> - `05-quality-requirements.md`
> - `08-components.md`
> - `09-architecture-style.md`
> - `11-replication-v1.md`
> - `12-partitioning-v1.md`
> - `13-transactions-and-consistency-v1.md`
> - `18-stream-processing-and-derived-state-v1.md`
> - `19-stream-processing-runtime-v1.md`
> - `../../decisions/adr-0013-outbox-and-delivery-semantics-v1.md`
> - `../../decisions/adr-0015-cache-policy-and-invalidation-redis-v1.md`
> - `../../decisions/adr-0018-transaction-boundaries-and-consistency-model-v1.md`
> - `../../decisions/adr-0021-clock-time-and-ordering-policy-v1.md`
> - `../../decisions/adr-0022-versioning-and-fencing-strategy-v1.md`
> - `../../decisions/adr-0027-stream-processing-and-derived-state-policy-v1.md`
> - `../../decisions/adr-0028-consumer-idempotency-replay-and-rebuild-policy-v1.md`

---

## 14.1 Purpose and scope

CommercialNews V1 is not a single-machine program in the architectural sense.

Even though V1 may run as a domain-partitioned modular monolith with a small number of deployable components, it already operates as a **distributed system** because it includes:

- multiple deployable components (Public API + Background Worker)
- asynchronous side effects through Outbox → Broker → Consumers
- truth stores plus lagging derived stores
- caches and broker-driven workflows
- stream-style derived-state maintenance and replay/rebuild workflows
- failure modes where one part of the system can be degraded while others continue serving traffic

This section defines the assumptions under which CommercialNews V1 is designed to remain:
- correct at truth boundaries
- available on critical read paths
- observable and recoverable during partial failures

It is a **system-wide policy** that informs:
- runtime behavior
- timeout/retry posture
- cache and replication rules
- consistency expectations
- module-level idempotency and fallback design
- stream-processing and rebuild/reconciliation posture

---

## 14.2 Why CommercialNews must reason as a distributed system

CommercialNews V1 has all of the failure characteristics that make distributed reasoning necessary:

- public reads are **read-heavy and bursty**
- write flows trigger **async side effects**
- derived state may lag behind truth
- network calls and broker delivery are not instantaneous or perfectly reliable
- workers and API hosts can restart, pause, or be temporarily isolated
- caches can be stale, missing, or evicted
- observability must reconstruct end-to-end behavior across sync and async boundaries
- stream-style consumers may process retries, duplicates, out-of-order deliveries, and replayed events
- bounded rebuild/reconciliation workflows may run while live traffic continues

Therefore, CommercialNews must not assume:
- immediate global visibility after every write
- that timeout proves failure
- that cross-node timestamps prove ordering
- that a local node’s belief about its own state is sufficient authority
- that lagging derived state is the same as truth
- that late events, replayed events, or duplicated events are exceptional

---

## 14.3 Timing model

### 14.3.1 Chosen timing model: partially synchronous

CommercialNews V1 assumes a **partially synchronous** environment.

This means:
- most of the time, network and process behavior falls within expected operating envelopes
- occasionally, latency, queueing, retries, pauses, backlog growth, and clock error may become much larger than normal for a finite period
- the system must continue reasoning safely even when timing temporarily becomes unpredictable

This matches the practical reality of:
- cloud/VM/container execution
- shared infrastructure
- bursty traffic
- background consumers
- cache and broker delays
- replay/rebuild workflows competing with live work

### 14.3.2 What is explicitly not assumed

CommercialNews V1 does **not** assume:
- bounded network delay at all times
- perfectly synchronized clocks across nodes
- zero or negligible process pauses
- immediate broker publish after truth commit
- immediate cache invalidation after a write
- immediate projection catch-up after a domain event
- hard real-time guarantees for server-side execution
- that a stream consumer observes events exactly when they occurred

### 14.3.3 Architectural consequence

Timing information is useful for:
- timeout heuristics
- retry scheduling
- latency measurement
- operational diagnosis
- backlog and freshness monitoring
- event-time vs processing-time analysis where module policy requires it

Timing information is **not** sufficient authority for:
- proving remote failure
- proving global ordering
- proving causal precedence across nodes
- proving that async side effects have already completed
- proving that the first event observed is the logically newest event

---

## 14.4 Node and process fault model

### 14.4.1 Chosen node/process model: crash-recovery

CommercialNews V1 assumes **crash-recovery** faults for internal components.

This means:
- API hosts, workers, or processes may crash or restart
- in-memory state may be lost
- durable truth in primary stores is expected to survive restart
- async work may resume after recovery through retry/replay mechanisms

This model is more realistic for V1 than crash-stop, because:
- pods/processes are expected to restart
- workers may recover and continue processing
- durable stores remain the basis for reconstruction and replay

### 14.4.2 Process pauses are normal

CommercialNews explicitly assumes that processes or threads may pause for non-trivial periods due to:
- runtime pauses (for example GC effects)
- CPU scheduling and noisy-neighbor effects
- VM or host-level suspend/resume events
- I/O stalls
- memory pressure or paging
- operational restarts or infrastructure transitions

Therefore, the architecture must not rely on assumptions such as:
- “this code path will definitely finish before the world changes”
- “if I checked validity just before acting, I must still be valid now”
- “a live process is continuously making progress”

### 14.4.3 Limping and degraded nodes are possible

A node may remain technically alive while being operationally unreliable:
- responding slowly
- processing intermittently
- timing out on dependencies
- lagging behind current state
- stuck in partial degradation
- draining backlog too slowly to preserve freshness expectations

CommercialNews therefore distinguishes:
- process liveness
- readiness to serve traffic
- dependency health
- business-flow health
- truth-path health
- derived-path freshness/lag health

Health checks alone are not treated as proof that end-to-end flows are healthy.

---

## 14.5 Internal trust model

### 14.5.1 Internal nodes are assumed faulty but honest

CommercialNews V1 assumes internal services and workers are:
- allowed to be slow
- allowed to retry
- allowed to restart
- allowed to process duplicate deliveries
- allowed to be stale or partitioned temporarily
- allowed to replay or rebuild derived state after recovery

But they are **not** assumed Byzantine by default.

In other words:
- internal nodes may fail, lag, or act on incomplete knowledge
- internal nodes are not designed under the assumption of arbitrary malicious protocol-breaking behavior

This is the practical V1 trust model for:
- Public API
- Background Worker
- SQL truth stores
- Redis
- RabbitMQ
- internal consumers and publishers

### 14.5.2 Byzantine tolerance is out of scope for internal services in V1

CommercialNews V1 does not attempt full Byzantine fault tolerance for internal components.

The architecture instead focuses on:
- crash-recovery resilience
- idempotent async processing
- retry safety
- version-aware stale-write protection
- observability and replay
- validation against malformed or inconsistent data where appropriate

---

## 14.6 External trust boundary

### 14.6.1 Clients may be malicious or malformed

CommercialNews assumes all external client input is untrusted unless verified server-side.

This includes:
- request payloads
- headers
- timestamps
- claims
- state assertions
- retry behavior
- duplicated requests
- malformed or abusive traffic

Therefore:
- client-provided time must not become authority for correctness/security decisions
- authorization must be enforced from server truth
- all public-facing inputs require validation, bounding, and safe logging

### 14.6.2 External providers may be ambiguous, slow, or unavailable

CommercialNews also assumes external providers and infrastructure dependencies may behave ambiguously:
- email providers may succeed without an immediately reliable acknowledgment
- external calls may timeout without proving failure
- network paths may delay or lose responses
- third-party systems may recover later than core truth paths

Therefore, external delivery success is not part of the originating truth transaction.

---

## 14.7 Knowledge limits and truth model

### 14.7.1 No node has complete knowledge

No single node in CommercialNews can directly know the full current truth of the system at all times.

A node observes:
- local state
- messages received
- messages not yet received
- dependency responses
- timeouts
- cached or lagging views
- replay/rebuild state that may be incomplete or in progress

Therefore, a node must not confuse:
- local belief
with
- system truth

### 14.7.2 Source of truth is explicit per module

Each module must define one authoritative source of truth for its business rules.

Examples at V1 system level:
- Content truth owns publication state and lifecycle correctness
- Identity truth owns account/security state
- Authorization truth owns role/permission assignment
- SEO truth owns slug uniqueness/routing truth where policy requires it
- Media truth owns attachment/ordering/primary-media rules

### 14.7.3 Derived state may lag

Derived state is expected to lag truth, including:
- Redis caches
- async projections
- interaction aggregates
- notification delivery views
- audit persistence views
- read-side enrichments
- search/serving artifacts
- batch-generated summaries or rebuild candidates not yet cut over

Derived stores are acceptable only when they are:
- rebuildable
- observable
- protected by fallback behavior
- not treated as sole authority for security- or visibility-critical correctness

### 14.7.4 Correctness must not depend on derived freshness unless explicitly stated

CommercialNews V1 follows this system-wide rule:

- truth defines correctness
- derived state improves latency, scalability, or operability
- if a derived view is stale or unavailable, the system must prefer:
  - truth fallback, or
  - safe degradation
over false confidence

This is especially important for:
- published vs unpublished visibility
- identity and governance self-state
- slug routing safety
- security-sensitive reads

---

## 14.8 Failure interpretation rules

### 14.8.1 Timeout is an operational heuristic, not proof of failure

A timeout means:
- the caller stopped waiting

It does **not** prove:
- the remote side did nothing
- the request failed before any state change
- the broker did not receive a publish attempt
- a side effect did not happen
- a consumer did not partially apply its effect

Therefore:
- retries must be designed as potentially duplicate
- async handlers must be idempotent
- write flows must expose authoritative truth state for reconciliation when outcomes are ambiguous

### 14.8.2 Lack of response is not a complete explanation

When no response is observed, possible explanations include:
- request loss
- response loss
- network delay
- remote crash
- remote pause
- downstream overload
- successful completion with failed acknowledgment

CommercialNews must therefore avoid reasoning such as:
- “no response means definitely failed”
- “timeout means safe to retry without side effects”
- “health endpoint green means business flow green”

### 14.8.3 Wall-clock time is not authority for causality

Cross-node timestamps are useful for:
- human investigation
- audit and reporting
- correlation support
- lag/freshness analysis
- event-time analysis where module policy explicitly uses it

They are not sufficient authority for:
- stale-write resolution
- cross-node causal ordering
- deciding which update is logically newer under concurrency
- deciding whether a late-arriving event should override a newer applied version

Ordering-sensitive paths must use:
- explicit version numbers
- sequence or revision counters
- optimistic concurrency markers
- fencing/generation-style tokens where appropriate

---

## 14.9 Truth transaction and async effect assumptions

### 14.9.1 Local truth transaction is the correctness boundary

CommercialNews assumes the primary correctness boundary is the local truth transaction of the owning module.

A successful write means:
- truth committed
- and, when required, the outbox intent committed in the same transaction

It does **not** mean:
- broker publish completed
- notification was delivered
- audit was already persisted
- cache invalidation already propagated
- projections already caught up
- all consumers have already applied the event
- all stream-derived views are fresh

### 14.9.2 Async side effects are at-least-once by design

Outbox → Broker → Consumers is the standard V1 replication path.

This means:
- duplicates are normal
- retries are normal
- out-of-order delivery is possible where ordering is not explicitly enforced
- replay is normal during recovery
- consumers must be idempotent
- ordering-sensitive consumers must be version-aware

### 14.9.3 Resource-side protection is preferred over caller self-confidence

Because nodes may pause, lag, or wake up with stale beliefs, systems must not rely solely on a caller’s local belief that it still has authority.

Where stale actor risk exists, V1 should prefer:
- version checks
- optimistic concurrency
- compare-and-set semantics
- monotonic generation/fencing-style rules
- rejecting stale writes at the resource boundary

### 14.9.4 Derived-state recovery is an expected runtime behavior

CommercialNews assumes some derived outputs will eventually need:
- replay
- rebuild
- reconciliation
- bounded recomputation

These recovery actions are normal architectural behaviors, not exceptional proof that the original model failed.

They must remain:
- bounded
- observable
- rerun-safe
- subordinate to truth ownership

---

## 14.10 Cache and read-path assumptions

### 14.10.1 Redis is derived-only

Redis is assumed to be:
- useful
- fast
- operationally valuable
- disposable and rebuildable

Redis is not assumed to be:
- an authoritative store for security state
- an authoritative store for visibility correctness
- an authoritative store for uniqueness truth

### 14.10.2 Read paths must degrade safely

When cache or derived reads are stale or unavailable, CommercialNews prefers:
- truth fallback
- or safe negative responses
over incorrect exposure

Examples:
- stale slug routing must not leak unpublished content
- stale cache must not overrule identity or governance truth
- public reads may degrade in latency but must preserve visibility correctness

### 14.10.3 Read-your-writes is selective and explicit

CommercialNews V1 explicitly requires truth-backed read-your-writes behavior for:
- Identity self-state after verify/reset/password change
- Authorization governance reads after changes
- truth-sensitive publication visibility checks after publish/unpublish
- other security-sensitive or admin-sensitive immediate follow-up reads

These flows must not depend on lagging cache or replica freshness.

---

## 14.11 Stream-processing and time assumptions

### 14.11.1 Event time and processing time may differ materially

CommercialNews assumes that:
- an event may occur at one time
- be published later
- be delivered later still
- and be processed even later due to backlog, retry, replay, or recovery

Therefore, stream-derived analytics and detection logic must not assume:
- observed arrival time equals business occurrence time
- processing order proves real-world temporal order

### 14.11.2 Late events are normal, not exceptional

CommercialNews assumes some event families may observe:
- delayed arrival
- replay after restart
- retry after timeout
- backlog drain after outage

Where windowed analytics or pattern detection matters, modules must define:
- whether event time or processing time is authoritative
- how late arrivals are handled
- what correctness vs freshness trade-off is accepted

### 14.11.3 Joins are time-dependent as well as key-dependent

CommercialNews assumes that some joins in stream-style processing are sensitive not only to key matching, but also to:
- version
- effective time
- freshness of reference state
- replay ordering

Therefore, important join-based pipelines must define whether they join against:
- current state
- event-time state
- latest available derived state

---

## 14.12 Operational consequences of the system model

The assumptions in this section imply the following non-negotiable V1 consequences.

### 14.12.1 Idempotency is a required design property
It is not an optimization.
It is required because:
- retries are normal
- duplicates are normal
- replay is normal
- timeout outcomes are ambiguous
- consumers are at-least-once

### 14.12.2 Observability must distinguish truth-path health from derived-path lag
CommercialNews must separately observe:
- truth write success/failure
- outbox backlog and publish delay
- consumer failure/lag/retry rate
- fallback rates from cache/projections to truth
- replay/rebuild backlog and age
- business-flow latency, not just component liveness

### 14.12.3 Deployment and config rollouts remain correlated-failure risks
Replication and modularity do not protect against:
- the same bad deployment on every node
- the same bad config applied everywhere
- the same incompatible contract rolled out system-wide

Therefore:
- rollout discipline matters
- compatibility matters
- correlated failure remains a real architectural risk

### 14.12.4 The system model supports reasoning, not denial of reality
This section provides a useful abstraction for reasoning about correctness and operations.

It does not claim that reality is perfectly captured by the model.
Implementation bugs, infrastructure faults, storage corruption, or operational mistakes may still violate assumptions.

Therefore CommercialNews relies on both:
- design-time architectural rules
- and runtime operational validation through testing, monitoring, alerting, replay, rebuild, reconciliation, and recovery drills

---

## 14.13 What CommercialNews V1 explicitly assumes

CommercialNews V1 explicitly assumes:

- a **partially synchronous** environment
- **crash-recovery** faults for internal nodes/processes
- **faulty but honest** internal services
- **malicious or malformed** external clients
- explicit **source-of-truth ownership** by module
- **derived state may lag**
- **timeout is not proof of failure**
- **wall-clock time is not causality**
- **process pauses are normal**
- **local truth transaction + outbox** is the core correctness boundary
- **at-least-once async delivery** for side effects and derived-state maintenance
- **idempotent consumers** are mandatory
- **late events and replay are normal runtime realities**
- **fallback to truth** is preferable to stale confidence

---

## 14.14 What CommercialNews V1 explicitly does not assume

CommercialNews V1 does **not** assume:

- global bounded latency
- perfect clock synchronization
- exact knowledge of remote state after timeout
- immediate async completion after request success
- exactly-once delivery across DB + broker + cache + provider
- hard real-time scheduling guarantees
- Byzantine-fault-tolerant internal infrastructure
- cache freshness as a correctness boundary
- broker retention as universal permanent replay history
- wall-clock timestamps as sole authority for conflict resolution

---

## 14.15 How modules should apply this section

Every module-level design should interpret this section through six questions:

1. **What is the authoritative truth boundary for this module?**
2. **Which derived views may lag, and what is the fallback behavior?**
3. **Which operations must be idempotent or version-aware under retry, duplicate delivery, and replay?**
4. **Which reads require truth-backed read-your-writes or correctness-first fallback?**
5. **Does any stream/window/join logic depend on event time rather than processing time?**
6. **What is the replay/rebuild/reconciliation posture for important derived outputs?**

Module docs should then reflect these assumptions in:
- `03-runtime-flows.md`
- `06-idempotency-consistency.md`
- `07-observability-slos.md`
- `08-dependencies-and-ownership.md`

---

## 14.16 Summary

CommercialNews V1 is designed for a world in which:
- some parts of the system may be slow while others continue
- messages may be delayed or duplicated
- replay and rebuild may be needed after failure or lag
- clocks may be close but not exact
- nodes may restart or pause
- caches may be stale
- local observations may be incomplete

The architecture therefore treats:
- truth as explicit and owned
- derived state as laggable
- timeout as ambiguous
- retries and replay as duplicating
- versioning and idempotency as normal
- fallback and observability as first-class design concerns
- stream-time semantics as explicit when they affect correctness

This is the system model under which CommercialNews V1 claims correctness and resilience.