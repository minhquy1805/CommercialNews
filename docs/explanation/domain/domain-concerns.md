# Domain Concerns — CommercialNews

This document captures the **domain concerns** (rules, risks, invariants, and policies) per business capability.
It is used to derive:
- module characteristic profiles (quality priorities)
- runtime scenarios (V1 workflows)
- API/event contract boundaries and decision points (ADRs)

---

## A) Content Management (Product Core)

### Domain concerns
- **Lifecycle invariants:** article state transitions must be valid and enforced (Draft → Published → Archived; Unpublish returns to a non-public state by policy).
- **Publish/unpublish governance:** unpublishing must record a reason (policy/compliance/operations).
- **Edit history integrity:** changes must be traceable (who/when/what changed), and history must be tamper-evident at a policy level.
- **Content ownership:** Content is the source of truth for article status and timestamps; other capabilities must not override lifecycle.
- **Metadata correctness:** author, timestamps, and status must be consistent and auditable.
- **Category/Tag consistency:** taxonomy must remain consistent (no orphan references; predictable attach/detach rules).
- **Operational safety:** core lifecycle must not depend on non-critical side effects (email, audit ingestion, indexing).

### Key decision points (likely ADRs)
- History strategy: snapshot vs diff; retention policy.
- Unpublish semantics: does it revert to Draft or become a separate state?
- Archiving vs deletion policy (if both exist).

---

## B) SEO & Discoverability

### Domain concerns
- **Slug uniqueness:** slugs must be unique under a defined scope (system-wide vs per category/locale).
- **Slug stability policy:** decide when slug changes (title change does not automatically mean slug change).
- **Canonical correctness:** canonical URL rules must be consistent to avoid SEO duplication issues.
- **Meta defaults:** define fallback rules for meta title/description when not explicitly provided.
- **Social preview correctness:** preview fields (title/description/image) must be predictable and aligned with content state.
- **Publication coupling:** SEO visibility must follow publication state (published content is indexable; unpublished/archived content is not, by policy).
- **Future automation (V2):** sitemap/robots generation policies (what is included/excluded, update triggers).

### Key decision points (likely ADRs)
- Slug change policy (and V2 alias/redirect strategy).
- Canonical rules for duplicates or near-duplicates.

---

## C) Media (Images / Videos / Files)

### Domain concerns
- **Attachment integrity:** media-to-article association must be consistent (no broken primary reference, ordering must be valid).
- **Primary media rule:** define exactly one primary/cover media per article (or none by policy) and how it changes.
- **Ordering semantics:** media ordering rules must be stable and deterministic.
- **Delete/restore policy:** soft delete vs hard delete, restore behavior, and retention window.
- **Safety and abuse surface:** media inputs introduce risk (file types, metadata). Policy must define allowed types and safe handling.
- **Publication coupling (optional):** clarify whether media can exist independently of article publication state.

### Key decision points (likely ADRs)
- Soft delete retention and restore semantics.
- Primary media selection rules and constraints.

---

## D) Reading Experience (Public)

### Domain concerns
- **Read path correctness:** listing/detail results must reflect publication state (draft/unpublished content must not be publicly visible).
- **Sorting/filter semantics:** define canonical rules for filters and sorts (time vs popularity; what “popularity” means).
- **Related articles logic:** deterministic rule (same category/tags) and fallback behavior (when insufficient related items).
- **Search semantics (scope-dependent):** keyword matching rules and what fields are searchable in V1.
- **Graceful degradation:** reading must remain usable if non-critical subsystems fail (policy-level expectation).

### Key decision points (likely ADRs)
- Popularity definition (views vs likes vs blended) and when it becomes authoritative.
- Search capability level (basic vs full-text) and upgrade path.

---

## E) Interaction (High Traffic, “Hot” Features)

