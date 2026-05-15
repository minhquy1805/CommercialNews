# SEO — Open Questions & ADR Hooks (V1)

## ADR-01: Slug uniqueness scope

- Should slug uniqueness be global?
- Should slug uniqueness be scoped by locale, category, tenant, or route namespace?
- Is `Scope + Slug` sufficient for V1?
- Do admin preview routes and public routes share the same slug namespace?

**V1 recommendation:** use `Scope + Slug` uniqueness. Start with `scope = public`.

---

## ADR-02: Slug stability policy

- When can slug change?
- Who is allowed to change it?
- Do title changes ever imply slug changes?
- Should slug changes require elevated permission?
- Should slug changes create redirect history?

**V1 recommendation:** title changes do not auto-change slug. Slug changes are explicit admin actions.

---

## ADR-03: Unpublish/archive/soft-delete routing behavior

- Should `/resolve` return `404` for unpublished, archived, or soft-deleted content?
- Or should it return `resourcePublicId` with `indexable = false` and let Reading handle visibility?
- Should preview/admin routes behave differently from public routes?
- How should restored content re-enter routing/indexability?

**V1 recommendation:** public routing should prefer safe `404` or safe deny behavior. Reading must still validate Content truth before public exposure.

---

## ADR-04: Canonical rules

- How should canonical URL be generated?
- Should canonical URL always follow the current slug?
- Can admins override canonical URL?
- How should near-duplicate content be handled?
- Should canonical URL be relative-only or allow approved absolute domains?

**V1 recommendation:** canonical URL should be deterministic and constrained to approved site/domain policy.

---

## ADR-05: Slug alias / redirect strategy

- Should old slugs be kept as aliases?
- Should old slugs return `301` permanent redirects?
- What is the retention window for aliases?
- Can an old slug be reused by another article?
- How are redirect loops prevented?

**V1 recommendation:** keep alias/redirect history out of baseline V1 unless explicitly needed. Use one active slug per scope/resource.

---

## ADR-06: Content event auto-sync policy

- Which Content events should SEO consume in V1?
- Should `content.article_updated` update SEO metadata defaults?
- Should title/summary changes update `MetaTitle` / `MetaDescription` automatically?
- Should category/tag changes affect canonical URL or slug?
- Should route/indexability updates be separated from metadata updates?

**V1 recommendation:** consume lifecycle events first: `content.article_published`, `content.article_unpublished`, `content.article_archived`, `content.article_soft_deleted`, `content.article_restored`. Treat `content.article_updated` as optional and policy-controlled.

---

## ADR-07: Manual override granularity

- Is manual override applied to the entire metadata record?
- Or is manual override tracked per field?
  - `MetaTitle`
  - `MetaDescription`
  - `OgTitle`
  - `OgDescription`
  - `OgImageUrl`
  - `CanonicalUrl`
- Can lifecycle-derived fields still update while metadata fields are manually overridden?
- How does admin reset metadata back to auto-generated defaults?

**V1 recommendation:** start with `IsManualOverride` at record level if simplicity is preferred. Consider field-level override in V2 if editorial workflows require it.

---

## ADR-08: SEO consumer idempotency storage

- Should SEO use a dedicated `SeoConsumedMessage` table?
- Or should dedupe/apply markers live on `SlugRegistry` / `SeoMetadata`?
- How long should consumed-message records be retained?
- Should replay/rebuild use the same dedupe table or bypass it under controlled mode?
- How are duplicate, stale, and replayed events represented operationally?

**V1 recommendation:** use durable storage for SEO truth-affecting consumer dedupe. Redis TTL is not enough for SEO truth mutation.

---

## ADR-09: Version gap and stale event behavior

- If SEO receives `AggregateVersion = 9` after version `10` was already applied, should it ignore or resync?
- If SEO receives version `11` but last applied is `9`, should it apply, defer, or resync from Content truth?
- Are Content lifecycle events guaranteed to carry monotonic aggregate versions?
- What is the exact threshold for triggering reconciliation?

