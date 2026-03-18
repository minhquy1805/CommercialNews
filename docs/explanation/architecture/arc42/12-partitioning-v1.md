## 12) Partitioning (V1)

This section defines the partitioning strategy and partition-readiness posture for CommercialNews (V1).
It applies DDIA Chapter 6 concepts pragmatically within the current domain-partitioned modular monolith (API + Worker).

### Related

* `03-building-blocks-modularity.md`
* `04-runtime-view-v1.md`
* `05-quality-requirements.md`
* `06-measurement-guide.md`
* `09-architecture-style.md`
* `10-system-data.md`
* `11-replication-v1.md`
* `13-transactions-and-consistency-v1.md`

---

### 12.1 Design intent (V1)

CommercialNews V1 is read-heavy and bursty. Partitioning in V1 is primarily a:

* **readiness strategy** (prepare for scale without premature sharding)
* **workload isolation strategy** (protect read path from async side effects)
* **hotspot mitigation framework** (before full DB sharding)

#### V1 priorities

* Protect public read path performance and availability
* Preserve truth correctness (identity/auth/publish visibility/SEO routing safety)
* Isolate bursty async work (audit, notifications, interaction aggregation)
* Keep a clean path to V2+ projections / stronger partitioning when signals justify it

---

### 12.2 Scope of partitioning in CommercialNews (V1)

Partitioning is not only “database sharding”.

CommercialNews applies partitioning concepts at two levels:

#### A) Data partitioning (future / selective)

How records may be split into logical partitions (range/hash/hybrid) and later placed on nodes/stores.

#### B) Workload partitioning (relevant in V1)

How async work is split and distributed across the Background Worker, e.g.:

* outbox publishing work
* consumer lanes / ownership
* aggregation lanes
* projection rebuild buckets (V2+)
* retry/delivery lanes (notifications)

---

### 12.3 Core principles

#### 12.3.1 Partitioning and replication are complementary

* **Partitioning** = who owns the data/work unit
* **Replication** = where copies/derived effects exist and how lag is handled

See `11-replication-v1.md` for truth vs derived, lag budgets, and fallbacks.

#### 12.3.2 Partitioning follows access patterns, not trends

Partitioning decisions must be driven by:

* exact lookup vs range scan
* secondary-index-heavy queries
* hotspot/skew risk
* rebalancing cost
* routing complexity
* fallback/correctness requirements

#### 12.3.3 V1 is partition-ready, not over-engineered

Before sharding, V1 prioritizes:

* bounded queries
* indexes
* cache/fallback policy
* async side effects
* idempotency
* backlog/lag observability

---

### 12.4 Partitioning strategies (guidance)

#### A) Key-range partitioning

**Good for**

* range scans / time-window queries
* retention/replay/investigation patterns

**Risk**

* hotspot on current/newest range (e.g., timestamp-heavy writes)

**Likely future fit**

* append-only logs (audit/login/view events)
* some history/replay workloads

#### B) Hash partitioning

**Good for**

* exact lookup
* more even distribution across many keys

**Risk**

* destroys ordering (range queries become harder)
* does not solve hot key overload by itself

**Likely future fit**

* exact lookup heavy paths
* some workload-lane ownership schemes

#### C) Hybrid partitioning (recommended mindset)

Use one part of the key for partition ownership and another for ordering within a partition.

This is often the most practical long-term model for CommercialNews workloads.

---

### 12.5 Secondary indexes and partitioning (important for admin/search paths)

Secondary-index queries do not map cleanly to partition keys. This matters for:

* Content admin list/filter/search
* SEO admin queries
* Audit investigation queries
* future projections/search paths

#### A) Local indexes (document-partitioned)

* simpler writes
* secondary-index reads may require scatter/gather

#### B) Global / term-partitioned indexes (or dedicated search/read projections)

* better read/search efficiency
* more complex writes and async index maintenance
* eventual consistency / index lag must be observable

#### V1 posture

* use truth-store indexes + bounded queries + Query Facade
* evolve to projections/read models/search when signals justify it (V2+)

---

### 12.6 Hotspots, skew, and relief strategy (V1)

CommercialNews tracks:

* data skew
* read skew
* write skew
* hot keys (viral article, hot slug, etc.)

