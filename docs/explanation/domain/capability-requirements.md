# Capability Requirements - CommercialNews V1

This document captures capability-level requirements for CommercialNews.

The requirements are used to derive:

- module boundaries
- architecture characteristics
- runtime scenarios
- API contracts
- event contracts
- DB ownership
- ADRs

## A) Content Management

### Explicit requirements

- Admins can create, edit, publish, unpublish, archive, and soft delete articles.
- Article lifecycle states must be validated.
- Unpublish must record a reason.
- Article revision and lifecycle history must be preserved.
- Categories and tags must be manageable.
- Content changes that affect other modules must emit outbox events.

### Inferred requirements

- Content is source truth for article visibility.
- Publish and unpublish are governance boundaries and must be authorized.
- Content writes must not depend on Audit, SEO, Reading, Notifications, or Interaction completing immediately.
- Article public identity must remain stable across module boundaries.

### Operational requirements

- Write volume is moderate, but mistakes are high impact.
- Lifecycle transitions need clear logs, correlation IDs, and auditability.
- Failed async side effects must be recoverable from outbox/backlog.

## B) SEO and Discoverability

### Explicit requirements

- Slugs must be unique.
- Canonical URLs must be supported.
- Meta title and description must be supported.
- Social preview fields must be supported.
- SEO should react to article lifecycle changes.

### Inferred requirements

- Slug routing is public-facing and must be fast.
- SEO must respect publication state.
- Title changes must not automatically break existing public routes unless policy says so.
- SEO derived state may lag, but it must not expose non-public content.

### Operational requirements

- SEO regressions can hurt traffic long after the incident.
- Route repair and reconciliation should be possible.

## C) Media

### Explicit requirements

- Media assets must store metadata and storage references.
- Media can be attached to articles.
- Article media can be reordered.
- One primary media item can be selected.
- Media supports soft delete and restore.

### Inferred requirements

- Article media ordering must be deterministic.
- Broken media references should degrade gracefully in public reads.
- Media lifecycle events should update Reading projections where needed.

### Operational requirements

- Upload and metadata handling are abuse surfaces.
- Storage failures should not corrupt article truth.

## D) Reading Experience

### Explicit requirements

- Public users can list articles.
- Public users can open article details.
- Public reads support pagination, filtering, sorting, and route lookup.
- Reading exposes public-safe data only.

### Inferred requirements

- Reading must enforce publication visibility.
- Reading should serve ordinary public requests from Reading-owned projections in V1.
- Reading must not synchronously call every source module on normal public requests.
- Derived state may lag but must be safe.

### Operational requirements

- Read traffic is bursty.
- P95/P99 latency matters.
- Stale counters or delayed enrichments are acceptable; leaking drafts is not.

## E) Interaction

### Explicit requirements

- Track article views.
- Support like and unlike.
- Support comments.
- Support moderation actions such as hide, restore, delete by author, report, and dismiss reports.
- Publish public counter snapshots to Reading.

### Inferred requirements

- Interaction is high-volume and abuse-prone.
- View tracking must not slow the public read path.
- Like/unlike must be idempotent.
- Moderation actions must be auditable.

### Operational requirements

- Counters may be eventually consistent.
- Batch materialization and projection repair must be possible.
- Consumers must tolerate duplicate messages.

## F) Identity

### Explicit requirements

- Users can register and sign in.
- Email verification is supported.
- Password reset is supported.
- Refresh tokens and logout are supported.
- Password change is supported.
- Public profile update is supported.

### Inferred requirements

- Identity is security-critical.
- Tokens must be protected and time-limited.
- Identity events must not leak secrets.
- Other modules should reference users by IDs and snapshots, not by loading Identity aggregates.

### Operational requirements

- Auth endpoints are abuse targets.
- Email delivery must not block registration or password reset request success.
- Session revocation and token rotation must be observable.

## G) Authorization

### Explicit requirements

- Roles can be created, updated, activated, and deactivated.
- Permissions can be created, updated, activated, and deactivated.
- Roles can be assigned and revoked from users.
- Permissions can be granted and revoked from roles.
- API endpoints must be protected by explicit authorization policies.

### Inferred requirements

- Least privilege must be enforceable.
- Permission keys are part of the admin API contract.
- Governance changes must be audited.
- Role/permission writes must not depend on Audit ingestion completing immediately.

### Operational requirements

- Missing policy coverage is a security defect.
- Role and permission changes need strong traceability.

## H) Audit and Compliance

### Explicit requirements

- Audit stores append-only evidence for sensitive actions.
- Audit uses `MessageId` as canonical idempotency key.
- Audit exposes admin investigation endpoints.
- Audit tracks ingestion processing, failure, duplicate, ignored, and dead-letter states.
- Audit payload exposed to APIs must be sanitized, not raw sensitive input.

### Inferred requirements

- Audit is a consumer-side module and must tolerate outbox redelivery.
- AuditLog identity is internal; public ID is admin/API-facing.
- Unsupported events should be tracked as ingestion outcomes, not crash the system.
- Audit writes should be consistent through Application transaction behavior.

### Operational requirements

- Audit backlog, ingestion failures, duplicate count, and dead-letter count must be observable.
- Audit evidence must preserve enough context for investigation without exposing unnecessary PII.

## I) Notifications

### Explicit requirements

- Send verification emails.
- Send password reset emails.
- Send password changed and email verified notifications.
- Send moderation alert emails.
- Track delivery attempts and failures.

### Inferred requirements

- Notifications must not block core flows.
- Retries must not create duplicate harmful sends.
- Templates must avoid leaking secrets.
- Provider failures are expected and must be handled.

### Operational requirements

- Delivery backlog and oldest pending age must be visible.
- Retry policy must distinguish transient and terminal failures.

## J) Outbox and Integration Runtime

### Explicit requirements

- Owner modules write outbox messages atomically with truth changes when side effects are required.
- Worker publishes pending outbox messages to RabbitMQ.
- RabbitMQ consumers process at least once.
- Consumers must be idempotent.
- Envelope metadata includes event identity, aggregate identity, priority, occurred time, and published time.

### Inferred requirements

- Outbox is infrastructure, not business truth.
- RabbitMQ is transport, not permanent replay storage.
- Derived state must be repairable if messages lag, duplicate, or fail.
- Command success must not require broker publish success in the same request.

### Operational requirements

- Track pending outbox count.
- Track oldest pending outbox age.
- Track retry attempts and terminal failures.
- Track queue ready/unacked counts.
- Track consumer error classes.

## Cross-Capability Requirements

### Identifier policy

- Internal DB identity columns remain internal.
- Public IDs are used for API/admin-facing identifiers.
- Cross-module contracts should prefer stable public IDs when available.
- Message IDs are used for idempotency in event consumers.

### Transaction policy

- Handlers do not open transactions manually.
- Application transaction behavior wraps commands that require writes.
- Queries do not require write transactions.
- Truth change plus outbox insert must be atomic when side effects are required.

### Privacy policy

- Raw sensitive payloads must not be exposed through admin APIs.
- Audit detail payloads should be sanitized.
- Logs should not contain secrets.
- Email, token, password, and credential fields require explicit redaction policy.

### Observability policy

- Every cross-module event should carry correlation information.
- API and Worker logs should include message IDs, event types, and correlation IDs where available.
- Backlog and lag are first-class health indicators for async modules.

### Evolution policy

- A module can be considered for independent deployment only when data ownership,
  contracts, observability, and idempotency are mature enough.
