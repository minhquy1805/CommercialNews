# Authorization — API Surface (V1)

Base path (Admin): `/api/v1/admin/authz`

> All endpoints in this module are **Admin APIs** and require Bearer auth + explicit policies.
> Governance-changing endpoints must emit audit or governance events asynchronously.
> Success of a governance write means **Authorization truth committed**; it does not mean downstream audit, cache, or materialization already completed.

---

## 1) Roles

### GET `/roles`

List roles.

**Query**

* `page`
* `pageSize`
* `q` (optional)
* `sort` allowlist (default `name`)

**Response (200)**

```json
{
  "items": [
    {
      "roleId": "string",
      "name": "Admin",
      "description": "..."
    }
  ],
  "pageInfo": {
    "page": 1,
    "pageSize": 50,
    "totalItems": 4,
    "totalPages": 1
  }
}
```

### POST `/roles`

Create a role.

**Headers**

* `Idempotency-Key` (recommended)
* `X-Correlation-Id` (optional)

**Request**

```json
{
  "name": "Editor",
  "description": "Can edit content"
}
```

**Response (201)**

```json
{
  "roleId": "string",
  "name": "Editor"
}
```

**Rules**

* Role name uniqueness must be enforced at the truth boundary.
* If create is retried with the same semantic intent, the result must converge safely.
* If role creation emits downstream events, they must be post-commit and retry-safe.

### PUT `/roles/{roleId}`

Update role metadata.

**Headers**

* `Idempotency-Key` (recommended for retry-prone clients)
* `If-Match` or revision token (optional, if optimistic concurrency is used)
* `X-Correlation-Id` (optional)

**Request**

```json
{
  "name": "Editor",
  "description": "Can edit content"
}
```

**Response (200)**

```json
{
  "updated": true
}
```

**Rules**

* If load-then-save editing is supported, stale update protection should be applied.
* Metadata update success does not wait for downstream audit or reporting completion.

### DELETE `/roles/{roleId}`

Delete a role (policy-defined: hard delete or soft delete in V1).

**Response (200)**

```json
{
  "deleted": true
}
```

**Rules**

* Invariant checks must be enforced before delete.
* The system must not leave dangerous orphan governance state silently.
* Repeated equivalent delete should converge safely as a no-op or documented conflict.

---

## 2) Permissions

### GET `/permissions`

List permissions.

**Query**

* `page`
* `pageSize`
* `q` (optional)
* `sort` allowlist (default `name`)

**Response (200)**

Standard list envelope.

### POST `/permissions`

Create a permission (optional if permissions are seeded-only in V1).

**Headers**

* `Idempotency-Key` (recommended)
* `X-Correlation-Id` (optional)

**Request**

```json
{
  "name": "content:publish",
  "description": "Can publish articles"
}
```

**Response (201)**

```json
{
  "permissionId": "string",
  "name": "content:publish"
}
```

**Rules**

* Permission name uniqueness must be enforced in Authorization truth.
* A seed-only posture is allowed in V1; if so, this endpoint should be documented as disabled or not exposed.

### PUT `/permissions/{permissionId}`

Update permission metadata.

**Headers**

* `If-Match` or revision token (optional)
* `X-Correlation-Id` (optional)

**Request**

```json
{
  "name": "content:publish",
  "description": "Can publish articles"
}
```

**Response (200)**

```json
{
  "updated": true
}
```

**Rules**

* Permission naming and versioning must remain stable by policy.
* If stale edit protection is used, outdated updates must return deterministic conflict behavior.

### DELETE `/permissions/{permissionId}`

Delete a permission (rare; often avoided by policy).

**Response (200)**

```json
{
  "deleted": true
}
```

**Rules**

* Dependent role-permission assignments and protected invariants must be validated before delete.
* Repeated equivalent delete should converge safely.

---

## 3) Role-to-permission grants

### GET `/roles/{roleId}/permissions`

List permissions granted to a role.

**Response (200)**

Standard list envelope.

### POST `/roles/{roleId}/permissions`

Grant a permission to a role.

**Headers**

* `Idempotency-Key` (recommended)
* `X-Correlation-Id` (optional)

**Request**

```json
{
  "permissionId": "string"
}
```

**Response (200)**

```json
{
  "granted": true
}
```

**Rules**

* Grant is idempotent.
* Repeating the same grant must not create duplicate truth rows.
* Logically unchanged truth must not emit duplicate harmful downstream effects.
* API success means governance truth committed; audit visibility may still lag.

