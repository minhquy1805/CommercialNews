# System Data Model — Audit (V1)

> **Recommended file:** `explanation/architecture/arc42/system-data/system-data-audit-v1.md`  
> **Module:** Audit Trail  
> **Purpose:** Append-only logging for sensitive actions across modules, designed for investigations, governance, and operational forensics.

---

## 0) Data System fit (V1)

Audit is a **governance-critical** subsystem, but it must be **non-blocking** for core flows.

- **Truth store:** append-only `AuditLog` (investigation-ready)
- **Ingestion:** async via Outbox + Worker (recommended)
- **Idempotency:** required (at-least-once delivery is assumed)
- **Privacy:** minimum necessary data; no secrets/tokens; safe logging

**Non-negotiables (from Quality Requirements)**
- Admin actions must be traceable (publish/unpublish, RBAC changes, deletes/restores)
- Audit ingestion failures must not break core writes
- Backlog/lag must be observable and recoverable

---

## 1) Capability → Entity mapping

### 1.1 Sensitive actions logging (audit trail)
**Entity**
- `AuditLog` (append-only, investigation-ready)

**Reliability & ingestion**
- Audit events should flow through a shared Outbox mechanism where possible.
- Consumers must be idempotent (dedup on `AuditEventId`).
- V2 can add consumer checkpoints and gap detection for stronger guarantees.

---

## 2) Identify entities (V1)

### V1 must-have
1. `AuditLog`

### V2 hooks
- `AuditIngestionCheckpoint` (consumer checkpoints)
- `AuditRedactionRule` (policy-driven redaction)
- `AuditGapDetection` (signals/alerts for missing events)

---

## 3) Dataflows (V1) — REST / DB / Broker (DDIA Ch4)

### 3.1 Producer side (core modules)
Core modules emit audit intents as events, but do not persist audit synchronously:

- Content: Publish/Unpublish/Archive/Delete/Restore
- Authorization: Role/Permission grants/revokes
- Identity: Email verified, password changed (policy-defined)

**Sync boundary**
- Core write succeeds even if audit is delayed.

### 3.2 Ingestion side (Worker)
- Worker consumes audit events from broker (or pulls from Outbox)
- Writes an immutable `AuditLog` row
- Dedup by `AuditEventId` (unique)

**Failure behavior**
- Retry with backoff; poison messages go to DLQ/manual intervention
- Backlog is observable (success/failure rates, lag)

---

## 4) Relationships (V1)

AuditLog references:
- `ActorUserId` (Identity) — nullable for system actions
- Target resource: `(ResourceType, ResourceId)` stored as strings (no hard FK)
  - supports multi-module targets
  - supports future split DBs and mixed ID types

> Design intent: audit must survive schema/ID evolution without being tightly coupled to each module’s physical DB.

---

## 5) Invariants (V1 rules)

### 5.1 Append-only immutability
- AuditLog is immutable after insert (no UPDATE/DELETE) unless explicit retention rules exist.

### 5.2 Mandatory fields (investigation-ready)
- Always capture:
  - actor (who) — may be null for system actions
  - action (what)
  - target resource type/id (which)
  - occurredAt (when)
  - correlationId (traceability)
- **Reason is required** when policy demands it (e.g., unpublish, RBAC changes).

### 5.3 Privacy / redaction (hard rule)
- Never store:
  - passwords, password hashes
  - raw verification/reset/refresh tokens
  - secrets/API keys
- Minimize PII; keep IP/UserAgent optional and policy-controlled.
- JSON payloads must be redacted/minimized (store only what investigations need).

### 5.4 Reliable under retries (idempotent ingestion)
- Same `AuditEventId` must not be written twice.

### 5.5 Queryable for investigations
Audit must be efficiently queryable by:
- time range
- actor
- resource
- action
- correlationId

---

## 6) Fields (Logical schema) — SQL Server (V1)

