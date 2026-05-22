# Reading — Observability & SLO Signals (V1)

Related:

* `../../../../architecture/arc42/04-runtime-view-v1.md`
* `../../../../architecture/arc42/11-replication-v1.md`
* `../../../../architecture/arc42/18-stream-processing-and-derived-state-v1.md`
* `../../../../architecture/arc42/19-stream-processing-runtime-v1.md`
* `../../../../decisions/adr-0015-cache-policy-and-invalidation-redis-v1.md`
* `../../../../decisions/adr-0020-timeout-retry-and-failure-detection-policy-v1.md`
* `../../../../decisions/adr-0021-clock-time-and-ordering-policy-v1.md`
* `../../../../decisions/adr-0022-versioning-and-fencing-strategy-v1.md`
* `../../../../decisions/adr-0025-batch-processing-and-derived-state-policy-v1.md`
* `../../../../decisions/adr-0026-batch-job-orchestration-and-materialization-policy-v1.md`
* `../../../../decisions/adr-0027-stream-processing-and-derived-state-policy-v1.md`
* `../../../../decisions/adr-0028-consumer-idempotency-replay-and-rebuild-policy-v1.md`

---

## Purpose

This document defines Reading-specific observability and SLO signals for:

* public read APIs
* source-derived public visibility safety
* Reading projection freshness
* async projection consumers
* cache/degraded response behavior
* rebuild/reconciliation workflows

Reading is a derived public read projection module.

Normal public path:

```text
Public API
    ↓
Reading projection
    ↓
Response
```

Policy-controlled source fallback may exist, but it must be explicit and should not become hidden cross-module ownership.

---

## 1) SLO focus

### Public endpoints

Primary public endpoints:

```text
GET /api/v1/articles
GET /api/v1/articles/{articlePublicId}
GET /api/v1/articles/slug/{slug}
GET /api/v1/articles/search
GET /api/v1/articles/{articlePublicId}/related
```

### Core SLIs

Track list, detail, search, and related endpoints separately.

Minimum SLIs:

* request count
* P50/P95/P99 latency
* 2xx rate
* 4xx rate
* 5xx rate
* timeout rate
* rate-limit count
* response size distribution where useful
* degraded success count

### Interpretation rule

Reading observability must distinguish:

* public API latency degradation
* public API availability failure
* optional enrichment lag
* projection lag
* cache degradation
* visibility safety risk
* rebuild/reconciliation failure

A degraded response is not automatically an incident if public visibility is safe.

---

## 2) Public visibility safety signals

Reading must treat public visibility safety as the highest-priority correctness signal.

### Must-be-zero incidents

These must be treated as critical:

* draft article exposed publicly
* unpublished article exposed publicly
* archived article exposed publicly
* soft-deleted article exposed publicly
* visibility-uncertain article exposed publicly
* stale cache or stale projection used as public authority incorrectly
* partial rebuild output exposed as complete active public data

### Recommended signals

* `reading_visibility_leak_incident_total`
* `reading_visibility_uncertain_total`
* `reading_visibility_fail_closed_total`
* `reading_public_not_found_total`
* `reading_visibility_denied_after_slug_match_total`
* `reading_projection_public_to_hidden_total`
* `reading_projection_hidden_to_public_total`

### Public vs internal meaning

Public clients should usually see safe `404`.

Internal metrics should distinguish:

* not found
* not public
* archived
* soft-deleted
* slug inactive
* visibility uncertain
* projection missing
* projection stale by policy

---

## 3) Projection freshness signals

Reading projection freshness must be observable because Reading follows source truth asynchronously.

### Minimum signals

* projection row count
* projected public article count
* projection missing count where detectable
* projection freshness age based on `LastSyncedAtUtc`
* oldest projected row sync age
* newest projected row sync age
* projection lag from source event `OccurredAtUtc` to `LastSyncedAtUtc`
* source version gap count
* stale source version reject count

### Suggested metrics

* `reading_projection_rows_total`
* `reading_projection_public_rows_total`
* `reading_projection_freshness_age_seconds`
* `reading_projection_lag_seconds`
* `reading_projection_missing_detected_total`
* `reading_projection_stale_version_reject_total`
* `reading_projection_version_gap_total`

### Interpretation rule

Projection lag after publish may cause:

* article missing from list
* safe `404` for a newly published article
* search not showing the article yet

This is acceptable within SLO.

Projection lag must not cause known or suspected non-public content to be exposed.

---

## 4) Async projection consumer signals

Reading async consumers must be observable independently from public API health.

### Consumer health signals

* consumer running state
* consumer subscription/connection health
* queue depth
* unacked message count
* oldest message age
* consume rate
* apply success count
* apply failure count
* retry count
* DLQ/dead-letter count where enabled
* handler duration
* repository call duration

### Idempotency/version signals

