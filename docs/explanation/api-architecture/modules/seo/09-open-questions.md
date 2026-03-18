# SEO — Open Questions & ADR Hooks (V1)

## ADR-01: Slug uniqueness scope
- Global uniqueness vs per-category vs per-locale?

## ADR-02: Slug stability policy
- When can slug change?
- Who is allowed to change it?
- Do title changes ever imply slug changes?

## ADR-03: Unpublish/archive routing behavior
- Should `/resolve` return 404 for unpublished/archived?
- Or return resourceId with `indexable=false` and let Reading handle visibility?
(Recommendation: safe 404 for public routing to avoid leaks.)

## ADR-04: Canonical rules
- How to handle near-duplicate content?
- How to enforce canonical consistency?

## ADR-05: Slug alias/redirect strategy (V2)
- Keep old slugs as aliases?
- Permanent redirects?
- Retention window for aliases?