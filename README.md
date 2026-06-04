# CommercialNews

CommercialNews is a modular news platform backend. It is built as a V1 modular
monolith with clear module ownership, SQL Server persistence, RabbitMQ event
delivery, and Worker-based asynchronous processing.

The project is currently focused on backend architecture, admin/public APIs,
module-owned database schemas, reliable outbox delivery, public reading
projections, audit evidence, notifications, and operational workflows.

## Current Backend Shape

Runtime hosts:

- `CommercialNews.Api`: HTTP API host for public and admin endpoints.
- `CommercialNews.Worker`: background host for outbox publishing, RabbitMQ consumers, projections, audit ingestion, notification delivery, and batch-style work.

Infrastructure:

- SQL Server: one database with module-owned schemas.
- RabbitMQ: at-least-once event transport.
- Outbox: reliable event publication from truth transactions.
- Docker Compose: local development runtime for API, Worker, SQL Server, RabbitMQ, and Nginx.

## Modules

Current V1 modules:

- Content: article lifecycle, categories, tags, editorial truth.
- SEO: slug routes, canonical URLs, SEO metadata.
- Media: media assets and article-media attachments.
- Reading: public read projections and public serving state.
- Interaction: views, likes, comments, moderation signals, public counters.
- Identity: users, credentials, sessions, verification, password reset.
- Authorization: roles, permissions, user-role assignments.
- Audit: audit evidence, ingestion tracking, admin investigation.
- Notifications: email delivery and delivery state.
- Outbox: reliable integration event publication infrastructure.

Each module owns its data and rules. Cross-module integration should use stable
IDs, application contracts, and events rather than shared domain entities or
direct table access.

## Repository Layout

```text
src/
  building-blocks/
  hosts/
    CommercialNews.Api/
    CommercialNews.Worker/
  modules/
db/
  00_bootstrap/
  10_modules/
docker/
docs/
```

Important entry points:

- Solution: `CommercialNews.sln`
- API project: `src/hosts/CommercialNews.Api/CommercialNews.Api.csproj`
- Worker project: `src/hosts/CommercialNews.Worker/CommercialNews.Worker.csproj`
- Docker compose: `docker/docker-compose.dev.yaml`
- Docker env template: `docker/.env.example`
- Documentation index: `docs/README.md`

## Quick Start

Create or update local Docker environment values:

```bash
cp docker/.env.example docker/.env.dev
```

Edit `docker/.env.dev` with local development secrets and connection strings.
Do not commit real secrets.

Start the local runtime:

```bash
docker compose --env-file docker/.env.dev -f docker/docker-compose.dev.yaml up --build -d
```

Main local endpoints:

- API: `http://localhost:8080`
- Nginx proxy: `http://localhost:8088`
- RabbitMQ management: `http://localhost:15672`
- SQL Server: `localhost,1433`

For full setup, database bootstrap order, and smoke checks, read:

- `docs/tutorials/01-onboarding.md`

## Build

Build API:

```bash
dotnet build src/hosts/CommercialNews.Api/CommercialNews.Api.csproj
```

Build Worker:

```bash
dotnet build src/hosts/CommercialNews.Worker/CommercialNews.Worker.csproj
```

Build the solution:

```bash
dotnet build CommercialNews.sln
```

## Database Scripts

Database scripts live under `db/`.

Bootstrap scripts:

- `db/00_bootstrap/`

Module scripts:

- `db/10_modules/{module}/001_tables.sql`
- `db/10_modules/{module}/010_indexes.sql`
- `db/10_modules/{module}/020_procs.sql`

Run bootstrap scripts first, then module scripts in dependency order. See:

- `db/README.md`
- `docs/tutorials/01-onboarding.md`

## Documentation

Start with:

- `docs/README.md`
- `docs/tutorials/01-onboarding.md`
- `docs/explanation/architecture/arc42/00-index.md`
- `docs/reference/architect-operating-model.md`
- `docs/reference/architecture-quantum.md`

Documentation is organized as:

- `docs/tutorials/`: guided learning paths.
- `docs/explanation/`: architecture and domain reasoning.
- `docs/reference/`: operating models, templates, and factual lookup.

## Development Notes

- Domain owns invariants, value objects, exceptions, and pure policy vocabulary.
- Application owns use cases, validation, MediatR handlers, ports, and pipeline behaviors.
- Infrastructure owns SQL persistence, repositories, mappers, serializers, normalizers, redaction, and provider implementations.
- API owns HTTP routes, authorization policy attributes, contracts, and HTTP mapping.
- Worker owns outbox publishing, RabbitMQ consumers, envelope validation, and runtime message handling.
- DB scripts own module schema, indexes, and stored procedures.

Core integration rule:

```text
truth transaction -> OutboxMessage -> Worker -> RabbitMQ -> idempotent consumer
```

Business success should not depend on async side effects completing immediately.

## Secrets

Do not commit real secrets. Local secret-bearing files are intentionally ignored,
including Docker env files and local appsettings files. Use tracked templates
such as `docker/.env.example` for shape only.
