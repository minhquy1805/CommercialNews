# System Data Model — Notifications (V1)

> **Recommended file:** `explanation/architecture/arc42/system-data/system-data-notifications-v1.md`  
> **Module:** Notifications  
> **Purpose:** Deliver system emails reliably **after committed truth exists**, without blocking core flows, using the standard **Outbox → Broker/Worker → Delivery State** pattern.

---

## 0) Data System fit (V1)

Notifications is a **non-critical side-effect module**.

Its job is to:

- react to already-committed domain events
- manage email delivery workflow
- preserve delivery visibility, retry safety, and operability
- avoid duplicate successful sends under at-least-once delivery

### Architectural posture

Notifications is **not** an owner of Identity truth, Authorization truth, or Content truth.

Notifications is a **downstream follower** of committed truth:

- Identity owns registration / verification / reset truth
- Authorization owns governance truth
- Content owns publication truth
- Notifications owns only **delivery workflow truth**

### Data role split

Notifications V1 contains two different data concerns:

1. **Replication / async intent artifact**
   - `OutboxMessage`
   - written atomically with truth change
   - used to cross the async boundary safely

2. **Notification-owned delivery workflow truth**
   - `EmailDelivery`
   - `EmailDeliveryAttempt`
   - used to track dedup, attempts, provider outcomes, retries, and terminal status

### V1 implementation note

If CommercialNews uses a **shared system outbox**, Notifications should **reuse** that outbox rather than redefine a separate domain-owned outbox model.

Therefore:

- `OutboxMessage` is treated as a **system/messaging artifact**
- `EmailDelivery` and `EmailDeliveryAttempt` are treated as **Notifications-owned operational truth**

### Non-negotiables

- register / verify / forgot / reset must still work if the email provider is down
- retries must be safe
- duplicate successful sends must be prevented
- logs and payloads must avoid secrets / unnecessary PII
- auth-critical emails must have higher delivery priority than low-priority notification traffic

---

## 1) Scope & boundaries (V1)

### In scope (V1)

- system emails:
  - verification email
  - password reset email
  - optional new-article notification
- async processing of committed events
- delivery state machine
- retry and backlog visibility
- idempotent delivery behavior
- operational querying for delivery status and failures

### Out of scope (V2+)

- template registry / template version management as first-class storage
- subscription / preference management for content notifications
- campaign / marketing delivery
- advanced engagement tracking
- DLQ as a first-class domain entity if not required yet

### Cross-module dependencies

- Identity emits verification/reset-related events; Notifications delivers emails
- Content may emit article-published events; Notifications may optionally deliver article notifications
- Authorization may emit governance events; Notifications may consume them only if policy requires email side effects
- Notifications does **not** own Identity, Authorization, or Content lifecycle/state

### Ownership boundary rule

Notifications may store:

- recipient address used for a send attempt
- template key/version used for delivery
- delivery outcome, attempts, timestamps, provider metadata

Notifications must **not** become the owner of:

- user verification state
- password reset validity
- role assignment truth
- article publication truth

---

## 2) Capability → Entity mapping

### 2.1 Async email delivery from committed events

**System / messaging artifact**
- `OutboxMessage`
  - durable async intent
  - scheduling / retry metadata for publication/processing
  - not business truth of Notifications itself

**Notifications-owned workflow truth**
- `EmailDelivery`
  - one delivery workflow per `MessageKey`
  - delivery-level visibility and terminal state
- `EmailDeliveryAttempt`
  - append-only or near-append-only attempt history
  - provider outcome / classification / error detail / timestamps

### 2.2 Abuse prevention

Primarily enforced outside delivery truth:

- Redis / app-layer rate limit for resend / forgot / similar endpoints
- optional DB visibility through a log table if operationally justified

### 2.3 V2 hooks

- `EmailTemplate`
- `NotificationSubscription`
- `NotificationPreference`
- `DeadLetterMessage`

---

## 3) Workload & hot paths (V1)

### Write patterns

- moderate steady delivery volume
- bursty spikes during:
  - register surges
  - forgot password abuse
  - resend storms
  - provider recovery after outage

### Read patterns

Mostly operational:

