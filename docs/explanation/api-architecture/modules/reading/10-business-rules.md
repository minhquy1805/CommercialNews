# Reading — Business Rules (V1)

## Purpose

This document defines business rules for the Reading module.

Reading is the public read-side module for CommercialNews.

Reading owns public serving behavior and Reading-owned projections.

Reading does not own source truth.

Source truth remains owned by:

* Content for article lifecycle and editorial data
* SEO for slug and canonical routing data
* Media for media asset and primary media data
* Interaction for views, likes, comments, and engagement counters

Reading follows source truth asynchronously and serves public APIs from Reading-owned projections.

For list, detail-by-public-id, search, and related articles, the normal public path reads from Reading-owned projections.

For slug-based reads, baseline V1 first resolves the route through SEO, then loads the Reading projection by `ResourcePublicId`, and finally applies Reading public visibility rules.

---

## Rule 1: Reading serves public content only

Reading public APIs must only return content that is safe for public exposure.

Public content requires:

* source-derived status is `Published`
* projection `IsPublic = true`
* article is not archived
* article is not soft-deleted
* SEO route resolution returns an active public route if accessed by slug
* visibility is not uncertain

If any condition is not satisfied, Reading must not return the article publicly.

---

## Rule 2: Unknown visibility means not public

If Reading cannot determine whether an article is public, Reading must fail closed.

```text
Unknown visibility => not public
```

Allowed behavior:

* return safe `404`
* omit from list/search/related results
* defer until projection catches up
* use explicit fallback only if policy confirms visibility safely

Not allowed:

* optimistic exposure
* trusting stale cache
* trusting stale projection
* trusting slug match alone
* exposing content because enrichment data exists

---

## Rule 3: Safe 404 is preferred over incorrect exposure

Reading must prefer safe `404` over leaking non-public content.

Public clients must not be able to distinguish:

* article does not exist
* article exists but is draft
* article exists but unpublished
* article exists but archived
* article exists but soft-deleted
* slug points to hidden content
* projection visibility is uncertain

These cases should normally collapse into:

```text
READING.NOT_FOUND
```

---

## Rule 4: Reading does not mutate article truth

Reading must not create, edit, publish, unpublish, archive, restore, or delete article truth.

All editorial and lifecycle mutations belong to Content.

Reading may project the result of Content truth changes, but it must not decide lifecycle legality.

---

## Rule 5: Reading owns projection, not source facts

Reading may copy source data into its read model.

Copied data does not transfer ownership.

Examples:

| Data copied into Reading | Source owner |
|---|---|
| Title, summary, body | Content |
| Status | Content |
| Optional projected slug / SEO metadata | SEO |
| Cover media | Media |
| View count | Interaction |

Slug routing truth remains owned by SEO. Reading may store projected slug or SEO metadata as optional optimization data, but baseline slug-based reads must resolve the route through SEO.

Reading owns the projected copy.

The source module owns the source fact.

---

## Rule 6: Normal public paths use Reading projection; slug path uses SEO route resolution

Normal public path for list/detail-by-public-id/search/related:

```text
Public API
    ↓
Reading projection
    ↓
Response
```

Slug-based public path:

```text
Public API
    ↓
SEO route resolve
    ↓
Reading projection by ResourcePublicId
    ↓
Reading visibility check
    ↓
Response
```

Reading public APIs should not synchronously compose article bodies from Content, SEO, Media, and Interaction on every request.

Slug-based reads are the explicit baseline exception for route resolution only. SEO resolves `Scope + Slug -> ResourcePublicId`; Reading still serves article content from its projection and must still apply public visibility rules.

Source fallback beyond SEO route resolution may exist, but it must be:

* explicit
* bounded
* observable
* policy-controlled
* not the normal hot path
* not hidden cross-module ownership

---

## Rule 7: Reading follows source truth asynchronously

Reading projection is updated after source truth commits.

Typical flow:

```text
Source module commits truth + outbox
    ↓
Outbox worker publishes event
    ↓
RabbitMQ delivers event
    ↓
Reading consumer applies projection update
```

Source modules must not wait for Reading projection completion as part of their truth transaction.

A successful Content publish does not mean Reading projection has already caught up.

---

## Rule 8: Projection lag is expected

Reading projection may lag behind source truth.

After publish:

* article may not appear in list immediately
* article detail may temporarily return safe `404`
* search may not include the article yet

This is acceptable within SLO.

After unpublish/archive/soft-delete:

* stale public exposure is more dangerous
* if visibility becomes uncertain, Reading must fail closed
* reconciliation or repair must correct projection drift

---

## Rule 9: Reading must reject stale source versions

Reading must not allow older source events to overwrite newer projection state.

