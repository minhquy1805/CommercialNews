# Reading — Business Rules (V1 Async Projections)

## Purpose

This document defines business rules for the Reading module.

Reading is the public serving module for CommercialNews.

Reading owns:

```text
Public article query behavior
Public response composition
Reading-owned serving projections
Local public visibility enforcement
Local route safety enforcement
Safe degradation behavior
Projection repair/rebuild behavior
```

Reading does not own source truth.

Source truth remains owned by:

| Concern | Source owner |
|---|---|
| Article lifecycle, editorial state and public visibility truth | Content |
| Canonical slug, route and metadata truth | SEO |
| Media asset and public presentation truth | Media |
| Views, likes, comments, moderation and public counter truth | Interaction |

Reading follows source truth asynchronously and serves ordinary public requests entirely from Reading-owned projections.

---

## Rule 1: Reading serves public responses from local projections only

Ordinary public requests must be served from Reading-owned projection state.

### Article by public id

```text
Public API
    -> Reading.ArticleReadModel
    -> Local public visibility check
    -> Response or safe 404
```

### Article by slug

```text
Public API
    -> Reading.ArticleSeoRouteProjection by Scope + Slug
    -> Reading.ArticleReadModel by ArticlePublicId
    -> Local route and public visibility checks
    -> Response or safe 404
```

Reading must not synchronously query Content, SEO, Media or Interaction during ordinary public article response composition.

---

## Rule 2: Reading serves public content only

Reading may return an article publicly only when its local article projection confirms:

```text
Status = Published
AND IsPublic = true
AND local visibility state is not unsafe / requires-resync
```

Reading must not publicly serve locally known:

```text
Draft content
Unpublished content
Archived content
Soft-deleted content
Visibility-unsafe content
```

---

## Rule 3: Missing or unsafe local visibility means not public

If Reading does not have safe local article visibility state, it must fail closed.

```text
Missing ArticleReadModel
OR local visibility denied
OR local visibility unsafe / requires-resync
    -> not public
```

Allowed behavior:

```text
Return safe 404 for detail requests.
Omit the article from list/search/related results.
Trigger repair or reconciliation outside the request path.
```

Not allowed:

```text
Optimistically expose content.
Synchronously call Content to repair an ordinary public request.
Use media or counters as evidence of public visibility.
Use cached public output when local state is known denied or unsafe.
```

---

## Rule 4: Safe not-found behavior protects hidden resource existence

Public clients must not be able to infer hidden article existence from Reading responses.

These cases should normally collapse into safe not-found behavior:

```text
Article does not exist in Reading projection.
Article is draft.
Article is unpublished.
Article is archived.
Article is soft-deleted.
Article visibility is locally unsafe.
Slug route is missing, inactive or unsafe.
Slug route points to a locally non-public article.
```

Recommended public error:

```text
READING.NOT_FOUND
```

---

## Rule 5: Reading owns projection state, not source facts

Reading may store copied or denormalized source data for serving performance.

| Projected data in Reading | Source owner |
|---|---|
| Title, summary, body, category and public article status | Content |
| Slug route, canonical URL and SEO metadata | SEO |
| Cover/public media presentation | Media |
| View, like and visible-comment counters | Interaction |

Rules:

```text
Reading owns the serving copy.

Source modules own the authoritative facts.

Reading repair may update Reading projection state only.

Reading must not mutate upstream truth.
```

---

## Rule 6: Reading does not mutate article lifecycle truth

Reading must not:

```text
Create article truth
Edit article truth
Publish or unpublish articles
Archive or restore articles
Soft-delete or hard-delete articles
Decide whether lifecycle transitions are legal
```

Those responsibilities belong to Content.

Reading only projects committed Content outcomes for public serving.

---

## Rule 7: Slug lookup uses `ArticleSeoRouteProjection`

SEO owns canonical route truth.

Reading owns a local serving projection:

```text
ArticleSeoRouteProjection
```