- inspect failed deliveries
- inspect pending backlog
- inspect attempts for a message
- inspect delivery status for a user or correlation id

### True hot path

The dominant operational hot path is:

- selecting pending work efficiently
- applying priority-aware and retry-aware processing
- preventing duplicate successful sends
- draining backlog without starving auth-critical messages

---

## 4) Dataflows (V1) — REST / DB / Broker

> Principle: core flows commit business truth and async intent first; Notifications acts later.

### 4.1 Email verification request (Identity → Notifications)

**Sync (Identity API → DB)**

Identity:

- creates `UserAccount` in unverified state
- creates `EmailVerificationToken` storing only token hash
- writes `OutboxMessage` in the same transaction:
  - `EventType = Auth.EmailVerificationRequested`

**Async (Worker / consumer → Notifications)**

Notifications:

- consumes the committed event
- resolves recipient + template inputs
- creates `EmailDelivery` if not already created for the `MessageKey`
- records an `EmailDeliveryAttempt`
- sends via provider
- updates delivery status safely

**Failure behavior**

- provider failure does not break registration
- retry is safe
- duplicate successful sends are prevented by workflow state + dedup checks

---

### 4.2 Resend verification

**Sync**

- rate-limited in Redis / app layer
- Identity emits a new outbox event only when allowed

**Async**

- Notifications processes the new event independently
- dedup is by `MessageKey`, not merely by user id

**Policy note**

Older verification tokens may remain valid or may be revoked by Identity policy; Notifications does not own that decision.

---

### 4.3 Forgot password / reset email

**Sync**

- Identity writes `Auth.PasswordResetRequested`
- only identifiers / safe template data are carried in the event
- raw reset secret must not be stored in outbox payload

**Async**

- Notifications creates/updates delivery workflow
- sends reset email
- records attempts and provider outcomes

---

### 4.4 Optional new article notification (Content → Notifications)

**Sync**

- Content commits publication truth
- Content writes `Content.ArticlePublished` to outbox

**Async**

- Notifications may process this event if article notifications are enabled by policy
- lower priority than auth-critical notifications

---

### 4.5 Optional governance email (Authorization → Notifications)

**Sync**

- Authorization commits governance truth
- Authorization writes governance event to outbox

**Async**

- Notifications may process governance-related emails only when policy explicitly requires them

**Boundary rule**

Governance truth remains in Authorization regardless of notification success/failure.

---

## 5) Invariants (V1 rules)

### 5.1 Non-blocking core flows

Core writes only require:

- business truth commit
- outbox write in the same transaction

Email delivery is not part of core success.

---

### 5.2 Idempotency & retry safety

- each email-triggering event has a unique `MessageKey`
- `EmailDelivery` allows **at most one terminal successful send** per `MessageKey`
- the same message may be observed multiple times by workers/consumers
- reprocessing must not create harmful duplicate successful sends

---

### 5.3 Delivery workflow ownership

Notifications owns:

- whether a delivery is queued, sending, sent, retryable-failed, or terminal-failed
- how many attempts were made
- provider result classification
- what operational error was seen last

Notifications does not own:

- whether the originating business action is still valid
- whether user/account/content/governance truth changed later

---

### 5.4 Delivery state machine

Recommended monotonic workflow:

- `Queued → Sending → Sent`
- `Queued/Sending → Failed`
- `Failed → Queued` only when retry policy explicitly re-queues the delivery
- `Sent` is terminal

Do not allow uncontrolled flip-flop across terminal states.

---

### 5.5 Attempt history correctness

- every provider send attempt should be traceable
- an attempt record must correspond to a real processing attempt
- attempt history must support incident investigation and replay reasoning

---

### 5.6 Privacy & safety

- never store raw verification/reset secrets in outbox or delivery tables
- redact `LastError`
- do not persist provider payloads that contain secrets unless explicitly sanitized
- avoid storing more recipient/context data than necessary

---

### 5.7 Burst handling

- backpressure is acceptable for low-priority notification traffic
- verification/reset emails must outrank optional content notifications
- retry storms must not starve fresh auth-critical deliveries

---

## 6) Redis plan (Notifications V1)

