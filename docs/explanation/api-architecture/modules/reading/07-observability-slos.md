# Reading — Observability & SLO Signals (V1 Async Projections)

## Purpose

This document defines Reading-specific observability and SLO signals for:

```text
Public read APIs
Local public visibility and route safety
Async projection consumers
Source-specific projection freshness
Optional media and counter degradation
Cache behavior
Repair and rebuild workflows
```

Reading is a fully asynchronous public serving module.

Reading serves ordinary public requests from Reading-owned projection state:

```text
ArticleReadModel
ArticleSeoRouteProjection
```

Related:

```text
../../../architecture/arc42/04-runtime-view-v1.md
../../../architecture/arc42/11-replication-v1.md
../../../architecture/arc42/13-transactions-and-consistency-v1.md
../../../architecture/arc42/18-stream-processing-and-derived-state-v1.md
../../../architecture/arc42/19-stream-processing-runtime-v1.md

../../../decisions/adr-0013-outbox-and-delivery-semantics-v1.md
../../../decisions/adr-0015-cache-policy-and-invalidation-redis-v1.md
../../../decisions/adr-0018-transaction-boundaries-and-consistency-model-v1.md
../../../decisions/adr-0020-timeout-retry-and-failure-detection-policy-v1.md
../../../decisions/adr-0021-clock-time-and-ordering-policy-v1.md
../../../decisions/adr-0022-versioning-and-fencing-strategy-v1.md
../../../decisions/adr-0025-batch-processing-and-derived-state-policy-v1.md
../../../decisions/adr-0026-batch-job-orchestration-and-materialization-policy-v1.md
../../../decisions/adr-0027-stream-processing-and-derived-state-policy-v1.md
../../../decisions/adr-0028-consumer-idempotency-replay-and-rebuild-policy-v1.md
```

---

## 1. Observability Posture

Reading observability must distinguish five different concerns:

| Concern | Meaning |
|---|---|
| Public API health | Can public clients receive responses with acceptable latency and availability? |
| Local serving safety | Does Reading deny locally missing, non-public or unsafe content/routes correctly? |
| Projection propagation lag | How far behind each upstream source lane is Reading? |
| Optional enrichment degradation | Are media or counters delayed while core serving remains safe? |
| Repair/rebuild health | Can Reading-owned derived state be recovered safely? |

Core operating principle:

```text
Correctness first.
Completeness second.
Freshness third.
```

Rules:

```text
A degraded response is not automatically an incident
when core local visibility and route safety remain valid.

A public exposure incident is critical when Reading serves content
that its available local state already identifies as non-public or unsafe,
or when detected drift/lag exceeds accepted safety policy.

Normal async propagation lag must be measured separately
from confirmed incorrect local serving behavior.
```

---

## 2. Public Endpoint SLO Scope

Primary public endpoints:

```text
GET /api/v1/articles
GET /api/v1/articles/{articlePublicId}
GET /api/v1/articles/slug/{slug}
GET /api/v1/articles/search
GET /api/v1/articles/{articlePublicId}/related
```

Track each endpoint group separately:

| Endpoint group | Why separate measurement matters |
|---|---|
| List | High traffic, paging/filter performance |
| Detail by public id | Core detail-serving health |
| Detail by slug | Depends on two local projections: route + article |
| Search | Query-heavy and abuse-prone |
| Related | Optional experience path; must not hide detail-path health |

### Minimum Public API SLIs

Track:

```text
Request count
P50 / P95 / P99 latency
2xx rate
400 validation rate
404 safe-denial rate
429 rate-limit rate
500 rate
503 rate
Timeout rate
Response size distribution where useful
Degraded-success rate
```

Suggested metrics:

```text
reading_public_requests_total{endpoint,status}
reading_public_request_duration_seconds{endpoint}
reading_public_timeout_total{endpoint}
reading_public_rate_limited_total{endpoint}
reading_public_degraded_success_total{endpoint,degradation}
reading_public_safe_not_found_total{endpoint,internal_reason}
reading_public_service_unavailable_total{endpoint,reason}
```