Public slug lookup must use:

```text
Scope = public
AND Slug = requested slug
AND IsActive = true
AND RequiresResync = false
```

Then Reading must load `ArticleReadModel` by `ArticlePublicId` and verify local public visibility.

Reading must not use synchronous SEO route resolution in the ordinary V1 public hot path.

---

## Rule 8: A valid route never overrides article visibility

A local slug route match is necessary for slug-based reads, but it is not enough to expose an article.

```text
Valid ArticleSeoRouteProjection
    + public ArticleReadModel
    -> may serve public response
```

```text
Valid ArticleSeoRouteProjection
    + missing/non-public/unsafe ArticleReadModel
    -> safe 404
```

Route existence must never make locally hidden content publicly readable.

---

## Rule 9: Missing or unsafe route state fails closed

For requests by slug, Reading must return safe not-found behavior when:

```text
Route projection does not exist.
Route projection is inactive.
Route projection requires resync.
Route projection points to no local article projection.
Target article is locally non-public or unsafe.
```

Reading must not synchronously call SEO to bypass missing or unsafe local route state in normal public requests.

---

## Rule 10: Reading follows source truth asynchronously

Reading projection updates occur after source truth commits.

Typical flow:

```text
Source module commits truth + OutboxMessage
    -> Worker publishes message
    -> RabbitMQ delivers message
    -> Reading consumer receives message
    -> Reading applies newer projection snapshot locally
```

Source modules must not wait for Reading projection completion as part of their truth transactions.

A successful upstream commit does not guarantee that Reading has already caught up.

---

## Rule 11: Bounded eventual-consistency lag is accepted in V1

Because Reading is asynchronous, projection lag can occur.

### After publish or route activation

```text
New article may temporarily be absent from public list/detail.
New slug may temporarily return safe 404.
```

### After unpublish, archive, delete or route deactivation

```text
Reading may temporarily retain previously applied public state
until the newer source snapshot arrives and is applied.
```

This is an accepted bounded eventual-consistency limitation in V1.

Required controls:

```text
Projection lag must be measurable.
Non-public and route-deactivation snapshots should receive operational priority.
Stale snapshots must not overwrite newer deny state.
Repair/reconciliation must exist for detected drift.
Once local state is known denied or unsafe, Reading must fail closed immediately.
```

Reading must not claim immediate global visibility consistency.

---

## Rule 12: Each source lane has its own version marker

Reading receives asynchronous projection input from independent modules.

Reading must track version separately per source lane:

| Source lane | Required freshness marker |
|---|---|
| Content article projection | `ContentSourceVersion` |
| SEO route projection | `SeoSourceVersion` |
| Media presentation projection | `MediaSourceVersion` |
| Interaction counter snapshot | `InteractionStatsVersion` |

Rules:

```text
Versions from different source modules are not comparable.

Reading must not use one shared SourceVersion across all upstream projection data.
```

---

## Rule 13: Reading applies only newer source-lane snapshots

For each independent source lane, Reading must apply:

```text
If IncomingVersion > CurrentAppliedVersion:
    apply known-value snapshot
Else:
    ignore as duplicate or stale
```

This rule should be enforced at the repository or stored-procedure boundary, not only in application memory.

Examples:

```text
Older Content snapshot must not re-expose a locally hidden article.

Older SEO snapshot must not reactivate a locally deactivated route.

Older Interaction snapshot must not overwrite newer counters.

Older Media snapshot must not overwrite newer cover presentation.
```

---

## Rule 14: Message dedupe and version protection solve different problems

Reading must handle both duplicate delivery and stale delivery.

### Duplicate message

```text
Same MessageId delivered again.
```

Protection:

```text
Message-level dedupe using ConsumerName + MessageId.
```

Expected behavior:

```text
No repeated projection effect.
No repeated downstream effect.
Safe acknowledgement or recorded duplicate decision.
```

### Stale message

```text
Different message carries an older source-lane version.
```

