# Media — Domain Contracts (V1)

## 1) Ownership

Media is the source of truth for media metadata and article-media attachment state.

Media owns:

* media asset metadata
* media asset active/deleted state
* attachment membership between article and media
* attachment ordering
* primary media selection
* attachment-level caption/alt override if supported
* media delete/restore policy
* Media integration events emitted through Outbox

Media does not own:

* article lifecycle and publication visibility; Content owns article truth
* public article composition; Reading owns public read composition
* SEO metadata; SEO owns SEO state
* audit log storage; Audit owns audit ingestion and audit truth
* CDN/cache/variant generation/scanning; these are downstream derived workflows
* binary storage truth beyond storing stable URL/path metadata

Public consumption of media should normally happen through Reading composition, not direct public Media endpoints.

---

## 2) Core entities

### 2.1 MediaAsset

Represents reusable media metadata.

**Fields**

* `MediaId`
* `PublicId` or external-safe identifier
* `Url` or storage path
* `Type`: `Image | Video | File`
* `AltText?`
* `MetadataJson?`
* `Status`: `Active | Deleted`
* `CreatedByUserId`
* `CreatedAtUtc`
* `UpdatedAtUtc?`
* `DeletedByUserId?`
* `DeletedAtUtc?`
* `RestoredAtUtc?`
* `Version`

**Rules**

* `Url`/storage path must be stable after registration unless explicitly allowed by policy.
* `Type`, size, and metadata must be validated by policy.
* Metadata must be sanitized.
* Deleted media must not be rendered as active primary media.
* Soft delete is reversible within retention policy.
* Hard delete/purge is out of scope for V1 API behavior.

---

### 2.2 ArticleMedia

Represents the attachment of a media asset to an article.

**Fields**

* `ArticleMediaId`
* `ArticleId`
* `MediaId`
* `SortOrder`
* `IsPrimary`
* `AltTextOverride?`
* `Caption?`
* `AttachedByUserId`
* `AttachedAtUtc`
* `UpdatedAtUtc?`
* `DetachedAtUtc?`
* `IsDeleted`
* `Version`

**Rules**

* Media owns the attachment row.
* Content owns the referenced article.
* An article may have zero media attachments.
* A media asset may be attached to many articles.
* Attachment membership is unique by `(ArticleId, MediaId)` for active attachments.
* Primary selection is scoped by `ArticleId`.
* Ordering is scoped by `ArticleId`.
* Attachment caption/alt override must be sanitized.

---

## 3) Aggregate boundaries

### 3.1 MediaAsset aggregate

**Aggregate identity**

* `MediaId`

**Responsible for**

* registration
* metadata update
* soft delete
* restore

**Versioning**

* `MediaAsset.Version` increments when asset metadata or active/deleted state changes.

**Events**

* `media.asset_registered`
* `media.asset_updated`
* `media.asset_soft_deleted`
* `media.asset_restored`

---

### 3.2 ArticleMediaSet aggregate

**Aggregate identity**

* `ArticleId`

This is the logical aggregate for attachment operations on an article.

**Responsible for**

* attach media
* detach media
* reorder attachments
* set primary media
* clear primary when detach/delete policy requires it

**Versioning**

* A current attachment-set version must be available for concurrency-sensitive operations.
* `expectedVersion` is required for reorder and set-primary.
* Attach/detach should return the new version.
* Reorder and set-primary must reject stale versions with `409 Conflict`.

**Events**

* `media.article_media_attached`
* `media.article_media_detached`
* `media.article_media_reordered`
* `media.article_primary_media_set`

---

## 4) Invariants and policies

### 4.1 Attachment integrity

**Rules**

* Cannot attach a non-existent media asset.
* Cannot attach a deleted media asset.
* Cannot create duplicate active attachment for `(ArticleId, MediaId)`.
* Cannot set a deleted media asset as primary.
* Cannot set primary to a media asset that is not attached to the article.
* Detaching a non-existing attachment should converge safely by policy.

**Recommended DB constraints**

* Unique active attachment by `(ArticleId, MediaId)`.
* FK or application-level contract to ensure `MediaId` exists.
* Cross-module FK to Content Article is optional in shared DB V1, but must not imply Content ownership.

---

### 4.2 Primary media rule

**Rules**

* At most one active primary media per article.
* Primary may be none.
* Setting primary must be atomic.
* If attaching with `isPrimary = true`, attach and primary selection occur in the same Media truth transaction.
* V1 does not automatically select fallback primary media.
* If the current primary is detached, primary is cleared.
* If soft-deleting a media asset affects active primary selections, those primary selections should be cleared in the same Media truth transaction where feasible.

