# Reading — Open Questions & ADR Hooks (V1 Async Projections)

## Purpose

This document tracks Reading-specific deferred decisions and future ADR hooks.

It does not reopen decisions already accepted for Reading V1.

Reading V1 is a fully asynchronous public serving module built from Reading-owned projections.

---

## 1. Accepted V1 Constraints

The following decisions are already accepted and are not open questions:

```text
Reading does not own article truth.

Content owns article lifecycle, editorial and publication truth.

SEO owns canonical slug, route and metadata truth.

Media owns media asset and presentation truth.

Interaction owns engagement and public counter truth.

Reading owns public serving projections and public response composition.

Reading serves ordinary public requests from Reading-owned projections only.

Slug-based public detail uses Reading.ArticleSeoRouteProjection,
not synchronous SEO route resolution.

Interaction publishes versioned known-value counter snapshots consumed by Reading.

Reading does not consume raw view events or blindly increment counters.

Each source lane uses its own version marker.

Core route or visibility uncertainty fails closed locally.

Optional media/counter lag degrades safely.

Bounded eventual-consistency lag before Reading receives an upstream change is accepted in V1.

RabbitMQ is transport, not permanent replay storage.
```

---

## ADR-01: Popularity and Trending Pipeline

### Status

```text
Deferred beyond V1.
```

### Current V1 Decision

Reading V1 does not expose popularity/trending as an established sort pipeline.

Baseline sort options are limited to publication-time ordering:

```text
-publishedAt
publishedAt
```

Interaction counters may be displayed as enrichment, but they do not establish a ranking contract.

### Questions for a Future ADR

- Should popularity be based on views, likes, visible comments, shares, reading time or a blended score?
- Should ranking use lifetime values or bounded time windows?
- Should score decay over time?
- Should abuse-filtered or bot-filtered signals be required?
- Should ranking be produced by Interaction, Reading or a separate analytics pipeline?
- What event-time, lateness, rebuild and cutover policy is required?

### Required Preservation Rule

```text
Popularity or trending must never override public visibility rules.
```

---

## ADR-02: Canonical Redirect and Historical Slug Behavior

### Status

```text
Partially decided; redirect behavior remains open.
```

### Current V1 Decision

Reading exposes public article detail by slug through local async projection state:

```text
GET /api/v1/articles/slug/{slug}
    -> Reading queries ArticleSeoRouteProjection
    -> Reading loads ArticleReadModel by ArticlePublicId
    -> Reading validates local route safety and public visibility
    -> Response or safe 404
```

Accepted rules:

```text
SEO owns canonical routing truth.

Reading consumes SEO route/metadata projection asynchronously.

Reading does not call SEO synchronously in the ordinary slug hot path.

Missing, inactive or RequiresResync route state fails closed locally.

Route success does not itself grant public visibility.
```

### Questions for a Future ADR

- Should previous slugs redirect to the current canonical slug or return `404`?
- What response code should canonical redirects use?
- How long are historical routes retained?
- How should redirect chains be prevented?
- Should route projection include redirect targets explicitly?
- How should route cache invalidation work after slug changes?
- What route-lag SLO is required for newly published or deactivated routes?

---

## ADR-03: Search Capability

### Status

```text
Open for implementation design.
```

### Current V1 Decision

Search, when implemented, reads from Reading-owned public projection state only.

Rules:

```text
Search must return only safely public articles.

Search output is derived and may lag.

Search must not synchronously query Content or SEO during ordinary requests.

Stale or incomplete search state must not expose locally known non-public content.
```

### Questions for a Future ADR

- Should V1 start with SQL keyword search against Reading projection?
- Should SQL Server full-text search be used?
- Should an external search backend be introduced later?
- Which filters are required: category, tag, author, date?
- Is relevance scoring required?
- Should search index rebuild use candidate-before-cutover?
- What query abuse and rate-limiting controls are required?

---

## ADR-04: Reading Cache Policy

### Status

