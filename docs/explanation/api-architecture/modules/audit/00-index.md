# Audit Module — API Architecture (V1)

## 1. Purpose

The Audit module provides an append-only, SQL-backed audit trail for important actions across CommercialNews.

Audit is investigation-ready and governance-focused. It records evidence about what happened, who caused it, which resource was affected, which source module emitted the event, and how the event should be interpreted from security, governance, and operational perspectives.

Audit preserves evidence truth separately from business truth owned by source modules.

Examples:

* Identity owns account truth.
* Authorization owns role and permission assignment truth.
* Content owns article lifecycle truth.
* Media owns media attachment truth.
* Interaction owns comment/report truth.
* SEO owns routing truth.
* Notifications owns delivery-state truth.
* Audit owns evidence that important events were recorded.

Audit V1 intentionally keeps the runtime scope simple:

* asynchronous ingestion
* `AuditLog`
* `AuditIngestion`
* `MessageId` deduplication
* event normalizer abstractions and registry
* investigation APIs
* lightweight dashboard queries from SQL

Future capabilities such as alerting, replay, remediation, reconciliation, archival, digest generation, materialized summaries, Redis acceleration, and partitioning are extension paths, not V1 core requirements.

---

## 2. Why This Module Is Critical

Audit is critical because governance and security failures require reliable traceability.

Audit must support:

* investigation after incidents
* governance review
* security-sensitive action tracking
* moderation and administrative accountability
* operational diagnosis of async pipelines
* future compliance/reporting workflows

Audit must also remain safe under distributed runtime conditions:

* at-least-once delivery
* duplicate messages
* worker restarts
* broker redelivery
* timeout ambiguity
* delayed ingestion
* retry and replay
* out-of-order event arrival

Audit records must be privacy-aware. Audit must not store or return raw secrets, tokens, password hashes, session cookies, or unsafe sensitive payloads.

---

## 3. Primary Consumers

Primary consumers of Audit are:

* `CommercialNews.Worker` audit ingestion consumer
* Admin UI
* operators
* security/governance reviewers
* incident response workflows
* future reporting/compliance workflows
* future replay/reconciliation/remediation workflows

Audit ingestion itself is not exposed publicly.

Audit APIs are admin-only.

Audit.Application does not read queues or brokers directly. Worker runtime consumes
Outbox/Broker messages, maps them into `IngestAuditEventCommand`, and invokes the
Application use case through MediatR.

---

## 4. V1 Scope

Audit V1 includes:

* async consumption of audit-relevant messages
* mapping Outbox messages into normalized audit evidence
* durable `MessageId` deduplication
* append-only `AuditLog`
* consumer-side `AuditIngestion`
* investigation query APIs
* module/resource/actor/correlation timelines
* failed ingestion visibility
* lightweight dashboard summary queries

V1 does not include:

* full audit rule engine
* persistent `AuditAlert`
* notification delivery
* batch digest generation
* materialized summary tables
* archive and retention jobs
* replay execution APIs
* reconciliation control APIs
* cache control APIs
* physical table partitioning
* full DB sharding
* tamper-evident hash chain enforcement
* public/user-facing audit history
* manual audit evidence insertion/update/delete APIs

---

## 5. Non-Goals in V1

Audit V1 does not aim to provide:

* real-time SIEM integration
* full compliance reporting platform
* configurable redaction engine
* global total ordering of all system events
* exactly-once broker delivery
* synchronous audit writes inside producer transactions
* cross-module transactions between source modules and Audit
* dashboards, summaries, or reports as canonical evidence truth
* Redis-backed audit evidence
* Redis-only dedupe for critical audit evidence
* replay/backfill as permission to rewrite historical evidence

---

## 6. Hard Constraints

Audit V1 follows these hard constraints:

1. Audit is downstream of source truth.
2. Source modules must not call Audit synchronously.
3. Source modules must not write directly into Audit tables.
4. Audit must not write back to source module truth tables.
5. Audit ingestion is asynchronous and at-least-once.
6. `MessageId` is the canonical async message identity.
7. One `MessageId` must produce at most one canonical `AuditLog`.
8. Audit records are append-only by default.
9. Audit must tolerate duplicate delivery, replay, timeout ambiguity, and worker restart.
10. Audit must redact before persistence.
11. Audit must not store or return secrets/tokens unsafely.
12. AuditLog is Audit evidence truth.
13. AuditIngestion is consumer-side processing state.
14. Dashboards, reports, alerts, digests, and summaries are derived.
15. Derived outputs may lag and be rebuilt.
16. Missing audit evidence temporarily does not undo source business truth.
17. Missing audit evidence is still operationally and governance-significant if not recovered.
18. Safe non-progress is preferable to unsafe duplicate evidence or history mutation.

