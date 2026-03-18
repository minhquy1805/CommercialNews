# Module Template (CommercialNews)

This template defines a scalable internal structure for each module under `src/Modules/{ModuleName}/`.
It is designed for a **Service-Based Modular Monolith** with **Clean Architecture** and optional **event-driven side effects**.

Use this as a starting point and keep it consistent across modules to reduce cognitive load.

---

## 1) Folder Template

src/
└─ Modules/
   └─ {ModuleName}/

      ├─ {ModuleName}.Domain/
      │  ├─ Aggregates/
      │  ├─ Entities/
      │  ├─ ValueObjects/
      │  ├─ DomainEvents/              (optional)
      │  ├─ Specifications/            (optional)
      │  ├─ Services/                  (pure domain services)
      │  ├─ Rules/                     (invariants, state transitions)
      │  ├─ Errors/                    (domain errors)
      │  ├─ Constants/                 (domain constants)
      │  └─ README.md                  (module overview, invariants, glossary)
      │
      ├─ {ModuleName}.Application/
      │  ├─ Abstractions/
      │  │  ├─ Persistence/            (repositories, unit of work if used)
      │  │  ├─ Messaging/              (event publisher interfaces if module-specific)
      │  │  ├─ Security/               (authorization context interfaces if needed)
      │  │  ├─ Time/                   (IClock usage or module-specific time ports)
      │  │  └─ External/               (external service ports, e.g., IEmailSender)
      │  │
      │  ├─ UseCases/
      │  │  ├─ Commands/
      │  │  └─ Queries/
      │  │
      │  ├─ Shared/
      │  │  ├─ Dtos/
      │  │  ├─ Mapping/
      │  │  ├─ Validation/
      │  │  └─ Errors/
      │  │
      │  ├─ Contracts/                 (request/response contracts if not in *.Contracts)
      │  ├─ Events/
      │  ├─ Outbox/                    (optional)
      │  ├─ Handlers/                  (application-level event handlers, not broker consumers)
      │  ├─ Contracts/                 (event payload types if not in *.Contracts)
      │  ├─ Policies/                  (business policies enforced at application boundary)
      │  └─ README.md                  (use cases list, public API of the module)
      │
      ├─ {ModuleName}.Infrastructure/
      │  ├─ Persistence/
      │  │  ├─ Sql/                    (or Mongo/)
      │  │  ├─ Repositories/
      │  │  └─ Migrations/             (optional, if applicable)
      │  │
      │  ├─ Config/
      │  ├─ Messaging/
      │  │  ├─ Producers/
      │  │  ├─ Consumers/              (only if infra-level handlers are used here)
      │  │  └─ Config/
      │  │
      │  ├─ External/
      │  │  ├─ Email/                  (if module owns email client - usually Notifications module)
      │  │  └─ Storage/                (object storage, CDN clients)
      │  │
      │  ├─ Observability/
      │  │  ├─ Logging/
      │  │  ├─ Metrics/
      │  │  └─ Config/
      │  │
      │  └─ README.md                  (infra notes, connection details, operational concerns)
      │
      └─ {ModuleName}.Contracts/       (optional but recommended)
         ├─ Api/
         │  ├─ Requests/
         │  └─ Responses/
         ├─ Events/
         │  ├─ V1/
         │  └─ V2/                     (future)
         └─ README.md                  (versioning rules, compatibility notes)


---

## 2) What goes where (rules)

### Domain
Put here:
- business invariants and state transitions
- aggregates/entities/value objects
- domain-level validation rules (not request-shape validation)
- domain errors (expressed in domain language)

Avoid:
- framework dependencies (ASP.NET Core)
- database code
- message broker code

### Application
Put here:
- use cases (commands/queries)
- orchestration (calling repositories, emitting events, enforcing policies)
- interfaces (ports) that Infrastructure implements
- request-level validation and mapping
- application-level events (what the module emits)

Avoid:
- concrete database implementations
- concrete broker clients
- web concerns (controllers)

### Infrastructure
Put here:
- repository implementations (SQL/Mongo)
- broker clients (RabbitMQ/Kafka) wiring
- external service clients (storage, email provider if owned here)
- configuration and operational wiring

### Contracts (optional)
Put here:
- DTOs and event payload schemas meant to be consumed outside the module
- versioning notes and compatibility rules

Avoid:
- domain entities and internal models

---

## 3) Public surface of a module (recommended)

Each module should expose a small, clear “public API” through Application, e.g.:
- `I{ModuleName}Service` or a set of command/query handlers
- `Publish/Emit` events through an abstraction (e.g., `IEventPublisher`)

Other modules should depend only on:
- `{ModuleName}.Contracts` (if used)
- `{ModuleName}.Application` interfaces (sparingly)
- IDs (SharedKernel)

---

## 4) Event-driven usage (recommended pattern)

### Outgoing events (producer side)
- Application emits events after successful state changes.
- Events are minimal and versionable.

Recommended event naming:
- `{Aggregate}{Action}Occurred` (e.g., `ArticlePublished`)
or
- `{Module}.{Entity}.{Action}` (e.g., `Content.Article.Published`)

### Incoming events (consumer side)
- Broker consumers usually live in the **Worker host** and call Application services.
- Application-level event handlers (inside module) should be pure and testable.

Idempotency rule:
- Assume at-least-once delivery; event handlers must be retry-safe.

---

## 5) Naming conventions (suggested)

- Commands: `{Verb}{Noun}Command` (e.g., `PublishArticleCommand`)
- Queries: `{Get/Find}{Noun}Query` (e.g., `GetArticleBySlugQuery`)
- Handlers: `{Command/Query}Handler`
- DTOs: `{Noun}Dto`, `{Noun}SummaryDto`
- Repositories: `I{Aggregate}Repository`, `{Db} {Aggregate}Repository`
- Policies: `{Topic}Policy` (e.g., `SlugStabilityPolicy`)

---

## 6) Minimal README.md per module (recommended)

### `{ModuleName}.Domain/README.md`
Include:
- module purpose
- core invariants (3–10 bullets)
- key domain terms (mini glossary)
- main aggregates and their responsibilities

### `{ModuleName}.Application/README.md`
Include:
- list of commands/queries (use cases)
- emitted events
- external dependencies (ports)
- key policies and trade-offs

---

## 7) When to add complexity (guidelines)

Add only when justified by signals:
- Separate `ReadModel/` when read complexity or burst traffic becomes painful
- Add `Outbox/` when reliable event publishing is required across failures
- Add `Policies/` when rules grow and need centralized enforcement

---

## 8) Example: applying the template (Content module)

- Domain: `Article` aggregate with lifecycle invariants
- Application:
  - Commands: `CreateDraft`, `UpdateDraft`, `Publish`, `Unpublish`
  - Events: `ArticlePublished`, `ArticleUnpublished`
- Infrastructure:
  - SQL repository for articles
- Contracts:
  - API DTOs for admin endpoints
  - Event payloads for published/unpublished