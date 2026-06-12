# Implementation Plan — Feature 001: Lure Catalog MVP

**Spec:** `specs/001-lure-catalog-mvp/spec.md`
**Constitution compliance:** verified against Articles I–IX
**Last updated:** 2026-06-10

---

## Pre-Implementation Gates

### Simplicity Gate (Article IX)
- [x] ≤ 3 top-level projects? → Yes: `api` (backend), `web` (frontend), `infra` (IaC).
- [x] No future-proofing beyond what spec requires? → Schema has forward-compat columns
  (`lat/lng`, `deleted_at`) but no code paths for v2 features.

### Search Gate (Article V)
- [x] No SQL `LIKE` for full-text? → Typesense for all search and autocomplete.
- [x] Sync strategy defined? → Write-through from API on catalog mutations.

### Auth Gate (Article IV)
- [x] Using standard OIDC? → Yes, via Supabase Auth (wraps OIDC for Google + Microsoft MSA).
- [x] No custom JWT? → Supabase issues JWTs; API validates using Supabase's JWKS endpoint.

---

## Tech Stack Decisions

| Layer | Choice | Rationale |
|---|---|---|
| API | **Node.js + TypeScript** (Fastify) | Type safety, fast startup, good Supabase SDK support. |
| Web | **Next.js 14** (App Router, SSR) | SEO-critical (catalog pages must be indexable). SSR for detail pages, client components for interactive filters. |
| Database | **Azure Database for PostgreSQL Flexible Server** | Managed, RGPD-compliant region (West Europe). |
| Search | **Typesense Cloud** | Managed, no ops, excellent faceted search. Fallback to single-node on Azure Container Apps if cost is a constraint. |
| Auth | **Supabase Auth** | Handles Google + Microsoft MSA OIDC, email/password, refresh tokens, and multi-provider linking out of the box. |
| Storage | **Azure Blob Storage** | For lure images. CDN via Azure Front Door. (v2: catch photos.) |
| Cache | **Azure Cache for Redis** | Session store, rate limiting counters, autocomplete cache. |
| IaC | **Bicep** | Azure-native, no Terraform state management overhead for a small team. |
| CI/CD | **GitHub Actions** | Standard for the repo. Separate pipelines per layer. |

---

## Architecture Overview

```
Browser / Mobile Web
       │
       ▼
  Next.js (SSR)  ◄──── Azure Front Door (CDN + WAF)
       │
       ▼
  Fastify API  ──────► Typesense Cloud  (catalog search)
       │
       ├──────────────► Azure PostgreSQL  (source of truth)
       │
       ├──────────────► Supabase Auth     (identity + sessions)
       │
       └──────────────► Azure Redis       (rate limits, cache)
```

All services in Azure West Europe (data residency EU — RGPD compliant).

---

## Implementation Phases

### Phase 0 — Infrastructure and Developer Environment
*Prerequisite for all other phases. Blocks nothing in parallel.*

- [ ] Provision Azure PostgreSQL Flexible Server (dev + prod environments).
- [ ] Provision Azure Blob Storage + CDN rule for `/images/*`.
- [ ] Provision Azure Cache for Redis.
- [ ] Configure Typesense Cloud project; obtain API keys.
- [ ] Configure Supabase project; enable Google and Microsoft providers.
- [ ] Set up GitHub Actions pipelines: lint → test → build → deploy (staging only in this
  phase).
- [ ] Initialise monorepo structure: `apps/api`, `apps/web`, `packages/db`, `infra/`.

### Phase 1 — Database Schema
*Can begin in parallel with Phase 0 after repo initialisation.*

- [ ] Apply full DDL from `specs/001-lure-catalog-mvp/data-model.md`.
- [ ] Seed script: 20 brands, 20 species (with PT translations), 50 lures (sample data
  for development). See `specs/001-lure-catalog-mvp/seed-plan.md`.
- [ ] `packages/db`: typed query client (Kysely or Drizzle ORM); generated types from
  schema.
- [ ] Database migration tooling set up (e.g. Flyway or Drizzle Kit migrations).

### Phase 2 — Catalog API (read path)
*Depends on Phase 1. [P] with Phase 3.*

- [ ] `GET /v1/lures` — paginated list with Typesense-backed filter + search.
  See `contracts/lures-list.yaml`.
- [ ] `GET /v1/lures/:slug` — detail page data.
  See `contracts/lure-detail.yaml`.
- [ ] `GET /v1/lures/suggest` — autocomplete endpoint (Typesense instant search).
- [ ] `GET /v1/species` — list for filter dropdown.
- [ ] `GET /v1/brands` — list for filter dropdown.
- [ ] Typesense collection schema + indexing job for initial seed.
- [ ] Write-through sync: on any lure INSERT/UPDATE in Postgres → re-index in Typesense.

### Phase 3 — Auth Integration
*Depends on Phase 0 (Supabase configured). [P] with Phase 2.*

- [ ] Supabase Auth integrated in Next.js (server-side session via `@supabase/ssr`).
- [ ] Google sign-in flow: end-to-end.
- [ ] Microsoft MSA sign-in flow: end-to-end (validate `iss` = `https://login.microsoftonline.com/9188040d-.../v2.0`).
- [ ] Email + password flow: registration, sign-in, password reset.
- [ ] Username selection screen on first OAuth sign-in.
- [ ] Multi-provider linking: user in settings can link a second provider to existing
  account.
