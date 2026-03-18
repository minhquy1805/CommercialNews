# Interaction — API Surface (V1)

Base path (Public): `/api/v1`

> Interaction endpoints must be designed to be **fast**, **retry-safe**, and **non-blocking** relative to Reading.
> Where possible, writes should be lightweight and/or async-buffered.
> Interaction write success is defined by **interaction truth commit** or **accepted async ingestion**, not by downstream counter or trending freshness.

---

## 1) Views

### POST `/articles/{articleId}/views`

Record a view for an article (V1 simple counter/log policy).

**Auth**

* Optional (anonymous allowed). If authenticated, include user context.

**Headers**

* `Idempotency-Key` (optional; recommended if client retries aggressively and a dedupe policy exists)
* `X-Correlation-Id` (optional)

**Request (optional body)**

```json
{
  "visitorKey": "optional-string"
}
```

**Response (202 preferred)**

```json
{
  "accepted": true
}
```

**Rules**

* Must not block article reads.
* If the system is under load, it may drop, sample, or buffer views by policy (must be observable).
* V1 does not guarantee uniqueness; counts may be approximate.
* An accepted response does not guarantee immediate counter update.
* Duplicate submissions and replay are acceptable in V1 unless a stricter dedupe policy is explicitly introduced.

---

## 2) Likes

### POST `/articles/{articleId}/likes`

Like an article (idempotent).

**Auth**

* Required (must identify user)

**Headers**

* `Idempotency-Key` (optional)
* `X-Correlation-Id` (optional)

**Response (200)**

```json
{
  "liked": true
}
```

**Rules**

* Like truth is authoritative; aggregate like totals are derived.
* Repeated like requests must converge deterministically.
* Timeout or retry ambiguity must be reconciled from Interaction truth, not from counters.

### DELETE `/articles/{articleId}/likes`

Unlike an article (idempotent).

**Auth**

* Required

**Headers**

* `Idempotency-Key` (optional)
* `X-Correlation-Id` (optional)

**Response (200)**

```json
{
  "liked": false
}
```

**Rules**

* Like and unlike must be idempotent:

  * liking twice returns `liked = true`
  * unliking twice returns `liked = false`
* Current like state is truth; totals may lag because they are aggregated asynchronously.
* Concurrent or retried like/unlike requests must converge to deterministic truth state.

---

## 3) Comments

### GET `/articles/{articleId}/comments`

List comments for an article (public).

**Query**

* `page`
* `pageSize`
* `sort` allowlist (default `-createdAt`)

**Response (200)**

Standard list envelope.

**Rules**

* Returned comments must follow current comment truth and visibility/moderation policy.
* Comment counts shown elsewhere may lag independently from comment truth.

### POST `/articles/{articleId}/comments`

Create a comment.

**Auth**

* Required (recommended baseline to reduce abuse; policy can allow anonymous later)

**Headers**

* `Idempotency-Key` (recommended if duplicate comments under retry are unacceptable)
* `X-Correlation-Id` (optional)

**Request**

```json
{
  "content": "string"
}
```

**Response (201)**

```json
{
  "commentId": "string",
  "createdAt": "2026-03-02T10:30:00Z"
}
```

**Rules**

* If `Idempotency-Key` is supported, repeated equivalent create attempts must converge to one logical comment result.
* Comment truth is authoritative; comment counters and summaries are derived.
* Downstream moderation, reporting, and audit hooks must remain post-commit and non-blocking.

### PUT `/comments/{commentId}`

Edit a comment.

**Auth**

* Required

**Headers**

* `If-Match` or version-based concurrency header (recommended, if adopted by API policy)
* `X-Correlation-Id` (optional)

**Request**

```json
{
  "content": "string"
}
```

**Response (200)**

```json
{
  "updated": true
}
```

**Rules**

* Only the author can edit by default (object-level auth).
* V2 may add moderation states and edit windows.
* If stale-edit protection is adopted, stale updates must return deterministic conflict behavior instead of silently overwriting newer truth.
* Retry of the same semantic edit should remain safe.

### DELETE `/comments/{commentId}`

Delete a comment.

**Auth**

* Required

**Headers**

* `X-Correlation-Id` (optional)

**Response (200)**

```json
{
  "deleted": true
}
```

**Rules**

* The author can delete their own comment; admins or moderators can remove abusive comments (V2 governance).
* Repeated equivalent delete requests should converge safely as a no-op or documented idempotent success.
* Comment truth changes first; comment counters may lag.

---

## 4) Counters (optional read endpoints)

If you want dedicated endpoints for counters (optional in V1):

### GET `/articles/{articleId}/counters` (optional)

**Response (200)**

```json
{
  "views": 123,
  "likes": 45,
  "comments": 6,
  "partial": false
}
```

**Rules**

* Counters are derived and may be stale.
* `partial = true` may be used if the endpoint wants to signal degraded aggregate completeness.
* Clients must not treat this endpoint as authority for whether an interaction truth change committed.

---

## 5) Versioning and conventions

* All endpoints use `/api/v1`.
* Errors follow the standard envelope.
* List responses follow `{ items[], pageInfo{} }`.
* Interaction truth and aggregate outputs have different consistency expectations:

  * likes and comments truth are deterministic
  * counters and trending are eventual
* Public reads must remain correct even when interaction-derived aggregates lag.