---

## 7. Truth vs Derived Posture

## 7.1 Audit truth

Audit truth includes:

* `AuditLog`
* `AuditIngestion`

`AuditLog` is the append-only evidence store.

`AuditIngestion` tracks consumer-side processing and helps distinguish:

* message not seen yet
* message processing succeeded
* message was duplicate-safe
* message failed
* message was ignored
* message was dead-lettered

## 7.2 Source business truth

Audit does not own source business truth.

Audit must not be used alone to determine:

* whether a user is currently active
* whether a role assignment still exists
* whether an article is currently published
* whether a comment is currently visible
* whether a media asset is currently attached
* whether a notification was delivered successfully

Those questions belong to the owning source modules.

## 7.3 Derived audit outputs

Derived outputs may include:

* dashboard summaries
* recent-risk panels
* module statistics
* actor summaries
* resource timeline materializations
* alerts
* digests
* reports
* archive indexes
* reconciliation results
* replay candidate sets
* cached dashboard panels

Rule:

Derived audit outputs may lag and be rebuilt. They must not replace `AuditLog` evidence truth.

---

## 8. Stream / Async Posture

Audit is a downstream consumer in the standard V1 async model:

```text
Source module commits truth
    ↓
Source module writes OutboxMessage in the same local transaction
    ↓
Outbox publisher sends message to RabbitMQ
    ↓
CommercialNews.Worker audit consumer receives message at least once
    ↓
Worker sends IngestAuditEventCommand through MediatR
    ↓
Audit.Application selects a registered normalizer and normalizes event
    ↓
Audit deduplicates by MessageId
    ↓
Audit inserts AuditLog and updates AuditIngestion
```

Audit must distinguish:

* duplicate delivery of the same `MessageId`
* different messages that represent distinct emitted events
* stale delivery that still represents valid historical evidence
* lagging dashboard/report output versus canonical evidence truth
* producer-side publication failure versus consumer-side Audit processing failure

Audit does not depend on global total order.

Stable event identity, append-only persistence, and traceability matter more than arrival order.

---

## 9. Idempotency Posture

Audit V1 uses:

```text
MessageId
```

as the canonical idempotency key.

`MessageId` comes from:

```text
outbox.OutboxMessage.MessageId
```

Rules:

* `AuditLog.MessageId` must be unique.
* `AuditIngestion.MessageId` should be unique.
* duplicate delivery must not create duplicate evidence.
* replay of the same `MessageId` must be safe.
* timeout during Audit persistence is ambiguous.
* retry must rely on durable SQL dedupe.

Audit V1 does not collapse different `MessageId` values by default.

Future business-level dedupe may be introduced for derived outputs such as alerts, digests, reports, or noisy dashboard views.

---

## 10. Batch / Replay / Reconciliation Posture

Replay, remediation, reconciliation, archival, and materialized reporting are valid future extension paths.

They are not required for V1 core delivery.

Future bounded workflows may support:

* replay of failed or missed ingestion
* DLQ remediation
* backfill of missing evidence from durable sources
* reconciliation between expected events and stored audit evidence
* reporting/compliance output generation
* archive and retention workflows
* summary/digest generation

These workflows must:

* start from bounded inputs
* preserve original `MessageId`
* preserve append-only evidence semantics
* avoid duplicate canonical facts
* remain rerun-safe
* remain observable
* treat derived outputs as derived
* publish important outputs only after validation where correctness matters

Replay/backfill is recovery for evidence completeness. It is not permission to rewrite history.

---

## 11. Cache Posture

Audit V1 does not require Redis caching.

SQL remains the source of audit evidence truth.

Future Redis usage may be introduced only as optional acceleration for:

* dashboard summaries
* recent high-risk event panels
* module/action metadata
* TTL-bound alert dedupe hints

Redis must not be used as:

