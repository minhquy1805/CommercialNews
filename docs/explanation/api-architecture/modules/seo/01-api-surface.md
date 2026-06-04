# SEO — API Surface (V1)

SEO has both **public read routing APIs** (hot path) and **admin management APIs**.

> Public routing APIs provide fast route resolution and SEO metadata access.
> They do **not** replace Content truth for final public visibility decisions.
> Admin SEO write success is defined by **SEO truth commit**, not by downstream cache, projection, or search/index completion.

---

## 1) Public routing APIs (hot path)

Base path (Public): `/api/v1/seo`

### GET `/resolve`

Resolve a slug to a target resource (routing table read).
This endpoint is designed to remain fast and should not depend on heavy metadata reads.

**Query**

* `scope` (required): e.g. `public`
* `slug` (required)

**Response (200)**

```json
{
  "scope": "public",
  "slug": "some-article-slug",
  "resourceType": "Article",
  "resourcePublicId": "string",
  "canonicalUrl": "/articles/some-article-slug",
  "indexable": true,
  "status": "Resolved",
  "version": 12
}
```

**Response (404)**

Safe not-found (must not leak drafts/unpublished or distinguish “missing route” from “non-public content” in a dangerous way).

**Rules**

* Must be fast and cache-friendly (policy defined in Reading/edge docs).
* Must not require fetching full SEO metadata.
* Route resolution is not the final authority for public exposure.
* Reading must still validate Content truth by public resource identity before returning public content.
* Cache hit alone is not sufficient authority; stale routing must lose to truth-backed visibility checks.
* If uncertainty exists, prefer safe not-found / safe deny behavior over stale routing confidence.
* Public routing responses must expose stable public identifiers, not internal database primary keys.

### GET `/metadata`

Fetch SEO metadata for a resource (not the hot routing table).

**Query**

* `resourceType` (required): `Article`
* `resourcePublicId` (required)
* `scope` (optional, default: `public`)

**Response (200)**

```json
{
  "scope": "public",
  "resourceType": "Article",
  "resourcePublicId": "string",
  "slug": "some-article-slug",
  "canonicalUrl": "/articles/some-article-slug",
  "metaTitle": "string",
  "metaDescription": "string",
  "ogTitle": "string",
  "ogDescription": "string",
  "ogImageUrl": "string",
  "version": 12
}
```

**Rules**

* May be slower than `/resolve`, but should still be bounded and cacheable.
* Metadata freshness may lag behind recent writes within policy.
* Metadata must not be treated as authority for public visibility.

---

## 2) Admin management APIs

Base path (Admin): `/api/v1/admin/seo`

> All admin SEO endpoints require Bearer auth + explicit authorization policies.
> Article identifiers in SEO admin APIs are `articlePublicId` values, not internal database primary keys.

### GET `/articles/{articlePublicId}`

Get SEO settings for an article.

**Response (200)**

```json
{
  "articlePublicId": "string",
  "scope": "public",
  "slug": "some-article-slug",
  "canonicalUrl": "/articles/some-article-slug",
  "metaTitle": "string",
  "metaDescription": "string",
  "ogTitle": "string",
  "ogDescription": "string",
  "ogImageUrl": "string",
  "isManualOverride": true,
  "sourceAggregateVersion": 12,
  "version": 13
}
```

### PUT `/articles/{articlePublicId}`

Upsert SEO metadata for an article.

**Headers**

* `If-Match` or version-based concurrency header (recommended, if adopted by API policy)
* `Idempotency-Key` (recommended for safe retry under ambiguous client/network outcomes)
* `X-Correlation-Id` (optional)

**Request**

```json
{
  "scope": "public",
  "slug": "optional-slug",
  "canonicalUrl": "optional",
  "metaTitle": "optional",
  "metaDescription": "optional",
  "ogTitle": "optional",
  "ogDescription": "optional",
  "ogImageUrl": "optional"
}
```

**Response (200)**

