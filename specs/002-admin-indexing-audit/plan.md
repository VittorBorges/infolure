# Implementation Plan: Painel de Administração, Controlo de Indexação e Base Auditável

**Branch**: `002-admin-indexing-audit` | **Date**: 2026-06-15 | **Spec**: [spec.md](spec.md)

**Input**: Feature specification from `specs/002-admin-indexing-audit/spec.md`

## Summary

Três pilares sobre a base da Feature 001:

1. **Base auditável transversal (P1, fundação)** — todas as ~16 entidades passam a expor `IsActive`,
   `Source` (manual|automation|import) e `DeletedAt` (soft-delete), via interface `IAuditable` + um
   **global query filter** do EF Core (`DeletedAt == null`) e um **SaveChanges interceptor** que
   carimba timestamps, define a origem e converte `Remove` em soft-delete. A visibilidade pública
   passa a respeitar o estado da entidade e do **pai verdadeiro** (marca→isca), mas não da relação
   fraca isca↔espécie.
2. **Backoffice de administração (P1)** — endpoints `/v1/admin/*` com CRUD completo, listagem
   filtrável/paginada (com inclusão opcional de inativos/eliminados), soft-delete/restore, toggle
   active e um endpoint de **dashboard**. Frontend novo em `apps/web/app/admin`, protegido por role
   admin. CRUD abrange dados pessoais (contas, favoritos, inventário) com aviso RGPD e auditoria.
3. **Controlo de indexação (P2)** — tabela singleton `app_settings` (`seo_indexing_enabled`) +
   `is_indexable` por isca, com cache Redis (TTL ≤ 60s, invalidação na escrita). `app/robots.ts`,
   `app/sitemap.ts` e `generateMetadata` do detalhe passam a refletir o estado em runtime.

Transversal: **registo de auditoria** (`admin_audit_log`) das ações de escrita, com instantâneo
antes→depois apenas para operações sobre dados pessoais; e reforço de autenticação para que
**utilizadores inativos/eliminados não autentiquem** e percam sessão de imediato.

## Technical Context

**Language/Version**:
- Backend: C# 13 / **.NET 10 (LTS)** (mantém-se da Feature 001).
- Frontend: TypeScript 5.x / **Next.js 16** (App Router). ⚠️ Esta versão tem breaking changes — ver
  `apps/web/AGENTS.md`: **ler `node_modules/next/dist/docs/` antes de codar** (ex.: convenção
  `middleware`→`proxy` já sinalizada como deprecada no arranque do dev).

**Primary Dependencies** (reutilizadas, sem novas dependências externas — Princípio I):
- Backend: EF Core + Npgsql (global query filters, interceptors), Serilog, StackExchange.Redis,
  cliente Typesense .NET, autorização nativa do ASP.NET Core.
- Frontend: Next.js App Router (`robots.ts`/`sitemap.ts` dinâmicos), `@supabase/ssr`, tipos
  derivados do contrato via `openapi-typescript`.

**Storage**: PostgreSQL (EF Core) — novas colunas auditáveis + 2 tabelas (`app_settings`,
`admin_audit_log`). Redis para cache do flag de indexação e do estado ativo do utilizador.

**Testing**: xUnit + WebApplicationFactory (integração, serviços Docker locais — padrão da 001);
Playwright (E2E admin + indexação). Foco em **regressão** após o query filter global (SC-009).

**Target Platform**: Azure West Europe (inalterado).

**Project Type**: Aplicação web (backend `apps/api` + frontend `apps/web`).

**Performance Goals**:
- Alteração do flag de indexação reflete-se nas instruções de rastreio em **< 60s** (SC-005).
- O check de utilizador ativo por requisição NÃO deve adicionar uma ida à BD por pedido → cache Redis.

**Constraints**:
- O global query filter altera **todas** as queries existentes → testes de regressão obrigatórios.
- Sem PII nos logs (Princípio II); o instantâneo antes→depois de dados pessoais vive no
  `admin_audit_log` (acesso restrito), não nos logs estruturados.
- Painel restrito a role admin, verificado no backend (fonte de verdade na BD) e no frontend.

**Scale/Scope**: ~16 entidades migradas; ~20–25 novos endpoints admin; ~8–12 ecrãs de backoffice;
2 tabelas novas; painel em PT-PT (i18n do backoffice fora de âmbito).

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

