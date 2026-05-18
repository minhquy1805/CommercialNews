# Media — Security & Abuse Controls (V1)

## 1) Admin-only enforcement

Media V1 is primarily admin-managed.

All endpoints under `/api/v1/admin/media/*` require:

- Bearer authentication
- explicit authorization policies
- deny-by-default behavior

Recommended policy split:

- `Media.Items.Read`
- `Media.Items.Register`
- `Media.Items.Update`
- `Media.Items.Delete`
- `Media.Items.Restore`
- `Media.Attachments.Attach`
- `Media.Attachments.Detach`
- `Media.Attachments.Reorder`
- `Media.Attachments.SetPrimary`

Rules:

- Public users must not call Admin Media endpoints.
- Direct public Media endpoints are not implemented in V1.
- Public consumption should happen through Reading composition to preserve publication visibility checks.
- Media authorization must not rely only on frontend hiding buttons.

---

## 2) Cross-module visibility boundaries

Media owns media metadata and attachment truth.

Media does not own:

- article publication visibility
- public article access policy
- SEO metadata
- Reading composition rules

Rules:

- Media Admin APIs may validate `ArticleId` through an approved read-only Content contract.
- Media must not expose draft/unpublished article media through direct public endpoints in V1.
- Reading must enforce public visibility when composing media for public article reads.
- Missing or deleted media must degrade gracefully instead of leaking internal state.

---

## 3) Abuse surface controls

Media has a large abuse surface because it references uploaded files and user-controlled metadata.

Mandatory controls:

- enforce allowed media types
- enforce size limits
- enforce metadata field allowlist
- enforce request body limits at API and edge/gateway
- enforce URL/path format policy
- reject dangerous or unexpected fields
- rate limit register/attach/reorder/set-primary/delete/restore operations
- monitor repeated failed validation attempts

Allowed media types should be policy-defined.

Example V1 posture:

- `Image`
- `Video`
- `File`

Rules:

- Do not trust client-supplied MIME/type blindly.
- Do not trust client-supplied width/height/metadata blindly.
- Do not treat file extension as sufficient proof of file safety.
- Malware scanning and deep content inspection may be V2+, but V1 must not pretend metadata validation equals malware safety.

---

## 4) Metadata, alt text, and caption sanitization

Client-supplied metadata must be treated as untrusted.

Rules:

- sanitize `metadata`
- sanitize `altText`
- sanitize attachment captions/alt overrides if supported
- reject raw HTML/script by default
- reject oversized metadata
- reject deeply nested metadata
- reject unknown fields unless explicitly allowed
- avoid storing EXIF or sensitive metadata unless policy allows it

Do not allow metadata to contain:

- raw HTML
- JavaScript
- event-handler attributes
- storage credentials
- signed URL tokens
- internal filesystem paths
- private bucket names if they reveal infrastructure
- user secrets or access tokens

Recommended policy:

- Store only safe metadata summary needed for rendering/investigation, such as width, height, duration, or file size.
- Strip or ignore sensitive EXIF-style metadata in V1 unless explicitly required.

---

## 5) Storage URL and path safety

Media V1 stores URLs/paths as metadata. These values must be controlled.

Rules:

- prefer stable relative paths or storage object IDs over arbitrary external URLs
- do not accept arbitrary remote URLs unless explicitly allowed by policy
- do not store long-lived signed URLs as canonical media URL
- do not expose storage provider credentials
- do not expose internal filesystem paths
- validate URL/path normalization
- prevent path traversal patterns such as `../`
- prevent control characters or encoded traversal variants
- prevent scheme abuse such as `javascript:` or unsafe `data:` URLs where applicable

Recommended V1 posture:

- Store canonical object path or relative media path.
- Generate temporary/signed delivery URLs outside core Media truth where needed.
- Keep secrets out of MediaAsset metadata and event payloads.

---

## 6) Idempotency and replay abuse controls

Some Media commands are vulnerable to duplicate intent after timeout or malicious retry.

High-risk commands:

- `POST /items`
- `POST /articles/{articleId}/attachments`

Rules:

- `Idempotency-Key` is strongly recommended for register and attach.
- Malformed idempotency keys must be rejected.
- Reusing the same key with a different semantic request must return conflict.
- Idempotency records must be scoped to actor/client and command intent.
- Idempotency keys must not be used as authorization bypass.
- Repeated attach must not create duplicate active membership.
- Replayed events must not produce duplicate audit records.

Business-level protections:

- active `(ArticleId, MediaId)` uniqueness
- one active primary per article
- `expectedVersion` for reorder and set-primary
- durable consumer dedupe by `MessageId`

---

## 7) Concurrency and invariant protection

Security also includes preventing unsafe state transitions under concurrent admin actions.

Rules:

