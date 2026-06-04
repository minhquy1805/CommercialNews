# 01 - Onboarding

This tutorial gets a developer from a fresh checkout to a working CommercialNews
backend in local development.

CommercialNews V1 is a modular backend with:

- `CommercialNews.Api`: HTTP API host for public and admin endpoints.
- `CommercialNews.Worker`: background host for outbox publishing, RabbitMQ consumers, email delivery, projections, audit ingestion, and batch-style work.
- SQL Server: one database with module-owned schemas.
- RabbitMQ: broker for at-least-once event delivery.
- Docker Compose: local dev runtime for API, Worker, SQL Server, RabbitMQ, and Nginx.

## 1) Repository Map

Important directories:

- `src/hosts/CommercialNews.Api`: API host, controllers, auth policies, HTTP contracts.
- `src/hosts/CommercialNews.Worker`: worker host, outbox publisher, RabbitMQ consumers, integration handlers.
- `src/modules`: module code split into Domain, Application, and Infrastructure projects.
- `src/building-blocks`: shared kernel, persistence abstractions, outbox primitives, common infrastructure.
- `db/00_bootstrap`: database, schema, login, role, and grant scripts.
- `db/10_modules`: module table, index, and procedure scripts.
- `docker`: local and production Docker assets.
- `docs`: architecture, domain, tutorial, and reference documentation.

## 2) Module Map

Current V1 modules:

- Content: article lifecycle, categories, tags, editorial truth.
- SEO: slug routes, canonical and SEO metadata.
- Media: media assets and article-media attachments.
- Reading: public read projections and public serving state.
- Interaction: views, likes, comments, moderation signals, public counters.
- Identity: users, credentials, sessions, verification, reset flows.
- Authorization: roles, permissions, user-role assignments.
- Audit: canonical audit evidence and ingestion tracking.
- Notifications: email delivery and delivery state.
- Outbox: reliable event publication infrastructure.

Each module owns its schema and rules. Other modules should communicate through
application contracts, stable identifiers, and events rather than sharing domain
entities or querying another module's tables.

## 3) Prerequisites

Install:

- .NET SDK 8
- Docker and Docker Compose
- SQL Server client tools if you want to run scripts outside containers

Optional but useful:

- RabbitMQ management UI knowledge
- `rg` for fast search

## 4) Environment Files

For local Docker development use:

```bash
docker/.env.dev
```

The tracked template is:

```bash
docker/.env.example
```

Do not commit real secrets. Do not use `.env.prod` for local development.

## 5) Start Local Services

From the repository root:

```bash
docker compose --env-file docker/.env.dev -f docker/docker-compose.dev.yaml up --build -d
```

Main endpoints:

- API direct: `http://localhost:8080`
- Nginx proxy: `http://localhost:8088`
- RabbitMQ management: `http://localhost:15672`
- SQL Server: `localhost,1433`

The compose file starts SQL Server and RabbitMQ first and waits for their
healthchecks before starting API and Worker.

## 6) Bootstrap The Database

The database scripts are intentionally split:

1. Bootstrap database and schemas:
   - `db/00_bootstrap/001_create_database.sql`
   - `db/00_bootstrap/010_create_schemas.sql`
   - `db/00_bootstrap/020_create_logins_users.local.sql`
   - `db/00_bootstrap/030_create_roles.sql`
   - `db/00_bootstrap/040_grants_baseline.sql`

2. Run module scripts in dependency order:
   - `outbox`
   - `identity`
   - `authorization`
   - `notifications`
   - `audit`
   - `content`
   - `media`
   - `seo`
   - `interaction`
   - `reading`

For each module, run:

- `001_tables.sql`
- `010_indexes.sql`
- `020_procs.sql`

Example using the SQL Server container:

```bash
docker exec -i cn-mssql /opt/mssql-tools18/bin/sqlcmd \
  -S localhost -U sa -P "$SA_PASSWORD" -C \
  -i /path/in/container/to/script.sql
```

If scripts are mounted or copied differently in your setup, keep the same script
order and run path. The order matters because modules may depend on shared
schemas, outbox infrastructure, or cross-module references.

## 7) Build Locally

Build the API:

```bash
dotnet build src/hosts/CommercialNews.Api/CommercialNews.Api.csproj
```

Build the Worker:

```bash
dotnet build src/hosts/CommercialNews.Worker/CommercialNews.Worker.csproj
```

Build the solution when validating broad changes:

```bash
dotnet build CommercialNews.sln
```

## 8) Runtime Flow To Understand First

Most write flows follow this shape:

1. API receives a request.
2. API maps HTTP contract to an Application use case.
3. Application validates and executes the use case.
4. Infrastructure persists the truth change through SQL stored procedures.
5. If side effects are needed, the owner module writes an outbox message in the same local transaction.
6. Worker publishes outbox messages to RabbitMQ.
7. Worker consumers process messages idempotently:
   - Audit writes audit evidence and ingestion state.
   - Notifications creates and sends delivery work.
   - Reading updates public projections.
   - SEO, Interaction, and other consumers update their own derived state.

Business success should not depend on async side effects completing immediately.

## 9) Quick Smoke Checks

After database bootstrap and service startup:

1. Open API health:

```bash
curl http://localhost:8080/health/live
```

2. Check RabbitMQ management at:

```text
http://localhost:15672
```

3. Run an admin action that emits an outbox event, such as publishing an article.

4. Check Worker logs:

```bash
docker logs commercialnews-worker --tail 200
```

5. Check Audit admin endpoints after an audited action:

```text
GET /api/v1/admin/audit/logs
GET /api/v1/admin/audit/ingestions
GET /api/v1/admin/audit/dashboard/summary
```

Use an authenticated admin token with the required authorization policies.

## 10) Common Local Issues

SQL login failures:

- Ensure bootstrap login/user scripts ran.
- Ensure `docker/.env.dev` connection strings match created users.

Worker starts before RabbitMQ/SQL is ready:

- Current compose uses healthchecks. Recreate containers if you still see stale behavior:

```bash
docker compose --env-file docker/.env.dev -f docker/docker-compose.dev.yaml up --build -d --force-recreate
```

No audit rows after an action:

- Check outbox messages were written.
- Check Worker is running.
- Check RabbitMQ exchange and queue bindings.
- Check Audit consumer routing keys.
- Check unsupported event handling in Audit ingestion state.

Public reads stale after writes:

- Reading is projection-driven and may lag.
- Check Worker consumers and RabbitMQ queue depth.
- Verify the relevant source event is published.

## 11) Development Rules To Keep In Mind

- Domain owns invariants and policy interfaces.
- Application owns use cases, validation, pipeline behaviors, and ports.
- Infrastructure implements persistence, serializers, normalizers, redaction, and external integrations.
- API owns HTTP contracts, controller routing, auth policy attributes, and HTTP mapping.
- Worker owns broker consumers, outbox publishing, and runtime message handling.
- DB scripts own table/index/procedure shape for the module.

Keep module boundaries visible. A small local shortcut that crosses boundaries
usually becomes a hard-to-remove architecture dependency later.
