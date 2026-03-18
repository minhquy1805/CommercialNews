# Audit Trail — Characteristic Profile (V1)

This profile captures the **top architecture characteristics** for the Audit Trail module,
derived from governance requirements and domain concerns.

---

## Top characteristics (3–5)

1) **Auditability & Accountability (Traceability)** (Cross-cutting)  
2) **Reliability (No Loss Under Retries/Failures)** (Operational)  
3) **Privacy (Redaction & Sensitive Data Handling)** (Cross-cutting)  
4) **Observability (Investigation Readiness)** (Cross-cutting)  
5) **Maintainability (Audit Policy Clarity)** (Structural)

> Notes:
> - Audit data exists for incident response and governance.
> - “Missing audit records” is often as damaging as an outage during investigations.

---

## Why it matters (domain-driven)

- Sensitive actions (publish/unpublish, delete/restore, role changes) must be traceable.
- Governance failures are high impact; audit correlation is essential for debugging incidents.
- Audit must not leak secrets/PII while still enabling investigations.
- Audit must remain trustworthy even under retries and partial failures.

---

## Design implications (what this forces in design)

### Auditability & Accountability (Traceability)
- Capture: actor (who), action (what), timestamp (when), target resource (which), and reason (why) where applicable.
- Maintain consistent correlation identifiers to link actions across modules.

### Reliability (No Loss Under Retries/Failures)
- Audit ingestion must tolerate retries and partial failures (idempotent processing).
- Audit must not block core flows; it should be asynchronous where possible.

### Privacy (Redaction & Sensitive Data Handling)
- Define redaction rules for secrets/PII and enforce them consistently.
- Prefer minimal payloads in audit records to reduce leakage risk.

### Observability (Investigation Readiness)
- Audit records must be queryable and usable for incident timelines.
- Track ingestion failures and backlog/lag signals to prevent silent gaps.

### Maintainability (Audit Policy Clarity)
- Define which actions MUST be audited and keep that policy centralized.
- Avoid scattered manual logging that becomes inconsistent over time.

---

## Suggested measures (policy-level)

- **Audit coverage:** % of mandatory actions producing an audit record (target: 100%).
- **Ingestion success rate:** audit handler success/failure rate (tracked).
- **Gap detection:** detection of missing audit events for critical operations (target: zero).
- **PII leakage checks:** zero occurrences of secrets/tokens/PII in audit payloads.

---

## Key trade-offs

- Stronger audit guarantees increase storage and processing overhead.
- Minimizing payload improves privacy but may reduce debug detail (needs good correlation).
- Asynchronous audit improves performance but requires retry/idempotency discipline.

---

## ADR candidates (from this profile)

- Audit coverage policy (which actions are mandatory to log).
- Audit event schema and redaction rules.
- Correlation strategy (how requests/events link to audit records).