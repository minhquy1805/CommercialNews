# CommercialNews Documentation

This repository follows the **Diátaxis** documentation system:

- **Tutorials**: step-by-step onboarding for new contributors.
- **How-to Guides**: goal-oriented instructions (deploy, operate, secure).
- **Reference**: precise, factual lookup (API, config keys, data schema).
- **Explanation**: architecture, decisions, and domain reasoning.

## Where to start
- New to the project → `tutorials/01-onboarding.md`
- Want to understand the system → `explanation/architecture/arc42/00-index.md`
- Want to know "why we chose X" → `explanation/decisions/`

## Documentation rules (lightweight)
- If you change architecture direction → update arc42 + add/update an ADR.
- If you change public interfaces (API/config) → update `reference/`.
- If you add an operational procedure → add a `how-to/` guide.

## Scope (current)
Docs currently focus on:
- Core business capabilities (content/seo/media/reading/interaction/auth/admin/notifications)
- Architecture baseline (arc42 minimal set)
- Decision history (ADR)