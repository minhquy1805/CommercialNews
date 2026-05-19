# Media — Business Rules (V1)

This document defines the business rules for Media V1.

Media V1 focuses on:

- media metadata registration
- safe metadata update
- article-media attachment management
- deterministic ordering
- primary media selection
- soft delete / restore
- Outbox-based event emission
- Audit ingestion as the first async consumer

Media V1 does **not** make CDN, cache, scan, variants, or public Reading composition part of Media write success.

---

## 1) Ownership rules

### BR-MEDIA-001 — Media owns media metadata truth

Media owns `MediaAsset` metadata, including:

- media identity
- stable URL/path/reference
- media type
- safe metadata summary
- alt text
- active/deleted state
- version/revision marker where implemented

Media metadata truth is stored in Media-owned persistence.

---

### BR-MEDIA-002 — Media owns article-media attachment truth

Media owns `ArticleMedia` attachment truth, including:

- whether a media asset is attached to an article
- attachment order
- primary media selection
- attachment active/deleted state if applicable
- attachment-level caption/alt override if supported

Content owns article lifecycle truth. Reading owns public composition.

---

### BR-MEDIA-003 — Media does not own public visibility truth

Media must not decide whether an article is publicly visible.

Public visibility belongs to Content/Reading composition.

Media may provide attachment truth, but public rendering must still enforce article visibility rules outside Media.

---

### BR-MEDIA-004 — Storage/CDN is not relationship truth

Object storage, CDN, thumbnails, variants, and transformed media outputs do not define:

- attachment membership
- primary media selection
- attachment order
- delete/restore truth

They are delivery or derived layers.

---

## 2) Media asset rules

### BR-MEDIA-010 — Register media metadata after binary upload

`POST /items` registers media metadata after binary upload or storage placement.

This endpoint does not prove that binary upload was performed by this API.

On success:

- `MediaAsset` truth is committed
- Outbox event `media.asset_registered` is committed

---

### BR-MEDIA-011 — Register media must validate safe metadata

Media registration must validate:

- allowed media type
- safe URL/path/reference format
- safe metadata shape
- safe alt text
- request size limits
- policy-level size/type constraints

Client-supplied metadata must not be trusted blindly.

---

### BR-MEDIA-012 — Media type must be allowlisted

Only policy-approved media types are allowed.

Example V1 type categories:

- `Image`
- `Video`
- `File`

The exact allowed set is policy-defined.

Unsupported media type must be rejected.

---

### BR-MEDIA-013 — Metadata must be sanitized

Media metadata must not contain:

- raw HTML
- script content
- event-handler attributes
- storage credentials
- signed URL secrets
- internal filesystem paths
- unsafe nested structures
- unexpected fields unless policy allows them

Unsafe metadata must be rejected or sanitized according to policy.

---

### BR-MEDIA-014 — Safe metadata update only

`PATCH /items/{mediaId}` may update safe metadata only.

Allowed examples:

- `altText`
- safe metadata summary

Normally immutable unless policy explicitly allows:

- URL/path/reference
- storage identity
- binary object identity
- size
- type

On success, emit:

- `media.asset_updated`

---

### BR-MEDIA-015 — Deleted media cannot be updated by default

Updating a soft-deleted media asset should be rejected unless policy explicitly allows metadata correction on deleted records.

Recommended V1 behavior:

- return `409 Conflict` with `MEDIA.MEDIA_DELETED`

---

## 3) Attachment rules

### BR-MEDIA-020 — Cannot attach missing media

Media cannot attach a non-existent `MediaAsset` to an article.

If media does not exist:

- return `404 Not Found`

---

### BR-MEDIA-021 — Cannot attach deleted media

A deleted media asset cannot be newly attached to an article.

If media is deleted:

- return `409 Conflict`

---

### BR-MEDIA-022 — Article reference must be valid by policy

When attaching media to article, Media must validate `ArticleId` according to approved V1 policy.