* duplicate message count
* stale version reject count
* version gap detected count
* ignored older event count
* replay apply count
* no-op apply count
* successful upsert/update count

### Suggested metrics

* `reading_consumer_messages_received_total`
* `reading_consumer_apply_success_total`
* `reading_consumer_apply_failed_total`
* `reading_consumer_duplicate_message_total`
* `reading_consumer_stale_version_reject_total`
* `reading_consumer_version_gap_total`
* `reading_consumer_handler_duration_seconds`
* `reading_consumer_repository_duration_seconds`
* `reading_consumer_queue_oldest_message_age_seconds`

### Required log fields

Important projection apply logs should include:

* `MessageId`
* `EventType`
* `AggregateId`
* `IncomingVersion`
* `CurrentSourceVersion`
* `CorrelationId`
* apply decision
* reject/failure reason
* retry attempt
* `LastSyncedAtUtc`

### Interpretation rule

Outbox publish failure and Reading consumer failure are different failure domains.

Operators must be able to tell whether:

* event was not published yet
* event is stuck in broker/queue
* Reading consumer received but failed
* Reading rejected as duplicate/stale
* Reading applied successfully but public API still uses stale cache

---

## 5) Outbox and producer-side signals relevant to Reading

Reading does not own producer-side outbox state, but Reading operations depend on upstream event flow.

Relevant system-level signals:

* outbox pending count by producer module
* outbox oldest pending age by producer module
* outbox failed/dead count
* publish attempt count
* publish success/failure rate
* RabbitMQ queue depth for Reading consumers
* RabbitMQ ready/unacked messages
* RabbitMQ oldest message age where available

Interpretation:

* growing Content outbox pending age may mean Reading projection will lag
* broker queue growth may mean Reading consumer lag
* Reading stale projection may be caused by producer-side or consumer-side delay
* these must be distinguished during investigation

---

## 6) Degraded success signals

Reading may return successful responses with degraded optional enrichments.

Optional enrichments:

* counters
* cover media
* media gallery
* SEO metadata
* related article signals
* popularity/trending scores
* summary enrichments

Track:

* degraded response count
* omitted counters count
* omitted media count
* omitted SEO metadata count
* omitted related signals count
* fallback sort count
* stale enrichment served count where policy allows
* enrichment source unavailable count

Suggested metrics:

* `reading_degraded_response_total`
* `reading_omitted_enrichment_total`
* `reading_omitted_counters_total`
* `reading_omitted_media_total`
* `reading_omitted_seo_metadata_total`
* `reading_omitted_related_total`
* `reading_stale_enrichment_served_total`

Interpretation:

* degraded success is acceptable if public visibility is safe
* sustained increase means upstream projection/enrichment health should be investigated
* optional enrichment failure should not be mixed with public visibility failure

---

## 7) Cache signals

If Reading cache is introduced, track:

* cache hit rate
* cache miss rate
* cache stale detected count
* cache refresh success/failure
* cache fallback count
* stale cache rejected count
* cache write skipped due to stale projection
* cache invalidation signal lag where available

Suggested metrics:

* `reading_cache_hit_total`
* `reading_cache_miss_total`
* `reading_cache_stale_detected_total`
* `reading_cache_refresh_failed_total`
* `reading_cache_stale_rejected_total`

Rules:

* cache hit must not bypass visibility safety
* stale cache must not override Reading projection visibility
* cache errors should not become hard errors if projection-backed response is safe

---

## 8) Search observability

Search is public and abuse-prone.

Track:

* search request count
* search latency
* search error rate
* empty query validation count
* invalid query count
* search backend/projection unavailable count
* search degraded/fallback count
* non-public candidate filtered count where measurable
* high-frequency search client count

Suggested metrics:

* `reading_search_requests_total`
* `reading_search_latency_seconds`
* `reading_search_validation_failed_total`
* `reading_search_backend_unavailable_total`
* `reading_search_degraded_total`

Interpretation:

* search failures should not imply article detail/list failure
* stale search must not expose non-public content
* high search traffic can indicate scraping or abuse

---

## 9) Related article observability

Track:

* related endpoint request count
* related latency
* parent not public/not found count
* related empty result count
* fallback-to-recent count
* related signal unavailable count
* non-public related candidate filtered count

Suggested metrics:

* `reading_related_requests_total`
* `reading_related_empty_total`
* `reading_related_fallback_total`
* `reading_related_signal_unavailable_total`

Interpretation:

* empty related result is acceptable
* related failure should not block article detail
* related output must never include non-public articles

---

## 10) Batch / rebuild / reconciliation signals

These apply to Reading-owned workflows such as:

* `ArticleReadModel` rebuild
* projection reconciliation
* counter/summary rebuild
* trending input generation
* derived enrichment repair

### Workflow health

Track:

* run started/completed/failed count
* run duration
* current stage
* last completed stage
* records selected
* records processed
* records skipped
* records repaired
* mismatch count
* candidate output generated count
* candidate validation failure count
* publication/cutover success/failure
* active output freshness age

Suggested metrics:

* `reading_rebuild_run_started_total`
* `reading_rebuild_run_completed_total`
* `reading_rebuild_run_failed_total`
* `reading_rebuild_duration_seconds`
* `reading_rebuild_records_processed_total`
* `reading_reconciliation_mismatch_total`
* `reading_candidate_validation_failed_total`
* `reading_cutover_failed_total`

### Replay / recovery indicators

Track:

* replay count
* rebuild triggered count
* repeated mismatch on same bounded scope
* candidate-built-but-not-published count
* stale input/candidate reject count
* rerun count

### Ownership-sensitive workflow signals

If exclusive rebuild or repair ownership is introduced, track:

* duplicate-run detection count
* stale-owner rejection count
* generation/fencing mismatch count
* safe no-owner/degraded interval count

### Interpretation rule

A failed rebuild is not automatically a public API incident if current projection remains safe.

A failed rebuild is an incident when:

* active projection cannot serve safe public reads
* visibility uncertainty grows beyond policy
* partial candidate output becomes active
* repeated drift cannot be repaired

---

## 11) Dependency health signals

Reading should observe dependencies by role.

### Required dependencies

Required for normal projection-backed public serving:

* Reading projection store
* Reading API host/runtime
* routing/read repository dependencies

Signals:

* dependency latency
* dependency error rate
* timeout rate
* connection failure rate
* saturation indicators

### Optional enrichment dependencies

Optional or derived:

* counters
* media projection
* SEO metadata projection
* related/trending summaries
* cache

Signals:

* omitted enrichment rate
* stale enrichment age
* degraded success count
* dependency unavailable count

### Explicit fallback dependencies

If fallback to source truth is enabled by policy, track separately:

* fallback attempted count
* fallback success/failure
* fallback latency
* fallback reason
* fallback target module

Interpretation:

* fallback growth is not automatically an incident
* sustained fallback growth means the projection path may be unhealthy
* fallback must not become hidden normal ownership

---

## 12) Release gates

During rollout, gate on:

* P99 latency deltas by endpoint
* 5xx/timeouts by endpoint
* safe `404` spike beyond expected rollout behavior
* projection apply failure spike
* stale version reject spike
* version gap spike
* Reading queue backlog growth
* outbox oldest pending age growth from source modules
* degraded response spike
* omitted enrichment spike
* rebuild/reconciliation failure spike
* candidate publication/cutover failure
* visibility uncertainty spike
* any visibility leak signal

### Strong stop conditions

Immediate pause/rollback is recommended if:

* draft/unpublished/archived/soft-deleted content is exposed
* partial rebuild output is exposed as active complete state
* stale cache/projection overrides public visibility safety
* public API cannot fail closed when projection visibility is uncertain
* version guard is not working and stale events overwrite newer projection state
* search or related endpoints expose non-public candidates

---

## 13) Degraded-but-acceptable behavior

Reading is degraded but acceptable when:

* article list/detail still returns safely
* counters are missing/stale
* media is missing or placeholdered
* SEO metadata is missing or defaulted
* related articles are empty or fallback-based
* search result is safely omitted/degraded
* cache is down but projection-backed response works
* rebuild is delayed but active projection is still safe

Reading is not acceptable when:

* non-public content is exposed
* visibility uncertainty is served as public
* projection store is unavailable and no safe response exists
* stale derived output is trusted over visibility safety
* partial rebuild/candidate output becomes active accidentally
* consumer lag is unbounded and projection freshness breaches policy

Operator rule:

```text
Correctness first.
Completeness second.
Freshness third.
```

---

## 14) Operator questions this module must answer

Reading observability should help answer:

* Is the public read path unhealthy, or are only optional enrichments lagging?
* Is projection lag caused by producer-side outbox delay, broker backlog, or Reading consumer failure?
* Did Reading reject an event because it was duplicate, stale, or invalid?
* Is a safe `404` caused by actual absence, projection lag, or visibility uncertainty?
* Are counters/summaries stale because Interaction lagged or because Reading projection failed?
* Did cache serve safely, or was stale cache rejected?
* Is a degraded response still correctness-safe?
* Is rebuild/reconciliation needed?
* Did rebuild safely publish/cutover, or is candidate output still inactive?
* Is fallback being used as an exception, or has it become hidden normal ownership?

---

## 15) Non-goals

This document does not define:

* exact alert thresholds
* dashboard layout
* Prometheus metric implementation details
* Grafana panel configuration
* WAF/bot-detection rules
* RabbitMQ topology
* Outbox schema
* exact Worker retry/DLQ implementation

Those belong to platform, infrastructure, Worker runtime, and deployment docs.