### 6.1 `AuditLog`
| Field | Type | Null | Default | Notes |
|---|---|---:|---|---|
| AuditId | BIGINT IDENTITY | NO |  | PK |
| AuditEventId | UNIQUEIDENTIFIER | NO | NEWID() | idempotency key |
| ActorUserId | BIGINT | YES |  | nullable if system |
| Action | NVARCHAR(120) | NO |  | e.g. `Content.Publish`, `Auth.RoleGrant` |
| ResourceType | NVARCHAR(60) | NO |  | `Article`, `User`, `Comment`, ... |
| ResourceId | NVARCHAR(100) | NO |  | stored as string |
| Reason | NVARCHAR(500) | YES |  | required by policy for some actions |
| OccurredAt | DATETIME2(3) | NO | SYSUTCDATETIME() | when |
| CorrelationId | NVARCHAR(100) | YES |  | trace |
| IpAddress | NVARCHAR(45) | YES |  | optional PII |
| UserAgent | NVARCHAR(300) | YES |  | optional PII |
| OldValuesJson | NVARCHAR(MAX) | YES |  | redacted/minimized |
| NewValuesJson | NVARCHAR(MAX) | YES |  | redacted/minimized |
| MetadataJson | NVARCHAR(MAX) | YES |  | module info, policy version |

**V2 hooks**
- `RedactionVersion`, `TenantId`
- `IngestedAt`, `IngestionStatus`

---

## 7) Constraints & indexes — Audit (V1)

### 7.1 PK / UNIQUE (idempotency)
- `PK_AuditLog(AuditId)`
- `UQ_AuditLog_AuditEventId(AuditEventId)` ✅ prevents duplicates under retries

### 7.2 CHECK (optional)
- `Action` not empty
- `ResourceType` not empty
- `ResourceId` not empty

### 7.3 Indexes (investigation readiness)
- `IX_AuditLog_OccurredAt` on `(OccurredAt DESC)`
- `IX_AuditLog_ActorUserId_OccurredAt` on `(ActorUserId, OccurredAt DESC)`
- `IX_AuditLog_Resource_OccurredAt` on `(ResourceType, ResourceId, OccurredAt DESC)`
- `IX_AuditLog_Action_OccurredAt` on `(Action, OccurredAt DESC)`
- `IX_AuditLog_CorrelationId` on `(CorrelationId)` (optional)

### 7.4 Append-only enforcement (policy)
Enforce immutability via:
- DB permissions (deny UPDATE/DELETE), and/or
- a trigger that blocks modifications

---

## 8) Retention & operational jobs (V1 policy)

- Define retention windows for audit logs (policy-level).
- Provide purge/archive jobs that:
  - do not break investigation requirements
  - are observable (job success/failure + records purged)
- Consider periodic “audit completeness” checks in V2 (gap detection).

---

## 9) ADR candidates
- Outbox → Audit event taxonomy (action naming conventions)
- What counts as “sensitive action” in V1 vs V2 (scope)
- PII policy for `IpAddress/UserAgent` retention
- Redaction policy and schema for Old/New JSON values

---

## 10) Partitioning Readiness (V1/V2)

> This section captures **partitioning and ingestion/workload-partitioning readiness** for Audit Trail.
> V1 remains **non-sharded by default**; priority is append-only ingestion reliability, investigation queryability, and non-blocking core flows.

### 10.1 Why Audit is a partitioning-risk module

Audit is a **governance-critical append-only** subsystem with two competing needs:

* high-volume async ingestion (especially during admin bursts / policy changes)
* efficient investigation queries (time range, actor, resource, action, correlationId)

**V1 principle:** optimize for **reliable ingestion + investigation indexes** before introducing shard complexity.

---

### 10.2 Primary access patterns (V1)

**Hot path (worker ingestion)**

* consume audit events asynchronously
* insert immutable `AuditLog` rows
* dedup by `AuditEventId`

**Investigation reads**

* by time range (`OccurredAt`)
* by actor + time
* by resource + time
* by action + time
* by `CorrelationId`

**Dependency rule**

* core writes must not block on audit ingestion success
* delayed audit is acceptable only if backlog/lag is visible and recoverable

---

### 10.3 Secondary-index-heavy queries (present and future)

**V1**

* investigation filters using combinations of:

  * `OccurredAt`
  * `ActorUserId`
  * `(ResourceType, ResourceId)`
  * `Action`
  * `CorrelationId`

**V2+**

* completeness/gap investigation
* richer JSON field search (metadata/old/new values)
* redaction-version / tenant / policy-version queries

