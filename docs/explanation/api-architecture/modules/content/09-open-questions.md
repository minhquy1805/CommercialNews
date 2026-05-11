# Content — Open Questions & ADR Hooks (V1)

This document tracks Content decisions that are either locked for V1 or intentionally left as V2 ADR hooks.

---

## 1) Locked V1 decisions

### 1.1 Unpublish semantics

**V1 decision:** Unpublish is an action, not a separate Article status.

V1 uses:

- `Draft`
- `Published`
- `Archived`

Unpublish transition:

- `Published -> Draft`

Supporting fields:

- `UnpublishedAt`
- `ArticleLifecycleEvent.ActionType = 'Unpublish'`
- mandatory `Reason`

**V2 ADR hook:** decide whether CommercialNews needs a separate `Unpublished` state for articles that were once public but should not be treated as ordinary drafts.

Questions for V2:

- Do editors need to distinguish "never published draft" from "previously published but removed"?
- Should `Unpublished` become a first-class lifecycle state?
- If yes, what transitions are allowed?
  - `Published -> Unpublished`
  - `Unpublished -> Draft`
  - `Unpublished -> Published`
  - `Unpublished -> Archived`

### 1.2 Archive and soft-delete boundary

**V1 decision:** Archive and soft-delete are separate concepts.

- `Archived` is a lifecycle status.
- `IsDeleted=1` is a soft-delete flag.
- Physical purge is out of scope for V1.

V1 public visibility remains:

```text
Status = 'Published' AND IsDeleted = 0
```

**V2 ADR hook:** define physical purge and retention policy.

Questions for V2:

- When may soft-deleted content be physically purged?
- Who can approve purge?
- What audit/revision/lifecycle evidence must be retained after purge?
- Should purge emit `content.article_purged`?
- Should purge be reversible, delayed, or require two-person approval?

### 1.3 Archived restore

**V1 decision:** Archived restore is out of scope.

In V1:

- `Archived -> no transitions`

**V2 ADR hook:** decide whether `Archived -> Draft` should be allowed.

Questions for V2:

- Can archived articles be restored?
- If restored, should they return to `Draft` only?
- Should restore require a reason?
- Should restore emit `content.article_restored`?
- Should restore preserve old `PublishedAt`, or require a new publish action?

---

## 2) Open ADR hooks

### ADR-Hook 01: Edit history strategy

**V1 posture:** `ArticleRevision` stores the previous snapshot before a meaningful edit.

Current V1 fields:

- `OldTitle`
- `OldSummary`
- `OldBody`
- `ChangeSummary`
- `EditedBy`
- `EditedAt`
- `CorrelationId`

Open questions:

- Should V2 store full post-edit snapshots instead of previous snapshots?
- Should V2 store both old and new values?
- Should V2 use patch/diff storage through `PatchJson`?
- Do we need `RevisionNumber`?
- Do we need `ArticleVersion` on `ArticleRevision`?
- What is the retention policy for revisions?
- Do we need tamper-evident chaining, for example hash chain, in V2?

Possible ADR outcome:

- Snapshot strategy
- Diff strategy
- Hybrid snapshot + diff strategy
- Tamper-evident revision chain

### ADR-Hook 02: Update rules for Published articles

**V1 posture:** Published article updates may be allowed by policy, but must remain version-aware and truth-safe.

Open questions:

- Are published articles editable directly?
- Or must editors create a draft revision and republish?
- Which fields can be edited after publish?
  - `Title`
  - `Summary`
  - `Body`
  - `Category`
  - `Tags`
  - `CoverMediaId`
- If a published article is edited, should `content.article_updated` trigger:
  - SEO refresh?
  - Reading projection refresh?
  - cache invalidation?
  - search reindex?
- Should major edits require a new publish approval?
- Should minor edits be allowed without republish?

Possible ADR outcome:

- Direct published edits
- Draft revision workflow
- Minor/major edit policy
- Approval-required edit policy

### ADR-Hook 03: Taxonomy deletion policy

**V1 posture:** Category/Tag deletion must not leave invalid Content truth.

Open questions:

- If a Category is referenced by existing articles, should deletion be blocked?
- Should Category use deactivate/soft-delete instead of physical delete?
- Should articles be reassigned to a fallback category?
- If a Tag is referenced by articles, should delete detach it from all articles?
- Should Tag use deactivate/soft-delete instead?
- Should taxonomy changes emit events?
  - `content.category_soft_deleted`
  - `content.tag_soft_deleted`
- Should public Reading filters hide inactive categories/tags?

Recommended V1 default:

- Block physical deletion when referenced.
- Prefer deactivate/soft-delete semantics for referenced taxonomy.

