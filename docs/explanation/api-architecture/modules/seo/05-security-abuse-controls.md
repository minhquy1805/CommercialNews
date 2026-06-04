# SEO — Security & Abuse Controls (V1)

## 1) Public endpoint safety

Public endpoints:

- `GET /api/v1/seo/resolve`
- `GET /api/v1/seo/metadata`

Public SEO endpoints must be safe for anonymous traffic, but they must not leak non-public Content state.

Rules:

- Public endpoints must return safe not-found for missing, inactive, unpublished, archived, soft-deleted, or uncertain routes.
- Public responses must not reveal whether a target article exists but is non-public.
- Public routing responses must expose stable public identifiers only, never internal database primary keys.
- Public metadata must not be treated as publication authority.
- Reading must still validate Content truth before exposing content.

---

## 2) Anti-enumeration posture

SEO public routes are naturally discoverable, but the system must avoid making private state enumeration easier.

Controls:

- Use generic public `404` for unavailable routes.
- Avoid distinct public errors such as:
  - “article exists but unpublished”
  - “route exists but inactive”
  - “slug reserved”
  - “metadata hidden”
- Avoid exposing inactive route metadata through public APIs.
- Avoid exposing internal resource ids.
- Monitor abnormal miss rates, high-cardinality slug probing, and sequential/bulk slug guessing.
- Apply edge protection or rate limiting where traffic patterns indicate scraping or abuse.

Allowed:

- Returning metadata for publicly resolvable, truth-visible resources.
- Logging aggregated route miss metrics.

Not allowed:

- Publicly revealing draft, archived, soft-deleted, or unpublished resource state.

---

## 3) Admin endpoint protection

Admin endpoints:

- `/api/v1/admin/seo/*`

All admin SEO endpoints require:

- Bearer authentication.
- Explicit authorization policy.
- Deny-by-default behavior.
- Audit logging for SEO truth mutations.
- Correlation ID propagation.

Recommended permissions:

- `seo.metadata.read`
- `seo.metadata.update`
- `seo.slug.read`
- `seo.slug.update`
- `seo.slug.generate`
- `seo.slug.check_availability`

Admin write operations must verify authorization before mutation.

---

## 4) Input validation and output encoding

SEO fields are user/admin-controlled and may be rendered in public HTML metadata.

Validate and bound:

- `scope`
- `slug`
- `canonicalUrl`
- `metaTitle`
- `metaDescription`
- `ogTitle`
- `ogDescription`
- `ogImageUrl`
- `robots`

Rules:

- Slugs must follow the approved slug format.
- Metadata lengths must be bounded.
- Canonical URLs must be relative URLs or approved absolute URLs according to policy.
- `ogImageUrl` must use allowed schemes and trusted host policy if external URLs are allowed.
- Reject scriptable or unsafe URL schemes such as `javascript:`, `data:`, and other unsafe schemes.
- Public rendering layers must HTML-encode metadata output.
- Do not trust stored metadata as already-safe HTML.
- Do not allow metadata fields to inject raw HTML unless a separate sanitization policy exists.

---

## 5) Canonical URL and redirect abuse controls

Canonical URLs and slug routes can influence crawlers and user navigation.

Controls:

- Canonical URL must match approved site/domain policy.
- Canonical URL must not become an open redirect vector.
- Slug changes must not silently create unsafe redirects.
- Redirect history, if introduced later, must validate target scope/domain and avoid redirect chains/loops.
- Admin slug/canonical changes should be auditable.

---

## 6) Slug generation and availability abuse

Utility endpoints:

- `POST /api/v1/admin/seo/generate-slug`
- `GET /api/v1/admin/seo/slug-availability`

Rules:

- These endpoints are admin-only unless explicitly changed later.
- Responses are advisory only and do not reserve slug ownership.
- Repeated generation/check attempts should be rate-limited.
- Availability checks must not expose unrelated private resource details.
- Conflict responses may expose `conflictResourcePublicId` only to authorized admin users.
- Final slug ownership is determined only by a successful SEO truth-bound write commit.

---

## 7) Rate limiting and traffic controls

Recommended controls:

- Public `/resolve`:
  - cache heavily
  - edge protection
  - rate limit abnormal miss-heavy traffic
  - monitor high-cardinality slug probing
- Public `/metadata`:
  - cache where safe
  - rate limit abusive resource probing
- Admin SEO writes:
  - lower rate limit than reads
  - anomaly detection for repeated slug conflicts or rapid canonical changes
- Utility endpoints:
  - rate limit aggressively enough to prevent brute-force slug probing

Rate limiting must not become the correctness mechanism. Safe visibility checks still rely on Content truth.

---

## 8) Safe logging

Logging must support investigation without leaking unsafe information.

Allowed low-risk fields:

- `scope`
- `slug`
- `resourceType`
- `resourcePublicId`
- `MessageId`
- `CorrelationId`
- `AggregateVersion`
- SEO error code
- dedupe/stale apply decision

Avoid logging:

- internal database primary keys
- access tokens
- refresh tokens
- reset or verification tokens
- full URLs containing token-like query parameters
- raw request bodies for admin metadata writes
- sensitive headers

Logging rules:

- Slugs are public identifiers but may still reveal editorial intent before publication; avoid excessive logging of non-public route attempts.
- Sanitize error messages before storing in Outbox/dead-letter/reconciliation records.
- Worker logs must include enough context for replay/rebuild investigation without leaking secrets.

---

## 9) Async consumer security

SEO consumers process Content events and optional SEO events.

Rules:

- Consumers must validate event type.
- Consumers must reject unsupported `ResourceType`.
- Consumers must treat payloads as untrusted input even if internal.
- Consumers must not blindly trust stale event order.
- Consumers must use `MessageId` dedupe and `AggregateVersion` checks.
- Consumers must not overwrite manual metadata from Content events unless policy explicitly allows it.
- Consumers must sanitize error details before logging or storing failure state.
- Consumer failures must not expose private route/content state through public APIs.

---

## 10) Cache and derived-state safety

Cache and serving artifacts are acceleration only.

Rules:

- Cache must not become the authority for publication visibility.
- Stale cache must lose to Content truth-backed checks.
- Cache keys must avoid internal IDs where public identifiers are sufficient.
- Cache invalidation failures must be observable.
- Duplicate invalidation/update events must be harmless.
- Stale cache refresh must not overwrite fresher SEO truth.

---

## 11) Audit and governance

Audit is required for SEO truth mutations that affect public serving or crawler behavior.

Audit-worthy actions:

- slug changed
- slug route deactivated/reactivated
- canonical URL changed
- metadata updated
- manual override enabled/disabled
- robots/indexability policy changed
- recovery/rebuild/cutover performed

Audit records should include:

- actor user id / public actor identity where available
- action
- resource type
- resource public id
- scope
- old/new high-level values where safe
- correlation id
- occurred at UTC

Do not include secrets or unsafe raw payloads in audit records.

---

## 12) Degraded mode and safe failure

When dependencies degrade:

- If cache is unavailable, fallback to SEO truth store where safe.
- If SEO truth store is unavailable, public slug resolution may fail safe.
- If Content truth cannot validate visibility, public serving must fail safe.
- If async SEO consumers lag, public visibility still relies on Content truth.
- If downstream cache/search/sitemap refresh fails, SEO truth commit remains valid and recovery proceeds through retry/reconciliation.

Safe failure means:

- prefer `404`, safe deny, or degraded UX
- never expose draft/unpublished/archived/soft-deleted content because SEO-derived state is stale
