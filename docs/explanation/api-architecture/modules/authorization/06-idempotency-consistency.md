# Authorization — Idempotency & Consistency (V1)

This document defines Authorization-specific idempotency, consistency guarantees, stale-governance protection, ordering-sensitive enforcement, cache-safe policy evaluation rules, and reconciliation/reporting posture for derived governance outputs.

System-wide rules live in:
- `../../../../architecture/arc42/11-replication-v1.md`
- `../../../../architecture/arc42/13-transactions-and-consistency-v1.md`
- `../../../../architecture/arc42/14-distributed-systems-assumptions-v1.md`
- `../../../../architecture/arc42/15-consistency-ordering-and-consensus-v1.md`
- `../../../../architecture/arc42/16-batch-processing-and-derived-data-v1.md`
- `../../../../architecture/arc42/17-dataflow-and-batch-workflows-v1.md`
- `../../../../architecture/arc42/18-stream-processing-and-derived-state-v1.md`
- `../../../../architecture/arc42/19-stream-processing-runtime-v1.md`
- ADR-0013 (Outbox & delivery semantics)
- ADR-0015 (Redis cache policy)
- ADR-0016 (Authorization model: RBAC/ABAC + policies)
- ADR-0018 (Transaction boundaries & consistency model)
- ADR-0019 (System model and fault assumptions)
- ADR-0020 (Timeout, retry, and failure detection policy)
- ADR-0021 (Clock, time, and ordering policy)
- ADR-0022 (Versioning and fencing strategy)
- ADR-0023 (Consistency, ordering, and consensus boundaries)
- ADR-0024 (Distributed coordination and singleton work policy)
- ADR-0025 (Batch processing and derived state policy)
- ADR-0026 (Batch job orchestration and materialization policy)
- ADR-0027 (Stream processing and derived state policy)
- ADR-0028 (Consumer idempotency, replay, and rebuild policy)

---

## 0) Role of Authorization in the system

Authorization owns **governance truth** for access-control assignments and policy-enforcement inputs.

It decides:
- which roles exist
- which permissions belong to which roles
- which roles are assigned to which users
- which governance truth is currently enforceable by the API layer

Authorization does **not** own:
- user identity truth
- audit persistence truth
- notification truth
- Content lifecycle truth

Therefore:
- governance correctness must come from Authorization truth
- audit is downstream evidence, not part of governance success
- cache may accelerate evaluation, but must not become authority
- uncertain/stale authorization state must fail closed on security-sensitive paths

---

## 1) Truth vs derived

### 1.1 Truth (Authorization store)
Authorization owns governance truth:
- roles
- permissions
- role-permission assignments
- user-role assignments
- optional direct user permissions if modeled

Truth invariants include:
- uniqueness of assignments:
  - `(UserId, RoleId)` unique
  - `(RoleId, PermissionId)` unique
- governance mutations must be immediately enforceable after successful commit
- protected invariants must be preserved where policy requires them

### 1.2 Derived (allowed but security-sensitive)
Derived or cached state may include:
- permission snapshots per user
- policy-evaluation caches
- authz revision caches
- derived policy materializations
- governance summaries and drift reports
- reconciliation candidate outputs

These are performance or operability aids only.

**Rule:** any authorization caching must be safe by default:
- never grant access if uncertain
- never use stale/unknown cache as authority for admin/governance decisions
- invalidate by governance events
- prefer primary truth reads for admin/self governance flows

### 1.3 Server truth beats cache belief
Authorization must not trust:
- stale cached grants
- stale local snapshots
- client-supplied claims about current role membership
- timeout-based assumptions such as “the grant probably failed”

Authority remains:
- Authorization truth in the primary store
- server-side policy evaluation against current truth
- deterministic revision/version markers where introduced

### 1.4 Consistency class for Authorization
Authorization intentionally uses multiple consistency classes:

#### Strong truth-backed consistency
Required for:
- role/permission assignment truth
- user-role truth
- governance mutation results
- invariants protecting enforceable access-control state
- any security-sensitive decision that changes who may do what

#### Ordered / causality-sensitive consistency
Required for:
- governance changes per `UserId`
- permission changes per `RoleId`
- stale admin action rejection where load-then-save workflows exist
- ordered cache/materialization invalidation when freshness markers are used
- downstream consumers that must not reapply older governance state after newer truth exists
- candidate publication that must not overwrite fresher derived governance output

