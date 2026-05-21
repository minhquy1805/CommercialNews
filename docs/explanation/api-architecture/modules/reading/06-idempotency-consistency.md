# Reading — Idempotency & Consistency (V1)

This document defines Reading-specific consistency guarantees for public read APIs, async projection updates, replay behavior, stale event handling, cache safety, and rebuild/reconciliation workflows.

System-wide rules live in:

* `../../../../architecture/arc42/11-replication-v1.md`
* `../../../../architecture/arc42/13-transactions-and-consistency-v1.md`
* `../../../../architecture/arc42/14-distributed-systems-assumptions-v1.md`
* `../../../../architecture/arc42/15-consistency-ordering-and-consensus-v1.md`
* `../../../../architecture/arc42/16-batch-processing-and-derived-data-v1.md`
* `../../../../architecture/arc42/17-dataflow-and-batch-workflows-v1.md`
* `../../../../architecture/arc42/18-stream-processing-and-derived-state-v1.md`
* `../../../../architecture/arc42/19-stream-processing-runtime-v1.md`
* ADR-0013 (Outbox & delivery semantics)
* ADR-0015 (Redis cache policy)
* ADR-0018 (Transaction boundaries & consistency model)
* ADR-0020 (Timeout, retry, and failure detection policy)
* ADR-0021 (Clock, time, and ordering policy)
* ADR-0022 (Versioning and fencing strategy)
* ADR-0025 (Batch processing and derived state policy)
* ADR-0026 (Batch job orchestration and materialization policy)
* ADR-0027 (Stream processing and derived state policy)
* ADR-0028 (Consumer idempotency, replay, and rebuild policy)

---

## 1) Consistency model

Reading is a derived public read projection module.

Reading owns:

* public read model
* public query semantics
* response composition
* projection freshness metadata
* safe degradation behavior
* rebuild/reconciliation posture for Reading-owned derived outputs

Reading does not own:

* article lifecycle truth
* slug generation truth
* media lifecycle truth
* interaction counter truth
* audit truth
* notification truth

Source ownership remains:

| Concern | Source owner |
|---|---|
| Article lifecycle and editorial fields | Content |
| Slug and SEO metadata | SEO |
| Media assets and primary media | Media |
| Views, likes, comments, counters | Interaction |
| Public serving projection | Reading |

Reading follows source truth asynchronously.

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

Policy-controlled fallback may read source truth when correctness requires it, but fallback must be explicit and must not become hidden cross-module ownership.

---

## 2) Truth vs derived state

### 2.1 Source truth

Source truth is committed synchronously in the owning module.

Examples:

* Content publishes/unpublishes an article.
* SEO changes canonical slug data.
* Media changes primary media.
* Interaction updates counter truth or aggregate state.

### 2.2 Reading derived state

Reading projection is derived state.

It may be:

* stale
* missing
* delayed
* replayed
* repaired
* rebuilt
* replaced

Reading projection must not silently become source truth.

Reading projection must be rebuildable from authoritative source modules.

---

## 3) Delivery assumption

CommercialNews V1 uses Outbox + RabbitMQ + consumers with at-least-once delivery.

Reading must assume:

* events may be delivered more than once
* broker may redeliver
* outbox publisher may retry
* consumers may crash and restart
* older events may arrive after newer events
* replay/rebuild may intentionally reprocess input

Reading targets effectively-once projection outcomes through:

* message-level identity
* version-aware projection apply
* idempotent upsert
* stale event rejection
* rebuild/reconciliation posture

Exactly-once delivery is not assumed.

---

## 4) Event identity

Baseline V1 Reading consumes Content events for core article projection. Media and Interaction events are optional/adopted flows. SEO-emitted events are future optimization only; baseline slug correctness comes from explicit SEO route resolution.

Important events consumed by Reading should carry:

| Field | Purpose |
|---|---|
| `MessageId` | Message-level identity and dedupe key |
| `EventType` | Handler routing |
| `AggregateId` | Source aggregate identity |
| `Version` | Per-aggregate freshness marker |
| `OccurredAtUtc` | Event timestamp for investigation and lag measurement |
| `CorrelationId` | End-to-end tracing |

Reading should log these fields for every important apply decision.

---

## 5) Message-level idempotency

Message-level idempotency protects against the same delivered message being processed more than once.

Primary key:

```text
MessageId
```

Reading may implement this through:

* `LastEventMessageId`
* durable processed-message table
* unique apply log
* idempotent repository/stored procedure behavior

Duplicate processing of the same message must be harmless.

---

## 6) Projection-level idempotency

Message-level dedupe alone is not sufficient.

A different older message may arrive after a newer message.

Therefore Reading must also protect projection state with source versioning.

Primary freshness marker:

