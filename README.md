# infolure

Catálogo de iscas de pesca para o mercado ibérico (PT/ES): descoberta por busca e filtros, fichas
técnicas, favoritos, inventário pessoal, perfis e avaliações.

**Stack**: backend .NET 10 (ASP.NET Core) + frontend Next.js (React/TS); PostgreSQL (EF Core),
Typesense, Redis, Supabase Auth, Azure (West Europe).

## Versionamento

Este projeto é versionado **por feature** (fluxo Spec Kit), **não** por número de release semântico
nem por tags de git. Cada incremento de produto é uma feature numerada em `specs/NNN-<nome>/`
(com `spec.md` → `plan.md` → `tasks.md`), e o histórico de produto vive nessas pastas e no git.

Features:

- **001 — Lure Catalog MVP** — catálogo, busca, favoritos, inventário, perfis e reviews (US-01…US-08).
- **002 — Admin, Indexação e Base Auditável** — backoffice (dashboard + CRUD), controlo de indexação
  e base auditável (ativo/origem/soft-delete) com registo de auditoria.

> As seguintes versões são **independentes** desta numeração e não representam a versão do produto:
> a versão da constituição (`.specify/memory/constitution.md`, SemVer de governança) e a versão do
> pacote do frontend (`apps/web/package.json`). Não usamos `CHANGELOG` nem tags de release.
