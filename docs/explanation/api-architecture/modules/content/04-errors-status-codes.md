# Content — Errors & Status Codes (V1)

## 1) Standard error envelope
See: `../../02-contracts-and-standards.md`

## 2) Status code mapping
- 201: draft created
- 200: successful updates/actions
- 400: validation errors, illegal transitions
- 401: unauthenticated
- 403: policy denied
- 404: article/category/tag not found
- 409: conflicts (slug/title uniqueness if enforced here; concurrency conflicts)
- 429: rate limited (policy)
- 500/503: unexpected failures

## 3) Error codes (examples)
- `CONTENT.VALIDATION_FAILED`
- `CONTENT.ARTICLE_NOT_FOUND`
- `CONTENT.CATEGORY_NOT_FOUND`
- `CONTENT.TAG_NOT_FOUND`
- `CONTENT.INVALID_STATE_TRANSITION`
- `CONTENT.UNPUBLISH_REASON_REQUIRED`
- `CONTENT.TAXONOMY_ORPHAN_REFERENCE`
- `CONTENT.CONCURRENCY_CONFLICT` (if using optimistic concurrency)

## 4) Safe not-found rule (public exposure)
Admin endpoints can return 404 normally.
Public Query must not leak existence of drafts/unpublished items.