# Authorization — API Surface (V1)

Base path (Admin): `/api/v1/admin/authz`

> All endpoints in this module are **Admin APIs** and require Bearer auth + explicit policies.  
> Governance-changing endpoints must emit audit/governance events asynchronously through the standard post-commit path.  
> Success of a governance write means **Authorization truth committed**. It does **not** mean downstream audit, cache invalidation, or derived materialization has already completed.

---

## 1) API posture in V1

Authorization V1 focuses on the synchronous governance truth lane.

### Included in V1

- role management
- permission management
- user-role assignment management
- role-permission grant management
- truth-backed governance reads
- truth-backed policy evaluation inputs for admin authorization

### Optional in V1

- diagnostic evaluation endpoint
- permission/module classification fields
- revision/version markers if implemented

### Not primary in V1

- physical delete as the normal lifecycle path for roles/permissions
- advanced reconciliation/reporting APIs
- derived governance snapshot publication APIs

**Rule:** lifecycle for `Role` and `Permission` is primarily managed through:

- create
- update metadata
- activate
- deactivate

Physical delete, if supported internally, is policy-controlled and not part of the normal V1 admin surface.

---

## 2) Common conventions

### 2.1 Response envelope

List endpoints return:

```json
{
  "items": [],
  "pageInfo": {
    "page": 1,
    "pageSize": 20,
    "totalItems": 0,
    "totalPages": 0
  }
}
```

Errors follow the standard error envelope from:

- `../../02-contracts-and-standards.md`

### 2.2 Headers

Recommended headers for governance-changing endpoints:

- `Idempotency-Key`
- `X-Correlation-Id`

Optional for metadata update endpoints where optimistic concurrency is introduced:

- `If-Match`

### 2.3 Read-after-write posture

For admin governance flows:

- post-write reads must reconcile from authoritative Authorization truth
- immediate confirmation must not depend on audit visibility, cache freshness, or downstream derived outputs

### 2.4 Idempotency posture

The following operations should converge safely under retry:

- assign role
- revoke role
- grant permission
- revoke permission
- activate role
- deactivate role
- activate permission
- deactivate permission

Timeout ambiguity must be reconciled from Authorization truth.

---

## 3) Roles

### `GET /roles`

List roles.

#### Query

- `page`
- `pageSize`
- `q` (optional)
- `sort` allowlist (default `name`)

#### Response (200)

```json
{
  "items": [
    {
      "roleId": "string",
      "name": "Admin",
      "description": "System administrator role",
      "isSystem": true,
      "isActive": true
    }
  ],
  "pageInfo": {
    "page": 1,
    "pageSize": 50,
    "totalItems": 1,
    "totalPages": 1
  }
}
```

#### Rules

- role reads for governance flows should use authoritative truth
- undocumented sort fields must be rejected deterministically

### `POST /roles`

Create a role.

#### Headers

- `Idempotency-Key` (recommended)
- `X-Correlation-Id` (optional)

#### Request

```json
{
  "name": "Editor",
  "description": "Can manage editorial content"
}
```

#### Response (201)

```json
{
  "roleId": "string",
  "name": "Editor"
}
```

#### Rules

- role name uniqueness must be enforced at the truth boundary
- uniqueness should be based on canonical/normalized name semantics
- create success means truth committed
- downstream audit/materialization may still lag

### `PUT /roles/{roleId}`

Update role metadata.

#### Headers

- `Idempotency-Key` (recommended for retry-prone clients)
- `If-Match` (optional, if optimistic concurrency is used)
- `X-Correlation-Id` (optional)

#### Request

```json
{
  "name": "Editor",
  "description": "Can manage editorial content"
}
```

#### Response (200)

```json
{
  "updated": true
}
```

#### Rules

- protected system roles may restrict metadata mutation by policy
- stale update protection should be used if load-then-save editing is supported
- update success does not wait for downstream audit/reporting completion

### `POST /roles/{roleId}:activate`

Activate a role.

#### Headers

- `Idempotency-Key` (recommended)
- `X-Correlation-Id` (optional)

#### Response (200)

```json
{
  "isActive": true
}
```

#### Rules

- activation should converge safely under retry
- activating an already active role should return stable success or documented no-op semantics
- active role state participates in effective permission evaluation

### `POST /roles/{roleId}:deactivate`

Deactivate a role.

#### Headers

- `Idempotency-Key` (recommended)
- `X-Correlation-Id` (optional)

#### Response (200)

```json
{
  "isActive": false
}
```

