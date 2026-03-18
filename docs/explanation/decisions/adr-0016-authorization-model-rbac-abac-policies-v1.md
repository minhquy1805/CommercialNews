# ADR-0016 — Authorization Model (RBAC/ABAC + Policies) (V1)

**Status:** Accepted  
**Date:** 2026-03-04  
**Decision owners:** Architecture / Security  
**Scope:** System-wide (Authorization module, policy evaluation, API enforcement)  
**Related:**
- `../architecture/arc42/03-building-blocks-modularity.md`
- `../architecture/arc42/04-runtime-view-v1.md`
- `../architecture/arc42/05-quality-requirements.md`
- `../architecture/arc42/10-system-data.md`
- `../architecture/arc42/11-replication-v1.md`
- `../api-architecture/01-api-architecture-charter-v1.md`
- ADR-0012 (Data store placement), ADR-0013 (Outbox semantics), ADR-0015 (Redis cache policy)

---

## Context

CommercialNews V1 has:
- public read endpoints (mostly open access)
- admin endpoints for governance and content operations (must be protected)
- security-critical identity flows (verification/reset/refresh)
- modular boundaries where each module owns its data but must enforce consistent access rules

We require an authorization model that:
- is explicit and auditable (governance-ready)
- scales across modules without ad-hoc checks
- supports both coarse roles and fine-grained rules (ownership, department, sensitivity)
- integrates cleanly into the V1 modular monolith (API + Worker)

---

## Decision

### 1) Primary model: RBAC for coarse access + ABAC for fine-grained decisions
- **RBAC** (roles/permissions) is the baseline:
  - Admin, Moderator, Author, User (system roles)
  - permissions are explicit and centrally managed
- **ABAC** is used where resource context matters:
  - ownership (AuthorUserId)
  - tenant (if applicable)
  - department/groups (if applicable)
  - resource sensitivity/status (draft/published/unpublished/archived)

ABAC is evaluated as a **policy layer** on top of permissions:
- Permission grants capability
- ABAC validates context constraints (who/what/when/where)

---

### 2) Enforcement mechanism: policy-based authorization in the API layer
V1 uses:
- `IAuthorizationHandler` + `PermissionRequirement` + policies
- Endpoint annotations (or centralized route policy mapping) enforce:
  - **admin policy coverage = 100%**
  - no scattered manual checks

**Rule:** every admin endpoint MUST declare a policy:
- `Authorize(Policy = "Permission:<PermissionKey>")`
- ABAC checks run inside the same policy pipeline when required

---

### 3) Permission taxonomy and naming rules
Permissions are defined as stable keys:
- Format: `"{Module}.{Resource}.{Action}"`
  - examples:
    - `Content.Article.Create`
    - `Content.Article.Publish`
    - `Content.Article.Unpublish`
    - `SEO.Slug.Update`
    - `Authorization.Role.Assign`
    - `Authorization.Permission.Grant`

**Rule:** permission keys are API contracts:
- additive changes are allowed in V1
- breaking changes require ADR + migration plan

---

### 4) Data model ownership (Authorization module owns governance state)
Authorization module owns:
- Roles
- Permissions
- RolePermission
- UserRole
(and optional: UserPermission if introduced later)

Cross-module references:
- by stable IDs only (`UserId`, `RoleId`, `PermissionKey`)

**Rule:** governance changes MUST be auditable and non-blocking on audit ingestion.

---

### 5) Auditability & replication of governance changes
All governance mutations emit events via Outbox:
- `RoleAssigned`, `RoleRevoked`
- `PermissionGranted`, `PermissionRevoked`
- `RoleCreated/Updated`, etc. (as needed)

Audit Trail consumes these events asynchronously.
Core governance writes must not block on audit completion.

---

### 6) Caching posture (safe-by-default)
Authorization decisions are security sensitive.

Rules:
- Default: evaluate from truth (DB) for admin/governance and self-critical flows.
- If caching is introduced:
  - cache must be short-lived and invalidated on governance events
  - use versioning (e.g., `AuthzRevision` per user) to prevent stale permission usage
  - no cache may grant access if uncertain (“fail closed” for admin actions)

**Rule:** never let Redis be the sole source of truth for authorization decisions.

---

## ABAC context model (V1 contract)

When ABAC is required, evaluation uses three attribute groups:

### SubjectAttributes
- `UserId`
- `Email`
- `Roles[]`
- `Department`
- `Groups[]`
- `TenantId`
- `IsMfaOn` (optional)

### ResourceAttributes
- `ResourceType`
- `Id`
- `OwnerId?`
- `Department?`
- `Sensitivity?`
- `Status?` (Draft/Published/Unpublished/Archived)
- `TenantId?`

### EnvironmentAttributes
- `Now`
- `Hour` (localized)
- `Ip?`
- `UserAgent?`
- `TenantId?`

ABAC policies must be deterministic and testable.

---

## Alternatives considered

1) **RBAC only**
- Pros: simpler.
- Cons: insufficient for ownership/sensitivity rules and future multi-tenant evolution.

2) **ABAC only**
- Pros: expressive.
- Cons: harder to manage and audit; risk of inconsistent rules across modules.

3) **Ad-hoc checks in controllers/services**
- Pros: quick.
- Cons: untestable drift, inconsistent enforcement, poor auditability.

---

## Consequences

### Positive
- Clear, centralized governance state and stable permission taxonomy
- Strong security posture with explicit policies
- ABAC provides future-proof fine-grained control (ownership/sensitivity)
- Works well with modular boundaries and Outbox-based replication

### Negative / Trade-offs
- Requires discipline: permissions and policies must be maintained
- ABAC rules introduce complexity; must be tested and documented per module
- Caching must be handled carefully (prefer correctness over speed)

---

## Implementation notes (V1)

- Implement policy names:
  - `Permission:<PermissionKey>`
- PermissionRequirement checks:
  - user has permission via roles
  - optional ABAC evaluation for resource context
- Provide a single service for building ABAC context (PIP):
  - maps user/resource/environment data into the ABAC models
- Create seeders for roles and permissions as part of bootstrap.
- Emit governance events via Outbox for auditability and downstream invalidation.

---

## Follow-ups

- Module docs should list required permissions under:
  - `modules/{module}/05-security-abuse-controls.md`
  - and reference policies in `01-api-surface.md`
- Add contract tests ensuring 100% admin endpoints have policies.
- Consider introducing `AuthzRevision`/`PermissionsEtag` per user if caching is introduced.