```text
Cache posture decided; operational policy remains open.
```

### Current V1 Decision

```text
Cache is acceleration only.

Cache stores Reading-derived serving data, not upstream truth.

Cache must not bypass local route or visibility checks.

Locally known deny/unsafe state wins over cached public output.

Cache misses do not trigger implicit synchronous calls to source modules.
```

### Candidate Invalidation Inputs

```text
Content projection applied:
    published / updated / unpublished / archived / soft-deleted state

SEO route projection applied:
    route activated / changed / deactivated / marked unsafe

Media projection applied:
    public cover or media presentation changed

Interaction projection applied:
    interaction.article_counters_projection_published
```

### Questions for a Future ADR

- Which endpoints are cacheable: list, detail, slug detail, search or related?
- What TTL applies to each response type?
- Is invalidation event-driven, TTL-based or both?
- Should cache keys include projection versions?
- Is stale-while-revalidate allowed for optional enrichments?
- How should cache stampede protection work?
- Should visibility/route denial invalidate public cache entries immediately?

---

## ADR-05: Counter Response Contract

### Status

```text
Projection source decided; response shape remains open.
```

### Current V1 Decision

Interaction owns public counter truth and publishes:

```text
interaction.article_counters_projection_published
```

Reading consumes known-value snapshots containing:

```text
ArticlePublicId
ViewCount
LikeCount
VisibleCommentCount
StatsVersion
OccurredAtUtc
```

Reading applies counters only when:

```text
IncomingStatsVersion > CurrentInteractionStatsVersion
```

Reading does not:

```text
Consume raw individual view events.
Blindly increment counters from message delivery.
Synchronously query Interaction in public read requests.
Use counters as article visibility authority.
```

### Questions for a Future ADR

- Should public list responses include counters?
- Should public detail responses always include counters?
- Before the first snapshot arrives, should counters return `0`, `null` or be omitted?
- Should APIs expose `countersPartial`, `countersStale` or freshness metadata?
- What publication/coalescing frequency should Interaction use for view-heavy updates?
- What lag SLO is acceptable for displayed counters?

---

## ADR-06: Exact Source Projection Event Contracts

### Status

```text
Direction decided; exact payload schemas remain open.
```

### Current V1 Decision

Reading consumes snapshot-shaped asynchronous projection input from:

| Producer | Reading projection impact |
|---|---|
| Content | `ArticleReadModel` core article and visibility fields |
| SEO | `ArticleSeoRouteProjection` route and metadata fields |
| Media | Optional public media presentation fields |
| Interaction | Public counter snapshot fields |

Interaction event type already adopted:

```text
interaction.article_counters_projection_published
```

General event rules:

```text
Messages must carry MessageId, EventType, source identity,
source-specific version, OccurredAtUtc and CorrelationId where available.

Snapshot-shaped messages are preferred.

Delta-shaped messages require explicit ordering or resync policy.
```

### Questions for a Future ADR or Contract Document

- What are the final Content → Reading event names and payloads?
- What is the final SEO → Reading route projection event name and payload?
- What is the final Media → Reading public presentation event name and payload?
- Which schema-versioning strategy is used?
- Which fields are required versus nullable?
- How are backward-compatible event changes introduced?
- How are resync-required messages represented?

---

## ADR-07: Reading Consumed-Message Persistence

### Status

```text
Reliability requirement decided; physical persistence design remains open.
```

### Current V1 Decision

Reading handlers require both:

```text
Message-level dedupe using MessageId and consumer identity.

Projection-level freshness using independent source-lane versions.
```

Required version lanes:

```text
ContentSourceVersion
SeoSourceVersion
MediaSourceVersion
InteractionStatsVersion
```

A single shared `SourceVersion` is not acceptable across all upstream modules.

### Questions for Implementation Design

- Should Reading have a durable table such as `ReadingConsumedMessage`?
- What apply decisions should be recorded: Applied, DuplicateIgnored, StaleIgnored, ResyncRequired, Failed?
- What retention period should consumed-message records use?
- Should rebuild/reconciliation consumers use the same dedupe table?
- What indexes are required for diagnostics and cleanup?