* AuditLog truth
* AuditIngestion truth
* the only dedupe mechanism for critical audit evidence
* replacement for SQL investigation detail queries
* source module business truth

---

## 12. Partitioning Posture

Audit V1 is partition-ready but not physically partitioned by default.

V1 relies on:

* SQL indexes
* bounded time-range queries
* source module metadata
* risk metadata
* consumer lag observability
* query latency observability

Future partitioning may include:

* time-range partitioning for `AuditLog`
* worker lane partitioning by source module
* worker lane partitioning by risk priority
* queue partitioning by module/category/priority
* batch-window partitioning for summary, digest, archive, or reconciliation workflows
* logical lane routing through indirection rather than direct `hash(key) mod N`

Partitioning must be signal-driven.

Signals include:

* audit query P95/P99 degradation
* growing AuditLog table/index pressure
* audit consumer lag
* oldest uningested message age
* retry/DLQ pressure
* security audit delayed by high-volume non-security events
* archive/report windows becoming too slow

---

## 13. Security and Privacy Posture

Audit is evidence-sensitive.

Audit must never store or return:

* passwords
* password hashes
* access tokens
* refresh tokens
* verification tokens
* reset tokens
* API keys
* session cookies
* raw authorization headers
* private signing keys
* raw provider secrets
* raw connection strings
* unsafe sensitive PII

Audit should prefer stable identifiers over personal data.

Preferred fields:

* `PublicId`
* `MessageId`
* `CorrelationId`
* `ResourceId`
* `ActorUserId`

Use cautiously:

* `ActorEmail`
* `ActorDisplayName`
* `IpAddress`
* `UserAgent`

Redaction must happen before persistence.

Audit APIs are admin-only and must use explicit authorization policies.

---

## 14. Primary Correctness Posture

Business truth remains with upstream modules.

Audit records evidence after that truth already committed.

Rules:

* upstream success does not imply audit evidence is immediately queryable
* audit lag does not roll back source truth
* missing dashboard/report output does not prove canonical audit evidence is missing
* missing audit evidence temporarily does not undo upstream business truth
* timeout or missing acknowledgement does not prove Audit insert did not happen
* if Audit completeness is uncertain, prefer bounded replay/reconciliation over unsafe synthetic evidence mutation

Audit must preserve investigation trust over convenience.

---

## 15. Consistency and Ordering Posture

Strong local consistency is required for:

* append-only AuditLog persistence
* durable MessageId dedupe
* AuditIngestion status update
* local evidence-store integrity
* safe protection against duplicate evidence

Eventual consistency is accepted for:

* evidence arrival relative to upstream business success
* dashboards
* reporting views
* replay/backfill convergence
* archival/summarization outputs
* future alert/digest generation

Audit does not require one global total order across all module events.

Timestamps support investigations, but `MessageId`, `CorrelationId`, `AggregateId`, `AggregateVersion`, and source module truth remain more important for reconstruction.

---

## 16. Failure and Recovery Posture

Audit must tolerate:

* queue lag
* worker restart
* broker redelivery
* consumer crash before ACK
* timeout during DB insert
* duplicate delivery
* out-of-order delivery
* unsupported event type
* schema drift
* redaction failure
* DLQ/poison messages

V1 recovery relies on:

* bounded retries
* `MessageId` dedupe
* AuditIngestion diagnostics
* DLQ/terminal failure visibility
* operator investigation

Future recovery may add:

* replay/remediation APIs
* completeness reconciliation
* archive/rebuild workflows
* derived report regeneration

Safe non-progress is preferable to unsafe duplicate evidence or silent history mutation.

---

## 17. Primary Runtime Components

Recommended V1 runtime components:

* `CommercialNews.Worker` audit message consumer
* `IngestAuditEventCommand`
* MediatR command handler and pipeline behaviors
* `AuditIngestionApplicationService`
* `IAuditEventNormalizerRegistry`
* `IAuditEventNormalizer`
* `IAuditLogRepository`
* `IAuditIngestionRepository`

Runtime placement:

* Worker consumes RabbitMQ/Outbox messages and builds `IngestAuditEventCommand`.
* Audit.Application owns the use case, validation, transaction behavior, normalizer abstraction, and registry.
* Audit.Infrastructure owns concrete normalizers and persistence implementations.
* Concrete normalizers are registered through Infrastructure DI.

