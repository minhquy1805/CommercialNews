# Authorization — Domain Contracts (V1)

## 1) Ownership

Authorization owns:

- `Role`
- `Permission`
- `UserRole` *(role assignment truth)*
- `RolePermission` *(permission grant truth)*
- centralized permission/governance truth for admin authorization
- the authoritative inputs from which effective permissions are computed
- the policy evaluation boundary for authorization decisions based on authoritative truth

Authorization is the canonical source of truth for:

- which roles exist
- which permissions exist
- which roles are assigned to which users
- which permissions are granted to which roles
- which active governance relationships participate in effective permission evaluation

Authorization does **not** own business resource state such as:

- article authorship
- comment ownership
- media ownership
- draft/publication state
- identity/account truth
- notification delivery truth
- audit evidence truth

Those resource-specific checks remain in their owning modules, but must compose with centralized authorization where required.

---

## 2) Domain responsibilities

Authorization is responsible for:

- maintaining governance truth for roles and permissions
- enforcing invariants on role/permission lifecycle
- ensuring assignments and grants are idempotent
- providing truth-backed inputs for policy enforcement
- supporting explicit authorization checks for all `/api/v1/admin/*` endpoints

Authorization must support:

- function-level authorization
- policy-based authorization
- truth-backed computation of effective permissions from current governance truth

Authorization must **not** rely on:

- client-supplied roles as authoritative truth
- stale cache alone for critical admin/governance decisions
- JWT scopes alone as a replacement for permission checks
- optional downstream notification/audit completion as a prerequisite for governance truth

---

## 3) Entities (conceptual contract)

### 3.1 Role

A `Role` represents a named governance grouping of permissions.

**Fields**

- `RoleId`
- `Name`
- `NameNormalized`
- `Description`
- `IsSystem`
- `IsActive`
- `CreatedAt`
- `UpdatedAt`
- `CreatedByUserId?`
- `UpdatedByUserId?`

**Notes**

- `Name` is the human-readable identifier.
- `NameNormalized` is the canonical comparison key used for uniqueness and lookups.
- `IsSystem` marks protected built-in roles.
- `IsActive` controls whether the role participates in effective permission evaluation.

---

### 3.2 Permission

A `Permission` represents a fine-grained authority such as `content:publish`.

**Fields**

- `PermissionId`
- `Name`
- `NameNormalized`
- `Description`
- `Module` or `Area` *(optional classification)*
- `IsSystem`
- `IsActive`
- `CreatedAt`
- `UpdatedAt`
- `CreatedByUserId?`
- `UpdatedByUserId?`

**Notes**

- permission naming must be stable and policy-governed
- `NameNormalized` is used for uniqueness and deterministic lookup
- inactive permissions must not contribute to effective authorization

---

### 3.3 UserRole

A `UserRole` represents assignment of a role to a user.

**Fields**

- `UserRoleId` or equivalent stable assignment identity
- `UserId`
- `RoleId`
- `AssignedAt`
- `AssignedByUserId?`
- `RevokedAt?`
- `RevokedByUserId?`

**Notes**

- a `UserRole` is active when `RevokedAt` is null
- re-assigning an already active `(UserId, RoleId)` pair must be idempotent
- revoked assignments must not contribute to effective permission evaluation

---

### 3.4 RolePermission

A `RolePermission` represents granting a permission to a role.

**Fields**

- `RolePermissionId` or equivalent stable grant identity
- `RoleId`
- `PermissionId`
- `GrantedAt`
- `GrantedByUserId?`
- `RevokedAt?`
- `RevokedByUserId?`

**Notes**

- a `RolePermission` is active when `RevokedAt` is null
- re-granting an already active `(RoleId, PermissionId)` pair must be idempotent
- revoked grants must not contribute to effective permission evaluation

---

## 4) Lifecycle rules

### 4.1 Role lifecycle

Roles support lifecycle transitions through:

- create
- update metadata
- activate
- deactivate

**V1 posture**

- `activate/deactivate` is the preferred lifecycle model for roles
- physical delete is discouraged for normal governance operations
- if delete exists internally or operationally, it must remain policy-controlled and must not violate protected invariants

### 4.2 Permission lifecycle

Permissions support lifecycle transitions through:

- create
- update metadata
- activate
- deactivate

**V1 posture**

- `activate/deactivate` is the preferred lifecycle model for permissions
- physical delete is discouraged for normal governance operations
- seeded/system permissions may be protected from update, deactivation, or deletion by policy

### 4.3 Assignment and grant lifecycle

Assignments and grants support lifecycle transitions through:

- assign / grant
- revoke
- optional re-activate by creating or restoring an active truth state according to implementation policy

Repeated equivalent operations must converge safely.

---

## 5) Invariants (must hold)

### 5.1 Uniqueness invariants

- role names are unique by `NameNormalized`
- permission names are unique by `NameNormalized`

Examples that must converge to the same canonical identity:

- `Admin`
- `admin`
- ` ADMIN `

### 5.2 Idempotency invariants