Protection:

```text
Source-specific version check.
```

Expected behavior:

```text
Do not overwrite newer local state.
Record stale-ignore decision where required.
```

Message-level dedupe alone is insufficient.

---

## Rule 15: Reading uses snapshot application, not blind deltas

Reading V1 should consume known-value snapshot-shaped inputs.

Approved behavior:

```text
Set projected state to values from a newer source snapshot.
```

Disallowed behavior for public counters:

```text
On every delivered interaction event:
    ViewCount = ViewCount + 1
```

At-least-once delivery, retry and replay make blind increment behavior unsafe unless a separate strict dedupe/order contract exists.

---

## Rule 16: Timestamps are diagnostic, not ordering authority

Reading must not use wall-clock timestamp ordering to resolve projection freshness.

Do not use:

```text
Largest UpdatedAtUtc wins.
Largest OccurredAtUtc wins.
Latest ProcessedAtUtc wins.
```

Use:

```text
Source lane + source-specific version.
```

Timestamps may be used for:

```text
Display
Operational investigation
Lag measurement
Reporting
Repair scheduling
```

---

## Rule 17: Search returns only locally safe public content

Search must read from Reading-owned public projection/search state.

Search must not return locally known:

```text
Draft articles
Unpublished articles
Archived articles
Soft-deleted articles
Visibility-unsafe articles
```

Rules:

```text
Search output is derived and may lag.

Safe omission is preferred over exposing locally denied or unsafe content.

Search must not synchronously query Content or SEO in ordinary public requests.
```

---

## Rule 18: Related articles return only locally safe public content

Related articles must never include:

```text
The current article itself
Locally non-public articles
Locally visibility-unsafe articles
```

Rules:

```text
Missing related state must not block article detail response.

A deterministic empty result or deterministic fallback is acceptable.

Interaction counters do not establish related ranking in V1.
```

Related ranking enhancements are deferred until separately designed.

---

## Rule 19: Media presentation is optional enrichment

Media owns asset and presentation truth.

Reading may project fields such as:

```text
CoverMediaPublicId
CoverMediaUrl
CoverAlt
```

Rules:

```text
Media projection must not determine article visibility.

Missing media must not block a safely public article response.

Reading must not synchronously query Media to patch ordinary public responses.

Media snapshots must apply by MediaSourceVersion.
```

Safe degradation:

```text
Missing cover/media -> null or omitted public media fields.
```

---

## Rule 20: Interaction owns counters and engagement truth

Interaction owns:

```text
Accepted view counting
Like truth
Comment truth
Moderation/report truth
ArticleInteractionStats
Counter snapshot publication
```

Reading owns only the consumed display copy of counters:

```text
ViewCount
LikeCount
VisibleCommentCount
InteractionStatsVersion
```

Reading must not:

```text
Create or mutate Interaction truth.
Reconstruct Interaction truth from displayed counters.
Use counters as evidence of article visibility.
Synchronously query Interaction during ordinary public reads.
```

---

## Rule 21: Reading consumes versioned counter snapshots only

Interaction publishes:

```text
interaction.article_counters_projection_published
```

Expected payload:

```text
ArticlePublicId
ViewCount
LikeCount
VisibleCommentCount
StatsVersion
OccurredAtUtc
```

Reading apply rule:

```text
If IncomingStatsVersion > CurrentInteractionStatsVersion:
    Set ViewCount = incoming ViewCount
    Set LikeCount = incoming LikeCount
    Set VisibleCommentCount = incoming VisibleCommentCount
    Set InteractionStatsVersion = incoming StatsVersion
Else:
    Ignore duplicate or stale counter snapshot
```

Reading V1 must not:

```text
Consume one event per article view.
Increment public totals from raw like/comment/view messages.
Calculate popularity or trending from raw interaction behavior.
```

---

## Rule 22: Counter lag degrades safely

Counters are optional public enrichments.

If no Interaction counter snapshot has arrived:

```text
Return documented zero/default counters.
```

If a previously applied snapshot exists but newer interaction activity has not yet propagated:

```text
Return last-known counters.
```

Counter lag must not:

```text
Block public article response.
Make article non-public.
Make article public.
Cause synchronous fallback to Interaction.
```

---

## Rule 23: View contribution is separate from article serving

Reading does not own view persistence.

Preferred flow:

```text
Reading returns public article detail successfully
    -> Client separately sends view contribution to Interaction
    -> Interaction applies eligibility and abuse/repeat-view policy
    -> Interaction updates accepted view count when appropriate
    -> Interaction eventually publishes newer counter snapshot
    -> Reading eventually applies newer counters
```

Rules:

```text
Article response success must not depend on view contribution success.

Reading must not wait for view recording.

Interaction failure must not invalidate an already served article response.
```

---

## Rule 24: Popularity and trending are deferred beyond V1

Reading V1 does not implement:

```text
Popularity sorting
Trending ranking
Reading-side interaction scoring
Raw engagement analytics
```

Allowed V1 sorting:

```text
-publishedAt
publishedAt
```

Reading may display projected counters, but those counters do not constitute a popularity pipeline.

A later popularity/trending design must define:

```text
Time-window semantics
Accepted signal ownership
Abuse-filtered input policy
Score computation owner
Versioned publication contract
Repair/rebuild behavior
```

---

## Rule 25: Cache is acceleration only

Reading cache may accelerate responses derived from safe Reading projection state.

Cache rules:

```text
Cache must not become source truth.

Cache must not trigger synchronous upstream fallback on miss.

Locally known route/visibility denial or unsafe state wins over cached public output.

Cache refresh failure must not break a safe projection-backed response.

Internal projection metadata must not be exposed through public cache responses.
```

When optional cached enrichments are stale:

```text
Media/counters may degrade according to documented response policy.
```

---

## Rule 26: Reading does not use synchronous source fallback in normal public paths

Normal public serving must not call:

```text
Content to confirm visibility
SEO to resolve slug
Media to retrieve missing presentation
Interaction to retrieve fresher counters
```

when Reading-owned projection state is missing or stale.

Required behavior:

| Missing state | Public behavior |
|---|---|
| Article core/visibility projection missing or unsafe | Safe 404 / omit |
| Slug route projection missing or unsafe | Safe 404 |
| Media enrichment missing | Null/omit media |
| Counter enrichment missing | Zero/default counters |

An emergency or operator-controlled fallback may be introduced only by a later explicit ADR.

---

## Rule 27: Projection diagnostics are internal-only

Public responses must not expose internal operational metadata such as:

```text
ContentSourceVersion
SeoSourceVersion
MediaSourceVersion
InteractionStatsVersion
Last source message ids
Consumed-message identifiers
Apply decisions
RequiresResync reason
Consumer retry state
Outbox identifiers
Audit evidence
Admin-only fields
```

These fields are for internal diagnostics, observability, repair and operations only.

---

## Rule 28: Reading must not block public requests for async catch-up

Reading must not hold public requests open waiting for:

```text
Outbox publication
RabbitMQ delivery
Consumer catch-up
SEO route projection repair
Interaction counter materialization
Media projection update
Cache refresh
Repair/reconciliation
Rebuild/cutover
Search reindex
Related-content regeneration
```

Required behavior:

```text
Core projection unavailable or unsafe -> fail closed.

Optional enrichment unavailable -> degrade safely.
```

---

## Rule 29: Reading projection consumers use bounded local transactions

A Reading consumer transaction may update:

```text
Reading-owned projection rows
Source-specific version metadata
Consumed-message / apply-decision state
Local repair/resync diagnostics
```

It must not update:

```text
Content truth
SEO truth
Media truth
Interaction truth
Notification truth
Audit truth
```

Source producers must not wait for Reading consumer completion as part of source truth transactions.

---

## Rule 30: Repair and reconciliation are outside the public request path

