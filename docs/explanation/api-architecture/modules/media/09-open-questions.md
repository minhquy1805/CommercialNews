# Media — Open Questions & ADR Hooks (V1)

This document captures unresolved Media decisions, future ADR candidates, and implementation policy hooks.

V1 defaults should remain conservative:

- Media owns truth for media metadata, attachment membership, ordering, primary selection, and delete/restore state.
- Media emits events through Outbox.
- Audit consumes Media events first.
- Reading, SEO, CDN, scan, and variant workflows are downstream extensions and must not block Media write success.

---

## ADR-01: Soft delete retention window

Questions:

- How long can media be restored?
- When do we hard-delete, if ever?
- Should retention differ by media type, article status, or legal policy?
- Should purge be manual, scheduled, or operator-approved?
- What audit evidence must remain after purge?

V1 default:

- Soft delete only.
- Hard delete/purge API is out of scope.
- Restore is allowed only within retention policy.

---

## ADR-02: Allowed media types and validation

Questions:

- Allowed types for V1:
  - Image only?
  - Image + File?
  - Image + Video + File?
- Maximum size limits by type?
- Allowed MIME types?
- Is file extension trusted, ignored, or only used as a hint?
- What metadata fields are allowed?
- Should EXIF be stripped, ignored, or stored selectively?
- Should unsafe metadata cause rejection or sanitization?

V1 default:

- Allowlist types.
- Sanitize metadata.
- Do not trust client-supplied MIME/type blindly.
- Do not claim malware safety without scan pipeline.

---

## ADR-03: Primary media delete/detach behavior

Questions:

- Should delete be blocked if media is primary?
- Should delete auto-clear primary?
- Should detach primary auto-clear primary?
- Should system auto-select fallback primary?
- If fallback exists, what is deterministic policy?
  - lowest `SortOrder`
  - newest attachment
  - first image only
  - no fallback

V1 default:

- Delete/detach clears primary where feasible.
- No automatic fallback primary.
- Admin explicitly sets a new primary.

---

## ADR-04: Upload strategy

Questions:

- Direct upload through API?
- Presigned URLs to object storage?
- Hybrid strategy?
- Should `POST /items` only register metadata after upload?
- How do clients reconcile timeout during upload/register?
- Should storage object keys be deterministic?
- Should object keys include tenant/user/date partitions?

V1 default:

- API surface registers metadata after binary upload.
- Binary upload mechanism is implementation-specific.
- Storage operations must not be inside Media DB transaction.
- Orphan cleanup/reconciliation is required as an operational policy.

---

## ADR-05: Publication coupling

Questions:

- Can media exist without an article?
- Can one media asset attach to multiple articles?
- Should archived/unpublished articles affect media visibility?
- Should article archive/unpublish detach media?
- Should deleted articles trigger media cleanup?
- Should media attached only to drafts be hidden from public composition?

V1 default:

- Media can exist independently as a media asset.
- Article publication visibility belongs to Content/Reading.
- Media does not mutate Content lifecycle truth.
- Public visibility must be enforced by Reading/Content composition.

---

## ADR-06: Idempotency-Key production policy

Questions:

- Should `Idempotency-Key` become required for `POST /items`?
- Should `Idempotency-Key` become required for `POST /articles/{articleId}/attachments`?
- What is the retention period for idempotency records?
- Is the key scoped by actor, client app, endpoint, and semantic request?
- How do we store request hash/result snapshot?
- What response is returned for same key + same request?
- What response is returned for same key + different request?

V1 default:

- Strongly recommended for register and attach.
- Same key + different semantic request returns `409 Conflict`.
- Malformed key returns `400 Bad Request`.

---

## ADR-07: Attachment-set versioning strategy

Questions:

- Where is `ArticleMediaSet.Version` stored?
  - on each `ArticleMedia` row?
  - separate per-article attachment state table?
  - derived from article media max version?
  - rowversion?
