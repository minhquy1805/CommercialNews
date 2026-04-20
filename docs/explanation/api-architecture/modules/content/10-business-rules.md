# Content — Business Rules (V1)

This document defines the core business rules for Content in V1.  
It focuses on concrete rules that application code, API behavior, lifecycle enforcement, and downstream integration must implement consistently.

Related:

- `01-api-surface.md`
- `02-domain-contracts.md`
- `03-runtime-flows.md`
- `04-errors-status-codes.md`
- `05-security-abuse-controls.md`
- `06-idempotency-consistency.md`
- `08-dependencies-and-ownership.md`

---

## 1) Content ownership rules

- Content owns:
  - article lifecycle truth
  - article visibility truth
  - article body/metadata truth
  - taxonomy truth for categories and tags
  - article-tag attachment truth
  - revision/history truth
  - per-article version truth
- Content is the authoritative source for whether an article is currently public.
- Content does **not** own:
  - notification delivery truth
  - audit evidence truth
  - SEO/search/read-model truth
  - public-read serving truth in Reading
  - media-object truth

---

## 2) Article lifecycle rules

### 2.1 Baseline lifecycle

Recommended V1 lifecycle states:

- `Draft`
- `Published`
- `Unpublished`
- `Archived`

If a different lifecycle model is chosen by ADR, all APIs, events, and validations must remain aligned with that chosen truth model.

### 2.2 Lifecycle transition rules

Allowed transitions must be explicit and deterministic.

Recommended baseline:

- `Draft -> Published`
- `Published -> Unpublished`
- `Unpublished -> Draft` or `Unpublished -> Published` according to policy
- `Published -> Archived`
- `Unpublished -> Archived`
- `Archived -> Draft` *(recommended baseline restore posture)*

### 2.3 Lifecycle legality rule

- invalid lifecycle transitions must be rejected deterministically
- lifecycle legality must be checked against current authoritative Content truth
- stale client belief, stale caches, or stale projections must not decide lifecycle legality

---

## 3) Visibility rules

- Public visibility is determined only by Content truth.
- Draft articles must never be publicly visible.
- Unpublished articles must never be publicly visible.
- Archived articles must never be publicly visible.
- Downstream lag in SEO, Reading, caches, routes, or projections must not re-expose non-public content.
- Safe not-found or safe degradation is preferable to incorrect public exposure.

---

## 4) Publish / unpublish rules

### 4.1 Publish rules

- publish must be a valid lifecycle transition
- publish must commit:
  - new lifecycle truth
  - required timestamps/metadata
  - version advancement
  - outbox intent where downstream effects are required
- publish success does not wait for downstream SEO, notifications, audit, projections, or caches
- publish should converge safely under retry according to the chosen idempotency policy

### 4.2 Unpublish rules

- unpublish must be a valid lifecycle transition
- unpublish requires a mandatory reason
- unpublish must commit:
  - non-public lifecycle truth
  - required timestamps/metadata
  - version advancement
  - outbox intent where downstream effects are required
- unpublish takes effect immediately at the truth boundary
- replay of older publish-derived state must not resurrect public visibility

---

## 5) Archive / restore rules

- archive is a truth-bound lifecycle action
- archive must remove public visibility immediately according to Content truth
- restore semantics must be explicitly documented and consistent
- repeated equivalent archive/restore requests must converge safely as no-op or documented conflict
- downstream lag must not weaken archive/restore truth

---

## 6) Draft and update rules

- article create should produce a `Draft` article by baseline V1 policy
- updates must respect current lifecycle policy:
  - draft-only updates
  - or restricted published-state updates, if allowed
- update flows must enforce stale-write protection where policy requires it
- update success means Content truth committed
- generic update payloads must not bypass lifecycle rules

---

## 7) Version and concurrency rules

- each article has a monotonic `Version` used for lifecycle/order/freshness protection
- meaningful lifecycle/content changes should advance version
- stale writes must be rejected or resolved by documented optimistic concurrency policy
- `UpdatedAt` is not the primary freshness authority where version semantics exist
- downstream consumers should use `(ArticleId, Version)` for stale-event rejection where ordering matters

---

## 8) Revision/history rules

