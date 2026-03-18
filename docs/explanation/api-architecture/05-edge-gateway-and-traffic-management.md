# Edge Gateway & Traffic Management — CommercialNews (V1)

This document applies Chapter 3 (API Gateways) and traffic management guidance to CommercialNews.
It defines how we handle **north–south traffic** (client → system) and how we use routing to support safe evolution.

Related:
- Constraints: `../architecture/arc42/02-constraints.md`
- Modularity & dependency rules: `../architecture/arc42/03-building-blocks-modularity.md`
- Runtime scenarios: `../architecture/arc42/04-runtime-view-v1.md`
- Components: `../architecture/arc42/08-components.md`
- Architecture style: `../architecture/arc42/09-architecture-style.md`

---

## 1) Purpose of the edge gateway in CommercialNews

The edge gateway (Ingress / API Gateway / APIM) is a **tier-0 component** because:
- all north–south traffic passes through it
- it is the first enforcement point for baseline policies
- it is a migration tool (strangler/facade routing)

In V1, CommercialNews is a modular monolith (API + Worker), so the gateway mainly:
- terminates TLS
- routes requests to the API component
- applies baseline protections and observability

---

## 2) North–south vs east–west: keep the boundary clean

### 2.1 North–south (edge)
- Client → Gateway/Ingress → Public API component

Responsibilities:
- TLS termination
- host/path routing
- basic protections (rate limits/WAF if available)
- request logging/metrics at the edge

### 2.2 East–west (internal)
In V1:
- module-to-module calls are **in-process** (no internal network hop)

If V2+ introduces services:
- east–west uses internal service discovery / mesh (not edge routing)

**Rule:** no gateway loopback. Internal services/workers must not call back through the public edge (`https://api...`) for internal communication.

---

## 3) Gateway responsibilities (what belongs at the edge)

### 3.1 Allowed at the gateway
- TLS termination and certificate management
- host/path-based routing
- request size limits (payload protection)
- baseline rate limiting (coarse; detailed per-endpoint controls still in API)
- security headers hardening (baseline)
- edge-level access logs and metrics
- health checks and readiness routing
- canary routing (if used for progressive delivery)

### 3.2 Forbidden at the gateway
- domain/business logic (no ESB behavior)
- data enrichment that affects domain semantics
- rewriting payloads in ways that change API contracts
- making the gateway a “mini application layer”

**Rule:** the gateway must never become a place where domain rules live.

---

## 4) Routing and path conventions (contract-friendly)

### 4.1 Stable base paths
CommercialNews uses stable versioned roots:
- `/api/v1/...`
- `/api/v1/admin/...`

### 4.2 Contract anchor
Clients should depend on:
- `api.commercialnews.com` (single contract anchor)

Backends may evolve behind this anchor without breaking consumers, using routing rules.

---

## 5) Traffic management patterns (how we evolve safely)

### 5.1 Strangler Fig routing (migration facade)
Use path/host routing to move traffic gradually:
- `/api/v1/media/*` → Media service (V2+)
- `/api/v1/*` → legacy API component (until migrated)

Key rules:
- keep the client contract stable
- move one slice at a time
- monitor and roll back quickly

### 5.2 Canary routing (progressive delivery)
When rolling out a new version:
- route a small percentage of traffic to the canary
- increase gradually with gates (latency/error/security signals)

Gates (policy-level):
- 5xx/timeouts
- P95/P99 latency
- 401/403 spikes (security signals)

### 5.3 Mirroring (dark launch)
For risky changes (performance/refactor):
- mirror traffic to a new backend without impacting users
- compare responses and performance internally

**Rule:** mirrored traffic must not cause side effects (no writes), or must be explicitly isolated.

---

## 6) SPOF avoidance (edge reliability requirements)

Because the gateway is tier-0:
- run multiple replicas
- use readiness/liveness probes
- use safe config rollout practices (canary for config, where possible)
- keep configuration under version control
- ensure fast rollback of routing/TLS config
- expose edge metrics and logs

---

## 7) Observability at the edge (minimum)

The gateway should provide:
- request rate, error rate, latency percentiles (where possible)
- TLS handshake errors / cert issues
- routing failures (404/502/503 at edge)
- correlation propagation (pass through correlation headers)

**Rule:** correlation ID must not be dropped by the gateway.

---

## 8) Anti-patterns to avoid (hard rules)

### 8.1 Gateway loopback
Internal services calling the public gateway again:
- increases latency and cost
- amplifies blast radius
- complicates debugging

### 8.2 Gateway as ESB
Adding business rules at the gateway:
- introduces hidden coupling
- makes rollouts fragile
- creates unclear ownership

### 8.3 Too many gateway layers (“turtles all the way down”)
Stacking multiple gateway layers causes:
- higher latency
- policy conflicts
- unclear ownership and failure modes

CommercialNews rule:
- keep the edge simple in V1
- add APIM features only when justified by requirements (keys/quotas/partner onboarding/portal)

---

## 9) Decision triggers (when to upgrade beyond simple Ingress)

Consider APIM / advanced gateway features when:
- partner onboarding requires keys/quotas/portals
- more complex traffic shaping is needed (per-client quotas, advanced analytics)
- security posture requires WAF/DDoS features at the edge
- multi-backend/hybrid routing becomes common (cloud migration)

Record upgrades as ADRs:
- requirements and constraints
- chosen option and rationale
- operational impacts and rollback plan

---

## 10) CommercialNews V1 implementation notes (policy-level)

V1 is expected to run with:
- a single Public API component behind an Ingress/gateway
- a Worker component for async workflows (not exposed publicly)

Edge goals:
- protect the read path
- provide stable contract anchor
- support future strangler routing without breaking clients