# Media — Errors & Status Codes (V1)

## 1) Standard error envelope
See: `../../02-contracts-and-standards.md`

## 2) Status code mapping
- 201: media registered
- 200: attach/reorder/set-primary/delete/restore success
- 400: validation errors (type not allowed, invalid reorder list)
- 401: unauthenticated
- 403: policy denied
- 404: media not found / article not found (policy-defined)
- 409: conflicts (primary rule violation, attachment duplicates)
- 429: rate limited (policy)
- 500/503: unexpected failures

## 3) Error codes (examples)
- `MEDIA.VALIDATION_FAILED`
- `MEDIA.MEDIA_NOT_FOUND`
- `MEDIA.MEDIA_DELETED`
- `MEDIA.ATTACHMENT_ALREADY_EXISTS`
- `MEDIA.ATTACHMENT_NOT_FOUND`
- `MEDIA.PRIMARY_CONSTRAINT_VIOLATION`
- `MEDIA.INVALID_REORDER_LIST`
- `MEDIA.RESTORE_WINDOW_EXPIRED`
- `MEDIA.TYPE_NOT_ALLOWED`