Redis is allowed for acceleration and protection, but it is **not the source of truth**.

### 6.1 Rate limiting

Examples:

- `cn:rl:resend:email:{emailNorm}:{window}`
- `cn:rl:forgot:email:{emailNorm}:{window}`
- `cn:rl:forgot:ip:{ip}:{window}`

### 6.2 Consumer dedup support

Examples:

- `cn:msg:processed:{MessageKey}` with TTL aligned to replay/troubleshooting window

This is a **supporting optimization**, not the only correctness mechanism.

### 6.3 REST idempotency support (optional)

Examples:

- `cn:idem:{operation}:{idempotencyKey}`

Useful for retryable public endpoints such as:

- register
- resend
- forgot

### 6.4 Redis correctness rule

Redis may help reduce duplicate work, but durable delivery correctness must still be enforced by:

- database uniqueness
- delivery workflow checks
- retry-safe application logic

---

## 7) Entities (Logical schema) — SQL Server (V1)

---

### 7.1 `OutboxMessage`

> Preferred interpretation: a **shared system outbox** or shared messaging artifact.  
> Reuse existing system outbox if present.

| Field | Type | Null | Default | Notes |
|---|---|---:|---|---|
| OutboxId | BIGINT IDENTITY | NO |  | PK |
| MessageKey | UNIQUEIDENTIFIER | NO | NEWID() | unique async message identity |
| EventType | NVARCHAR(120) | NO |  | `Auth.EmailVerificationRequested`, `Auth.PasswordResetRequested`, ... |
| AggregateType | NVARCHAR(60) | NO |  | `User`, `Article`, `RoleAssignment`, ... |
| AggregateId | NVARCHAR(100) | NO |  | string-form cross-module id |
| PayloadJson | NVARCHAR(MAX) | NO |  | minimal + redacted |
| Priority | TINYINT | NO | `5` | 1 = highest |
| OccurredAt | DATETIME2(3) | NO | SYSUTCDATETIME() | |
| Status | VARCHAR(20) | NO | `'Pending'` | Pending / Processing / Processed / Failed |
| RetryCount | INT | NO | `0` | publication/processing retries |
| NextRetryAt | DATETIME2(3) | YES |  | |
| LastError | NVARCHAR(500) | YES |  | redacted |
| ProcessedAt | DATETIME2(3) | YES |  | |

**V2 hooks**
- `PartitionKey`
- `ExpiresAt`

**Ownership note**
- system/messaging concern
- not notification business truth by itself

---

### 7.2 `EmailDelivery`

> One delivery workflow per `MessageKey`.

| Field | Type | Null | Default | Notes |
|---|---|---:|---|---|
| EmailDeliveryId | BIGINT IDENTITY | NO |  | PK |
| MessageKey | UNIQUEIDENTIFIER | NO |  | unique workflow key |
| UserId | BIGINT | YES |  | optional recipient identity reference |
| ToEmail | NVARCHAR(320) | NO |  | recipient used for this workflow |
| TemplateKey | NVARCHAR(80) | NO |  | `VerifyEmail`, `ResetPassword`, `NewArticle`, ... |
| TemplateVersion | INT | YES |  | V2-ready |
| Subject | NVARCHAR(200) | YES |  | optional rendered subject snapshot |
| Provider | VARCHAR(30) | NO | `'smtp'` | smtp / sendgrid / ... |
| Status | VARCHAR(20) | NO | `'Queued'` | Queued / Sending / Sent / Failed |
| AttemptCount | INT | NO | `0` | total attempts made |
| LastAttemptAt | DATETIME2(3) | YES |  | |
| SentAt | DATETIME2(3) | YES |  | terminal success time |
| LastError | NVARCHAR(500) | YES |  | redacted |
| CorrelationId | NVARCHAR(100) | YES |  | traceability |
| CreatedAt | DATETIME2(3) | NO | SYSUTCDATETIME() | |
| UpdatedAt | DATETIME2(3) | NO | SYSUTCDATETIME() | |

**Rules**
- one row per `MessageKey`
- at most one successful terminal outcome per `MessageKey`

**V2 hooks**
- `ProviderMessageId`
- engagement counters if ever needed