#### V1 mitigation priorities (before DB sharding)

* protect read path (non-blocking interaction tracking)
* cache-first + truth fallback (correctness-first behavior)
* async isolation (Outbox → Broker → Consumers)
* retries/backoff + idempotent consumers
* batching and backpressure
* Redis for cache/counters/rate-limit/dedup (not source of truth)

#### V2+ selective options

* sharded counters / salted keys
* aggregation lanes / ownership shards
* projection/search partitions

---

### 12.7 Consistency and fallback impact on partitioning

Partitioning/projections must preserve `11-replication-v1.md` guarantees.

#### Strict / read-your-writes paths (primary-first)

* Identity self-state after sensitive changes
* Admin governance reads after role/permission changes

#### Public read path (latency-sensitive, correctness-first fallback)

* cache-first where appropriate
* fallback to truth when derived data is stale/unavailable
* never leak drafts/unpublished content

#### Eventual paths (observable + recoverable)

* audit ingestion
* notifications delivery state
* interaction aggregates
* V2+ projections/search indexes

---

### 12.8 Rebalancing strategy (future scale path)

CommercialNews adopts the **indirection principle**:

`key/work-unit -> logical partition/lane -> node/worker`

#### Anti-pattern to avoid

Do not use direct `hash(key) mod N` for scale-critical ownership routing.

#### Rebalancing goals

* fair-enough load distribution
* keep core reads/writes functional during rebalance (or explicit degraded mode)
* minimize data/work movement

#### Candidate models (future, component-specific)

* fixed partitions
* dynamic split/merge
* vnode-like partitioning
* fixed logical worker lanes with ownership reassignment

---

### 12.9 Rebalance operations (automatic vs manual control)

Rebalancing is high-impact. CommercialNews prefers controlled automation for stateful/high-impact changes:

* system suggests plan
* operator approves execution (fully or partially)
* execution is throttled and observable

#### Guardrail examples (policy-level)

Pause/throttle when:

* read P95/P99 worsens
* read error rate increases
* outbox oldest pending age rises
* queue lag/backlog degrades
* consumer failure / DLQ rates spike

---

### 12.10 Request routing and routing metadata readiness

Partitioned systems require routing to the correct owner.

CommercialNews recognizes three future routing patterns:

* any node/worker + forward
* routing tier / coordinator
* partition-aware client/producer

#### Core requirement

There must be an authoritative mapping for:

* `partition/lane -> current owner`

And a defined policy for:

* metadata propagation
* stale metadata handling
* bootstrap discovery

**V1 relevance:** applies already to worker lane ownership and future projection/aggregation lanes.

---

### 12.11 Partition-readiness observability (V1)

Partitioning decisions must be signal-driven.

Key signals (see `06-measurement-guide.md`, `04-runtime-view-v1.md`, `11-replication-v1.md`):

* read path P95/P99 + error rate
* SEO fallback rate / truth fallback rate
* outbox pending count + oldest pending age
* queue depth / lag per consumer
* consumer latency/failure/retry rate
* DLQ rate/age (if enabled)
* dedupe hits / idempotency rejects
* projection freshness/checkpoint age (V2+)

---

### 12.12 V1 policy and follow-up

#### V1 policy

CommercialNews V1 does not introduce full cross-node DB sharding by default.

V1 must instead:

* document access patterns and hotspot risks
* keep read paths bounded and protected
* use cache/async/buffering before shard complexity
* measure lag/freshness/fallback explicitly
* preserve truth-first correctness and path-specific consistency

#### Required follow-up in module docs

Each `system-data-<module>-v1.md` **SHOULD** add a **Partitioning Readiness (V1/V2)** section:

* primary access patterns
* secondary-index-heavy queries
* hotspot/skew risks
* V1 mitigations
* V2+ options
* observability signals

#### Priority modules

* Interaction
* Notifications
* Audit
* SEO

---

### 12.13 Summary (V1)

CommercialNews applies partitioning concepts pragmatically inside the current architecture style:

* protect the read path first
* preserve truth correctness
* isolate bursty side effects via async processing
* measure lag/freshness/hotspots explicitly
* evolve toward stronger partitioning/projections only when signals justify it
