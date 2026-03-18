# Audit — Errors & Status Codes (V1)

## 1) Standard error envelope
See: `../../02-contracts-and-standards.md`

## 2) Status code mapping (admin read APIs)
- 200: read success
- 400: invalid query (bad time range, invalid pageSize)
- 401: unauthenticated
- 403: policy denied
- 404: audit record not found
- 429: rate limited (policy)
- 500/503: unexpected failures

## 3) Error codes (examples)
- `AUDIT.VALIDATION_FAILED`
- `AUDIT.POLICY_DENIED`
- `AUDIT.LOG_NOT_FOUND`
- `AUDIT.REDACTION_VIOLATION` (internal; should not leak details)