**Recommended DB constraint**

Filtered unique index for one active primary per article.

**Example**

```sql
CREATE UNIQUE INDEX UX_ArticleMedia_OnePrimary
ON ArticleMedia (ArticleId)
WHERE IsPrimary = 1 AND IsDeleted = 0;
```

---

### 4.3 Ordering semantics

**Rules**

* `SortOrder >= 0`.
* Attachment ordering is scoped by `ArticleId`.
* Reorder is a final-state set operation.
* The provided reorder list must match the current active attachment set unless a specific policy says otherwise.
* Partial reorder application is not allowed.
* Reorder must be performed atomically.
* Retrying the same reorder command should converge to the same final truth.

---

### 4.4 Delete and restore policy

**Rules**

* `MediaAsset` delete is soft delete in V1.
* Restore is allowed only within retention/legal policy.
* Deleted media must not remain active primary if policy forbids it.
* Restore must not silently violate current primary/order invariants.
* Restore does not automatically reselect the media as primary.
* Hard delete/purge is out of scope for V1 API behavior and belongs to retention/purge operations.

---

### 4.5 Safety and abuse policy

**Rules**

* Allowed media types must be enforced.
* Size constraints must be enforced.
* Metadata must be sanitized.
* Alt text and captions must not allow unsafe HTML/script injection.
* Binary upload mechanism is implementation-specific.
* If storage upload succeeds but DB registration fails, orphan cleanup/reconciliation is required.
* If DB metadata exists but storage object is missing, Reading must degrade gracefully.

---

## 5) Command contracts

### 5.1 Register media asset

**Command**

`RegisterMediaAsset`

**Input**

* `Url`
* `Type`
* `AltText?`
* `MetadataJson?`
* `IdempotencyKey?`
* `ActorUserId`
* `CorrelationId?`

**Rules**

* Registers metadata after binary upload.
* Does not prove binary upload was performed by this API.
* Validates type, metadata, and safety policy.
* Reusing the same `Idempotency-Key` with the same semantic request should converge safely.
* On success, emit `media.asset_registered`.

**Output**

* `MediaId`
* `Url`
* `Type`
* `Version`

---

### 5.2 Update media asset metadata

**Command**

`UpdateMediaAsset`

**Input**

* `MediaId`
* `AltText?`
* `MetadataJson?`
* `ActorUserId`
* `CorrelationId?`

**Rules**

* Only safe metadata fields may be updated.
* `Url`, storage path, size, and type should not be changed unless explicitly allowed.
* Repeated update with same final state should converge safely.
* On success, emit `media.asset_updated`.

**Output**

* `Updated`
* `Version`

---

### 5.3 Soft delete media asset

**Command**

`SoftDeleteMediaAsset`

**Input**

* `MediaId`
* `ActorUserId`
* `Reason?`
* `CorrelationId?`

**Rules**

* Soft delete is truth-first.
* Repeated delete should converge safely.
* If deleted media is active primary, clear primary where feasible.
* Does not automatically select fallback primary.
* On success, emit `media.asset_soft_deleted`.

**Output**

* `Deleted`
* `Version`
* `AffectedArticleIds?`
* `PrimaryClearedArticleIds?`

---

### 5.4 Restore media asset

**Command**

`RestoreMediaAsset`

**Input**

* `MediaId`
* `ActorUserId`
* `CorrelationId?`

**Rules**

* Restore must obey retention policy.
* Restore does not automatically reattach media.
* Restore does not automatically set primary.
* Repeated restore should converge safely where policy allows.
* On success, emit `media.asset_restored`.

**Output**

* `Restored`
* `Version`

---

### 5.5 Attach media to article

**Command**

`AttachArticleMedia`

**Input**

* `ArticleId`
* `MediaId`
* `IsPrimary`
* `IdempotencyKey?`
* `ActorUserId`
* `CorrelationId?`

**Rules**

* Article must be valid by application contract.
* Media asset must exist and be active.
* Duplicate active attachment must not be created.
* If `IsPrimary = true`, attach and primary selection must happen atomically.
* Repeated attach must not create duplicate membership or duplicate meaningful side effects.
* On success, emit `media.article_media_attached`.

**Output**

* `Attached`
* `Version`
* `PrimaryChanged`

---

### 5.6 Detach media from article

**Command**

`DetachArticleMedia`

**Input**

* `ArticleId`
* `MediaId`
* `ActorUserId`
* `CorrelationId?`

**Rules**

