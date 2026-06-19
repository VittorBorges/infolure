<!-- SPECKIT START -->
Active feature: **006 — Melhorias ao Formulário de Iscas**.
Stack: backend **.NET 10 (ASP.NET Core)** + frontend **Next.js 16 (React/TS)**;
PostgreSQL (EF Core), Typesense, Redis, Supabase Auth, Azure (West Europe).
Feature 006 builds on 005: (US1) remove per-lure SEO `is_indexable` → single **global** indexing
toggle in the admin panel; (US2) **brand CRUD** in the backoffice; (US3) select a lure's brand via
**name autocomplete** (no UUID); (US4) **rename** "tamanho da isca" → **"configuração da isca"**
(`LureSize`→`LureConfiguration`, `lure_sizes`→`lure_configurations`) everywhere and move **hook**
data (size/count/type) to each configuration; (US5) each color gets **multiple photos** and the
**>1 MB upload bug** is fixed (Next.js server-actions body limit → 5 MB); (US6) **species CRUD** in the
backoffice + select a lure's **target species by name autocomplete** (multi-select, optional
confidence). Also FR-007a: configuration **weight is now optional** (`weight_g` nullable). Read the plan:
`specs/006-lure-form-enhancements/plan.md` (with `research.md`, `data-model.md`,
`contracts/admin-api-delta.yaml`, `quickstart.md`). Features 001–005 remain the baseline. Governing
principles: `.specify/memory/constitution.md`.
<!-- SPECKIT END -->

Versioning: **per feature** (`specs/NNN-*`), not semantic releases or git tags. See
`README.md` → Versionamento. The constitution version and `apps/web/package.json` version are
independent and do NOT represent the product version.