#### Rules

- deactivation must take effect immediately in authoritative evaluation
- system/protected roles may restrict deactivation by policy
- repeated equivalent deactivation should converge safely
- stale derived state must not continue granting dangerous access on critical paths

---

## 4) Permissions

### `GET /permissions`

List permissions.

#### Query

- `page`
- `pageSize`
- `q` (optional)
- `sort` allowlist (default `name`)

#### Response (200)

```json
{
  "items": [
    {
      "permissionId": "string",
      "name": "content:publish",
      "description": "Can publish content",
      "module": "content",
      "isSystem": true,
      "isActive": true
    }
  ],
  "pageInfo": {
    "page": 1,
    "pageSize": 50,
    "totalItems": 1,
    "totalPages": 1
  }
}
```

#### Rules

- permission naming must remain stable by policy
- undocumented sort fields must be rejected deterministically

### `POST /permissions`

Create a permission.

#### Headers

- `Idempotency-Key` (recommended)
- `X-Correlation-Id` (optional)

#### Request

```json
{
  "name": "content:publish",
  "description": "Can publish content",
  "module": "content"
}
```

#### Response (201)

```json
{
  "permissionId": "string",
  "name": "content:publish"
}
```

#### Rules

- permission name uniqueness must be enforced in Authorization truth
- canonical/normalized name comparison should be used
- if the system adopts a seed-only permission posture, this endpoint may be disabled by policy

### `PUT /permissions/{permissionId}`

Update permission metadata.

#### Headers

- `Idempotency-Key` (recommended)
- `If-Match` (optional)
- `X-Correlation-Id` (optional)

#### Request

```json
{
  "name": "content:publish",
  "description": "Can publish content",
  "module": "content"
}
```

#### Response (200)

```json
{
  "updated": true
}
```

#### Rules

- protected system permissions may restrict mutation by policy
- permission naming/versioning must remain stable
- stale updates should return deterministic conflict behavior if optimistic concurrency is enabled

### `POST /permissions/{permissionId}:activate`

Activate a permission.

#### Headers

- `Idempotency-Key` (recommended)
- `X-Correlation-Id` (optional)

#### Response (200)

```json
{
  "isActive": true
}
```

#### Rules

- activation should converge safely under retry
- activating an already active permission should return stable success or documented no-op semantics

### `POST /permissions/{permissionId}:deactivate`

Deactivate a permission.

#### Headers

- `Idempotency-Key` (recommended)
- `X-Correlation-Id` (optional)

#### Response (200)

```json
{
  "isActive": false
}
```

#### Rules

- deactivation must take effect immediately in authoritative evaluation
- protected system permissions may restrict deactivation by policy
- stale derived permission views must not override committed truth

---

## 5) Role-to-permission grants

### `GET /roles/{roleId}/permissions`

List permissions granted to a role.

#### Response (200)

```json
{
  "items": [
    {
      "permissionId": "string",
      "name": "content:publish",
      "description": "Can publish content",
      "module": "content",
      "isActive": true
    }
  ],
  "pageInfo": {
    "page": 1,
    "pageSize": 50,
    "totalItems": 1,
    "totalPages": 1
  }
}
```

#### Rules

- for immediate post-write governance reads, use authoritative truth
- inactive or revoked relationships must not appear as active authority unless explicitly documented

### `POST /roles/{roleId}/permissions`

Grant a permission to a role.

#### Headers

- `Idempotency-Key` (recommended)
- `X-Correlation-Id` (optional)

#### Request

```json
{
  "permissionId": "string"
}
```

#### Response (200)

```json
{
  "granted": true
}
```

#### Rules

- grant is idempotent
- repeating the same active grant must not create duplicate truth rows
- logically unchanged truth should not emit duplicate harmful downstream effects
- success means truth committed; audit/materialization may still lag

#### Conflict and safety

- if the target role or permission is invalid, inactive, or protected by policy, return a documented error or conflict
- timeout ambiguity must be reconciled from Authorization truth

### `DELETE /roles/{roleId}/permissions/{permissionId}`

Revoke a permission from a role.

#### Response (200)

```json
{
  "granted": false
}
```

#### Rules

- revoke must converge safely under retry
- revocation truth must take effect immediately in authoritative evaluation
- downstream lag must not preserve stale effective authority on critical paths
- replay of older grant-derived state must not resurrect revoked authority

---

## 6) User-to-role assignments

### `GET /users/{userId}/roles`

List roles assigned to a user.

#### Response (200)

