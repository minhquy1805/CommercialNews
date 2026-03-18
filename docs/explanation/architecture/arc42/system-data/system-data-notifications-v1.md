# System Data Model — Notifications (V1)

> **Recommended file:** `explanation/architecture/arc42/system-data/system-data-notifications-v1.md`  
> **Module:** Notifications  
> **Purpose:** Deliver system emails (verification/reset/optional new-article) **reliably** without blocking core flows, using **Outbox + delivery state machine**.

---

## 0) Data System fit (V1)

Notifications is a **non-critical side-effect module** that must not break core flows.

- **Truth store for async intent:** `OutboxMessage` (durable, retry-driven)
- **Delivery workflow:** `EmailDelivery` (visibility + dedup + provider outcomes)
- **Transport:** broker/worker processes outbox; email provider is external
- **Redis:** rate limiting + idempotency/dedup (required by constraints)

**Non-negotiables (from Quality Requirements)**
- Register/verify/forgot/reset **must work even if email provider is down**
- Retries must be **safe** (idempotent; no duplicate successful sends)
- Logs/events must avoid secrets/PII leakage (minimum necessary payload)

---

## 1) Scope & boundaries (V1)

### In scope (V1)
- System emails:
  - verification email
  - password reset email
  - optional “new article” notification
- Outbox-driven processing (retry + backlog visibility)
- Delivery state machine (queued/sent/failed + attempts)

### Out of scope (V2+)
- Template versioning storage (`EmailTemplate`)
- Subscriptions/preferences for “new article” notifications
- DLQ in DB as a first-class store (`DeadLetterMessage`) if desired

### Cross-module dependencies
- Identity triggers verification/reset events; Notifications sends emails
- Content triggers article published events (optional notifications)
- Notifications does **not** own Identity or Content state; it reacts to events.

---

## 2) Capability → Entity mapping

### 2.1 System email (verification, reset, optional new-article)
**Entities**
- `OutboxMessage` — durable intent + retry scheduling
- `EmailDelivery` — email workflow state machine and provider attempts

**V2 hooks**
- `EmailTemplate` (versioned templates)
- `NotificationSubscription` / `NotificationPreference`

### 2.2 Abuse prevention (rate-limit)
- Enforced primarily in **Redis/app layer**
- Optional DB visibility via:
  - `EmailRateLimitLog`, or
  - analysis using `EmailDelivery` outcomes (less explicit)

---

## 3) Workload & hot paths (V1) (DDIA Ch3)

- Writes are moderate (register/forgot/resend spikes possible during abuse)
- Reads are mostly operational (dashboards, investigating failures)
- The key “hot path” is **worker pulling backlog efficiently**:
  - pending messages ordered by priority + next retry time

---

## 4) Dataflows (V1) — REST / DB / Broker (DDIA Ch4)

> Principle: core flows write business state + outbox in the same transaction; delivery is async.

### 4.1 Email verification request (Identity → Notifications)
**Sync (Identity API → DB)**
- Create `UserAccount` (unverified)
- Create `EmailVerificationToken` (store token hash in Identity)
- Insert `OutboxMessage` event:
  - `EventType = Auth.EmailVerificationRequested`
  - payload contains only identifiers and template data (no raw token)

**Async (Worker → Provider)**
- Worker reads Outbox, creates/updates `EmailDelivery`
- Sends email via provider

**Failure behavior**
- Provider failure does not break registration
- Retries safe; duplicate successful sends are prevented

### 4.2 Resend verification (rate-limited)
- Rate limit in Redis/app
- Insert new OutboxMessage (new MessageKey) only if allowed
- Policy: older verification tokens can remain valid or be revoked (Identity policy)

### 4.3 Forgot password / Reset email
- Identity inserts Outbox event:
  - `Auth.PasswordResetRequested`
- Notifications sends reset email async
- Token raw value is only in email; DB stores only hash

### 4.4 Optional: New article notification (Content → Notifications)
- Content emits `Content.ArticlePublished`
- Notifications may send subscriber emails (V2 subscription model)

---

## 5) Invariants (V1 rules)

### 5.1 Non-blocking core flows
- Register/reset/publish flows only write Outbox; sending is async.
- Failures in Notifications must not fail core writes.

### 5.2 Idempotency & retry safety
- Each email-trigger event has a unique `MessageKey`.
- `EmailDelivery` allows **at most one successful send** per `MessageKey`.
- Worker may process same outbox multiple times; must check state before sending.

### 5.3 Delivery state machine (monotonic)
- `Queued → Sending → Sent | Failed`
- `Sent` is terminal.
- `Failed` may be terminal or may allow retry depending on policy; do not flip-flop.

