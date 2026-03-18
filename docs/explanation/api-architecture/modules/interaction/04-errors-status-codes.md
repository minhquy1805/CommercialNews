# Interaction — Errors & Status Codes (V1)

## 1) Standard error envelope
See: `../../02-contracts-and-standards.md`

## 2) Status code mapping
- 202: view accepted (preferred)
- 200: like/unlike success, edit/delete success
- 201: comment created
- 400: validation errors (empty comment, invalid ids)
- 401: unauthenticated (like/comment requires auth)
- 403: forbidden (object-level auth failure)
- 404: not found (comment/article not found, safe)
- 409: conflict (rare; like uniqueness conflicts should be handled idempotently)
- 429: rate limited
- 500/503: unexpected failures

## 3) Error codes (examples)
- `INTERACTION.VALIDATION_FAILED`
- `INTERACTION.ARTICLE_NOT_FOUND` (optional; ensure safe behavior)
- `INTERACTION.COMMENT_NOT_FOUND`
- `INTERACTION.NOT_COMMENT_OWNER`
- `INTERACTION.DISABLED_FOR_ARTICLE_STATE`
- `INTERACTION.RATE_LIMITED`