- revision history is append-only by intent
- updates must create traceable revision entries according to policy
- previous revisions must not be silently rewritten
- revision history is historical content evidence, not current live truth
- revision retention/purge, if implemented, must preserve policy and auditability requirements

---

## 9) Taxonomy rules

### 9.1 Category rules

- an article must not reference a non-existent category
- category naming/uniqueness must be deterministic under the chosen normalization policy
- category delete/deactivate behavior must not leave invalid article truth
- reassignment/orphan-prevention policy must be explicit

### 9.2 Tag rules

- tags attached to articles must exist
- duplicate tag attachment must not create duplicate truth
- tag naming/uniqueness must be deterministic under the chosen normalization policy
- tag delete/deactivate behavior must not leave invalid attachment truth

### 9.3 Attachment rules

- attach/detach rules must be deterministic
- stale attach/detach attempts must not corrupt current attachment truth
- article-tag attachment remains Content-owned truth

---

## 10) Security and authorization rules

- all admin content endpoints require Bearer auth
- all admin content endpoints require explicit authorization policies
- publish/unpublish/archive/restore/delete actions must be auditable
- object-level checks, where needed, must compose with centralized authorization
- clients must not directly set server-owned lifecycle/security-sensitive fields such as:
  - `Status`
  - lifecycle timestamps
  - `AuthorUserId`
  - version markers

---

## 11) Truth-first write rules

For content-changing operations, success means:

- Content truth committed successfully

Where applicable, it also means:

- async intent/outbox committed successfully

It does **not** mean:

- audit is already queryable
- SEO is already updated
- notifications are already sent
- public projections are already refreshed
- caches are already invalidated
- search/index artifacts are already current

---

## 12) Idempotency and retry rules

The following operations should converge safely under retry where feasible:

- publish
- unpublish
- archive
- restore

Rules:

- repeated equivalent command must not create duplicate harmful downstream effects
- timeout does not prove mutation failure
- safe reconciliation after ambiguity must inspect Content truth
- duplicate/replayed downstream delivery must be harmless under dedupe/version rules

---

## 13) Event rules

Typical Content events include:

- `Content.ArticleCreated`
- `Content.ArticleUpdated`
- `Content.ArticlePublished`
- `Content.ArticleUnpublished`
- `Content.ArticleArchived`
- `Content.ArticleRestored`

Rules:

- events are emitted only after truth commit
- events carry stable `MessageId`
- events are minimal and privacy-aware
- downstream consumers must tolerate:
  - at-least-once delivery
  - duplicate delivery
  - delayed delivery
  - replay after crash/rebuild
- Content events propagate committed truth; they do not replace it

---

## 14) Downstream and derived-state rules

Downstream systems may derive from Content truth, including:

- SEO
- Notifications
- Reading projections
- search/index artifacts
- analytics/reporting outputs

Rules:

- derived outputs remain subordinate to Content truth
- derived outputs may lag, be rebuilt, or be replayed
- stale derived outputs must not outrank current Content truth
- replay/rebuild/reconciliation is a recovery mechanism, not a truth-transfer mechanism

---

## 15) Public-serving safety rules

- routing success does not prove public visibility
- cache/projection/search hit does not prove current public visibility
- if derived state is stale or uncertain, public-serving logic must prefer:
  - Content truth-backed visibility check
  - safe degradation
  - safe not-found
- no downstream system may re-expose content that Content truth marks as non-public

---

## 16) Business rules summary

1. Content owns lifecycle and visibility truth.  
2. Public visibility is determined only by Content truth.  
3. Publish/unpublish/archive/restore are truth-bound lifecycle actions.  
4. Unpublish requires a mandatory reason.  
5. Meaningful lifecycle/content changes advance per-article version.  
6. Revisions are append-only historical evidence, not live truth.  
7. Taxonomy references and attachments must remain non-orphaned and deterministic.  
8. Content writes are truth-first and do not wait for downstream completion.  
9. Events are post-commit, minimal, and replay-safe.  
10. Derived outputs may help serving and discovery, but never outrank current Content truth.  
11. Safe fallback beats stale public exposure.  
12. Replay/rebuild/reconciliation remain recovery tools, not truth-transfer mechanisms.