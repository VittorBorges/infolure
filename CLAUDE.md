<!-- SPECKIT START -->
Active feature: **004 — Design System Partilhado + Storybook**.
Stack: backend **.NET 10 (ASP.NET Core)** + frontend **Next.js 16 (React/TS)**;
PostgreSQL (EF Core), Typesense, Redis, Supabase Auth, Azure (West Europe).
Feature 004 promotes the shadcn/ui components + tokens from feature 003 into a shared monorepo
package **`@infolure/design-system`** (`packages/design-system`, npm workspaces, tsup build,
Storybook). Tokens are the single source of truth; the admin migrates to consume the package, and
the public frontend gains availability + a pilot. Backend is untouched.
For technologies, project structure, shell commands, and other context, read the current plan:
`specs/004-design-system-package/plan.md` (with `research.md`, `data-model.md`,
`contracts/package-api.md`, `quickstart.md`). Features 001–003 remain the baseline. Governing
principles: `.specify/memory/constitution.md`.
<!-- SPECKIT END -->

Versioning: **per feature** (`specs/NNN-*`), not semantic releases or git tags. See
`README.md` → Versionamento. The constitution version and `apps/web/package.json` version are
independent and do NOT represent the product version.
