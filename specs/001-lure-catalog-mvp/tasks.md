# Tasks — Feature 001: Lure Catalog MVP

Generated from `plan.md`, `data-model.md`, and `contracts/api.yaml`.
`[P]` marks tasks safe to run in parallel within the same group.

---

## Group A — Infrastructure (Phase 0)
*No dependencies. Start immediately.*

- [ ] A1 — Provision Azure PostgreSQL Flexible Server (dev + staging + prod). Region: West Europe.
- [ ] A2 — Provision Azure Blob Storage + configure CDN rules for `/images/*` via Azure Front Door.
- [ ] A3 — Provision Azure Cache for Redis (C1 tier for dev, C2 for prod).
- [ ] A4 `[P]` — Set up Typesense Cloud project; create `lures` collection with schema from `data-model.md`.
- [ ] A5 `[P]` — Configure Supabase project; enable Google OAuth and Microsoft OIDC providers. Document redirect URIs.
- [ ] A6 — Initialise monorepo: `apps/api` (Fastify + TypeScript), `apps/web` (Next.js 14), `packages/db` (Kysely), `infra/` (Bicep). Linting, formatting, path aliases.
- [ ] A7 — Set up GitHub Actions: lint + type-check + test pipeline per workspace. Deploy pipeline to staging (manual trigger for now).

---

## Group B — Database (Phase 1)
*Depends on A1, A6.*

- [ ] B1 — Apply full DDL from `data-model.md` (brands, species, lures, lure_translations, lure_colors, lure_images, lure_target_species, lure_retailer_prices, users, user_auth_providers, user_lure_favorites, user_lure_inventory, lure_reviews, review_helpful_votes).
- [ ] B2 `[P]` — Migration tooling: configure Drizzle Kit migrations folder; create initial migration from DDL.
- [ ] B3 `[P]` — Seed script: 20 brands (PT translations), 20 species (PT common names), 50 sample lures (all mandatory fields). Must pass Article II completeness check.
- [ ] B4 — `packages/db`: generate TypeScript types from schema. Export typed query helpers for the 10 most common read patterns (list lures, get lure by slug, get user favorites, etc.).

---

## Group C — Catalog API Read Path (Phase 2)
*Depends on B1, B4. [P] with Group D.*

- [ ] C1 — Typesense sync job: on lure INSERT/UPDATE in Postgres, upsert document in Typesense `lures` collection. Implement as Fastify plugin + event emitter.
- [ ] C2 — `GET /v1/lures` — implement per `contracts/api.yaml`. Typesense query with facets, filters, pagination. Return `LureCard[]` + `CatalogFacets`.
- [ ] C3 `[P]` — `GET /v1/lures/suggest` — Typesense instant search, max 8 results, debounce handled client-side.
- [ ] C4 `[P]` — `GET /v1/lures/:slug` — Postgres join query (lure + translations + colors + images + species + pricing + review aggregate). Return `LureDetail`.
- [ ] C5 `[P]` — `GET /v1/species` and `GET /v1/brands` — simple list endpoints for filter dropdowns (Redis-cached, TTL 1h).
- [ ] C6 — Integration tests for C2–C5 against a test Postgres instance with seed data. No mocks for DB layer.

---

## Group D — Auth (Phase 3)
*Depends on A5, A6. [P] with Group C.*

- [ ] D1 — Integrate Supabase Auth in `apps/api`: JWT validation middleware using Supabase JWKS. Attach `user` to Fastify request context.
- [ ] D2 `[P]` — Integrate Supabase Auth in `apps/web`: `@supabase/ssr` server-side session. `middleware.ts` to protect `/conta/*` routes.
- [ ] D3 — Google OAuth flow: end-to-end test (sign in → Supabase creates user → webhook → `users` row created in Postgres).
- [ ] D4 `[P]` — Microsoft MSA flow: end-to-end test. Validate issuer = `https://login.microsoftonline.com/9188040d-6c67-4c5b-b112-36a304b66dad/v2.0` (consumers endpoint).
- [ ] D5 `[P]` — Email + password: registration, sign-in, password reset via Supabase Auth.
- [ ] D6 — `POST /v1/auth/sync` internal webhook endpoint: creates `users` row on Supabase `user.created` event. Idempotent.
- [ ] D7 — Username selection screen (Next.js route `/onboarding/username`): triggered on first sign-in when `username IS NULL`. Validates uniqueness against Postgres.
- [ ] D8 — Rate limiting middleware: Redis-backed sliding window. 100 req/min per IP (unauthed), 300 req/min per user (authed).

---

## Group E — Catalog Frontend (Phase 4)
*Depends on C2, C3, C4, D2.*