```json
{
  "updated": true,
  "articlePublicId": "string",
  "scope": "public",
  "isManualOverride": true,
  "version": 13
}
```

**Rules**

* Must enforce slug uniqueness (by scope) at the SEO truth boundary.
* Slug stability policy applies (title change does not auto-change slug).
* Should enforce stale-write protection via version/rowversion/compare-and-set semantics according to module policy.
* If omitted in V1, `scope` defaults to `public`.
* Admin-edited SEO metadata must be treated as a manual override unless the request explicitly opts into an allowed auto-sync overwrite policy.
* Auto-sync from Content events must not overwrite manually edited SEO metadata unless explicitly allowed by policy.
* SEO truth change + local version advancement + outbox record (if needed) must commit atomically.
* Write success is defined by SEO truth commit only.
* Cache invalidation, search/index refresh, and downstream projections remain post-commit async effects.

**Important error cases**

* `400 Bad Request`: invalid slug format, invalid canonical URL, invalid metadata length.
* `401 Unauthorized`: missing or invalid authentication.
* `403 Forbidden`: authenticated caller lacks SEO permission.
* `404 Not Found`: article/resource not found.
* `409 Conflict`: slug already owned by another resource in the same scope.
* `412 Precondition Failed`: stale version or `If-Match` mismatch.
* `429 Too Many Requests`: admin anomaly protection or utility endpoint abuse.

**Async effects**

SEO event emission is optional in V1. If adopted, SEO truth change and the outbox record must commit atomically.

Admin SEO writes may emit SEO integration events such as:

* `seo.slug_route_changed`
* `seo.metadata_updated`
* `seo.slug_route_deactivated`

These events are post-commit side effects consumed by Audit, cache invalidation, sitemap/search refresh, or future indexing workflows.

### POST `/generate-slug` (optional)

Suggest a slug from a title and check current uniqueness (utility endpoint).

**Request**

```json
{
  "scope": "public",
  "title": "My New Article"
}
```

**Response (200)**

```json
{
  "slug": "my-new-article",
  "isUnique": true,
  "conflictResourceType": null,
  "conflictResourcePublicId": null
}
```

**Rules**

* This endpoint is a utility/helper only.
* Returned uniqueness is advisory until a truth-bound SEO write commits successfully.
* Clients must not treat generated slug availability as reserved ownership unless reservation semantics are explicitly introduced later.

### GET `/slug-availability` (optional)

Check whether a slug is currently available for admin editing workflows.

**Query**

* `scope` (required): e.g. `public`
* `slug` (required)
* `resourceType` (optional): `Article`
* `resourcePublicId` (optional): exclude the current owner when editing an existing resource

**Response (200)**

```json
{
  "scope": "public",
  "slug": "some-article-slug",
  "isAvailable": true,
  "conflictResourceType": null,
  "conflictResourcePublicId": null
}
```

**Rules**

* This endpoint is advisory only and does not reserve slug ownership.
* Final slug ownership is determined by the truth-bound SEO write commit.
* If `resourceType` and `resourcePublicId` identify the current owner, the existing slug may be reported as available for that edit flow.

---

## 3) Versioning and conventions

* Public: `/api/v1/seo/*`
* Admin: `/api/v1/admin/seo/*`
* Errors follow the standard envelope.
* List responses use `{ items[], pageInfo{} }` if needed.
* Where ordering or stale-write protection matters, APIs should expose or align with SEO version semantics.
* Public routing success does not imply final public visibility success.

---

## 4) Rate limiting (policy-level)

* `/resolve` is read-heavy: protect via caching + edge protections.
* Admin SEO endpoints are protected by authz; still monitor anomaly spikes.
* Utility endpoints such as `/generate-slug` should also be protected against loops or abuse.

---

## 5) Notes on visibility coupling

* SEO resolves routes and stores metadata truth.
* Content remains the source of truth for publication visibility.
* A resolved slug may still be denied on the public path if Content truth says the target is non-public.
* Safe routing degradation is preferable to incorrect public exposure.