---

## 3. Local Public Visibility Safety Signals

Content owns article public visibility truth.

Reading owns local serving enforcement based on its applied Content projection.

### Locally Required Public Condition

Reading may serve an article only when local state confirms:

```text
ArticleReadModel.Status = Published
AND ArticleReadModel.IsPublic = true
AND local visibility state is not unsafe / requires-resync
```

### Critical Local Safety Incidents

The following should be treated as severe correctness incidents:

```text
Reading serves an article although local projection already marks it non-public.

Reading serves an article although local projection marks visibility unsafe
or requires resync.

A stale Content snapshot overwrites a newer local deny/non-public state.

Cache serves public content after local Reading state denies or marks it unsafe.

A partial rebuild/candidate projection becomes active public serving state
without successful validation/cutover.
```

Suggested metrics:

```text
reading_visibility_local_denied_total{reason}
reading_visibility_unsafe_deny_total{reason}
reading_visibility_stale_reexposure_prevented_total
reading_visibility_local_safety_violation_total{reason}
reading_visibility_public_to_non_public_apply_total
reading_visibility_non_public_to_public_apply_total
```

### Async Lag Clarification

Because Reading is asynchronous:

```text
Content may commit unpublish/archive/soft-delete
before Reading receives and applies the newer non-public snapshot.
```

That propagation window is not automatically evidence of a Reading handler bug. It is a bounded eventual-consistency risk that must be monitored separately.

Measure:

```text
Content -> Reading propagation lag
Age of pending Content messages affecting public visibility
Time from source OccurredAtUtc to local non-public apply
Detected divergence during reconciliation
```

If lag exceeds policy or reconciliation detects exposure inconsistent with authoritative source truth, escalate as a visibility safety incident.

---

## 4. Local Slug Route Safety Signals

SEO owns canonical route truth.

Reading serves slug requests using local:

```text
ArticleSeoRouteProjection
```

Slug request requires:

```text
Scope = public
AND route exists
AND IsActive = true
AND RequiresResync = false
AND target ArticleReadModel passes local public visibility
```

### Safe-Deny Signals

Track slug requests denied because of:

```text
RouteProjectionMissing
RouteInactive
RouteRequiresResync
RouteTargetProjectionMissing
RouteTargetNotPublic
RouteTargetVisibilityUnsafe
```

Suggested metrics:

```text
reading_slug_requests_total{status}
reading_slug_route_missing_total
reading_slug_route_inactive_total
reading_slug_route_requires_resync_total
reading_slug_route_target_missing_total
reading_slug_route_target_not_public_total
reading_slug_route_target_unsafe_total
```

### Critical Local Route Incidents

Treat as critical:

```text
Reading serves through a route locally marked inactive.

Reading serves through a route locally marked RequiresResync.

A stale SEO projection snapshot reactivates a newer locally deactivated route.

Cache serves a slug response after local route state denies or marks it unsafe.
```

Suggested metrics:

```text
reading_route_local_safety_violation_total{reason}
reading_route_stale_reactivation_prevented_total
reading_route_active_to_inactive_apply_total
reading_route_inactive_to_active_apply_total
```

### Async Route Lag Signals

Track:

```text
SEO -> Reading route projection lag
New route unavailable because projection has not arrived
Route deactivation apply delay
Routes marked RequiresResync
Route repair/reconciliation outcomes
```

Suggested metrics:

```text
reading_seo_route_projection_lag_seconds
reading_seo_route_activation_lag_seconds
reading_seo_route_deactivation_lag_seconds
reading_seo_route_requires_resync_rows
reading_seo_route_reconciliation_mismatch_total
```

---

## 5. Source-Specific Projection Freshness Signals

Reading consumes independent async lanes from multiple source owners.

Do not use one generic `SourceVersion` or one generic freshness interpretation for all lanes.

