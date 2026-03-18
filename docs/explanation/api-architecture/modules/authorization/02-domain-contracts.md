
---

## `docs/explanation/api-architecture/modules/authorization/02-domain-contracts.md`

```md
# Authorization — Domain Contracts (V1)

## 1) Ownership
Authorization owns:
- Role
- Permission
- UserRole (assignment)
- RolePermission (grant)
- Policy evaluation boundary (centralized enforcement)

## 2) Entities (conceptual)

### Role
- RoleId
- Name (unique)
- Description
- IsSystem? (optional; protects built-in roles)

### Permission
- PermissionId
- Name (unique) — e.g. `content:publish`
- Description
- Module/Area (optional classification)

### UserRole
- UserId
- RoleId
- AssignedAt
- AssignedBy? (actor)

### RolePermission
- RoleId
- PermissionId
- GrantedAt
- GrantedBy? (actor)

## 3) Invariants (must hold)
- Role names are unique.
- Permission names are unique.
- Assignments and grants are idempotent:
  - assigning the same role twice does not duplicate
  - granting the same permission twice does not duplicate
- Built-in roles/permissions may be protected from deletion by policy.

## 4) Policy coverage invariant (system-level)
- 100% of `/api/v1/admin/*` endpoints must enforce explicit authorization policies.
- Authorization checks must be centralized (avoid scattered checks).

## 5) Domain events (for audit)
Authorization emits governance events (examples):
- `RoleCreated`, `RoleUpdated`, `RoleDeleted`
- `PermissionCreated`, `PermissionUpdated`, `PermissionDeleted`
- `RolePermissionGranted`, `RolePermissionRevoked`
- `UserRoleAssigned`, `UserRoleRevoked`

Events must be minimal and privacy-aware:
- include `ActorUserId`, target identifiers, timestamps, correlationId.