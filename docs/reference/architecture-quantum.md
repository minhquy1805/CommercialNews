# Architecture Quantum (Reference)

This note explains how CommercialNews uses architecture quantum thinking to
reason about deployability, cohesion, runtime coupling, and future splits.

> Reading flow:
> - `docs/explanation/architecture/arc42/03-building-blocks-modularity.md`
> - `docs/explanation/architecture/arc42/04-runtime-view-v1.md`
> - `docs/explanation/architecture/arc42/09-architecture-style.md`
> - `docs/reference/connascence-and-contract-coupling.md`
> - `docs/explanation/decisions/`

## 1) Definition

An architecture quantum is an independently deployable artifact with:

- high functional cohesion
- synchronous connascence inside the boundary
- its own operational fate

Practical meaning:

- Inside a quantum, synchronous wiring can be acceptable because components
  succeed or fail together.
- Between quanta, synchronous coupling should be minimized.
- Async integration helps keep independently evolving parts from becoming one
  operational unit.

## 2) CommercialNews V1 Reality

CommercialNews V1 is a modular monolith backend with two runtime hosts:

- `CommercialNews.Api`
- `CommercialNews.Worker`

It uses:

- one SQL Server database with module-owned schemas
- RabbitMQ for event delivery
- Outbox for reliable publication
- Docker Compose for local and production-style packaging

Because API, Worker, SQL Server, and RabbitMQ are operationally connected in V1,
the deployed backend behaves mostly as one product quantum. Inside that product
quantum, modules preserve logical ownership and keep open the option to split
later.

## 3) Module Boundaries Are Still Real

V1 modules are not independently deployed services yet, but they are important
architecture boundaries:

- Content owns article lifecycle truth.
- SEO owns slug routes and SEO metadata.
- Media owns media assets and article-media attachment state.
- Reading owns public serving projections.
- Interaction owns engagement, comments, moderation signals, and counter snapshots.
- Identity owns users, credentials, sessions, verification, and reset flows.
- Authorization owns roles, permissions, and policy evaluation.
- Audit owns audit evidence and ingestion tracking.
- Notifications owns email delivery state.
- Outbox owns reliable publication infrastructure.

The shared database does not mean shared ownership. Each schema is owned by one
module and should not be treated as a common data bucket.

## 4) Runtime Lanes

CommercialNews uses three lanes.

### Lane A: synchronous request/response

Used for truth writes and immediate user/admin outcomes:

- create, edit, publish, unpublish articles
- login, refresh, verify, reset password
- role and permission changes
- public reads served from Reading projections
- admin reads and investigations

### Lane B: async event-driven side effects

Used for work that should not block the truth transaction:

- audit ingestion
- email delivery
- Reading projection updates
- SEO reactions to content lifecycle events
- Interaction counter publication
- notifications and alerting

Standard shape:

```text
Owner module truth transaction
  -> OutboxMessage
  -> Worker publishes to RabbitMQ
  -> Worker consumer handles message idempotently
  -> Consumer-owned state changes
```

### Lane C: batch, rebuild, and reconciliation

Used for:

- projection repair
- aggregation
- retention cleanup
- rebuilding derived outputs
- operational reconciliation

Batch output must not redefine source truth.

## 5) Candidate Quanta For Future Splits

Current conceptual candidates:

### Product authoring quantum

- Content
- SEO
- Media

This group has tight editorial cohesion, but still keeps module ownership.

### Public serving quantum

- Reading
- selected cache/projection infrastructure

Reading is the main candidate for independent scaling because public traffic is
bursty and latency-sensitive.

### Interaction quantum

- Interaction
- stats materialization
- public counter publication

Interaction can become operationally hot and abuse-prone.

### Security and governance quantum

- Identity
- Authorization
- Audit

These modules share security and governance concerns, but Audit remains async
and append-only where possible.

### Notification quantum

- Notifications
- email delivery workers

Notifications is naturally async and provider-dependent.

## 6) Split Checklist

Splitting a module into its own deployable quantum is justified when:

- it has clearly different scale, latency, or reliability needs
- it can own its data boundary without cross-schema joins
- contracts can be versioned safely
- sync calls to other modules are bounded or removed
- operational metrics show real benefit
- the split reduces change coupling rather than increasing it

Avoid splitting when:

- the module still requires many synchronous calls to complete ordinary work
- it shares domain models or DTOs with other modules
- database ownership is unclear
- consumers cannot tolerate event lag or duplicates
- most features still change several modules together

## 7) Guardrails

Avoid distributed-monolith patterns:

- shared domain DTO packages across modules or services
- direct cross-module table access
- long synchronous dependency chains
- treating RabbitMQ messages as permanent replay history
- letting derived state become hidden source truth

Keep:

- module-owned schemas
- stable public/cross-module IDs
- explicit event contracts
- idempotent consumers
- observable backlog and lag
- repair and rebuild paths for derived state

## 8) Summary

V1 is one deployable backend system with strong module boundaries. The goal is
not to split early. The goal is to make future splits possible by controlling
connascence now.