- assigning the same active role to the same user twice must not create duplicate truth
- granting the same active permission to the same role twice must not create duplicate truth
- repeating an equivalent revoke must converge safely as a no-op or documented deterministic result

### 5.3 Active-state invariants

- inactive roles must not contribute to effective permissions
- inactive permissions must not contribute to effective permissions
- revoked `UserRole` records must not contribute to effective permissions
- revoked `RolePermission` records must not contribute to effective permissions

### 5.4 Protected-system invariants

- system roles may be protected from rename, deactivation, or deletion by policy
- system permissions may be protected from rename, deactivation, or deletion by policy
- protected governance relationships may be restricted from unsafe mutation by policy

### 5.5 Referential invariants

- a role assignment must reference a valid role
- a permission grant must reference a valid permission
- a permission grant must reference a valid role
- authorization truth must not silently create dangling governance relationships

### 5.6 Effective-permission invariant

A user’s effective permissions are computed only from:

- active roles assigned to that user
- active permissions granted to those roles
- current authoritative governance truth

Inactive, revoked, invalid, or stale relationships must not grant authority.

### 5.7 Replay-safety invariant

Replayed or delayed derived state must not resurrect revoked authority or override fresher committed governance truth.

---

## 6) Policy coverage invariant (system-level)

- 100% of `/api/v1/admin/*` endpoints must enforce explicit authorization policies
- authorization checks must be deny-by-default
- authorization checks must be centralized and auditable
- client identity alone is never sufficient; permission/policy enforcement is required
- object-level checks must be composed with centralized authorization where business resources are involved

---

## 7) Evaluation boundary contract

Authorization provides the canonical truth and decision inputs for policy enforcement.

### 7.1 Centralized boundary

Authorization owns:

- role and permission truth
- assignment and grant truth
- authoritative inputs for effective permission computation
- policy decision inputs for governance authorization

### 7.2 Composition with business modules

Business modules may enforce additional resource-specific checks, for example:

- author can edit own draft
- user can edit own comment
- public caller cannot view unpublished content

These checks do not replace centralized authorization.
They compose with it.

### 7.3 Critical-path rule

For admin/governance paths:

- authoritative truth must be preferred over stale cache or lagging materializations
- uncertainty on security-sensitive authorization paths should fail closed or fall back to authoritative truth

---

## 8) Domain events (governance/audit oriented)

Authorization emits minimal, privacy-aware governance events through the standard post-commit async path.

### 8.1 Event identity and transport posture

Async governance events must:

- be emitted only after authorization truth commits
- carry stable `MessageId`
- include `CorrelationId` where available
- be safe under retry, duplicate delivery, and replay
- remain minimal and privacy-aware

### 8.2 Event categories

#### Role events

- `Authz.RoleCreated`
- `Authz.RoleUpdated`
- `Authz.RoleActivated`
- `Authz.RoleDeactivated`

#### Permission events

- `Authz.PermissionCreated`
- `Authz.PermissionUpdated`
- `Authz.PermissionActivated`
- `Authz.PermissionDeactivated`

#### Relationship events

- `Authz.RolePermissionGranted`
- `Authz.RolePermissionRevoked`
- `Authz.UserRoleAssigned`
- `Authz.UserRoleRevoked`

### 8.3 Event payload rules

Events should include only the minimum necessary data, such as:

- `MessageId`
- `ActorUserId?`
- target identifiers
- timestamp
- correlation metadata

Events must not expose unnecessary sensitive data.

### 8.4 Downstream posture

Downstream consumers such as Audit, cache/materialization refreshers, or optional notification consumers must tolerate:

- at-least-once delivery
- duplicate governance events
- delayed governance events
- replayed governance events

These downstream effects do not define governance truth.

---

## 9) Read/write truth contract

### 9.1 Write success means

For governance-changing operations, success means:

- authorization truth committed successfully
- async intent/outbox committed where applicable

Write success does **not** guarantee that:

- an audit record is already queryable
- cache invalidation has already propagated
- a derived policy snapshot has already been rebuilt
- downstream reporting already reflects the change
- optional downstream notifications have already completed

### 9.2 Read-after-write requirement

For admin governance confirmation flows:

- post-write reads must reflect authoritative current truth
- immediate reconciliation must use authorization truth, not downstream audit/cache/materialization visibility

---

## 10) Abuse and safety rules

Authorization must support safe handling of:

- repeated client retries
- automation loops
- no-op repeated grants/assignments/revokes
- protected governance mutation attempts

The model must produce deterministic outcomes for:

- uniqueness violations
- protected-system mutation attempts
- invalid references
- repeated equivalent writes

---

## 11) V1 boundaries and exclusions

V1 includes:

- RBAC-style roles and permissions
- idempotent assignment and grant behavior
- centralized policy boundary for admin authorization

V1 may optionally include:

- diagnostic policy evaluation endpoint
- permission classification by module/area

V1 does not require:

- full externalized policy engines as the primary enforcement model
- advanced ABAC as the primary enforcement model
- synchronous dependency on optional notification side effects

If ABAC contexts are introduced later, they must compose with — not replace — authoritative authorization truth.