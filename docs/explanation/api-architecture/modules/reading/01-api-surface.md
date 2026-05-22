# Reading — API Surface (V1)

Base path (Public): `/api/v1`

> These endpoints are public-facing and must prioritize read-path performance and availability.
> They must not block on interaction, telemetry, cache-refresh, or rebuild/reconciliation pipelines.
> Public read success is defined by **source-derived visibility and safe response composition**, not by completion of non-critical enrichments.
> If public visibility is uncertain, the endpoint must fail closed.

---

## 1) Article listing

### GET `/articles`

Public list endpoint with paging, filter, and sort.

**Query**

* `page` (default `1`)
* `pageSize` (default `20`, max enforced)
* `categoryId` (optional)
* `tagId` (optional)
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
      "articlePublicId": "string",
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

* Only publicly visible content may be returned.
* Public visibility requires source status `Published` and projection `IsPublic = true`.
* Archived, soft-deleted, unpublished, or visibility-uncertain content must not be returned.
* Counters, related ranking signals, and other enrichments may be missing or stale; must degrade gracefully.
* Projection visibility must be derived from source truth and fail closed when uncertain.
* If a derived listing source is stale or unavailable, Reading may fall back to source-truth composition only where policy explicitly requires.

---

## 2) Article detail

### GET `/articles/{articlePublicId}`

Get public article detail by public ID.

**Response (200)**

```json
{
  "articlePublicId": "string",
  "title": "string",
  "body": "string",
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
    "canonicalUrl": "/articles/example-slug",
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

* Only publicly visible content may be returned.
* Public visibility requires source status `Published` and projection `IsPublic = true`.
* Archived, soft-deleted, unpublished, or visibility-uncertain content must not be returned.
* If media, SEO, or counters are unavailable, return a valid response with fallbacks.
* `cover` is the denormalized primary media shortcut; `media` is the ordered gallery/assets collection and may include an item with `isPrimary = true`.
* Public detail correctness is determined by source-derived projection visibility, not by freshness of enrichments.
* Missing enrichments should be omitted or defaulted safely rather than causing incorrect `404` or content exposure.

### GET `/articles/slug/{slug}`

This endpoint is included in V1 as the preferred public article detail route for website clients.

**Response (200)**

Same as `GET /articles/{articlePublicId}`.

**Rules**

* In the normal public path, Reading may resolve slug from its projected read model.
* SEO remains the source of truth for slug generation and canonical routing rules.
* If projection freshness is uncertain or redirect/canonical behavior is needed, the system may fall back to SEO `/resolve` according to policy.
* SEO route resolution is not sufficient by itself; Reading must still require source-derived public visibility before returning content.
* Safe `404` for non-existent or non-public slugs.
* A successful route resolution does not guarantee the article is still publicly visible.

---

## 3) Related articles

### GET `/articles/{articlePublicId}/related`

Return related articles deterministically.

**Query**

* `limit` (default `6`, max enforced)

**Response (200)**

Standard list envelope or fixed array; choose one and keep consistent (recommended: standard list envelope).

**Rules**

* Deterministic: prioritize same category, then shared tags, then fallback to recent published.
* Only publicly visible content may be returned.
* Public visibility requires source status `Published` and projection `IsPublic = true`.
* Archived, soft-deleted, unpublished, or visibility-uncertain content must not be returned.
* Related-content enrichments may be eventually consistent; safe fallback must remain deterministic.
* Missing or stale related-content signals must not block the core detail response path.

---

## 4) Search (V1 basic)

### GET `/articles/search`

Basic keyword search (V1 scope-dependent).

**Query**

* `q` (required)
* `page`
* `pageSize`
* `sort` allowlist (default `-publishedAt`)

**Response (200)**

Same list envelope as `/articles`.

**Rules**

* Only publicly visible content may be returned.
* Public visibility requires source status `Published` and projection `IsPublic = true`.
* Archived, soft-deleted, unpublished, or visibility-uncertain content must not be returned.
* Search/materialized query outputs, if introduced, remain derived and may lag.
* Safe fallback or safe omission is preferred over exposing non-public content due to stale search state.

---

## 5) View tracking (non-blocking signal)

Reading itself does not expose interaction write endpoints in V1.

Preferred V1 decision:

* Clients should call Interaction endpoint `POST /api/v1/articles/{articlePublicId}/views` after successful article rendering.
* Reading-triggered internal view signals are deferred beyond V1 unless a later policy explicitly adopts them.

**Rules**

* Read success must not depend on view tracking success.
* Duplicate view signals must be tolerated safely by downstream Interaction policy.
* Timeout, retry, or signal lag must not affect public visibility or read correctness.
* Document the chosen option in the Interaction module.

---

## 6) Versioning and conventions

* All endpoints are under `/api/v1`.
* List response envelope and error model follow system standards.
* Static routes such as `/articles/search` and `/articles/slug/{slug}` must be registered before dynamic `/articles/{articlePublicId}` routes, or route constraints must be used.
* Public read routes normally consume Reading projections, with visibility derived from source truth.
* Cache hit, route hit, or projection hit must fail closed when public readability is uncertain.