---

## ADR-08: Version Gap and Resync Behavior

### Status

```text
Apply principle decided; operational response remains open.
```

### Current V1 Decision

For known-value snapshot input:

```text
If IncomingVersion > CurrentAppliedVersion:
    apply snapshot
Else:
    ignore duplicate or stale snapshot
```

Reading does not use timestamps as freshness authority.

Unsafe/gapped projection state may require fail-closed behavior until repaired.

### Questions for a Future ADR

- Should any source lane require strict contiguous versions?
- Which gaps are safe for snapshot replacement?
- Which source lanes need a `RequiresResync` marker?
- What causes automatic resync versus operator remediation?
- How should consumer failure/DLQ behavior connect to repair?
- What alerts should fire for persistent resync-required state?

---

## ADR-09: Reading Rebuild and Cutover Strategy

### Status

```text
Rebuildability required; execution model remains open.
```

### Current V1 Decision

Reading-owned projections must be repairable or rebuildable from source-approved inputs.

Recovery sources:

| Reading state | Recovery input owner |
|---|---|
| Article core and visibility | Content |
| Slug route and SEO metadata | SEO |
| Public media presentation | Media |
| Interaction counters | Interaction |

Rules:

```text
RabbitMQ is not permanent replay history.

Repair updates Reading-owned derived state only.

Full rebuild must not expose incomplete candidate output as complete serving state.
```

### Questions for a Future ADR

- Should rebuild use direct idempotent upsert or candidate-before-cutover?
- Should Reading introduce rebuild-run tables?
- Are generation or fencing tokens needed for cutover?
- Should rebuild be full, per-source-lane or per-article scoped?
- What candidate validation rules apply before activation?
- How should old projection state be retained or cleaned up?

---

## ADR-10: Related Articles Strategy

### Status

```text
Open.
```

### Current V1 Decision

If related articles are exposed:

```text
Only safely public articles may appear.

The current article must be excluded.

Missing related state must not block article detail response.

A deterministic empty or fallback result is acceptable.
```

### Questions for a Future ADR

- Should related articles be computed on request or precomputed?
- Which signals should be used: category, tags, author, recency?
- Should Interaction counters ever contribute after a ranking ADR exists?
- How should deterministic tie-breaking work?
- Should related responses be cached?
- How should a related projection be repaired or rebuilt?

---

## ADR-11: Public Source Fallback Policy

### Status

```text
Normal hot-path fallback rejected for V1.
```

### Current V1 Decision

Ordinary public reads use Reading-owned projections only:

```text
List / detail by id / search / related
    -> Reading projections only.

Detail by slug
    -> Reading ArticleSeoRouteProjection
    -> Reading ArticleReadModel.
```

Reading does not use synchronous fallback to:

```text
Content
SEO
Media
Interaction
```

during normal public serving.

Behavior when local required state is missing or unsafe:

```text
Missing/unsafe article visibility projection -> safe deny / 404.

Missing/unsafe slug route projection -> safe deny / 404.

Missing optional media/counter fields -> degrade safely.
```

### Future Reconsideration Hook

A later ADR may explicitly introduce emergency or operator-controlled fallback only if it defines:

- exact endpoints;
- safety requirements;
- dependency/time budgets;
- observability;
- disable controls;
- proof that fallback does not become hidden ownership.

---

## ADR-12: Public Preview and Draft Access

### Status

```text
Deferred.
```

### Current V1 Decision

```text
Public Reading routes do not serve drafts.

Admin/editor preview is outside public Reading V1.

Preview must not be silently added to normal public visibility rules.
```

### Questions for a Future ADR

- Which module owns preview query behavior?
- Should preview read Content truth or a dedicated secured projection?
- What authorization model is required?
- Are signed/tokenized preview URLs permitted?
- How is preview content excluded from public cache and search?
- What audit rules apply?