Recommended future extension components:

* `IAuditRealtimeRule`
* `IAuditWindowRule`
* `AuditAlertEvaluator`
* `AuditAlertPublisher`
* `AuditReplayService`
* `AuditReconciliationService`
* `AuditDigestJob`
* `AuditArchiveJob`
* `AuditDashboardCacheService`

Future extension components must not change the core ingestion contract.

---

## 18. API Surface Summary

Audit V1 may expose Admin read APIs under:

```http
/api/v1/admin/audit
```

V1 API groups:

* audit log search
* audit log detail by `PublicId`
* lookup by `MessageId`
* lookup by `CorrelationId`
* module-specific logs
* resource timeline
* actor timeline
* ingestion status
* failed ingestion status
* dashboard summary
* recent high-risk events

V1 does not expose:

* public ingestion APIs
* manual evidence write APIs
* replay execution APIs
* archive execution APIs
* alert management APIs
* digest APIs
* cache control APIs
* partition management APIs

---

## 19. Key Links

System-wide rules:

* `../../01-api-architecture-charter-v1.md`
* `../../02-contracts-and-standards.md`
* `../../07-security-threat-modeling.md`
* `../../09-observability-and-slos.md`

Arc42:

* `../../../architecture/arc42/03-building-blocks-modularity.md`
* `../../../architecture/arc42/04-runtime-view-v1.md`
* `../../../architecture/arc42/05-quality-requirements.md`
* `../../../architecture/arc42/06-measurement-guide.md`
* `../../../architecture/arc42/10-system-data.md`
* `../../../architecture/arc42/11-replication-v1.md`
* `../../../architecture/arc42/13-transactions-and-consistency-v1.md`
* `../../../architecture/arc42/16-batch-processing-and-derived-data-v1.md`
* `../../../architecture/arc42/17-dataflow-and-batch-workflows-v1.md`
* `../../../architecture/arc42/18-stream-processing-and-derived-state-v1.md`
* `../../../architecture/arc42/19-stream-processing-runtime-v1.md`

System data model:

* `../../../architecture/arc42/system-data/system-data-audit-v1.md`

Decisions:

* `../../../decisions/adr-0011-replication-topology-v1.md`
* `../../../decisions/adr-0012-data-store-placement-v1.md`
* `../../../decisions/adr-0013-outbox-and-delivery-semantics-v1.md`
* `../../../decisions/adr-0014-public-identifier-strategy-v1.md`
* `../../../decisions/adr-0015-cache-policy-and-invalidation-v1.md`
* `../../../decisions/adr-0017-partitioning-strategy-v1.md`
* `../../../decisions/adr-0018-transaction-boundaries-and-consistency-model-v1.md`
* `../../../decisions/adr-0020-timeout-retry-and-failure-detection-policy-v1.md`
* `../../../decisions/adr-0025-batch-processing-and-derived-state-policy-v1.md`
* `../../../decisions/adr-0026-batch-job-orchestration-and-materialization-policy-v1.md`
* `../../../decisions/adr-0027-stream-processing-and-derived-state-policy-v1.md`
* `../../../decisions/adr-0028-consumer-idempotency-replay-and-rebuild-policy-v1.md`

---

## 20. ADR Hooks

Future ADR hooks:

* Audit payload shape and redaction policy per event type
* Audit retention, archive, and purge policy
* Audit tamper-evident evidence strategy
* Audit replay and remediation control policy
* Audit completeness reconciliation policy
* Audit alerting and admin notification policy
* Audit digest and summary materialization policy
* Audit storage and indexing evolution
* Audit Redis dashboard cache policy
* Audit partitioning and worker lane strategy
* Audit correction and redaction remediation policy
* Audit export policy
* Audit-of-audit policy

---

## 21. Summary

Audit V1 should be small, trustworthy, observable, and extension-ready.

The core V1 responsibility is:

```text
consume audit-relevant messages asynchronously
    ↓
dedupe by MessageId
    ↓
redact safely
    ↓
persist append-only AuditLog
    ↓
track AuditIngestion
    ↓
support admin investigation
```

Everything else is future extension unless the business need becomes clear.

The guiding rule:

```text
AuditLog is append-only evidence truth.
AuditIngestion is consumer-side processing state.
Dashboards, alerts, reports, digests, caches, and summaries are derived.
```