| Source lane | Reading state affected | Version marker |
|---|---|---|
| Content | Article fields and public visibility | `ContentSourceVersion` |
| SEO | Slug route and SEO metadata | `SeoSourceVersion` |
| Media | Optional cover/media presentation | `MediaSourceVersion` |
| Interaction | Public displayed counters | `InteractionStatsVersion` |

### Common Apply Metrics

Track by `source_lane` and `event_type`:

```text
Message received count
Apply success count
Duplicate ignored count
Stale version ignored count
Version gap / resync-required count
Apply failure count
Retry count
Handler duration
Repository duration
Last successful apply time
Oldest unprocessed message age
```

Suggested metrics:

```text
reading_projection_message_received_total{source_lane,event_type}
reading_projection_apply_success_total{source_lane,event_type}
reading_projection_duplicate_ignored_total{source_lane,event_type}
reading_projection_stale_ignored_total{source_lane,event_type}
reading_projection_version_gap_total{source_lane,event_type}
reading_projection_requires_resync_total{source_lane,event_type}
reading_projection_apply_failed_total{source_lane,event_type,failure_code}
reading_projection_handler_duration_seconds{source_lane,event_type}
reading_projection_repository_duration_seconds{source_lane,event_type}
reading_projection_last_success_timestamp_seconds{source_lane}
reading_projection_lag_seconds{source_lane,event_type}
```

### Apply Lag Measurement

For successfully applied messages, measure:

```text
AppliedAtUtc - OccurredAtUtc
```

Interpretation:

```text
Content lag affects article availability and visibility risk.

SEO lag affects slug route availability and deactivation risk.

Media lag affects optional presentation only.

Interaction lag affects displayed counter freshness only.
```

---

## 6. Content Projection Consumer Signals

Content is the highest-safety source lane because it determines local public visibility.

### Track

```text
Content article projection messages received/applied
Publish snapshots applied
Non-public snapshots applied
Duplicate/stale Content snapshots ignored
Content version gap/resync detections
Content projection apply failures
Time to apply non-public visibility changes
```

Suggested metrics:

```text
reading_content_projection_apply_total{state}
reading_content_projection_lag_seconds{state}
reading_content_non_public_apply_lag_seconds{state}
reading_content_stale_snapshot_ignored_total
reading_content_version_gap_total
reading_content_visibility_requires_resync_total
```

### Interpretation

```text
Publish lag may cause safe temporary absence.

Unpublish/archive/delete lag is higher risk because old public state
may remain locally available until the newer projection is applied.

A locally applied non-public snapshot must immediately prevent public serving.

A stale Content message must never restore older public visibility.
```

### Alert Direction

Higher alert priority should be considered for lag involving:

```text
Unpublish
Archive
Soft delete
Visibility unsafe / resync-required transitions
```

than for newly published content temporarily not appearing.

---

## 7. SEO Route Projection Consumer Signals

SEO route projection controls local slug lookup availability.

### Track

```text
Route snapshots received/applied
Route activation applied
Route deactivation applied
Route RequiresResync state
Duplicate/stale route snapshots ignored
Route projection lag
Slug request misses after expected route activation
```

Suggested metrics:

```text
reading_seo_route_apply_total{route_state}
reading_seo_route_projection_lag_seconds{route_state}
reading_seo_route_stale_snapshot_ignored_total
reading_seo_route_version_gap_total
reading_seo_route_requires_resync_total
reading_slug_safe_404_total{internal_reason}
```

### Interpretation

```text
Route activation lag may cause a newly available slug to return safe 404.

Route deactivation lag may temporarily preserve old local route state.

Once a route is locally inactive or unsafe, Reading must not serve through it.

A stale route message must never reactivate a newer deactivated route.
```

---

## 8. Media Enrichment Signals

Media presentation is optional for core public-serving correctness.

### Track

```text
Media projection messages received/applied
Media projection lag
Missing cover responses
Media stale snapshot ignores
Media apply failures
Responses served without cover/media enrichment
```

Suggested metrics:

```text
reading_media_projection_apply_total
reading_media_projection_lag_seconds
reading_media_stale_snapshot_ignored_total
reading_media_apply_failed_total
reading_response_media_missing_total{endpoint}
reading_response_media_last_known_served_total{endpoint}
```

