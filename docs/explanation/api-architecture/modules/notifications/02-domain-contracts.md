# Notifications — Domain Contracts (V1)

## 1) Ownership

Notifications owns:

- email delivery workflow state
- email delivery attempt history
- outbound email dedupe and idempotency safeguards
- provider outcome classification for delivery processing
- delivery retry/remediation policy at the notification workflow level
- template rendering policy
  - allowed variables
  - masking/redaction rules
  - safe rendering constraints
- delivery telemetry and operational visibility

Notifications does **not** own:

- identity lifecycle truth
- verification truth
- password reset validity
- content lifecycle truth
- publication truth
- authorization/governance truth
- audit persistence truth

### Boundary rule

Notifications is a **downstream side-effect module**.

It reacts to already-committed upstream truth and manages delivery workflow safely.
It does not make upstream truth valid, invalidate it, or roll it back.

---

## 2) Domain posture in V1

Notifications V1 is primarily a **delivery workflow domain**, not a business command domain.

Its central concern is:

- whether an intended notification delivery was queued
- whether it was attempted
- whether it succeeded, failed, was suppressed, or is retryable
- whether retries/remediation remain safe under at-least-once delivery

### Important separation

`OutboxMessage` is a **system/messaging artifact**, not the core business entity of Notifications.

In V1:

- upstream modules write truth + outbox atomically
- Worker/runtime crosses the async boundary
- Notifications owns the delivery workflow that begins after committed async intake is available

Therefore, Notifications domain contracts should center on:

- `EmailDelivery`
- `EmailDeliveryAttempt`

not on the outbox record itself.

---

## 3) Core domain concepts

### 3.1 EmailDelivery

Represents one durable notification delivery workflow for a specific async message / protected delivery intent.

#### Identity

- `MessageId`
- optional upstream linkage fields such as:
  - `AggregateId`
  - `CorrelationId`
  - `UserId` where relevant

#### Core attributes

- `Recipient`
  - delivery-usable recipient value may be present where needed
  - operational views/logs must follow masking/redaction policy
- `TemplateKey`
  - `VerifyEmail`
  - `ResetPassword`
  - `NewArticle`
  - other policy-approved values
- `Provider`
- `Status`
- `AttemptCount`
- `CreatedAt`
- `LastAttemptAt?`
- `SentAt?`
- `LastErrorCode?`
- `LastErrorMessage?` *(sanitized)*
- `CorrelationId?`

#### Recommended workflow states

V1 delivery workflow should use a small, monotonic state model such as:

- `Queued`
- `Sending`
- `Sent`
- `Failed`

Optional operational extensions, only if clearly implemented and documented:

- `RetryScheduled`
- `TerminalFailed`
- `Suppressed`

`Sent` is terminal.
A failed workflow may re-enter a retryable state only through explicit, policy-governed workflow logic.

---

### 3.2 EmailDeliveryAttempt

Represents one concrete attempt to perform the outward delivery effect.

#### Identity

- `EmailDeliveryAttemptId` or equivalent internal identity
- parent link to `EmailDelivery`
- `MessageId`
- `AttemptNumber`

#### Core attributes

- `StartedAt`
- `CompletedAt?`
- `Outcome`
  - `Succeeded`
  - `Failed`
  - `TimedOut`
  - `Rejected`
  - other policy-approved provider outcome classes
- `Provider`
- `ProviderMessageId?` *(only if policy allows exposure/storage)*
- `ErrorClass?`
  - transient
  - permanent
  - policy
  - unknown
- `ErrorCode?`
- `ErrorMessage?` *(sanitized)*
- `CorrelationId?`

#### Role

`EmailDeliveryAttempt` exists to support:

- retry reasoning
- incident investigation
- provider outcome visibility
- bounded, observable remediation
- proof that a send attempt actually happened

---

### 3.3 Template rendering policy (concept, not first-class V1 entity)

Notifications V1 requires a template rendering policy, but does not require template storage/versioning to be a central persisted domain entity.

#### Conceptual concerns

- `TemplateKey`
- optional `TemplateVersion`
- allowed variable allowlist
- redaction/masking rules
- safe HTML/text rendering rules

#### V1 posture

Template storage may be implementation-specific in V1.

A first-class `EmailTemplate` registry/version store is a V2+ hook, not required as a central V1 entity.

---

## 4) Invariants (must hold)

### 4.1 Non-blocking upstream truth

Upstream truth-owning workflows must succeed without waiting for notification completion.

Examples:

- registration must not depend on email provider success
- password reset request acceptance must not depend on email send completion
- publication truth must not depend on notification delivery completion

Notifications must never redefine upstream business success.

---

### 4.2 Message-level idempotency

The same `MessageId` may be retried, replayed, or redelivered.

Notifications must remain safe under:

- producer-side publish retry
- broker redelivery
- consumer restart
- replay/rebuild flows where applicable