#### Eventual consistency
Accepted for:
- audit persistence
- cache invalidation propagation
- optional derived policy views
- downstream observability materializations
- governance reconciliation/reporting convergence

---

## 2) Idempotent writes (mandatory)

### 2.1 Assignment/grant commands must converge
`AssignRole` and `GrantPermission` must be idempotent:
- repeated calls result in the same final truth state

Examples:
- assigning a role the user already has returns success with unchanged truth state
- granting a permission a role already has returns success with unchanged truth state

### 2.2 Revoke commands must also converge
`RevokeRole` and `RevokePermission` should converge safely:
- repeated revoke does not corrupt truth
- repeated revoke returns a deterministic result (`no-op`, `success`, or documented conflict)

### 2.3 Implementation posture
Recommended implementation posture:
- enforce uniqueness with DB constraints
- treat uniqueness conflicts as idempotent success or a documented conflict outcome
- do not emit duplicate downstream side effects for logically unchanged governance state

### 2.4 Timeout ambiguity posture
If an admin call times out:
- timeout does **not** prove the governance mutation failed
- safe reconciliation must inspect Authorization truth
- retries must not create duplicate assignments/grants or contradictory revokes

### 2.5 Idempotency is preferred over singleton assumptions
Governance correctness must not depend on:
- “only one admin request will arrive”
- “only one worker will process this mutation”
- local process leadership or startup order
- only one reconciliation/reporting workflow being assumed exclusive without explicit protection

Authorization should instead rely on:
- truth-store authority
- uniqueness constraints
- deterministic command semantics
- explicit version/revision checks where needed

### 2.6 Idempotency must survive replay
If the same governance event is replayed because of:
- worker restart
- broker redelivery
- bounded replay
- cache/materialization repair
- reconciliation rerun

the outcome must still converge to one correct derived downstream state and zero duplicate harmful effects.

---

## 3) Consistency expectations

### 3.1 Strong consistency for governance truth
Governance changes are strongly consistent in Authorization truth immediately after request success.

This means:
- admin reads after write must reflect the new state (read-your-writes)
- policy evaluation for subsequent requests must enforce the new truth immediately
- stale caches must not continue granting or denying based on obsolete state for critical governance flows

**Policy:** admin governance reads MUST bypass stale replicas/caches.

### 3.2 Eventual consistency for audit and side effects
The following are eventual:
- audit persistence
- cache invalidation propagation
- optional derived policy views
- governance reporting and reconciliation outputs

Core governance writes must not block on these subsystems.

### 3.3 Fail-closed rule
If authorization state is uncertain or stale during evaluation of security-sensitive/admin requests:
- deny rather than grant

This applies especially to:
- admin endpoints
- governance mutation endpoints
- protected operations where stale grants would be dangerous

### 3.4 No global ordering assumption
Authorization does **not** assume:
- one total global order across all governance mutations
- one globally ordered stream for all users, roles, and permissions

**Rule:** ordering is scoped per governance subject or aggregate where needed.

### 3.5 Cause-before-effect rule
Derived governance effects must not outrun committed truth.

Examples:
- a permission snapshot must not show a role grant before Authorization truth committed it
- an old derived role view must not survive as effective truth after revoke
- a rebuilt materialization must not publish older governance knowledge over fresher truth

---

## 4) Transaction boundary (V1)

### 4.1 Truth boundary
The Authorization transaction boundary stops at the Authorization-owned truth change.

Typical truth changes include:
- assigning or revoking a role for a user
- granting or revoking a permission for a role
- creating/updating governance reference data where supported
- updating governance revision/version markers used for cache invalidation or truth-backed evaluation

### 4.2 Atomic commit set
For governance-changing commands, Authorization MUST commit atomically:
- the governance truth change
- any local metadata required for deterministic enforcement
- any local revision/version marker required for cache invalidation or stale-read protection
- the Outbox record for downstream side effects

Typical examples:
- `AssignRole`: `UserRole` change + revision/version update if used + Outbox
- `RevokeRole`: `UserRole` change + revision/version update if used + Outbox
- `GrantPermission`: `RolePermission` change + revision/version update if used + Outbox
- `RevokePermission`: `RolePermission` change + revision/version update if used + Outbox

### 4.3 Outside the transaction
The following MUST NOT be required inside the Authorization truth transaction:
- audit persistence in downstream stores
- broker publish
- Redis invalidation as a success condition
- cache rebuild/materialization
- external policy propagation calls
- external HTTP/API calls
- reconciliation/reporting workflow completion