### Interpretation

```text
Media lag is a user-experience degradation, not a public visibility failure.

Media apply failure should not cause a safely public article to return 404.

Media must never alter article public visibility state.
```

---

## 9. Interaction Counter Snapshot Signals

Interaction owns engagement truth and publishes versioned public counter snapshots.

Reading consumes:

```text
interaction.article_counters_projection_published
```

Expected displayed counter fields:

```text
ViewCount
LikeCount
VisibleCommentCount
InteractionStatsVersion
```

### Track

```text
Counter snapshots received/applied
Counter snapshot lag
Duplicate/stale StatsVersion ignores
Counter snapshots received before article projection exists
Responses using zero/default counters
Responses using last-known counters
Counter apply failures
```

Suggested metrics:

```text
reading_interaction_counter_snapshot_received_total
reading_interaction_counter_snapshot_applied_total
reading_interaction_counter_snapshot_lag_seconds
reading_interaction_counter_snapshot_duplicate_ignored_total
reading_interaction_counter_snapshot_stale_ignored_total
reading_interaction_counter_snapshot_apply_failed_total
reading_interaction_counter_snapshot_without_article_total
reading_response_counters_defaulted_total{endpoint}
reading_response_counters_last_known_total{endpoint}
```

### Interpretation

```text
Counter lag does not affect public article visibility.

Counter lag should result in degraded but valid public responses.

Reading must never increment counters from raw interaction delivery.

Reading must never synchronously call Interaction to fill missing counters
during ordinary public reads.
```

### Not Tracked as V1 Reading Responsibilities

Do not treat the following as Reading V1 pipeline metrics:

```text
Raw per-view processing
Popularity score calculation
Trending ranking generation
Interaction moderation workflow
Counter truth reconciliation inside Reading
```

Those remain outside Reading V1 ownership.

---

## 10. Consumer Idempotency and Apply-Decision Signals

Reading consumers must handle at-least-once delivery safely.

### Apply Outcomes

Recommended apply-decision labels:

```text
Applied
DuplicateIgnored
StaleVersionIgnored
RequiresResync
FailedTransient
FailedPermanent
```

### Required Log Fields

Important projection consumer logs should include:

```text
ConsumerName
MessageId
EventType
SourceLane
AggregateId
ArticlePublicId where available
IncomingVersion
CurrentAppliedVersion
ApplyDecision
CorrelationId
OccurredAtUtc
ReceivedAtUtc
ProcessedAtUtc
RetryAttempt
FailureCode where applicable
```

### Suggested Metrics

```text
reading_consumer_apply_decision_total{source_lane,event_type,decision}
reading_consumer_retry_total{source_lane,event_type}
reading_consumer_dlq_total{source_lane,event_type}
reading_consumer_processing_duration_seconds{source_lane,event_type}
reading_consumer_last_applied_version{source_lane}
```

### Interpretation

```text
Duplicate delivery is expected and should usually be harmless.

Stale delivery is expected and must not overwrite newer local state.

Version gaps or unsafe apply outcomes may require resync or repair.

Timestamp order is diagnostic only and must not be used as freshness authority.
```

---

## 11. Producer-Side and Broker Signals Relevant to Reading

Reading does not own source-module outbox publication, but projection freshness depends on it.

### Relevant Upstream Signals

Track by source producer module:

```text
Outbox pending count
Outbox oldest pending age
Outbox publish success/failure rate
Outbox retry count
Outbox dead/final-failure count where applicable
```

Relevant producers:

```text
Content
SEO
Media
Interaction
```

Suggested platform-facing metrics consumed by Reading dashboards:

```text
outbox_pending_total{producer_module}
outbox_oldest_pending_age_seconds{producer_module}
outbox_publish_failed_total{producer_module,event_type}
rabbitmq_queue_depth{consumer="reading"}
rabbitmq_oldest_message_age_seconds{consumer="reading"}
rabbitmq_unacked_messages{consumer="reading"}
```

