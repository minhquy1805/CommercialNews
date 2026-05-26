# Reading — API Surface (V1 Async Projections)

Base path (Public): `/api/v1`

Reading exposes public article query endpoints backed by Reading-owned asynchronous projections.

Core serving projections:

```text
ArticleReadModel
ArticleSeoRouteProjection
```

Public API rules:

```text
Ordinary public requests read from Reading-owned projections only.

Reading does not synchronously query Content, SEO, Media or Interaction
during ordinary public response composition.

Core article visibility or slug-route state that is locally missing,
denied or unsafe fails closed.

Optional media and counter enrichment may be absent or stale
without blocking safely public article responses.

Reading V1 accepts bounded eventual-consistency lag before newer
upstream source changes have reached and been applied locally.
```

---

## 1. Article Listing

### `GET /articles`

Returns paged public article summaries.

### Query Parameters

| Parameter | Required | Description |
|---|---:|---|
| `page` | No | Page number. Default `1`. |
| `pageSize` | No | Number of items per page. Default `20`; server enforces maximum. |
| `categoryPublicId` | No | Filter by projected public category identity. |
| `tagPublicId` | No | Filter by projected public tag identity, if tag projection is supported in the implemented read model. |
| `sort` | No | Allowlisted ordering value. Default `-publishedAt`. |

### V1 Allowed Sort Values

```text
-publishedAt
publishedAt
```

Rules:

```text
Popularity/trending sorting is deferred beyond V1.

Unsupported sort values must follow standard validation behavior.

Stable paging must use deterministic tie-breaking,
for example PublishedAtUtc DESC, ArticlePublicId DESC.
```

### Response `200 OK`

```json
{
  "items": [
    {
      "articlePublicId": "01JARTICLEPUBLICID000000000",
      "title": "Article title",
      "summary": "Short public article summary.",
      "slug": "article-title",
      "publishedAtUtc": "2026-03-02T10:30:00Z",
      "category": {
        "categoryPublicId": "01JCATEGORYPUBLICID0000000",
        "name": "Technology"
      },
      "cover": {
        "mediaPublicId": "01JMEDIAPUBLICID00000000000",
        "url": "/media/article-cover.jpg",
        "alt": "Article cover"
      },
      "counters": {
        "views": 123,
        "likes": 45,
        "visibleComments": 6
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

### Rules

```text
Only locally safe public ArticleReadModel rows may be returned.

A returned article requires:
    Status = Published
    IsPublic = true
    local visibility state is not unsafe / requires-resync.

Locally known unpublished, archived, soft-deleted or unsafe rows
must not be returned.

Slug, cover/media and counters are projected response fields.

Missing cover/media enrichment may be represented as null or omitted.

Missing Interaction counter projection may return documented zero/default values.

Counter lag must not affect article visibility.

The endpoint must not synchronously call upstream source modules
to fill missing projection fields.
```

### Failure / Degradation Behavior

| Situation | Behavior |
|---|---|
| No matching public rows | Return `200` with empty `items` |
| Optional media projection missing | Return null/omitted `cover` |
| Counter snapshot missing | Return zero/default counters |
| Counter snapshot delayed | Return last-known counters |
| Reading query store unavailable | Return standard service failure response |
| Locally unsafe/non-public row | Exclude from results |

---

## 2. Article Detail by Public ID

### `GET /articles/{articlePublicId}`

Returns one public article detail by stable public article identity.

### Route Parameter

| Parameter | Description |
|---|---|
| `articlePublicId` | Public identifier of the article. |

### Response `200 OK`

```json
{
  "articlePublicId": "01JARTICLEPUBLICID000000000",
  "title": "Article title",
  "summary": "Short public article summary.",
  "body": "Rendered or public article body.",
  "slug": "article-title",
  "publishedAtUtc": "2026-03-02T10:30:00Z",
  "category": {
    "categoryPublicId": "01JCATEGORYPUBLICID0000000",
    "name": "Technology"
  },
  "tags": [
    {
      "tagPublicId": "01JTAGPUBLICID000000000000",
      "name": "Cloud"
    }
  ],
  "cover": {
    "mediaPublicId": "01JMEDIAPUBLICID00000000000",
    "url": "/media/article-cover.jpg",
    "alt": "Article cover"
  },
  "media": [
    {
      "mediaPublicId": "01JMEDIAPUBLICID00000000000",
      "url": "/media/article-cover.jpg",
      "alt": "Article cover",
      "isPrimary": true,
      "order": 1
    }
  ],
  "seo": {
    "canonicalUrl": "/articles/article-title",
    "metaTitle": "Article title",
    "metaDescription": "Public SEO description."
  },
  "counters": {
    "views": 123,
    "likes": 45,
    "visibleComments": 6
  }
}
```

### Rules

```text
Reading loads detail from ArticleReadModel.

The response may be returned only when local article state confirms:
    Status = Published
    IsPublic = true
    visibility is not unsafe / requires-resync.

Reading does not synchronously call Content to verify visibility.

Reading does not synchronously call Media for missing media fields.

Reading does not synchronously call Interaction for fresher counters.