- Should attach/detach increment attachment-set version?
- Should soft delete media that clears primary increment affected article attachment-set versions?
- Should version be `int`, `bigint`, `rowversion`, or ULID-like revision?
- How does API return the current version to Admin UI?

V1 default:

- `expectedVersion` is required for reorder and set-primary.
- Attach/detach return the new version.
- Versioning must not rely only on `UpdatedAt`.

---

## ADR-08: Outbox event payload contract

Questions:

- What fields are mandatory in every Media event payload?
- Should events include `mediaPublicId` as well as internal `mediaId`?
- Should payload include URL/path?
- Should payload include metadata summary?
- Should payload include before/after values?
- Should `media.asset_soft_deleted` include affected article IDs?
- Should attach with `isPrimary=true` emit one event or two?
  - only `media.article_media_attached` with `primaryChanged=true`
  - or both `media.article_media_attached` and `media.article_primary_media_set`

V1 default:

- Use one attach event with `isPrimary` and `primaryChanged`.
- Payload must be sanitized.
- No binary content, signed URL secrets, or storage credentials.

---

## ADR-09: Audit event coverage

Questions:

- Which Media actions must be audited?
- Should register/update be governance-sensitive?
- Should read/list actions be audited?
- Should audit include before/after metadata summary?
- Should audit include affected article IDs when primary is cleared by media delete?
- What is the target audit ingestion lag SLO?

V1 default:

- Audit consumes all Media governance events.
- Audit dedupes by `MessageId`.
- Audit lag does not redefine Media truth.

---

## ADR-10: Reading integration strategy

Questions:

- Should Reading query Media truth directly during article composition?
- Should Reading use a projection/cache of article media?
- Which Media events should invalidate Reading caches?
- Should Reading consume:
  - `media.article_primary_media_set`
  - `media.article_media_reordered`
  - `media.article_media_detached`
  - `media.asset_soft_deleted`
  - `media.asset_restored`
- What is the fallback behavior when media is missing or stale?

V1 default:

- Public media composition belongs to Reading.
- Direct public Media endpoint is not implemented in V1.
- Reading must degrade gracefully.
- Reading does not need to consume Media events in the first async phase.

---

## ADR-11: SEO / social preview image strategy

Questions:

- Does `og:image` derive from primary media?
- Does SEO own explicit preview image independently?
- Should SEO consume `media.article_primary_media_set`?
- What happens if primary media is deleted?
- Should SEO fallback to article/category/default image?
- Should stale Media events update SEO metadata?

V1 default:

- SEO has no required V1 dependency on Media.
- SEO may consume selected Media events in later phases.
- Any SEO projection must be version-aware or rebuildable from truth.

---

## ADR-12: CDN/cache invalidation strategy

Questions:

- Should Media trigger CDN purge?
- Which operations require purge?
  - update metadata
  - delete
  - restore
  - replace binary object
- Should CDN purge be best-effort or durable tracked side effect?
- What is acceptable purge lag?
- What is the dead-letter/remediation policy?

V1 default:

- CDN/cache invalidation is not required for Media truth success.
- Any future CDN/cache consumer must be idempotent and observable.

---

## ADR-13: Virus scanning / safety scanning

Questions:

- Is scanning required before media can be public?
- Is scan status stored in Media?
- What states exist?
  - Pending
  - Clean
  - Suspicious
  - Rejected
  - Failed
- Does Reading hide unscanned media?
- How are scan timeouts handled?
- What is the retry/dead policy?

V1 default:

- Scan pipeline is V2+.
- V1 uses allowlist + metadata validation.
- Do not claim files are malware-safe merely because metadata was registered.

---

## ADR-14: Variant / thumbnail generation

Questions:

- Which variants are required?
- Are variants generated synchronously or asynchronously?
- Where is variant state stored?
- What is the idempotency key?
  - `(MediaId, VariantName)`
  - `(MediaId, Width, Height, Format)`
- How are stale variant jobs rejected?
- Can article rendering use base media if variants are missing?

V1 default:

- Variant generation is V2+.
- Missing variants must degrade gracefully.
- Variant output is derived, not Media relationship truth.

