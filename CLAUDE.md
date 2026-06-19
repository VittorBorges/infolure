<!-- SPECKIT START -->
Active feature: **007 — Sessão do utilizador no painel admin**.
Stack: backend **.NET 10 (ASP.NET Core)** + frontend **Next.js 16 (React/TS)**;
PostgreSQL (EF Core), Typesense, Redis, Supabase Auth, Azure (West Europe).
Feature 007 (independent of 006): the `/admin` panel persistently shows the **authenticated user's
identity** (name or email + role) and a **logout button** (Supabase `signOut` → `/login`). Adds a
read-only **`GET /v1/me`** endpoint (resolved by the JWT `sub` claim; never exposes the UUID),
consumed server-side by the admin layout; reuses existing auth/route-guard; **no schema changes**.
Read the plan: `specs/007-admin-user-session/plan.md` (with `research.md`, `data-model.md`,
`contracts/me-api.yaml`, `quickstart.md`). Features 001–006 are the baseline. Governing
principles: `.specify/memory/constitution.md`.
<!-- SPECKIT END -->

Versioning: **per feature** (`specs/NNN-*`), not semantic releases or git tags. See
`README.md` → Versionamento. The constitution version and `apps/web/package.json` version are
independent and do NOT represent the product version.