### 5.4 Privacy & safety
- Never store raw secrets/tokens in Outbox payload.
- LastError fields must be redacted (no tokens, no stack traces with secrets).
- Templates must sanitize variables (avoid injecting untrusted HTML).

### 5.5 Burst handling
- Backpressure is allowed for non-critical emails (new-article can lag).
- Verification/reset should have higher priority than marketing-style sends.

---

## 6) Redis plan (Notifications V1) — required

### 6.1 Rate limiting (shared with Identity policy)
Keys (examples):
- `cn:rl:resend:email:{emailNorm}:{window}`
- `cn:rl:forgot:email:{emailNorm}:{window}`
- `cn:rl:forgot:ip:{ip}:{window}`

### 6.2 Dedup for consumers (at-least-once safe)
- `cn:msg:processed:{MessageKey}` TTL 7–30 days  
(align with max replay window / troubleshooting retention)

### 6.3 Optional idempotency for REST commands
If endpoints may be retried (timeouts):
- `cn:idem:{operation}:{idempotencyKey}` TTL 10–60 minutes  
Operations: register, resend, forgot.

---

## 7) Entities (Logical schema) — SQL Server (V1)

### 7.1 `OutboxMessage`
> If you already have a shared system outbox, reuse it. This schema is recommended for Notifications processing.

| Field | Type | Null | Default | Notes |
|---|---|---:|---|---|
| OutboxId | BIGINT IDENTITY | NO |  | PK |
| MessageKey | UNIQUEIDENTIFIER | NO | NEWID() | idempotency key |
| EventType | NVARCHAR(120) | NO |  | `Auth.EmailVerificationRequested`, ... |
| AggregateType | NVARCHAR(60) | NO |  | `User`, `Article` |
| AggregateId | NVARCHAR(100) | NO |  | string for cross-module ids |
| PayloadJson | NVARCHAR(MAX) | NO |  | **minimal + redacted** |
| Priority | TINYINT | NO | `5` | 1=highest |
| OccurredAt | DATETIME2(3) | NO | SYSUTCDATETIME() | |
| Status | VARCHAR(20) | NO | `'Pending'` | Pending/Processing/Processed/Failed |
| RetryCount | INT | NO | `0` | |
| NextRetryAt | DATETIME2(3) | YES |  | backoff |
| LastError | NVARCHAR(500) | YES |  | redacted |
| ProcessedAt | DATETIME2(3) | YES |  | |

**V2 hooks**
- `PartitionKey` (burst control)
- `ExpiresAt` (drop stale messages)

---

### 7.2 `EmailDelivery`
> Delivery workflow visibility + dedup (one workflow per MessageKey).

| Field | Type | Null | Default | Notes |
|---|---|---:|---|---|
| EmailDeliveryId | BIGINT IDENTITY | NO |  | PK |
| MessageKey | UNIQUEIDENTIFIER | NO |  | link to Outbox |
| UserId | BIGINT | YES |  | optional |
| ToEmail | NVARCHAR(320) | NO |  | recipient |
| TemplateKey | NVARCHAR(80) | NO |  | VerifyEmail/ResetPassword/NewArticle |
| TemplateVersion | INT | YES |  | V2-ready |
| Subject | NVARCHAR(200) | YES |  | optional |
| Provider | VARCHAR(30) | NO | `'smtp'` | smtp/sendgrid… |
| Status | VARCHAR(20) | NO | `'Queued'` | Queued/Sending/Sent/Failed |
| AttemptCount | INT | NO | `0` | |
| LastAttemptAt | DATETIME2(3) | YES |  | |
| SentAt | DATETIME2(3) | YES |  | terminal |
| LastError | NVARCHAR(500) | YES |  | redacted |
| CorrelationId | NVARCHAR(100) | YES |  | trace |
| CreatedAt | DATETIME2(3) | NO | SYSUTCDATETIME() | |
| UpdatedAt | DATETIME2(3) | NO | SYSUTCDATETIME() | |

**V2 hooks**
- `ProviderMessageId`
- `OpenCount`, `ClickCount` (engagement tracking)

---

### 7.3 (Optional) `EmailRateLimitLog`
| Field | Type | Null | Default | Notes |
|---|---|---:|---|---|
| RateLogId | BIGINT IDENTITY | NO |  | PK |
| UserId | BIGINT | YES |  | |
| Endpoint | NVARCHAR(60) | NO |  | register/forgot/resend |
| ToEmail | NVARCHAR(320) | YES |  | |
| IpAddress | NVARCHAR(45) | YES |  | |
| Allowed | BIT | NO | `1` | decision |
| Reason | NVARCHAR(120) | YES |  | RateLimited/Blocked |
| OccurredAt | DATETIME2(3) | NO | SYSUTCDATETIME() | |
| CorrelationId | NVARCHAR(100) | YES |  | |

