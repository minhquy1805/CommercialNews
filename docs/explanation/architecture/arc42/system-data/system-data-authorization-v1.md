# System Data Model — Authorization (V1)

> **Recommended file:** `explanation/architecture/arc42/system-data/system-data-authorization-v1.md`  
> **Module:** Authorization (Roles / Permissions / Policy)  
> **Purpose:** Provide RBAC foundation (roles/permissions) to protect endpoints and admin workflows.  
> **V1 scope:** roles + permissions + assignments. Audit of changes is handled by the Audit module.

---

## 0) Data System fit (V1)

Authorization is a **governance-critical OLTP module**.

- **Truth store:** SQL Server (strong integrity + join path for evaluations)
- **Read path coupling:** public read endpoints should not depend on authorization checks (admin endpoints do)
- **Audit:** every grant/revoke must emit audit events (async), but the assignment itself must remain correct and durable
- **Performance:** evaluation path must be cheap: `UserId → Roles → Permissions`

**Non-negotiables (from Quality Requirements)**
- 100% policy coverage on admin endpoints
- Least privilege (deny by default)
- Changes must be traceable (AuditTrail)

---

## 1) Scope & boundaries (V1)

### In scope (V1)
- Role definitions
- Permission definitions (stable keys)
- Assign roles to users
- Grant permissions to roles

### Out of scope (V2+)
- Role hierarchy/inheritance
- Fine-grained policy snapshots/claims caching as a first-class model
- Multi-tenant scoping of roles/permissions

### Cross-module references
- `UserId`, `AssignedBy`, `GrantedBy` reference Identity users (contract-level)
- Audit trail is owned by Audit module (no local audit tables required in V1)

---

## 2) Capability → Entity mapping

### 2.1 Roles / Permissions (assign/revoke)
**Entities**
- `Role`
- `Permission`
- `UserRole`
- `RolePermission`

**Audit**
- V1 does not require RoleChangeEvent/PermissionChangeEvent tables if you already have `AuditLog`.
- Every grant/revoke must emit an audit record/event (handled by Audit module).

### 2.2 Endpoint protection and admin workflows
- No `AdminPanel` table is needed (UI/use-case concern).
- DB needs **granular permission keys** to guard endpoints:
  - `Content:Publish`, `Content:Unpublish`, `User:Deactivate`, `Comment:Hide`, ...

---

## 3) Workload & evaluation path (V1) (DDIA Ch3)

### 3.1 Hot path: policy evaluation
The core evaluation path is:

`UserId → UserRole → RolePermission → Permission`

Design goal:
- predictable performance under admin traffic
- low join cost using composite PKs

### 3.2 Admin operations
- Create/update roles and permissions (rare)
- Assign/revoke user roles (moderate)
- Grant/revoke role permissions (moderate)

---

## 4) Dataflows (V1) — REST / DB / Broker (DDIA Ch4)

### 4.1 Assign role to user (sync OLTP)
- API validates caller permission (admin)
- Write `UserRole(UserId, RoleId)` idempotently (composite PK)
- Emit audit event `Auth.UserRoleGranted` / `Auth.UserRoleRevoked` (async)

### 4.2 Grant permission to role (sync OLTP)
- Write `RolePermission(RoleId, PermissionId)` idempotently
- Emit audit event `Auth.RolePermissionGranted` / `Auth.RolePermissionRevoked` (async)

**Failure behavior**
- Governance change must succeed even if Audit ingestion is delayed.
- Audit backlog must be observable (success/failure/backlog).

---

## 5) Invariants (V1 rules)

1. **Least privilege (deny by default)**
- If a user lacks permission → deny.

2. **Permission keys are stable identifiers**
- `Permission.Key` is unique and should not be renamed casually.

3. **Assignments are idempotent**
- Assigning twice does not create duplicates.
- Revoking missing assignments is a no-op (logic-level).

4. **Governance actions are auditable**
- Role/permission grant/revoke must produce `AuditLog` entries (via events).

5. **Naming policy**
- Standardize permission keys as `Module:Action` (recommended) or `Module.Action` (ADR candidate).

---

## 6) Entities (Logical schema) — SQL Server (V1)

### 6.1 `Role`
| Field | Type | Null | Default | Notes |
|---|---|---:|---|---|
| RoleId | BIGINT IDENTITY | NO |  | PK |
| Name | NVARCHAR(80) | NO |  | unique (Admin/User/Moderator/Author…) |
| DisplayName | NVARCHAR(120) | YES |  | UI |
| Description | NVARCHAR(300) | YES |  | |
| IsSystem | BIT | NO | `0` | protect system roles |
| IsActive | BIT | NO | `1` | |
| CreatedAt | DATETIME2(3) | NO | SYSUTCDATETIME() | |
| UpdatedAt | DATETIME2(3) | NO | SYSUTCDATETIME() | |

