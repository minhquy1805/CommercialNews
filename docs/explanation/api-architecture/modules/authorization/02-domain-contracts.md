# Authorization — Domain Contracts (V1)

## 1) Ownership

Authorization owns:

- `Role`
- `Permission`
- `UserRole` (role assignment truth)
- `RolePermission` (permission grant truth)
- centralized permission model for admin/governance access
- policy evaluation boundary for authorization decisions based on authoritative truth

Authorization is the canonical source of truth for:

- which roles exist
- which permissions exist
- which roles are assigned to which users
- which permissions are granted to which roles
- which effective permissions can be derived from active roles and active grants

Authorization does **not** own business resource state such as:

- article authorship
- comment ownership
- media ownership
- draft/publication state

Those resource-specific checks remain in their owning modules, but must compose with centralized authorization.

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
- derivation of effective permissions from current truth

Authorization must **not** rely on:

- client-supplied roles as authoritative truth
- stale cache alone for critical admin/governance decisions
- JWT scopes alone as a replacement for permission checks

---

## 3) Entities (conceptual contract)

### 3.1 Role

A Role represents a named governance grouping of permissions.

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

A Permission represents a fine-grained authority such as `content:publish`.

**Fields**
- `PermissionId`
- `Name`
- `NameNormalized`
- `Description`
- `Module` or `Area` (optional classification)
- `IsSystem`
- `IsActive`
- `CreatedAt`
- `UpdatedAt`
- `CreatedByUserId?`
- `UpdatedByUserId?`

**Notes**
- Permission naming must be stable and policy-governed.
- `NameNormalized` is used for uniqueness and deterministic lookup.
- Inactive permissions must not contribute to effective authorization.

---

### 3.3 UserRole

A UserRole represents assignment of a role to a user.

**Fields**
- `UserRoleId` or equivalent stable assignment identity
- `UserId`
- `RoleId`
- `AssignedAt`
- `AssignedByUserId?`
- `RevokedAt?`
- `RevokedByUserId?`

**Notes**
- A UserRole is active when `RevokedAt` is null.
- Re-assigning an already active `(UserId, RoleId)` pair must be idempotent.
- Revoked assignments must not contribute to effective permission evaluation.

---

### 3.4 RolePermission

A RolePermission represents granting a permission to a role.

**Fields**
- `RolePermissionId` or equivalent stable grant identity
- `RoleId`
- `PermissionId`
- `GrantedAt`
- `GrantedByUserId?`
- `RevokedAt?`
- `RevokedByUserId?`

**Notes**
- A RolePermission is active when `RevokedAt` is null.
- Re-granting an already active `(RoleId, PermissionId)` pair must be idempotent.
- Revoked grants must not contribute to effective permission evaluation.

---

## 4) Lifecycle rules

### 4.1 Role lifecycle
Roles support lifecycle transitions through:

- create
- update metadata
- activate
- deactivate

**V1 posture**
- `activate/deactivate` is the preferred lifecycle model for roles.
- physical delete is discouraged for normal governance operations.
- if delete exists internally or operationally, it must remain policy-controlled and must not violate protected invariants.

### 4.2 Permission lifecycle
Permissions support lifecycle transitions through:

- create
- update metadata
- activate
- deactivate

**V1 posture**
- `activate/deactivate` is the preferred lifecycle model for permissions.
- physical delete is discouraged for normal governance operations.
- seeded/system permissions may be protected from update, deactivation, or deletion by policy.

### 4.3 Assignment and grant lifecycle
Assignments and grants support lifecycle transitions through:

- assign / grant
- revoke
- optional re-activate by creating or restoring an active truth state according to implementation policy

Repeated equivalent operations must converge safely.

---

## 5) Invariants (must hold)

### 5.1 Uniqueness invariants
- Role names are unique by `NameNormalized`.
- Permission names are unique by `NameNormalized`.

Examples that must converge to the same canonical identity:
- `Admin`
- `admin`
- ` ADMIN `

### 5.2 Idempotency invariants
- Assigning the same active role to the same user twice must not create duplicate truth.
- Granting the same active permission to the same role twice must not create duplicate truth.
- Repeating an equivalent revoke must converge safely as a no-op or documented deterministic result.

### 5.3 Active-state invariants
- Inactive roles must not contribute to effective permissions.
- Inactive permissions must not contribute to effective permissions.
- Revoked `UserRole` records must not contribute to effective permissions.
- Revoked `RolePermission` records must not contribute to effective permissions.

### 5.4 Protected-system invariants
- System roles may be protected from rename, deactivation, or deletion by policy.
- System permissions may be protected from rename, deactivation, or deletion by policy.
- Protected governance relationships may be restricted from unsafe mutation by policy.

### 5.5 Referential invariants
- A role assignment must reference a valid role.
- A permission grant must reference a valid permission.
- A permission grant must reference a valid role.
- Authorization truth must not silently create dangling governance relationships.

### 5.6 Effective-permission invariant
A user’s effective permissions are derived only from:

- active roles assigned to that user
- active permissions granted to those roles
- current authoritative truth

Inactive, revoked, or invalid relationships must not grant authority.

---

## 6) Policy coverage invariant (system-level)

- 100% of `/api/v1/admin/*` endpoints must enforce explicit authorization policies.
- Authorization checks must be deny-by-default.
- Authorization checks must be centralized and auditable.
- Client identity alone is never sufficient; permission/policy enforcement is required.
- Object-level checks must be composed with centralized authorization where business resources are involved.

---

## 7) Evaluation boundary contract

Authorization provides the canonical truth and decision inputs for policy enforcement.

### 7.1 Centralized boundary
Authorization owns:
- role and permission truth
- assignment and grant truth
- effective permission derivation inputs
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

Authorization emits minimal, privacy-aware governance events such as:

### Role events
- `RoleCreated`
- `RoleUpdated`
- `RoleActivated`
- `RoleDeactivated`

### Permission events
- `PermissionCreated`
- `PermissionUpdated`
- `PermissionActivated`
- `PermissionDeactivated`

### Relationship events
- `RolePermissionGranted`
- `RolePermissionRevoked`
- `UserRoleAssigned`
- `UserRoleRevoked`

### Event rules
Events must be:
- post-commit relative to authorization truth
- minimal
- privacy-aware
- safe for async handling and retry

Events should include only the minimum necessary data, such as:
- `ActorUserId`
- target identifiers
- timestamp
- correlation id

Events must not expose unnecessary sensitive data.

---

## 9) Read/write truth contract

### Write success means
For governance-changing operations, success means:
- authorization truth committed successfully

Write success does **not** guarantee that:
- an audit record is already queryable
- cache invalidation has already propagated
- a derived policy snapshot has already been rebuilt
- downstream reporting already reflects the change

### Read-after-write requirement
For admin governance confirmation flows:
- post-write reads must reflect authoritative current truth
- immediate reconciliation must use authorization truth, not downstream audit visibility

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

V1 does not require full externalized policy engines or advanced ABAC as the primary enforcement model.
If ABAC contexts are introduced later, they must compose with — not replace — authoritative authorization truth.