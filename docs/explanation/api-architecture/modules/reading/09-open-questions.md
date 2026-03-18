# Reading — Open Questions & ADR Hooks (V1)

## ADR-01: Popularity definition
- Views vs likes vs blended score?
- Time window vs lifetime counters?

## ADR-02: Slug routing API shape
- Do we expose `/articles/slug/{slug}` or require SEO resolve client-side?
- If we expose, it must still use SEO `/resolve` internally.

## ADR-03: Search capability (V1 vs V2)
- Basic keyword search only in V1?
- Upgrade path to full-text / managed search.

## ADR-04: Caching policy
- Cache TTLs for list/detail?
- Invalidation triggers (SeoUpdated, ArticlePublished/Unpublished).
- Canary/caching pitfalls mitigation.

## ADR-05: Counter inclusion policy
- Return counters as optional fields?
- Add `countersPartial` flag vs return null/zero?