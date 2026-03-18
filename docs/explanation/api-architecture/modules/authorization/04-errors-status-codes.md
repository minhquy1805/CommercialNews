# Authorization — Errors & Status Codes (V1)

## 1) Standard error envelope
See: `../../02-contracts-and-standards.md`

## 2) Status code mapping
- 200: success
- 201: created (role/permission)
- 204: delete success (if used)
- 400: validation errors
- 401: unauthenticated
- 403: policy denied (most common)
- 404: role/permission/user not found (safe not-found)
- 409: uniqueness/conflict (role name exists, permission name exists)
- 429: rate-limited (policy)

## 3) Error codes (examples)
- `AUTHORIZATION.POLICY_DENIED`
- `AUTHORIZATION.ROLE_EXISTS`
- `AUTHORIZATION.PERMISSION_EXISTS`
- `AUTHORIZATION.ROLE_NOT_FOUND`
- `AUTHORIZATION.PERMISSION_NOT_FOUND`
- `AUTHORIZATION.USER_NOT_FOUND`
- `AUTHORIZATION.SYSTEM_ROLE_PROTECTED`