Duplicate handling by `MessageId` is mandatory.

---

### 4.3 Business-level idempotency

Retries, redeliveries, or replay must not create duplicate **harmful outward effects** for the same protected delivery intent.

This is stronger than simple message dedupe.

Examples:

- one verification delivery intent must not accidentally produce multiple successful sends unless policy explicitly allows resend
- one reset delivery intent must not accidentally produce multiple harmful visible sends because of timeout ambiguity or retry behavior

---

### 4.4 Safe terminal success

For one protected delivery workflow:

- at most one terminal successful send outcome may be accepted
- later duplicates for the same protected delivery intent must be rejected, ignored, or handled by explicit resend policy

---

### 4.5 Attempt history integrity

Every stored delivery attempt must correspond to a real processing attempt.

Attempt records must support:

- operational debugging
- retry reasoning
- provider incident analysis
- audit-adjacent investigations where needed

Attempt history must not be silently inconsistent with delivery workflow state.

---

### 4.6 Safe content and privacy

Notifications must not store or log unsafe secret-bearing payloads.

Specifically:

- raw verification/reset secrets must not be stored in delivery workflow records
- logs must not expose raw secrets or unsafe provider payloads
- recipient information in logs or admin views must follow masking/redaction policy
- error details must be sanitized

---

### 4.7 Retry-safe bounded remediation

Failures must be handled with bounded retry/remediation behavior.

Notifications must not rely on:

- infinite hidden retry loops
- blind same-intent replay after timeout ambiguity
- uncontrolled operator resend behavior

Retry/remediation must remain:

- observable
- bounded
- policy-controlled
- safe under at-least-once delivery

---

## 5) Trigger contracts (events consumed)

Notifications consumes committed events emitted by upstream truth-owning modules.

### 5.1 Consumes from Identity

Typical V1 events include:

- `Auth.EmailVerificationRequested`
- `Auth.PasswordResetRequested`

Module-local aliases or older names may exist during transition, but V1 docs should converge toward a stable system-wide naming policy.

### 5.2 Consumes from Content (optional)

Typical optional event:

- `Content.ArticlePublished`

Used only when article notification behavior is enabled by policy.

### 5.3 Consumes from Authorization (optional)

Possible future/policy-driven examples:

- governance-related events where outbound operator/user notification is explicitly required

Notifications must not assume such events are required in all V1 deployments.

---

## 6) Consumed event payload expectations

Consumed events should include stable async identity and enough safe context to render/send without exposing sensitive truth improperly.

### Minimum expectations

- `MessageId`
- `EventType`
- `OccurredAt`
- `CorrelationId`
- `AggregateId` where relevant
- `Version` where ordered transitions matter
- payload fields required for safe delivery execution

### Recipient expectations

Recipient data may be included where operationally necessary for delivery, but the following rules apply:

- only minimum necessary delivery data should travel in the event
- logs and operational views must mask/redact recipient information by policy
- raw secrets/tokens must not appear in the event payload
- if upstream modules own a secret-bearing token flow, payload should carry only safe identifiers or delivery-safe derived data

---

## 7) Optional events emitted by Notifications

Notifications may emit operational or derived events such as:

- `Notifications.EmailSent`
- `Notifications.EmailFailed`
- `Notifications.EmailSuppressed`
- `Notifications.EmailDeliveryRetried`

These are optional and should be treated as:

- operational/derived events
- useful for audit/ops/reporting/projections
- not authoritative upstream business truth

A successful notification event does **not** define whether registration, reset, publication, or governance truth committed.
A failed notification event does **not** invalidate already-committed upstream truth.

---

## 8) Domain rules for retry, replay, and resend

### 8.1 Retry

Retry is an operational continuation/remediation of an existing delivery workflow.

Retry must:

- respect `MessageId`-level dedupe
- respect business-intent protections
- respect suppression/policy state
- remain bounded and observable

### 8.2 Replay

Replay of the same message must be harmless.

The workflow must not assume replay means “safe to send again.”

### 8.3 Resend

Resend is not the same as replay.

Resend is a policy-controlled business/operational action that may intentionally create a new delivery intent.
If resend is allowed, it must be explicit and must not be confused with accidental duplicate processing.

---

## 9) What Notifications truth means

Notifications truth in V1 is **delivery workflow truth**, not business truth.

Examples of notification truth:

- this delivery workflow exists
- it has been attempted N times
- it succeeded / failed / was suppressed
- it is retryable or terminal-failed according to policy

Examples of things that are **not** notification truth:

- whether a user is verified
- whether a password reset is still valid
- whether an article is published
- whether a role assignment is effective

---

## 10) Open V2 hooks

Potential V2+ extensions include:

- first-class template registry/versioning
- notification preferences/subscriptions
- richer provider abstraction and provider metadata
- explicit suppression entity/store
- dead-letter entity/store if operational complexity justifies it