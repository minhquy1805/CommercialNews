# Reading — Runtime Flows (V1)

Primary runtime scenario:

* arc42 Scenario 3: public list + open by slug

Related:

* `../../../../architecture/arc42/04-runtime-view-v1.md`
* `../../../../architecture/arc42/13-transactions-and-consistency-v1.md`
* `../../../../architecture/arc42/18-stream-processing-and-derived-state-v1.md`
* `../../../../architecture/arc42/19-stream-processing-runtime-v1.md`
* `../../../../architecture/arc42/16-batch-processing-and-derived-data-v1.md`
* `../../../../architecture/arc42/17-dataflow-and-batch-workflows-v1.md`
* `../../../../decisions/adr-0015-cache-policy-and-invalidation-redis-v1.md`
* `../../../../decisions/adr-0027-stream-processing-and-derived-state-policy-v1.md`
* `../../../../decisions/adr-0028-consumer-idempotency-replay-and-rebuild-policy-v1.md`

---

## Runtime posture in V1

Reading participates in three runtime lanes:

### A) Synchronous public read lane

Used for:

* public article listing
* public article detail by public id or slug
* public article search
* related article composition
* safe response composition from Reading-owned projections
* policy-controlled fallback when projection state is missing or uncertain

### B) Async projection update lane

Used for:

* consuming source events from Content, SEO, Media, and Interaction
* applying idempotent updates to Reading-owned read models
* refreshing projected public visibility, slug, media, counters, and searchable text
* handling replay, duplicate delivery, and stale event arrival safely

### C) Batch / rebuild / reconciliation lane

Used for:

* rebuilding Reading-owned derived projections
* generating analytics and trending inputs
* reconciling source truth with Reading projections
* replaying or repairing missing derived outputs when async processing lagged or failed

**Rule:** Reading correctness is defined by source-derived visibility stored in Reading projections and safe response composition.

**Rule:** Reading is a derived public read projection module, not the owner of publication truth.

**Rule:** Reading projections, counters, summaries, and enrichments may lag, replay, or be rebuilt. They improve read performance and experience, but do not redefine source truth.

**Rule:** If public visibility is missing or uncertain, Reading must fail closed unless an explicit fallback policy applies.

---

## Flow A — List articles (read path)

### Goal

Return public article lists quickly from Reading-owned projections while tolerating lag in non-critical enrichments.

### Normal path

```text
Client calls GET /api/v1/articles
    ↓
Reading queries reading.ArticleReadModel
    ↓
Reading applies public visibility filters
    ↓
Reading composes optional enrichments already projected
    ↓
Reading returns response quickly
```

### Runtime rules

* Only rows with source-derived public visibility may be returned.
* Public visibility requires source status `Published` and projection `IsPublic = true`.
* Archived, soft-deleted, unpublished, or visibility-uncertain content must not be returned.
* Missing or stale optional enrichments must not fail the entire list response if the projection contains safe public content.
* Reading may omit or degrade projected enrichments, but must not invent visibility or serve non-public content.
* Source-truth fallback is policy-controlled and must not become the normal public hot path.

### Failure modes

* Interaction projection lag: return content without counters, stale counters, or safe counter defaults.
* Media projection partial: return null, omitted, or placeholder cover according to API policy.
* SEO projection partial: omit or default route metadata safely; do not expose non-public content.
* Cache/projection stale: fail closed or use explicit fallback policy; do not treat stale cache as authority.
* Reading projection indicates uncertainty: safe omission or safe 404/empty result wins.

### Observability notes

* Track list latency and error rate separately from detail.
* Track omitted-enrichment rate for counters, media, and SEO fields.
* Track projection-lag indicators such as `LastSyncedAtUtc` age.
* Track policy-controlled fallback rate when Reading projection state is stale or unavailable.

---

## Flow B — Open article by slug (hot path)

### Goal

Resolve slug from the Reading projection and return article detail without leaking non-public content.

### Normal path

```text
Client opens /articles/slug/{slug}
    ↓
Reading looks up slug in reading.ArticleReadModel
    ↓
Reading verifies source-derived public visibility from projection
    ↓
Reading composes article detail from projected fields
    ↓
Reading returns response
```

### Runtime rules

* Reading may resolve slug from its projected read model in the normal public path.
* SEO remains the source of truth for slug generation and canonical routing rules.
* If canonical redirect behavior or projection freshness is uncertain, Reading may use SEO `/resolve` according to explicit policy.
* Projected slug match alone is not sufficient; projected public visibility must also pass.
* If the slug is unknown, inactive, non-public, or visibility-uncertain, Reading must return safe 404 / safe deny.
* Detail response success is defined by source-derived public visibility and safe response composition, not by completion of counters, telemetry, or cache refresh.

