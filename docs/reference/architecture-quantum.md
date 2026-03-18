# Architecture Quantum (Reference)

This note explains **architecture quantum** and how we use it in CommercialNews to reason about:
- the scope of architecture characteristics (system vs quantum)
- sync vs async coupling boundaries
- independent deployment and evolution

> Reading flow:
> - `docs/explanation/architecture/arc42/03-building-blocks-modularity.md`
> - `docs/explanation/architecture/arc42/04-runtime-view-v1.md`
> - `docs/reference/connascence-and-contract-coupling.md`
> - `docs/explanation/decisions/` (ADRs)

---

## 1) Definition

**Architecture quantum** = an independently deployable artifact with:
- **high functional cohesion**, and
- **synchronous connascence** (runtime coupling through call-and-wait interactions).

Practical interpretation:
- A quantum is the smallest meaningful unit that can **run and evolve independently**,
  while containing cohesive functionality.
- Inside a quantum, synchronous wiring is acceptable because components share operational fate.
- Between quanta, prefer async integration to reduce call-time coupling.

---

## 2) The three parts (and what they imply)

### 2.1 Independently deployable
A quantum contains everything required to run “for real”.

**Implication**
- If a component requires a database to function, the database is part of the quantum.
- Sharing a DB/schema across “independent services” often forces coordinated releases.

**CommercialNews note**
- V1 is a modular monolith with a shared DB but **owned schema per module**.
  This keeps logical independence and preserves the option to split later.

---

### 2.2 High functional cohesion
A quantum should serve a clear business capability, not be a “misc bucket”.

**Implication**
- Cohesion aligns naturally with domain boundaries (capabilities / bounded contexts).
- Splitting by technical layers or tables often yields low cohesion and high coordination cost.

**CommercialNews note**
- Our module map (Content, SEO, Media, Public Query, Interaction, Identity, Authorization, Audit, Notifications)
  is designed to preserve functional cohesion.

---

### 2.3 Synchronous connascence
Synchronous calls create runtime coupling:
- latency composes
- availability composes
- failures cascade

**Implication**
- A chain of synchronous calls can behave like “one quantum” operationally.
- To keep quanta independent, reduce synchronous wiring between them.

**CommercialNews note**
- Read path must remain bounded: avoid long synchronous dependency chains.
- Side effects (audit, notifications, view aggregation, indexing) use async boundaries.

---

## 3) Why characteristics may be defined at quantum level
Some characteristics are best measured and optimized at the quantum boundary, not system-wide.
Examples:
- a read-path quantum may prioritize performance and availability
- a notification quantum may prioritize reliability and backlog visibility

**CommercialNews note**
- System-level characteristics exist (security, read performance, availability, recoverability, etc.).
- Module profiles identify which characteristics dominate each capability.

---

## 4) CommercialNews: candidate quanta (pragmatic view)

### V1 (current)
- Primary “product quantum”: **Content + SEO + Media + Public Query** (tight product cohesion)
- Hot-path quantum: **Interaction** (operationally distinct; can scale independently)
- Security/governance quantum: **Identity + Authorization + Audit**
- Async side-effect quantum: **Notifications**

> Note: This is a conceptual grouping, not necessarily deployment units in V1.

### V2+ (possible evolution)
- Keep the read path bounded; introduce a **Read Model** quantum if burst traffic requires it.
- Keep side effects asynchronous: indexing, notifications, audit ingestion.

---

## 5) Checklist: deciding whether to split a module into its own deployable quantum

A split is justified when:
- the module has distinct operational needs (scale, latency, reliability)
- deployment independence delivers real value
- contracts can be versioned safely
- data ownership is clear (no shared schema coupling)

Avoid splitting if:
- the module shares domain models heavily with others
- it requires many synchronous calls to function
- it cannot own its data boundary without coordination overhead

---

## 6) Related risks and guardrails

Common “distributed monolith” indicators:
- shared domain DTO packages across services
- shared DB schema with cross-service joins
- long synchronous dependency chains on the read path

Guardrails:
- see `docs/explanation/architecture/arc42/07-architecture-governance.md`
- see `docs/reference/connascence-and-contract-coupling.md`

---