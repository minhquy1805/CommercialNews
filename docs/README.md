# CommercialNews Documentation

CommercialNews docs follow the Diataxis idea:

- Tutorials: step-by-step learning paths.
- Explanation: architecture and domain reasoning.
- Reference: factual lookup and operating models.
- How-to guides: goal-oriented operational procedures.

Current docs mainly use Tutorials, Explanation, and Reference. Add How-to only
when we have a stable procedure that operators or developers repeat.

## Start Here

New contributor:

- `tutorials/01-onboarding.md`

System overview:

- `explanation/architecture/arc42/00-index.md`
- `reference/architecture-quantum.md`
- `reference/architect-operating-model.md`

Domain overview:

- `explanation/domain/business-capabilities.md`
- `explanation/domain/capability-requirements.md`
- `explanation/domain/domain-concerns.md`

API architecture:

- `explanation/api-architecture/00-index.md`
- `explanation/api-architecture/README.md`

Decision history:

- `explanation/decisions/README.md`

## Folder Guide

### `tutorials/`

Use tutorials for guided, sequential learning.

Current tutorial:

- `01-onboarding.md`: local development onboarding for API, Worker, SQL Server, RabbitMQ, Docker, DB bootstrap, and first smoke checks.

Keep this folder small. Do not put long architecture essays or one-off deployment notes here.

### `explanation/`

Use explanation docs for why the system is shaped this way.

Important areas:

- `architecture/arc42/`: architecture overview, runtime view, quality requirements, governance, data and consistency policy.
- `api-architecture/`: API contracts, standards, versioning, security, observability, release/evolution topics.
- `domain/`: business capabilities, capability requirements, domain concerns.
- `decisions/`: ADRs for architecture decisions.

Explanation docs may be narrative and contextual. They should connect decisions,
tradeoffs, and consequences.

### `reference/`

Use reference docs for stable lookup material.

Important references:

- `architect-operating-model.md`: ownership and governance rules for architecture work.
- `developer-operating-model.md`: developer responsibilities inside the architecture.
- `architecture-quantum.md`: deployability, cohesion, and future split reasoning.
- `architecture-style-decision-criteria.md`: criteria for choosing architecture style.
- `component-identification-cycle.md`: process for identifying components.
- `connascence-and-contract-coupling.md`: coupling vocabulary and guardrails.
- `module-template.md`: recommended module structure.
- `source-structure.md`: repository/source layout.

Reference docs should be direct, factual, and easy to scan.

## Current Backend Shape

CommercialNews V1 is a modular backend with:

- `CommercialNews.Api`: HTTP API host.
- `CommercialNews.Worker`: outbox publisher, RabbitMQ consumers, projections, audit ingestion, notification delivery, and batch-style work.
- SQL Server: one database with module-owned schemas.
- RabbitMQ: at-least-once event transport.
- Outbox: reliable event publication.
- Docker Compose: local dev runtime for API, Worker, SQL Server, RabbitMQ, and Nginx.

Current modules:

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

## Documentation Rules

When changing architecture direction:

- update the relevant arc42 explanation
- update reference docs if operating rules changed
- add or update an ADR when the decision is durable

When changing module ownership or data shape:

- update domain explanation
- update system-data or DB-related architecture docs when applicable
- update reference docs if the rule affects future work

When changing API contracts:

- update API architecture docs if the standard changed
- update concrete endpoint docs once endpoint documentation exists

When changing Docker or local workflow:

- update `tutorials/01-onboarding.md`
- update deployment/how-to docs if a stable operational guide exists

When changing Worker, Outbox, RabbitMQ, or consumer behavior:

- update runtime/consistency explanation
- update onboarding smoke checks if local behavior changes
- update ADRs if envelope, idempotency, or delivery semantics changed

## Writing Style

- Keep tutorials procedural.
- Keep reference concise.
- Keep explanation contextual.
- Prefer current-state wording over aspirational wording.
- Avoid documenting implementation details that are likely to churn unless they are part of a contract or operating rule.
