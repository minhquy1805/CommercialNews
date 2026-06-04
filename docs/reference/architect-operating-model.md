# Architect Operating Model (CommercialNews)

This document defines how architecture is defined, refined, managed, and
governed in CommercialNews. It is intentionally lightweight and project-specific.

> Links:
> - Modules and dependency rules: `docs/explanation/architecture/arc42/03-building-blocks-modularity.md`
> - Runtime view: `docs/explanation/architecture/arc42/04-runtime-view-v1.md`
> - Architecture style: `docs/explanation/architecture/arc42/09-architecture-style.md`
> - Quality requirements: `docs/explanation/architecture/arc42/05-quality-requirements.md`
> - Measurement guide: `docs/explanation/architecture/arc42/06-measurement-guide.md`
> - Governance and fitness functions: `docs/explanation/architecture/arc42/07-architecture-governance.md`
> - Module template: `docs/reference/module-template.md`

## 1) Define

Architecture starts by defining ownership.

### Business modules

- Content
- SEO
- Media
- Reading
- Interaction
- Identity
- Authorization
- Audit
- Notifications
- Outbox

### Runtime hosts

- API host: HTTP routing, auth policy attributes, HTTP contracts, MediatR entry points.
- Worker host: outbox publishing, RabbitMQ consumers, integration handlers, projection work, email work, batch-style jobs.

### Data ownership

- One SQL Server database in V1.
- One schema owner per module.
- Stored procedures are part of the module persistence contract.
- Other modules must not treat another module's tables as shared data.

## 2) Refine

Refine architecture when real signals appear:

- repeated cross-module changes for one feature
- a use case requires data that belongs to another module
- read path latency regresses because of sync dependency chains
- Worker backlog or queue lag becomes a product risk
- module scale or reliability needs diverge strongly
- event contracts become hard to evolve safely
- DB schema changes require too much cross-module coordination

Refinement should usually happen in this order:

1. clarify ownership
2. clarify contracts
3. reduce sync coupling
4. add observability
5. split deployment only when justified

## 3) Manage

### Domain

Domain owns:

- invariants
- value objects
- domain exceptions
- constants that define domain vocabulary
- pure policy interfaces and default pure policy implementations when they do not need infrastructure

Domain does not own:

- SQL
- broker clients
- HTTP
- filesystem
- external service clients

### Application

Application owns:

- use cases
- MediatR request/handler contracts
- FluentValidation validators
- pipeline behaviors
- repository and service ports
- transaction intent markers
- result models used by API and other hosts

Handlers should not open transactions manually. Transaction behavior wraps
commands that need persistence transactions.

### Infrastructure

Infrastructure owns:

- repository implementations
- SQL unit of work
- SQL parameter helpers and data mappers
- stored procedure names and persistence exception translation
- serializers
- redaction implementations
- normalizer implementations
- external clients and provider-specific integrations

Repositories should stay thin: call stored procedures, map rows, and return
domain/application results.

### API

API owns:

- controllers
- route design
- authorization policy attributes
- HTTP request and response contracts
- HTTP mapping
- HTTP-only parsing such as `sort=-occurredAtUtc`

API should not contain domain policy logic, SQL logic, or broker logic.

### Worker

Worker owns:

- outbox polling
- RabbitMQ publishing
- RabbitMQ consumers
- envelope validation
- mapping broker messages to Application commands
- runtime retries, acks, nacks, and logging

Worker should call Application use cases rather than bypass module boundaries.

## 4) Govern

Architecture governance is a set of small guardrails.

### Module boundaries

- No shared domain entities across modules.
- No direct cross-module table access unless explicitly documented as a temporary V1 exception.
- Cross-module references use IDs, not object graphs.
- Events use stable contracts and idempotency keys.

### Data and persistence

- DB scripts are module-owned:
  - `001_tables.sql`
  - `010_indexes.sql`
  - `020_procs.sql`
- Stored procedures should use explicit column lists.
- SQL schema names should not be changed casually.
- Public IDs are API/admin-facing; internal identity columns remain internal.

### Async integration

- Truth write plus outbox message must be atomic when side effects are required.
- RabbitMQ delivery is at-least-once.
- Consumers must be idempotent.
- Derived state may lag and must be repairable.
- Queue depth, oldest message age, retry counts, and dead-letter state must be observable.

### Security and privacy

- Admin endpoints require explicit policy coverage.
- Sensitive payloads must be redacted before becoming audit evidence or responses.
- Raw sensitive payloads should not be exposed through admin APIs.
- Logs should include correlation IDs and avoid secrets.

## 5) ADR Triggers

Write an ADR when changing:

- module boundaries
- runtime host responsibilities
- sync versus async integration strategy
- event envelope shape
- event payload contracts
- transaction and outbox semantics
- data ownership or schema ownership
- retention policies
- public identifier strategy
- security or authorization policy model

## 6) Day-To-Day Review Questions

Ask these before merging a significant change:

- Which module owns this rule?
- Which module owns this data?
- Is this use case truth-changing or derived-state work?
- Should this be sync, async, or batch?
- What is the idempotency key?
- What happens if the Worker is down?
- What happens if RabbitMQ redelivers the message?
- Is the public API exposing internal IDs or raw sensitive data?
- Does the DB script, Application model, Infrastructure mapper, API contract, and docs all agree?
