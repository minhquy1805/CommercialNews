# Source Structure (Service-Based Modular Monolith + Clean Architecture)

This document defines the recommended **source code structure** for CommercialNews.
The goal is to support:
- clear module ownership (domain partitioning)
- clean dependency direction (Clean Architecture)
- scalable growth (more modules, more components, optional microservice extraction later)
- safe integration (sync calls minimized, async events for side effects)

---

## 1) High-level Layout

src/
├─ building-blocks/
│  ├─ CommercialNews.BuildingBlocks.Core/
│  ├─ CommercialNews.BuildingBlocks.Persistence.Sql/
│  ├─ CommercialNews.BuildingBlocks.Outbox/
│  ├─ CommercialNews.BuildingBlocks.Storage/
│  └─ CommercialNews.BuildingBlocks.Storage.Infrastructure/
│
├─ modules/
│  ├─ Content/
│  ├─ Seo/
│  ├─ Media/
│  ├─ Reading/
│  ├─ Interaction/
│  ├─ Identity/
│  ├─ Authorization/
│  ├─ Audit/
│  └─ Notifications/
│
├─ hosts/
│  ├─ CommercialNews.Api/
│  └─ CommercialNews.Worker/
│
├─ tests/
│  ├─ Unit/
│  └─ Integration/
│
└─ docs/

---

## 2) Core principles (non-negotiable)

### 2.1 Module ownership
- Each module owns its **domain rules** and **data model** (logical ownership even if DB is shared).
- Other modules reference it by **IDs**, not by importing its domain entities.

### 2.2 Dependency direction (Clean Architecture)
- Domain has **no dependencies** on other layers.
- Application depends on Domain.
- Infrastructure implements interfaces defined in Application (or module-level abstractions).
- Hosts wire everything together but should contain **no business logic**.

### 2.3 Integration rule
- **Sync**: only for immediate user flows.
- **Async (events)**: for side effects and burst isolation (audit, notifications, aggregation, indexing).

### 2.4 Avoid static connascence
- Do not share domain DTO/entities across modules as shared packages.
- Use explicit contracts and versioning for cross-module communication.

(See: `docs/reference/connascence-and-contract-coupling.md`)

---

Top-level folders use lowercase names in the repository. Project names and
namespaces keep the `CommercialNews.*` / `{Module}.*` casing.

---

## 3) Building blocks (shared foundations)

### 3.1 `src/building-blocks/CommercialNews.BuildingBlocks.Core`
Purpose: stable primitives used across modules without importing business logic.

Typical contents:
- Result types: `Result<T>`, `Error`, `ProblemDetails` helpers
- Strong IDs: `ArticleId`, `UserId` (or a generic ID strategy)
- Time abstraction: `IClock`
- Pagination/filter primitives: `PageRequest`, `SortSpec` (generic)
- Domain event envelope primitives (if shared): `EventEnvelope`

Rules:
- Must remain **small** and **stable**.
- No module-specific rules (no `Content` logic here).

### 3.2 `src/building-blocks/CommercialNews.BuildingBlocks.Persistence.Sql`
Purpose: reusable SQL Server foundations.

Typical contents:
- SQL connection factory
- SQL options
- transaction/unit-of-work base abstractions
- persistence exception and SQL exception translation base

Rules:
- May depend on SQL client packages.
- Must not contain module-specific repositories.

### 3.3 `src/building-blocks/CommercialNews.BuildingBlocks.Outbox`
Purpose: reliable integration-event publication and worker processing support.

Typical contents:
- Outbox message model and integration envelope
- Outbox ports, use cases, validation, runtime processors
- Outbox SQL repository/writer/unit of work
- Outbox exception translator

Rules:
- Uses `CommercialNews.BuildingBlocks.Core`.
- Uses `CommercialNews.BuildingBlocks.Persistence.Sql`.
- Must not import module domain models.
- Events should use minimal payloads and be versionable.

### 3.4 `src/building-blocks/CommercialNews.BuildingBlocks.Storage`
Purpose: storage abstraction and storage request/result contracts.

Typical contents:
- `IFileStorageService`
- upload/delete request and result models
- provider and purpose constants

### 3.5 `src/building-blocks/CommercialNews.BuildingBlocks.Storage.Infrastructure`
Purpose: concrete file storage providers and DI wiring.

Typical contents:
- Local file storage provider
- Google Cloud Storage provider
- storage options and registration extensions