SEO fields in this response come from Reading-owned projected SEO state
where available; they are not required for safely serving detail by public id.

Missing optional SEO, media or counter enrichment must degrade safely
rather than block a safely public article response.
```

### Response `404 Not Found`

Return safe not-found behavior when:

```text
Article projection is missing.
Article is locally non-public.
Article visibility is locally unsafe / requires-resync.
```

Public responses must not reveal whether the source resource exists but is hidden.

---

## 3. Article Detail by Slug

### `GET /articles/slug/{slug}`

Preferred website-facing public article detail route.

### Route Parameter

| Parameter | Description |
|---|---|
| `slug` | Public article slug. |

### Normal V1 Runtime Path

```text
Client calls GET /api/v1/articles/slug/{slug}
    -> Reading queries ArticleSeoRouteProjection by:
         Scope = public
         Slug = requested slug
    -> Require:
         IsActive = true
         RequiresResync = false
    -> Reading obtains ArticlePublicId from local route projection
    -> Reading loads ArticleReadModel by ArticlePublicId
    -> Require:
         Status = Published
         IsPublic = true
         local visibility is safe
    -> Reading composes response from local projected state
    -> Return response or safe 404
```

### Response `200 OK`

Same response contract as:

```text
GET /articles/{articlePublicId}
```

### Rules

```text
SEO owns canonical slug and route truth.

Reading owns ArticleSeoRouteProjection as its local public-serving route projection.

Reading does not synchronously call SEO `/resolve` during ordinary V1 slug requests.

A valid local route projection does not itself grant public article exposure.

The referenced ArticleReadModel must also pass local public visibility checks.

Optional media/counter enrichment may lag without blocking a safely routed,
safely public article response.
```

### Response `404 Not Found`

Return safe not-found behavior when:

```text
Slug route projection is missing.
Route is inactive.
Route is marked RequiresResync.
Route points to a missing ArticleReadModel.
Target article is locally non-public.
Target article visibility is locally unsafe / requires-resync.
```

### Async Lag Clarification

Because SEO route and Content visibility updates are applied asynchronously:

```text
A newly activated slug may temporarily return 404 until projected.

A route deactivation or article hide/unpublish change may not be known
to Reading until its newer projection snapshot is applied.
```

V1 accepts bounded propagation lag. Once Reading locally knows a route or article is denied/unsafe, it must fail closed immediately.

---

## 4. Related Articles

### `GET /articles/{articlePublicId}/related`

Returns related public article summaries.

### Query Parameters

| Parameter | Required | Description |
|---|---:|---|
| `limit` | No | Maximum number of related items. Default `6`; server enforces maximum. |

### Response `200 OK`

Recommended response envelope:

```json
{
  "items": [
    {
      "articlePublicId": "01JRELATEDARTICLE00000000000",
      "title": "Related article title",
      "summary": "Related article summary.",
      "slug": "related-article-title",
      "publishedAtUtc": "2026-03-01T10:30:00Z",
      "cover": null,
      "counters": {
        "views": 0,
        "likes": 0,
        "visibleComments": 0
      }
    }
  ]
}
```

### Rules

```text
The requested source article must be locally public or return safe 404.

The current article must not appear in its own related results.

Only locally safe public articles may be returned.

Related output may be computed from Reading-owned projected fields
or a future Reading-owned related projection.

Missing related state returns an empty or deterministic fallback result.

Interaction counters do not define related ranking in V1.

Missing related output must not block article detail response.
```

### V1 Deterministic Fallback Direction

When basic related behavior is implemented, acceptable signal ordering is:

```text
Same category
    -> shared tags where projected
    -> most recent public articles
```

Popularity-based related ranking is deferred beyond V1.

---

## 5. Search

### `GET /articles/search`

Returns public articles matching a keyword query.

### Query Parameters

| Parameter | Required | Description |
|---|---:|---|
| `q` | Yes | Search keyword or phrase. |
| `page` | No | Page number. Default `1`. |
| `pageSize` | No | Number of items per page; maximum enforced. |
| `sort` | No | Allowlisted sort. Default `-publishedAt`. |

### V1 Allowed Sort Values

```text
-publishedAt
publishedAt
```

### Response `200 OK`

Uses the same list envelope shape as:

```text
GET /articles
```

### Rules

```text
Search must operate on Reading-owned public projection/search state.

Only locally safe public articles may be returned.

Locally known draft, unpublished, archived, soft-deleted
or unsafe content must not be returned.

Search may lag behind source truth within the accepted async model.

Safe omission is preferred over exposing locally denied or unsafe content.

Search does not synchronously query source modules in ordinary public requests.

Popularity/relevance ranking beyond baseline keyword matching
is deferred unless separately designed.
```

---

## 6. Counter Fields in Public Responses

Counters may appear in article list, detail, related and search response items.

### Public Response Shape

```json
{
  "counters": {
    "views": 123,
    "likes": 45,
    "visibleComments": 6
  }
}
```

### Ownership

```text
Interaction owns counter truth and counter publication.