---

## ADR-13: Personalized Reading Experience

### Status

```text
Deferred.
```

### Current V1 Decision

```text
Public Reading V1 is anonymous-safe.

User-specific recommendation or reading-history behavior is not included.
```

### Questions for a Future ADR

- Should personalized recommendations be supported?
- Which module owns recommendation truth/output?
- Which Interaction signals may be used?
- What privacy/consent model applies?
- How do personalized responses affect caching?
- How are anonymous and authenticated responses separated safely?

---

## ADR-14: External Search or Recommendation Services

### Status

```text
Deferred.
```

### Current V1 Decision

```text
No external search or recommendation service is required in V1.

Any later external index remains derived and subordinate
to Reading local public visibility enforcement.
```

### Questions for a Future ADR

- Which provider or technology should be used?
- What source data may be sent externally?
- How is external index deletion/deactivation synchronized?
- How is hidden-content exposure prevented under stale external state?
- What fallback applies when the provider is unavailable?
- How is rebuild or reindex performed?

---

## ADR-15: Projection Freshness SLO

### Status

```text
Required before production hardening.
```

### Current V1 Decision

Projection propagation is asynchronous.

Therefore:

```text
Newly public content may temporarily be absent from Reading.

New or changed routes may temporarily be unavailable by slug.

Upstream non-public or route-deactivation changes may remain unseen by Reading
until corresponding snapshots are applied.

Once local state is known non-public or unsafe, Reading must fail closed.

Optional media/counter lag may degrade safely.
```

### Questions for a Future ADR

- What is acceptable Content → Reading lag after publish?
- What is acceptable Content → Reading lag after unpublish/archive/delete?
- Should visibility-denying snapshots receive stricter priority/SLO?
- What is acceptable SEO → Reading route activation/deactivation lag?
- What is acceptable Interaction counter snapshot lag?
- What metrics trigger reconciliation or operational alerts?
- Should prolonged lag automatically mark local route/visibility state unsafe?

---

## ADR-16: Media Projection Adoption Boundary

### Status

```text
Direction accepted; exact V1 scope to confirm.
```

### Current V1 Decision

Media remains the owner of media lifecycle and article-media presentation truth.

Reading may serve projected cover/media fields without synchronous Media calls.

Rules:

```text
Media enrichment never determines article visibility.

Missing media fields degrade safely.

MediaSourceVersion is independent from Content, SEO and Interaction versions.
```

### Questions for Implementation

- Is cover-only media projection required in initial Reading V1, or deferred?
- What event name/payload does Media publish?
- Does Reading store gallery state or cover state only initially?
- What is the safe default when media has been removed but the new projection is delayed?

---

## Summary of Decisions Already Closed

The following issues are no longer open for Reading V1:

| Topic | Closed V1 decision |
|---|---|
| Public serving source | Reading-owned async projections only |
| Slug lookup | Local `ArticleSeoRouteProjection`, not synchronous SEO resolve |
| Interaction counters | Consume `interaction.article_counters_projection_published` |
| Counter update behavior | Set known values by newer `StatsVersion`; no blind increments |
| Version model | Independent version per source lane |
| Public sync fallback | Not used in normal public hot path |
| Popularity sort | Deferred beyond V1 |
| Raw view analytics in Reading | Not owned by Reading V1 |
| Core missing/unsafe local state | Fail closed locally |
| Optional counter/media lag | Degrade safely |

---

## Open Items Before Production Hardening

The remaining high-value decisions are:

1. Final source projection event schemas for Content, SEO and Media.
2. Reading consumed-message persistence and retention design.
3. Version-gap and resync operational policy.
4. Rebuild/cutover strategy.
5. Cache TTL and invalidation policy.
6. Counter API response/freshness exposure policy.
7. Canonical redirect and historical slug behavior.
8. Projection freshness SLOs, especially visibility denial and route deactivation.
9. Search and related-content implementation strategy.
10. Optional media projection scope for initial delivery.