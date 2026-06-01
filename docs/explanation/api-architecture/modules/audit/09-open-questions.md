# Audit — Open Questions & ADR Hooks (V1)

## 1. Purpose

This document tracks unresolved questions, future decision points, and ADR hooks for the Audit module.

Audit V1 is intentionally simple:

* asynchronous ingestion
* `AuditLog`
* `AuditIngestion`
* `MessageId` deduplication
* investigation APIs
* lightweight dashboard queries from SQL

The questions below should not block the V1 core unless explicitly marked as V1-required.

---

## 2. V1 Required Decisions

These questions should be answered before or during V1 implementation.

---

## OQ-AUD-001 — Minimum audit coverage for V1

### Question

Which event families are mandatory for Audit V1?

### Candidate V1 baseline

Required:

* Authorization governance events:

  * `authorization.user_role_assigned`
  * `authorization.user_role_revoked`
  * `authorization.role_permission_granted`
  * `authorization.role_permission_revoked`

Recommended soon after:

* Identity security events
* Content lifecycle events
* Interaction moderation events
* Media governance events
* SEO route/metadata governance events
* Notification failure events where operationally important

### Decision needed

Define which source modules must emit audit-relevant messages in the first implementation slice.

### ADR hook

May become a module-level audit coverage ADR if scope grows.

---

## OQ-AUD-002 — Redaction rules by event type

### Question

For each consumed `EventType`, which fields are allowed in:

* `Summary`
* `MetadataJson`
* `HeadersJson`
* `RawPayloadJson`
* `BeforeJson`
* `AfterJson`
* `ChangesJson`

### Required V1 rule

Never store or return:

* passwords
* password hashes
* access tokens
* refresh tokens
* verification tokens
* reset tokens
* API keys
* session cookies
* raw authorization headers
* raw provider secrets
* sensitive PII beyond minimum necessary

### Decision needed

Define a redaction allowlist per event family.

### ADR hook

If redaction policy becomes system-wide and formal, create:

```text
ADR — Audit Redaction and Sensitive Payload Policy
```

---

## OQ-AUD-003 — AuditLog schema finalization

### Question

Which conceptual fields become physical V1 columns, and which remain JSON metadata?

### Candidate physical columns

Strong candidates:

* `AuditLogId`
* `PublicId`
* `MessageId`
* `EventType`
* `SourceModule`
* `Action`
* `ActionCategory`
* `AggregateType`
* `AggregateId`
* `AggregatePublicId`
* `AggregateVersion`
* `ResourceType`
* `ResourceId`
* `ActorInternalId`
* `ActorUserId`
* `Outcome`
* `Severity`
* `RiskLevel`
* `Summary`
* `CorrelationId`
* `OccurredAtUtc`
* `IngestedAtUtc`

Candidate JSON fields:

* `MetadataJson`
* `HeadersJson`
* `RawPayloadJson`
* `BeforeJson`
* `AfterJson`
* `ChangesJson`

### Decision needed

Choose the physical schema that supports V1 investigation queries without over-normalizing.

---

## OQ-AUD-004 — AuditIngestion schema finalization

### Question

Which consumer-side processing fields are required in V1?

### Candidate fields

* `AuditIngestionId`
* `PublicId`
* `MessageId`
* `EventType`
* `AggregateType`
* `AggregateId`
* `AggregatePublicId`
* `AggregateVersion`
* `CorrelationId`
* `SourcePriority`
* `SourceOccurredAtUtc`
* `SourcePublishedAtUtc`
* `ConsumerName`
* `Status`
* `AttemptCount`
* `FirstReceivedAtUtc`
* `LastAttemptAtUtc`
* `ProcessedAtUtc`
* `DeadLetteredAtUtc`
* `LastErrorCode`
* `LastErrorMessage`
* `LastErrorClass`
* `CreatedAtUtc`
* `UpdatedAtUtc`

### Decision needed

Decide whether `AuditIngestion.MessageId` is unique and how duplicate deliveries update status.

Recommended:

```text
AuditIngestion.MessageId should be unique.
AuditLog.MessageId must be unique.
```

---

## OQ-AUD-005 — Audit lag threshold

### Question

What amount of Audit lag is acceptable in V1?

### Candidate SLOs

Possible starting points:

* P95 occurred-to-ingest lag under normal load: under 60 seconds
* P99 occurred-to-ingest lag under normal load: under 5 minutes
* high-risk governance events should have tighter operational expectations

### Decision needed

Define warning and critical thresholds for:

* occurred-to-ingest lag
* publish-to-ingest lag
* failed ingestion age
* DLQ oldest age
* queue backlog

