# Identity — Observability & SLO Signals (V1)

Related:
- `../../../../architecture/arc42/04-runtime-view-v1.md`
- `../../../../architecture/arc42/11-replication-v1.md`
- `../../../../architecture/arc42/18-stream-processing-and-derived-state-v1.md`
- `../../../../architecture/arc42/19-stream-processing-runtime-v1.md`
- `../../../../decisions/adr-0013-outbox-and-delivery-semantics-v1.md`
- `../../../../decisions/adr-0016-authorization-model-rbac-abac-policies-v1.md`
- `../../../../decisions/adr-0018-transaction-boundaries-and-consistency-model-v1.md`
- `../../../../decisions/adr-0020-timeout-retry-and-failure-detection-policy-v1.md`
- `../../../../decisions/adr-0027-stream-processing-and-derived-state-policy-v1.md`
- `../../../../decisions/adr-0028-consumer-idempotency-replay-and-rebuild-policy-v1.md`

---

## 1) Critical endpoints (must measure)
- `POST /api/v1/auth/register`
- `POST /api/v1/auth/login`
- `POST /api/v1/auth/refresh`
- `POST /api/v1/auth/verify-email`
- `POST /api/v1/auth/resend-verification`
- `POST /api/v1/auth/forgot-password`
- `POST /api/v1/auth/reset-password`
- `POST /api/v1/auth/logout`
- `GET /api/v1/auth/me`

### Identity Admin endpoints

- `GET /api/v1/admin/identity/users`
- `GET /api/v1/admin/identity/users/{userId}`
- `GET /api/v1/admin/identity/users/{userId}/sessions`
- `GET /api/v1/admin/identity/users/{userId}/security-summary`
- `POST /api/v1/admin/identity/users/{userId}:activate`
- `POST /api/v1/admin/identity/users/{userId}:deactivate`
- `POST /api/v1/admin/identity/users/{userId}:mark-email-verified`
- `POST /api/v1/admin/identity/users/{userId}:revoke-sessions`
- `POST /api/v1/admin/identity/users/{userId}:lock` *(conditional if schema supports lock state)*
- `POST /api/v1/admin/identity/users/{userId}:unlock` *(conditional if schema supports lock state)*

---

## 2) SLIs (minimum)

### Availability & performance
- success/failure rates per endpoint (by status family and error code)
- latency percentiles (P95/P99) for auth endpoints
- error rate spikes (5xx/timeouts) and sustained degradation

### Abuse & rate limiting
- rate-limit trigger counts (by endpoint group)
- blocked requests by reason (burst, suspicious IP, user-based limits)

### Security anomalies (must watch)
- 401/403 spikes (segmented by endpoint)
- login failure spikes (bad password vs user not found vs locked)
- refresh failure spikes (expired/revoked/invalid token, rotation mismatch)
- resend/forgot spikes (possible enumeration or abuse attempts)

### Admin security signals
- admin permission-denied count by permission key
- admin ABAC-denied count by rule
- protected account mutation attempts
- self-action denial attempts
- admin activate/deactivate count
- admin mark-email-verified count
- admin revoke-sessions count
- admin lock/unlock count where supported
- admin mutation failure rate by error code
- unusual actor/target operation patterns

### Correctness signals (identity-critical)
- read-your-writes regressions (if measurable):
  - verify/reset succeeded but subsequent `/me` shows old state (should be ~0)
- token rotation integrity:
  - unexpected increase in "old token accepted" incidents (should be 0)
  - excessive refresh retries from clients (could indicate latency/timeouts)
- admin command truth/outbox commit failures
- admin deactivate succeeded but subsequent truth read still shows active user
- admin revoke-sessions succeeded but refresh token remains valid
- admin mark-email-verified succeeded but truth read still shows unverified
- Audit ingestion lag for `identity.admin.*`
- Audit dedupe hits for `identity.admin.*`

### Interpretation rule
Identity observability must distinguish:
- truth commit health
- downstream email/audit lag
- cache/derived-state lag
- retry/abuse/client-bug behavior
- cleanup/maintenance lag versus real live-security breakage

---

## 3) Correlation requirements

Propagate `X-Correlation-Id` into:
- API logs
- emitted events (event envelope `CorrelationId`)
- outbox records (recommended)
- worker logs (email sending)

Required log fields for Identity actions:
- correlationId + traceId
- actorUserId (when authenticated; avoid PII)
- endpoint + outcome classification (`success`, `invalid`, `already-used`, `replayed`, `revoked`, `rate-limited`, `suspicious`)
- never log tokens or raw secrets

### Async correlation requirements
For important flows, operators should be able to trace:
- request
- truth commit
- outbox record
- broker/consumer lag
- notification/audit outcome
- replay/remediation if one later occurs

---

## 4) Async workflow signals (Notifications + replication freshness)

### Email delivery SLIs
- email send success/failure (by template: verify/reset)
- retry volume and error classification (transient vs permanent)
- DLQ size and DLQ oldest age (if enabled)
- duplicate prevention indicators:
  - dedupe hits / unique-key conflicts in delivery log