```json
{
  "items": [
    {
      "roleId": "string",
      "name": "Editor",
      "description": "Can manage editorial content",
      "isActive": true
    }
  ],
  "pageInfo": {
    "page": 1,
    "pageSize": 50,
    "totalItems": 1,
    "totalPages": 1
  }
}
```

#### Rules

- for immediate post-write governance reads, prefer authoritative truth over stale cache/materialization
- revoked assignments must not continue granting authority

### `POST /users/{userId}/roles`

Assign a role to a user.

#### Headers

- `Idempotency-Key` (recommended)
- `X-Correlation-Id` (optional)

#### Request

```json
{
  "roleId": "string"
}
```

#### Response (200)

```json
{
  "assigned": true
}
```

#### Rules

- assignment is idempotent
- only one active `(UserId, RoleId)` truth relationship may exist at a time
- repeated equivalent assignment must not create duplicate rows or duplicate harmful side effects
- success does not wait for downstream audit completion

### `DELETE /users/{userId}/roles/{roleId}`

Revoke a role from a user.

#### Response (200)

```json
{
  "assigned": false
}
```

#### Rules

- revoke should converge safely as no-op or documented stable result
- revocation must be reflected immediately in truth-backed evaluation for critical/admin paths
- timeout ambiguity must be reconciled from Authorization truth
- replay or stale derived views must not reintroduce revoked authority

---

## 7) Effective permission read (recommended admin diagnostic read)

### `GET /users/{userId}/effective-permissions`

Return the effective permissions currently derived for a user from authoritative truth.

#### Response (200)

```json
{
  "items": [
    {
      "permissionId": "string",
      "name": "content:publish",
      "module": "content"
    }
  ],
  "pageInfo": {
    "page": 1,
    "pageSize": 100,
    "totalItems": 1,
    "totalPages": 1
  }
}
```

#### Rules

- this endpoint is a truth-backed governance read
- effective permissions must be derived only from active roles, active permissions, and active non-revoked relationships
- security-sensitive confirmation should not depend solely on cache/materialized views

---

## 8) Policy evaluation (optional diagnostic API)

In V1, policy evaluation primarily happens inside the API component via authorization handlers and middleware.  
If an evaluation endpoint is needed for diagnostics:

### `POST /evaluate` (optional)

#### Request

```json
{
  "permission": "content:publish",
  "subject": {
    "userId": "string"
  },
  "resource": {
    "type": "Article",
    "id": "string"
  },
  "environment": {
    "now": "2026-03-02T10:30:00Z"
  }
}
```

#### Response (200)

```json
{
  "allowed": true,
  "reason": "PolicySatisfied"
}
```

#### Rules

- diagnostic only unless explicitly promoted to a supported product surface
- the endpoint must not trust client-supplied roles as authoritative truth
- if evaluation uses cache/materialized inputs, uncertainty on security-sensitive paths must fail closed or fall back to authoritative truth
- response must not leak unnecessary policy internals

---

## 9) Consistency and retry semantics

### Governance write endpoints

These endpoints should be treated as:

- truth-first
- idempotent where logically applicable
- safe under client retry
- non-blocking with respect to audit/reporting side effects

### Recommended endpoint behavior

- repeated assign or grant → stable success or no-op semantics
- repeated revoke → stable success or no-op semantics
- repeated activate/deactivate → stable success or no-op semantics
- uniqueness or invariant violations → deterministic conflict or documented error response
- timeout ambiguity → reconcile from truth, not from audit, cache, or report state

### Read-your-writes expectation

For admin governance flows:

- post-write reads must reflect current truth
- caches/materializations must not be trusted blindly for immediate confirmation

---

## 10) Rate limiting and abuse controls

Admin endpoints are not public, but they still need protection against:

- accidental loops
- admin token misuse
- retry storms from automation
- bulk governance mutation mistakes

#### Rules

- rate limiting policy is implementation-defined
- anomalies should be logged and alerted

Useful signals include:

- bursty repeated assigns, grants, revokes, activates, or deactivates
- unusual no-op or idempotent hit rates
- spikes in `403`, `409`, or `5xx`
- repeated operations against the same user, role, or permission scope

---

## 11) No direct dependency on downstream completion

Authorization APIs do not guarantee that, at response time:

- an audit record is already queryable
- cache invalidation has already propagated
- a derived policy snapshot has already been rebuilt
- a governance report already reflects the change

They guarantee:

- Authorization truth committed successfully
- downstream side effects were emitted through the standard async path where applicable