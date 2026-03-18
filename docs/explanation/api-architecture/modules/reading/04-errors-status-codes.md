# Reading — Errors & Status Codes (V1)

## 1) Standard error envelope
See: `../../02-contracts-and-standards.md`

## 2) Status code mapping
- 200: success
- 400: invalid query params (unknown sort, invalid pageSize)
- 404: not found (safe; do not leak drafts/unpublished)
- 429: rate limited (edge/API)
- 500/503: unexpected failures (keep rare; prefer graceful degradation)

## 3) Error codes (examples)
- `READING.VALIDATION_FAILED`
- `READING.INVALID_SORT_FIELD`
- `READING.NOT_FOUND`
- `READING.DEPENDENCY_DEGRADED` (optional; for observability not consumer logic)