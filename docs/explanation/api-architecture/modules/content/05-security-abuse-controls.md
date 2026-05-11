# Content — Security & Abuse Controls (V1)

## 1) Mandatory authorization

All endpoints under `/api/v1/admin/content/*` require:

- Bearer authentication
- explicit authorization policies
- deny-by-default posture

Rules:

- authentication alone is never sufficient
- publish, unpublish, archive, soft-delete, and taxonomy mutation must all be policy-protected
- archived article restore is out of scope for V1 unless a later lifecycle policy explicitly enables it
- physical purge is out of scope for V1 and requires explicit retention/admin policy
- if evaluation is uncertain on a security-sensitive admin path, fall back to authoritative truth or fail closed

## 2) Governance boundaries

Publish, unpublish, archive, and soft-delete actions:

- must be auditable
- must validate lifecycle legality
- must respect optimistic concurrency/freshness rules where applicable
- must record required reason fields, for example unpublish reason
- must commit Content truth + required `OutboxMessage` atomically
- must not rely on downstream completion for success

Rules:

- Content truth owns lifecycle and visibility legality
- downstream SEO, notifications, projections, Reading, search, and caches must not redefine current lifecycle truth
- stale derived state must not re-expose draft, archived, unpublished-back-to-draft, or soft-deleted content

## 3) Mass-assignment and server-owned fields

The server owns security-sensitive and lifecycle-sensitive fields such as:

- `ArticleId`
- `ArticlePublicId`
- `Status`
- `IsDeleted`
- `AuthorUserId`
- `PublishedAt`
- `UnpublishedAt`
- `ArchivedAt`
- `DeletedAt`
- `DeletedBy`
- `Version`
- lifecycle/version markers
- system timestamps
- `CreatedAt`
- `CreatedBy`
- `UpdatedAt`
- `UpdatedBy`
- `ArticleLifecycleEvent` fields

Clients must not set these directly unless an explicitly documented API action allows it through controlled command semantics.

Rules:

- publish/unpublish/archive/soft-delete must go through dedicated lifecycle actions
- clients must not bypass lifecycle policy through generic update payloads
- server-side validation must reject or ignore unsafe field injection attempts

## 4) Content validation and stored-XSS protection

Content fields must be validated according to editorial policy.

Rules:

- `Title`, `Summary`, `Body`, and metadata length limits must be enforced
- article body, summary, title, and metadata must be validated and normalized according to editorial policy
- if `Body` supports HTML, Markdown, embeds, or rich text, server-side sanitization is required before public serving
- stored XSS, unsafe scripts, unsafe embeds, and dangerous URLs must be rejected or sanitized
- public rendering must not trust raw admin-submitted content blindly

## 5) Safe logging and disclosure

Never log unnecessarily in high-cardinality logs:

- full article body
- large editorial payloads
- sensitive moderation/internal notes where policy restricts them
- raw secret-bearing downstream payloads

Prefer logging:

- `actorUserId`
- action name
- `articleId`
- `articlePublicId`
- `version`
- `correlationId`
- `messageId` for async/outbox processing
- stable outcome classification

Rules:

- logs and errors must not leak protected internals unnecessarily
- public visibility failures should not disclose hidden draft/unpublished-back-to-draft/archived/soft-deleted state beyond safe semantics
- replay/rebuild/debug logs should use bounded identifiers rather than unsafe content dumps

## 6) Abuse and misuse posture

Admin Content endpoints still require protection against:

- compromised admin tokens
- automation loops
- repeated ambiguous retries on lifecycle actions
- mass mutation mistakes
- stale admin forms causing conflicting writes
- repeated publish/unpublish/archive/soft-delete attempts
- bulk taxonomy mistakes that could affect many articles

Recommended controls:

- rate limiting or throttling by policy
- optimistic concurrency for edit/update flows
- `Idempotency-Key` for high-impact lifecycle actions where supported
- explicit review/audit for high-impact lifecycle transitions where policy requires it
- deterministic no-op or conflict behavior for repeated equivalent lifecycle actions
- monitoring for suspicious bulk updates and taxonomy mutations

## 7) Truth-safe serving under lag

Content visibility correctness must survive downstream lag.

Rules:

- draft, archived, unpublished-back-to-draft, and soft-deleted content must not be exposed because of stale SEO, cache, route, Reading projection, or search state
- public-serving paths must prefer Content truth or a truth-backed visibility check when freshness is uncertain
- safe not-found or safe degradation is preferable to incorrect public exposure

## 8) Abuse signals to monitor

Monitor at minimum:

- spikes in publish/unpublish/archive/soft-delete attempts
- unusual `403`, `409`, or `429` patterns
- optimistic concurrency conflict spikes
- repeated equivalent lifecycle actions
- missing/duplicate `Idempotency-Key` patterns where applicable
- stale-evaluation or truth-fallback events on public-serving paths
- downstream lag indicators that could threaten visibility correctness
- replay/reconciliation anomalies for content-derived downstream state
- abnormal body sizes, suspicious markup, or repeated rejected content payloads