These are post-commit async effects.

### 4.4 Transaction duration rule
Authorization transactions must be short:
- no waiting on downstream systems
- no cross-request interactive transaction
- no long-running workflow inside one open transaction
- no retry loops over external dependencies inside the transaction

This is especially important because governance writes affect security enforcement and should remain predictable under load.

### 4.5 Shared DB does not widen Authorization ownership
Even in a shared DB deployment, Authorization must not use the same truth transaction to directly perform:
- Audit business writes
- Identity business writes
- Notification business writes

Authorization may write:
- Authorization-owned truth tables
- approved local replication artifacts such as Outbox
- local durable idempotency/revision records when policy requires them

It must not couple governance success to downstream completion.

### 4.6 No heterogeneous distributed transaction
Authorization does **not** attempt one atomic workflow across:
- Authorization truth DB
- Redis
- RabbitMQ
- audit stores
- other module-owned truth stores

Atomicity stops at:
- Authorization truth mutation
- required local metadata/revision state
- Outbox intent

---

## 5) Concurrency and stale-governance protection

### 5.1 Concurrency expectations
Authorization must assume:
- client retries
- concurrent admin actions
- duplicate command submission
- stale admin forms or stale admin views
- racing grant/revoke operations
- delayed or replayed governance events for downstream consumers
- later-running reconciliation/reporting jobs over already-changed truth

### 5.2 Required protections
At minimum, the design must prevent:
- duplicate assignment rows
- duplicate grant rows
- stale authorization reads after successful governance changes
- non-deterministic outcomes under concurrent identical operations
- stale admin actions silently overwriting newer governance state where editable aggregate documents are used

Typical implementation options include:
- DB uniqueness constraints
- deterministic idempotent command handling
- durable idempotency records for retriable admin operations
- primary-read enforcement for immediate post-write governance reads
- optimistic concurrency / revision checks if governance documents/forms are edited in load-then-save style

### 5.3 Multi-row governance invariants
If governance rules go beyond simple uniqueness and become aggregate invariants, they require explicit design.

Examples:
- “the system must always retain at least one active admin”
- “a protected role must never lose all required permissions”
- “a tenant must always retain at least one governance owner”

These invariants must not rely on naive application-side `check then write` logic alone.

They are candidates for:
- stronger database constraints where expressible
- stronger isolation / locking
- invariant-specific transaction design
- explicit conflict handling with safe failure

### 5.4 Version/revision posture
Where governance state is edited in document-style or aggregate-style workflows, version/revision should be preferred over timestamps.

Authorization must not use:
- `UpdatedAt`
- latest timestamp
- stale cached view timestamps

as primary freshness authority for correctness-sensitive governance writes.

### 5.5 Resource-side protection beats caller belief
A caller saying:
- “this role membership is still current”
- “this permission grant is still the latest intended state”
- “the previous request surely failed because of timeout”

is not sufficient.

The Authorization truth boundary must verify:
- current assignment truth
- current role-permission truth
- current revision/version where used
- invariant legality where applicable

### 5.6 Stale derived governance must not outrun truth
If a derived permission snapshot, cache, or materialized policy view is older than current Authorization truth:
- it must not be treated as effective authority
- it must be ignored, invalidated, rebuilt, or bypassed according to policy
- it must never silently grant dangerous access on critical paths

---

## 6) Replication mechanics (Outbox + events)

### 6.1 Outbox is required for governance mutations
For each governance mutation, Authorization MUST:
- commit the truth change
- write an Outbox event in the same transaction

### 6.2 Events emitted (V1)
Typical events:
- `UserRoleAssigned`
- `UserRoleRevoked`
- `RolePermissionGranted`
- `RolePermissionRevoked`
- `RoleCreated/Updated`
- `PermissionCreated/Updated` where needed

Envelope requirements:
- `messageId`
- `aggregateId`
- `version` when ordering matters
- `occurredAt`
- `correlationId`
- `actorUserId?`

### 6.3 Consumers
Typical consumers:
- Audit Trail
- optional cache invalidation/materialization consumers

Consumers must be:
- idempotent
- safe under duplicate delivery
- able to resync from truth if ordered change gaps are detected

### 6.4 Outbox is the causal boundary
For Authorization, Outbox is the durable bridge between:
- governance truth mutation
- downstream cache/materialization invalidation
- downstream audit/observability side effects