Rules:
- May depend on provider SDKs.
- Application modules should reference `Storage`, not `Storage.Infrastructure`.

---

## 4) Modules (domain partitioning)

Each module has a consistent internal structure.

Example:
src/modules/Content/
├─ Content.Domain/
├─ Content.Application/
└─ Content.Infrastructure/


### 4.1 `*.Domain`
Purpose: business rules and invariants.

Contains:
- Aggregates/entities/value objects
- Domain services (pure domain)
- Domain rules for lifecycle transitions
- Domain events (optional; can be in Application depending on style)

Must NOT depend on:
- Infrastructure, persistence, web framework, messaging libraries

### 4.2 `*.Application`
Purpose: use cases and orchestration.

Contains:
- Application services / command handlers / query handlers
- Interfaces (ports): repositories, external services, event publisher
- Validation (request-level)
- Transaction boundaries (policy-level)
- Emits events for side effects

Depends on:
- Domain
- BuildingBlocks (SharedKernel, Messaging abstractions)

Must NOT depend on:
- Infrastructure implementations
- ASP.NET Core / hosting concerns

### 4.3 `*.Infrastructure`
Purpose: adapters to external systems.

Contains:
- Repository implementations (SQL/Mongo)
- Message bus implementation details (if module-owned)
- External service clients (email provider, storage, etc.)
- Data mappings (row/document ↔ domain/entity)

Depends on:
- Application (interfaces)
- external libraries

### 4.4 `*.Contracts` (optional but recommended)
Purpose: explicit boundary contracts.

Use when:
- you expose module APIs/events that other modules consume
- you want versioning discipline and fewer accidental dependencies

Contains:
- API DTOs (request/response)
- Event schemas (payload types)
- Contract versioning notes

Rule:
- Keep contracts minimal; avoid leaking domain internals.

---

## 5) Hosts (deployable components)

### 5.1 `src/hosts/CommercialNews.Api`
Purpose: HTTP entry point.

Contains:
- Controllers/endpoints
- Middleware (authn/authz, correlation, exception handling)
- Dependency Injection wiring
- API docs (OpenAPI)
- Minimal mapping to Application DTOs

Rules:
- No domain logic in controllers.
- Controllers call Application services/handlers only.
- Use policies for authorization; do not scatter permission checks.

### 5.2 `src/hosts/CommercialNews.Worker`
Purpose: background processing.

Contains:
- Event consumers (audit ingestion, notifications, view aggregation)
- Schedulers/Cron-like jobs (if any)
- Retry and DLQ wiring
- Worker-specific observability (backlog/lag, consumer errors)

Rules:
- Worker uses module Application services/handlers.
- Consumers must be idempotent and retry-safe.

---

## 6) Cross-cutting placement guide (where code should live)

### Authentication/Authorization
- Auth endpoints and policy enforcement: in API Host
- Authorization rules & permissions model: Authorization module
- Domain modules must not embed auth logic; they receive `ActorUserId` context when needed.

### Audit
- Event emission: from Application layer of modules (domain events)
- Audit persistence: Audit module (Worker consumer)

### Notifications (email)
- Triggers: emitted as events
- Sending/retries: Notifications module (Worker consumer)

### View tracking / aggregation
- Ingestion should be non-blocking for read path
- Aggregation and counters: Interaction module (Worker)

---

## 7) Testing Structure (Recommended)

tests/
├─ Unit/
│  ├─ Content.Domain.Tests/
│  ├─ Identity.Application.Tests/
│  └─ ...
│
└─ Integration/
   ├─ Api.IntegrationTests/
   └─ Worker.IntegrationTests/

---

## 8) Scaling strategy (how this structure evolves)

### V1 (recommended)
- Keep two deployable components: API + Worker.
- Grow modules inside `src/modules/`.
- Keep contracts explicit and avoid shared domain packages.

### V2+
- Introduce `Reading.ReadModel` module and projections if read bursts demand it.
- Split a module into its own deployable component only when signals justify it
  (performance, independent scaling, blast radius).

---

## 9) Practical conventions (recommended)

- Naming:
  - `{Module}.Domain`, `{Module}.Application`, `{Module}.Infrastructure`
- IDs:
  - Prefer strong IDs or consistent primitives (Guid/string) across modules.
- Contracts:
  - Version events when breaking changes are unavoidable.
- Boundaries:
  - No direct DB access across module boundaries.
  - Avoid synchronous chains across multiple modules on the read path.

---
