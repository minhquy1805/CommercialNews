# Media Module — API Architecture (V1)

**Purpose**
- Manage media objects and their attachment policy to articles:
  - store media metadata (url/path, type, alt text)
  - attach/detach media to articles
  - select a primary (cover) media
  - reorder attachments
  - soft delete / restore by policy
- Keep **media relationship truth** explicit and authoritative.
- Support derived media delivery, optional derivative generation, and bounded cleanup/reconciliation workflows without letting storage/CDN/thumbnail outputs become hidden truth.

**Why this module is critical**
- Media strongly affects perceived platform quality (broken cover = bad UX).
- Upload/ingestion is an abuse surface (malicious files, oversized payloads, metadata risks).
- Attachment rules must be deterministic and consistent.
- Derived delivery layers may fail independently, so truth and presentation must stay clearly separated.
- Cleanup/reconciliation workflows may grow over time and must remain bounded, rerun-safe, and subordinate to Media truth.
- At-least-once async delivery, replay, and stale worker execution are normal operational conditions for derived media workflows and must not weaken truth.

**Primary consumers**
- Admin UI (upload/register, attach, reorder, set primary, delete/restore)
- Reading module (read-only access to primary/attachments)
- Content module (may reference cover media by ID only; no workflow joining)
- Optional downstream workers for cache invalidation, derivative generation, cleanup, or repair workflows

**Non-goals (V1)**
- Heavy media processing (transcoding, image resizing pipelines) (V2+)
- CDN/object storage orchestration (future; may be purchased/managed)
- Advanced rights management and licensing
- Treating storage/CDN/thumbnail availability as relationship truth
- Coordination-heavy global media processing ownership

**Hard constraints**
- Attachment integrity: no broken references.
- Primary media rule: at most one primary per article (or none by policy).
- Ordering semantics must be stable and deterministic.
- Delete/restore must follow soft-delete policy and retention window.
- Media handling must follow secure operational practices.
- Media truth is **primary and authoritative** for attachment membership, order, primary selection, and deletion state.
- Derived media outputs must remain **observable**, **rebuildable where practical**, and **subordinate to Media truth**.
- Cleanup/reconciliation workflows must be **bounded**, **observable**, and **rerun-safe**.
- Partial or candidate derived outputs must not be exposed as if they were final active truth.
- Truth commit defines Media success; CDN/cache/derivative completion does not.

**Truth vs derived posture**
- **Truth**:
  - media metadata
  - attachment membership
  - primary selection
  - ordering
  - soft delete / restore state
  - local revision/version markers where implemented
- **Derived**:
  - CDN/object-store delivery state
  - thumbnails or transformed variants
  - cached media lists
  - derived summaries/snapshots
  - cleanup candidate sets
  - reconciliation and mismatch reports
- **Rule**: Media truth decides relationship state; storage/CDN/derived variants may lag and be rebuilt, but they do not become authority for attachment/order/primary truth.

**Batch / cleanup / reconciliation posture**
- Media may run bounded workflows for:
  - orphan or expired media cleanup
  - truth-vs-storage reconciliation
  - derived media list rebuild
  - derivative/thumbnail repair
  - attachment/order/primary drift detection and reporting
- These workflows must:
  - start from authoritative Media truth or approved durable input
  - remain rerun-safe
  - avoid overwriting fresher truth with stale repair output
  - validate important candidate outputs before publication/cutover where correctness matters

**Primary correctness posture**
- Reading may degrade gracefully when media delivery or derived variants fail.
- Missing CDN/object-store output does not redefine Media truth.
- Timeout does not prove a Media mutation failed.
- If uncertainty exists, reconcile from Media truth rather than infer from derived delivery state.
- Cause must be durable before effect becomes meaningful:
  - attachment/order/primary/delete truth commits first
  - cache/CDN/derivative side effects follow later

**Key links**
- System-wide rules:
  - `../../01-api-architecture-charter-v1.md`
  - `../../02-contracts-and-standards.md`
  - `../../07-security-threat-modeling.md`
  - `../../09-observability-and-slos.md`
- Arc42:
  - `../../../architecture/arc42/03-building-blocks-modularity.md`
  - `../../../architecture/arc42/05-quality-requirements.md`
  - `../../../architecture/arc42/13-transactions-and-consistency-v1.md`
  - `../../../architecture/arc42/18-stream-processing-and-derived-state-v1.md`
  - `../../../architecture/arc42/19-stream-processing-runtime-v1.md`
  - `../../../architecture/arc42/16-batch-processing-and-derived-data-v1.md`
  - `../../../architecture/arc42/17-dataflow-and-batch-workflows-v1.md`
- Upstream/downstream:
  - Content (articleId reference only)
  - Reading (read path composition)
  - SEO (optional social preview image pointer)
  - Audit (async ingestion of governance actions)

**ADR hooks**
- Soft delete retention and restore semantics
- Allowed media types and safe handling policy
- Whether media can exist independently of article state (publication coupling)
- Upload strategy (direct upload vs presigned URLs) (future)
- Cleanup/reconciliation policy for media-derived outputs
- Candidate publication/cutover policy for important derived media outputs
- Derivative-generation policy and stale-worker protection if Media processing expands in V2+