### Diagnosis Rule

Operators must be able to separate:

```text
Source truth committed but producer outbox has not published.

Message is published but waiting in broker backlog.

Reading consumer received message but failed to apply.

Reading consumer safely ignored duplicate or stale input.

Reading applied state but cache/public query still serves unexpected output.
```

---

## 12. Degraded Success Signals

Reading may successfully serve safe content while optional enrichment is missing or delayed.

### V1 Optional Degradation Categories

```text
Media cover/gallery missing or delayed
SEO response metadata missing where not required for local slug routing
Interaction counters defaulted or last-known
Related result state unavailable
Optional search presentation fields missing
Cache unavailable while projection query still succeeds
```

Popularity/trending is not included because it is deferred beyond V1.

### Suggested Metrics

```text
reading_degraded_response_total{endpoint,degradation}
reading_response_media_missing_total{endpoint}
reading_response_seo_metadata_missing_total{endpoint}
reading_response_counters_defaulted_total{endpoint}
reading_response_counters_last_known_total{endpoint}
reading_related_empty_or_fallback_total
reading_cache_bypassed_total{reason}
```

### Interpretation

```text
Degraded success is acceptable when core route/visibility safety passes.

Sustained degradation indicates projection or dependency health problems.

Optional enrichment failure must not be mixed with public visibility failure.
```

---

## 13. Cache Signals

If Reading cache is introduced, it must accelerate safe Reading-owned serving state only.

### Track

```text
Cache hit count
Cache miss count
Cache lookup duration
Cache refresh success/failure
Cache invalidation count
Stale cache rejected count
Cache write skipped because local state is unsafe
Cached response bypassed because route or visibility is denied
```

Suggested metrics:

```text
reading_cache_hit_total{endpoint}
reading_cache_miss_total{endpoint}
reading_cache_lookup_duration_seconds{endpoint}
reading_cache_refresh_failed_total{endpoint}
reading_cache_invalidation_total{source_lane}
reading_cache_stale_rejected_total{reason}
reading_cache_write_skipped_total{reason}
```

### Rules

```text
Cache must not bypass local article visibility checks.

Cache must not bypass local slug-route safety checks.

Locally known deny or unsafe state wins over cached public response.

Cache failure does not cause hard failure when Reading projection can still serve safely.

Cache miss must not silently trigger synchronous upstream-source fallback.
```

---

## 14. Search Observability

Search is public and potentially abuse-prone.

### Track

```text
Search request count
Search P50/P95/P99 latency
Search validation failure count
Search empty-result count
Search 503 count
Search rate-limit count
Non-public/unsafe candidate filtered count where measurable
Optional enrichment degradation on search results
High-frequency query/client patterns where platform policy allows
```

Suggested metrics:

```text
reading_search_requests_total{status}
reading_search_duration_seconds
reading_search_validation_failed_total{reason}
reading_search_empty_result_total
reading_search_unavailable_total
reading_search_candidate_filtered_total{reason}
reading_search_rate_limited_total
```

### Rules

```text
Search reads Reading-owned projection/search state only.

Search must not expose locally non-public or unsafe content.

Popularity/relevance ranking beyond basic V1 search is not assumed.

External search infrastructure is deferred unless separately adopted.
```

---

## 15. Related Article Observability

### Track

```text
Related endpoint request count
Related P50/P95/P99 latency
Parent article safe-404 count
Related empty-result count
Deterministic fallback count
Non-public/unsafe related candidate filtered count
Optional media/counter degradation in related results
```

Suggested metrics:

```text
reading_related_requests_total{status}
reading_related_duration_seconds
reading_related_parent_not_public_total
reading_related_empty_total
reading_related_fallback_total
reading_related_candidate_filtered_total{reason}
```

### Rules

```text
Empty related output is acceptable.

Related output failure must not invalidate an otherwise successful detail response.

Only locally safe public candidates may be returned.

Popularity-based related ranking is deferred beyond V1.
```