* Detach should converge safely if already detached.
* If detached media is current primary, primary selection is cleared.
* V1 does not automatically select fallback primary.
* On success, emit `media.article_media_detached`.

**Output**

* `Detached`
* `Version`
* `PrimaryCleared`

---

### 5.7 Reorder article media

**Command**

`ReorderArticleMedia`

**Input**

* `ArticleId`
* `ExpectedVersion`
* `MediaIds[]`
* `ActorUserId`
* `CorrelationId?`

**Rules**

* `ExpectedVersion` is required.
* Missing `ExpectedVersion` returns `400 Bad Request`.
* Version mismatch returns `409 Conflict`.
* Provided list must match current active attachment set.
* Reorder is atomic.
* Partial reorder is not allowed.
* On success, emit `media.article_media_reordered`.

**Output**

* `Reordered`
* `Version`

---

### 5.8 Set primary article media

**Command**

`SetPrimaryArticleMedia`

**Input**

* `ArticleId`
* `MediaId`
* `ExpectedVersion`
* `ActorUserId`
* `CorrelationId?`

**Rules**

* `ExpectedVersion` is required.
* Missing `ExpectedVersion` returns `400 Bad Request`.
* Version mismatch returns `409 Conflict`.
* Media must be attached to the article.
* Media must be active.
* If already primary, operation converges safely.
* Primary invariant must be enforced atomically.
* On success, emit `media.article_primary_media_set`.

**Output**

* `PrimarySet`
* `Version`

---

## 6) Integration events

Media emits integration events through the system Outbox.

**Rules**

* Event is committed in the same local transaction as Media truth change.
* Event delivery is at-least-once.
* Consumers must be idempotent.
* Audit consumes Media events in V1.
* Reading, SEO, CDN, scan, and variant workflows may consume selected Media events in later phases.
* Consumer lag must not redefine Media truth.

### 6.1 Event identity

All important Media integration events must carry:

* `MessageId`
* `EventType`
* `AggregateType`
* `AggregateId`
* `Version`
* `OccurredAtUtc`
* `CorrelationId`
* `InitiatorUserId`

Ordering-sensitive events must include a meaningful aggregate version.

### 6.2 Event names

**Media item events**

* `media.asset_registered`
* `media.asset_updated`
* `media.asset_soft_deleted`
* `media.asset_restored`

**Article attachment events**

* `media.article_media_attached`
* `media.article_media_detached`
* `media.article_media_reordered`
* `media.article_primary_media_set`

### 6.3 Event payload principles

Payloads must:

* be sanitized
* avoid binary content
* avoid secret storage credentials
* avoid long-lived signed URLs
* include enough identifiers for Audit and investigation
* include enough version/context for future derived consumers
* avoid copying full unrelated article/content state

Payloads may include:

* `mediaId`
* `mediaPublicId`
* `articleId`
* `url`
* `type`
* `altText`
* `metadataSummary`
* `version`
* `actorUserId`
* `primaryChanged`
* `primaryCleared`
* `affectedArticleIds`
* `items[]` for reorder final state

---

## 7) Event payload contracts

### 7.1 `media.asset_registered`

**Aggregate**

* `AggregateType`: `MediaAsset`
* `AggregateId`: `MediaId`
* `Version`: `MediaAsset.Version`

**Payload**

```json
{
  "mediaId": "string",
  "mediaPublicId": "string",
  "url": "/media/abc.jpg",
  "type": "Image",
  "altText": "optional",
  "metadataSummary": {
    "width": 1200,
    "height": 630
  },
  "registeredByUserId": "string",
  "registeredAtUtc": "2026-05-18T00:00:00Z"
}
```

### 7.2 `media.asset_updated`

**Aggregate**

* `AggregateType`: `MediaAsset`
* `AggregateId`: `MediaId`
* `Version`: `MediaAsset.Version`

**Payload**

```json
{
  "mediaId": "string",
  "mediaPublicId": "string",
  "altText": "Updated alt text",
  "metadataSummary": {
    "width": 1200,
    "height": 630
  },
  "updatedByUserId": "string",
  "updatedAtUtc": "2026-05-18T00:00:00Z"
}
```

### 7.3 `media.asset_soft_deleted`

**Aggregate**

* `AggregateType`: `MediaAsset`
* `AggregateId`: `MediaId`
* `Version`: `MediaAsset.Version`

**Payload**

```json
{
  "mediaId": "string",
  "mediaPublicId": "string",
  "deletedByUserId": "string",
  "deletedAtUtc": "2026-05-18T00:00:00Z",
  "primarySelectionsCleared": true,
  "affectedArticleIds": ["article-id-1", "article-id-2"]
}
```

