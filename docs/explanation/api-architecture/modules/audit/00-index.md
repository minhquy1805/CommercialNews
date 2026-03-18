# Audit Module — API Architecture (V1)

**Purpose**
- Provide an **append-only audit trail** for sensitive actions across modules.
- Audit is investigation-ready and governance-focused.
- Preserve **evidence truth** separately from business truth owned by other modules.
- Support safe replay, remediation, bounded reconciliation, and archival/reporting workflows when ingestion lags or gaps are detected.

**Why this module is critical**
- Governance failures are high impact and require traceability.
- Audit must not block core flows (publish/authz changes).
- Audit records must be privacy-aware (no token/PII leakage).
- Audit ingestion must tolerate retries, replay, and duplicate delivery (at-least-once delivery).
- Audit dashboards and reports may lag, but canonical audit evidence must remain append-only and durable.
- Missing evidence is operationally and governance-significant, so completeness and replay posture matter.

**Primary consumers**
- Worker (audit ingestion consumer)
- Admin UI / operators (read/search audit logs)
- Incident response and investigations
- Future reporting/compliance workflows
- Replay/reconciliation/backfill workflows over bounded audit scopes

**Non-goals (V1)**
- Full redaction engine with configurable rules (V2 hook)
- Complex correlation across distributed systems (V2+)
- Real-time SIEM integrations (future)
- Treating dashboards, summaries, or compliance reports as audit truth
- Global coordination-heavy ownership for ingest/backfill by default

**Hard constraints**
- Append-only intent: no updates/deletes except explicit retention/purge policy.
- Must be event-driven: consumes domain events; does not couple to synchronous workflows.
- Must be idempotent and retry-safe.
- Must not store or emit secrets/tokens/PII unsafely.
- Canonical audit evidence is **truth** for the Audit module; dashboards, summaries, and reports are **derived**.
- Async audit ingestion is **at-least-once**; duplicates, replay, lag, and worker restart must be tolerated safely.
- Replay/backfill/reconciliation workflows must be **bounded**, **observable**, and **rerun-safe**.
- Batch/remediation workflows must not silently rewrite prior audit facts.
- Safe non-progress is preferable to unsafe duplicate evidence or history mutation.

**Truth vs derived posture**
- **Truth**:
  - append-only audit records
  - canonical event identity / dedupe outcome
  - ingestion outcome needed for investigation-grade evidence
  - correction linkage if an explicit correction model is introduced
- **Derived**:
  - audit dashboards
  - search summaries
  - reporting/compliance materializations
  - gap-detection or reconciliation reports
  - archival indexes
  - replay candidate sets
  - timeline/reporting views
- **Rule**: derived audit outputs may lag and be rebuilt; they do not replace canonical audit evidence.

**Stream / async posture**
- Audit is a downstream evidence consumer in the standard V1 model:
  1. upstream module commits truth
  2. outbox/event is published asynchronously
  3. Audit consumer receives events at least once
  4. canonical event identity + dedupe prevent duplicate evidence
  5. append-only audit persistence converges through retry, replay, and remediation
- Audit must distinguish:
  - duplicate delivery of the same canonical event
  - replay/backfill of previously missing evidence
  - stale delivery that still represents a valid historical fact
  - lagging derived reports versus canonical evidence truth
- Audit does not depend on a global total order; stable event identity and append-only persistence matter more.

**Batch / replay / reconciliation posture**
- Audit may run bounded workflows for:
  - replay of failed or missed ingestion
  - backfill of missing evidence from durable upstream signals
  - reconciliation between expected governance events and stored audit facts
  - reporting/compliance output generation
  - archival/retention operations over historical audit windows
- These workflows must:
  - start from durable truth inputs
  - preserve append-only audit semantics
  - avoid duplicate canonical facts
  - publish derived outputs only after validation where correctness matters
  - treat replay/backfill as recovery for evidence completeness, not as permission to rewrite history

**Primary correctness posture**
- Business truth remains with upstream modules; Audit records evidence after that truth already committed.
- Missing dashboard/report output does not prove canonical audit evidence is missing.
- Missing audit evidence temporarily does not undo upstream business truth, but it is still a governance incident if not recovered.
- Timeout or missing acknowledgment does not prove the audit record was not persisted.
- If audit completeness is uncertain, the system must prefer bounded replay/reconciliation over unsafe synthetic evidence mutation.

**Consistency and ordering posture**
- Strong consistency is required for:
  - append-only canonical evidence persistence
  - durable dedupe for canonical event identity
  - local evidence-store integrity
  - safe protection against duplicate or contradictory persistence outcomes
- Eventual consistency is accepted for:
  - evidence arrival relative to upstream business success
  - dashboards and reporting views
  - replay/backfill convergence
  - archival/summarization outputs
- Audit does not require one global total order across all module events.
- Timestamps support investigations, but correlation identifiers and canonical event identity remain primary.

**Failure and recovery posture**
- Upstream success does not imply audit evidence is already queryable.
- Queue lag, worker restart, and replay are normal operational conditions.
- Replay/backfill is acceptable when safer than fragile selective repair, provided canonical identity and append-only integrity are preserved.
- Derived reports may lag while canonical evidence truth remains authoritative.
- If ownership for replay/backfill is ever sensitive, it must use explicit authoritative coordination rather than naive singleton assumptions.

**Key links**
- System-wide rules:
  - `../../01-api-architecture-charter-v1.md`
  - `../../07-security-threat-modeling.md`
  - `../../09-observability-and-slos.md`
- Arc42:
  - `../../../architecture/arc42/03-building-blocks-modularity.md`
  - `../../../architecture/arc42/04-runtime-view-v1.md` (Scenarios 1,2,6)
  - `../../../architecture/arc42/05-quality-requirements.md` (Security/Observability/Recoverability)
  - `../../../architecture/arc42/13-transactions-and-consistency-v1.md`
  - `../../../architecture/arc42/18-stream-processing-and-derived-state-v1.md`
  - `../../../architecture/arc42/19-stream-processing-runtime-v1.md`
  - `../../../architecture/arc42/16-batch-processing-and-derived-data-v1.md`
  - `../../../architecture/arc42/17-dataflow-and-batch-workflows-v1.md`
- System data model:
  - `../../../architecture/arc42/system-data/system-data-audit-v1.md`

**ADR hooks**
- Audit payload shape per event type (minimal necessary fields)
- Retention and purge policy (how long to keep)
- Tamper-evident strategy (hash chaining) (V2)
- Redaction policy rules (what to mask/remove)
- Replay/backfill policy for missing audit evidence
- Reporting/materialization policy for audit-derived outputs
- Explicit correction model if correction-by-new-fact is formalized