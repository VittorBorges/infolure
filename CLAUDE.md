<!-- SPECKIT START -->
Active feature: **005 — Formulário de Registo e Edição de Iscas**.
Stack: backend **.NET 10 (ASP.NET Core)** + frontend **Next.js 16 (React/TS)**;
PostgreSQL (EF Core), Typesense, Redis, Supabase Auth, Azure (West Europe).
Feature 005 adds a backoffice admin form to register/edit lures with all properties. It evolves the
model: a lure gains a **list of sizes** (label + length_mm + weight_g), each **color** gains an
**open list of hex codes** (each hex optionally labeled with a base color), replacing the fixed
`hex_primary`/`hex_secondary` pair, and each color gets an **optional photo** (Azure Blob upload).
Backend extends the admin write endpoints (`POST`/`PUT /v1/admin/lures`) transactionally; the public
search keeps reading a denormalized representative size. No data migration (catalog has no real
colors/sizes yet).
For technologies, project structure, shell commands, and other context, read the current plan:
`specs/005-lure-registration-form/plan.md` (with `research.md`, `data-model.md`,
`contracts/admin-lures-api.yaml`, `quickstart.md`). Features 001–004 remain the baseline. Governing
principles: `.specify/memory/constitution.md`.
<!-- SPECKIT END -->

Versioning: **per feature** (`specs/NNN-*`), not semantic releases or git tags. See
`README.md` → Versionamento. The constitution version and `apps/web/package.json` version are
independent and do NOT represent the product version.