### 6.2 `Permission`
| Field | Type | Null | Default | Notes |
|---|---|---:|---|---|
| PermissionId | BIGINT IDENTITY | NO |  | PK |
| Key | NVARCHAR(120) | NO |  | unique: `Content:Publish` |
| Module | NVARCHAR(50) | YES |  | optional grouping |
| Action | NVARCHAR(50) | YES |  | optional |
| Description | NVARCHAR(300) | YES |  | |
| IsActive | BIT | NO | `1` | |
| CreatedAt | DATETIME2(3) | NO | SYSUTCDATETIME() | |

### 6.3 `UserRole`
> Composite key ensures idempotent assignment by design.

| Field | Type | Null | Default | Notes |
|---|---|---:|---|---|
| UserId | BIGINT | NO |  | PK part, ref Identity.UserAccount |
| RoleId | BIGINT | NO |  | PK part |
| AssignedAt | DATETIME2(3) | NO | SYSUTCDATETIME() | |
| AssignedBy | BIGINT | YES |  | actor user |

### 6.4 `RolePermission`
| Field | Type | Null | Default | Notes |
|---|---|---:|---|---|
| RoleId | BIGINT | NO |  | PK part |
| PermissionId | BIGINT | NO |  | PK part |
| GrantedAt | DATETIME2(3) | NO | SYSUTCDATETIME() | |
| GrantedBy | BIGINT | YES |  | actor user |

**V2 hooks**
- `TenantId` (multi-tenant)
- `ValidFrom/ValidTo` (time-bound grants)

---

## 7) Constraints & indexes — Authorization (V1)

### 7.1 PK
- `PK_Role(RoleId)`
- `PK_Permission(PermissionId)`
- `PK_UserRole(UserId, RoleId)` (composite)
- `PK_RolePermission(RoleId, PermissionId)` (composite)

### 7.2 FK
- `UserRole.RoleId → Role.RoleId`
- `RolePermission.RoleId → Role.RoleId`
- `RolePermission.PermissionId → Permission.PermissionId`
- Optional cross-module FK (only if same DB):
  - `UserRole.UserId → Identity.UserAccount.UserId`
  - `AssignedBy/GrantedBy → Identity.UserAccount.UserId`

### 7.3 UNIQUE
- `UQ_Role_Name(Name)`
- `UQ_Permission_Key(Key)`

### 7.4 Indexes (evaluation + ops)
Composite PKs already make joins cheap. Add indexes only if needed:

- `IX_UserRole_RoleId_UserId` (already in your ERD) helps listing users in a role
- `IX_RolePermission_PermissionId_RoleId` helps reverse lookup (who has a permission)

> Keep indexes minimal until you observe bottlenecks.

---

## 8) Evolution rules (V1) — safe change over time (DDIA Ch4)

- Add-only changes for permissions: prefer **adding new keys** instead of renaming.
- Deprecation strategy:
  - mark `IsActive=0` for a permission key
  - keep the key for history/audit readability
- Keep system roles protected (`IsSystem=1`) and restrict destructive edits (policy)

---

## 9) Retention & operational notes (V1)
- Authorization tables are not high-write; retention typically equals DB retention.
- Governance investigations rely on AuditLog; ensure audit retention meets policy.

---

## 10) V2 hooks
- `PermissionGroup/Module` for UI organization
- `RoleHierarchy` (inheritance)
- `PolicySnapshot` / cached claims (performance)
- Multi-tenant scoping (TenantId)

---

## 11) ADR candidates
- Permission key naming: `Module:Action` vs `Module.Action`
- System roles policy: immutability and edit constraints
- Cross-module FK enforcement: DB FK vs application contract
- Caching strategy for authorization evaluation (claims snapshot vs DB joins)

---

## 12) Partitioning Readiness (V1/V2)

> This section captures **partitioning and evaluation-path scale readiness** for Authorization.
> V1 remains **non-sharded by default**; priority is correctness, cheap policy evaluation, and auditable governance changes.

### 12.1 Why Authorization is a partitioning-risk module

Authorization is a **governance-critical OLTP** module with a strict correctness requirement:

* admin endpoints depend on policy evaluation correctness
* grants/revokes must be durable and auditable
* evaluation path must remain cheap and predictable under admin traffic

**V1 principle:** optimize **join path + indexes + stable permission keys** before any shard complexity.

---

### 12.2 Primary access patterns (V1)

**Hot path (admin policy evaluation)**

* `UserId -> UserRole -> RolePermission -> Permission`
* deny by default when permission is absent

**Admin/write paths**

* assign/revoke user role (idempotent)
* grant/revoke role permission (idempotent)
* manage roles/permissions (rare writes)

**Operational reads**

* list users in a role
* reverse lookup roles by permission (optional ops/admin tooling)

---