### Replication freshness SLIs (Outbox → broker → Notifications consumer)
- outbox pending count (identity events)
- outbox oldest pending age (seconds)
- broker queue depth (ready/unacked) for notifications consumer
- consumer processing latency (P95/P99)

### Audit ingestion SLIs
- Audit ingestion success/failure for `identity.*`
- Audit ingestion success/failure for `identity.admin.*`
- Audit consumer retry count
- Audit dedupe hit count by `MessageId` / `AuditEventId`
- Audit ingestion lag:
  - `identity occurredAt -> audit appliedAt`
- DLQ/dead-state count and age for audit consumer where enabled

Interpretation:

- Audit lag must be visible.
- Audit lag must not redefine Identity truth.
- Duplicate audit events must be deduped and treated as idempotent success where appropriate.

### Time-to-consistency indicators
Measure where practical:
- `verification intent created -> email send attempted`
- `reset intent created -> email send attempted`
- `identity occurredAt -> downstream consumer appliedAt`

Operational expectation:
- identity flows do not block on email, but backlog/lag must be visible and recoverable.

### Interpretation rule
Operators should be able to tell whether:
- identity truth is healthy but email is delayed
- outbox publishing is lagging
- notification consumer is unhealthy
- dedupe is suppressing replay correctly
- ambiguity is only in delivery, not in security truth

---

## 5) Maintenance / cleanup workflow signals

These apply when Identity runs bounded workflows for:
- expired verification-token cleanup
- expired reset-token cleanup
- revoked/expired refresh-token cleanup
- auth artifact reconciliation
- login-history or auth summary archival

### Workflow health SLIs
- run success/failure count
- run duration
- records selected / processed / skipped / cleaned
- cleanup lag (age of oldest eligible-but-not-cleaned artifact)
- reconciliation mismatch count where implemented
- summary/archive freshness age where relevant

### Replay / repair indicators
- rerun count
- repeated mismatch on same bounded scope
- stale candidate reject count
- truth-resync trigger count where maintenance/report workflows detect drift

### Policy
Current account and token validity truth remains authoritative even when cleanup/reporting workflows lag or fail.
Maintenance lag is operationally important, but it must not weaken live security correctness.

### Strong stop condition
If a maintenance workflow is observed to:
- delete still-needed security artifacts
- reintroduce stale token/session assumptions
- publish stale maintenance output over fresher live truth

that is a release blocker.

---

## 6) Safe logging checks

Non-negotiable:
- periodic scans to ensure no token/PII leakage

Must never appear in logs:
- passwords
- access tokens
- refresh tokens
- verification/reset tokens
- raw emails beyond minimum necessary (prefer userId)

Audit/redaction posture:
- security/audit events include identifiers and correlationId, but redact sensitive payloads.

### Additional checks
- hashed/redacted token identifiers only
- no replay of sensitive payloads in error logs
- no provider/debug logs that accidentally expose reset or verification secrets

### Secret-bearing delivery-trigger payload checks

Approved delivery-trigger events may contain raw one-time tokens:

- `identity.verification_email_requested`
- `identity.password_reset_requested`

These payloads must be treated as secret-bearing.

Must never appear in:

- API logs
- worker logs
- exception logs
- traces
- metrics labels
- `Audit.Data`
- admin dashboards
- support tooling
- operational message viewers

Required checks:

- periodic scan for `RawVerificationToken`
- periodic scan for `RawResetToken`
- periodic scan for raw event payload logging
- alert if secret-bearing fields appear in logs, traces, metrics, or audit data

---

## 7) Release gates (recommended)

During rollout, gate on:
- auth endpoint 5xx/timeouts spikes
- sustained P99 latency regression on `/login` and `/refresh`
- rate-limit trigger spikes (may indicate misconfig or attack)
- refresh failure spike (especially reuse/rotation anomalies)
- outbox oldest pending age sustained growth for identity events
- notifications DLQ growth / DLQ oldest age breach
- cleanup/reconciliation failure spikes for security artifact maintenance if those workflows are enabled
- admin permission/ABAC denial spike after rollout
- protected account mutation attempts
- admin mutation truth/outbox commit failures
- Audit ingestion lag breach for `identity.admin.*`
- any measurable rise in "old token accepted" or stale-state acceptance (release blocker)
- any measurable read-your-writes regression on security-sensitive state (release blocker)
- any secret-bearing field detected in logs, traces, metrics, `Audit.Data`, dashboards, or support tooling (release blocker)

---

## 8) Operator questions this module must answer

Identity observability should help answer:

1. Did the security truth commit successfully?  
2. Is the problem in live auth truth, downstream email delivery, or maintenance cleanup?  
3. Are retries caused by abuse, latency, or client bugs?  
4. Did refresh reuse detection and revocation apply correctly?  
5. Is maintenance lag only operational, or is it beginning to threaten security hygiene?  
6. Is a failure in canonical identity truth, in outbox/consumer lag, or only in derived maintenance/reporting state?
