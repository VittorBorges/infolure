# Feature 001 — Lure Catalog MVP

**Branch:** `001-lure-catalog-mvp`
**Status:** Draft
**Author:** [architect]
**Last updated:** 2026-06-10

---

## Overview

Build the core catalog experience for a fishing lure platform targeting the Iberian market
(Portugal and Spain). Users can discover lures through search and filters, view detailed
technical sheets, save favorites, manage a personal inventory ("lures I own"), and see
curated pricing from Iberian retailers — all in PT-PT as the primary language.

This is the **v1 scope**. Social features, map, photo uploads, and price monitoring are
explicitly excluded and tracked in future feature specs.

---

## Problem Statement

Iberian fishing enthusiasts have no dedicated platform that combines:
1. A technically complete lure catalog with species and environment context specific to
   Portuguese and Spanish waters.
2. A personal inventory to track what lures they own.
3. Curated pricing in EUR from local retailers.
4. A native PT-PT experience.

Existing platforms (Fishbrain, Tackle Warehouse, Pecheur.com) are either English-only,
lack technical depth, or are e-commerce sites without community or inventory features.

---

## Goals

- **G1** — A user can find a lure in under 30 seconds using filters relevant to Iberian
  fishing (species, water type, weight, environment).
- **G2** — A user can understand a lure's technical characteristics without leaving the
  platform or consulting external sources.
- **G3** — A user can maintain a personal record of the lures they own, with quantity and
  condition.
- **G4** — A user can see curated EUR pricing and a link to buy from PT/ES retailers.
- **G5** — The entire experience is available in PT-PT natively; EN and ES as supported
  secondary locales.

---

## Non-Goals (explicitly out of scope for v1)

- NG1 — Social feed, follow graph, or activity stream.
- NG2 — User-uploaded photos or catch logging.
- NG3 — Map of fishing spots or catch heatmap.
- NG4 — Automated price monitoring or price history charts.
- NG5 — Mobile native apps (iOS / Android).
- NG6 — Azure AD / Entra ID / B2B authentication.
- NG7 — User-generated catalog contributions (UGC).

---

## User Stories

### Catalog discovery

**US-01** — As a visitor (unauthenticated), I can browse the lure catalog with filters so
that I find lures suitable for my fishing context without creating an account.

*Acceptance criteria:*
- [ ] Filter by: lure type, water type, target species, weight range, brand, depth range.
- [ ] Results update without full page reload (client-side filter state).
- [ ] Filter state is reflected in URL query params (shareable / bookmarkable).
- [ ] Default sort is by popularity (number of favorites + inventory adds); user can
  switch to avg price asc/desc, newest.
- [ ] Empty state when filters return 0 results shows a clear message and a "reset
  filters" CTA.
- [ ] Minimum 20 results per page; pagination or infinite scroll.

**US-02** — As a visitor, I can search lures by name, brand, or model so that I can find
a specific lure I already know.

*Acceptance criteria:*
- [ ] Full-text search across lure name, brand name, and model reference (all locales).
- [ ] Search results ranked by relevance; exact model match scores highest.
- [ ] Autocomplete suggestions appear after 2 characters with debounce ≥ 200ms.
- [ ] Search and filter can be combined (search within filtered results).
- [ ] No results state shows the query and suggests broadening filters.

**US-03** — As a visitor, I can view a lure's detail page so that I understand its full
technical specification.

*Acceptance criteria:*
- [ ] Detail page shows: name, brand, type, all color variants (with swatches), weight,
  length, depth range, hook size, hook type, hook count, material, target species (with
  confidence level), suitable environment (water type, depth zone), technique
  recommendations, description.
- [ ] Price section shows: avg price (6m), min/max range, and up to 3 retailer links with
  individual prices and stock status. Section hidden if no pricing data exists.
- [ ] Images: primary image prominent; gallery for additional images / color variants.
- [ ] Page is indexable by search engines (SSR or SSG); canonical URL uses lure slug.
- [ ] Breadcrumb: Home > [type] > [brand] > [lure name].

### Authentication

**US-04** — As a visitor, I can create an account or sign in using Google or Microsoft so
that I can access personalized features.

*Acceptance criteria:*
- [ ] Google OAuth 2.0 / OIDC sign-in works.
- [ ] Microsoft personal account (MSA) sign-in works.
- [ ] Email + password registration and sign-in works as fallback.
- [ ] On first OAuth sign-in, user is prompted to choose a username (3–20 chars,
  alphanumeric + underscore, unique).