---

## 8) Constraints & indexes — Notifications (V1)

### 8.1 OutboxMessage
**Constraints**
- PK: `PK_OutboxMessage(OutboxId)`
- UNIQUE: `UQ_Outbox_MessageKey(MessageKey)`

**Indexes (backlog + processing)**
- `IX_Outbox_Status_Priority_NextRetryAt`
  - Key: `(Status, Priority, NextRetryAt)`
- `IX_Outbox_OccurredAt`
  - Key: `(OccurredAt DESC)`
- `IX_Outbox_Aggregate`
  - Key: `(AggregateType, AggregateId, OccurredAt DESC)`

### 8.2 EmailDelivery
**Constraints**
- PK: `PK_EmailDelivery(EmailDeliveryId)`
- UNIQUE (dedup): `UQ_EmailDelivery_MessageKey(MessageKey)`
- CHECK:
  - `Status IN ('Queued','Sending','Sent','Failed')`
  - `AttemptCount >= 0`

**Indexes (ops/dashboards)**
- `IX_EmailDelivery_Status_LastAttemptAt`
  - Key: `(Status, LastAttemptAt DESC)`
- `IX_EmailDelivery_UserId_CreatedAt`
  - Key: `(UserId, CreatedAt DESC)`
- `IX_EmailDelivery_TemplateKey_CreatedAt`
  - Key: `(TemplateKey, CreatedAt DESC)`
- `IX_EmailDelivery_ToEmail_CreatedAt` (optional; PII)
  - Key: `(ToEmail, CreatedAt DESC)`

### 8.3 EmailRateLimitLog (optional)
- `IX_RateLog_UserId_OccurredAt`
- `IX_RateLog_IpAddress_OccurredAt`
- `IX_RateLog_Endpoint_OccurredAt`

---

## 9) Retention & operational jobs (V1 policy)

### 9.1 Outbox retention
- Keep successful outbox messages for a short window (troubleshooting/replay), then purge.
- Keep failed/DLQ messages longer for investigations.

### 9.2 Delivery retention
- Keep EmailDelivery records long enough for ops + audit needs.
- Purge/redact PII fields if required by privacy policy.

### 9.3 Replay policy
- Reprocessing must be safe due to idempotency (MessageKey dedup).
- Poison messages beyond max attempts move to Failed/DLQ and require manual action.

---

## 10) V2 hooks (Notifications)
- New-article notifications:
  - `NotificationSubscription(UserId, Type, CategoryId?, IsEnabled, CreatedAt)`
  - `NotificationPreference(UserId, Channel, IsEnabled)`
- Template safety/versioning:
  - `EmailTemplate(TemplateKey, Version, Subject, BodyHtml, BodyText, UpdatedAt, UpdatedBy)`
- DLQ as DB entity:
  - `DeadLetterMessage(MessageKey, EventType, PayloadJson, Reason, FailedAt)`

---

## 11) ADR candidates
- Outbox strategy: shared system outbox vs module-specific outbox
- Retry/backoff policy: attempt thresholds and backoff schedule
- Redis policy: rate-limit keys + dedup TTL window
- Provider strategy: SMTP vs provider API and provider id handling
- PII policy: what to store in EmailDelivery and how long

---

## 12) Partitioning Readiness (V1/V2)

> This section captures **partitioning and workload-partitioning readiness** for Notifications.
> V1 remains **non-sharded by default**; priority is reliable async processing and backlog control without impacting core flows.

### 12.1 Why Notifications is a partitioning-risk module

Notifications is a **bursty async side-effect module**:

* register/forgot/resend spikes can create backlog quickly
* provider slowdowns/failures amplify retries and queue pressure
* worker pull efficiency becomes the operational hot path

**V1 principle:** optimize for **reliable backlog processing** and **safe retries** before DB sharding.

---

### 12.2 Primary access patterns (V1)

**Hot path (worker)**

* pull pending outbox messages by `(Status, Priority, NextRetryAt)`
* process and update delivery state machine
* retry failed deliveries safely

**Operational reads**

* backlog inspection
* failed deliveries investigation
* delivery status by message/user/template/time

**Core flows dependency rule**

* register/verify/forgot/reset/publish only write business state + outbox
* email sending is async and must not block core flows

---

### 12.3 Secondary-index-heavy queries (present and future)

**V1**

* Outbox polling by status/priority/retry time
* EmailDelivery operational queries by status/time/user/template
* failure investigations (`Failed`, `LastAttemptAt`, `MessageKey`)

