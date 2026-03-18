# Notifications — Errors & Status Codes (V1)

## 1) Standard error envelope
See: `../../02-contracts-and-standards.md`

## 2) Status codes (admin read APIs)
- 200: read success
- 400: invalid query params
- 401: unauthenticated
- 403: policy denied
- 404: message not found
- 429: rate limited (policy)
- 500/503: unexpected failures

## 3) Error codes (examples)
- `NOTIFICATIONS.VALIDATION_FAILED`
- `NOTIFICATIONS.POLICY_DENIED`
- `NOTIFICATIONS.MESSAGE_NOT_FOUND`
- `NOTIFICATIONS.PROVIDER_FAILURE` (admin/internal; do not leak provider details)