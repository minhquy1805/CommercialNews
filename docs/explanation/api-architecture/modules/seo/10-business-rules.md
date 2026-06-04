# SEO — Business Rules (V1)

This document defines the core business rules for SEO in V1.
It focuses on rules that API behavior, domain validation, async consumers, cache/rebuild workflows, and public-serving safety must implement consistently.

Related:

- `01-api-surface.md`
- `02-domain-contracts.md`
- `03-runtime-flows.md`
- `04-errors-status-codes.md`
- `05-security-abuse-controls.md`
- `06-idempotency-consistency.md`
- `07-observability-slos.md`
- `08-dependencies-and-ownership.md`
- `09-open-questions.md`

---

## 1) SEO ownership rules

- SEO owns:
  - slug routing truth
  - SEO metadata truth
  - canonical URL policy
  - slug conflict and active-route policy
  - route/metadata version or revision markers where used
  - SEO-owned Outbox intent for downstream cache/sitemap/search/audit signals, if adopted
  - rebuild/reconciliation of SEO-owned derived serving artifacts, if introduced
- SEO does **not** own:
  - Content lifecycle truth
  - public visibility truth
  - article body/content truth
  - Reading public response composition
  - Audit ingestion truth
  - search/index truth unless explicitly introduced as a SEO-owned derived artifact

**Rule:** SEO can answer where a slug points. Content/Reading decides whether that target may be publicly served.

**Rule:** Public and cross-module SEO contracts use stable public identifiers such as `ResourcePublicId`, not internal database primary keys.

---

## 2) Slug and route rules

### 2.1 Slug uniqueness

- active slugs must be unique by `(Scope, Slug)`
- V1 starts with `scope = public`
- slug ownership is determined only by a successful SEO truth-bound write commit
- slug availability and slug generation endpoints are advisory only
- check-then-insert logic must not be the only protection; the SEO truth store must enforce uniqueness
- a slug conflict must return a clear `409 Conflict`

### 2.2 Slug stability

- title changes do not automatically change slugs in V1
- slug changes are explicit admin SEO actions
- slug changes should require SEO update permission
- slug changes must advance SEO version/revision where ordering matters
- old-slug alias/redirect history is out of baseline V1 unless a later policy explicitly introduces it

### 2.3 Route ownership

- `(Scope, ResourceType, ResourcePublicId)` should identify at most one active/current route
- public route responses must expose `resourcePublicId`, not internal database identifiers
- route resolution is a routing helper, not final public visibility authority
- inactive, stale, or uncertain public routes must prefer safe not-found / safe deny behavior

---

## 3) Public routing and visibility rules

- `GET /api/v1/seo/resolve` returns `200` only when SEO can safely resolve the route.
- public not-found is a safe `404`; it must not leak whether content exists but is non-public
- Reading must validate Content truth before returning public content
- a stale active route must not expose draft, unpublished, archived, or soft-deleted content
- cache hits and derived route artifacts are never enough authority for public exposure
- safe not-found or safe degradation is preferable to incorrect public exposure

**Rule:** routing success is not visibility success.

---

## 4) Metadata and canonical rules

### 4.1 Metadata ownership

- SEO owns current SEO metadata for a resource:
  - `MetaTitle`
  - `MetaDescription`
  - `OgTitle`
  - `OgDescription`
  - `OgImageUrl`
  - `Robots` if adopted
- metadata freshness may lag Content updates within policy
- metadata must not be used as authority for public visibility
- metadata writes must enforce length, format, and allowlist rules

### 4.2 Canonical URL

- canonical URL must be deterministic and constrained by approved site/domain policy
- canonical URL must not point to an unapproved external domain
- canonical URL changes are SEO truth changes
- canonical URL updates should advance metadata/route version where ordering matters
- canonical URL policy must avoid duplicate indexing where practical

---

## 5) Manual override rules

- admin-edited SEO metadata is treated as a manual override unless the request explicitly opts into an allowed auto-sync overwrite policy
- automatic Content event sync must not overwrite manual SEO metadata unless explicitly allowed
- manual `MetaTitle` blocks automatic title-derived meta title overwrite
- manual `MetaDescription` blocks automatic summary-derived meta description overwrite
- manual social preview fields block automatic social-preview overwrites for those fields
- lifecycle-derived route/indexability updates may still apply while metadata is manually overridden
- manual override decisions must be persisted in SEO truth, not inferred from timestamps

**Rule:** manual override is a business-level stale-overwrite protection, not a UI hint.

---

## 6) Content lifecycle coupling rules

SEO follows Content lifecycle events but does not own Content lifecycle truth.

SEO may consume:

- `content.article_published`
- `content.article_unpublished`
- `content.article_archived`
- `content.article_soft_deleted`
- `content.article_restored`
- `content.article_updated` where SEO auto-sync policy requires metadata evaluation

Lifecycle policy:

- published content may become routable/indexable by policy
- unpublished content must be non-indexable
- archived content must be non-indexable
- soft-deleted content must be non-indexable
- restored content must not automatically become indexable unless Content truth says the article is publicly visible
- stale or replayed publish-derived events must not resurrect now-non-public content

Content events are inputs for SEO convergence only.
They do not authorize SEO to write Content lifecycle state or infer final public visibility without truth validation.

---

## 7) Admin SEO write rules