- [ ] `POST /v1/auth/sync` — internal endpoint; called by Supabase webhook on new user
  created; creates `users` row in Postgres.
- [ ] Rate limiting middleware on API (Redis-backed, per IP and per user).

### Phase 4 — Catalog Frontend (read path)
*Depends on Phases 2 and 3 (auth needed for favorites state on cards).*

- [ ] Catalog listing page (`/iscas`): grid + filter panel.
- [ ] Filter panel: lure type, water type, species, weight range (slider), brand,
  depth range. URL-synced state.
- [ ] Sort controls: popularity, avg price, newest.
- [ ] Lure card component: image, name, brand, type badge, weight, species icons,
  avg price, favorite count, favorite button (auth-gated).
- [ ] Infinite scroll or pagination (decision: [NEEDS CLARIFICATION — UX preference]).
- [ ] Autocomplete search bar in header (shared component).
- [ ] Lure detail page (`/iscas/:slug`): full spec sheet, gallery, pricing table, reviews.
- [ ] i18n: PT-PT strings 100%, EN strings 100%. ES strings 80%.
- [ ] SEO: `<title>`, `<meta description>`, Open Graph, structured data
  (`Product` schema.org for lures with price).

### Phase 5 — Favorites and Inventory
*Depends on Phase 3 (auth) and Phase 4 (frontend components).*

- [ ] `POST /v1/me/favorites/:lureId` — add favorite.
- [ ] `DELETE /v1/me/favorites/:lureId` — remove favorite.
- [ ] `GET /v1/me/favorites` — list favorites (paginated, filterable, same as catalog).
  See `contracts/favorites.yaml`.
- [ ] `POST /v1/me/inventory` — add to inventory (with quantity, condition, color, notes).
- [ ] `PATCH /v1/me/inventory/:entryId` — update entry.
- [ ] `DELETE /v1/me/inventory/:entryId` — remove entry.
- [ ] `GET /v1/me/inventory` — list inventory grouped by lure type.
  See `contracts/inventory.yaml`.
- [ ] Favorites page (`/conta/favoritos`).
- [ ] Inventory page (`/conta/inventario`): grouped by type, shows quantity + condition.
- [ ] Optimistic UI for favorite toggle.
- [ ] "Add to inventory" modal on lure card and detail page.

### Phase 6 — Reviews
*Depends on Phase 3 (auth) and Phase 4 (detail page).*

- [ ] `POST /v1/lures/:slug/reviews` — submit review (rating + optional text).
- [ ] `PATCH /v1/lures/:slug/reviews/:reviewId` — edit own review.
- [ ] `DELETE /v1/lures/:slug/reviews/:reviewId` — delete own review.
- [ ] `GET /v1/lures/:slug/reviews` — paginated list, sorted by recent.
- [ ] `POST /v1/reviews/:reviewId/helpful` — mark helpful (toggle).
- [ ] Review list + form on lure detail page.
- [ ] Aggregate rating (avg + distribution) displayed prominently on detail page.

### Phase 7 — User Profile and Backoffice
*Can begin in parallel with Phase 6.*

- [ ] Public profile page (`/u/:username`): avatar, username, member since, counts.
- [ ] Settings page: update display name, avatar. Link/unlink auth providers.
- [ ] RGPD: "Delete my account" flow → soft-delete (`deleted_at`), nullify PII,
  confirmation email.
- [ ] Backoffice (internal, auth-gated to `role = 'admin'`):
  - Lure CRUD with form validation against mandatory schema fields.
  - Brand and species management.
  - `lure_retailer_prices` management (add link + price per retailer; auto-recalculates
    `price_6m_*` fields on save).
  - Review moderation: hide/show review.

### Phase 8 — QA, Performance, and Launch Readiness
- [ ] Lighthouse CI: LCP < 2.5s, CLS < 0.1, FID < 100ms on catalog and detail pages.
- [ ] Typesense query latency: p95 < 80ms (measured from API, not browser).
- [ ] Load test: 200 concurrent users on catalog listing, < 200ms p95 API response.
- [ ] WCAG 2.1 AA audit on catalog, detail, favorites, inventory pages.
- [ ] Security review: OWASP Top 10 checklist, rate limiting validation, auth flow
  penetration.
- [ ] Populate production catalog: 500+ lures, 50+ brands, 20+ species.
- [ ] PT-PT translation review by native speaker.
- [ ] Cookie banner: consent for analytics only (no essential cookies require consent).
- [ ] Staging → Production cutover checklist.

---

## Complexity Tracking

| Decision | Complexity Added | Justification |
|---|---|---|
| Typesense alongside Postgres | Medium | Required by Article V; SQL LIKE not acceptable at scale. |
| Supabase Auth | Low | Removes custom auth complexity; worth managed service cost. |
| SSR via Next.js | Medium | Required by SEO non-functional requirement (G5, US-03 indexability). |
| Write-through Typesense sync | Low-Medium | Simpler than CDC / event-driven sync for v1 volume. |