This means:
- audit and cache propagation derive from committed governance truth
- delayed side effects do not weaken or redefine the governance decision
- replay and reconciliation start from Authorization truth + Outbox, not from timing assumptions

### 6.5 At-least-once downstream posture
Authorization assumes downstream governance events may be:
- duplicated
- delayed
- replayed
- reprocessed after crash
- consumed out of the ideal operational sequence

Therefore:
- downstream consumers must not treat arrival order alone as authority
- dedupe and truth resync are required tools
- Authorization truth remains the authoritative rebuild source

---

## 7) Ordering and conflict posture

### 7.1 Ordering model (per governance subject)
Governance operations are ordered per subject:
- per `UserId` for user-role changes
- per `RoleId` for role-permission changes

If ordered versioning exists:
- include `(UserId, Version)` or `(RoleId, Version)` in events

### 7.2 Consumer rule
Consumers should:
- apply idempotently
- reject duplicates
- if gaps are detected and correctness depends on exact order, resync from truth

### 7.3 Conflict posture
Authorization avoids many conflicts by design using:
- single truth store
- DB uniqueness constraints
- deterministic no-op or conflict responses

If concurrent requests occur:
- DB constraints enforce convergence for uniqueness rules
- APIs return deterministic outcomes (`applied`, `no-op`, `conflict`, or policy-defined equivalent)

### 7.4 Timestamps are informational, not freshness authority
Governance conflict resolution must not rely on:
- “largest `UpdatedAt` wins”
- “latest event time wins”

Where freshness matters, use:
- revision/version
- authoritative truth read
- conditional mutation semantics

### 7.5 No global total order across governance
Authorization ordering guarantees do not imply:
- one total order between all user-role mutations
- one total order between all role-permission mutations
- one cross-module governance ordering stream

Ordering is scoped to the governance subject that actually needs it.

### 7.6 Replay of older governance state must not weaken newer truth
If a stale derived view or older governance event reappears after newer truth exists:
- consumers must reject, ignore, or resync
- it must not reintroduce a revoked role or revoked permission in derived state
- it must not weaken fail-closed evaluation posture

---

## 8) Retry safety (sync + async)

### 8.1 Client retries
If clients retry due to timeout ambiguity:
- the system must not duplicate assignments/grants
- recommended support for `Idempotency-Key` on governance write endpoints

### 8.2 Consumer idempotency
Consumers such as Audit ingestion must be idempotent:
- dedupe by `messageId`
- use unique audit event identifiers where applicable

### 8.3 Safe reconciliation after ambiguous outcomes
After timeout or retry ambiguity, safe reconciliation must rely on:
- current Authorization truth
- current user-role / role-permission truth
- revision/version markers if used
- durable idempotency records where supported

Do not infer from:
- audit arrival timing
- cache contents alone
- downstream consumer completion

Authorization truth is authoritative.

### 8.4 Retry-safe design beats exclusive execution assumptions
Authorization correctness must not depend on:
- only one governance consumer being active
- one process being “the current authority propagator”
- local ownership belief

If future ownership-sensitive workflows are introduced, they must use authoritative generation/fencing checks rather than naive singleton assumptions.

### 8.5 Replay-safe derived governance workflows
Reconciliation, reporting, snapshot rebuild, and materialization repair workflows must be safe to rerun on the same bounded input:
- without corrupting active derived outputs
- without downgrading to older governance knowledge
- without assuming one exclusive worker by default

---

## 9) Caching posture (security guardrails)

### 9.1 Default rule
Default rule:
- evaluate governance state from primary truth for admin/governance flows

### 9.2 If caching is introduced
If caching is introduced:
- it must be invalidated by governance events
- it must fail closed when stale or uncertain
- it must not grant access solely from stale cached grants
- it should use revision/etag semantics per user/role if practical

### 9.3 Redis is never truth
Redis is never the source of truth for authorization.

It may accelerate:
- permission snapshots
- policy evaluation inputs
- revision markers

But it must not override authoritative governance truth.

### 9.4 Cache uncertainty must not grant
If a cache lookup is stale, missing, or ambiguous for a security-sensitive path:
- deny or fall back to authoritative truth according to endpoint posture
- never convert uncertainty into permission to proceed

### 9.5 Cache is acceleration, not governance memory
A cache hit is useful only while it remains consistent enough by policy.
It is not evidence that:
- the latest grant definitely exists
- the latest revoke definitely failed
- the current permission set is safe to trust without verification on critical paths