### 7.4 `media.asset_restored`

**Aggregate**

* `AggregateType`: `MediaAsset`
* `AggregateId`: `MediaId`
* `Version`: `MediaAsset.Version`

**Payload**

```json
{
  "mediaId": "string",
  "mediaPublicId": "string",
  "restoredByUserId": "string",
  "restoredAtUtc": "2026-05-18T00:00:00Z"
}
```

### 7.5 `media.article_media_attached`

**Aggregate**

* `AggregateType`: `ArticleMediaSet`
* `AggregateId`: `ArticleId`
* `Version`: `ArticleMediaSet.Version`

**Payload**

```json
{
  "articleId": "string",
  "mediaId": "string",
  "mediaPublicId": "string",
  "isPrimary": true,
  "primaryChanged": true,
  "sortOrder": 0,
  "attachedByUserId": "string",
  "attachedAtUtc": "2026-05-18T00:00:00Z"
}
```

### 7.6 `media.article_media_detached`

**Aggregate**

* `AggregateType`: `ArticleMediaSet`
* `AggregateId`: `ArticleId`
* `Version`: `ArticleMediaSet.Version`

**Payload**

```json
{
  "articleId": "string",
  "mediaId": "string",
  "mediaPublicId": "string",
  "primaryCleared": true,
  "detachedByUserId": "string",
  "detachedAtUtc": "2026-05-18T00:00:00Z"
}
```

### 7.7 `media.article_media_reordered`

**Aggregate**

* `AggregateType`: `ArticleMediaSet`
* `AggregateId`: `ArticleId`
* `Version`: `ArticleMediaSet.Version`

**Payload**

```json
{
  "articleId": "string",
  "items": [
    {
      "mediaId": "string",
      "mediaPublicId": "string",
      "sortOrder": 0
    }
  ],
  "reorderedByUserId": "string",
  "reorderedAtUtc": "2026-05-18T00:00:00Z"
}
```

### 7.8 `media.article_primary_media_set`

**Aggregate**

* `AggregateType`: `ArticleMediaSet`
* `AggregateId`: `ArticleId`
* `Version`: `ArticleMediaSet.Version`

**Payload**

```json
{
  "articleId": "string",
  "mediaId": "string",
  "mediaPublicId": "string",
  "setByUserId": "string",
  "setAtUtc": "2026-05-18T00:00:00Z"
}
```

---

## 8) Idempotency and duplicate protection

### 8.1 Message-level idempotency

Consumers must dedupe by:

* `MessageId`

Audit must not append duplicate investigation records for the same `MessageId`.

### 8.2 Business-level idempotency

Business-level protections:

* Register media: `Idempotency-Key` strongly recommended.
* Attach media: active `(ArticleId, MediaId)` uniqueness.
* Detach media: repeated detach converges safely.
* Reorder: final-state operation with `ExpectedVersion`.
* Set primary: final-state operation with `ExpectedVersion`.
* Soft delete: repeated delete converges safely.
* Restore: repeated restore converges safely where policy allows.

---

## 9) Ordering, stale events, and replay

**Rules**

* Media events are delivered at-least-once.
* Consumers must not rely on RabbitMQ arrival order.
* Ordering-sensitive consumers must use `AggregateId + Version`.
* Duplicate version should be ignored.
* Older version should be ignored or trigger resync depending on consumer role.
* Version gap should trigger resync/rebuild if exact projection correctness matters.
* Replay must not corrupt Audit, Reading, SEO, or future projection state.

**V1 posture**

* Audit is append-only and dedupes by `MessageId`.
* Reading/SEO projections are not required consumers in V1.
* Future projection consumers must be version-aware.

---

## 10) Recovery and reconciliation

**Recovery posture**

* Media truth is authoritative for attachment membership, ordering, primary selection, and asset state.
* Audit can recover by replaying Media events with `MessageId` dedupe.
* Reading can rebuild media presentation from Media + Content truth in future phases.
* SEO can rebuild image-related metadata from Media + Content truth in future phases.
* Orphan storage cleanup must reconcile storage objects against `MediaAsset` truth.
* Missing storage objects must degrade gracefully in public reads.

---

## 11) Out of scope

Out of scope for Media V1 domain contracts:

* virus scanning workflow
* image/video variant pipeline
* CDN purge workflow
* direct public Media API
* polymorphic attachments beyond articles
* hard delete/purge API
* deduplication by content hash
* generalized media ownership model