**V2+**

* subscription/preference queries
* campaign/new-article delivery analytics
* DLQ search and replay tooling

**Implication**

* Notifications is operational-query-heavy, but the dominant scale risk is usually **worker throughput and backlog**, not immediate truth-store sharding.

---

### 12.4 Candidate partitioning strategy (future)

Partitioning choices should be driven by **workflow shape** (queue/retry/backoff), not just table size.

#### A) `OutboxMessage` (durable async intent)

**Likely fit (future):** **workload lanes first**, then optional data partitioning

* lane ownership by logical shard (e.g., hash bucket / priority class / event family)
* optional DB partitioning later if backlog volume or retention pressure grows

**V2 hook already present**

* `PartitionKey` (burst control) is a strong future-ready field

#### B) `EmailDelivery` (workflow state / dedup)

**Likely fit:** defer DB partitioning in V1

* prioritize dedup uniqueness (`MessageKey`) and operational indexes
* scale reads via ops queries/indexes before sharding

#### C) Retry / DLQ workloads

**Likely fit:** workload partitioning (retry lanes, priority lanes)

* separate urgent auth emails from non-critical sends (e.g., new-article notifications)

---

### 12.5 Hotspot and skew risks (V1)

#### A) Workload hotspots (most important)

* many pending messages with similar `NextRetryAt`
* retry storms after provider outage/recovery
* all workers competing for the same pending set

#### B) Priority skew

* verification/reset should outrank lower-priority sends
* poor scheduling can cause starvation or latency spikes for security-related emails

#### C) Operational read skew

* repeated dashboard/ops queries on failed/sending states during incidents

---

### 12.6 V1 mitigations (no sharding yet)

CommercialNews V1 already has the right baseline mitigations:

* **Outbox + async delivery** (core flows remain non-blocking)
* **Idempotent processing** with `MessageKey` and dedup checks
* **Retry/backoff + failure visibility** (`RetryCount`, `NextRetryAt`, `LastError`)
* **Priority-aware polling** (`Status`, `Priority`, `NextRetryAt`)
* **Redis rate-limit + dedup keys** (abuse and at-least-once safety)
* **Operational indexes** for backlog and delivery state visibility

These are preferred before introducing shard complexity.

---

### 12.7 V2+ scale options (selective)

Introduce stronger partitioning only when sustained signals justify it.

#### Option A — Worker lanes / ownership partitioning (recommended first)

Partition processing work by logical lane, for example:

* priority lane (auth-critical vs non-critical)
* hash bucket (`MessageKey` / `AggregateId`)
* event family (`Auth.*`, `Content.*`)

**Why first**

* directly improves throughput and contention control
* lower complexity than DB sharding
* aligns with API + Worker topology

#### Option B — Retry lanes / backoff buckets

Separate retry-heavy workloads from fresh pending workloads to avoid queue starvation and retry storms overwhelming normal delivery.

#### Option C — Data partitioning for outbox/delivery tables (later)

Consider when:

* retention/purge/replay becomes operationally expensive
* polling/update contention persists despite lane ownership and indexing
* recovery/rebuild windows become too slow

---

### 12.8 Rebalancing and routing readiness (future)

Notifications is a strong candidate for **workload rebalancing** before data-store sharding.

**Likely rebalance unit**

* worker lane / ownership shard
* retry lane / priority lane

**Routing requirement**

* authoritative mapping for `lane -> worker owner`
* safe reassignment with throttling and observability

**Guardrail**

* rebalance/scale changes must not worsen:

  * auth email lag (verification/reset)
  * outbox oldest pending age
  * consumer failure/DLQ rates

---

### 12.9 Partition-readiness observability signals (Notifications)

Use existing V1 measurement signals to decide when stronger partitioning is needed:

* outbox pending count
* outbox oldest pending age
* queue depth (ready/unacked)
* consumer processing latency P95/P99
* consumer failure/retry rate
* DLQ rate / DLQ oldest age (if enabled)
* dedupe hits / idempotency rejects
* delivery lag for verification/reset emails vs lower-priority sends
* provider failure bursts and recovery backlog drain time

**Scale trigger (policy-level)**
Consider stronger workload/data partitioning when sustained pressure causes:

* backlog/lag that does not self-recover after provider recovery
* retry storms causing contention or starvation
* auth email latency degradation despite current indexing + backoff policy
* replay/recovery operations becoming operationally unsafe

---

## 13) ERD (dbdiagram.io)

See: `../diagrams/erd/notifications-v1.dbml`

How to render:

1. Open dbdiagram.io
2. Copy DBML content from the file above
3. Paste into dbdiagram.io to view/export