- primary selection must be enforced atomically
- at most one active primary media per article
- reorder must be atomic
- partial reorder must not be observable as final truth
- set-primary and reorder require `expectedVersion`
- stale version must return `409 Conflict`
- deleted media must not become active primary
- deleted media must not be newly attached
- restore must not silently violate current primary/order invariants

Recommended safeguards:

- DB unique constraint for active `(ArticleId, MediaId)`
- filtered unique index for one active primary per article
- transaction around set-primary/reorder
- optimistic concurrency for attachment set operations

---

## 8) Rate limits, quotas, and abuse monitoring

Media operations can be abused for storage growth, DB growth, or admin workflow disruption.

Recommended controls:

- per-user/admin rate limits for register operations
- per-user/admin rate limits for attach/detach/reorder/set-primary
- request size limit at API gateway/reverse proxy
- metadata size limit
- max attachment count per article
- max reorder list size
- quota or operational alerting for abnormal media registration volume
- alert on repeated validation failures
- alert on unusual delete/restore spikes
- alert on repeated primary/reorder conflicts

Recommended monitoring signals:

- media register attempts
- validation failure rate
- type-not-allowed rate
- metadata-unsafe rejection rate
- storage unavailable rate
- idempotency conflict rate
- reorder/set-primary version conflict rate
- attachment duplicate attempts
- soft-delete/restore spikes
- orphan cleanup backlog

---

## 9) Safe logging

Do not log:

- raw binary payloads
- full request body when metadata may contain unsafe/sensitive data
- signed URLs
- storage credentials
- provider secrets
- internal filesystem paths
- raw SQL errors
- stack traces in client responses
- full external URLs if they include tokens

Prefer logging:

- `actorUserId`
- `mediaId`
- `mediaPublicId`
- `articleId`
- `action`
- `eventType`
- `messageId`
- `correlationId`
- `requestId`
- sanitized error code
- sanitized storage/provider error class
- version conflict decisions
- idempotency decisions

Rules:

- Logs must support investigation without leaking sensitive storage or user-controlled content.
- Event payloads must be sanitized before being written to Outbox.
- Audit payloads should contain enough context for governance, not raw binary or unsafe metadata.

---

## 10) Governance actions auditing

The following actions are governance-sensitive and must emit Media integration events through Outbox:

- register media metadata
- update media metadata
- attach media to article
- detach media from article
- reorder article media
- set primary media
- soft delete media
- restore media

V1 audit posture:

- Media emits events through Outbox.
- Audit consumes Media events asynchronously.
- Audit dedupes by `MessageId`.
- Audit lag does not redefine Media truth.
- Consumer failure must not retroactively fail a committed Media write.

Audit records should capture:

- actor
- action
- media identity
- article identity when applicable
- before/after summary where appropriate
- correlation ID
- event/message ID
- timestamp

---

## 11) Public rendering safety

Public rendering normally belongs to Reading, not direct Media endpoints.

Rules:

- public article visibility must be checked by Reading/Content truth
- media attached to draft/unpublished articles must not leak through public endpoints
- deleted media must not render as active primary
- missing media must render placeholder/omission where policy allows
- stale CDN/cache must not override truth-backed visibility
- safe `404` or omission is preferred over incorrect exposure

If optional public Media endpoints are introduced later:

- they must validate article publication visibility
- they must not expose admin-only metadata
- they must not expose deleted media as active
- they must use truth-backed visibility checks

---

## 12) Async and downstream security posture

Downstream workflows are not required for Media truth success in V1.

Future consumers may include:

- Reading cache invalidation
- SEO image projection
- CDN purge
- scan workflow
- variant generation

Rules:

- downstream consumers must be idempotent
- duplicate messages must not create duplicate harmful effects
- stale events must not overwrite fresher projection state
- external side effects require durable tracking where harmful
- scan/variant/CDN failures must be observable
- downstream lag must not redefine Media truth

---

## 13) Malware scanning and derivative processing

V1 does not require malware scanning or derivative generation as a synchronous dependency.

V2+ scan/processing posture:

- scan jobs should run asynchronously
- processing jobs should be idempotent
- job identity should be deterministic
- failed jobs should enter retry/dead state
- scan result must not be guessed from timeout
- unsafe scan result must prevent public use according to policy
- variant generation must not overwrite newer derived artifacts with stale output

Until scanning exists:

- use allowlist and metadata validation
- keep public rendering conservative
- do not claim files are malware-safe merely because they were registered

---

## 14) Incident and remediation posture

During incidents:

- prefer disabling or rate-limiting register operations over corrupting Media truth
- prefer placeholder/omission over exposing unsafe or stale media
- prefer dead-letter/remediation over infinite retry loops
- prefer truth reconciliation over guessing from timeout symptoms
- prefer admin-visible degraded state over silent failure

Operational remediation may include:

- orphan storage cleanup
- rebuilding Reading media projections
- replaying Media audit events
- reconciling primary/order state
- purging unsafe media by policy