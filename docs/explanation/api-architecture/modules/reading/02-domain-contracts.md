
---

## `docs/explanation/api-architecture/modules/reading/02-domain-contracts.md`

```md
# Reading — Domain Contracts (V1)

Reading is a **query facade**. It owns query semantics and response composition, not the source-of-truth lifecycle.

## 1) Ownership
Reading owns:
- public query semantics (filters/sorts/paging)
- composition rules (what data is included, fallbacks)
- caching policy (V1)
- degradation behavior (what is optional vs required)

Reading does not own:
- publication state (Content owns)
- slug routing rules (SEO owns)
- media object lifecycle (Media owns)
- counters truth (Interaction owns)

---

## 2) Visibility invariants (non-negotiable)
- Public responses must include **only Published** content.
- Draft/unpublished/archived must not be exposed (even if SEO resolves incorrectly).
- Safe not-found behavior must not leak non-public resources.

---

## 3) Sorting/filter semantics (must be explicit)
Reading must document:
- allowed sort fields (allowlist)
- filter behavior and defaults
- tie-break rules for stable paging (e.g., `publishedAt desc, articleId desc`)

---

## 4) Related content contract
Related articles must be deterministic:
- primary: same category
- secondary: shared tags
- fallback: newest published

---

## 5) Counter semantics (eventual)
- Views/likes counters may lag or be unavailable.
- Reading must degrade gracefully:
  - omit counters or return zeros with a `countersPartial=true` flag (optional) to avoid lying.

---

## 6) Events (V2+ hooks)
V1: Reading may read directly by policy.
V2+: Reading becomes a Read Model and consumes events:
- `ArticlePublished/Unpublished/Updated`
- `SeoUpdated`
- `MediaPrimaryChanged`
- `ArticleViewed/ArticleLiked`