### Domain concerns
- **View counting semantics:** define what a “view” means (simple counter in V1; unique view policy in V2).
- **Like semantics:** like/unlike must be idempotent; totals must remain consistent.
- **Comment lifecycle:** create/edit/delete rules; visibility rules (e.g., hidden/approved in V2).
- **Moderation policy (V2):** what constitutes spam/abuse and how moderation state affects visibility.
- **Non-blocking requirement:** interaction tracking must not degrade the public read experience (policy expectation).
- **Abuse controls:** rate limits and anti-spam hooks (especially comments) must exist or be planned for.

### Key decision points (likely ADRs)
- Unique view strategy (V2) and privacy implications.
- Comment moderation model and enforcement boundaries.

---

## F) Identity & Access

### Domain concerns
- **Account verification policy:** what actions require verified email; resend verification rules.
- **Session semantics:** refresh token rotation/revocation rules; logout behavior.
- **Password reset safety:** reset flow must be secure and rate-limited; tokens must be time-bound by policy.
- **Account state:** active/inactive/locked semantics (if applicable), and how it impacts login and admin actions.
- **PII handling:** define what user data is considered sensitive and how it must be handled in logs/events/audit.

### Key decision points (likely ADRs)
- Refresh token strategy (rotation, reuse detection, revocation semantics).
- Verification gating rules (what is allowed before verification).

---

## G) Admin Governance & Authorization

### Domain concerns
- **Least privilege:** roles/permissions must enforce minimum necessary access.
- **Policy correctness:** admin endpoints must have explicit authorization policy coverage.
- **Role/permission change governance:** changes are sensitive and must be auditable.
- **Administrative content controls:** publish/unpublish, delete/restore, and moderation actions must be traceable.
- **Boundary clarity:** governance concerns must not leak into domain logic arbitrarily (avoid scattered checks).

### Key decision points (likely ADRs)
- Permission naming and versioning strategy.
- Audit coverage policy (which actions are mandatory to log).

---

## H) Notifications

### Domain concerns
- **Trigger correctness:** notifications must be sent to the correct recipients for the correct events.
- **Non-blocking rule:** notification delivery must not block core flows (policy-level).
- **Duplication prevention:** repeated events/retries must not cause duplicate emails (idempotency expectation).
- **Abuse prevention:** rate limits for sensitive email workflows (verification/reset/resend).
- **Privacy and safety:** templates must avoid leaking tokens/PII; logs must be redacted.

### Key decision points (likely ADRs)
- Email delivery retry policy and deduplication strategy.
- New-article notification policy (who receives, opt-out/unsubscribe in V2).

---

## I) Audit

### Domain concerns
- **Append-only evidence:** `AuditLog` records canonical audit evidence and must not gain update/delete semantics in V1.
- **Canonical idempotency:** `MessageId` copied from Outbox is the audit idempotency key.
- **Public vs internal identity:** `AuditLogId`/`AuditIngestionId` are internal DB identities; `PublicId` is API/admin-facing.
- **Consumer-side ingestion state:** `AuditIngestion` tracks Audit consumer processing separately from producer-side Outbox publication state.
- **Redaction boundary:** stored and returned payloads must be sanitized; raw sensitive payloads must not be exposed by Audit detail responses.
- **Normalizer ownership:** Application owns normalizer abstractions and registry usage; Infrastructure owns concrete normalizers; Worker owns broker consumption.

### Key decision points (likely ADRs)
- Audit retention and archival policy.
- Tamper-evidence/hash-chain policy depth for V1 vs V2.
- Which event payload fields are allowed in sanitized audit evidence.

---

## J) Outbox & Integration Delivery

### Domain concerns
- **Atomic async intent:** required integration messages must commit in the same local transaction as the owning module truth change.
- **At-least-once delivery:** consumers must be idempotent and replay-safe.
- **Producer vs consumer state:** Outbox publication status must not be confused with downstream consumer completion.
- **Envelope consistency:** messages carry stable identity, event type, aggregate identity, priority, occurred time, and published time.
- **Operational recovery:** failed publication, redelivery, and replay must be observable and repairable.

### Key decision points (likely ADRs)
- Outbox retention window and replay procedure.
- DLQ handling and poison-message policy.
- Priority semantics across publishers and consumers.

---
