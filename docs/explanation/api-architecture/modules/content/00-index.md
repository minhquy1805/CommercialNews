# Content Module — API Architecture (V1)

**Purpose**
- Own the editorial lifecycle and taxonomy:
  - articles (draft → published → archived / non-public by policy)
  - publish/unpublish with governance reasons
  - edit history (traceable changes)
  - categories and tags (attach/detach rules)
- Keep **publication truth** explicit and authoritative.
- Support safe downstream projections, caches, notifications, SEO reactions, stream-style derived-state maintenance, and bounded rebuild/reconciliation workflows without turning those outputs into hidden truth.

**Why this module is critical**
- Content is the **source of truth** for publication state.
- Writes are low volume but high impact (bad publish/unpublish harms trust).
- Core lifecycle must not depend on side effects (email/audit/indexing).
- Public visibility correctness must remain true even when SEO, caches, projections, notifications, or downstream stream consumers lag.
- Derived read models may exist later, but must remain subordinate to Content truth.
- Content versions drive stale-write protection and downstream ordered apply rules for article-derived state.

**Primary consumers**
- Admin UI (write workflows: create/edit/publish/unpublish)
- Public Query/Reading module (read-only by policy in V1; projections in V2)
- SEO module (reacts to publication state via events)
- Audit module (ingests governance events asynchronously)
- Notifications module (optional: new-article notifications)
- Future search/projection/read-model consumers
- Future rebuild/reconciliation/reporting workflows for derived content outputs

**Non-goals (V1)**
- Full content moderation workflows beyond publish/unpublish (V2)
- Multi-tenant editorial partitions (future)
- Complex scheduling/embargo (future)
- Treating SEO/caches/projections as publication truth
- Letting derived outputs redefine current lifecycle truth
- Full event sourcing for Content truth
- Requiring synchronous completion of downstream projections before publish/unpublish succeeds

**Hard constraints**
- Lifecycle transitions must be validated and enforced.
- Unpublish must record a reason (governance).
- Edit history must be preserved and traceable (tamper-evident at policy level).
- Public read must never expose drafts/unpublished content.
- Side effects (audit/email/seo updates) must be async and non-blocking.
- Content truth is **primary and authoritative** for public visibility.
- Content-derived async delivery is **at-least-once**; duplicates, replay, and stale deliveries must be tolerated safely downstream.
- Per-article version/order semantics must protect against stale overwrite in both sync truth mutations and downstream derived-state apply.
- Derived content outputs must remain **rebuildable**, **observable**, and **subordinate to truth**.
- Rebuild/reconciliation workflows must be **bounded**, **observable**, and **rerun-safe**.
- Partially built candidate outputs must not leak as active/public truth.

**Truth vs derived posture**
- **Truth**:
  - article lifecycle state
  - publication timestamps and governance reasons
  - edit/revision history
  - taxonomy truth (categories/tags and assignment rules)
  - per-article version/revision where used
- **Derived**:
  - SEO/indexability reactions
  - search/serving artifacts
  - read models and projections
  - cached article lists/details
  - notification fan-out state
  - reporting, rebuild, reconciliation, or mismatch outputs
- **Rule**: Content truth decides whether content is public; derived outputs may lag, replay, or be rebuilt, but do not become publication authority.

**Stream / async posture**
- Content follows the standard V1 model:
  1. validate command
  2. enforce authorization
  3. commit Content truth + version + outbox atomically
  4. return success based on truth commit only
  5. let downstream consumers react asynchronously
- Content-derived events are truth-following, not truth-replacing.
- RabbitMQ/broker delivery is used for propagation, not as Content’s permanent truth store.
- Downstream consumers should use:
  - `messageId` for dedupe
  - `(articleId, version)` for stale-event rejection / ordered apply where relevant
- If a downstream consumer detects gaps or uncertainty, truth resync or bounded rebuild is preferred over unsafe stale apply.

**Batch / rebuild / reconciliation posture**
- Content may participate in bounded workflows for:
  - replay or repair of downstream article-derived outputs
  - projection rebuilds from Content truth
  - reconciliation between Content truth and derived public-facing outputs
  - reporting on lifecycle drift or publication mismatches
  - retention/cleanup workflows for historical artifacts by policy
- These workflows must:
  - start from authoritative Content truth
  - remain rerun-safe
  - avoid reintroducing older state over newer truth
  - validate important candidate outputs before publication/cutover where correctness matters
  - treat replay/rebuild as recovery for derived state, not as a redefinition of Content truth

**Primary correctness posture**
- Publication visibility is truth-first.
- Slug resolution, cache presence, projection presence, or notification success do not prove public visibility by themselves.
- If derived state is stale or uncertain, public behavior must fall back to safe truth-backed behavior.
- Safe non-progress or safe not-found beats incorrect exposure.
- Unpublish must win immediately at the truth boundary even if downstream state is behind.

**Consistency and ordering posture**
- Strong consistency is required at the Content truth boundary for:
  - lifecycle transitions
  - version advancement
  - visibility legality
  - revision/history append where policy requires it
- Eventual consistency is accepted for:
  - audit
  - notifications
  - SEO/reactive updates
  - projections/caches/search artifacts
  - reports and summaries
- Ordering is scoped **per article**, not globally across Content or across modules.
- Wall-clock timestamps are useful for observability, not as the main authority for Content causality.

**Failure and recovery posture**
- Timeout does not prove the Content mutation failed.
- Successful Content commit does not prove broker publish or downstream completion already happened.
- Downstream lag is acceptable only if:
  - it is observable
  - it is repairable
  - it cannot override Content visibility truth
- Important derived outputs must have a replay/rebuild/reconciliation path.

**Key links**
- System-wide rules:
  - `../../01-api-architecture-charter-v1.md`
  - `../../02-contracts-and-standards.md`
  - `../../04-versioning-and-compatibility.md`
  - `../../09-observability-and-slos.md`
- Arc42:
  - `../../../architecture/arc42/03-building-blocks-modularity.md`
  - `../../../architecture/arc42/04-runtime-view-v1.md` (Scenario 1–2)
  - `../../../architecture/arc42/05-quality-requirements.md`
  - `../../../architecture/arc42/13-transactions-and-consistency-v1.md`
  - `../../../architecture/arc42/18-stream-processing-and-derived-state-v1.md`
  - `../../../architecture/arc42/19-stream-processing-runtime-v1.md`
  - `../../../architecture/arc42/16-batch-processing-and-derived-data-v1.md`
  - `../../../architecture/arc42/17-dataflow-and-batch-workflows-v1.md`
- System data model:
  - `../../../architecture/arc42/system-data/system-data-content-v1.md` (if present)
- Quality profile:
  - `../../../architecture/arc42/quality/content-management.md` (if present)

**ADR hooks**
- Unpublish semantics: revert to Draft vs separate non-public state
- Edit history strategy: snapshot vs diff; retention policy
- Archive vs delete semantics (if both exist)
- Projection rebuild and publication/cutover policy for content-derived outputs
- Reconciliation policy between Content truth and public-facing derived views
- Idempotency-key policy for high-impact lifecycle commands
- Consumer version-gap handling and resync rules for Content-derived events