**V1 recommendation:** ignore stale events. Trigger truth resync or bounded reconciliation on version gaps or ambiguous ordering.

---

## ADR-10: SEO emitted events

- Should SEO emit its own integration events in V1?
- Which events are needed?
  - `seo.slug_route_changed`
  - `seo.slug_route_deactivated`
  - `seo.metadata_updated`
- Should Audit consume SEO events?
- Should cache/sitemap/search refresh consume SEO events?
- Are SEO events required now, or can cache invalidation be handled internally first?

**V1 recommendation:** make SEO event emission optional in V1. If adopted, SEO truth mutation and Outbox record must commit atomically.

---

## ADR-11: Sitemap and search/index scope

- Is sitemap generation part of SEO V1?
- Is search indexing part of SEO V1 or a future Search module?
- Should sitemap refresh be event-driven, batch-driven, or hybrid?
- Does unpublish/archive/soft-delete immediately remove entries from sitemap/index?
- What lag budget is acceptable?

**V1 recommendation:** document sitemap/search as future derived workflows unless implementation is explicitly in scope.

---

## ADR-12: Cache invalidation strategy

- Should `/resolve` use Redis in V1?
- Should cache be write-through, read-through, or invalidation-based?
- What TTL should route cache use?
- Should cache entries include `routeVersion` and `sourceAggregateVersion`?
- How should stale cache writes be rejected?

**V1 recommendation:** cache is acceleration only. SEO truth DB remains routing authority. Cache refresh/invalidation must be idempotent and version-aware.

---

## ADR-13: Public route response shape

- Should public `/resolve` return inactive/non-indexable routes?
- Should it return `indexable = false`, or just safe `404`?
- Should public response include `sourceAggregateVersion`?
- Should public response expose only minimal routing data?

**V1 recommendation:** keep public response minimal. Do not expose internal debug fields. Prefer safe `404` when public visibility is uncertain.

---

## ADR-14: Rebuild and reconciliation ownership

- Which SEO outputs are rebuildable?
- Should rebuild start from SEO truth, Content truth, or both?
- Should rebuild produce candidate output before cutover?
- How are stale candidates rejected?
- Who can trigger rebuild/reconciliation manually?

**V1 recommendation:** SEO-derived serving artifacts are rebuildable. Candidate-before-cutover is required for correctness-sensitive outputs.

---

## ADR-15: Multi-locale / multi-tenant SEO

- Will V1 support locale-specific slugs?
- Will V1 support tenant-specific routing?
- Does `Scope` cover locale/tenant, or are separate fields required?
- How does canonical URL behave across locales?

**V1 recommendation:** keep V1 simple with `scope = public`. Reserve schema/API space for future locale/tenant expansion if needed.

---

## ADR-16: Robots and indexability policy

- Should `Robots` be stored as SEO metadata?
- Is `IsIndexable` derived only from Content lifecycle?
- Can admin manually set `noindex` for published content?
- Can archived/unpublished content ever be indexable?
- Should `robots` be field-level override protected?

**V1 recommendation:** non-public lifecycle states must be non-indexable. Admin-controlled `noindex` for published content may be allowed later if needed.

---

## ADR-17: Admin permission model

- Which permissions are required for reading SEO metadata?
- Which permissions are required for changing slug?
- Should slug change require stronger permission than metadata edit?
- Should canonical URL changes require stronger permission?
- Should generate/check slug endpoints have separate permissions?

**V1 recommendation:** separate read/update permissions at minimum. Consider stronger permission for slug/canonical changes.

---

## ADR-18: Preview/admin route behavior

- Should drafts have preview slugs?
- Are preview routes part of SEO or Content/Reading?
- Should preview routes use separate `scope = preview`?
- Can preview route resolution expose resource existence to authenticated admins only?

**V1 recommendation:** keep preview routing separate from public SEO V1 unless explicitly needed.