- [ ] A single user identity can link multiple providers (e.g., sign in with Google,
  later link Microsoft).
- [ ] Sign-in state persists across browser sessions (refresh token).
- [ ] Sign-out clears session and redirects to home.

### Favorites

**US-05** — As an authenticated user, I can favorite a lure so that I can quickly revisit
it later.

*Acceptance criteria:*
- [ ] Heart icon visible on lure card and detail page.
- [ ] Toggling favorite is optimistic (UI updates immediately, server confirms async).
- [ ] Unauthenticated tap on favorite icon redirects to sign-in with return URL preserved.
- [ ] "My Favorites" page lists all favorited lures as cards with the same filter/sort
  as the main catalog.
- [ ] Favorite count visible on the lure card is the global count (all users).

### Inventory

**US-06** — As an authenticated user, I can mark a lure as "I own this" so that I can
track my lure collection.

*Acceptance criteria:*
- [ ] "Add to inventory" button on lure detail page and card.
- [ ] When adding, user can specify: quantity (1–99), condition (new / good / used), and
  optional notes (max 200 chars).
- [ ] User can edit quantity, condition, and notes for an inventory entry.
- [ ] User can remove a lure from inventory.
- [ ] If a lure has color variants, user can specify which color(s) they own.
- [ ] "My Inventory" page lists all owned lures grouped by lure type; shows quantity and
  condition per entry.
- [ ] Inventory total count (unique lures owned) visible on user profile.

### User profile

**US-07** — As an authenticated user, I have a public profile page so that my reviews
are attributed to an identity.

*Acceptance criteria:*
- [ ] Public profile shows: username, avatar (from OAuth provider or default), member
  since date, count of favorites, count of inventory entries, and reviews written.
- [ ] No other personal data (email, real name) is visible publicly.
- [ ] User can update display name and avatar from settings.

### Reviews

**US-08** — As an authenticated user, I can rate and review a lure so that other users
benefit from my experience.

*Acceptance criteria:*
- [ ] Rating: 1–5 stars, required.
- [ ] Review text: optional, max 1 000 chars.
- [ ] One review per user per lure (edit allowed; delete allowed).
- [ ] Reviews display: author username, avatar, rating, date, text.
- [ ] Reviews sorted by most recent by default; secondary sort by helpful count.
- [ ] "Mark as helpful" button per review (one vote per user per review, authenticated
  only).
- [ ] Review form accessible from lure detail page only.
- [ ] Reviews are published immediately (no moderation queue in v1); admin can hide via
  backoffice.

---

## Out-of-Scope Clarifications

| Topic | Decision |
|---|---|
| Price monitoring / history | NG4 — future feature. `lure_price_history` table reserved in schema but not exposed. |
| User-uploaded catch photos | NG2 — v2. Schema prepared (`catch_photos` table exists in v2 migration plan). |
| Fishing spots map | NG3 — v2. `lat/lng` columns exist in `fishing_sessions` (v2 table) for forward compatibility. |
| Apple Sign-In | Not in v1. Add when native iOS app ships (v3). |
| Lure comparison tool | Not in v1. Favorites + reviews address the underlying need. |

---

## Constraints and Non-Functional Requirements

**Performance:**
- Catalog listing endpoint: p95 < 200ms (Typesense-backed, with locale filter).
- Lure detail page: LCP < 2.5s on 4G mobile.
- Autocomplete: first suggestion visible < 150ms after keystroke.

**Availability:**
- Target: 99.5% monthly uptime for public catalog (read-only path).
- Auth and write operations: 99% monthly uptime.

**Security:**
- All traffic over HTTPS; HSTS enforced.
- OAuth state parameter validated to prevent CSRF.
- API rate limiting: 100 req/min per IP for unauthenticated, 300 req/min per user for
  authenticated.
- No PII in logs beyond hashed user ID.

**Accessibility:**
- WCAG 2.1 AA for all public pages.
- Filter and search UI keyboard-navigable.

**Localization:**
- PT-PT: 100% coverage before launch.
- EN: 100% coverage before launch (fallback locale).
- ES: 80% coverage acceptable at launch; 100% within 30 days post-launch.

**Data:**
- Minimum 500 published lures with complete mandatory fields at launch (Article II).
- Minimum 50 brands represented.
- Minimum 20 species with PT-PT common names.
