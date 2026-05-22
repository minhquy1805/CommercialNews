# Reading Module — API Architecture (V1)

## Purpose

Reading provides the public article reading experience for CommercialNews.

Reading is a **derived public read projection module**.

It owns the public read model and public response behavior for:

* article listing with paging/filter/sort
* article detail by public id
* article detail by slug
* related articles
* basic keyword search
* safe response composition from Reading-owned projections
* graceful degradation of optional enrichments

Reading does not own article truth.

Source truth remains owned by:

* Content for publication lifecycle and editorial fields
* SEO for slug and canonical routing rules
* Media for media assets and primary media
* Interaction for views, likes, comments, and counters

Normal public path:

```text
Public API
    ↓
Reading projection
    ↓
Response
```

Policy-controlled fallback to source truth may exist, but it must be explicit, bounded, observable, and must not become hidden cross-module ownership.

---

## Why this module is critical

Reading is critical because:

* it is the public website read path
* it must remain fast and available under burst traffic
* it must never expose draft, unpublished, archived, soft-deleted, or visibility-uncertain content
* it serves from derived projections that may lag behind source truth
* it must degrade safely when optional enrichments are missing or stale
* it must support async projection updates from upstream source events
* it must support rebuild/reconciliation for Reading-owned derived outputs

The most important correctness rule is:

```text
Safe public visibility first.
Performance second.
Freshness third.
```

---

## Primary consumers

Reading serves:

* public website clients
* mobile clients if introduced
* anonymous users
* authenticated public users
* edge/CDN layers where configured
* public search and crawler traffic
* monitoring and synthetic checks

Reading may also support internal operational workflows such as:

* projection rebuild
* projection reconciliation
* derived output validation
* search/materialized query rebuild
* related/trending recomputation

---

## Non-goals in V1

Reading V1 does not own:

* article creation or editing
* publish/unpublish/archive/restore commands
* slug generation rules
* media upload/storage lifecycle
* interaction write endpoints
* raw counter/event truth
* audit truth
* notification delivery
* admin preview
* draft preview
* personalized recommendations
* permanent event history

Reading V1 also does not guarantee:

* immediate article visibility after publish
* immediate projection catch-up after source changes
* strongly consistent counters
* exactly-once event delivery
* global ordering across all source modules

---

## Hard constraints

Reading must follow these constraints:

* only publicly visible content may be returned
* unknown visibility means not public
* safe `404` is preferred over incorrect public exposure
* normal public path reads from Reading-owned projection data
* source fallback is explicit and exceptional
* interaction tracking must not block responses
* optional enrichments may lag or be omitted safely
* cache must not become hidden truth
* projection data must not expose internal diagnostics
* projection updates must be idempotent
* stale source versions must not overwrite newer projection state
* timestamps must not be used as freshness authority
* rebuild/reconciliation workflows must be bounded, observable, and rerun-safe
* partial rebuild output must not be exposed as complete active output

---

## Truth vs derived posture

### Source truth

| Concern | Owner |
|---|---|
| Article lifecycle | Content |
| Article editorial fields | Content |
| Category/tag truth | Content |
| Slug/canonical routing | SEO |
| Media lifecycle and primary media | Media |
| Interaction events and counters | Interaction |

### Reading-owned derived data

Reading may own:

* `ArticleReadModel`
* public search projection
* related article projection
* trending/summary inputs
* public response cache
* rebuild candidate artifacts
* reconciliation reports

These outputs are derived.

They may lag, replay, rebuild, and be replaced.

They must remain subordinate to source ownership.

---

## Stream / async posture

Reading follows source truth asynchronously.

Standard flow:

```text
Source module commits truth + outbox
    ↓
Outbox worker publishes event
    ↓
RabbitMQ delivers event
    ↓
Reading consumer receives event
    ↓
Reading applies idempotent projection update
```

Reading may consume events such as:

```text
content.article_published
content.article_updated
content.article_unpublished
content.article_archived
content.article_soft_deleted
seo.slug_updated
seo.metadata_updated
media.article_primary_media_changed
interaction.article_counters_updated
```

Reading consumer rules:

* delivery is at-least-once
* duplicate delivery is expected
* replay is expected
* events may arrive out of order
* `MessageId` supports duplicate detection
* `SourceVersion` protects against stale overwrite
* timestamp order is not freshness authority
* idempotent upsert is preferred
* stale events must be ignored or rejected

---

## Batch / rebuild / reconciliation posture

Reading projections are derived and must be recoverable.

Reading may participate in bounded workflows for:

* rebuilding `ArticleReadModel`
* reconciling source truth with Reading projection
* regenerating related article outputs
* rebuilding search/materialized query outputs
* regenerating trending or summary inputs
* repairing missing projected enrichments

These workflows must:

* define bounded input
* remain rerun-safe
* avoid mutating source truth
* validate important candidate outputs
* avoid exposing partial candidate output as complete
* prefer candidate-before-cutover where correctness matters

RabbitMQ is delivery infrastructure, not permanent replay history.

Recovery should rely on:

* source truth
* retained operational history where policy allows
* outbox retention where policy allows
* bounded rebuild/reconciliation
* deterministic regeneration from authoritative inputs

---

## Cache posture

Cache is acceleration only.

Reading cache may improve latency, but must not become hidden truth.

Rules:

* cache hit must not bypass Reading projection visibility
* stale cache must not expose non-public content
* cache write must be skipped when visibility is uncertain
* cache refresh failure must not break safe projection-backed response
* cache keys and values must not include internal diagnostics or sensitive metadata

---

## Primary correctness posture

Reading correctness is based on source-derived projection visibility.

Rules:

* slug match is not enough
* route fallback is not enough
* cache hit is not enough
* search hit is not enough
* related candidate is not enough
* enrichment presence is not enough

Public response requires Reading visibility to pass.

If visibility is missing or uncertain:

```text
Fail closed
```

unless explicit fallback confirms visibility safely.

---

## Consistency and ordering posture

Reading uses eventual consistency for projections and enrichments.

Accepted eventual consistency:

* counters
* related signals
* search/materialized outputs
* cached fragments
* SEO metadata projections
* media enrichments
* summaries/trending inputs

Required correctness behavior:

* public visibility fails closed
* stale events do not overwrite newer projection state
* duplicate messages are harmless
* timestamps are not freshness authority
* source versions are used for ordering-sensitive projection apply
* rebuild/reconciliation exists for important derived outputs

---

## Failure and recovery posture

Reading must handle:

* projection lag
* broker redelivery
* consumer retry
* stale event arrival
* cache miss/stale cache
* optional enrichment failure
* rebuild failure
* timeout ambiguity

Failure rules:

* timeout does not prove absence
* cache miss does not prove absence
* route success does not prove public visibility
* public read success does not require interaction tracking
* public read success does not require rebuild completion
* optional enrichment failure should degrade safely
* required projection failure may return safe `404` or `503` according to policy
* visibility uncertainty must fail closed

---

## Documentation map

| Document | Purpose |
|---|---|
| [01-api-surface.md](01-api-surface.md) | Public endpoint contract |
| [02-domain-contracts.md](02-domain-contracts.md) | Read model, projection, and ownership contracts |
| [03-runtime-flows.md](03-runtime-flows.md) | Read path, async projection, rebuild/reconciliation flows |
| [04-errors-status-codes.md](04-errors-status-codes.md) | Public-safe error behavior |
| [05-security-abuse-controls.md](05-security-abuse-controls.md) | Scraping, exposure, cache, and logging controls |
| [06-idempotency-consistency.md](06-idempotency-consistency.md) | Delivery, replay, versioning, and consistency posture |
| [07-observability-slos.md](07-observability-slos.md) | Metrics, logs, SLOs, and rollout gates |
| [08-dependencies-and-ownership.md](08-dependencies-and-ownership.md) | Module boundaries and dependency ownership |
| [09-open-questions.md](09-open-questions.md) | ADR hooks and deferred decisions |
| [10-business-rules.md](10-business-rules.md) | Reading business rules |

---

## Key links

System-wide:

* `../../01-api-architecture-charter-v1.md`
* `../../02-contracts-and-standards.md`
* `../../09-observability-and-slos.md`

Arc42:

* `../../../architecture/arc42/03-building-blocks-modularity.md`
* `../../../architecture/arc42/04-runtime-view-v1.md`
* `../../../architecture/arc42/05-quality-requirements.md`
* `../../../architecture/arc42/13-transactions-and-consistency-v1.md`
* `../../../architecture/arc42/16-batch-processing-and-derived-data-v1.md`
* `../../../architecture/arc42/17-dataflow-and-batch-workflows-v1.md`
* `../../../architecture/arc42/18-stream-processing-and-derived-state-v1.md`
* `../../../architecture/arc42/19-stream-processing-runtime-v1.md`

Relevant ADRs:

* ADR-0013 Outbox & Delivery Semantics
* ADR-0015 Cache Policy & Invalidation
* ADR-0018 Transaction Boundaries & Consistency
* ADR-0020 Timeout, Retry, and Failure Detection
* ADR-0021 Clock, Time, and Ordering
* ADR-0022 Versioning and Fencing
* ADR-0025 Batch Processing and Derived State
* ADR-0026 Batch Job Orchestration and Materialization
* ADR-0027 Stream Processing and Derived State
* ADR-0028 Consumer Idempotency, Replay, and Rebuild

Upstream modules:

* Content
* SEO
* Media
* Interaction

---

## ADR hooks

Important open decisions are tracked in:

* [09-open-questions.md](09-open-questions.md)

Key ADR hooks include:

* popularity definition
* slug canonical redirect behavior
* search backend and indexing strategy
* read caching policy
* counter response/freshness policy
* source event payload shape
* processed-message tracking strategy
* version gap and resync behavior
* rebuild/cutover/fencing strategy
* related articles strategy
* source fallback policy
* draft/admin preview boundary
* personalization boundary
* external search/recommendation integration
* projection freshness SLO
