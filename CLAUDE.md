<!-- SPECKIT START -->
Active feature: **003 — Design System do Backoffice Admin**.
Stack: backend **.NET 10 (ASP.NET Core)** + frontend **Next.js 16 (React/TS)**;
PostgreSQL (EF Core), Typesense, Redis, Supabase Auth, Azure (West Europe).
Feature 003 adds **Tailwind CSS v4 + shadcn/ui** to `apps/web`, scoped to the `/admin` subtree
(light theme, white/blue/green); the public frontend is intentionally untouched.
For technologies, project structure, shell commands, and other context, read the current plan:
`specs/003-admin-design-system/plan.md` (with `research.md`, `data-model.md`,
`contracts/design-tokens.md`, `quickstart.md`). Features 001 (`specs/001-lure-catalog-mvp/`) and
002 (`specs/002-admin-indexing-audit/`) remain the baseline. Governing principles:
`.specify/memory/constitution.md`.
<!-- SPECKIT END -->

Versioning: **per feature** (`specs/NNN-*`), not semantic releases or git tags. See
`README.md` → Versionamento. The constitution version and `apps/web/package.json` version are
independent and do NOT represent the product version.
