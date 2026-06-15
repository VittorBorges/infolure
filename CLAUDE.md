<!-- SPECKIT START -->
Active feature: **002 — Admin, Indexação e Base Auditável**.
Stack: backend **.NET 10 (ASP.NET Core)** + frontend **Next.js 16 (React/TS)**;
PostgreSQL (EF Core), Typesense, Redis, Supabase Auth, Azure (West Europe).
For technologies, project structure, shell commands, and other context, read the current plan:
`specs/002-admin-indexing-audit/plan.md` (with `research.md`, `data-model.md`,
`contracts/admin-api.yaml`, `quickstart.md`). Feature 001 (`specs/001-lure-catalog-mvp/`) remains the
baseline. Governing principles: `.specify/memory/constitution.md`.
<!-- SPECKIT END -->

Versioning: **per feature** (`specs/NNN-*`), not semantic releases or git tags. See
`README.md` → Versionamento. The constitution version and `apps/web/package.json` version are
independent and do NOT represent the product version.
