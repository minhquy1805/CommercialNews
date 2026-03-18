# Authorization (Roles / Permissions / Policy) — Characteristic Profile (V1)

This profile captures the **top architecture characteristics** for the Authorization module,
derived from governance requirements and domain concerns.

---

## Top characteristics (3–5)

1) **Security (Least Privilege Enforcement)** (Cross-cutting)  
2) **Correctness (Policy Accuracy & Coverage)** (Cross-cutting)  
3) **Maintainability & Evolvability (Permission Model)** (Structural)  
4) **Performance (Policy Evaluation Cost)** (Operational)  
5) **Auditability (Governance Actions Traceability)** (Cross-cutting)

> Notes:
> - Authorization failures are high impact and often catastrophic.
> - “Correct but slow” can block admin operations; “fast but wrong” becomes a security incident.

---

## Why it matters (domain-driven)

- Admin operations must be protected systematically (no forgotten endpoints).
- Least privilege must be enforceable and verifiable.
- Permission changes are sensitive and must be traceable for governance and incident response.
- Scattered checks create inconsistent security posture and drift over time.

---

## Design implications (what this forces in design)

### Security (Least Privilege Enforcement)
- Enforce least privilege by default; deny-by-default for admin actions.
- Keep authorization checks centralized and consistent across endpoints.

### Correctness (Policy Accuracy & Coverage)
- Define explicit policy coverage for all admin endpoints.
- Prevent “policy gaps” by design (avoid ad-hoc permission checks scattered in services).
- Ensure policy evaluation uses stable identifiers (avoid ambiguous rules).

### Maintainability & Evolvability (Permission Model)
- Permission naming/versioning must be consistent to prevent “string chaos”.
- Support adding new permissions without refactoring existing policy wiring.
- Keep the permission model understandable to administrators.

### Performance (Policy Evaluation Cost)
- Policy checks must not become a bottleneck for admin operations.
- Minimize expensive lookups during authorization where possible (policy-level expectation).

### Auditability (Governance Actions Traceability)
- Role/permission assignment and revocation actions must be traceable (who/when/what).

---

## Suggested measures (policy-level)

- **Policy coverage:** % admin endpoints protected by explicit authorization policies (target: 100%).
- **Authorization failures:** number of unauthorized access incidents (target: trend toward zero).
- **Policy evaluation latency:** keep within acceptable bounds for admin endpoints.
- **Permission drift signals:** frequency of ad-hoc checks found in code review (target: near-zero).

---

## Key trade-offs

- Fine-grained permissions improve security but increase management and complexity.
- Centralized policy enforcement improves correctness but requires discipline to avoid bypasses.
- Faster evaluation may require caching or optimization later, increasing complexity.

---

## ADR candidates (from this profile)

- Permission naming and versioning strategy.
- Policy enforcement approach (central policy handlers vs scattered checks).
- Governance rules for role/permission changes (who can grant what).