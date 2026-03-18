# Cloud Migration & Zero Trust — CommercialNews Guidance (V2+)

This document applies Chapter 9 (cloud migration patterns, gateway facade, north–south vs east–west traffic, and zero trust evolution) from *Mastering API Architecture* to CommercialNews.

V1 is a service-based modular monolith (API + Worker). This document is primarily for **V2+ evolution** when CommercialNews:
- adopts more managed cloud services
- introduces additional components/services (read model, interaction, media, search)
- runs hybrid (multi-cluster / cloud ↔ on-prem) during migration

Related:
- Components: `../architecture/arc42/08-components.md`
- Architecture style: `../architecture/arc42/09-architecture-style.md`
- Gateway & traffic management: `05-edge-gateway-and-traffic-management.md`
- Release strategies: `06-release-rollout-strategies.md`
- Governance: `../architecture/arc42/07-architecture-governance.md`

---

## 1) The 6Rs migration spectrum (make it an explicit decision)

Cloud migration is not one thing. It is a spectrum:
- **Retain**: keep as-is
- **Rehost**: lift-and-shift
- **Replatform**: move to managed/container platform with minimal code change
- **Repurchase**: replace with a managed product/SaaS
- **Refactor / Re-architect**: change architecture/code significantly
- **Retire**: remove entirely

### CommercialNews default posture
- Prefer **Replatform** for core components (API, Worker, DB) when moving to cloud.
- Prefer **Repurchase** for non-core capabilities (e.g., email delivery provider, analytics tooling).
- Use **Refactor** per module only after routing + observability are stable.
- Use **Retire** as a cleanup opportunity (remove dead endpoints/features).

**Rule:** every migration step must be captured as an ADR with goals/constraints/finish line.

---

## 2) Traffic patterns become blurry in migration (design it deliberately)

During incremental/hybrid migration, a request often:
- enters from outside (north–south)
- traverses services internally (east–west)
- crosses networks/clusters (cloud ↔ on-prem, multi-region, multi-cluster)

### CommercialNews rule
Design traffic patterns deliberately to avoid:
- unpredictable latency
- hidden failure modes
- security gaps
- debugging complexity (no clear ownership of the hop)

---

## 3) The gateway/APIM as the contract anchor (migration facade)

### 3.1 Principle
Keep a stable entrypoint so consumers do not change contracts while backends move.

### 3.2 Contract anchor target
- `api.commercialnews.com`
- stable `/api/v1/...` and `/api/v1/admin/...` paths

### 3.3 Facade routing pattern (Strangler)
Route by path/host to move slices gradually:

Example slice migration:
- `/api/v1/media/*` → new Media component/service (cloud)
- `/api/v1/search/*` → new Search component/service (cloud)
- `/api/v1/*` (everything else) → legacy API component until migrated

**Rules:**
- consumers must not be forced to change contracts due to backend relocation
- routing changes must be version-controlled and rollbackable
- routing must not become a place where domain logic lives

---

## 4) North–south vs east–west (roles remain distinct)

### 4.1 North–south (edge)
Handled by:
- edge gateway / ingress / API gateway / WAF (depending on platform)

Responsibilities:
- TLS termination
- host/path routing
- baseline protections (payload limits, coarse rate limits)
- edge observability

### 4.2 East–west (service-to-service)
Handled by:
- service discovery and internal networking
- service mesh *when justified* (mTLS, policy enforcement, standardized telemetry)

**Rule:** avoid gateway loopback — internal services should not call the public edge to reach other internal services.

---

## 5) Zero trust evolution (direction, not a switch)

### 5.1 The direction
Move from perimeter-only security (castle-and-moat) toward **zero trust**:
- never trust, always verify
- identity-based access between workloads
- microsegmentation via policy

### 5.2 Transitional hybrid stance
During migration, perimeter controls still exist:
- firewall rules, WAF, DDoS protection, private connectivity

Zero trust controls are layered internally:
- workload identity
- mTLS between services (when services exist)
- default-deny network policies + explicit allow rules
- consistent authorization policies for service-to-service calls

**Rule:** zero trust does not remove zoning; it complements it during hybrid transitions.

---

## 6) When service mesh becomes justified (not V1 by default)

CommercialNews V1 has two components and in-process module calls.
A service mesh becomes justified when:
- multiple services/components exist with east–west traffic
- mTLS and consistent policy enforcement are required across services
- cross-service telemetry must be standardized
- traffic shifting/retries/timeouts must be enforced consistently

### Mesh adoption guidance (if adopted)
- start small (one namespace/segment)
- prioritize observability first, policy second
- treat the control plane as tier-0:
  - HA, monitoring, safe upgrades
- avoid anti-patterns:
  - mesh as ESB (no business logic)
  - mesh as API gateway replacement
  - too many networking layers (policy conflicts + latency)

---

## 7) Migration guardrails (must not regress)

During cloud/hybrid migration, preserve:
- **Contract stability** (OpenAPI diff checks)
- **Read path SLOs** (latency/error budgets remain acceptable)
- **Non-blocking side effects** (audit/email/aggregation remain async)
- **Security posture** (no gaps in authz coverage, rate limits, safe logging)
- **Observability continuity** (correlation IDs across hops, backlog/lag visibility)

These guardrails map to:
- `../architecture/arc42/05-quality-requirements.md`
- `../architecture/arc42/06-measurement-guide.md`
- `../architecture/arc42/07-architecture-governance.md`

---

## 8) Practical V2+ migration roadmap (example)

### Phase 1 — Replatform the baseline
- containerize API and Worker
- move to managed runtime (platform choice)
- centralize metrics/logs and validate SLO baselines

### Phase 2 — Introduce read models/projections
- Public Query evolves into read model component
- event-driven projections reduce read pressure on transactional stores

### Phase 3 — Slice out hot domains
- Interaction and/or Media become separate components if metrics justify
- keep contracts stable via gateway facade routing

### Phase 4 — Zero trust hardening
- introduce workload identity and mTLS across services (mesh if justified)
- enforce service-to-service authorization policies

---

## 9) ADR hooks (decisions to record)

Record ADRs for:
- migration strategy choice per component (6Rs)
- gateway/APIM selection and routing policy
- mesh adoption decision (type-1 decision) and rollout plan
- zero trust posture (what is enforced where)
- hybrid connectivity patterns (if applicable)