Allowed approaches:

- read-only validation through Content contract
- application-level contract without cross-module mutation
- physical FK only if shared DB strategy allows it

Media must not mutate Content truth.

---

### BR-MEDIA-023 — Active attachment must be unique

There must not be duplicate active attachment for the same:

- `ArticleId`
- `MediaId`

Repeated attach must converge safely and must not create duplicate membership.

Recommended protection:

- unique constraint for active `(ArticleId, MediaId)`

---

### BR-MEDIA-024 — Attach may set primary in the same transaction

If attach request includes `isPrimary = true`, Media must:

- attach media
- unset previous primary if needed
- set the newly attached media as primary
- commit the change atomically
- emit `media.article_media_attached`

The event payload should include:

- `isPrimary = true`
- `primaryChanged = true` if primary changed

---

### BR-MEDIA-025 — Attach does not require expectedVersion in V1

Attach is protected by:

- active `(ArticleId, MediaId)` uniqueness
- optional/strongly recommended `Idempotency-Key`
- primary invariant if `isPrimary = true`

`expectedVersion` is not required for attach in V1.

---

## 4) Detach rules

### BR-MEDIA-030 — Detach removes active attachment

Detach removes or deactivates the media attachment from the article attachment set.

On success, emit:

- `media.article_media_detached`

---

### BR-MEDIA-031 — Repeated detach converges safely

If an attachment is already detached, repeated detach should converge safely.

Policy may choose:

- return `200 OK` with equivalent result
- or return `404` if strict delete semantics are required

Recommended V1 posture:

- convergent detach where practical

---

### BR-MEDIA-032 — Detaching primary clears primary

If the detached media is current primary:

- primary selection is cleared
- no fallback primary is selected automatically
- admin may explicitly set a new primary later

Event payload should include:

- `primaryCleared = true`

---

### BR-MEDIA-033 — Detach must not mutate Content lifecycle

Detaching media from an article must not:

- archive the article
- unpublish the article
- mutate article status
- mutate Content lifecycle truth

---

## 5) Primary media rules

### BR-MEDIA-040 — At most one active primary per article

For each article, there must be at most one active primary media.

Primary media may be none.

Recommended protection:

- transactional update
- filtered unique index where supported

Example:

```sql
UNIQUE (ArticleId)
WHERE IsPrimary = 1 AND IsDeleted = 0
```

---

### BR-MEDIA-041 — Primary media must be attached

A media asset cannot be primary for an article unless it is actively attached to that article.

If selected media is not attached:

- return `409 Conflict` or policy-defined error

Recommended code:

- `MEDIA.MEDIA_NOT_ATTACHED`

---

### BR-MEDIA-042 — Primary media must be active

Deleted media cannot be selected as primary.

If selected media is deleted:

- return `409 Conflict`
- code: `MEDIA.MEDIA_DELETED`

---

### BR-MEDIA-043 — Set-primary requires expectedVersion

`POST /articles/{articleId}/attachments:set-primary` requires `expectedVersion`.

Rules:

- missing `expectedVersion` returns `400 Bad Request`
- version mismatch returns `409 Conflict`
- passing version check does not bypass invariant validation

---

### BR-MEDIA-044 — Set-primary is atomic

Set-primary must atomically:

- unset old primary
- set selected media as new primary
- increment/update attachment-set version
- write Outbox event

On success, emit:

- `media.article_primary_media_set`

---

### BR-MEDIA-045 — Same primary with current version converges safely

If selected media is already primary and `expectedVersion` is current:

- operation should converge safely
- response may return `primarySet = true`
- no duplicate meaningful side effect should be emitted for unchanged state unless audit policy explicitly records attempted action

---

## 6) Ordering rules

### BR-MEDIA-050 — Attachment order is scoped by article

Attachment order is scoped by `ArticleId`.

There is no global ordering guarantee across all media operations.

---