- [ ] E1 — `LureCard` component: image, name, brand, type badge, weight, species icons, avg price, favorites count, favorite button (auth-gated with optimistic update).
- [ ] E2 — Filter panel component: checkboxes for type/water/species/brand, range sliders for weight and depth. URL query param sync via `useSearchParams`.
- [ ] E3 — Catalog page `/iscas`: grid layout, filter panel (collapsible on mobile), sort controls. SSR for initial load (SEO), client navigation for filter changes.
- [ ] E4 `[P]` — Autocomplete search bar: Typesense `suggest` endpoint, 200ms debounce, keyboard navigation.
- [ ] E5 `[P]` — Lure detail page `/iscas/:slug`: SSR. Full spec sheet layout, image gallery, color swatch selector, pricing table, review aggregate, breadcrumb.
- [ ] E6 — i18n setup: `next-intl` or `next-i18next`. PT-PT and EN namespaces 100%. ES namespace 80%.
- [ ] E7 — SEO: `generateMetadata` for catalog + detail pages. Schema.org `Product` structured data on detail page.
- [ ] E8 — Accessibility pass on catalog and detail pages: keyboard nav, ARIA labels on filter inputs, focus management.

---

## Group F — Favorites and Inventory (Phase 5)
*Depends on D1, D2, E3.*

- [ ] F1 — `POST/DELETE /v1/me/favorites/:lureId` — implement per contract. Atomic upsert/delete.
- [ ] F2 `[P]` — `GET /v1/me/favorites` — paginated list, returns `LureCard[]` with `is_favorited: true`.
- [ ] F3 `[P]` — `POST /v1/me/inventory`, `PATCH /v1/me/inventory/:entryId`, `DELETE /v1/me/inventory/:entryId`, `GET /v1/me/inventory` — implement per contract.
- [ ] F4 — Optimistic favorite toggle: update local state immediately, revert on API error.
- [ ] F5 `[P]` — Favorites page `/conta/favoritos`: reuses catalog grid + filter components with `GET /v1/me/favorites` as data source.
- [ ] F6 `[P]` — Inventory page `/conta/inventario`: grouped by `lure_type`, shows quantity + condition badge.
- [ ] F7 `[P]` — "Add to inventory" modal: lure card and detail page trigger. Form with quantity, condition, color selector, notes.

---

## Group G — Reviews (Phase 6)
*Depends on D1, E5.*

- [ ] G1 — `POST /v1/lures/:slug/reviews` — create review. Enforce 1-per-user uniqueness.
- [ ] G2 `[P]` — `PATCH/DELETE /v1/lures/:slug/reviews/:reviewId` — owner-only guard.
- [ ] G3 `[P]` — `GET /v1/lures/:slug/reviews` — paginated, sorted by recent/helpful.
- [ ] G4 `[P]` — `POST /v1/reviews/:reviewId/helpful` — toggle vote.
- [ ] G5 — Review list + form on detail page (`/iscas/:slug`): star rating input, text area, submit.
- [ ] G6 `[P]` — Rating aggregate display: avg stars + distribution bar chart on detail page.

---

## Group H — Profile and Backoffice (Phase 7)
*Depends on D2. [P] with Group G.*

- [ ] H1 — Public profile page `/u/:username`: avatar, username, counts (favorites, inventory, reviews).
- [ ] H2 `[P]` — Settings page `/conta/definicoes`: update display name and avatar. Link/unlink providers.
- [ ] H3 `[P]` — Account deletion flow: soft-delete + nullify PII + confirmation email + redirect.
- [ ] H4 — Backoffice scaffold: auth-gated (`role = 'admin'`), route group `/admin`.
- [ ] H5 `[P]` — Backoffice lure CRUD: form validates mandatory schema fields before publish. Drag-and-drop image reorder.
- [ ] H6 `[P]` — Backoffice brand and species management.
- [ ] H7 `[P]` — Backoffice `lure_retailer_prices` management: add/edit retailer link + price. On save, recalculate `price_6m_*` fields in `lures`.
- [ ] H8 `[P]` — Backoffice review moderation: list reviews, hide/show action.

---

## Group I — QA and Launch (Phase 8)
*Depends on all groups. Sequential.*

- [ ] I1 — Lighthouse CI integration in GitHub Actions. Fail build if LCP > 2.5s on catalog or detail page.
- [ ] I2 — Load test: k6 script, 200 VUs on `GET /v1/lures`. Assert p95 < 200ms.
- [ ] I3 — WCAG 2.1 AA audit: axe-core automated scan + manual keyboard navigation test.
- [ ] I4 — Security checklist: OWASP Top 10, rate limit bypass test, auth flow review.
- [ ] I5 — PT-PT native speaker review of all UI copy.
- [ ] I6 — Populate production catalog: 500 published lures, 50 brands, 20 species.
- [ ] I7 — Cookie banner: implement consent management (analytics opt-in only).
- [ ] I8 — Staging → Production cutover: DNS, SSL, smoke test checklist.

---

## Parallel Execution Summary

Safe parallel groups for a 2-developer team:

| Sprint | Dev 1 | Dev 2 |
|---|---|---|
| 1 | A1–A3 (infra) | A4–A5 (Typesense + Supabase config) |
| 2 | B1–B4 (schema + seed) | A6–A7 (monorepo + CI) |
| 3 | C1–C6 (catalog API) | D1–D8 (auth) |
| 4 | E1–E8 (catalog frontend) | F1–F7 (favorites + inventory API + pages) |
| 5 | G1–G6 (reviews) | H1–H8 (profile + backoffice) |
| 6 | I1–I8 (QA + launch) | I1–I8 (shared) |