Reading owns only the locally applied serving copy.
```

### Inbound Projection Contract

Reading consumes:

```text
interaction.article_counters_projection_published
```

Expected counter snapshot values:

```text
ArticlePublicId
ViewCount
LikeCount
VisibleCommentCount
StatsVersion
OccurredAtUtc
```

### Reading Apply Rule

```text
If IncomingStatsVersion > CurrentInteractionStatsVersion:
    Set projected ViewCount.
    Set projected LikeCount.
    Set projected VisibleCommentCount.
    Set InteractionStatsVersion.
Else:
    Ignore duplicate or stale counter snapshot.
```

### API Rules

```text
Counters are derived enrichments and may lag.

Missing counters may return zero/default values.

Counters do not decide article visibility.

Reading must not increment public counters from raw interaction events.

Reading must not synchronously query Interaction during public response composition.

Internal counter freshness/version metadata is not exposed publicly in V1.
```

---

## 7. View Contribution

Reading does not expose view mutation endpoints.

After successful article rendering, a client may separately call Interaction:

### Interaction-Owned Endpoint

```http
POST /api/v1/articles/{articlePublicId}/views
```

### Runtime Shape

```text
Reading returns article detail response
    -> Client separately sends view contribution to Interaction
    -> Interaction applies eligibility / abuse / suppression rules
    -> Interaction updates accepted view count where appropriate
    -> Interaction later publishes newer counter snapshot
    -> Reading eventually reflects newer displayed counter
```

### Rules

```text
Reading response success must not depend on view contribution success.

Reading must not wait for view persistence.

A failed, suppressed or duplicated view contribution must not change
whether article content was safely served.

View and counter truth remain owned by Interaction.
```

---

## 8. Error and Failure Behavior

### Safe Not Found

The following should return safe not-found behavior:

```text
Missing article projection
Locally non-public article
Locally unsafe article visibility
Missing slug route projection
Inactive slug route
Slug route requiring resync
Slug route pointing to missing or non-public article projection
```

Recommended public error code:

```text
READING.NOT_FOUND
```

### Service Failure

If required Reading serving infrastructure itself is unavailable, the API may return a standard service error according to common API standards.

### Safe Degradation

| Missing / delayed data | Public behavior |
|---|---|
| Cover/media enrichment | Null or omitted media fields |
| SEO metadata on detail-by-id | Null or omitted SEO fields |
| Counter snapshot | Zero/default or last-known counters |
| Related result state | Empty/deterministic fallback |
| Search optional enrichment | Omit safely |

---

## 9. Versioning and Route Conventions

### API Versioning

```text
All endpoints are exposed beneath /api/v1.
```

### Route Registration

Static routes must be registered before dynamic article-id routes, or protected with route constraints.

Static routes:

```text
/articles/search
/articles/slug/{slug}
```

Dynamic route:

```text
/articles/{articlePublicId}
```

### Public Response Boundaries

Public APIs must not expose internal projection metadata such as:

```text
ContentSourceVersion
SeoSourceVersion
MediaSourceVersion
InteractionStatsVersion
Last applied MessageId values
RequiresResync reason/details
Consumed-message apply state
Outbox or broker metadata
Internal visibility diagnostic reason
```

---

## 10. V1 Endpoint Summary

| Method | Endpoint | Purpose | Serving dependency |
|---|---|---|---|
| `GET` | `/articles` | Public article list | `ArticleReadModel` |
| `GET` | `/articles/{articlePublicId}` | Public detail by id | `ArticleReadModel` |
| `GET` | `/articles/slug/{slug}` | Public detail by slug | `ArticleSeoRouteProjection` + `ArticleReadModel` |
| `GET` | `/articles/{articlePublicId}/related` | Related public articles | Reading-owned public projection/query state |
| `GET` | `/articles/search` | Basic public search | Reading-owned public projection/search state |

Interaction-owned client-side follow-up endpoint:

| Method | Endpoint | Purpose | Owner |
|---|---|---|---|
| `POST` | `/articles/{articlePublicId}/views` | Submit public view contribution | Interaction |

---

## 11. V1 Non-Goals

Reading API V1 does not provide:

```text
Popularity/trending sorting
Synchronous SEO route resolution during public slug reads
Synchronous Content visibility fallback
Synchronous Media presentation fallback
Synchronous Interaction counter fallback
Draft or admin preview
Personalized recommendations
Raw interaction analytics
Public projection diagnostic endpoints
Public exposure of source-version/freshness internals
```

---

## 12. Final V1 Public API Posture

```text
Reading public endpoints serve from Reading-owned async projections.

List/detail/search/related use ArticleReadModel and Reading-owned query state.

Slug detail uses ArticleSeoRouteProjection plus ArticleReadModel.

SEO remains route truth owner, but is not called synchronously
in ordinary slug request handling.

Interaction remains counter truth owner and publishes versioned
counter snapshots consumed by Reading.

Media and counters may lag safely.

Missing or unsafe local article/route state fails closed.

Bounded propagation lag before Reading receives newer upstream snapshots
is accepted and must be observable.

Popularity/trending is deliberately deferred beyond V1.
```