# Media — API Surface (V1)

Media is primarily **Admin-managed**. Public consumption happens through Reading composition.

Base path (Admin): `/api/v1/admin/media`

> All endpoints in this module require Bearer auth + explicit authorization policies.
> Governance actions must emit audit events (async ingestion).
> V1 async scope: Media emits events through Outbox. Audit consumes these events first.
> Reading, SEO, CDN, scan, and variant workflows are downstream extensions and must not be required for Media write success.
> Success means **Media truth committed**. It does not guarantee that CDN, cache, or derivative side effects have already completed.

---

## 1) Media objects

### POST `/items`

Register a media item (metadata) after binary upload (upload mechanism is implementation-specific).

**Headers**

* `Idempotency-Key` (strongly recommended; may become required for production clients)
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

* This endpoint registers media metadata after binary upload. It does not guarantee that binary upload was performed by this API.
* Allowed types and size constraints must be validated by policy.
* Metadata must be sanitized and must not accept dangerous fields.
* If `Idempotency-Key` is reused with the same semantic request, registration should converge safely.
* Timeout ambiguity must be reconciled from Media truth, not from client belief or downstream derivative presence.
* On success, emit `media.asset_registered`.

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

### PATCH `/items/{mediaId}`

Update safe media metadata.

**Headers**

* `X-Correlation-Id` (optional)

**Request**

```json
{
  "altText": "Updated alt text",
  "metadata": {
    "width": 1200,
    "height": 630
  }
}
```

**Response (200)**

```json
{
  "updated": true
}
```

**Rules**

* Only safe metadata fields may be updated.
* `url`, storage path, size, and type should not be changed unless explicitly allowed by policy.
* Metadata must be sanitized.
* Repeated update with the same final state should converge safely.
* On success, emit `media.asset_updated`.

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
* V1 policy: if the deleted media is active primary in any article attachment, primary selection for those attachments is cleared in the same Media truth transaction where feasible.
* The system does not automatically select fallback primary media in V1.
* Repeated delete should converge safely as a no-op or equivalent success by policy.
* On success, emit `media.asset_soft_deleted`.

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
* On success, emit `media.asset_restored`.

---

## 2) Attachments (Media-to-Article)

### POST `/articles/{articleId}/attachments`

Attach a media item to an article.

**Headers**

* `Idempotency-Key` (strongly recommended to avoid duplicate attach intent after client timeout)
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
  "attached": true,
  "version": 5
}
```

**Rules**

* Attachment integrity must be enforced.
* If `isPrimary = true`, the same transaction attaches the media and sets it as primary.
* Repeated attach must not create duplicate membership or duplicate meaningful side effects.
* If timeout occurs, callers must reconcile from current Media truth.
* On success, emit `media.article_media_attached`.
* If primary was changed by this request, include `primaryChanged = true` and `isPrimary = true` in the event payload.

### DELETE `/articles/{articleId}/attachments/{mediaId}`

Detach media from an article.

**Response (200)**

```json
{
  "detached": true,
  "version": 6
}
```

**Rules**

* V1 policy: if the detached media is the current primary, primary selection is cleared.
* The system does not automatically select a fallback primary in V1.
* Admin may explicitly set a new primary through `attachments:set-primary`.
* Repeated detach should converge safely.
* Derived caches and projections may lag, but detach truth is immediate inside Media.
* On success, emit `media.article_media_detached`.
* If primary was cleared, include `primaryCleared = true` in the event payload.

### POST `/articles/{articleId}/attachments:reorder`

Reorder attachments deterministically.

**Headers**

* `X-Correlation-Id` (optional)

**Request**

```json
{
  "expectedVersion": 7,
  "mediaIds": ["id1", "id2", "id3"]
}
```

**Response (200)**

```json
{
  "reordered": true,
  "version": 8
}
```

**Rules**

* The provided list must match the current attachment set, unless policy explicitly defines another behavior.
* Ordering must be stable.
* Reorder is a final-state set operation, not a loose partial mutation sequence.
* Retrying the same order must converge to the same final truth.
* Partial reorder application is not acceptable.
* `expectedVersion` is required to prevent stale admin updates.
* If `expectedVersion` is missing, return `400 Bad Request`.
* If the provided version does not match current Media attachment version, return `409 Conflict`.
* On success, emit `media.article_media_reordered`.

### POST `/articles/{articleId}/attachments:set-primary`

Set the primary media for an article.

**Headers**

* `X-Correlation-Id` (optional)

**Request**

```json
{
  "mediaId": "string",
  "expectedVersion": 7
}
```

**Response (200)**

```json
{
  "primarySet": true,
  "version": 9
}
```

**Rules**

* Exactly one primary (or none, by policy) must be enforced.
* This operation must be idempotent.
* The primary invariant must be enforced atomically.
* `expectedVersion` is required to prevent stale primary selection.
* If `expectedVersion` is missing, return `400 Bad Request`.
* If the version does not match current Media attachment version, return `409 Conflict`.
* If the selected media is already primary, the operation should converge safely.
* If the version does not match current Media attachment version, return `409 Conflict`.
* Stale or replayed downstream projection or cache updates must not recreate an older primary state.
* On success, emit `media.article_primary_media_set`.

---

## 3) Public read endpoints (not implemented in V1)

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

---

## 6) Emitted events

Media emits integration events through the system Outbox.

Events are committed in the same local transaction as the Media truth change.

### Media item events

* `media.asset_registered`
  * Emitted after media metadata is registered.
* `media.asset_updated`
  * Emitted after safe metadata is updated.
* `media.asset_soft_deleted`
  * Emitted after a media item is soft-deleted.
* `media.asset_restored`
  * Emitted after a media item is restored.

### Article attachment events

* `media.article_media_attached`
  * Emitted after a media item is attached to an article.
* `media.article_media_detached`
  * Emitted after a media item is detached from an article.
* `media.article_media_reordered`
  * Emitted after article attachments are reordered.
* `media.article_primary_media_set`
  * Emitted after primary media is set for an article.

### V1 consumers

In V1, Media events are consumed by Audit for asynchronous audit ingestion.

Reading, SEO, CDN invalidation, media scanning, and variant generation may consume selected Media events in later phases, but they are not required for Media truth success in V1.