---

### 7.3 `EmailDeliveryAttempt`

> Delivery attempt history for visibility, retry reasoning, and incident investigation.

| Field | Type | Null | Default | Notes |
|---|---|---:|---|---|
| EmailDeliveryAttemptId | BIGINT IDENTITY | NO |  | PK |
| EmailDeliveryId | BIGINT | NO |  | parent workflow |
| MessageKey | UNIQUEIDENTIFIER | NO |  | denormalized lookup aid |
| AttemptNumber | INT | NO |  | 1..N within workflow |
| StartedAt | DATETIME2(3) | NO | SYSUTCDATETIME() | |
| CompletedAt | DATETIME2(3) | YES |  | |
| Outcome | VARCHAR(20) | NO |  | Succeeded / Failed / TimedOut / Rejected |
| Provider | VARCHAR(30) | NO | `'smtp'` | |
| ErrorClass | VARCHAR(30) | YES |  | transient / permanent / policy / unknown |
| ErrorCode | NVARCHAR(80) | YES |  | provider/app classification |
| ErrorMessage | NVARCHAR(500) | YES |  | redacted |
| CorrelationId | NVARCHAR(100) | YES |  | |
| CreatedAt | DATETIME2(3) | NO | SYSUTCDATETIME() | |

**Rules**
- attempt history must be append-safe
- one attempt number per workflow must be unique

---

### 7.4 `EmailRateLimitLog` (optional)

| Field | Type | Null | Default | Notes |
|---|---|---:|---|---|
| RateLogId | BIGINT IDENTITY | NO |  | PK |
| UserId | BIGINT | YES |  | |
| Endpoint | NVARCHAR(60) | NO |  | register / forgot / resend |
| ToEmail | NVARCHAR(320) | YES |  | |
| IpAddress | NVARCHAR(45) | YES |  | |
| Allowed | BIT | NO | `1` | decision |
| Reason | NVARCHAR(120) | YES |  | RateLimited / Blocked / ... |
| OccurredAt | DATETIME2(3) | NO | SYSUTCDATETIME() | |
| CorrelationId | NVARCHAR(100) | YES |  | |

---

## 8) Constraints & indexes — Notifications (V1)

### 8.1 OutboxMessage

**Constraints**
- PK: `PK_OutboxMessage(OutboxId)`
- UNIQUE: `UQ_Outbox_MessageKey(MessageKey)`

**Indexes**
- `IX_Outbox_Status_Priority_NextRetryAt`
  - Key: `(Status, Priority, NextRetryAt)`
- `IX_Outbox_OccurredAt`
  - Key: `(OccurredAt DESC)`
- `IX_Outbox_Aggregate`
  - Key: `(AggregateType, AggregateId, OccurredAt DESC)`

---

### 8.2 EmailDelivery

**Constraints**
- PK: `PK_EmailDelivery(EmailDeliveryId)`
- UNIQUE: `UQ_EmailDelivery_MessageKey(MessageKey)`
- CHECK:
  - `Status IN ('Queued','Sending','Sent','Failed')`
  - `AttemptCount >= 0`

**Indexes**
- `IX_EmailDelivery_Status_LastAttemptAt`
  - Key: `(Status, LastAttemptAt DESC)`
- `IX_EmailDelivery_UserId_CreatedAt`
  - Key: `(UserId, CreatedAt DESC)`
- `IX_EmailDelivery_TemplateKey_CreatedAt`
  - Key: `(TemplateKey, CreatedAt DESC)`
- `IX_EmailDelivery_ToEmail_CreatedAt` *(optional / PII-sensitive)*
  - Key: `(ToEmail, CreatedAt DESC)`

---

### 8.3 EmailDeliveryAttempt

**Constraints**
- PK: `PK_EmailDeliveryAttempt(EmailDeliveryAttemptId)`
- FK/logical ref: `EmailDeliveryId -> EmailDelivery.EmailDeliveryId`
- UNIQUE: `(EmailDeliveryId, AttemptNumber)`
- CHECK:
  - `AttemptNumber > 0`