**Implication**

* Audit is append-only for writes, but **secondary-index-heavy for investigations**.
* V2+ may need dedicated investigation/search projections before truth-store sharding.

---

### 10.4 Candidate partitioning strategy (future)

Audit partitioning should follow **append-only + investigation time-window** behavior.

#### A) `AuditLog` (append-only truth)

**Likely fit (future):** **range/hybrid**

* time-based range partitioning (natural fit for retention and investigation windows)
* hybrid option (bucket/module/actor prefix + time) if ingestion hotspots emerge

**Why range/hybrid is attractive**

* investigation queries are strongly time-oriented
* retention/archive jobs are time-oriented
* replay/reconciliation often use time windows

**Risk**

* pure time-range partitioning can create a hot “current” partition under bursts

#### B) Investigation/search paths (V2+)

**Likely fit:** dedicated projection/search path

* keep `AuditLog` as append-only truth
* offload heavy search/filtering to projection/index if query complexity grows

---

### 10.5 Hotspot and skew risks (V1)

#### A) Ingestion bursts

* admin bulk actions / scripts / migrations / policy changes can create short spikes
* retries after downstream outages can create replay bursts

#### B) Current-range hotspot (future partitioning risk)

* if partitioned purely by time, newest partition may become hot

#### C) Investigation read skew

* incident investigations repeatedly query recent time windows and same `CorrelationId` / resource

---

### 10.6 V1 mitigations (no sharding yet)

CommercialNews V1 already has the correct baseline mitigations for Audit:

* **Async ingestion** (core writes remain non-blocking)
* **Idempotent inserts** via `AuditEventId` unique key
* **Append-only policy** (immutability + governance safety)
* **Investigation-ready indexes** on time/actor/resource/action/correlation
* **Retry/backoff + observability** for ingestion failures/backlog
* **Privacy/redaction rules** to keep payloads safe and minimal

These tactics are preferred before introducing shard complexity.

---

### 10.7 V2+ scale options (selective)

Introduce stronger partitioning only when signals justify it.

#### Option A — Ingestion lanes / ownership partitioning (recommended first)

Partition ingestion work by logical lane, for example:

* event family / source module
* hash bucket on `AuditEventId` or `CorrelationId`
* priority lane (if policy differentiates)

**Why first**

* directly addresses worker throughput and backlog control
* lower complexity than truth-store sharding
* aligns with API + Worker topology

#### Option B — Time-based or hybrid partitioning for `AuditLog`

Use when:

* retention/archive operations become too expensive
* recent-window investigations degrade despite indexes
* audit volume materially impacts recovery/replay operations

#### Option C — Investigation/search projection

Introduce a derived search/read path for complex investigations while keeping `AuditLog` as immutable truth.

---

### 10.8 Rebalancing and routing readiness (future)

Audit will likely need **workload rebalancing** before truth-store sharding.

**Likely rebalance unit**

* ingestion lane / worker ownership shard
* later: time bucket or hybrid partition ranges

**Routing requirement**

* authoritative mapping for `lane -> worker owner`
* safe reassignment with throttling and observability

**Guardrail**

* rebalance/scale changes must not cause sustained backlog growth that threatens audit completeness expectations

---

### 10.9 Partition-readiness observability signals (Audit)

Use existing V1 measurement signals to decide when stronger partitioning is needed:

* audit ingestion success/failure rate
* queue backlog/lag trend for audit consumers
* consumer processing latency P95/P99
* retry rate and DLQ rate/age (if enabled)
* dedupe hits / duplicate-prevention indicators (`AuditEventId`)
* investigation query latency on recent windows (if measured)
* backlog recovery time after bursts/incidents

**Scale trigger (policy-level)**
Consider stronger workload/data partitioning when sustained pressure causes:

* backlog/lag that does not self-recover
* investigation query performance degradation despite current indexes
* replay/recovery/archive jobs becoming operationally unsafe
* incident investigations repeatedly stressing the same recent-window queries

---

## 11) ERD (dbdiagram.io)

See: `../diagrams/erd/audit-v1.dbml`

How to render:

1. Open dbdiagram.io
2. Copy DBML content from the file above
3. Paste into dbdiagram.io to view/export