### BR-MEDIA-051 — SortOrder must be deterministic

Attachment order must be deterministic and stable.

`SortOrder` must be non-negative.

---

### BR-MEDIA-052 — Reorder is a final-state set operation

Reorder must be treated as a final-state set operation, not a sequence of loose partial row updates.

The provided list must represent the intended final order.

---

### BR-MEDIA-053 — Reorder list must match active attachment set

For V1, reorder request must include the current active attachment set unless policy explicitly defines another behavior.

Invalid cases:

- missing attached media
- duplicate media IDs
- media not attached to the article
- deleted/inactive attachment included where policy forbids it

Invalid reorder list returns:

- `400 Bad Request`
- code: `MEDIA.INVALID_REORDER_LIST`

---

### BR-MEDIA-054 — Reorder requires expectedVersion

`POST /articles/{articleId}/attachments:reorder` requires `expectedVersion`.

Rules:

- missing `expectedVersion` returns `400 Bad Request`
- version mismatch returns `409 Conflict`
- stale reorder must not overwrite newer order truth

---

### BR-MEDIA-055 — Reorder must be atomic

Partial reorder application is not acceptable.

Media must not expose a partially applied order as final truth.

On success, emit:

- `media.article_media_reordered`

---

## 7) Delete and restore rules

### BR-MEDIA-060 — Delete is soft delete in V1

Media delete is soft delete in V1.

Hard delete/purge API is out of scope.

On success, emit:

- `media.asset_soft_deleted`

---

### BR-MEDIA-061 — Repeated delete converges safely

Repeated delete should converge safely by policy.

If already deleted:

- return equivalent success
- or return documented conflict if stricter policy is chosen

Recommended V1 posture:

- repeated delete converges safely

---

### BR-MEDIA-062 — Deleted media cannot remain active primary

If deleted media is active primary in any article attachment:

- primary selection is cleared in the same Media truth transaction where feasible
- system does not automatically select fallback primary

Event payload may include:

- `primarySelectionsCleared = true`
- affected article IDs where safe

---

### BR-MEDIA-063 — Deleted media must not break article reads

Deleted or missing media must not cause article detail/list rendering to fail systematically.

Reading must degrade gracefully through:

- placeholder
- omission
- fallback presentation

---

### BR-MEDIA-064 — Restore obeys retention policy

Restore is allowed only within retention/legal policy.

If restore window expired:

- return `409 Conflict`
- code: `MEDIA.RESTORE_WINDOW_EXPIRED`

---

### BR-MEDIA-065 — Restore does not automatically set primary

Restoring media does not automatically:

- reattach media
- reselect primary
- rebuild public visibility
- make derived artifacts authoritative

On success, emit:

- `media.asset_restored`

---

## 8) Idempotency and retry rules

### BR-MEDIA-070 — Timeout does not prove failure

Timeout during Media operation does not prove:

- no DB mutation happened
- no Outbox record was written
- no downstream side effect happened
- operation failed

Clients and operators must reconcile from Media truth.

---

### BR-MEDIA-071 — Register should use Idempotency-Key

`POST /items` should use `Idempotency-Key`.

V1 posture:

- strongly recommended
- may become required for production clients

Same key + same semantic request should return original/equivalent result.

Same key + different semantic request must return:

- `409 Conflict`

Malformed key returns:

- `400 Bad Request`

---

### BR-MEDIA-072 — Attach should use Idempotency-Key

`POST /articles/{articleId}/attachments` should use `Idempotency-Key`.

Business guard:

- active `(ArticleId, MediaId)` uniqueness

Same key + different semantic attach intent must return:

- `409 Conflict`

---

### BR-MEDIA-073 — MessageId dedupe is mandatory for consumers

Media events are delivered at-least-once.

Consumers must dedupe by:

- `MessageId`

Audit must not append duplicate audit evidence for the same `MessageId`.

---

### BR-MEDIA-074 — Message-level dedupe is not always enough