---

## 16. Repair and Rebuild Signals

Reading repair/rebuild applies only to Reading-owned derived state.

### V1 Repair/Rebuild Scopes

```text
ArticleReadModel repair/rebuild from Content-approved input
ArticleSeoRouteProjection repair/rebuild from SEO-approved input
Media enrichment repair from Media-approved input
Interaction counter enrichment repair from Interaction-approved snapshot input
Consumed-message/apply-decision investigation
```

Reading V1 does not own:

```text
Raw Interaction analytics rebuild
Popularity/trending score generation
Engagement truth reconciliation
Moderation workflow repair
```

### Workflow Metrics

Track:

```text
Run started/completed/failed count
Duration
Scope/source lane
Rows selected
Rows compared
Rows mismatched
Rows repaired
Rows skipped because newer local state exists
Candidate output generated count
Candidate validation failure count
Cutover success/failure count where candidate rebuild is used
```

Suggested metrics:

```text
reading_repair_run_started_total{scope}
reading_repair_run_completed_total{scope}
reading_repair_run_failed_total{scope,failure_code}
reading_repair_duration_seconds{scope}
reading_reconciliation_mismatch_total{source_lane}
reading_repair_rows_applied_total{source_lane}
reading_repair_newer_state_preserved_total{source_lane}
reading_rebuild_candidate_validation_failed_total{scope}
reading_rebuild_cutover_failed_total{scope}
```

### Safety Rules

```text
Repair must not mutate source truth.

Repair must not overwrite newer local projection with older input.

Repair must not reactivate stale route state.

Repair must not double-count counter state.

Incomplete candidate rebuild output must not become active serving state.
```

---

## 17. Required Dependency Health Signals

### Required for Public Serving

Required local dependencies include:

```text
Reading API runtime
Reading projection/query database
Required repository/query execution path
Optional cache only when cache configuration incorrectly makes it mandatory
```

Track:

```text
Dependency latency
Dependency error rate
Connection failure rate
Timeout rate
Pool saturation or resource exhaustion indicators
503 rate by dependency failure
```

Suggested metrics:

```text
reading_required_dependency_duration_seconds{dependency}
reading_required_dependency_failed_total{dependency,failure_code}
reading_required_dependency_timeout_total{dependency}
reading_public_service_unavailable_total{dependency}
```

### Optional Enrichment Dependencies

Optional dependencies or optional local data lanes include:

```text
Media enrichment state
Interaction counter enrichment state
Related output
Optional SEO response metadata
Cache acceleration
```

Track as degradation rather than hard serving failure unless the endpoint explicitly requires the capability.

### Explicit Non-Dependency in V1

Do not define normal public request dependency metrics for:

```text
Synchronous SEO route resolve
Synchronous Content visibility lookup
Synchronous Media fallback lookup
Synchronous Interaction counter lookup
```

These calls are not part of ordinary Reading V1 serving design.

---

## 18. Release Gates

During Reading rollout, monitor:

```text
P95/P99 latency changes per endpoint group
500 and 503 changes per endpoint group
Safe 404 changes for detail and slug routes
Content projection apply failure and lag
SEO route projection apply failure and lag
Media projection degradation rate
Interaction counter snapshot lag/defaulted response rate
Consumer duplicate/stale/resync apply decisions
RabbitMQ Reading queue backlog
Upstream producer outbox oldest-pending age by module
Cache stale-rejection and refresh-failure rates
Repair/rebuild failure and cutover validation failure
```

### Strong Stop Conditions

Immediate pause, rollback or operator intervention is warranted if:

```text
Reading serves locally known non-public article state.

Reading serves through locally inactive or RequiresResync route state.

Stale Content apply overwrites a newer local visibility denial.

Stale SEO apply reactivates a newer local route denial.

Stale cache overrides locally denied/unsafe state.

Partial rebuild output becomes active public serving state.

Public responses expose internal projection or hidden-resource diagnostics.

Source-specific version guards are missing or demonstrably ineffective.
```

---