Reading projections are derived and must be recoverable.

Approved repair/rebuild inputs:

| Reading state | Input owner |
|---|---|
| Article content and visibility | Content |
| Slug route and metadata | SEO |
| Media presentation | Media |
| Public counters | Interaction |

Rules:

```text
Repair/rebuild updates Reading-owned projection state only.

Repair must not mutate upstream source truth.

Repair must not be hidden inside ordinary public requests.

RabbitMQ is transport, not permanent replay storage.
```

---

## Rule 31: Rebuild and repair must be rerun-safe

Rerunning repair or rebuild on the same bounded scope must not:

```text
Duplicate article projections
Reactivate stale routes
Double-count counters
Overwrite newer state with older snapshots
Expose locally known non-public content
Publish incomplete candidate output as complete serving state
```

For broad rebuilds, preferred shape:

```text
Build candidate
    -> Validate candidate safety/completeness
    -> Cut over only when acceptable
```

---

## Rule 32: Timeout and transport ambiguity do not permit unsafe serving

A timeout does not prove:

```text
A source message was never published.
A Reading consumer apply did not commit.
An article does not exist upstream.
A route does not exist upstream.
A counter snapshot does not exist upstream.
```

Despite this ambiguity, public request behavior remains local-projection based:

```text
No safe local core projection -> safe deny / 404.

Missing optional enrichment -> degrade safely.
```

Reading must not introduce synchronous source calls in the public hot path merely to resolve ambiguity.

---

## Rule 33: Static routes must avoid dynamic route conflicts

Static routes such as:

```text
/articles/search
/articles/slug/{slug}
```

must be registered before:

```text
/articles/{articlePublicId}
```

or protected with route constraints.

This prevents static actions from being interpreted as article ids.

---

## Rule 34: Reading remains anonymous-safe in V1

Public Reading endpoints are designed for anonymous public access.

Reading V1 must not silently mix user-specific state into anonymous responses.

Deferred capabilities requiring separate policy include:

```text
Draft or admin preview
Personalized recommendations
User-specific reading history
User-specific ranking
Authenticated personalized caching
```

---

## Rule 35: External search or recommendation state remains derived

If Reading later integrates an external search or recommendation system:

```text
External output remains derived.
External output must not override local Reading visibility.
Stale external state must not expose locally known hidden content.
Rebuild/reindex behavior must be documented.
Failure and fallback behavior must be explicit.
```

---

## Rule 36: Correctness precedes completeness and freshness

Reading exists to make public article serving fast, but public exposure safety is more important than enrichment completeness.

Priority order:

```text
Correctness first.
Completeness second.
Freshness third.
```

Performance or availability must not justify:

```text
Serving locally known hidden content
Bypassing local route/visibility safety
Treating stale events as newer state
Synchronously coupling normal public requests to source modules
Inventing missing enrichment beyond documented defaults
Exposing partial rebuild output as complete
```

---

## Summary of V1 Business Decisions

| Topic | V1 decision |
|---|---|
| Public serving source | Reading-owned async projections only |
| Article visibility owner | Content truth projected into Reading |
| Slug route owner | SEO truth projected into `ArticleSeoRouteProjection` |
| Slug public read path | Reading route projection → Reading article projection |
| Media behavior | Optional async enrichment; degrade safely |
| Counter owner | Interaction |
| Counter inbound contract | `interaction.article_counters_projection_published` |
| Counter application | Known-value snapshot apply by `StatsVersion` |
| Popularity/trending | Deferred beyond V1 |
| Versioning | Independent version per source lane |
| Message delivery | At-least-once; duplicate/stale safe |
| Sync source fallback | Not used in ordinary public serving |
| Missing/unsafe core local state | Fail closed |
| Optional media/counter lag | Safe degradation |
| Eventual consistency | Bounded lag accepted before newer upstream state reaches Reading |
| Repair/rebuild | Reading-owned state only; rerun-safe |