---

## ADR-15: Storage reconciliation and orphan cleanup

Questions:

- How do we detect storage objects without MediaAsset records?
- How do we detect MediaAsset records pointing to missing objects?
- Should cleanup be automatic or report-only first?
- What is the safe retention window for orphan objects?
- How do we prevent cleanup from deleting still-needed artifacts?
- What metrics/alerts are required?

V1 default:

- Orphan cleanup is an operational policy.
- Storage-vs-DB mismatch does not redefine Media truth.
- Cleanup must be bounded and rerun-safe.

---

## ADR-16: Cross-module FK strategy

Questions:

- Should `ArticleMedia.ArticleId` have a physical FK to Content Article in shared DB V1?
- If modules split databases later, how does integrity move to application contracts?
- Should Media validate article existence synchronously?
- What happens if Content article is deleted/archived?

V1 default:

- Media may validate `ArticleId` through approved read-only Content contract.
- Shared DB FK is optional.
- Physical reachability must not imply shared transactional ownership.

---

## ADR-17: Authorization and permission granularity

Questions:

- Are media actions covered by one permission or split by action?
- Suggested permissions:
  - `Media.Items.Read`
  - `Media.Items.Register`
  - `Media.Items.Update`
  - `Media.Items.Delete`
  - `Media.Items.Restore`
  - `Media.Attachments.Attach`
  - `Media.Attachments.Detach`
  - `Media.Attachments.Reorder`
  - `Media.Attachments.SetPrimary`
- Do Author/Editor roles have article-scoped media permissions?
- Can Moderator delete/restore media?
- Are there tenant/ownership constraints?

V1 default:

- All Admin Media endpoints require Bearer auth + explicit policies.
- Deny by default.

---

## ADR-18: Public Media endpoint strategy

Questions:

- Do we ever expose `GET /public/articles/{articleId}/media`?
- If yes, how do we enforce publication visibility?
- Should response include only public-safe metadata?
- Should deleted/unscanned media be omitted?
- Should this live in Media or Reading?

V1 default:

- Direct public Media endpoints are not implemented.
- Public article/media composition belongs to Reading.

---

## ADR-19: Media replacement strategy

Questions:

- Can a MediaAsset binary object be replaced?
- If yes, does URL/path change?
- Does replacement increment version?
- Does it invalidate variants/CDN?
- Does it emit `media.asset_updated` or a separate `media.asset_replaced`?
- How do we avoid stale CDN serving old binary?

V1 default:

- Binary replacement is out of scope.
- `PATCH /items/{mediaId}` updates safe metadata only.

---

## ADR-20: Multi-tenant / ownership expansion

Questions:

- Will Media be tenant-scoped?
- Are media assets private to tenant/user/article?
- Can media be reused across articles or tenants?
- Do event payloads need tenant ID?
- Do storage paths need tenant partitioning?
- Do cleanup jobs operate tenant-by-tenant?

V1 default:

- Keep schema and event payloads evolvable.
- Do not assume global reuse is always safe if tenant boundaries are introduced later.

---

## ADR-21: Observability and SLO thresholds

Questions:

- What are final P95/P99 targets after measurement?
- What outbox pending age should page operators?
- What audit ingestion lag is acceptable?
- What placeholder rate is acceptable?
- What orphan cleanup backlog is acceptable?
- What signals block release?

V1 default:

- Use initial SLO targets from `07-observability-slos.md`.
- Refine after real traffic measurement.

---

## ADR-22: Event consumer expansion order

Questions:

- After Audit, which consumer should be added next?
  - Reading cache invalidation
  - SEO preview image projection
  - CDN purge
  - scan pipeline
  - variant generation
- Which consumer requires version-aware projection first?
- Which consumer requires durable side-effect tracking?
- Which consumer has highest user/business value?

V1 default:

- Audit first.
- Reading/SEO/CDN/scan/variant later.
- Do not add consumers without idempotency/replay/rebuild policy.