### Failure modes

* Slug missing from Reading projection: return safe 404 or use explicit fallback policy.
* Projection marks article non-public or uncertain: return safe 404.
* SEO/canonical fallback fails when invoked: return safe 404 unless policy defines another safe response.
* Interaction projection lag: detail still succeeds; counters may be stale, defaulted, or omitted.
* Media projection lag: return fallback media or omit media fields safely.
* Stale projected detail still contains now-non-public content: it must fail closed once visibility is known or uncertain.

### Observability notes

* Track detail latency/error.
* Track slug lookup miss rate.
* Track visibility-denied-after-slug-match rate (internal only).
* Track SEO resolve fallback rate where policy invokes it.
* Track derived-detail omitted-enrichment rate.

---

## Flow C — View tracking signal owned by Interaction

### Goal

Record view intent without coupling Reading success or latency to Interaction processing.

### Flow

1. Reading returns article detail successfully.
2. Client sends view signal to Interaction.
3. Interaction owns dedupe, counting, aggregation, and abuse policy.
4. Reading counters catch up later through projection or query policy.

### Rules

* Reading success must not depend on Interaction view tracking success.
* Reading does not expose interaction write endpoints in V1.
* Duplicate view signals are tolerated by Interaction policy.
* View-signal timing may lag behind response timing; analytics correctness belongs to downstream event-time policy, not to synchronous read completion.
* Counter lag must not affect public visibility or article detail correctness.

### Failure modes

* Client does not send view signal: article response remains correct.
* Interaction endpoint fails: Reading response is already complete.
* Broker or consumer lag: counters/trending lag, but read response remains valid.
* Duplicate signal delivery: counters/aggregates must remain safe under downstream dedupe or recompute policy.

---

## Flow D — Reading projection apply from source event

### Goal

Keep `reading.ArticleReadModel` synchronized with source modules through idempotent async projection updates.

### Runtime shape

```text
Source module commits truth + outbox
    ↓
Outbox worker publishes event
    ↓
RabbitMQ delivers event
    ↓
Reading consumer receives event
    ↓
Reading checks MessageId and SourceVersion
    ↓
Reading applies idempotent upsert/update
    ↓
reading.ArticleReadModel is updated
```

### Typical source events

* Content article published/updated/unpublished/archived/soft-deleted
* SEO slug or metadata changed
* Media primary media changed
* Interaction counters updated

### Rules

* Delivery is at-least-once.
* Reading projection handlers must be idempotent.
* `IncomingVersion <= SourceVersion` must be ignored or rejected as duplicate/stale.
* Timestamp order must not be used as freshness authority.
* `MessageId`, source aggregate id, source version, event type, and apply decision must be logged and measured.
* Projection updates must use bounded local transactions.
* Source modules must not wait for Reading projection completion as part of their source transactions.

### Failure modes

* Duplicate delivery: ignored through message or projection-level idempotency.
* Older event arrives after a newer event: ignored or rejected by source version.
* Consumer failure after delivery: message is retried; apply remains safe.
* Projection update fails: message retry or repair workflow handles recovery according to policy.
* Event gap or prolonged lag: public reads use current projection state and fail closed when visibility is uncertain.

### Observability notes

* Track consumer lag and queue depth.
* Track apply, duplicate, stale, reject, and failure counts by event type.
* Track projection freshness by source version and `LastSyncedAtUtc`.
* Track visibility state transitions such as public -> non-public and uncertain -> public.

---

## Flow E — Reading analytics / aggregation workflow

### Goal

Generate reading-side derived summaries from bounded interaction or read-model input.

### Typical workflow shape

1. Select bounded input:
   * e.g. all view events in window W
2. Normalize / partition by key:
   * `ArticlePublicId`
   * `(ArticlePublicId, Date)`
3. Aggregate into candidate summaries.
4. Validate candidate output.
5. Publish / replace active derived summary if policy requires.
6. Cleanup temporary workflow state.

### Typical outputs

* daily article view summaries
* trending inputs
* bounded summary tables
* repairable derived reading enrichments

### Rules

* Output is derived state, not publication truth.
* Workflow rerun on the same bounded input must be harmless or explicitly controlled.
* Partial candidate output must not be exposed as completed active state.
* If event-time semantics matter, lateness policy must be explicit at the workflow/module level.
* If a full rebuild is simpler than fragile partial repair, rebuild is preferred.

### Failure modes

* Aggregation failure: reading still works; summaries lag.
* Candidate publication failure: previous active derived summary remains valid if one exists.
* Replay/rebuild overlap: must be safe under rerun/duplicate execution policy.
* Late or replayed interaction input must not silently corrupt active derived summaries.

