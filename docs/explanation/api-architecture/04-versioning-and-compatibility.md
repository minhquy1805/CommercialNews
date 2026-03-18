# Versioning & Compatibility — CommercialNews (V1)

This document defines how CommercialNews manages **API evolution** without breaking consumers.
It applies Chapter 1 (versioning/standards) and Chapter 5 (release safety) from *Mastering API Architecture*.

Related:
- Constraints: `../architecture/arc42/02-constraints.md`
- Quality requirements: `../architecture/arc42/05-quality-requirements.md`
- Governance: `../architecture/arc42/07-architecture-governance.md`
- Release strategies: `06-release-rollout-strategies.md`
- Contracts: `02-contracts-and-standards.md`

---

## 1) Principles

### 1.1 Versioning is a product feature
Versioning increases producer complexity, but it protects consumers and trust.

### 1.2 Backward compatibility is the default
In V1, every change should assume existing consumers **must keep working**.

### 1.3 Evolve APIs as seams
APIs are seams: we change implementations behind stable contracts whenever possible.

---

## 2) Versioning scheme (how we convey versions)

### 2.1 URL versioning
CommercialNews conveys versions in the URL:
- `/api/v1/...`

**Rule:** breaking changes require a new major version:
- `/api/v2/...`

### 2.2 Public vs Admin
Both public and admin APIs follow the same version scheme:
- Public: `/api/v1/...`
- Admin: `/api/v1/admin/...`

---

## 3) Compatibility rules (what is allowed in V1)

### 3.1 Compatible changes (allowed)
These changes are considered backward compatible:
- Add new endpoints
- Add new optional fields to responses
- Add new optional fields to request bodies
- Add new error codes (without removing old ones)
- Add new query parameters that are optional
- Relax validation rules (more permissive), when it does not change meaning

### 3.2 Breaking changes (not allowed without v2)
These changes require a new major version and a migration plan:
- Remove or rename fields
- Change field meaning or constraints (semantic break)
- Change a field from optional → required
- Change response shape or nesting structure
- Change default behavior in a way that surprises consumers
- Remove or rename endpoints
- Change pagination/sorting/filter semantics incompatibly
- Change auth requirements for an existing endpoint (unless it was explicitly “TBD” and documented)

### 3.3 Enum evolution rules (special case)
Enums are a common break point.

Allowed (preferred):
- Use string enums and document that clients must tolerate unknown values.
- Add new enum values only when consumers are expected to be forward-compatible.

Breaking (requires v2) if:
- Consumers are known/expected to reject unknown enum values.
- You change meaning of existing values.

---

## 4) Deprecation policy (how we retire safely)

### 4.1 Deprecation lifecycle (required for breaking change)
1) **Announce**
   - Document deprecation in API docs and changelog.
2) **Introduce replacement**
   - Provide the v2 endpoint/shape in parallel.
3) **Provide migration guide**
   - Concrete mapping: old → new fields, examples, behavior differences.
4) **Monitor usage**
   - Track remaining traffic on deprecated endpoints.
5) **Sunset**
   - Communicate a sunset date.
6) **Retire**
   - Remove or disable deprecated behavior.

### 4.2 Runtime signals (recommended)
When an endpoint is deprecated, the API MAY return a response header:
- `Deprecation: true`
- `Sunset: <date>`

(Exact headers and formats are a policy choice; keep them consistent across modules.)

### 4.3 Retired endpoint behavior
For fully retired endpoints:
- Prefer `410 Gone` with a helpful error message and a reference to migration docs.

---

## 5) OpenAPI-driven compatibility enforcement (CI)

### 5.1 OpenAPI is the source of truth
- OpenAPI must be updated in the same PR as API code changes.
- OpenAPI examples should include critical error cases.

### 5.2 Breaking change detection
CI should fail PRs that introduce breaking changes, including:
- removed/renamed paths
- removed/renamed fields
- optional → required changes
- response schema shape changes

Allowed changes should pass:
- additive optional fields
- new endpoints
- new optional parameters

---

## 6) Event contract versioning (API → Worker)

CommercialNews uses selective event-driven architecture for side effects.
Event contracts must evolve safely too.

### 6.1 Envelope versioning
All events include:
- `EventType`
- `Version`
- `Payload`

### 6.2 Event payload compatibility rules
Allowed (compatible):
- add new optional fields to payload

Breaking (requires bumping `Version`, and a consumer plan):
- remove fields consumers rely on
- change meaning of existing fields
- change requiredness
- change event semantics (what the event *means*)

### 6.3 Coexistence strategy (when Version bumps)
When an event version changes:
- producers may emit V1 and V2 in parallel temporarily, or
- emit only V2 but keep consumers tolerant (depending on migration plan)

**Rule:** do not silently change payload semantics without a version bump.

---

## 7) Release strategy alignment (Chapter 5)

Versioning is only useful if it supports safe release.

### 7.1 Minor/patch releases (compatible)
- Deploy and roll out gradually:
  - canary or progressive delivery
- Consumers should not need changes for minor/patch updates.

### 7.2 Major releases (breaking)
- Run v1 and v2 in parallel (versioned paths)
- Provide migration guidance and a sunset plan
- Use routing and release gates to control exposure

---

## 8) Consumer expectations (what we require from clients)

To keep compatibility manageable, clients should:
- tolerate unknown JSON fields (ignore what they don’t understand)
- handle new enum values gracefully (or treat as “Unknown”)
- avoid strict ordering assumptions in lists unless documented
- implement retry with backoff for transient errors (429/503), respecting rate limits

---

## 9) Changelog and change communication (recommended)

For every API change:
- record in a changelog (module or system-level)
- specify:
  - what changed
  - compatibility impact
  - migration notes (if relevant)
  - rollout notes (if relevant)

---

## 10) ADR hooks (decisions that must be explicit)

Create ADRs for:
- introducing `/api/v2` (what is breaking and why)
- enum strategy and forward-compat expectations
- event version bump strategy (dual publish vs cutover)
- deprecation headers and sunset policy