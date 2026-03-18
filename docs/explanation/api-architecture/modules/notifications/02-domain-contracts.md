
---

## `docs/explanation/api-architecture/modules/notifications/02-domain-contracts.md`

```md id="f6e1x1"
# Notifications — Domain Contracts (V1)

## 1) Ownership
Notifications owns:
- email delivery orchestration (queueing, retries, DLQ)
- dedupe/idempotency for outbound emails
- template rendering policy (safe variables, redaction rules)
- delivery telemetry (success/failure/backlog)

Notifications does not own:
- identity lifecycle (Identity owns)
- content lifecycle (Content owns)
- audit persistence (Audit owns)

---

## 2) Entities (conceptual)

### 2.1 OutboundEmailMessage (logical)
- `MessageId`
- `EventId` (idempotency key candidate)
- `CorrelationId`
- `Recipient` (store masked/hashes by policy)
- `Template` (VerifyEmail|ResetPassword|NewArticle)
- `TemplateModel` (redacted/minimal)
- `Status`: `Queued|Sending|Sent|Failed|DeadLettered`
- `AttemptCount`
- `LastErrorCode?` (sanitized)
- `QueuedAt`, `SentAt?`

### 2.2 Template
- `TemplateName`
- `Version` (optional)
- `AllowedVariables` (allowlist)
- `RedactionRules`

---

## 3) Invariants (must hold)
- Non-blocking: domain workflows must succeed even if Notifications is down.
- Dedupe: repeated events/retries must not send duplicate emails.
- Safe content: templates must not leak tokens/PII in logs or stored fields.
- Retry-safe: at-least-once delivery tolerant; failures go to DLQ after policy threshold.

---

## 4) Trigger contracts (events consumed)

### 4.1 Consumes from Identity
- `UserRegistered` or `VerificationEmailRequested`
- `PasswordResetRequested`

### 4.2 Consumes from Content (optional)
- `ArticlePublished` (new-article notifications)

Payload expectations:
- include `EventId`, `CorrelationId`
- include recipient reference in a privacy-safe way:
  - typically userId/email (email usage is allowed for delivery but must be masked in logs/storage)

---

## 5) Events emitted (optional)
- `EmailSent`
- `EmailFailed`
These are optional but useful for audit/ops projections.