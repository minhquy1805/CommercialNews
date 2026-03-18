# SEO & Discoverability — Characteristic Profile (V1)

This profile captures the **top architecture characteristics** for the SEO & Discoverability module,
derived from its explicit/inferred requirements and domain concerns.

---

## Top characteristics (3–5)

1) **Correctness** (Cross-cutting)  
2) **Performance (Read Path Entry Point)** (Operational)  
3) **Availability / Robustness (Routing Safety)** (Operational)  
4) **Maintainability & Evolvability (Policy-driven SEO)** (Structural)

> Notes:
> - Slug/canonical rules are small in surface area but high in impact.
> - A single regression can cause long-lived traffic loss.

---

## Why it matters (domain-driven)

- **Slug is a public entry point**: it is used to route public requests to content. If slug lookup is slow or unstable, the entire read experience suffers.
- **SEO regressions are long-lived**: traffic loss often persists after the incident is fixed.
- Editors may rename titles frequently; the system must avoid breaking external links.
- SEO visibility must follow publication state: unpublished/archived content should not be indexable by policy.

---

## Design implications (what this forces in design)

### Correctness
- Enforce **slug uniqueness** under a clearly defined scope (e.g., system-wide).
- Canonical URL rules must be consistent to prevent duplicate indexing and SEO fragmentation.
- Meta defaults must be deterministic and safe (consistent fallback rules).
- Social preview fields must align with content state (published vs unpublished).

### Performance (Read Path Entry Point)
- Slug lookup (slug → articleId) must be fast and stable under peak read traffic.
- SEO read endpoints should remain lightweight and predictable.

### Availability / Robustness (Routing Safety)
- Routing by slug must degrade gracefully:
  - if SEO metadata is missing or inconsistent, the system should fail safely (e.g., not expose draft content).
- SEO must not become a single point of failure for the read path.

### Maintainability & Evolvability (Policy-driven SEO)
- Define a **slug stability policy**: title changes do not automatically imply slug changes.
- Keep SEO rules and policies isolated within the SEO module (avoid leaking slug policy into Content logic).
- Support an evolution path for V2:
  - slug alias/redirect strategy
  - sitemap/robots generation policies and triggers

---

## Suggested measures (policy-level)

- **Slug uniqueness violations**: collisions detected (target: 0).
- **Slug routing latency**: P95/P99 for slug lookup on public entry points (must remain responsive under peak).
- **SEO correctness checks** (automated or sampled):
  - canonical present and consistent
  - meta defaults applied when missing
- **Incidents**: SEO regressions tracked as high-severity because of long-lived impact.

---

## Key trade-offs

- Stronger slug stability (never changing slug) improves link stability but reduces editorial flexibility.
- Supporting redirects/aliases (V2) increases complexity but reduces SEO breakage risk.
- Keeping SEO robust for peak traffic may require caching and careful failure handling.

---

## ADR candidates (from this profile)

- Slug change policy (and V2 alias/redirect strategy).
- Canonical rules for duplicates or near-duplicates.
- Slug uniqueness scope (global vs per category/locale).
- SEO behavior when content is unpublished/archived (indexability policy).