### ADR hook

May become part of a system-wide async flow SLO decision.

---

## OQ-AUD-006 — API permissions

### Question

Which exact permissions protect Audit endpoints?

### Candidate permissions

* `Audit.Read`
* `Audit.ReadDashboard`
* `Audit.ReadIngestion`
* `Audit.ReadOperationalHealth`
* `Audit.ReadSensitive`

Future permissions:

* `Audit.Alert.Read`
* `Audit.Alert.Manage`
* `Audit.Replay.Execute`
* `Audit.Retention.Read`
* `Audit.Retention.Manage`
* `Audit.Export.Execute`

### Decision needed

Map permissions to built-in roles:

* Admin
* Moderator
* Author
* User

Recommended V1 default:

```text
Only Admin receives Audit.Read by default.
Audit.ReadSensitive should be more restricted than Audit.Read.
```

---

## 3. V1.1 / Near-Future Questions

These are likely useful after V1 core ingestion and queries are stable.

---

## OQ-AUD-007 — Audit dashboard scope

### Question

Which dashboard panels are valuable enough for V1.1?

### Candidate panels

* total audit events by time window
* events by source module
* events by severity
* events by risk level
* recent high-risk events
* failed ingestion count
* oldest failed ingestion age
* duplicate ingestion count
* top actors by high-risk actions
* top resources by audit activity

### Decision needed

Decide which panels are direct SQL queries and which may later require cache/materialization.

---

## OQ-AUD-008 — AuditAlert introduction

### Question

Should `AuditAlert` be introduced in V1.1?

### Candidate alert types

* critical authorization change
* repeated login failure spike
* refresh token reuse detected
* audit ingestion failure spike
* DLQ oldest age breach
* high-risk action count threshold
* suspicious audit browsing pattern

### Decision needed

Decide whether V1.1 needs persistent alert state or dashboard-only warning panels are enough.

### ADR hook

If introduced, define:

```text
ADR — Audit Alerting and Admin Notification Policy
```

---

## OQ-AUD-009 — Realtime vs window-based alerting

### Question

Which alert rules should run immediately after `AuditLog` insertion, and which should run over a time window?

### Realtime candidates

* critical permission granted
* account locked/unlocked by admin
* refresh token reuse detected
* audit ingestion dead-lettered

### Window-based candidates

* repeated login failures
* failed ingestion spike
* high-risk action spike
* suspicious audit read pattern
* daily audit summary

### Decision needed

Define which rules are event-driven and which are batch/window-based.

---

## OQ-AUD-010 — Notification integration

### Question

Should Audit emit notification requests when critical alerts occur?

### Candidate future flow

```text
AuditAlert created
    ↓
Audit emits AuditAlertRaised through Outbox
    ↓
Notifications consumes event
    ↓
Notifications sends email/admin notification
```

### Decision needed

Define which alert types justify delivery through Notifications.

### Guardrail

Audit must not send emails directly.

---

## OQ-AUD-011 — Audit-of-audit

### Question

Should sensitive Audit API usage itself be audited?

### Candidate audit-of-audit events

* sensitive audit detail viewed
* raw/redacted payload requested
* audit export requested
* replay/remediation executed
* alert acknowledged/resolved
* retention policy changed

### Decision needed

Define whether audit-of-audit is required for V1.1 or later.

### Guardrail

Avoid recursive uncontrolled audit loops.

---

## 4. Future Batch, Replay, and Reconciliation Questions

These are not required for V1 core delivery.

---

## OQ-AUD-012 — Replay and remediation controls

### Question

Should Audit expose admin replay/remediation controls?

### Candidate future endpoints

* `POST /api/v1/admin/audit/replay`
* `POST /api/v1/admin/audit/failed-ingestions/{publicId}/replay`

### Decision needed

Define:

* who can run replay
* what scope is allowed
* whether dry-run is required
* how to preserve original `MessageId`
* how to audit the replay action
* how to prevent duplicate evidence

### ADR hook

If introduced, create:

```text
ADR — Audit Replay and Remediation Control Policy
```

---

## OQ-AUD-013 — Audit completeness reconciliation

### Question

Do we need a workflow that compares expected auditable events against persisted `AuditLog` records?

### Candidate scopes

* Authorization governance events
* Identity security events
* Content publish/unpublish events
* Interaction moderation events

### Decision needed

Define:

* source of expected event set
* bounded time window
* mismatch output format
* replay candidate generation
* operator remediation flow

### Guardrail

Reconciliation output is derived. It must not replace `AuditLog`.

---

## OQ-AUD-014 — Batch summaries and digests

### Question

