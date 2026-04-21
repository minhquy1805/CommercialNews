# Authorization — Errors & Status Codes (V1)

## 1) Standard error envelope

Authorization follows the standard error envelope defined in:

- `../../02-contracts-and-standards.md`

All failures must return:

```json
{
  "traceId": "string",
  "error": {
    "code": "AUTHORIZATION.SOME_ERROR",
    "message": "Human-friendly message",
    "details": []
  }
}
```

Rules:

- `traceId` must always be included for failures.
- `code` must be stable and machine-readable.
- `message` should be safe for clients and must not leak sensitive internals.
- `details` is optional and is primarily used for validation failures.

## 2) Status code mapping

### 2.1 Success codes

#### 200 OK

- successful reads
- successful updates
- successful activate/deactivate actions
- successful assign/grant/revoke actions
- successful idempotent no-op operations when documented as stable success

#### 201 Created

- role created
- permission created

#### 204 No Content

- delete success, only if physical delete is supported and the endpoint contract explicitly uses `204`
- V1 should prefer `200` for relationship revokes when a response body is returned

### 2.2 Client error codes

#### 400 Bad Request

- malformed request
- validation failures
- unsupported sort field
- illegal input values
- invalid lifecycle transition input where treated as request error

#### 401 Unauthorized

- missing bearer token
- invalid token
- expired token
- authentication failed before policy evaluation

#### 403 Forbidden

- authenticated caller lacks required permission/policy
- protected system role/permission mutation denied by policy
- object-level authorization denied when combined with centralized authorization
- fail-closed deny on a security-sensitive path when policy chooses deny semantics

#### 404 Not Found

- role not found
- permission not found
- user not found
- assignment/grant target not found
- safe not-found only; do not leak protected existence unnecessarily

#### 409 Conflict

- role name already exists
- permission name already exists
- optimistic concurrency conflict if used
- protected invariant conflict
- illegal duplicate state that does not qualify as safe idempotent success

#### 429 Too Many Requests

- rate-limited
- retry storm or abuse protection triggered

### 2.3 Server / dependency failure codes

#### 500 Internal Server Error

- unexpected server-side failure

#### 503 Service Unavailable

- required dependency unavailable for authoritative truth path
- should be used carefully
- downstream async failure alone must not force `503`

## 3) Core rule: truth success vs downstream side effects

For governance-changing operations, API success means:

- Authorization truth committed successfully

API success does not guarantee that:

- audit is already queryable
- cache invalidation has propagated
- derived authorization snapshots are rebuilt
- reporting views are updated
- optional downstream notification side effects have completed

Async side-effect delay or retry must not turn a committed governance write into `5xx`.

## 4) Error code catalog (V1 baseline)

### 4.1 Authorization / policy errors

- `AUTHORIZATION.POLICY_DENIED`
- `AUTHORIZATION.AUTHENTICATION_REQUIRED`
- `AUTHORIZATION.FAIL_CLOSED`
- `AUTHORIZATION.OBJECT_ACCESS_DENIED`

### 4.2 Role errors

- `AUTHORIZATION.ROLE_NOT_FOUND`
- `AUTHORIZATION.ROLE_EXISTS`
- `AUTHORIZATION.ROLE_NAME_INVALID`
- `AUTHORIZATION.ROLE_NAME_TOO_LONG`
- `AUTHORIZATION.ROLE_INACTIVE`
- `AUTHORIZATION.SYSTEM_ROLE_PROTECTED`
- `AUTHORIZATION.ROLE_DELETE_NOT_ALLOWED`
- `AUTHORIZATION.ROLE_STATE_CONFLICT`

### 4.3 Permission errors

- `AUTHORIZATION.PERMISSION_NOT_FOUND`
- `AUTHORIZATION.PERMISSION_EXISTS`
- `AUTHORIZATION.PERMISSION_NAME_INVALID`
- `AUTHORIZATION.PERMISSION_NAME_TOO_LONG`
- `AUTHORIZATION.PERMISSION_INACTIVE`
- `AUTHORIZATION.SYSTEM_PERMISSION_PROTECTED`
- `AUTHORIZATION.PERMISSION_DELETE_NOT_ALLOWED`
- `AUTHORIZATION.PERMISSION_STATE_CONFLICT`

### 4.4 User/assignment/grant errors

