# CommercialNews — API Architecture Docs

This folder defines the **API architecture** for CommercialNews: how we **design**, **validate**, **operate**, and **evolve** APIs safely in production.

These documents are **contract-first** and **operations-aware**:
- protect consumers from breaking changes
- protect the read path under burst traffic
- enforce governance (authz + audit) without blocking core flows
- keep async workflows retry-safe and observable
- keep **truth vs derived state** explicit at API boundaries
- ensure batch / rebuild / reconciliation behavior never becomes hidden API truth

> System architecture is documented in arc42 under `docs/explanation/architecture/arc42/`.
> This folder focuses on **API contracts + operational rules** and applies them per module.

---

## 1) How to read (recommended order)

### Step 1 — System-wide rules (read once, apply everywhere)
1. `00-index.md`
2. `01-api-architecture-charter-v1.md`  
   **Non-negotiable rules**: read-path-first, async side effects, safe logging, policy coverage, idempotency.
3. `02-contracts-and-standards.md`  
   REST conventions, error model, paging/filter/sort rules, OpenAPI as contract.
4. `04-versioning-and-compatibility.md`  
   Compatibility rules, deprecation lifecycle, retirement policy.
5. `03-testing-and-contract-validation.md`  
   Test strategy as architecture decision (unit/contract/component/integration/E2E + CI guardrails).

### Step 2 — Operations, security, and correctness under lag
6. `05-edge-gateway-and-traffic-management.md`  
   North–south routing, gateway anti-patterns, migration facade patterns.
7. `06-release-rollout-strategies.md`  
   Deploy vs release, canary/blue-green/mirroring, gates, rollback.
8. `07-security-threat-modeling.md`  
   Threat modeling packet (DFD + STRIDE + OWASP + DREAD) + validation plan.
9. `08-authentication-and-authorization.md`  
   OAuth2/OIDC, token strategy, refresh rotation/reuse detection, scope strategy.
10. `09-observability-and-slos.md`  
    SLIs/SLOs for read path + async workflows, correlation IDs, telemetry rules.

### Step 3 — Module-by-module (the real working docs)
- `modules/`  
  Each module folder applies the rules above and is the primary place for endpoint-level decisions.

### Step 4 — Derived-state aware API reasoning
Use the relevant module docs together with arc42 when the API interacts with:
- async side effects
- caches
- projections/materialized views
- counters and summaries
- batch/rebuild/reconciliation workflows

In those cases, always verify:
- what is **truth**
- what is **derived**
- what may **lag**
- what must **fail closed / fall back safely**
- what output is only a **candidate** and not yet active/public

---

## 2) Relationship to arc42 (source of truth links)

This folder **must remain consistent** with the following arc42 docs:
- Constraints: `../architecture/arc42/02-constraints.md`
- Modularity & dependency rules: `../architecture/arc42/03-building-blocks-modularity.md`
- Runtime scenarios: `../architecture/arc42/04-runtime-view-v1.md`
- Quality requirements: `../architecture/arc42/05-quality-requirements.md`
- Measurement guide: `../architecture/arc42/06-measurement-guide.md`
- Governance & fitness functions: `../architecture/arc42/07-architecture-governance.md`
- Components mapping: `../architecture/arc42/08-components.md`
- Architecture style: `../architecture/arc42/09-architecture-style.md`
- Transactions & consistency: `../architecture/arc42/13-transactions-and-consistency-v1.md`
- Batch processing & derived-state policy: `../architecture/arc42/16-batch-processing-and-derived-data-v1.md`
- Dataflow & batch workflow model: `../architecture/arc42/17-dataflow-and-batch-workflows-v1.md`

**Rule:** If a change affects runtime boundaries, quality priorities, truth-vs-derived rules, or governance guardrails,
update arc42 first (or in the same PR), then update this folder.

---

## 3) Folder structure

### System-wide (applies to all modules)
- `01-api-architecture-charter-v1.md`
- `02-contracts-and-standards.md`
- `03-testing-and-contract-validation.md`
- `04-versioning-and-compatibility.md`
- `05-edge-gateway-and-traffic-management.md`
- `06-release-rollout-strategies.md`
- `07-security-threat-modeling.md`
- `08-authentication-and-authorization.md`
- `09-observability-and-slos.md`
- `10-evolutionary-architecture-with-apis.md`
- `11-cloud-migration-and-zero-trust.md`

### Module-by-module
`modules/{module}/` contains:
- `00-index.md` (scope, non-goals, links)
- `01-api-surface.md` (endpoints, shapes, paging/filter/sort)
- `02-domain-contracts.md` (states, invariants, events)
- `03-runtime-flows.md` (sync/async boundaries, failure modes, batch/rebuild hooks where relevant)
- `04-errors-status-codes.md` (error taxonomy, anti-enumeration rules)
- `05-security-abuse-controls.md` (authn/authz, rate limits, safe logging)
- `06-idempotency-consistency.md` (idempotency keys, retry rules, consistency expectations, truth-vs-derived rules where relevant)
- `07-observability-slos.md` (SLIs, logs, metrics, dashboard hooks, derived-workflow signals where relevant)
- `08-dependencies-and-ownership.md` (allowed/forbidden dependencies, truth ownership, derived-output ownership where relevant)
- `09-open-questions.md` (ADRs, V2 hooks, unresolved policies)

---

## 4) How to contribute (PR checklist)

When changing APIs:
- [ ] Update OpenAPI spec and examples (contract-first).
- [ ] Confirm change is backward compatible; if not, propose versioning plan.
- [ ] Update module docs (`modules/{module}/...`) for endpoint semantics.
- [ ] Add/adjust tests:
  - unit tests for invariants
  - contract checks (OpenAPI diff)
  - component/integration tests for critical paths
- [ ] Confirm governance:
  - admin endpoints have explicit authorization policy coverage
  - sensitive flows have rate limits
  - audit events emitted for governance actions
- [ ] Confirm operations:
  - correlation IDs present for critical flows
  - metrics/logs do not leak tokens/PII
  - async consumers remain idempotent and retry-safe
- [ ] Confirm truth-vs-derived posture:
  - API does not expose derived state as if it were authoritative truth
  - stale or missing derived data degrades safely
  - correctness-sensitive APIs use truth-backed fallback or fail-closed behavior
- [ ] Confirm batch/rebuild/reconciliation posture where relevant:
  - rerun-safe behavior
  - bounded input
  - candidate outputs are validated before publication/cutover
  - partial rebuild output is not exposed as final active state

---

## 5) Evolution rules (V1 → V2+)

CommercialNews uses APIs as seams:
- evolve behind stable contracts (strangler/facade routing)
- keep modules cohesive and loosely coupled
- introduce read models/projections when traffic signals justify it
- keep derived outputs subordinate to truth even as the system adds more projections, caches, and batch workflows

See:
- `10-evolutionary-architecture-with-apis.md`
- `11-cloud-migration-and-zero-trust.md`

---

## 6) Practical interpretation rule

When an API returns data, ask these questions explicitly:

1. Is this field coming from **truth** or **derived state**?  
2. If the derived path is stale or unavailable, what is the safe behavior?  
3. Does this endpoint require **truth-backed fallback** or **fail-closed** behavior?  
4. Is any batch/rebuild/reconciliation output being exposed before validation/cutover?  

If those questions are not clearly answerable, the API design is not finished yet.