---

## 10) Reconciliation / reporting posture

### 10.1 Governance truth is the authoritative rebuild source
When derived policy snapshots or governance reports need rebuild or repair, authoritative input must come from:
- current Authorization truth
- versioned governance events where applicable
- bounded truth snapshots

### 10.2 Derived outputs remain derived
Permission snapshots, governance reports, drift reports, and reconciliation outputs are derived artifacts.

They may be:
- rebuilt
- replaced
- reconciled
- delayed

But they do not become governance truth.

### 10.3 Candidate-before-publication
If a reconciliation/reporting workflow produces an important derived governance output:
- build candidate first
- validate candidate
- publish/cut over explicitly
- do not treat partial candidate output as complete active state

### 10.4 Rerun safety
Important reconciliation/reporting workflows must be safe to rerun on the same bounded input without corrupting current derived outputs.

### 10.5 Rebuild is acceptable when safer than fragile repair
If a derived governance snapshot or materialized policy view is cheap enough to rebuild from Authorization truth, full rebuild is preferable to fragile partial mutation.

---

## 11) Coordination and ownership posture (Authorization)

### 11.1 Authorization does not require global singleton coordination by default
Ordinary Authorization correctness must not depend on:
- one global authz leader
- one process being “the only evaluator”
- startup order deciding who is authoritative
- timeout-only assumptions about which invalidator or materializer is current

Authorization correctness should instead be achieved through:
- truth-store authority
- uniqueness constraints
- revision/version-aware mutations
- fail-closed policy evaluation
- idempotent downstream consumers

### 11.2 If future ownership-sensitive governance workflows are introduced
If a future Authorization workflow truly requires one current owner
(for example exclusive rebuild of a permission materialization or one-current governance repair owner),
that workflow must define:
- ownership source of truth
- monotonic generation/fencing token
- resource-side rejection of stale owner actions

Naive leader/lock patterns are not acceptable.

### 11.3 Safe non-progress beats unsafe stale governance output
If ownership is ambiguous for a correctness-sensitive reconciliation/materialization workflow, Authorization must prefer:
- delayed rebuild
- stale-owner rejection
- operator retry
- continued truth-first evaluation

over unsafe dual publication or stale governance overwrite.

---

## 12) Observability signals (Authorization-specific)

Minimum signals:
- governance write success/failure rate
- policy deny spikes (unexpected `403/401` trends on admin endpoints)
- outbox backlog/age for governance events
- audit ingestion lag/backlog and DLQ rate
- duplicate-prevention indicators (dedupe hits, uniqueness conflicts)
- stale-governance incidents or forced truth fallback if measurable
- invariant-protection rejects for protected governance rules
- idempotency-key reuse conflicts where supported
- revision/version conflict rejects where applicable
- reconciliation/reporting activity for derived governance outputs
- candidate publication/cutover failures for important derived outputs
- ownership-generation mismatch count if future ownership-sensitive workflows are introduced

Logs should include:
- `correlationId`
- `actorUserId`
- target identifiers (`UserId`, `RoleId`, `PermissionKey`)
- outcome (`applied`, `no-op`, `idempotent`, `conflict`, `denied`, `stale-rejected`)
- no unnecessary PII or secret material

---

## 13) Summary

Authorization correctness in V1 rests on fifteen rules:

1. Governance truth is authoritative and must be primary-consistent.  
2. Read-your-writes is mandatory for admin/governance truth reads.  
3. Caches may accelerate evaluation but must never become authorization authority.  
4. If uncertain, fail closed rather than grant.  
5. Timeout does not prove a governance mutation failed.  
6. Uniqueness constraints and deterministic command handling are the baseline for convergence.  
7. Multi-row governance invariants require explicit design beyond naive check-then-write logic.  
8. Audit and cache invalidation are downstream eventual effects; governance truth remains immediate and authoritative.  
9. Async downstream processing is at-least-once; duplicates, replay, and lag are normal.  
10. No global ordering or distributed transaction is assumed for Authorization workflows.  
11. Governance reports and policy snapshots are derived, not truth.  
12. Replay of stale governance-derived state must not weaken newer truth.  
13. Important reconciliation/materialization workflows must be rerun-safe.  
14. Candidate derived governance output must be validated before publication when correctness matters.  
15. Singleton/ownership semantics are not relied on unless explicitly protected by authoritative generation/fencing rules.