Where harmful duplicate business effects are possible, consumers must also use business-level idempotency.

Examples:

- duplicate media registration intent
- duplicate attachment intent
- stale projection overwrite
- duplicate external side effects in future CDN/variant workflows

---

### BR-MEDIA-075 — Same-intent replay must be controlled

Different `MessageId`s may represent the same effective business intent.

Same-intent replay must be protected by:

- `Idempotency-Key`
- business uniqueness constraint
- `expectedVersion`
- current state convergence
- or truth reconciliation

---

## 9) Outbox and async rules

### BR-MEDIA-080 — Media writes Outbox in the same transaction

For commands that emit integration events, Media must commit atomically:

- Media truth change
- required version/revision update
- Outbox message

---

### BR-MEDIA-081 — Broker publish is post-commit

RabbitMQ publish happens after Media truth commit.

Media write success must not wait for:

- broker publish
- Audit ingestion
- CDN purge
- cache invalidation
- variant generation
- scan pipeline

---

### BR-MEDIA-082 — Outbox Published does not mean consumed

Outbox `Published` means broker handoff succeeded.

It does not mean:

- Audit consumed the event
- Audit evidence is queryable
- Reading/SEO/CDN/variant consumers completed
- downstream projections are caught up

---

### BR-MEDIA-083 — Audit is required V1 consumer

In V1, Audit consumes Media events for governance evidence.

Audit must:

- dedupe by `MessageId`
- append evidence durably
- ack only after durable append or durable dedupe decision
- retry or DLQ according to consumer policy

---

### BR-MEDIA-084 — Audit lag does not redefine Media truth

Media truth remains authoritative even when Audit is delayed, retrying, or temporarily unavailable.

A successful Media write must not be retroactively failed because Audit is lagging.

---

## 10) Security and abuse rules

### BR-MEDIA-090 — All Admin Media endpoints require explicit policies

All endpoints under `/api/v1/admin/media/*` require:

- Bearer authentication
- explicit authorization policy
- deny-by-default behavior

---

### BR-MEDIA-091 — Media authorization must be server-side

Frontend hiding buttons is not authorization.

Server must enforce all Media permissions.

---

### BR-MEDIA-092 — User-controlled paths and metadata are untrusted

Client-supplied fields such as URL/path, metadata, alt text, caption, and type are untrusted.

They must be validated and sanitized.

---

### BR-MEDIA-093 — Do not store secrets in Media metadata

Media metadata and event payloads must not contain:

- storage credentials
- signed URL secrets
- access tokens
- provider secrets
- internal paths that reveal infrastructure
- raw binary payloads

---

### BR-MEDIA-094 — Rate limits and quotas protect Media abuse surface

Media operations should be protected by:

- request body limits
- register/upload rate limits
- metadata size limits
- max attachment count per article
- reorder list size limits
- alerting for abnormal media activity

---

## 11) Logging and audit rules

### BR-MEDIA-100 — Logs must be safe and useful

Logs should include:

- `actorUserId`
- `mediaId`
- `articleId`
- `action`
- `eventType`
- `MessageId`
- `correlationId`
- sanitized error code
- outcome

Logs must not include:

- raw binary payload
- signed URL secrets
- storage credentials
- unsafe metadata
- raw SQL errors
- stack traces in client responses

---

### BR-MEDIA-101 — Governance actions emit events

The following actions must emit Media integration events:

- register media metadata
- update media metadata
- attach media
- detach media
- reorder media
- set primary media
- soft delete media
- restore media

---

### BR-MEDIA-102 — Audit evidence must identify actor and target

Audit evidence should identify:

- actor
- action
- media identity
- article identity where applicable
- event/message identity
- correlation ID
- timestamp
- important before/after summary where safe

---

## 12) Public rendering rules

### BR-MEDIA-110 — Direct public Media endpoints are out of scope in V1

V1 does not expose direct public Media endpoints.

