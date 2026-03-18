# Reading — API Surface (V1)

Base path (Public): `/api/v1`

> These endpoints are public-facing and must prioritize read-path performance and availability.
> They must not block on interaction, telemetry, cache-refresh, or rebuild/reconciliation pipelines.
> Public read success is defined by **truth-safe response composition**, not by completion of non-critical enrichments.

---

## 1) Article listing

### GET `/articles`

Public list endpoint with paging, filter, and sort.

**Query**

* `page` (default `1`)
* `pageSize` (default `20`, max enforced)
* `categoryId` (optional)
* `tagId` (optional)
* `q` (optional; basic keyword search in V1)
* `sort` (optional allowlist):

  * `-publishedAt` (default)
  * `publishedAt`
  * `-popularity` (policy-defined; may be eventual)
  * `popularity`

**Response (200)**

```json
{
  "items": [
    {
      "articleId": "string",
      "title": "string",
      "summary": "string",
      "slug": "string",
      "publishedAt": "2026-03-02T10:30:00Z",
      "category": {
        "categoryId": "string",
        "name": "string"
      },
      "cover": {
        "mediaId": "string",
        "url": "string",
        "alt": "string"
      },
      "counters": {
        "views": 123,
        "likes": 45
      }
    }
  ],
  "pageInfo": {
    "page": 1,
    "pageSize": 20,
    "totalItems": 123,
    "totalPages": 7
  }
}
```

**Rules**

* Only Published content.
* Counters, related ranking signals, and other enrichments may be missing or stale; must degrade gracefully.
* Cache/projection presence must not bypass truth-backed visibility rules.
* If a derived listing source is stale or unavailable, Reading must fall back to truth-backed composition where policy requires.

---

## 2) Article detail

### GET `/articles/{articleId}`

Get public article detail by ID.

**Response (200)**

```json
{
  "articleId": "string",
  "title": "string",
  "body": "string",
  "summary": "string",
  "slug": "string",
  "publishedAt": "2026-03-02T10:30:00Z",
  "category": {
    "categoryId": "string",
    "name": "string"
  },
  "tags": [
    {
      "tagId": "string",
      "name": "string"
    }
  ],
  "media": [
    {
      "mediaId": "string",
      "url": "string",
      "alt": "string",
      "isPrimary": true,
      "order": 1
    }
  ],
  "seo": {
    "canonicalUrl": "/articles/slug",
    "metaTitle": "string",
    "metaDescription": "string"
  },
  "counters": {
    "views": 123,
    "likes": 45
  }
}
```

**Rules**

* Only Published content.
* If media, SEO, or counters are unavailable, return a valid response with fallbacks.
* Public detail correctness is determined by truth-backed visibility, not by freshness of enrichments.
* Missing enrichments should be omitted or defaulted safely rather than causing incorrect `404` or content exposure.

### GET `/articles/slug/{slug}` (optional)

Alternative convenience endpoint if clients prefer direct slug routing without calling SEO explicitly.

**Response (200)**

Same as `GET /articles/{articleId}`.

**Rules**

* Internally must use SEO `/resolve` first; do not couple slug routing to heavy reads.
* SEO route resolution is not sufficient by itself; Reading must still validate Content truth before returning content.
* Safe `404` for non-existent or non-public slugs.
* A successful route resolution does not guarantee the article is still publicly visible.
* If you prefer a single canonical route, you may omit this endpoint and let the client call SEO resolve.

---

## 3) Related articles

### GET `/articles/{articleId}/related`

Return related articles deterministically.

**Query**

* `limit` (default `6`, max enforced)

**Response (200)**

Standard list envelope or fixed array; choose one and keep consistent (recommended: standard list envelope).

**Rules**

* Deterministic: prioritize same category, then shared tags, then fallback to recent published.
* Only Published content.
* Related-content enrichments may be eventually consistent; safe fallback must remain deterministic.
* Missing or stale related-content signals must not block the core detail response path.

---

## 4) Search (V1 basic)

### GET `/search`

Basic keyword search (V1 scope-dependent).

**Query**

* `q` (required)
* `page`
* `pageSize`
* `sort` allowlist (default `-publishedAt`)

**Response (200)**

Same list envelope as `/articles`.

**Rules**

* Search results must still obey truth-backed publication visibility.
* Search/materialized query outputs, if introduced, remain derived and may lag.
* Safe fallback or safe omission is preferred over exposing non-public content due to stale search state.

---

## 5) View tracking (non-blocking signal)

Reading itself should not expose interaction write endpoints, but in V1 you may send a non-blocking signal.

Two options:

* **Option A (preferred):** client calls Interaction endpoint `POST /api/v1/articles/{id}/views`
* **Option B:** Reading triggers an async view signal internally (still must be non-blocking)

**Rules**

* Read success must not depend on view tracking success.
* Duplicate view signals must be tolerated safely by downstream Interaction policy.
* Timeout, retry, or signal lag must not affect public visibility or read correctness.
* Document the chosen option in the Interaction module.

---

## 6) Versioning and conventions

* All endpoints are under `/api/v1`.
* List response envelope and error model follow system standards.
* Public read routes may consume derived inputs, but visibility decisions remain truth-backed.
* Cache hit, route hit, or projection hit must not be treated as final authority for readability.