- `AUTHORIZATION.USER_NOT_FOUND`
- `AUTHORIZATION.USER_ROLE_NOT_FOUND`
- `AUTHORIZATION.ROLE_PERMISSION_NOT_FOUND`
- `AUTHORIZATION.USER_ROLE_ALREADY_ASSIGNED`
- `AUTHORIZATION.ROLE_PERMISSION_ALREADY_GRANTED`
- `AUTHORIZATION.USER_ROLE_REVOKE_CONFLICT`
- `AUTHORIZATION.ROLE_PERMISSION_REVOKE_CONFLICT`
- `AUTHORIZATION.INVALID_ASSIGNMENT_TARGET`
- `AUTHORIZATION.INVALID_GRANT_TARGET`

### 4.5 Query/filter/sort errors

- `AUTHORIZATION.INVALID_PAGING`
- `AUTHORIZATION.INVALID_SORT_FIELD`
- `AUTHORIZATION.INVALID_FILTER`

### 4.6 Generic/system errors

- `AUTHORIZATION.CONCURRENCY_CONFLICT`
- `AUTHORIZATION.DEPENDENCY_FAILURE`
- `AUTHORIZATION.UNEXPECTED_ERROR`

## 5) Idempotency and conflict guidance

### 5.1 Stable idempotent success

These operations should usually return stable success instead of conflict when repeated with the same semantic intent:

- assigning the same active role to the same user
- granting the same active permission to the same role
- revoking an already revoked assignment, if V1 chooses no-op success semantics
- revoking an already revoked grant, if V1 chooses no-op success semantics

### 5.2 Conflict cases

Use `409 Conflict` when:

- uniqueness invariants are violated on create/update
- optimistic concurrency fails
- protected governance state blocks the mutation
- the requested mutation contradicts lifecycle or policy rules in a deterministic way

### 5.3 Important rule

The implementation must not mix:

- “sometimes idempotent success”
- “sometimes conflict”

for the same semantic operation without documenting the rule explicitly.

Codes such as:

- `AUTHORIZATION.USER_ROLE_ALREADY_ASSIGNED`
- `AUTHORIZATION.ROLE_PERMISSION_ALREADY_GRANTED`

should only be surfaced on API paths that intentionally choose conflict semantics instead of stable idempotent success semantics.

## 6) Validation error shape

For validation failures, use the standard validation pattern:

```json
{
  "traceId": "string",
  "error": {
    "code": "COMMON.VALIDATION_FAILED",
    "message": "Validation failed.",
    "details": [
      { "field": "name", "reason": "Role name is required." },
      { "field": "sort", "reason": "Unsupported sort field." }
    ]
  }
}
```

Typical validation cases:

- missing role or permission name
- invalid permission format
- invalid page or pageSize
- unsupported sort field
- illegal action request payload

## 7) Safe-not-found and leak prevention rules

Authorization must use safe not-found behavior where appropriate.

Rules:

- do not leak protected governance internals unnecessarily
- return `404` for missing role/permission/user targets using safe resource semantics
- do not reveal extra protected metadata in error messages
- prefer stable machine-readable error codes over verbose internals

## 8) Suggested status mapping by operation

### Roles

- `GET /roles` → `200`
- `POST /roles` → `201`
- `PUT /roles/{roleId}` → `200`
- `POST /roles/{roleId}:activate` → `200`
- `POST /roles/{roleId}:deactivate` → `200`

### Permissions

- `GET /permissions` → `200`
- `POST /permissions` → `201`
- `PUT /permissions/{permissionId}` → `200`
- `POST /permissions/{permissionId}:activate` → `200`
- `POST /permissions/{permissionId}:deactivate` → `200`

### Role-permission grants

- `GET /roles/{roleId}/permissions` → `200`
- `POST /roles/{roleId}/permissions` → `200`
- `DELETE /roles/{roleId}/permissions/{permissionId}` → `200`

### User-role assignments

- `GET /users/{userId}/roles` → `200`
- `POST /users/{userId}/roles` → `200`
- `DELETE /users/{userId}/roles/{roleId}` → `200`

## 9) Operational rule for security-sensitive uncertainty

If authorization cannot be evaluated safely on a security-sensitive path because derived state is stale, uncertain, or unavailable:

- fall back to authoritative truth if possible
- otherwise fail closed

Recommended error codes:

- `AUTHORIZATION.FAIL_CLOSED`
- `AUTHORIZATION.DEPENDENCY_FAILURE`

Status:

- usually `403` for deny/fail-closed policy outcome
- `503` only when the authoritative evaluation path is unavailable and the request cannot be safely evaluated