Public media composition belongs to Reading.

---

### BR-MEDIA-111 — Reading must enforce public visibility

Reading must enforce:

- article publication visibility
- safe media rendering
- deleted media exclusion
- fallback/placeholder behavior

---

### BR-MEDIA-112 — Missing media must degrade gracefully

If media delivery, CDN, thumbnail, or variant is missing/stale:

- article rendering should continue where policy allows
- placeholder/omission is preferred over article failure
- stale derived media must not override fresher Media truth

---

## 13) Cleanup and reconciliation rules

### BR-MEDIA-120 — Orphan cleanup is derived operational workflow

Orphan cleanup may detect:

- storage object without `MediaAsset`
- `MediaAsset` pointing to missing object
- expired soft-deleted artifact
- stale derived output

Cleanup does not redefine Media truth.

---

### BR-MEDIA-121 — Cleanup must be bounded and rerun-safe

Cleanup/reconciliation jobs must be:

- bounded
- observable
- rerun-safe
- subordinate to Media truth

---

### BR-MEDIA-122 — Safe non-progress beats unsafe cleanup

If cleanup cannot prove an artifact is safe to delete or repair:

- do not delete
- do not overwrite
- report/defer/operator review

Unsafe cleanup is worse than temporary orphan retention.

---

## 14) Future consumer rules

### BR-MEDIA-130 — Reading consumer is future phase

Reading may later consume selected Media events for cache invalidation or read projection.

Future Reading consumer must:

- dedupe by `MessageId`
- use `AggregateId + Version` where ordering matters
- fallback to truth where needed
- never expose draft/unpublished article media due to stale event

---

### BR-MEDIA-131 — SEO consumer is future phase

SEO may later consume selected Media events if social preview image derives from primary media.

Future SEO consumer must:

- dedupe by `MessageId`
- use version-aware apply
- fallback to explicit SEO/default image policy
- avoid stale event overwriting newer SEO state

---

### BR-MEDIA-132 — CDN/scan/variant consumers are future phases

Future CDN, scan, or variant consumers must:

- be idempotent
- use durable side-effect tracking where harmful
- handle timeout ambiguity
- use retry/backoff/dead-state policy
- avoid stale output overwriting fresher derived state

---

## 15) Observability rules

### BR-MEDIA-140 — Media must expose truth-path signals

At minimum, observe:

- register/update success/failure/latency
- attach/detach success/failure/latency
- reorder/set-primary success/failure/latency
- delete/restore success/failure/latency
- validation failures
- version conflicts
- idempotency conflicts
- duplicate attach prevention
- primary invariant prevention

---

### BR-MEDIA-141 — Media async path must be observable

At minimum, observe:

- Media Outbox pending count
- Media Outbox oldest pending age
- Media publish success/failure/retry count
- dead Media outbox messages
- Audit ingestion success/failure
- Audit lag
- Audit dedupe hits by `MessageId`

---

### BR-MEDIA-142 — Read-path degradation must be observable

At minimum, observe:

- placeholder rate
- missing media rate
- degraded-but-successful read rate
- article read failures caused by media delivery
- truth valid but derivative missing rate

---

## 16) Summary

Media V1 business rules can be summarized as:

- Media owns media metadata and article-media relationship truth.
- Content owns article lifecycle and public visibility truth.
- Reading owns public article/media composition.
- Storage/CDN/variants are delivery or derived layers, not relationship truth.
- Attachment membership must be unique.
- At most one active primary media is allowed per article.
- Reorder is an atomic final-state set operation.
- Delete is soft delete in V1.
- Delete/detach primary clears primary and does not auto-select fallback.
- Restore does not automatically reattach or reselect primary.
- Register/attach should use `Idempotency-Key`.
- Reorder/set-primary require `expectedVersion`.
- Media writes Outbox messages in the same transaction as truth changes.
- Audit consumes Media events first in V1.
- Downstream lag does not redefine Media truth.
