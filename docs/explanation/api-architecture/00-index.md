# API Architecture — CommercialNews (V1)

This index is the **entry point** for CommercialNews API architecture documentation.  
It translates the ideas from *Mastering API Architecture* into **practical, enforceable rules** for how we design, validate, operate, and evolve APIs in production.

> System architecture is documented in arc42 under `docs/explanation/architecture/arc42/`.  
> This folder focuses on **API contracts + operations + evolution**, and applies them per module.

---

## 1) What this folder answers

- What does a “correct” CommercialNews API look like? (paths, naming, errors, paging, filtering)
- How do we keep APIs stable for 2–3 years without breaking consumers? (compatibility, versioning, deprecation)
- How do we validate APIs and prevent accidental breakage? (contract tests, CI guardrails)
- How do we operate safely in production? (SLOs/SLIs, observability, rollout strategies, safe logging)
- How do we evolve the system using APIs as seams? (strangler/facade, migration playbooks)
- How do APIs interact with **derived state**, async processing, stream processing, and bounded batch/rebuild workflows without turning those workflows into hidden truth?
- How should API-facing modules behave under **at-least-once delivery, replay, duplicate processing, stale consumers, and delayed derived-state convergence**?

---

## 2) Canonical references (arc42 links)

These API docs must remain consistent with:
- Constraints: `../architecture/arc42/02-constraints.md`
- Modularity & ownership rules: `../architecture/arc42/03-building-blocks-modularity.md`
- Runtime scenarios: `../architecture/arc42/04-runtime-view-v1.md`
- Quality requirements: `../architecture/arc42/05-quality-requirements.md`
- Measurement guide: `../architecture/arc42/06-measurement-guide.md`
- Governance & fitness functions: `../architecture/arc42/07-architecture-governance.md`
- Components: `../architecture/arc42/08-components.md`
- Architecture style: `../architecture/arc42/09-architecture-style.md`
- Transactions & consistency: `../architecture/arc42/13-transactions-and-consistency-v1.md`
- Distributed-systems assumptions: `../architecture/arc42/14-distributed-systems-assumptions-v1.md`
- Consistency, ordering, and consensus boundaries: `../architecture/arc42/15-consistency-ordering-and-consensus-v1.md`
- Batch processing & derived-state policy: `../architecture/arc42/16-batch-processing-and-derived-data-v1.md`
- Dataflow & batch workflow model: `../architecture/arc42/17-dataflow-and-batch-workflows-v1.md`
- Stream processing & derived-state policy: `../architecture/arc42/18-stream-processing-and-derived-state-v1.md`
- Stream runtime model: `../architecture/arc42/19-stream-processing-runtime-v1.md`

**Rule:** if a change affects boundaries, quality priorities, truth/derived rules, stream-processing behavior, or guardrails, update arc42 first (or in the same PR).

---

## 3) Reading paths (choose your goal)

### A) You are implementing or changing an endpoint
1. `01-api-architecture-charter-v1.md`
2. `02-contracts-and-standards.md`
3. `modules/{module}/01-api-surface.md`
4. `modules/{module}/04-errors-status-codes.md`
5. `modules/{module}/06-idempotency-consistency.md`

### B) You are adding a new module or capability
1. `01-api-architecture-charter-v1.md`
2. `10-evolutionary-architecture-with-apis.md`
3. `modules/00-index.md` (module checklist and ordering)
4. Create `modules/{module}/` using the standard module file set

### C) You are planning releases or migrations
1. `06-release-rollout-strategies.md`
2. `04-versioning-and-compatibility.md`
3. `05-edge-gateway-and-traffic-management.md`
4. `11-cloud-migration-and-zero-trust.md` (V2+)

### D) You are hardening security
1. `07-security-threat-modeling.md`
2. `08-authentication-and-authorization.md`
3. `01-api-architecture-charter-v1.md` (safe logging, policy coverage, abuse controls)

### E) You are troubleshooting production incidents
1. `09-observability-and-slos.md`
2. `06-release-rollout-strategies.md`
3. Relevant module’s `07-observability-slos.md` + `03-runtime-flows.md`

### F) You are designing or reviewing async / derived-state behavior behind APIs
1. `01-api-architecture-charter-v1.md`
2. `03-testing-and-contract-validation.md`
3. Relevant module’s `03-runtime-flows.md`
4. Relevant module’s `06-idempotency-consistency.md`
5. arc42:
   - `../architecture/arc42/13-transactions-and-consistency-v1.md`
   - `../architecture/arc42/16-batch-processing-and-derived-data-v1.md`
   - `../architecture/arc42/17-dataflow-and-batch-workflows-v1.md`

### G) You are designing or reviewing stream / consumer behavior behind APIs
1. `01-api-architecture-charter-v1.md`
2. Relevant module’s `03-runtime-flows.md`
3. Relevant module’s `06-idempotency-consistency.md`
4. Relevant module’s `07-observability-slos.md`
5. arc42:
   - `../architecture/arc42/18-stream-processing-and-derived-state-v1.md`
   - `../architecture/arc42/19-stream-processing-runtime-v1.md`
