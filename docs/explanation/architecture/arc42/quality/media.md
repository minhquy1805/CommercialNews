# Media (Images / Videos / Files) — Characteristic Profile (V1)

This profile captures the **top architecture characteristics** for the Media module,
derived from its explicit/inferred requirements and domain concerns.

---

## Top characteristics (3–5)

1) **Availability (Public UX Impact)** (Operational)  
2) **Recoverability (Lifecycle / Restore Policy)** (Operational)  
3) **Security (Abuse Surface)** (Cross-cutting)  
4) **Correctness (Attachment Integrity)** (Cross-cutting)  
5) **Maintainability (Policy-driven Rules)** (Structural)

> Notes:
> - Media issues are highly visible to users (broken cover images reduce perceived quality).
> - Upload is a common abuse vector; safety rules must be explicit.

---

## Why it matters (domain-driven)

- Media availability strongly affects the reading experience (especially the primary/cover media).
- Attachment rules must be consistent: ordering must be valid, and primary selection must be deterministic.
- Media lifecycle (delete/restore) is operationally sensitive and must follow retention policy.
- Upload and media metadata are an abuse surface (malicious files, oversized payloads, unsafe metadata).

---

## Design implications (what this forces in design)

### Availability (Public UX Impact)
- Reading pages must degrade gracefully if media is missing/unavailable (policy expectation).
- Primary media selection should not create broken references.

### Recoverability (Lifecycle / Restore Policy)
- Implement soft delete in V1 with a defined retention window (policy).
- Restore semantics must be explicit (what happens to primary/order when a media item is restored).

### Security (Abuse Surface)
- Define allowed media types and safe handling policies (validation, size limits, metadata safety).
- Ensure media workflows do not introduce secret/PII leakage via logs or metadata.

### Correctness (Attachment Integrity)
- Enforce deterministic rules:
  - 0 or 1 primary media per article (by policy)
  - stable ordering semantics
  - no broken attachment references
- Clarify whether media can exist independently of article state (optional policy).

### Maintainability (Policy-driven Rules)
- Keep rules (primary/order/delete/restore) centralized in the Media module.
- Media must not embed Content lifecycle workflow; it should react via `ArticleId` and events where needed.

---

## Suggested measures (policy-level)

- **Broken reference rate**: occurrences of missing primary media or invalid ordering (target: near-zero).
- **Media availability/error rate** on public pages (monitor trends).
- **Restore success rate** within retention window.
- **Rejected upload attempts** due to policy violations (tracked to detect abuse patterns).

---

## Key trade-offs

- Stronger safety controls reduce risk but add friction (validation rules and processing).
- Soft delete + restore improves operational recoverability but increases complexity (retention, cleanup, consistency rules).
- Designing graceful degradation for media improves UX but requires fallback logic and testing.

---

## ADR candidates (from this profile)

- Soft delete retention and restore semantics.
- Primary media selection rules and constraints.
- Allowed media types and upload safety policy.
- Media behavior when the associated article is unpublished/archived (publication coupling policy).