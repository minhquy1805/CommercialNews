# Authorization — Observability & SLO Signals (V1)

Related:
- `../../../../architecture/arc42/04-runtime-view-v1.md`
- `../../../../architecture/arc42/11-replication-v1.md`
- `../../../../architecture/arc42/18-stream-processing-and-derived-state-v1.md`
- `../../../../architecture/arc42/19-stream-processing-runtime-v1.md`
- `../../../../decisions/adr-0013-outbox-and-delivery-semantics-v1.md`
- `../../../../decisions/adr-0027-stream-processing-and-derived-state-policy-v1.md`
- `../../../../decisions/adr-0028-consumer-idempotency-replay-and-rebuild-policy-v1.md`

---

## 1) Critical signals

### Policy enforcement health
- policy deny rate (403) per endpoint and per permission key
- unexpected allow/deny drift indicators (optional):
  - sudden drop in deny rate on sensitive endpoints
  - sudden increase in deny rate on previously stable endpoints

### Governance mutation health
- mutation rate for:
  - roles
  - permissions
  - user-role assignments
  - role-permission grants
- idempotent/no-op rate (optional):
  - repeated assigns/grants that result in no-op (can indicate client retries or automation loops)
- uniqueness conflict / constraint violation rate (should be low; if high, likely retry storms)

### Availability & latency (admin APIs)
- error rate (5xx/timeouts)
- latency percentiles (P95/P99) for admin endpoints
  - not as strict as public read path, but must be stable
- dependency latency (DB) for common governance queries

### Cache posture signals (if caching exists)
- permission cache hit rate (per key group)
- cache invalidation event processing lag
- stale/unknown decision count (should be 0 for admin; fail-closed posture)

### Interpretation rule
Observability must distinguish:
- governance truth commit health
- policy evaluation correctness
- derived cache/materialization lag
- downstream audit/reporting lag
- client/automation retry storms versus true system instability

---

## 2) Governance / audit completeness signals

### Outbox and event freshness for authorization events
- outbox pending count for authorization events
- outbox oldest pending age (seconds)
- broker queue depth (ready/unacked) for:
  - audit ingestion consumers
  - any authz-related invalidation consumers

### Audit pipeline health (for authz events)
- audit handler success/failure rate
- retry volume + error classification
- DLQ size and DLQ oldest age
- time-to-ingest distribution for authz governance actions:
  - `occurredAt → audit storedAt` (P50/P95/P99)

### Completeness checks (optional, high value)
- mismatch rate between governance mutations and audit inserts over a window
  - by correlationId/eventType sampling for: AssignRole, RevokeRole, GrantPermission, RevokePermission

### Emission / ingestion interpretation rule
Operators should be able to tell whether:
- governance truth committed but audit is merely delayed
- outbox emission is lagging
- broker/backlog is the bottleneck
- audit consumer is failing
- completeness mismatch is a real evidence gap versus temporary lag

---

## 3) Derived governance rebuild / reconciliation signals

These apply when Authorization runs bounded workflows for:
- permission snapshot rebuild
- governance drift detection
- orphan mapping checks
- reconciliation of derived policy materializations vs truth
- governance reporting jobs

### Workflow health SLIs
- run success/failure count
- run duration
- records selected / processed / skipped / repaired
- mismatch count from reconciliation
- derived snapshot/report freshness age
- candidate output generation success/failure
- publication/cutover success/failure for important derived governance outputs

### Replay / recovery indicators
- rerun count
- full rebuild chosen vs targeted repair count
- repeated mismatch on same bounded scope
- stale candidate / stale snapshot reject count
- truth-resync trigger count where ordered consumers/materializers detect gaps

### Ownership-sensitive workflow signals (if introduced later)
- duplicate-run detection count
- stale-owner rejection count
- safe no-owner / degraded intervals

### Policy
Authorization truth remains authoritative even when reconciliation/reporting workflows lag or fail.
Derived governance outputs may lag, but must not be mistaken for enforceable truth.

### Recovery interpretation rule
Observability should make it clear whether:
- truth is healthy but derived snapshots are stale
- reconciliation is finding real drift
- rebuild is successfully converging
- a cutover/publication failure is blocking derived output refresh
- fail-closed behavior is protecting security while derived outputs recover

---

## 4) Security anomaly signals

### Access pattern anomalies
- sudden spike in 403 across many endpoints (global)
- spike in 401/403 specifically on admin endpoints (possible attack/misconfig)
- repeated denies for high-privilege permissions (possible probing)

### Governance change anomalies
- unusual frequency of role/permission changes
- bursty patterns of:
  - mass user-role assignments
  - mass permission grants/revokes
- repeated changes targeting the same user/role (toggle behavior)

### Integrity signals
- sudden increase in fail-closed decisions (if caching exists)
- unusual drop in governance mutations (could indicate system outage or blocked access)
- unexpected rise in stale-truth fallback / cache-bypass decisions
- repeated invariant-protection rejects for special governance rules (for example “last admin” protections if implemented)

### Interpretation rule
Security anomaly observability should help distinguish:
- misconfigured policies
- malicious probing
- stale or broken derived materialization
- real governance churn from operations
- retry/automation loops that only look like malicious activity

---

## 5) Release gates (recommended)

During rollout, gate on:
- spikes in 5xx/timeouts for admin endpoints
- sustained P99 latency regression for governance endpoints
- spikes in 403 across multiple endpoints (misconfigured policies)
- sustained outbox backlog/age growth for authorization events
- audit ingestion DLQ growth or DLQ oldest age breach
- reconciliation/reporting failure spikes for important derived governance outputs
- unexpected increase in fail-closed decisions caused by stale/uncertain derived state
- any signal that stale derived governance state is still being trusted on critical paths (release blocker)

### Strong stop conditions
Immediate pause/rollback is recommended if:
- policy misconfiguration causes broad unexpected deny or allow drift
- stale cache/materialization is being used as effective authority on admin/security-sensitive paths
- governance truth commits are succeeding but downstream invalidation/reporting paths are producing unsafe contradictory behavior
- replay/rebuild of derived governance outputs is publishing stale knowledge over fresher truth

---

## 6) Operator questions this module must answer

Authorization observability should help answer:

1. Did governance truth commit successfully?  
2. Is a deny/allow problem caused by truth, cache/materialization lag, or policy misconfiguration?  
3. Is audit merely delayed, or is governance emission broken?  
4. Are retries caused by client behavior, automation loops, or system instability?  
5. Are reconciliation/reporting workflows only lagging operationally, or drifting away from truth?  
6. Are fail-closed decisions correctly protecting the system, or are they masking a broader derived-state problem?  
7. Is the current incident in canonical governance truth, in downstream audit/invalidations, or only in derived reporting/materialization?