```text
SourceVersion
```

Approved apply rule:

```text
If IncomingVersion > CurrentSourceVersion:
    apply event
Else:
    ignore or reject as duplicate/stale
```

The version guard should be enforced at the repository/stored procedure boundary, not only in application memory.

---

## 7) Duplicate vs stale delivery

### 7.1 Duplicate delivery

The same message arrives again.

Example:

```text
MessageId = 01ABC
Version = 7
```

Expected behavior:

* no duplicate row
* no harmful side effect
* no projection corruption
* duplicate metric/log may be emitted

### 7.2 Stale delivery

An older but different message arrives after newer state is already applied.

Example:

```text
CurrentSourceVersion = 7
IncomingVersion = 6
```

Expected behavior:

* ignore or reject stale event
* do not overwrite projection
* emit stale-event metric
* retain enough logs for investigation

---

## 8) Timestamp policy

Reading must not use wall-clock timestamps as freshness authority.

Do not use:

* `largest UpdatedAtUtc wins`
* `largest OccurredAtUtc wins`
* `latest ProcessedAtUtc wins`

Use:

* `AggregateId + Version`
* `SourceVersion / LastAppliedVersion`

Timestamps are allowed for:

* public display
* audit/investigation
* projection lag measurement
* reporting
* scheduling

They are not ordering authority.

---

## 9) Version gap policy

V1 default:

* snapshot-like events may be applied when `IncomingVersion > CurrentSourceVersion`
* delta-like events require strict ordering or resync
* if exact prior state matters and a gap is detected, Reading must defer, resync, or rebuild according to policy

Event shape must be explicit:

```text
Snapshot event => newer version can replace projection.
Delta event => strict order or resync may be required.
```

---

## 10) Public read idempotency

Public read endpoints are naturally idempotent.

Repeated reads must not mutate source truth.

Affected endpoints:

```text
GET /api/v1/articles
GET /api/v1/articles/{articlePublicId}
GET /api/v1/articles/slug/{slug}
GET /api/v1/articles/search
GET /api/v1/articles/{articlePublicId}/related
```

A repeated read may return a newer projection if Reading has caught up between requests.

This is acceptable.

---

## 11) Public visibility consistency

Reading must fail closed for public visibility.

Public APIs must not expose:

* draft articles
* unpublished articles
* archived articles
* soft-deleted articles
* visibility-uncertain articles

Public visibility requires:

* source-derived status is `Published`
* projection `IsPublic = true`
* article is not archived
* article is not soft-deleted
* SEO route resolution returns an active public route if accessed by slug
* visibility is not uncertain

If visibility is uncertain:

```text
Unknown visibility => not public.
Safe 404 is preferred over incorrect public exposure.
```

---

## 12) Projection lag behavior

Projection lag is expected.

### After publish

If Content has published an article but Reading projection has not caught up yet:

* article may be missing from list
* detail by slug/public id may return safe `404`
* search may not include the article yet

This lag is acceptable within SLO and must be observable.

### After unpublish/archive/soft-delete

If source truth has hidden an article but Reading projection has not caught up yet:

* visibility-sensitive stale exposure is not acceptable where detected
* if projection freshness policy marks visibility as uncertain, Reading must fail closed
* repair/reconciliation must correct projection drift

---

## 13) Optional enrichment consistency

The following are allowed to be eventually consistent:

* counters
* cover media
* media gallery
* optional projected SEO metadata
* related article signals
* popularity/trending signals
* summaries

Slug routing for slug-based reads is handled by explicit SEO route resolution in baseline V1; projected slug fields in Reading are optional optimization data.

If optional enrichments are missing, stale, or unavailable, Reading may:

* omit them
* return null
* return empty arrays
* return safe defaults
* return stale non-sensitive values where policy allows

Optional enrichment failure must not make visibility permissive.

---

## 14) Counter consistency

Counters are derived and may lag.

Examples:

* views
* likes
* comments

Reading must not blindly increment counters under at-least-once delivery.

Disallowed pattern:

```text
On every delivered view event:
    ViewCount = ViewCount + 1
```

Preferred patterns:

```text
Set ViewCount to known aggregate value.
```

or:

```text
Deduplicate raw interaction event before incrementing.
```

For V1, Reading should prefer absolute counter updates or default counters until Interaction counter projection is designed.

Counter truth belongs to Interaction.

---

## 15) View tracking consistency

Reading does not expose interaction write endpoints in V1.

Recommended flow:

```text
Reading returns article detail
    ↓
Client sends view signal to Interaction
    ↓
Interaction handles dedupe, counting, abuse controls, and aggregation
```

Rules:

* Reading success does not depend on view tracking success
* view signal failure does not change article visibility
* duplicate view signals must be tolerated by Interaction
* Reading counters may lag behind Interaction

