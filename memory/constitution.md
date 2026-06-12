# Project Constitution — Lure Platform

> Governing principles for all specification, planning, and implementation decisions.
> Every technical choice must trace back to one or more of these articles.

---

## Article I: Iberian-First Content

All content, UX copy, species names, and lure names must treat **Portuguese (PT-PT)** as
the primary locale. Spanish (ES) is a first-class second locale. English (EN) is the
fallback for untranslated content only. UI must never default silently to EN when PT is
available.

## Article II: Catalog Integrity Before Features

A catalog with 400 complete lure records is worth more than a catalog with 2 000 partial
records. No feature that depends on catalog data ships until a **minimum completeness
schema** is enforced: `lure_type`, `weight_g`, `brand_id`, `water_type`, and at least one
`lure_target_species` entry are required fields. Draft records (`status = 'draft'`) are
never exposed via public API.

## Article III: Privacy and RGPD by Default

Any data that can identify a user — including approximate location, fishing session
records, and uploaded photos (EXIF) — must be treated as personal data under RGPD /
Lei 58/2019. Decisions:

- Soft-delete pattern on `users` (`deleted_at`) — never hard DELETE in application code.
- EXIF stripped server-side before storage (v2, when photos arrive).
- No personal data in URL parameters or query strings.
- Consent collected explicitly before storing session location data (v2).

## Article IV: Auth is Infrastructure, Not a Feature

Authentication is a prerequisite, not a differentiator. Use **OIDC/OAuth 2.0** standards.
Never build custom auth flows. Supported providers for v1: Google (personal accounts) and
Microsoft (MSA personal accounts only — not Azure AD / Entra). Email + password via a
standard identity provider (e.g. Supabase Auth or Auth0), not custom bcrypt + JWT.

## Article V: Search is a First-Class Service

Full-text and faceted search must **never** be implemented with SQL `LIKE` or `ILIKE` in
production. Typesense is the designated search engine for the catalog. The PostgreSQL
schema is the source of truth; Typesense is a read replica for search. Sync strategy:
write-through from the application layer on catalog mutations.

## Article VI: Denormalization for Read Performance, Authoritative Write to Postgres

Fields like `price_6m_min_eur`, `price_6m_avg_eur`, `price_6m_max_eur` on `lures` are
**derived, read-optimized fields**. They are calculated and written by the application
layer (not database triggers in v1). The application is the single source of recalculation
logic. Never read price aggregates from subqueries in listing endpoints.

## Article VII: No Premature Social Features

Features that require a critical mass of users to provide value (feed, follow graph, map
heatmap) must not ship before the catalog has > 500 published lures AND > 1 000 registered
users. Adding social plumbing (follower tables, activity events) to the schema before this
threshold is over-engineering.

## Article VIII: API Contracts Before Implementation

Every endpoint must have a written contract (OpenAPI 3.1 fragment) before any
implementation begins. The contract is the spec; the implementation must conform to it, not
the other way around. Breaking changes to contracts require a new spec version.

## Article IX: Operational Simplicity for a Small Team

The initial team is small. Prefer managed services over self-hosted. Prefer Azure-native
managed services (Azure Database for PostgreSQL Flexible Server, Azure Cache for Redis,
Azure Blob Storage) over Kubernetes-orchestrated self-hosted equivalents. Typesense Cloud
or a single-node Typesense on Azure Container Apps is preferred over a Typesense cluster
in v1. Complexity must be justified by scale requirements, not architectural preference.
