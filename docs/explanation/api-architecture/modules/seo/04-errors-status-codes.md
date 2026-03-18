# SEO — Errors & Status Codes (V1)

## 1) Standard error envelope
See: `../../02-contracts-and-standards.md`

## 2) Status code mapping
- 200: successful resolve/metadata read
- 400: invalid scope/slug input
- 401/403: admin endpoints auth failures
- 404: slug not found (safe not-found)
- 409: slug uniqueness conflict (admin upsert/generate)
- 429: rate limited (policy)
- 500/503: unexpected failures

## 3) Error codes (examples)
- `SEO.INVALID_SCOPE`
- `SEO.INVALID_SLUG`
- `SEO.SLUG_NOT_FOUND`
- `SEO.SLUG_CONFLICT`
- `SEO.METADATA_NOT_FOUND`
- `SEO.CANONICAL_RULE_VIOLATION` (if enforced)