---

## 16) Cache consistency

Cache is acceleration only.

Cache must not become hidden truth.

Rules:

* cache hit must not bypass source-derived visibility
* stale cache must not expose unpublished, archived, or soft-deleted content
* cache refresh must be harmless under duplicate requests/signals
* cache refresh failure must not break a safe projection-backed response
* cache data must not override Reading projection visibility

If cache conflicts with projection visibility:

```text
Projection visibility wins.
```

If projection visibility is uncertain:

```text
Fail closed unless explicit fallback confirms visibility safely.
```

---

## 17) Transaction boundary

Reading projection updates use bounded local transactions.

A Reading projection transaction may update:

* projection fields
* `SourceVersion`
* `LastEventMessageId`
* `LastSourceOccurredAtUtc`
* `LastSyncedAtUtc`

It must not update source module truth.

It must not open long-running cross-module transactions.

Source modules must not wait for Reading projection completion as part of their truth transactions.

---

## 18) Producer-side vs consumer-side failure

Reading distinguishes producer-side and consumer-side failures.

### Producer-side failure

Examples:

* outbox publication is delayed
* outbox publisher cannot publish to RabbitMQ
* message is not yet handed off to the broker

This is not a Reading consumer failure.

### Consumer-side failure

Examples:

* Reading receives event but cannot apply projection
* Reading repository is unavailable
* version conflict or stale event is detected
* projection update fails transiently

This belongs to Reading consumer/retry/recovery behavior.

These must be logged and measured separately.

---

## 19) Rebuild and reconciliation posture

`ArticleReadModel` is derived state.

It must have a documented recovery path.

Approved recovery strategies:

* rebuild from Content truth
* resolve/reconcile optional projected slug and metadata from SEO truth
* rebuild from Media truth
* rebuild/reconcile counters from Interaction
* bounded recomputation
* replay from retained operational history where policy allows

RabbitMQ is not the permanent replay source.

Production rebuild should avoid exposing partial output as complete.

Candidate-before-cutover or equivalent safe publication is preferred for full rebuilds.

---

## 20) Rerun safety

Important rebuild/reconciliation workflows must be safe to rerun on the same bounded input.

Rerun must not:

* duplicate articles
* double-count counters
* expose non-public content
* overwrite newer projection state with older data
* publish partial candidate output as complete

---

## 21) Coordination and ownership posture

Reading should not depend on global singleton assumptions by default.

Correctness should come from:

* source-derived projection state
* version-aware apply
* idempotent upsert
* safe cache behavior
* rerun-safe rebuild/reconciliation

If a future rebuild or repair workflow requires exclusive ownership, it must define:

* ownership source of truth
* monotonic generation/fencing token
* resource-side rejection of stale owner actions

Safe non-progress is preferred over unsafe dual publication or unsafe repair.

---

## 22) Timeout and ambiguity

Timeouts are ambiguous.

A timeout does not prove:

* a source event was not published
* a projection update did not apply
* an article does not exist
* SEO route resolution did not complete or did not map the slug
* a media/counter enrichment is absent

Reading must handle ambiguity through:

* bounded retry where safe
* safe degradation
* fail-closed visibility
* explicit fallback policy
* rebuild/reconciliation

---

## 23) Safe non-progress rule

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

---

## 24) Observability signals

Reading should expose or log:

* projection apply count
* projection apply failure count
* duplicate message count
* stale version reject count
* version gap detected count
* projection lag / freshness age
* rebuild/reconciliation count
* rebuild/reconciliation failure count
* fallback count
* degraded response count
* omitted enrichment count
* cache hit/miss count
* visibility uncertain count

Logs should include:

* `MessageId`
* `EventType`
* `AggregateId`
* `IncomingVersion`
* `CurrentSourceVersion`
* `CorrelationId`
* apply decision
* failure/reject reason

---

## 25) Summary

Reading consistency in V1 rests on these rules:

* Reading is a derived public read projection module.
* Source modules own truth; Reading owns projection.
* Normal list/detail/search/related paths read from Reading projection; slug-based reads resolve route through SEO before loading Reading projection.
* Delivery is at-least-once; handlers must be idempotent.
* `MessageId` protects duplicate delivery.
* `SourceVersion` protects stale overwrite.
* Timestamps are not freshness authority.
* Public visibility fails closed.
* Optional enrichments may lag or degrade.
* Counters must not increment blindly under replay.
* Cache is acceleration only, not hidden truth.
* Source modules do not wait for Reading projection completion.
* RabbitMQ is delivery infrastructure, not permanent replay history.
* Rebuild/reconciliation is required for important derived outputs.
* Safe non-progress beats unsafe stale apply.