Gates derivados de `.specify/memory/constitution.md` (v1.1.1 — 5 princípios).

### Gate I — Simplicidade Primeiro (YAGNI) — NON-NEGOTIABLE
- [x] Sem novos projetos top-level (`apps/api`, `apps/web`, `infra` mantêm-se).
- [x] Sem novas dependências externas — reutiliza EF Core, Redis, Serilog, ASP.NET Core auth.
- [x] Base auditável aplicada por **convenção** (interface + loop no `OnModelCreating` + interceptor),
  não por entidade à mão. Complexidade do query filter global e do CRUD genérico justificada em
  Complexity Tracking.

### Gate II — Observabilidade por Padrão — NON-NEGOTIABLE
- [x] Ações de escrita do admin registadas (correlation-id já existente) + `admin_audit_log` dedicado.
- [x] Fronteiras de rede (Redis, Postgres, Typesense) já logadas; novas leituras de `app_settings`
  herdam o mesmo logging.
- [x] Sem PII em logs; instantâneos de dados pessoais ficam no audit log restrito, não em logs.

### Gate III — Contratos Explícitos (Frontend ↔ Backend)
- [x] Novos endpoints descritos em [contracts/admin-api.yaml](contracts/admin-api.yaml) (OpenAPI 3.1,
  `/v1/admin/*` + `/v1/seo/*`); tipos do frontend derivados via `openapi-typescript`.
- [x] Mantém versionamento `/v1`; aditivo, sem breaking changes nos contratos públicos da 001.

### Gate IV — Qualidade Testável
- [x] Regras de negócio testadas (cascata de visibilidade, bloqueio do último admin, soft-delete).
- [x] Integração: query filter exclui eliminados em todas as superfícies (SC-002/SC-009); CRUD admin;
  toggle de indexação reflete em robots/sitemap; utilizador inativo recebe 401/403.
- [x] A suite verde da 001 (15 integração + 6 E2E) é a rede de regressão.

### Gate V — Experiência do Utilizador Consistente
- [x] Backoffice com estados de loading/empty/error explícitos por ecrã com I/O.
- [x] Avisos RGPD compreensíveis antes de operações sobre dados pessoais; erros sem stack traces.

**Resultado**: todos os gates passam. Complexidade justificada abaixo.

## Project Structure

### Documentation (this feature)

```text
specs/002-admin-indexing-audit/
├── plan.md              # Este arquivo
├── research.md          # Fase 0 — decisões técnicas (auditável, auth, indexação)
├── data-model.md        # Fase 1 — colunas auditáveis + app_settings + admin_audit_log
├── quickstart.md        # Fase 1 — guia de validação ponta-a-ponta
├── contracts/
│   └── admin-api.yaml   # OpenAPI 3.1 — /v1/admin/* + /v1/seo/*
└── tasks.md             # Fase 2 (gerado por /speckit-tasks — NÃO criado aqui)
```

### Source Code (repository root)

```text
apps/api/src/Infolure.Api/
├── Infrastructure/
│   ├── Persistence/
│   │   ├── Auditing/               # NOVO: IAuditable, AuditSaveChangesInterceptor, QueryFilter helpers
│   │   ├── Entities/               # +IAuditable em todas; +AppSetting, +AdminAuditEntry
│   │   ├── AppDbContext.cs         # aplica query filters por convenção + regista interceptor
│   │   └── Migrations/             # NOVA migration: colunas auditáveis + 2 tabelas + backfill
│   └── Auth/
│       ├── AuthExtensions.cs       # AdminPolicy passa a basear-se em role da BD
│       └── ActiveUserMiddleware.cs # NOVO: rejeita utilizador inativo/eliminado (cache Redis)
├── Features/
│   ├── Admin/                      # EXPANDIDO: CRUD genérico por entidade, dashboard, audit query
│   │   ├── AdminController.cs      # (existente) + recursos novos
│   │   ├── DashboardService.cs     # NOVO: métricas de cadastros/estados
│   │   └── AuditService.cs         # NOVO: escrita/consulta de admin_audit_log
│   ├── Seo/                        # NOVO: GET /v1/seo (flag global + dados de sitemap)
│   └── Catalog/                    # ajustar visibilidade (pai + is_indexable)
└── Infrastructure/Search/LureIndexer.cs   # respeitar IsActive/DeletedAt/is_indexable

apps/web/
├── app/
│   ├── admin/                      # NOVO: dashboard + CRUD por entidade (role-gated)
│   ├── robots.ts                   # NOVO: dinâmico (flag global)
│   ├── sitemap.ts                  # NOVO: dinâmico (iscas published+active+indexable)
│   └── iscas/[slug]/page.tsx       # generateMetadata → robots noindex conforme flag/per-isca
├── middleware.ts (ou proxy)        # gate de role admin para /admin (ver AGENTS.md / docs Next)
└── components/admin/               # data-table + form genéricos
```

