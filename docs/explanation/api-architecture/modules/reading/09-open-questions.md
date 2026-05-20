# Reading — Open Questions & ADR Hooks (V1)

## Purpose

This document tracks Reading-specific open questions, deferred decisions, and future ADR hooks.

Reading V1 is a derived public read projection module.

Open questions must preserve the following accepted constraints:

- Reading does not own article truth.
- Content owns publication and editorial truth.
- SEO owns slug and canonical routing truth.
- Media owns media truth.
- Interaction owns engagement truth.
- Reading owns public serving projections.
- Reading follows source truth asynchronously.
- Reading must fail closed when public visibility is uncertain.
- Reading projection must remain idempotent, version-aware, and rebuildable.

---

## ADR-01: Popularity definition

Questions:

- Should popularity be based on views, likes, comments, shares, reading time, or a blended score?
- Should popularity use lifetime counters or a time window?
- Should popularity decay over time?
- Should bot-filtered interaction data be required?
- Should popularity be computed by Interaction, Reading, or a separate analytics workflow?

Current V1 decision:

- `popularity` is allowed as a sort option, but it is derived and may lag.
- Popularity must not affect public visibility.
- If popularity data is unavailable, Reading may fall back to `publishedAt` sorting according to policy.

Future ADR should define:

- scoring formula
- source input ownership
- windowing semantics
- event-time vs processing-time behavior
- bot/abuse filtering dependency
- rebuild/reconciliation strategy

---

## ADR-02: Slug routing and canonical behavior

Questions:

- Should `GET /articles/slug/{slug}` be the preferred public website route?
- Should clients ever call SEO `/resolve` directly?
- When should Reading use projected slug lookup vs SEO fallback?
- How should canonical redirects be handled?
- Should old slugs redirect or return safe `404`?

Current V1 decision:

- Reading exposes `GET /articles/slug/{slug}` as the preferred public detail route for website clients.
- In the normal public path, Reading may resolve slug from its projected read model.
- SEO remains the source of truth for slug generation, canonical routing, and redirect rules.
- SEO `/resolve` is a policy-controlled fallback, not the mandatory normal path.
- A slug match or route resolve is not serve authority; Reading visibility must still pass.

Future ADR should define:

- canonical redirect response behavior
- old slug retention
- slug conflict handling
- redirect cache rules
- SEO fallback policy
- route-level observability

---

## ADR-03: Search capability

Questions:

- Is V1 search limited to basic keyword search against Reading projection?
- Should search be implemented with SQL `LIKE`, SQL Server full-text search, external search, or managed search later?
- Should search support tags, categories, author filters, and date filters?
- Should search support relevance ranking?
- Should search index rebuild be candidate-before-cutover?

Current V1 decision:

- V1 search may start as basic keyword search.
- Search must only return publicly visible content.
- Search/materialized query outputs are derived and may lag.
- Stale search output must not expose non-public content.

Future ADR should define:

- search backend
- indexing strategy
- relevance model
- rebuild/reindex workflow
- query abuse limits
- fallback behavior when search backend is unavailable

---

## ADR-04: Caching policy for Reading

Questions:

- Should Reading cache list responses, detail responses, slug detail, search, or related articles?
- What TTL should each route group use?
- Should cache invalidation be event-driven, TTL-based, or both?
- Should cache entries include projection version or freshness markers?
- How should cache behave during projection lag or rebuild?

Current V1 decision:

- Cache is acceleration only.
- Cache must not become hidden truth.
- Cache hit must not bypass Reading projection visibility.
- Stale cache must not expose unpublished, archived, soft-deleted, or visibility-uncertain content.
- Cache refresh failure must not break a safe projection-backed response.

Future ADR should define:

- route-level cache TTLs
- cache key structure
- invalidation triggers
- cache stampede protection
- stale-while-revalidate policy if any
- canary/cache rollout guardrails

Potential invalidation inputs:

- `content.article_published`
- `content.article_updated`
- `content.article_unpublished`
- `content.article_archived`
- `content.article_soft_deleted`
- `seo.slug_updated`
- `seo.metadata_updated`
- `media.article_primary_media_changed`
- `interaction.article_counters_updated`

