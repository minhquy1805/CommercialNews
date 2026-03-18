# Architecture Style Decision Criteria (Reference)

This document defines the **shared evaluation criteria** used to select architecture styles for CommercialNews.
It is intentionally **style-agnostic** and reusable for future decisions (e.g., when splitting components, adding read models, or adopting new integration patterns).

> Related:
> - System priorities: `docs/explanation/architecture/arc42/05-quality-requirements.md`
> - Module boundaries: `docs/explanation/architecture/arc42/03-building-blocks-modularity.md`
> - Runtime scenarios: `docs/explanation/architecture/arc42/04-runtime-view-v1.md`
> - Coupling reference: `docs/reference/connascence-and-contract-coupling.md`
> - Architecture quantum reference: `docs/reference/architecture-quantum.md`

---

## 1) Evaluation mindset

Selecting an architecture style is **not selecting a diagram**.  
It is choosing a structural approach that best supports:

- the domain’s workload shape and core workflows
- the highest-priority architecture characteristics
- data ownership and consistency needs
- team/process maturity and operational constraints
- long-term evolution with minimal coupling and manageable complexity

**Output of the decision process**
1) An architecture topology (style or hybrid style)
2) ADR(s) documenting key trade-offs
3) Governance/fitness functions to protect important characteristics over time

---

## 2) Inputs required before deciding

### A) Domain understanding (workload and behavior)
Capture:
- read-heavy vs write-heavy
- burstiness (spikes vs steady load)
- user interaction patterns (peak times, “hot events”)
- abuse/spam/attacker behaviors relevant to the domain

**Evidence sources**
- `capability-requirements.md` (explicit + inferred + additional context)
- `domain-concerns.md` (rules, invariants, risks)

---

### B) Architecture characteristics that shape structure
Identify the top characteristics (usually 5–7 system-level), and any “hot” module profiles.

Typical examples:
- security
- read path performance and availability
- reliability/resilience of async workflows
- recoverability (backup/restore)
- maintainability/evolvability
- observability

**Evidence sources**
- `05-quality-requirements.md`
- module profiles under `arc42/quality/`

---

### C) Data architecture (ownership, consistency, and transactions)
Decide:
- where data lives (shared DB vs owned schema vs per-service DB)
- whether cross-module ACID transactions are required
- whether eventual consistency is acceptable (and where)

**Key anti-pattern to avoid**
- “distributed monolith”: split services but keep shared schema and cross-service coupling.

---

### D) Organizational and operational factors
Consider:
- team size and skill maturity
- CI/CD maturity and test automation
- observability/on-call readiness
- operational cost budget (tooling, monitoring, incident handling)

---

### E) Domain/architecture isomorphism
Evaluate whether the domain naturally aligns with certain styles:
- plugin/customization-heavy → microkernel
- stepwise transformations → pipeline
- extreme scale + contention → space-based
- independently scalable capabilities → service-based/microservices
- heavy side effects and decoupled workflows → event-driven

---

## 3) The three “big decisions” to make explicitly

### 1) Monolith vs Distributed
Ask:
- Do we need **one** dominant set of architecture characteristics?
- Or do parts of the system require **very different** characteristics (scale, latency, consistency, deployability)?

Guideline:
- If most capabilities share similar characteristics → start monolithic (modular) and evolve.
- If a capability is operationally “different” → consider isolating it as a separate component first.

---

### 2) Data placement and ownership
Ask:
- Which capability is the source of truth for a given data set?
- Can ownership be enforced (no cross-module direct writes)?
- If distributed, can we avoid cross-service joins and shared schema coupling?

---

### 3) Synchronous vs Asynchronous integration
Guideline (pragmatic):
- Use **synchronous** communication when immediate answers are required to complete the user flow.
- Use **asynchronous** communication for side effects and burst absorption (audit, notifications, aggregation, indexing).

Important:
- Async reduces call-time coupling but increases contract and delivery-semantic discipline
  (schema versioning, idempotency, DLQ, lag monitoring).

---

## 4) Scoring rubric (lightweight)

Use this rubric to compare candidate styles.

For each candidate style, rate 1–5:
- **Simplicity / operational cost** (lower ops burden is higher score)
- **Support for read path performance and availability**
- **Support for burst handling**
- **Security posture and governance friendliness**
- **Data consistency fit** (ACID vs eventual consistency)
- **Maintainability / evolvability**
- **Deployability independence** (only if needed)
- **Observability requirements** (how hard to observe/debug)

> Tip: avoid over-optimizing for deployability independence if you do not need it in V1.

---

## 5) Checklist (quick, decision-ready)

Before selecting a system style, answer these:

1) Is the domain primarily **read-heavy** or **write-heavy**? Is traffic **bursty**?
2) What are the **top 3–7** characteristics we must optimize (and what can be “good enough”)?
3) Where does data live, and do we need **cross-module transactions**?
4) Are we integrating with legacy systems or external partners?
5) Do we have enough CI/CD, testing, and observability maturity for distributed complexity?
6) What operational cost (time/money) is acceptable in V1?
7) Are bounded contexts clear enough to support service boundaries if needed later?
8) Can we enforce data ownership and avoid cross-boundary schema coupling?
9) Which workflows require sync responses, and which are better as async side effects?
10) What ADRs and fitness functions will protect the decision over time?

---

## 6) Typical guidance by style (when it tends to fit)

- **Layered**: good for technical separation, but can smear domain workflows across layers at system topology level.
- **Pipeline**: best for stepwise transformations/ETL; not ideal as primary topology for CRUD/read-heavy domains.
- **Microkernel**: best for plugin/customization ecosystems.
- **Service-Based**: good balance for domain partitioning with low ops overhead; supports evolution into distributed later.
- **Event-Driven**: excellent for side effects, decoupling, and burst buffering; requires contract discipline.
- **Space-Based**: suited for extreme scale and contention; ops-heavy for most V1 projects.
- **Microservices**: best when independent scaling/deployability is required and the org is mature in CI/CD/observability.

---