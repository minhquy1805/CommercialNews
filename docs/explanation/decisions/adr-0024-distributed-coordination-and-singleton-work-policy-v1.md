# ADR-0024 — Distributed Coordination and Singleton Work Policy (V1)

**Status:** Accepted  
**Date:** 2026-03-09  
**Decision owners:** Architecture / Platform  
**Scope:** System-wide (singleton work, ownership transfer, leases, fencing, future coordination-service posture)  
**Related:**
- `../architecture/arc42/04-runtime-view-v1.md`
- `../architecture/arc42/11-replication-v1.md`
- `../architecture/arc42/13-transactions-and-consistency-v1.md`
- `../architecture/arc42/14-distributed-systems-assumptions-v1.md`
- `../architecture/arc42/15-consistency-ordering-and-consensus-v1.md`
- ADR-0013 (Outbox & delivery semantics)
- ADR-0018 (Transaction boundaries & consistency model)
- ADR-0019 (System model and fault assumptions)
- ADR-0020 (Timeout, retry, and failure detection policy)
- ADR-0021 (Clock, time, and ordering policy)
- ADR-0022 (Versioning and fencing strategy)
- ADR-0023 (Consistency, ordering, and consensus boundaries)

---

## Context

CommercialNews V1 is intentionally designed to minimize coordination-heavy behavior.

Most V1 workflows can and should be made correct through:

- local truth transactions
- DB-enforced uniqueness/invariants
- outbox-backed async propagation
- idempotent consumers
- per-aggregate ordering
- explicit versioning and stale-write rejection
- safe truth fallback under lag or uncertainty

However, some workflows naturally create pressure toward distributed coordination, such as:

- “only one worker should run this task”
- “only one instance should own this resource right now”
- “this maintenance or scheduling action must have a current owner”
- “if ownership transfers, a former owner must stop affecting truth”
- “a stale process waking up later must not continue acting as if it still owns work”

Without one explicit system-wide policy, the architecture risks drift toward unsafe patterns such as:

- local in-memory `isLeader` flags
- startup-order ownership (“first one wins”)
- naive Redis-key-based locks without fencing/resource-side validation
- timeout interpreted as proof that a former owner is dead
- singleton work that depends on caller self-confidence rather than authoritative rejection
- premature introduction of a coordination service for problems that could have been solved more simply

CommercialNews needs one accepted decision that states:

- when singleton or ownership semantics are justified
- how V1 should avoid unnecessary coordination
- what correctness rules apply if ownership can transfer
- when fencing/generation semantics are mandatory
- when a real coordination service would be justified in the future
- which naive patterns are explicitly disallowed

---

## Decision

### 1) V1 minimizes distributed coordination by design

CommercialNews V1 does **not** assume that every important workflow needs a distributed leader, lock, or singleton owner.

The default posture is:

- avoid global singleton semantics unless business correctness truly requires them
- prefer local truth decisions over cluster-wide coordination
- prefer per-aggregate or partitioned ownership over global ownership
- prefer idempotency and replay safety over exclusive execution when possible

This means V1 should first attempt to solve a problem using:

- DB constraints
- local transaction boundaries
- outbox + replay
- dedupe/idempotency
- version-aware stale-write rejection
- partitioned work assignment
- explicit state-machine transitions

before introducing distributed ownership or lease semantics.

---

### 2) Singleton work is not assumed safe unless its ownership model is explicit

Any workflow that claims:

- “only one worker may do this”
- “only one instance may own this task”
- “this process is the current leader”
- “this node is the active scheduler”
- “this actor exclusively owns a mutable resource”

must be treated as a **coordination-sensitive** workflow.

Such a workflow is not considered architecture-complete unless it explicitly defines:

- who grants ownership
- how ownership is represented
- how ownership transfer occurs
- how stale owners are rejected
- what happens when ownership confidence is lost
- whether correctness depends on exclusivity or only efficiency does

Informal singleton assumptions are non-compliant.

---

### 3) V1 prefers designs that avoid exclusive ownership when correctness allows it

Before introducing distributed singleton coordination, CommercialNews V1 must prefer one or more of the following design strategies where applicable:

#### A) Idempotent multi-worker processing
Allow more than one worker to attempt work if duplicate execution is harmless or rejectable.

#### B) Per-aggregate or per-partition work ownership
Scope ownership to one key, stream, aggregate, or partition instead of the whole system.

#### C) DB-enforced winner selection
Let the authoritative truth store choose the winner through:
- unique constraints
- compare-and-set
- expected-version write semantics
- generation checks

#### D) Replay-safe state-machine transitions
Allow duplicate attempts, but make invalid or stale transitions rejectable at the truth/resource boundary.

#### E) Safe at-least-once handling
Prefer observable replay and dedupe over brittle “exactly one runner” assumptions.

This policy exists because the safest coordination problem is often the one the architecture no longer needs to solve.

---

### 4) If ownership can transfer, fencing/generation semantics are mandatory

Whenever ownership may move from one actor to another, CommercialNews V1 requires:

- a monotonic generation / fencing token
- authoritative resource-side validation of that token
- rejection of operations using an older generation than the latest accepted one

This rule applies to future workflows such as:

- singleton schedulers
- lane-owned or partition-owned workers
- maintenance or rebuild ownership
- one-current-owner processing rights
- any workflow where an older paused actor may wake up later and continue acting incorrectly

The system must not rely on:

- the caller’s belief that it still owns the work
- timeout alone as proof that a previous owner is gone
- “I acquired the lock earlier” without resource-side freshness validation

This follows ADR-0022 and is mandatory whenever stale-owner risk exists.

---

### 5) Resource-side validation is required for correctness-critical ownership actions

CommercialNews V1 explicitly prefers **resource-side rejection** over caller self-belief.

If a workflow uses ownership/lease/generation semantics, then correctness-critical writes or transitions under that ownership must be validated at the authoritative boundary using one or more of:

- generation/fencing token comparison
- expected-version compare-and-set
- authoritative ownership record check
- current-state transition legality check

This means:

- the caller does not get to declare itself current owner unilaterally
- a stale actor must be rejectable even if it wakes up and continues sending commands
- ownership confidence is not enough; authority must be validated where state changes are accepted

---

### 6) Timeout and liveness signals do not by themselves transfer authority

CommercialNews V1 reaffirms ADR-0020 and ADR-0019:

- timeout is ambiguity, not proof
- process pauses are normal
- local belief is not authority
- a node may be slow, paused, partitioned, or unhealthy without the system knowing the absolute truth immediately

Therefore the following are disallowed as sole authority for ownership transfer:

- one timeout
- a missing heartbeat interpreted without authoritative policy
- local clock-based expiry without authoritative validation
- a health endpoint becoming red once
- a worker deciding on its own that the old owner must be dead

Operational suspicion may trigger attempts to transfer or reassign work.
It must not by itself justify correctness-critical stale-owner writes.

---

### 7) V1 baseline does not require a dedicated coordination service

CommercialNews V1 does **not** require ZooKeeper/etcd/Consul-style coordination service as a baseline dependency.

Reason:

- V1 architecture can keep most correctness-critical decisions inside local truth boundaries
- many workflows can be made safe without cluster-wide coordination
- premature coordination infrastructure adds cost, operational overhead, and design pressure without enough business value

This means:

- V1 does not introduce a coordination service “for completeness”
- V1 does not add a generalized cluster membership subsystem unless a real use case justifies it
- V1 does not build app logic that assumes such a service already exists

---

### 8) Future coordination service use is allowed only for explicit, justified cases

If a future version of CommercialNews truly requires strong coordination, an explicit ADR must justify introducing a proven coordination system.

Examples of potentially valid future use cases:

- strict singleton scheduling with automatic failover
- worker-group leadership where correctness depends on one current owner
- partition assignment with rebalance semantics
- membership management for a stateful processing cluster
- lease/lock service for coordination-sensitive workflows that cannot be reduced to DB truth decisions
- ownership semantics requiring quorum-backed current-authority guarantees

If introduced, the coordination service must be:

- a proven external or infrastructure-grade system
- explicit about quorum/majority assumptions
- explicit about membership source of authority
- explicit about fencing/generation strategy
- explicit about safe behavior under loss of coordination confidence

Homemade coordination logic is not acceptable.

---

### 9) Naive lock/leader patterns are explicitly disallowed

CommercialNews V1 explicitly disallows relying on any of the following as a correctness-critical coordination mechanism:

- local in-memory `isLeader` or `isOwner` flags
- “first process to start wins”
- naive Redis lock without fencing/resource-side validation
- wall-clock timestamp ordering as freshness proof for ownership
- startup sequence or deployment order as ownership rule
- caller-only lease checks with no authoritative stale-owner rejection
- timeout-only failover logic that permits stale writers

These patterns may appear to work in light testing but are not safe under:

- process pause
- restart
- delayed network
- duplicate execution
- retry storms
- crash-recovery
- partial partitions
- stale local belief

---

### 10) Coordination loss must fail safe

For any current or future coordination-sensitive workflow, CommercialNews adopts the rule:

- if current ownership or authority cannot be established safely, the system must prefer no progress over unsafe dual ownership

This means future coordination-aware workflows must prefer:

- ownership rejection
- explicit retry later
- safe blocking
- degraded mode
- revalidation at truth/resource boundary

over:

- two actors writing as if both are current
- stale owner continuing after transfer
- silent duplicate finalization
- “best effort” writes under ambiguous ownership

This follows the system-wide posture:
- temporary degraded availability is acceptable
- silent corruption is not

---

## Decision summary

CommercialNews V1 adopts the following distributed coordination and singleton-work policy:

- minimize distributed coordination by design
- do not assume singleton work is safe unless ownership is explicitly modeled
- prefer idempotency, partitioning, DB-enforced winners, and replay-safe workflows before introducing coordination
- require fencing/generation semantics when ownership can transfer
- require resource-side validation for correctness-critical ownership actions
- do not treat timeout/liveness suspicion as sufficient authority transfer
- do not require a dedicated coordination service in V1 baseline
- allow future coordination service introduction only by explicit ADR and only for justified cases
- explicitly disallow naive lock/leader patterns without authoritative stale-owner protection
- prefer safe non-progress over unsafe dual ownership

---

## Consequences

### Positive

- Prevents accidental drift into unsafe homegrown leader/lock patterns
- Keeps V1 simpler and aligned with actual business needs
- Encourages designs that reduce coordination demand instead of amplifying it
- Aligns singleton/ownership safety with versioning and fencing strategy
- Protects the system against stale-owner behavior after pause/restart
- Creates a clear bar for when introducing etcd/ZooKeeper-style infrastructure is actually justified

### Negative / Trade-offs

- Some workflows that might look simpler with a naive “one owner” shortcut must instead be redesigned around idempotency or partitioning
- Teams must think more explicitly about ownership and stale-actor rejection
- Future truly coordination-heavy workflows may require additional infrastructure and ADR work
- Some operational tasks may initially be manual or less automated until there is enough justification for a coordination subsystem

---

## Alternatives considered

### 1) Introduce a coordination service in V1 by default
- Pros: future-ready in theory.
- Cons: premature complexity; weak justification for current V1 needs; risk of using the tool for problems that do not need it.

Rejected.

### 2) Use naive app-level singleton rules
- Pros: fast to implement.
- Cons: unsafe under pause, retry, partition, and crash-recovery; invites stale-owner writes and split-brain behavior.

Rejected.

### 3) Solve singleton needs with timeout-only lease semantics
- Pros: superficially simple.
- Cons: unsafe without fencing/resource-side validation; treats ambiguity as authority.

Rejected.

### 4) Require exclusive ownership for all important background work
- Pros: simple mental model.
- Cons: unnecessary coordination load; prevents safer idempotent/partitioned designs; poor fit for many V1 async workflows.

Rejected.

### 5) Ignore ownership transfer and rely on operator discipline only
- Pros: fewer moving parts.
- Cons: fragile under automation, restart, scaling, and future growth; unsafe once work becomes multi-instance.

Rejected.

---

## Implementation notes (V1)

- `15-consistency-ordering-and-consensus-v1.md` is the architecture-level narrative reference for this ADR.
- Module docs and runtime designs must explicitly state whether a workflow:
  - truly requires singleton/ownership semantics
  - can instead be made idempotent or partitioned
  - needs generation/fencing validation
- Any future singleton-style job design must document:
  - ownership record location
  - generation/fencing field
  - resource-side validation point
  - behavior when ownership confidence is lost
- Reviews must reject designs that:
  - rely on local-memory leadership
  - rely on timestamp freshness for ownership
  - rely on timeout alone for authority transfer
  - claim exclusivity without authoritative stale-owner rejection

---

## Recommended application by area (V1)

### Background Workers
- Prefer idempotent work and replay-safe handlers.
- Prefer per-message, per-aggregate, or per-partition correctness over global worker leadership.
- Only introduce exclusive owner semantics when correctness truly depends on it.

### Scheduled / Maintenance Tasks
- Prefer tasks that are safe if retried or re-entered.
- If strict singleton behavior becomes necessary, require generation/fencing and explicit authoritative ownership.

### Projection / Rebuild Work
- Prefer version-aware apply/rebuild logic.
- Do not assume “one rebuild worker at a time” is safe unless ownership transfer is explicitly protected.

### Media / Processing Pipelines
- Prefer state-machine-driven processing and idempotent retries.
- Do not rely on naive worker exclusivity to guarantee correctness.

### Future Clustered Services
- If a service truly needs leader election, membership, or lease-based ownership, that requirement must trigger a separate infrastructure ADR rather than local improvisation.

---

## Operational guidance (V1)

### Recommended signals
For any coordination-sensitive workflow introduced in V1 or later, observe at minimum:

- ownership-generation mismatch count
- stale-owner rejection count
- duplicate execution detection count
- retry-after-ownership-loss count
- work reassignment count
- explicit degraded-mode / no-owner intervals

### Investigation posture
When a singleton/ownership incident occurs:

- check whether the workflow actually required strict singleton semantics
- check whether generation/fencing was present
- verify that the authoritative resource rejected stale-owner actions
- do not rely on timestamp order alone
- verify whether duplicate execution was harmless/idempotent or correctness-breaking

---

## Follow-ups

- Update `docs/explanation/decisions/README.md` to include ADR-0024
- Update `docs/explanation/architecture/arc42/00-index.md` to include section 15 if not already done
- Update module docs where ownership-sensitive workflows exist or may be introduced:
  - `03-runtime-flows.md`
  - `06-idempotency-consistency.md`
  - `08-dependencies-and-ownership.md`
- If future coordination infrastructure is introduced, add ADR(s) covering:
  - chosen coordination system
  - quorum assumptions
  - membership authority
  - fencing model
  - failure and loss-of-confidence behavior