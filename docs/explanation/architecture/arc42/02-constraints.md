# 02 — Architecture Constraints

This section captures the non-negotiable constraints that shape the CommercialNews architecture. These constraints are treated as “rules of the game” to prevent architectural drift over time.

---

> Related:
> - `04-runtime-view-v1.md`
> - `09-architecture-style.md`
> - `13-transactions-and-consistency-v1.md`

---

## 2.1 Business Constraints

- **V1 must be shippable**: deliver a working product with public reading (list/detail) and an admin workflow to create and publish content.
- **Governance is mandatory**: admin actions must be protected by authorization policies (roles/permissions).
- **Auditability is required**: sensitive administrative actions must be traceable (publish/unpublish, delete/restore, role/permission changes).
- **SEO is a first-class concern**: content must be discoverable, with stable slug/canonical and metadata support (basic SEO in V1).

---

## 2.2 Quality Attribute Constraints (NFRs)

### Performance & Scalability
- **Read path is the priority**: public list/detail endpoints must remain responsive under load.
- Interaction features (views/likes/comments) must not degrade the read path.
- Slow or non-critical work (notifications, audit persistence, aggregation, indexing) should not block core user flows.

### Reliability & Resilience
- Failure in non-critical subsystems (email delivery, audit storage, counters aggregation, background handlers) must not take down core flows.
- Background handlers must be designed to handle retries safely (idempotent processing).

### Security & Privacy
- Authentication must support secure session management (verification, refresh, logout, password reset).
- Abuse prevention is required for sensitive flows (registration, resend verification, forgot/reset).
- Logs/audit records must avoid leaking secrets and sensitive PII (apply redaction and safe logging practices).

### Observability
- Minimal production observability is required for critical flows:
  - auth flows (register/login/refresh/verify/reset)
  - content publish/unpublish
  - email workflows
  - background processing reliability (success/failure rates)

---

## 2.3 Technical & Platform Constraints

- The system must support a **modular architecture** that can evolve:
  - V1 can run as a modular monolith / service-based system
  - V2+ can introduce projections/read models and additional workers without rewriting core modules
- Asynchronous workflows are required for:
  - audit trail ingestion
  - system notifications (email)
  - high-traffic interaction processing (view aggregation, counters)

> Note: specific technologies (queues, databases, runtime) are implementation choices and should be recorded in ADRs, not hard-coded here unless they are truly fixed constraints.

---

## 2.4 Organizational Constraints

- The project must remain feasible for a **small team / single developer**:
  - avoid unnecessary complexity in V1
  - prefer clear module boundaries and incremental evolution
- Documentation must remain **maintainable**:
  - architecture described using arc42 structure
  - key decisions captured with ADRs

---

## 2.5 Data & Compliance Constraints

- **Ownership is explicit**: each module is the logical owner of its data and rules (even if sharing one database).
- **Retention policies must be defined** (at least at policy level) for:
  - audit logs
  - login history
  - content edit history (news history)
- Sensitive data handling:
  - avoid storing or emitting secrets in events/logs
  - minimize event payloads to reduce accidental leakage
  - ensure any public identifiers (slug, URLs) follow a consistent policy