---

## ADR-05: Counter inclusion policy

Questions:

- Should list/detail responses always include counters?
- Should missing counters be returned as `0`, `null`, omitted, or marked partial?
- Should Reading expose `countersPartial`?
- Should counters be absolute aggregate updates or event-based increments?
- Should counter freshness be visible to clients?

Current V1 decision:

- Counters are optional derived enrichments.
- Counters may lag.
- Counter truth belongs to Interaction.
- Reading must not blindly increment counters under replay.
- Preferred update shape is set-to-known aggregate value.

Future ADR should define:

- response shape
- freshness indicators
- counter source of truth
- counter rebuild/reconciliation strategy
- bot-filtering implications
- exactness requirements

---

## ADR-06: Reading projection source event shape

Questions:

- Should Content events be snapshot events or delta events?
- Which fields must be included in `content.article_published`?
- Which fields must be included in `content.article_updated`?
- Should SEO/Media/Interaction events carry full projection fields or only ids requiring later lookup?
- What event schema versioning strategy should be used?

Current V1 decision:

- Reading projection apply must be idempotent and version-aware.
- Important events should carry `MessageId`, `EventType`, `AggregateId`, `Version`, `OccurredAtUtc`, and `CorrelationId`.
- Snapshot-like events are easier for Reading projection apply.
- Delta-like events require stricter ordering or resync behavior.

Future ADR should define:

- exact event payload contracts
- snapshot vs delta trade-off
- schema versioning
- backward compatibility
- resync behavior for missing fields

---

## ADR-07: Processed message tracking strategy

Questions:

- Is `SourceVersion + LastEventMessageId` sufficient for Reading V1?
- Do we need a durable processed-message table?
- Do different source modules need separate freshness markers?
- Should Reading track `ContentSourceVersion`, `SeoSourceVersion`, `MediaSourceVersion`, and `InteractionSourceVersion` separately?

Current V1 decision:

- `MessageId` protects against duplicate message delivery.
- `SourceVersion` protects against stale overwrite.
- Message-level dedupe alone is not enough.
- Stale event rejection must be version-based, not timestamp-based.

Possible V1 implementation:

- start with `SourceVersion` for Content article events
- store `LastEventMessageId` for traceability
- add durable processed-message tracking if duplicate harmful effects or multi-source replay require it

Future ADR should define:

- processed-message table need
- source-specific version fields
- dedupe retention
- replay behavior
- storage/indexing strategy

---

## ADR-08: Version gap and resync behavior

Questions:

- Should Reading require strict `IncomingVersion == CurrentSourceVersion + 1`?
- Or should Reading allow `IncomingVersion > CurrentSourceVersion` for snapshot events?
- What happens when Reading detects a version gap?
- Should the consumer defer, retry, rebuild, or resync from source truth?

Current V1 decision:

- For snapshot-like events, `IncomingVersion > CurrentSourceVersion` may be acceptable.
- For delta-like events, strict sequencing or resync is required.
- Timestamp order must not be used as freshness authority.

Future ADR should define:

- strict vs loose version apply policy
- gap detection threshold
- resync workflow
- dead-letter behavior
- operational alerts

---

## ADR-09: Reading rebuild strategy

Questions:

- Should rebuild use direct idempotent upsert or candidate-before-cutover?
- Should there be a `ReadingRebuildRun` table?
- Should rebuild use generation/fencing tokens?
- Should rebuild be full, partial, or scoped by bounded input?
- What validation checks are required before cutover?

Current V1 decision:

- Reading projection is derived and rebuildable.
- RabbitMQ is not the permanent replay source.
- Rebuild input must be bounded.
- Rebuild must be rerun-safe.
- Partial candidate output must not be exposed as complete.

Future ADR should define:

- source input boundary
- direct upsert vs candidate/cutover
- rebuild ownership/fencing
- validation checks
- rollback behavior
- operator controls

---

## ADR-10: Related articles strategy

Questions:

- Should related articles be computed on request from projection fields?
- Should related articles be precomputed as a derived projection?
- Which signals should be used: category, tags, author, popularity, recency?
- Should related output be deterministic?
- How should related article projection be rebuilt?

Current V1 decision:

- Related results must include only public articles.
- Current article must be excluded.
- Deterministic fallback is required.
- Missing related signals should not block detail response.

Future ADR should define:

- on-demand vs precomputed related strategy
- scoring formula
- fallback order
- cache policy
- rebuild/reconciliation posture

---

## ADR-11: Reading projection fallback policy

Questions:

- When may Reading fall back to source truth?
- Which endpoints may use fallback?
- Should fallback be allowed for list, detail, slug, search, and related?
- What are the latency and dependency budgets?
- How do we prevent fallback from becoming hidden normal ownership?

Current V1 decision:

- Normal public path should read from Reading projection.
- Source fallback is exceptional, explicit, and observable.
- If visibility is uncertain and no safe fallback confirms it, Reading must fail closed.

Future ADR should define:

- allowed fallback cases
- fallback target modules
- timeout budgets
- fallback metrics
- disable/feature flag strategy

---

## ADR-12: Public preview and draft access

Questions:

- Should Reading ever support draft preview?
- Should admin preview use Reading projection or Content truth?
- How should preview URLs be authorized?
- Should preview bypass public visibility rules?

Current V1 decision:

- Public Reading endpoints do not serve drafts.
- Draft/admin preview is out of scope for public Reading V1.
- If preview is introduced, it must be explicitly authorized and separated from public Reading routes.

Future ADR should define:

- preview API ownership
- authorization model
- tokenized preview URL policy
- cache restrictions
- audit requirements

---

## ADR-13: Personalized Reading experience

Questions:

- Should Reading support personalized recommendations?
- Should responses vary by user identity?
- How should privacy, consent, and authorization be handled?
- How should personalized cache keys work?
- What data may be used for personalization?

Current V1 decision:

- Public Reading V1 remains anonymous-safe.
- Personalized recommendations are deferred.
- User-specific history must not be mixed into anonymous public responses without explicit policy.

Future ADR should define:

- personalization ownership
- privacy model
- consent model
- cache key strategy
- interaction with Identity/Authorization

---

## ADR-14: External search or recommendation services

Questions:

- Should Reading integrate with external search/recommendation services?
- What happens when the external provider is unavailable?
- What data can be sent externally?
- How are indexes rebuilt?
- How do we prevent external stale index from exposing hidden content?

Current V1 decision:

- No external search/recommendation dependency is required in V1.
- If introduced, external indexes remain derived.
- External index output must not override Reading visibility rules.

Future ADR should define:

- provider choice
- privacy/data sharing policy
- indexing/rebuild strategy
- fallback behavior
- stale-index safety

---

## ADR-15: Projection freshness SLO

Questions:

- What is the acceptable lag after publish?
- What is the acceptable lag after unpublish/archive/soft-delete?
- Should hide events have stricter freshness SLO than publish events?
- What alerts should fire when projection lag exceeds threshold?

Current V1 decision:

- Projection lag is expected.
- Publish lag may temporarily hide new articles.
- Hide/unpublish/archive lag is more sensitive because stale exposure is dangerous.
- Visibility uncertainty must fail closed where detected.

Future ADR should define:

- freshness targets
- endpoint-specific behavior during lag
- hide-event priority
- alert thresholds
- reconciliation trigger thresholds

---

## Summary

Open questions that must be resolved before production-hardening:

1. Popularity formula and time window.
2. Slug canonical redirect behavior.
3. Search backend and indexing strategy.
4. Reading cache TTL and invalidation policy.
5. Counter response and freshness policy.
6. Exact source event payload shape.
7. Processed-message tracking strategy.
8. Version gap and resync behavior.
9. Rebuild/cutover/fencing strategy.
10. Related articles strategy.
11. Source fallback policy.
12. Draft/admin preview boundary.
13. Personalization boundary.
14. External search/recommendation integration.
15. Projection freshness SLO.