---

## Flow F — Reading reconciliation / repair workflow

### Goal

Detect and repair divergence between source truth and Reading-owned projections.

### Typical cases

* source content exists but Reading projection is missing
* source content is no longer public but Reading projection still marks it public
* counters/trending input missing after consumer lag
* stale derived summary beyond freshness policy
* cache/projection mismatch detected by reconciliation policy
* projected slug, media, or SEO metadata differs from source-owned state

### Typical workflow shape

1. Select bounded source and/or derived comparison scope.
2. Detect mismatches.
3. Generate bounded repair candidate set.
4. Validate repair candidate.
5. Apply repair safely or publish repaired derived output.
6. Record completion / cleanup.

### Rules

* Repair workflow must not redefine publication truth.
* Public APIs must fail closed for missing or uncertain visibility while repair is pending.
* Repair output must follow candidate-before-apply discipline where correctness matters.
* If derived state is too uncertain, bounded rebuild from source inputs is preferred over fragile patch mutation.
* Reconciliation may correct Reading projection state, but it must not create hidden cross-module ownership.

### Recovery posture

Reading repair workflows are allowed to:

* replay missing enrichments
* rebuild projections and summaries
* regenerate read-side candidates
* correct drift against source-owned state

They are not allowed to:

* declare content readable when source-derived visibility would deny it
* bypass visibility checks because a derived candidate "looks complete"

---

## Flow G — Reading-side cache refresh / fallback behavior

### Goal

Use cache as acceleration only, never as hidden truth.

### Flow

1. Attempt cache-backed read path where configured.
2. On miss or suspected stale data, query Reading projection or apply explicit fallback policy.
3. Optionally refresh cache after safe projection-backed or policy-approved read.
4. Return response based on source-derived visibility and safe composition.

### Rules

* Cache hit must not bypass source-derived visibility enforcement.
* On miss or suspected stale data, Reading may fall back according to explicit policy.
* The normal path should remain Reading projection.
* Fallback must not create hidden cross-module ownership.
* Cache refresh is optional and must not stretch the read path unboundedly.
* Reading must prefer safe omission over stale invented enrichment.
* Cache refresh/update must be harmless under duplicate signals or repeated requests.
* Stale cache must lose to source-derived projection visibility every time.

### Failure modes

* Cache miss: latency may increase, correctness must hold.
* Cache stale after unpublish/archive: projection denial or uncertainty must win.
* Cache refresh fails: response may still succeed from Reading projection.
* Replayed cache update after fresher projection exists: stale cache content must not regain authority.

---

## Flow H — Serving under projection or enrichment lag

### Goal

Preserve correct public behavior when read-side projections, counters, summaries, or enrichments are behind.

### Typical runtime shape

1. Reading receives a public request.
2. A derived projection, cache, or enrichment source may provide partial fast-path data.
3. Reading validates visibility using source-derived projection state.
4. If derived enrichment data is stale, missing, or inconsistent:
   * core readable content may still serve if projection visibility is public
   * stale enrichments are omitted, degraded, or repaired later
5. If the projection is missing or visibility is uncertain:
   * Reading must fail closed
   * explicit truth fallback may be used only according to documented policy
6. Recovery happens through replay/rebuild/reconciliation workflows as needed.

### Examples

* article is public in projection, but counters are stale
* article is public in projection, but trending enrichment is missing
* a source visibility change is delayed and projection freshness becomes uncertain by policy
* slug exists in SEO, but Reading projection is missing or visibility-uncertain

### Rules

* Source-derived projection visibility comes before enrichment freshness.
* Core readable content may still succeed while optional enrichments lag.
* Safe degrade is preferable to blocking on derived freshness.
* Derived enrichments are never authority for public exposure.
* Missing or uncertain projection visibility must fail closed unless an explicit truth fallback policy applies.

---

## Summary

Reading runtime in V1 is governed by ten rules:

1. Source-derived projection visibility comes first.
2. Reading is a derived public read projection module, not the owner of publication truth.
3. Normal public hot paths read from Reading-owned projections.
4. Policy-controlled source-truth fallback is exceptional, explicit, and must not become hidden ownership.
5. Interaction view tracking is owned by Interaction and is optional for read success.
6. Reading projection apply is at-least-once, idempotent, and version-gated.
7. Derived enrichments may lag or be omitted safely.
8. Route resolution does not override source-derived public visibility.
9. Batch workflows support summaries, replay, rebuild, and repair, not truth ownership.
10. Replay, rerun, and lag are normal for derived Reading outputs and must remain safe.