Should Audit generate daily/hourly summaries or digests?

### Candidate outputs

* `AuditDailySummary`
* `AuditDigest`
* `AuditRiskReport`
* `AuditGovernanceSummary`

### Decision needed

Define:

* summary window
* input boundary
* output contract
* rerun behavior
* publication/cutover behavior
* whether Notifications sends digest

### ADR hook

If summaries become important, create:

```text
ADR — Audit Digest and Summary Materialization Policy
```

---

## OQ-AUD-015 — Archive and retention workflow

### Question

How long are audit logs retained in the primary store?

### Sub-questions

* How long does `AuditLog` stay in primary SQL?
* How long does `AuditIngestion` stay in primary SQL?
* Are older records archived?
* Are archived records queryable through the same API?
* Who can purge?
* Is purge allowed at all?
* Does purge require approval?
* Does purge create audit-of-audit evidence?

### Candidate policies

* keep all V1 audit evidence in primary SQL during early project phase
* introduce archive only when table size or compliance policy requires it
* no purge without explicit retention ADR

### ADR hook

Create:

```text
ADR — Audit Retention, Archive, and Purge Policy
```

---

## 5. Future Storage, Cache, and Partitioning Questions

---

## OQ-AUD-016 — Storage and indexing strategy

### Question

When is normal SQL indexing no longer enough?

### V1 posture

Use SQL Server with focused indexes.

Candidate indexes:

* `MessageId`
* `PublicId`
* `(SourceModule, OccurredAtUtc)`
* `(ActorUserId, OccurredAtUtc)`
* `(ResourceType, ResourceId, OccurredAtUtc)`
* `(CorrelationId, OccurredAtUtc)`
* `(RiskLevel, OccurredAtUtc)`
* `(Severity, OccurredAtUtc)`
* `(Action, OccurredAtUtc)`

### Decision needed

Define the initial index set based on V1 API access patterns.

### ADR hook

If storage pressure grows, revisit:

```text
ADR — Audit Storage and Indexing Evolution
```

---

## OQ-AUD-017 — Redis cache for Audit dashboard

### Question

Should Audit introduce Redis for dashboard acceleration?

### Candidate cache targets

* dashboard summary
* recent high-risk events
* module/action metadata
* TTL-bound alert dedupe hints

### V1 posture

No Redis required for Audit core.

### Decision needed

Introduce Redis only if dashboard query cost justifies it.

### Guardrail

Redis must not become Audit evidence truth.

---

## OQ-AUD-018 — Partitioning strategy for Audit

### Question

When should Audit introduce stronger partitioning?

### Candidate future partitioning

* time-range partitioning for `AuditLog`
* worker lane partitioning by source module
* worker lane partitioning by risk priority
* queue partitioning by module/category/priority
* batch-window partitioning for summaries/archive/reconciliation

### Signals

* audit query P95/P99 degradation
* growing `AuditLog` table/index pressure
* audit consumer lag
* oldest uningested message age
* retry/DLQ pressure
* security audit delayed by high-volume non-security events
* archive/report windows becoming too slow

### ADR hook

If introduced, create:

```text
ADR — Audit Partitioning and Worker Lane Strategy
```

---

## OQ-AUD-019 — Warehouse or analytics store

### Question

Should Audit data later flow into a warehouse or analytical store?

### Candidate uses

* long-window compliance reporting
* governance analytics
* security trend analysis
* product/admin behavior analysis
* historical report generation

### V1 posture

Out of scope.

### Guardrail

Warehouse outputs must remain downstream derived outputs and must not replace `AuditLog` evidence truth.

---

## 6. Future Evidence Integrity Questions

---

## OQ-AUD-020 — Tamper-evident strategy

### Question

Should Audit implement tamper-evident evidence?

### Candidate approaches

* per-record hash
* hash chain
* per-partition hash chain
* periodic external anchoring
* signed archive snapshots

### V1 posture

`Hash` and `PrevHash` are reserved hooks only.

Tamper-evident enforcement is out of scope for V1.

### Decision needed

Define whether V2 needs tamper-evident guarantees and what threat model it addresses.

### ADR hook

Create:

```text
ADR — Audit Tamper-Evident Evidence Strategy
```

---

## OQ-AUD-021 — Correction model

### Question

How should Audit correct wrong mapping, classification, or redaction mistakes?

### Candidate approaches

* append corrective audit record
* mark original as superseded by correction
* redaction-only remediation with restricted operator flow
* correction reason and operator identity

### V1 posture

No manual correction API.

### Guardrail

Do not silently rewrite historical evidence.

### ADR hook

If introduced, create:

```text
ADR — Audit Correction and Redaction Remediation Policy
```

---

## OQ-AUD-022 — Sensitive payload viewing

### Question

Should any admin be able to view redacted payload details?

### Candidate approach

* hide raw payload entirely in V1
* expose only normalized metadata
* future endpoint requires `Audit.ReadSensitive`
* audit sensitive reads through audit-of-audit

### Decision needed

Define whether V1 returns `RawPayloadJson` at all.

Recommended V1 posture:

```text
Do not expose raw payload through API by default.
```

---

## 7. Future API Questions

---

## OQ-AUD-023 — Export API

### Question

Should Audit support export?

### Candidate formats

* CSV
* JSON
* PDF report
* archive bundle

### V1 posture

Out of scope.

### Guardrails

If introduced:

* require strong permission
* enforce bounded scope
* rate limit
* audit the export action
* redact sensitive fields
* do not allow unbounded export

---

## OQ-AUD-024 — Manual evidence insertion

### Question

Should admins ever be allowed to manually insert audit evidence?

### Recommended answer

No for V1.

Manual evidence insertion weakens trust unless governed by a strict correction model.

### Guardrail

If introduced later, it must be represented as a corrective/manual evidence record, not as if it came from the original source event.

---

## OQ-AUD-025 — Public/user-facing audit history

### Question

Should end users see their own audit/security history?

### Candidate examples

* login history
* password changed
* email verified
* sessions revoked

### V1 posture

Out of Audit module scope.

This may belong to Identity or a user-security activity view, not Admin Audit.

### Guardrail

Do not expose admin/governance audit logs to normal users.

---

## 8. Decision Tracking Summary

| ID         |                              Topic | V1 Required? | Likely Phase                            |
| ---------- | ---------------------------------: | -----------: | --------------------------------------- |
| OQ-AUD-001 |             Minimum audit coverage |          Yes | V1                                      |
| OQ-AUD-002 |      Redaction rules by event type |          Yes | V1                                      |
| OQ-AUD-003 |       AuditLog schema finalization |          Yes | V1                                      |
| OQ-AUD-004 | AuditIngestion schema finalization |          Yes | V1                                      |
| OQ-AUD-005 |                Audit lag threshold |          Yes | V1                                      |
| OQ-AUD-006 |                    API permissions |          Yes | V1                                      |
| OQ-AUD-007 |                    Dashboard scope |      Partial | V1 / V1.1                               |
| OQ-AUD-008 |            AuditAlert introduction |           No | V1.1+                                   |
| OQ-AUD-009 |          Realtime vs window alerts |           No | V1.1+                                   |
| OQ-AUD-010 |           Notification integration |           No | V1.1+                                   |
| OQ-AUD-011 |                     Audit-of-audit |           No | V1.1+                                   |
| OQ-AUD-012 |        Replay/remediation controls |           No | V1.1+                                   |
| OQ-AUD-013 |        Completeness reconciliation |           No | V1.1+                                   |
| OQ-AUD-014 |            Batch summaries/digests |           No | V2                                      |
| OQ-AUD-015 |            Retention/archive/purge |           No | V2                                      |
| OQ-AUD-016 |          Storage/indexing strategy |          Yes | V1                                      |
| OQ-AUD-017 |          Redis cache for dashboard |           No | V1.1+                                   |
| OQ-AUD-018 |              Partitioning strategy |           No | V2                                      |
| OQ-AUD-019 |          Warehouse/analytics store |           No | V2+                                     |
| OQ-AUD-020 |            Tamper-evident strategy |           No | V2+                                     |
| OQ-AUD-021 |                   Correction model |           No | V2                                      |
| OQ-AUD-022 |          Sensitive payload viewing |      Partial | V1 / V1.1                               |
| OQ-AUD-023 |                         Export API |           No | V2                                      |
| OQ-AUD-024 |          Manual evidence insertion |           No | Probably never / strict correction only |
| OQ-AUD-025 |   Public/user-facing audit history |           No | Separate Identity/User Security scope   |

---

## 9. Summary

Audit V1 should not be blocked by advanced reporting, archive, alerting, replay, cache, partitioning, or tamper-evident features.

The V1 decisions that matter most are:

1. Which events are audited first.
2. What fields are physically stored.
3. How redaction works per event type.
4. What `AuditLog` and `AuditIngestion` schemas look like.
5. What lag thresholds are acceptable.
6. Which admin permissions protect the API.
7. Which indexes support the initial query surface.

Everything else should remain an explicit future extension path with guardrails.

The guiding principle:

```text
Audit V1 should be small, trustworthy, observable, and extension-ready.
```
