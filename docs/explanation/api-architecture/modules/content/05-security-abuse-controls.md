# Content — Security & Abuse Controls (V1)

## 1) Mandatory authorization

All endpoints under `/api/v1/admin/content/*` require:

- Bearer authentication
- explicit authorization policies
- deny-by-default posture

Rules:

- authentication alone is never sufficient
- publish, unpublish, archive, restore, delete, and taxonomy mutation must all be policy-protected
- if evaluation is uncertain on a security-sensitive admin path, fall back to authoritative truth or fail closed

## 2) Governance boundaries

Publish, unpublish, archive, restore, and delete actions:

- must be auditable
- must validate lifecycle legality
- must respect optimistic concurrency/freshness rules where applicable
- must record required reason fields *(for example unpublish reason)*
- must not rely on downstream completion for success

Rules:

- Content truth owns lifecycle and visibility legality
- downstream SEO, notifications, projections, and caches must not redefine current lifecycle truth
- stale derived state must not re-expose non-public content

## 3) Mass-assignment and server-owned fields

The server owns security-sensitive and lifecycle-sensitive fields such as:

- `Status`
- `AuthorUserId`
- `PublishedAt`
- `UnpublishedAt`
- `ArchivedAt`
- lifecycle/version markers
- system timestamps

Clients must not set these directly unless an explicitly documented API action allows it through controlled command semantics.

Rules:

- publish/unpublish/archive/restore must go through dedicated lifecycle actions
- clients must not bypass lifecycle policy through generic update payloads
- server-side validation must reject or ignore unsafe field injection attempts

## 4) Safe logging and disclosure

Never log unnecessarily in high-cardinality logs:

- full article body
- large editorial payloads
- sensitive moderation/internal notes where policy restricts them
- raw secret-bearing downstream payloads

Prefer logging:

- `actorUserId`
- action name
- `articleId`
- `version` where relevant
- `correlationId`
- stable outcome classification

Rules:

- logs and errors must not leak protected internals unnecessarily
- public visibility failures should not disclose hidden draft/unpublished/archived state beyond safe semantics
- replay/rebuild/debug logs should use bounded identifiers rather than unsafe content dumps

## 5) Abuse and misuse posture

Admin Content endpoints still require protection against:

- compromised admin tokens
- automation loops
- repeated ambiguous retries on lifecycle actions
- mass mutation mistakes
- stale admin forms causing conflicting writes

Recommended controls:

- rate limiting or throttling by policy
- optimistic concurrency for edit/update flows
- explicit review/audit for high-impact lifecycle transitions where policy requires it
- protection against duplicate publish/unpublish/archive/restore attempts causing unsafe downstream effects

## 6) Truth-safe serving under lag

Content visibility correctness must survive downstream lag.

Rules:

- unpublished/archived/non-public content must not be exposed because of stale SEO, cache, route, or projection state
- public-serving paths must prefer Content truth or a truth-backed visibility check when freshness is uncertain
- safe not-found or safe degradation is preferable to incorrect public exposure

## 7) Abuse signals to monitor

Monitor at minimum:

- spikes in publish/unpublish/archive/restore attempts
- unusual `403` or `409` patterns
- optimistic concurrency conflict spikes
- repeated equivalent lifecycle actions
- stale-evaluation or truth-fallback events on public-serving paths
- downstream lag indicators that could threaten visibility correctness
- replay/reconciliation anomalies for content-derived downstream state