**Conflict and safety**

* If the grant violates protected governance invariants or references invalid truth, return a documented error or conflict.
* If timeout occurs, callers must reconcile from Authorization truth, not from downstream audit visibility.

### DELETE `/roles/{roleId}/permissions/{permissionId}`

Revoke a permission from a role.

**Response (200)**

```json
{
  "granted": false
}
```

**Rules**

* Revoke should converge safely as a no-op or documented conflict.
* Revocation truth must take effect immediately in authoritative evaluation.
* Downstream lag must not preserve stale effective authority on critical paths.

---

## 4) User-to-role assignments

### GET `/users/{userId}/roles`

List roles assigned to a user.

**Response (200)**

Standard list envelope.

**Rules**

* For immediate post-write governance reads, prefer authoritative truth over stale cache or materialization.

### POST `/users/{userId}/roles`

Assign a role to a user.

**Headers**

* `Idempotency-Key` (recommended)
* `X-Correlation-Id` (optional)

**Request**

```json
{
  "roleId": "string"
}
```

**Response (200)**

```json
{
  "assigned": true
}
```

**Rules**

* Assignment is idempotent.
* `(UserId, RoleId)` uniqueness must be enforced in the truth store.
* Repeated equivalent assignment must not create duplicate rows or duplicate harmful side effects.
* Success does not wait for audit completion.

### DELETE `/users/{userId}/roles/{roleId}`

Revoke a role from a user.

**Response (200)**

```json
{
  "assigned": false
}
```

**Rules**

* Revoke should converge safely as a no-op or documented conflict.
* Revocation must be reflected immediately in truth-backed evaluation for critical or admin paths.
* Timeout ambiguity must be reconciled from Authorization truth.

---

## 5) Policy evaluation (optional diagnostic API)

In V1, policy evaluation primarily happens inside the API component via middleware and authorization handlers.
If an evaluation endpoint is needed for diagnostics:

### POST `/evaluate` (optional)

**Request**

```json
{
  "permission": "content:publish",
  "subject": {
    "userId": "string",
    "roles": ["Admin"]
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

**Response (200)**

```json
{
  "allowed": true,
  "reason": "PolicySatisfied"
}
```

**Rules**

* Diagnostic only unless explicitly promoted to a supported product surface.
* The endpoint must not rely on client-supplied roles as authoritative truth.
* If evaluation uses caches or materializations, uncertainty on security-sensitive paths must fail closed or fall back to authoritative truth.
* Response should not leak unnecessary policy internals.

---

## 6) Consistency and retry semantics

### Governance write endpoints

These endpoints should be treated as:

* truth-first
* idempotent where logically applicable
* safe under client retry
* non-blocking with respect to audit and reporting side effects

### Recommended endpoint behavior

* repeated assign or grant → stable success or no-op semantics
* repeated revoke → stable success or no-op semantics
* uniqueness or invariant violations → deterministic conflict or error response
* timeout ambiguity → reconcile from truth, not from audit, cache, or report state

### Read-your-writes expectation

For admin governance flows:

* post-write reads must reflect current truth
* caches and materializations must not be trusted blindly for immediate confirmation

---

## 7) Versioning and conventions

* All endpoints use `/api/v1/admin/authz/*`.
* List responses follow `{ items[], pageInfo{} }`.
* Errors follow the standard error envelope.
* Governance-changing endpoints should include stable correlation support.
* If revision or version markers are used internally, conflict behavior should be exposed consistently rather than silently using last-write-wins.

---

## 8) Rate limiting and abuse controls

Admin endpoints are not public, but they still need protection against:

* accidental loops
* admin token misuse
* retry storms from automation
* bulk governance mutation mistakes

**Rules**

* Rate limiting policy is implementation-defined; anomalies should be logged and alerted.
* Recommended signals include:

  * bursty repeated assigns, grants, or revokes
  * unusual no-op or idempotent hit rate
  * spikes in `403`, `409`, or `5xx`
  * repeated operations against the same user, role, or permission scope

---

## 9) No direct dependency on downstream completion

Authorization APIs do not guarantee that, at response time:

* an audit record is already queryable
* cache invalidation has already propagated
* a derived policy snapshot has already been rebuilt
* a governance report already reflects the change

They guarantee:

* Authorization truth committed successfully
* downstream side effects were emitted through the standard async path where applicable
