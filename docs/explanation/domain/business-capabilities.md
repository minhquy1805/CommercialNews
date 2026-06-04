# Business Capabilities - CommercialNews

This document describes the business capabilities that shape CommercialNews
modules and runtime boundaries. It is intentionally capability-focused, not
class-focused.

## A) Content Management

Purpose: manage editorial truth and article lifecycle.

Capabilities:

- Create articles and drafts.
- Edit articles and preserve revision/lifecycle history.
- Publish, unpublish, archive, and soft delete articles.
- Record reasons for governance-sensitive transitions.
- Manage categories and tags.
- Attach tags to articles.
- Emit article lifecycle events for downstream consumers.

Key output:

- A correct article lifecycle.
- Stable public article identity.
- Traceable editorial changes.

## B) SEO and Discoverability

Purpose: control how public content is discovered and routed.

Capabilities:

- Manage slug routes.
- Ensure slug uniqueness.
- Manage canonical URLs.
- Manage meta title and description.
- Manage social preview data.
- React to content lifecycle events.
- Support future sitemap, robots, redirects, and alias policies.

Key output:

- Stable public routes.
- Search-friendly metadata.
- Safe behavior when content is unpublished or archived.

## C) Media

Purpose: manage media assets and article-media composition.

Capabilities:

- Register media assets.
- Store media metadata such as type, dimensions, duration, alt text, and storage location.
- Update media metadata.
- Soft delete and restore media assets.
- Attach and detach media from articles.
- Reorder article media.
- Set one primary media item per article.

Key output:

- Reliable media metadata.
- Deterministic article media presentation.
- Recoverable media lifecycle.

## D) Reading Experience

Purpose: serve public readers quickly and safely.

Capabilities:

- Serve article lists.
- Serve article details.
- Resolve public routes through projected data.
- Filter by publication visibility.
- Include public-safe SEO, media, and interaction counter data.
- Support basic search and related content where in scope.
- Converge from source-module events through projections.

Key output:

- Fast public reads.
- Public-safe derived state.
- A read path that does not synchronously depend on every source module.

## E) Interaction

Purpose: handle high-volume public engagement and moderation signals.

Capabilities:

- Track views.
- Like and unlike articles.
- Create, hide, restore, and delete comments.
- Report comments and dismiss report groups.
- Publish public counter snapshots to Reading.
- Trigger moderation-related notifications where needed.

Key output:

- Engagement state.
- Moderation workflow state.
- Public counter projections.

## F) Identity

Purpose: manage accounts, credentials, and sessions.

Capabilities:

- Register and authenticate users.
- Verify email addresses.
- Resend verification emails.
- Manage refresh tokens and logout.
- Request and complete password reset.
- Change password.
- Update public profile data.
- Emit identity events for notifications, authorization setup, reading projections, and audit.

Key output:

- Secure user identity.
- Stable user public identity.
- Session and credential lifecycle.

## G) Authorization

Purpose: control what admins and users can do.

Capabilities:

- Manage roles.
- Manage permissions.
- Assign and revoke user roles.
- Grant and revoke role permissions.
- Evaluate permissions for API policy enforcement.
- Emit governance events for audit.

Key output:

- Least-privilege access control.
- Explicit permission catalog.
- Traceable role and permission changes.

## H) Audit and Compliance

Purpose: preserve canonical evidence for sensitive actions and operational ingestion state.

Capabilities:

- Ingest audit-relevant events from RabbitMQ through Worker.
- Normalize module-specific event payloads.
- Classify action and risk.
- Redact sensitive payloads before evidence storage.
- Store append-only audit evidence.
- Track consumer-side ingestion state and idempotency by message ID.
- Expose admin investigation APIs and dashboard summaries.

Key output:

- Append-only audit log.
- Operational audit ingestion tracking.
- Admin-facing investigation tools.

## I) Notifications

Purpose: deliver system messages reliably after truth changes commit.

Capabilities:

- Send verification emails.
- Send password reset emails.
- Send password changed and email verified notifications.
- Send moderation alert emails.
- Track email delivery state.
- Retry transient failures.
- Avoid duplicate harmful sends.

Key output:

- Reliable non-blocking notification delivery.
- Delivery history and operational visibility.

## J) Outbox and Integration Runtime

Purpose: connect modules without making core workflows depend on immediate side effects.

Capabilities:

- Store outbox messages atomically with owner-module truth changes.
- Publish outbox messages to RabbitMQ.
- Carry envelope metadata such as message ID, event type, aggregate identity, priority, occurred time, and published time.
- Support at-least-once delivery.
- Support idempotent consumers.
- Expose backlog, retry, and failure visibility.

Key output:

- Reliable event publication.
- Async module integration.
- Recoverable side-effect processing.

## Capability-To-Module Map

| Capability | Primary module |
| --- | --- |
| Content Management | Content |
| SEO and Discoverability | SEO |
| Media | Media |
| Reading Experience | Reading |
| Interaction | Interaction |
| Identity | Identity |
| Authorization | Authorization |
| Audit and Compliance | Audit |
| Notifications | Notifications |
| Outbox and Integration Runtime | Outbox + Worker |

## Cross-Capability Rule

Business capabilities may collaborate, but ownership stays local:

- Source truth lives in the owning module.
- Derived state lives in the consuming module.
- Cross-module contracts use stable IDs and events.
- Async side effects must not define whether the original business action succeeded.