- admin SEO endpoints require authentication and explicit SEO authorization
- `PUT /api/v1/admin/seo/articles/{articlePublicId}` mutates SEO truth for that article's public identity
- if omitted in V1, request `scope` defaults to `public`
- admin writes must validate:
  - slug format
  - scope
  - canonical URL
  - metadata lengths
  - image URL shape where applicable
  - supported `ResourceType`
- stale admin writes must be rejected through `If-Match`, version, rowversion, or compare-and-set policy where adopted
- slug conflict must return `409 Conflict`
- stale version / precondition mismatch must return `412 Precondition Failed`
- admin write success means SEO truth committed
- admin write success does not mean cache invalidation, sitemap refresh, search/index refresh, Audit ingestion, or downstream projections have completed

---

## 8) Transaction and event emission rules

- SEO truth mutation and required local version/revision advancement must commit atomically
- if SEO event emission is adopted, SEO truth mutation and the Outbox record must commit atomically
- Redis/cache update, broker publish, sitemap refresh, search/index refresh, Audit ingestion, and projection refresh are post-commit async effects
- downstream async failure must not turn a committed admin SEO write into a failed API response

Possible SEO-owned integration events:

- `seo.slug_route_changed`
- `seo.slug_route_deactivated`
- `seo.metadata_updated`

SEO event emission is optional in V1.

---

## 9) Idempotency and ordering rules

- Content-derived SEO consumers must dedupe by `MessageId`
- SEO truth-affecting consumers must use durable dedupe/apply markers
- Redis TTL dedupe may be used only for non-critical cache refresh/invalidation dedupe
- Content-derived apply must be version-aware:
  - use `AggregateId` / `ResourcePublicId`
  - use `AggregateVersion`
  - track `SourceAggregateVersion`
  - record `LastAppliedMessageId`
  - record `LastSyncedAtUtc`
- duplicate events must converge harmlessly
- stale events must be ignored or rejected
- version gaps or ambiguity should trigger retry, defer, truth resync, or bounded reconciliation
- timestamp ordering such as `UpdatedAt` or `OccurredAtUtc` must not replace version/freshness checks

---

## 10) Cache and derived-state rules

- Redis and edge caches are acceleration only
- SEO truth store remains the routing authority
- cache entries must not bypass Content truth visibility checks
- stale cache writes must lose to fresher route/version knowledge
- TTL is a safety net, not the correctness mechanism
- route cache miss, timeout, or ambiguity must not be treated as permanent truth-backed absence
- derived serving artifacts must remain:
  - observable
  - rebuildable
  - subordinate to SEO truth
  - subordinate to Content visibility truth for public exposure

---

## 11) Rebuild and reconciliation rules

- SEO rebuild/reconciliation workflows may repair derived SEO-serving state
- rebuild may use bounded SEO truth and bounded Content truth inputs
- rebuild output is derived state unless explicitly defined as SEO truth
- candidate output must be validated before publication/cutover
- partial candidate output must not become active automatically
- stale candidates must not replace fresher SEO truth or fresher Content visibility truth
- rebuild/reconciliation must be rerun-safe
- if uncertainty exists, truth re-read/resync is preferred over unsafe cutover

---

## 12) Security and privacy rules

- public SEO APIs must not expose internal database primary keys
- public routing `404` must be safe and must not reveal draft/non-public existence
- slugs are public identifiers and may be logged carefully, but metrics must not use raw `slug` as a high-cardinality label by default
- metrics must not use raw `ResourcePublicId` as high-cardinality labels by default
- admin SEO writes must be audited or made auditable through SEO events / Audit ingestion where adopted
- Audit ingestion lag must not redefine SEO truth commit success

---

## 13) Observability and release-safety rules

- any draft/non-public exposure incident is a release blocker
- `404` on public routing is an expected outcome and must be tracked separately from `5xx` / timeouts
- `/resolve` latency and error rate are hot-path critical
- DB fallback rate is a cache/staleness signal, not automatically a user-visible failure
- "resolved but denied by visibility" must be tracked as an internal correctness/drift signal
- stale-event rejects, version-gap/resync counts, dedupe hits, and DLQ age must be observable
- manual override protected/skip count must be observable
- admin `409 Conflict` and `412 Precondition Failed` rates must be observable

---

## 14) Summary

SEO correctness in V1 rests on fourteen rules:

1. SEO owns routing truth and SEO metadata truth, not Content visibility truth.
2. Public/cross-module SEO contracts use stable public identifiers such as `ResourcePublicId`.
3. `(Scope, Slug)` active uniqueness is enforced at the SEO truth boundary.
4. Title changes do not automatically change slug in V1.
5. Route resolution is not public serve permission.
6. Metadata freshness may lag, but metadata must not influence public visibility truth.
7. Manual SEO metadata must not be overwritten by auto-sync unless policy explicitly allows it.
8. Content lifecycle events are convergence inputs, not ownership transfer.
9. Admin SEO write success means SEO truth committed, not downstream async completion.
10. SEO event emission is optional in V1; if adopted, truth mutation and Outbox commit atomically.
11. SEO consumers must be idempotent, durable where truth-affecting, and version-aware.
12. Redis/cache/derived artifacts accelerate serving but never become routing or visibility truth.
13. Rebuild/cutover must not publish stale route knowledge over fresher truth.
14. Visibility leak tolerance is zero.