### ADR-Hook 04: Category slug policy

**V1 posture:** `Category.Slug` is optional and not required by current Reading public routes.

Open questions:

- Will public routes need category slug?
  - `/category/{slug}`
  - `/reading/categories/{slug}/articles`
- If yes, should `Category.Slug` become required?
- Should uniqueness be global or scoped?
- If nullable, should SQL Server use filtered unique index?
  - `WHERE Slug IS NOT NULL`
- Is category slug owned by Content or SEO?

Recommended V1 default:

- `Category.Slug` remains nullable.
- No public route depends on `Category.Slug` in V1.

### ADR-Hook 05: Command idempotency storage

**V1 posture:** High-impact lifecycle actions should support idempotent retry semantics where feasible.

Open questions:

- Do we need a `CommandDeduplication` table?
- Should `Idempotency-Key` be required for:
  - publish
  - unpublish
  - archive
  - soft-delete
- What is the retention window for idempotency keys?
- Should idempotency be scoped by:
  - `actorUserId`
  - `articlePublicId`
  - command type
  - request body hash
- What response should repeated equivalent commands return?
  - `200 OK` stable success
  - `409 Conflict`
  - cached original response

Possible ADR outcome:

- No explicit command dedupe in V1
- `Idempotency-Key` for lifecycle commands
- `CommandDeduplication` table
- Request body hash validation

### ADR-Hook 06: Content event payload shape

**V1 posture:** Article events must include stable identity and version.

Required baseline:

- `MessageId`
- `EventType`
- `AggregateType = Article`
- `AggregateId = ArticlePublicId`
- `ArticleId`
- `ArticlePublicId`
- `Version = Article.Version`
- `CorrelationId`
- `OccurredAt`

Open questions:

- Should `content.article_updated` include changed fields?
- Should events include `Title` and `Summary`, or should consumers re-read Content truth?
- Should `content.article_published` include `SlugCandidate`?
- Should payloads be minimal or consumer-convenient?
- What is the event payload versioning strategy?

Recommended V1 default:

- Keep payloads minimal.
- Include `ArticleId`, `ArticlePublicId`, `Version`, timestamps, and only fields required by consumers.
- Consumers must tolerate add-only payload evolution.

### ADR-Hook 07: Reading projection strategy

**V1 posture:** Public read APIs live in Reading. Reading must not override Content visibility truth.

Open questions:

- Should Reading query Content truth directly in V1?
- Should Reading maintain its own projection later?
- If Reading has a projection, how is stale visibility prevented?
- Should Reading projection apply events by `ArticlePublicId + Version`?
- Should Reading fallback to Content truth when projection freshness is uncertain?
- What is the rebuild strategy for Reading projections?

Recommended V1 default:

- Reading may use direct truth-backed reads or truth-safe projections.
- Public visibility must remain consistent with Content truth.

### ADR-Hook 08: SEO integration strategy

**V1 posture:** SEO owns slug/canonical routing metadata. Content owns visibility truth.

Open questions:

- Should Content send `SlugCandidate` in events?
- Should SEO generate slug from title?
- Should slug changes create redirect/alias history?
- Should SEO deactivate slug routes on unpublish/archive/soft-delete?
- Or should slug remain resolvable while final visibility check returns safe 404?
- How should slug conflicts be resolved?

Recommended V1 default:

- Slug/routing success does not equal visibility success.
- SEO may own slug registry, but Content truth decides whether the article is public.

### ADR-Hook 09: Outbox event scope for Category/Tag

**V1 posture:** Article events are first implementation priority. Category/Tag events are reserved for later phases.

Open questions:

- Should category/tag create/update/soft-delete emit Content events in V1?
- Which downstream modules need them?
  - Reading
  - SEO
  - Audit
  - Search
- Should taxonomy events use their own aggregate identity?
- Should taxonomy changes trigger cache invalidation?

Reserved event names:

- `content.category_created`
- `content.category_updated`
- `content.category_soft_deleted`
- `content.tag_created`
- `content.tag_updated`
- `content.tag_soft_deleted`

---

## 3) Decision log to revisit before implementation

Before coding Content async, confirm:

- Article events are in scope for Phase 1.
- Category/Tag events are reserved, not required for Phase 1.
- `ArticlePublicId` is generated server-side and immutable.
- `Article.Version` increments on meaningful content/lifecycle changes.
- Lifecycle events store `ArticleVersion`.
- `content.article_soft_deleted` represents V1 soft-delete, not physical purge.
- Restore and physical purge are out of scope for V1.
- Reading public visibility remains truth-safe.
- SEO slug/routing success does not imply public visibility.