**Structure Decision**: mantém os 3 projetos top-level da Feature 001. O backoffice vive como mais
um conjunto de vertical slices em `Features/Admin` (+ `Features/Seo`), e a base auditável concentra-se
em `Infrastructure/Persistence/Auditing` aplicada por convenção, evitando edição entidade-a-entidade.

## Complexity Tracking

| Decisão | Complexidade adicionada | Justificativa / Alternativa simples rejeitada |
|---|---|---|
| Global query filter (soft-delete) | Média | Garante SC-002 (0 eliminados em superfícies públicas) sem repetir `Where(!Deleted)` em dezenas de queries — repetir manualmente é frágil e o que a spec quer evitar. |
| SaveChanges interceptor (audit + soft-delete) | Baixa | Centraliza timestamps/origem/soft-delete; alternativa (carimbar em cada serviço) viola DRY e falha em silêncio. |
| Middleware de utilizador ativo + cache | Média | FR-013a exige bloqueio imediato; o backend só valida o JWT Supabase (stateless). Sem este check, "desativar utilizador" não tem efeito. Cache Redis evita ida à BD por requisição. |
| AdminPolicy baseada na BD (não no claim do JWT) | Baixa | A role vive na BD (`users.role`); depender do claim do JWT atrasa alterações de role e duplica a verdade. Reaproveita o middleware que já carrega o utilizador. |
| CRUD admin "genérico" | Média | ~16 entidades; um padrão data-table+form genérico no front e endpoints uniformes reduz código repetido. Não se cria um framework — só um padrão partilhado. |

## Phase 0 — Outline & Research

**Output**: [research.md](research.md) — resolve: (1) padrão de soft-delete/auditoria no EF Core
(.NET 10) com query filters por convenção e interceptor; (2) tratamento do aviso de query filter em
navegações requeridas; (3) enforcement de utilizador ativo por requisição com cache e invalidação;
(4) origem da role admin (BD vs claim JWT); (5) `robots.ts`/`sitemap.ts` dinâmicos em Next 16 e cache
do flag global para cumprir o SC-005 (< 60s); (6) backfill da migration sem perda de dados (SC-008).

## Phase 1 — Design & Contracts

**Prerequisites**: `research.md` completo.

1. **Data model** → [data-model.md](data-model.md): colunas auditáveis (`is_active`, `source`,
   `deleted_at`, e `created_at`/`updated_at` onde faltam) em todas as tabelas; `app_settings`
   (singleton) com `seo_indexing_enabled`; `lures.is_indexable`; `admin_audit_log`. Inclui DDL de
   migração + estratégia de backfill (ativos, não-eliminados, origem por proveniência).
2. **Contracts** → [contracts/admin-api.yaml](contracts/admin-api.yaml): OpenAPI 3.1 dos endpoints
   `/v1/admin/*` (CRUD por entidade, list filtrável/paginada, soft-delete/restore, toggle active,
   dashboard, audit) e `/v1/seo/*` (flag de indexação + dados de sitemap).
3. **Quickstart** → [quickstart.md](quickstart.md): cenários que provam US-01…US-04 (desativar/
   eliminar/restaurar; dashboard + CRUD; toggle de indexação refletido em robots/sitemap/meta;
   auditoria).
4. **Agent context**: `CLAUDE.md` atualizado entre os marcadores SPECKIT para apontar para este plano.

## Phase 2 — Tasks

`/speckit-tasks` gera `tasks.md` (não criado por este comando). Ordem natural: Pilar 0 (base
auditável + migration + regressão) → Pilar B backend (CRUD/dashboard/audit) → Pilar A (indexação) →
Pilar B frontend (backoffice) → E2E.
