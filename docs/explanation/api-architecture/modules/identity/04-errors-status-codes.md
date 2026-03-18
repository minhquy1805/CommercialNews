# Identity — Errors & Status Codes (V1)

## 1) Standard error envelope
See: `../../02-contracts-and-standards.md`

## 2) Status code mapping
- 201: register success
- 200: all other successful operations
- 400: validation errors, invalid/expired tokens
- 401: invalid credentials, invalid access token
- 403: policy denied (rare for auth endpoints; common for admin elsewhere)
- 429: rate-limited
- 500/503: unexpected failures (avoid surfacing email failures as 5xx)

## 3) Error codes (examples)
- `IDENTITY.VALIDATION_FAILED`
- `IDENTITY.INVALID_CREDENTIALS`
- `IDENTITY.ACCOUNT_LOCKED` (if applicable)
- `IDENTITY.TOKEN_INVALID`
- `IDENTITY.TOKEN_EXPIRED`
- `IDENTITY.REFRESH_REUSE_DETECTED`
- `IDENTITY.PASSWORD_POLICY_VIOLATION`

## 4) Anti-enumeration rules (mandatory)
- `/forgot-password`: always `200 { "accepted": true }`
- `/resend-verification`: always `200 { "accepted": true }`

## 5) Email already exists on register (policy decision)
Two acceptable approaches:
- **A (preferred for privacy):** return a safe response that does not confirm existence.
- **B (admin/internal only):** `409 IDENTITY.EMAIL_EXISTS`.

Record the chosen approach in an ADR and implement consistently.