6. ADRs:
   - `../decisions/adr-0027-stream-processing-and-derived-state-policy-v1.md`
   - `../decisions/adr-0028-consumer-idempotency-replay-and-rebuild-policy-v1.md`

---

## 4) Documents (design → validate → operate → evolve)

### Design (contracts-first)
- `01-api-architecture-charter-v1.md` — system-wide non-negotiable rules
- `02-contracts-and-standards.md` — REST conventions, error model, paging/filter/sort
- `04-versioning-and-compatibility.md` — compatibility rules, deprecation lifecycle

### Validate (prevent breakage)
- `03-testing-and-contract-validation.md` — unit/contract/component/integration/E2E + CI guardrails

### Operate (run it safely)
- `05-edge-gateway-and-traffic-management.md` — gateway rules, routing, migration facade, anti-patterns
- `06-release-rollout-strategies.md` — deploy vs release, canary/blue-green/mirroring, gates
- `09-observability-and-slos.md` — SLIs/SLOs, correlation IDs, telemetry rules

### Secure (threats + auth)
- `07-security-threat-modeling.md` — threat modeling packet (DFD/STRIDE/OWASP/DREAD) + validation
- `08-authentication-and-authorization.md` — OAuth2/OIDC, tokens, refresh rotation, scopes, enforcement

### Evolve (APIs as seams)
- `10-evolutionary-architecture-with-apis.md` — seams, cohesion/coupling, migration finish lines, patterns
- `11-cloud-migration-and-zero-trust.md` — 6Rs, gateway facade, hybrid traffic, zero trust, mesh triggers

---

## 5) Module documentation

All module-specific docs live under:
- `modules/`

Start here:
- `modules/00-index.md`

Each module folder follows the same file set:
- `00-index.md` (scope, non-goals, links)
- `01-api-surface.md` (endpoints + request/response shapes)
- `02-domain-contracts.md` (states, invariants, domain events)
- `03-runtime-flows.md` (sync/async/stream boundaries, failure modes, and batch/rebuild hooks where relevant)
- `04-errors-status-codes.md` (error taxonomy + mapping)
- `05-security-abuse-controls.md` (authn/authz, rate limits, safe logging)
- `06-idempotency-consistency.md` (retry safety, idempotency, consistency, truth-vs-derived rules, stream/replay/reconciliation posture where relevant)
- `07-observability-slos.md` (SLIs, metrics/logs, dashboards hooks, lag/backlog/rebuild signals where relevant)
- `08-dependencies-and-ownership.md` (allowed/forbidden deps, truth ownership, and derived-output ownership where relevant)
- `09-open-questions.md` (ADRs + V2 hooks)

**Done definition (V1) for a module**  
A module is “API-architecture complete” when:
- API surface and error model are specified
- invariants and state transitions are explicit
- sync/async/stream boundaries and failure modes are documented
- idempotency rules exist for retries, duplicates, and replay
- security/abuse controls are defined
- observability signals are listed
- ownership and dependency rules are clear
- truth vs derived behavior is explicit where relevant
- batch/rebuild/reconciliation posture is documented where relevant
- stream-processing and consumer behavior are documented where relevant
- open questions are tracked as ADRs

---

## 6) Contribution rules (high-level)

When changing APIs:
- update OpenAPI spec and examples
- ensure backward compatibility (or propose versioning/deprecation plan)
- update the relevant module docs
- add tests (unit + contract checks; integration/E2E when critical)
- ensure:
  - admin policy coverage stays 100%
  - safe logging (no token/PII leaks)
  - async consumers remain idempotent and retry-safe
  - replay/rebuild behavior stays safe under at-least-once delivery
  - read path remains non-blocking
  - derived-state behavior does not become hidden API truth
  - stream/consumer lag does not weaken truth-backed correctness
  - batch/rebuild/reconciliation outputs are not exposed as authoritative before validation/cutover

See also:
- `03-testing-and-contract-validation.md`
- `04-versioning-and-compatibility.md`
- arc42 governance: `../architecture/arc42/07-architecture-governance.md`
- arc42 derived-state rules:
  - `../architecture/arc42/16-batch-processing-and-derived-data-v1.md`
  - `../architecture/arc42/17-dataflow-and-batch-workflows-v1.md`
- arc42 stream/consumer rules:
  - `../architecture/arc42/18-stream-processing-and-derived-state-v1.md`
  - `../architecture/arc42/19-stream-processing-runtime-v1.md`
- ADRs:
  - `../decisions/adr-0027-stream-processing-and-derived-state-policy-v1.md`
  - `../decisions/adr-0028-consumer-idempotency-replay-and-rebuild-policy-v1.md`