**Indexes**
- `IX_EmailDeliveryAttempt_EmailDeliveryId_AttemptNumber`
  - Key: `(EmailDeliveryId, AttemptNumber DESC)`
- `IX_EmailDeliveryAttempt_MessageKey_CreatedAt`
  - Key: `(MessageKey, CreatedAt DESC)`
- `IX_EmailDeliveryAttempt_Outcome_CreatedAt`
  - Key: `(Outcome, CreatedAt DESC)`

---

### 8.4 EmailRateLimitLog (optional)

- `IX_RateLog_UserId_OccurredAt`
- `IX_RateLog_IpAddress_OccurredAt`
- `IX_RateLog_Endpoint_OccurredAt`

---

## 9) Retention & operational jobs (V1 policy)

### 9.1 Outbox retention

- keep successfully processed outbox rows for a short troubleshooting/replay window
- keep failed/poison rows longer for investigation
- purge policy must not remove data needed for active replay/debug windows

### 9.2 Delivery retention

- keep `EmailDelivery` long enough for ops, troubleshooting, and audit-adjacent needs
- redact or purge PII where policy requires it

### 9.3 Attempt retention

- keep `EmailDeliveryAttempt` long enough to support incident investigation and retry analysis
- attempts may be retained longer than transient outbox rows when useful operationally

### 9.4 Replay policy

- replay must be safe because correctness is guarded by `MessageKey` + delivery workflow state
- poison messages beyond max attempts move to terminal-failed / DLQ-style handling according to policy
- replay must not cause multiple successful sends for the same `MessageKey`

---

## 10) V2 hooks

- `NotificationSubscription(UserId, Type, CategoryId?, IsEnabled, CreatedAt)`
- `NotificationPreference(UserId, Channel, IsEnabled)`
- `EmailTemplate(TemplateKey, Version, Subject, BodyHtml, BodyText, UpdatedAt, UpdatedBy)`
- `DeadLetterMessage(MessageKey, EventType, PayloadJson, Reason, FailedAt)`

---

## 11) ADR candidates

- shared system outbox vs module-specific outbox
- retry/backoff schedule and max-attempt policy
- Redis dedup TTL and replay window alignment
- provider strategy and provider result classification
- PII storage/redaction/retention policy
- whether `EmailDeliveryAttempt` is mandatory in V1 or can be introduced incrementally

---

## 12) Partitioning readiness (V1/V2)

Notifications is a **bursty async processing module**.

### 12.1 Main scaling risk

The first scaling bottleneck is usually:

- worker throughput
- retry storms
- backlog drain time
- priority starvation

before database sharding becomes necessary.

### 12.2 Dominant access patterns

**Hot path**
- poll pending outbox by `(Status, Priority, NextRetryAt)`
- create/update delivery workflow
- append attempt history
- classify failures and schedule retry

**Operational reads**
- backlog inspection
- failed delivery inspection
- attempt history by message/delivery/user/template/time

### 12.3 Workload partitioning posture

Prefer workload partitioning before DB sharding:

- auth-critical lane vs low-priority lane
- retry lane vs fresh-pending lane
- event-family lane (`Auth.*`, `Content.*`, `Authorization.*`)
- hash-bucket ownership if needed later

### 12.4 Hotspot/skew risks

- many messages sharing similar `NextRetryAt`
- provider outage causing retry wave
- all workers competing for the same pending set
- auth-critical notifications being starved by low-priority traffic

### 12.5 V1 mitigations

- outbox-driven async delivery
- priority-aware polling
- durable dedup/correctness via workflow state
- Redis rate limit + duplicate-work reduction
- retry/backoff and failure visibility
- operational indexes for backlog and attempt analysis

### 12.6 Readiness signals

- outbox pending count
- outbox oldest pending age
- delivery lag for verification/reset
- queue depth ready/unacked
- worker P95/P99 processing latency
- failure/retry rate
- terminal-failed / DLQ rate
- dedupe hits / idempotency rejects
- backlog drain time after provider recovery

---

## 13) ERD (dbdiagram.io)

See: `../diagrams/erd/notifications-v1.dbml`

How to render:

1. Open dbdiagram.io
2. Copy DBML content from the file above
3. Paste into dbdiagram.io to view/export