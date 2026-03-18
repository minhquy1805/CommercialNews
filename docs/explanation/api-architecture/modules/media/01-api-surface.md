# Media — API Surface (V1)

Media is primarily **Admin-managed**. Public consumption happens through Reading composition.

Base path (Admin): `/api/v1/admin/media`

> All endpoints in this module require Bearer auth + explicit authorization policies.
> Governance actions must emit audit events (async ingestion).
> Success means **Media truth committed**. It does not guarantee that CDN, cache, or derivative side effects have already completed.

---

## 1) Media objects

### POST `/items`

Register a media item (metadata) after upload (upload mechanism is implementation-specific).

**Headers**

* `Idempotency-Key` (recommended)
* `X-Correlation-Id` (optional)

**Request**

```json
{
  "url": "/media/abc.jpg",
  "type": "Image",
  "altText": "optional",
  "metadata": {
    "width": 1200,
    "height": 630
  }
}
```

**Response (201)**

```json
{
  "mediaId": "string",
  "url": "/media/abc.jpg",
  "type": "Image"
}
```

**Rules**

* Allowed types and size constraints must be validated by policy.
* Metadata must be sanitized and must not accept dangerous fields.
* If `Idempotency-Key` is reused with the same semantic request, registration should converge safely.
* Timeout ambiguity must be reconciled from Media truth, not from client belief or downstream derivative presence.

### GET `/items`

List media items (admin).

**Query**

* `page`
* `pageSize`
* `type` (optional)
* `status` (optional): `Active | Deleted`
* `sort` allowlist (for example `-createdAt`)

**Response (200)**

Standard list envelope.

### GET `/items/{mediaId}`

Get media item details.

**Response (200)**

Media detail object.

### DELETE `/items/{mediaId}`

Soft delete a media item.

**Response (200)**

```json
{
  "deleted": true
}
```

**Rules**

* Soft delete is truth-first.
* Deleted media must not remain active primary if policy forbids it.
* Repeated delete should converge safely as a no-op or equivalent success by policy.

### POST `/items/{mediaId}:restore`

Restore a soft-deleted media item (within the retention window).

**Response (200)**

```json
{
  "restored": true
}
```

**Rules**

* Restore must obey retention and legality policy.
* Restore must not silently violate current primary or ordering invariants.
* Repeated restore should converge safely where policy allows.

---

## 2) Attachments (Media-to-Article)

### POST `/articles/{articleId}/attachments`

Attach a media item to an article.

**Headers**

* `Idempotency-Key` (recommended)
* `X-Correlation-Id` (optional)

**Request**

```json
{
  "mediaId": "string",
  "isPrimary": false
}
```

**Response (200)**

```json
{
  "attached": true
}
```

**Rules**

* Attachment integrity must be enforced.
* If `isPrimary = true`, the primary rule (`0..1`) must be enforced atomically.
* Repeated attach must not create duplicate membership or duplicate meaningful side effects.
* If timeout occurs, callers must reconcile from current Media truth.

### DELETE `/articles/{articleId}/attachments/{mediaId}`

Detach media from an article.

**Response (200)**

```json
{
  "detached": true
}
```

**Rules**

* If detaching the primary media, primary becomes none or falls back deterministically by policy.
* Repeated detach should converge safely.
* Derived caches and projections may lag, but detach truth is immediate inside Media.

### POST `/articles/{articleId}/attachments:reorder`

Reorder attachments deterministically.

**Headers**

* `X-Correlation-Id` (optional)

**Request**

```json
{
  "mediaIds": ["id1", "id2", "id3"]
}
```

**Response (200)**

```json
{
  "reordered": true
}
```

**Rules**

* The provided list must match the current attachment set, unless policy explicitly defines another behavior.
* Ordering must be stable.
* Reorder is a final-state set operation, not a loose partial mutation sequence.
* Retrying the same order must converge to the same final truth.
* Partial reorder application is not acceptable.

### POST `/articles/{articleId}/attachments:set-primary`

Set the primary media for an article.

**Headers**

* `X-Correlation-Id` (optional)

**Request**

```json
{
  "mediaId": "string"
}
```

**Response (200)**

```json
{
  "primarySet": true
}
```

**Rules**

* Exactly one primary (or none, by policy) must be enforced.
* This operation must be idempotent.
* The primary invariant must be enforced atomically.
* Stale or replayed downstream projection or cache updates must not recreate an older primary state.

---

## 3) Public read endpoints (optional)

Typically Reading composes media. If direct public reads are needed:

### GET `/public/articles/{articleId}/media` (optional)

Return a public-facing media list (only for published articles, by policy).

Prefer keeping this in Reading to avoid leakage and to preserve truth-backed visibility checks.

**Rules**

* Media relationship truth may be returned, but final public visibility still belongs to truth-backed public composition rules.
* Missing derivatives should degrade gracefully rather than fail the whole response.

---

## 4) Versioning and conventions

* All endpoints use `/api/v1`.
* Errors follow the standard envelope.
* List responses use `{ items[], pageInfo{} }`.

---

## 5) Surface-level consistency rules

Media truth governs:

* attachment membership
* primary selection
* order
* delete and restore state

CDN, cache, thumbnails, and transformed variants are downstream and derived.

**Rules**

* Timeout does not prove attach, reorder, set-primary, delete, restore, or register failed.
* Derived delivery lag may affect presentation, but it must not redefine Media truth.
* If downstream side effects are emitted, they are at-least-once and must be safe under duplicate delivery and replay.
