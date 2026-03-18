# Notifications Module — API Architecture (V1)

**Purpose**
- Deliver system notifications (email) reliably without blocking core workflows:
  - email verification
  - password reset
  - (optional) new-article notifications
- Keep **delivery truth** explicit and authoritative for notification execution state.
- Support retry, replay, stream-style backlog recovery, and bounded reconciliation workflows without turning provider outcomes or dashboards into hidden business truth.

**Why this module is critical**
- Email providers fail in real life; the system must handle partial failure.
- Retries must not cause duplicate emails (idempotency requirement).
- Templates must avoid leaking tokens/PII; logs must be redacted.
- Burst sending can happen (new-article notifications, mass resend requests).
- Provider timeout or ambiguity must not corrupt delivery-state truth.
- Derived delivery reports may exist, but they must remain subordinate to canonical notification delivery records.
- At-least-once event delivery, replay, worker restarts, and redelivery are normal runtime conditions for this module.

**Primary consumers**
- Worker component (email sending handlers)
- Identity module (produces verification/reset events)
- Content module (produces publish events for optional notifications)
- Audit module (optional: audit notification outcomes)
- Future reporting/reconciliation workflows for delivery health and backlog analysis

**Non-goals (V1)**
- Full user subscription management (opt-in/out, topics) (V2)
- Multi-channel notifications (push/SMS) (future)
- Complex campaign management
- Treating provider callbacks, dashboards, or caches as delivery truth
- Global coordination-heavy sender ownership
- Requiring synchronous provider success before upstream business success is returned

**Hard constraints**
- Notifications must not block core flows (register/forgot/publish).
- Delivery must be retry-safe and idempotent (dedupe).
- Templates must not leak tokens/PII.
- Success/failure/backlog must be observable.
- Delivery truth is **primary and authoritative** for send attempts, dedupe outcome, retry/dead state, and operational delivery status.
- Async notification processing is **at-least-once**; duplicate delivery, replay, stale retry, and worker restart must be tolerated safely.
- Derived outputs must remain **observable**, **rebuildable where practical**, and **subordinate to notification delivery truth**.
- Replay/reconciliation workflows must be **bounded**, **observable**, and **rerun-safe**.
- Partial or candidate reporting/materialized outputs must not be exposed as if they were final authoritative delivery truth.
- Safe non-progress is preferable to unsafe duplicate-send behavior.

**Truth vs derived posture**
- **Truth**:
  - delivery records
  - dedupe outcome
  - attempt count
  - retry/dead/ambiguous/suppressed state
  - last known provider result classification
  - local delivery-state transitions
- **Derived**:
  - dashboards and summaries
  - backlog reports
  - reconciliation outputs
  - provider health rollups
  - reporting/materialized delivery views
  - resend/remediation candidate sets
- **Rule**: Notifications truth answers what the system attempted and what operational state it reached; provider-side ambiguity, dashboards, and reports may lag and be rebuilt, but do not replace canonical delivery truth.

**Stream / async posture**
- Notifications is a downstream side-effect executor in the standard V1 model:
  1. upstream module commits truth
  2. outbox/event is published asynchronously
  3. Notifications consumer receives events at least once
  4. business-intent dedupe prevents harmful duplicate sends
  5. delivery-state truth converges through retry, replay, dead-letter handling, and remediation
- Notifications must distinguish:
  - duplicate event processing
  - legitimate new intent
  - ambiguous provider outcome
  - stale retry against newer terminal state
- Ordering is only important where a specific workflow requires it; dedupe and safe state convergence are primary.

**Batch / replay / reconciliation posture**
- Notifications may run bounded workflows for:
  - replay of failed or dead notification intents
  - backlog recovery after provider outage
  - reconciliation between queued intents and stored delivery state
  - reporting on delivery lag, duplicate suppression, and dead-letter volume
  - cleanup/retention of expired delivery artifacts by policy
- These workflows must:
  - start from authoritative delivery truth or approved durable input
  - remain rerun-safe
  - avoid duplicate sends for the same business intent
  - validate important candidate outputs before publication/cutover where correctness matters
  - treat replay/remediation as recovery of delivery execution, not as redefinition of upstream business truth

**Primary correctness posture**
- Business truth belongs to the upstream module; Notifications executes side effects after that truth already committed.
- Missing email receipt, delayed provider response, or absent dashboard entry does not prove upstream truth failed.
- Timeout does not prove an email was not sent.
- If delivery state is ambiguous, the system must prefer dedupe-safe recovery over blind resend.
- Canonical delivery-state truth is more important than fast but uncertain resend behavior.

**Consistency and ordering posture**
- Strong consistency is required for:
  - canonical delivery-state records
  - durable business-intent dedupe
  - safe terminal-state transitions
  - stale-attempt rejection where applicable
- Eventual consistency is accepted for:
  - actual delivery completion relative to upstream API success
  - dashboards and summaries
  - backlog convergence
  - replay/remediation convergence
- Notifications does not require one global total order across all sends.
- Legitimate resend must create a **new business intent**; duplicate replay must not.

**Failure and recovery posture**
- Upstream success does not imply email already sent.
- Provider timeout does not imply email not sent.
- Retry/remediation must always respect current delivery-state truth and business-intent dedupe.
- Full bounded replay/remediation is acceptable when safer than fragile selective repair.
- Derived reports and dashboards may lag while canonical delivery truth remains authoritative.

**Key links**
- System-wide rules:
  - `../../01-api-architecture-charter-v1.md`
  - `../../07-security-threat-modeling.md`
  - `../../09-observability-and-slos.md`
- Arc42:
  - `../../../architecture/arc42/02-constraints.md` (non-blocking, abuse prevention)
  - `../../../architecture/arc42/04-runtime-view-v1.md` (Scenario 4–5, optional publish notifications)
  - `../../../architecture/arc42/05-quality-requirements.md` (Reliability/Recoverability/Observability)
  - `../../../architecture/arc42/13-transactions-and-consistency-v1.md`
  - `../../../architecture/arc42/18-stream-processing-and-derived-state-v1.md`
  - `../../../architecture/arc42/19-stream-processing-runtime-v1.md`
  - `../../../architecture/arc42/16-batch-processing-and-derived-data-v1.md`
  - `../../../architecture/arc42/17-dataflow-and-batch-workflows-v1.md`
- System data model:
  - `../../../architecture/arc42/system-data/system-data-notifications-v1.md`

**ADR hooks**
- Email dedupe key strategy (eventId vs template+recipient+tokenId)
- Retry policy and DLQ strategy
- New-article notification policy (recipients, opt-out, V2)
- Template engine choice and safe rendering rules
- Replay/reconciliation policy for delivery-state recovery and reporting outputs
- Publication/cutover policy for important derived notification reports
- Ambiguous-provider-outcome modeling (`Ambiguous` state vs equivalent policy)