Approved rule:

```text
If IncomingVersion > CurrentSourceVersion:
    apply event
Else:
    ignore or reject as duplicate/stale
```

This rule should be enforced at the repository/stored procedure boundary, not only in application memory.

---

## Rule 10: Message duplicate and stale event are different cases

Reading must handle both duplicate delivery and stale delivery.

### Duplicate delivery

Same message arrives again.

Protection:

```text
MessageId
```

Expected behavior:

* no duplicate projection row
* no harmful side effect
* safe no-op

### Stale delivery

Older event arrives after newer projection state already exists.

Protection:

```text
SourceVersion
```

Expected behavior:

* ignore or reject stale event
* do not overwrite projection
* log/measure stale rejection

Message-level dedupe alone is not sufficient.

---

## Rule 11: Reading must not use timestamp last-write-wins

Reading must not use wall-clock timestamps as freshness authority.

Do not use:

* `largest UpdatedAtUtc wins`
* `largest OccurredAtUtc wins`
* `latest ProcessedAtUtc wins`

Use:

* `AggregateId + Version`
* `SourceVersion / LastAppliedVersion`

Timestamps are allowed for:

* display
* reporting
* investigation
* lag measurement
* scheduling

Timestamps are not ordering authority.

---

## Rule 12: SEO route resolution is not serve authority

A route match does not automatically mean the article is public.

Baseline V1 slug-based reads use SEO route resolution:

```text
GET /api/v1/articles/slug/{slug}
    ↓
SEO resolve with scope=public and slug
    ↓
SEO returns ResourcePublicId and route metadata
    ↓
Reading loads ArticleReadModel by ResourcePublicId
    ↓
Reading projection visibility check
    ↓
Response or safe 404
```

SEO owns canonical slug and routing truth.

Reading may store optional projected slug and SEO metadata, but projected slug lookup is not the baseline routing authority in V1.

Reading must still require public visibility to pass before serving content.

If SEO resolves a slug but Reading projection is missing, non-public, or visibility-uncertain, Reading must return safe `404` unless an explicit fallback policy safely confirms visibility.

---

## Rule 13: Source fallback beyond SEO route resolution must be explicit

SEO route resolution is part of the baseline slug-based read path.

It is not considered source fallback.

Source fallback beyond baseline SEO route resolution may be used only according to explicit policy.

Fallback must define:

* when it is allowed
* target source module
* timeout budget
* what happens if fallback fails
* how visibility is safely confirmed
* how fallback is logged and measured
* whether feature flags or disable switches are required

Fallback must not become hidden normal ownership.

---

## Rule 14: Search must only return public content

Search results must apply the same visibility rules as list/detail APIs.

Search must not return:

* draft articles
* unpublished articles
* archived articles
* soft-deleted articles
* visibility-uncertain articles

If search index/projection is stale, Reading must prefer safe omission over incorrect exposure.

---

## Rule 15: Related articles must only return public content

Related articles must never include:

* the current article
* non-public articles
* archived articles
* soft-deleted articles
* visibility-uncertain articles

Related results should be deterministic.

Recommended fallback order:

* same category
* shared tags
* same author if available
* newest public articles

---

## Rule 16: Optional enrichments may degrade safely

Optional enrichments include:

* counters
* cover media
* media gallery
* optional projected SEO metadata
* related signals
* popularity/trending signals
* summaries

Slug routing for slug-based reads is handled by explicit SEO route resolution in baseline V1. Projected slug fields inside Reading are optional optimization data and must not become public visibility authority.

If optional enrichments are missing, stale, or unavailable, Reading may:

* omit them
* return null
* return empty arrays
* return safe defaults
* return stale non-sensitive values where policy allows

Optional enrichment failure must not make public visibility more permissive.

---

## Rule 17: Counters are derived and may lag

Counters such as views, likes, and comments are owned by Interaction.

Reading may project counters for public display.

Counters may lag.

Counter lag must not affect article visibility.

Reading must not blindly increment counters under replay.

Disallowed pattern:

```text
On every delivered view event:
    ViewCount = ViewCount + 1
```

Preferred pattern:

```text
Set ViewCount to known aggregate value.
```

or:

```text
Deduplicate raw interaction event before incrementing.
```

---

## Rule 18: View tracking is owned by Interaction

Reading does not expose interaction write endpoints in V1.

Preferred flow:

```text
Reading returns article detail
    ↓
Client sends view signal to Interaction
    ↓
Interaction handles dedupe, counting, abuse policy, and aggregation
```

Reading success must not depend on view tracking success.

If view tracking fails, article response remains correct.

---

## Rule 19: Cache is acceleration only

Reading cache must not become hidden truth.

Cache rules:

* cache hit must not bypass projection visibility
* stale cache must not expose non-public content
* cache write must be skipped if visibility is uncertain
* cache refresh failure must not break safe projection-backed response
* cache must not expose internal projection metadata

If cache conflicts with projection visibility:

```text
Projection visibility wins
```

If projection visibility is uncertain:

```text
Fail closed unless explicit fallback confirms visibility safely
```

---

## Rule 20: Projection diagnostics are internal-only

Public responses must not expose:

* `SourceVersion`
* `LastEventMessageId`
* `LastSourceOccurredAtUtc`
* `LastSyncedAtUtc`
* internal visibility reason
* internal projection status
* outbox message identifiers
* event metadata
* audit fields
* admin-only fields

These are for diagnostics, observability, and internal operations only.

---

## Rule 21: Reading must not block public requests waiting for async catch-up

Public requests must use bounded waits.

Reading must not block a request waiting for:

* outbox publishing
* RabbitMQ delivery
* Reading consumer catch-up
* Interaction aggregation
* counter update
* cache refresh
* rebuild/reconciliation
* search reindex
* related article regeneration

If data is not available safely, Reading should degrade or fail closed according to policy.

---

## Rule 22: Rebuild and reconciliation are recovery tools

Reading projections are derived state and must be recoverable.

Approved recovery strategies:

* rebuild from Content truth
* resolve/reconcile optional projected slug and SEO metadata from SEO truth
* rebuild from Media truth
* rebuild/reconcile counters from Interaction
* bounded recomputation
* replay from retained operational history where policy allows

RabbitMQ is not the permanent replay source.

---

## Rule 23: Rebuild must be rerun-safe

Reading rebuild and reconciliation workflows must be safe to rerun on the same bounded input.

Rerun must not:

* duplicate articles
* double-count counters
* expose non-public content
* overwrite newer projection state with older data
* publish partial candidate output as complete

---

## Rule 24: Partial rebuild output must not be exposed as complete

Production rebuild should avoid exposing partially built output as active complete data.

Preferred shape:

```text
Build candidate
    ↓
Validate candidate
    ↓
Publish / cut over
```

If candidate validation fails, previous safe active output should remain active where possible.

---

## Rule 25: Reading must not mutate source truth during repair

Reading repair/reconciliation may correct Reading-owned projection state.

It must not mutate Content, SEO, Media, or Interaction truth.

Repair is not an excuse to bypass module ownership.

---

## Rule 26: Safe non-progress beats unsafe stale apply

When Reading cannot establish safe forward progress, it must prefer:

* no-op
* reject
* retry
* defer
* resync
* rebuild
* operator-controlled remediation

over applying a possibly wrong effect.

This applies especially to:

* public visibility
* slug routing
* stale projection writes
* rebuild/cutover
* counter exactness
* cache refresh

---

## Rule 27: Timeout outcomes are ambiguous

A timeout does not prove:

* source event was not published
* projection update did not apply
* article does not exist
* SEO route resolution did not complete or did not map the slug
* enrichment is absent
* rebuild did not partially run

Reading must handle timeout ambiguity through:

* bounded retry where safe
* idempotency
* version-aware apply
* safe degradation
* fail-closed visibility
* rebuild/reconciliation

---

## Rule 28: Public API must not leak hidden resource existence

Reading public error responses must not reveal whether hidden content exists.

Public clients should not receive different responses for:

* missing article
* draft article
* unpublished article
* archived article
* soft-deleted article
* slug mapped to hidden content
* projection visibility uncertain

Use safe not found behavior.

---

## Rule 29: Static routes must avoid dynamic route conflicts

Static routes such as:

```text
/articles/search
/articles/slug/{slug}
```

must be registered before:

```text
/articles/{articlePublicId}
```

or route constraints must be used.

This prevents accidental route matching errors.

---

## Rule 30: Reading must remain anonymous-safe in V1

Public Reading endpoints generally do not require authentication.

Reading V1 must not mix user-specific data into anonymous public responses.

Deferred features requiring separate policy:

* draft preview
* admin preview
* personalized recommendation
* user-specific reading history
* personalized ranking

---

## Rule 31: External search or recommendation output remains derived

If an external search or recommendation service is introduced later:

* output remains derived
* it must not override Reading visibility
* stale index must not expose hidden content
* rebuild/reindex path must be documented
* fallback behavior must be explicit

---

## Rule 32: Public read performance matters, but correctness comes first

Reading exists to make the public website read path fast.

However:

```text
Correctness first.
Completeness second.
Freshness third.
```

Performance must not justify:

* leaking drafts
* serving stale hidden content
* bypassing projection visibility
* trusting stale cache
* exposing partial rebuild output