## 19. Degraded but Acceptable Behavior

Reading is degraded but acceptable when:

```text
Public article list/detail still serves from locally safe state.

Newly published content is temporarily absent during bounded propagation lag.

Newly activated slug temporarily returns safe 404 during bounded route lag.

Counters are defaulted or last-known.

Media presentation is missing or delayed.

SEO presentation metadata is absent but slug-route/article visibility state is safe.

Related output is empty or deterministic fallback.

Cache is unavailable but projection-backed serving still works.

A repair/rebuild run is delayed while current active projection remains safe.
```

Reading is not acceptable when:

```text
Locally known non-public or unsafe content is served.

Locally known inactive/unsafe route is served.

Counter or media enrichment is incorrectly used as public visibility evidence.

Projection propagation or drift exceeds accepted SLO without alert/repair.

Stale event apply corrupts newer local state.

Incomplete rebuild candidate is exposed publicly.
```

---

## 20. Operator Questions This Module Must Answer

Reading observability should enable operators to answer:

```text
Is the public API unhealthy, or are only optional enrichments delayed?

Is a slug request returning 404 because local route projection is missing,
inactive, unsafe, or points to non-public local article state?

Is an article unavailable because Content projection has not arrived
or because local state denies it?

Is projection lag caused by source outbox delay, broker backlog,
Reading consumer failure, stale rejection or cache behavior?

Did Reading apply, ignore duplicate, ignore stale, mark resync-required
or fail a source projection message?

Are Interaction counters defaulted because Interaction did not publish,
broker delivery lagged or Reading failed to apply?

Did a stale message attempt to overwrite newer visibility, route,
media or counter state?

Is repair/rebuild required, running, failed or safely completed?

Is any local visibility or route safety invariant being violated?
```

---

## 21. Recommended Dashboard Separation

Reading operational dashboards should separate:

### Dashboard A — Public API Health

```text
Endpoint latency
2xx / 4xx / 5xx / 503 rates
Rate-limit rates
Degraded-success rates
```

### Dashboard B — Core Serving Safety

```text
Visibility denies
Route denies
RequiresResync rows
Local safety violations
Stale re-exposure/reactivation prevention
Content and SEO high-risk lag
```

### Dashboard C — Projection Consumer Health

```text
Message receive/apply/failure outcomes
Duplicate/stale/resync decisions
Queue backlog
Handler duration
Apply lag per source lane
```

### Dashboard D — Optional Enrichment Health

```text
Media projection lag
Counter snapshot lag
Counter defaulted/last-known response rates
Related empty/fallback results
Cache behavior
```

### Dashboard E — Repair/Rebuild Health

```text
Mismatch rates
Repair outcomes
Rebuild runs
Candidate validation
Cutover safety
```

---

## 22. Non-Goals

This document does not define:

```text
Exact numeric SLO thresholds
Alert severity thresholds
Dashboard implementation code
Prometheus instrumentation implementation
Grafana layout details
RabbitMQ topology
Outbox schema
Worker retry/DLQ implementation
Cache TTL values
Canonical redirect behavior
Popularity/trending observability
Emergency upstream fallback behavior
```

These belong to future operational policy, infrastructure implementation or dedicated ADRs.

---

## 23. V1 Observability Summary

| Concern | V1 observability position |
|---|---|
| Public serving | Measure per endpoint group from Reading-owned projections |
| Article visibility | Track local deny/unsafe behavior and Content projection lag |
| Slug routing | Track local route denies and SEO route projection lag |
| Media | Track optional enrichment delay/degradation |
| Interaction counters | Track snapshot lag, stale/duplicate apply and defaulted responses |
| Popularity/trending | Not part of Reading V1 |
| Sync source fallback | Not part of ordinary Reading V1 serving |
| Cache | Acceleration only; track stale rejection and safe bypass |
| Repair/rebuild | Reading-owned derived state only; validate before cutover |
| Event ordering | Measure source-specific version apply decisions |
| Critical incident | Serving locally known non-public/unsafe content or unsafe route |
```