### 12.3 Secondary-index-heavy queries (present and future)

**V1**

* reverse lookups (`RoleId -> Users`, `PermissionId -> Roles`) for admin tooling
* role/permission listing and filtering (light/moderate)

**V2+**

* policy snapshots / claims materialization
* tenant-scoped authorization queries
* richer governance analytics and review workflows

**Implication**
Authorization scale issues usually appear first in:

* evaluation path joins
* admin tooling lookups
* governance/read freshness requirements
  rather than immediate truth-table sharding.

---

### 12.4 Candidate partitioning strategy (future)

Authorization partitioning must preserve strict correctness and deny-by-default behavior.

#### A) `Role`, `Permission`, `UserRole`, `RolePermission` (OLTP truth)

**Likely fit:** defer DB partitioning in V1

* preserve simple joins for policy evaluation
* preserve transaction simplicity for grant/revoke operations
* prioritize composite PKs and minimal supporting indexes

#### B) Policy/claims projections (V2+)

**Likely fit:** derived cache/projection before truth sharding

* per-user claims snapshots or cached permission sets
* measurable freshness and invalidation strategy required
* must fail safe (deny by default) under uncertainty

#### C) Audit and governance history (handled by Audit module)

* if governance investigations grow heavy, scale those search/read paths in Audit before touching Authorization truth tables

---

### 12.5 Hotspot and skew risks (V1)

#### A) Evaluation-path hotspots (most important)

* repeated checks for a small set of admin/service accounts
* bursty admin sessions causing repeated `UserId -> Roles -> Permissions` joins

#### B) Governance write bursts

* bulk role assignment/revocation or seeding operations can create short spikes
* audit side effects may lag, but assignments must remain correct

#### C) Reverse-lookup admin skew

* repeated role/member/permission inspection queries during governance changes

---

### 12.6 V1 mitigations (no sharding yet)

CommercialNews V1 already has the correct baseline mitigations for Authorization:

* **Composite PKs** for `UserRole` and `RolePermission` (idempotent writes + cheap joins)
* **Minimal supporting indexes** only where needed (`RoleId->UserId`, `PermissionId->RoleId`)
* **Stable permission keys** (`Permission.Key`) with add-only/deprecate strategy
* **Deny-by-default** semantics (fail safe)
* **Async audit side effects** (grant/revoke correctness does not depend on audit ingestion timing)
* **Audit trail in dedicated Audit module** (keeps Authorization truth path focused)

These are preferred before any shard complexity.

---

### 12.7 V2+ scale options (selective)

Introduce stronger partitioning only when sustained signals justify it.

#### Option A — Claims/policy snapshot cache (recommended first)

* derived per-user or per-session permission materialization
* fast evaluation path for repeated checks
* strict invalidation and freshness policy required
* must preserve deny-by-default behavior when cache is stale/missing

#### Option B — Workload partitioning for governance side effects

* separate/background lanes for audit events or bulk seeding jobs
* keep core grant/revoke OLTP path isolated

#### Option C — Authorization truth partitioning (high bar)

Consider only when:

* evaluation path remains a measured bottleneck despite indexing/caching
* correctness/freshness semantics can be preserved safely
* operational complexity is justified by sustained scale needs

---

### 12.8 Rebalancing and routing readiness (future)

Authorization truth is correctness-sensitive, so any partitioning requires conservative routing/rebalancing policy.

**Likely first scalable units**

* derived claims/policy snapshots (cache/projection)
* governance side-effect lanes (audit/event processing), not core truth joins

**Guardrails**

* must not weaken deny-by-default behavior
* must preserve freshness for admin reads after grant/revoke (read-your-writes expectations)
* must not introduce inconsistent policy decisions under lag/rebalance

---

### 12.9 Partition-readiness observability signals (Authorization)

Use existing V1 measurement and runtime signals to decide when stronger partitioning is needed:

* admin endpoint latency/error rates (especially policy-protected flows)
* authorization evaluation latency (if measured)
* grant/revoke success/failure rates
* audit backlog/lag for governance events (indirect signal; correctness still on truth path)
* reverse-lookup query latency (`Role -> Users`, `Permission -> Roles`) if measured
* cache hit/miss + invalidation lag for claims snapshots (V2+, if introduced)

**Scale trigger (policy-level)**
Consider stronger workload/projection partitioning when sustained pressure causes:

* repeated evaluation-path latency issues despite current indexes and join design
* admin freshness/read-after-write expectations becoming hard to meet under load
* governance tooling queries stressing truth tables operationally

---

## 13) ERD (dbdiagram.io)

See: `../diagrams/erd/authorization-v1.dbml`

How to render:

1. Open dbdiagram.io
2. Copy DBML content from the file above
3. Paste into dbdiagram.io to view/export

> Note: Audit of grant/revoke is handled by the Audit module (no local audit tables required in V1).
