# Implementation Plan: Lure Catalog MVP

**Branch**: `001-lure-catalog-mvp` | **Date**: 2026-06-13 | **Spec**: [spec.md](spec.md)

**Input**: Feature specification from `specs/001-lure-catalog-mvp/spec.md`

> **Re-plan note (2026-06-13)**: este plano foi reescrito para a stack **.NET (backend) +
> Next.js (frontend)** e realinhado à constituição vigente (`/.specify/memory/constitution.md`,
> v1.1.x — 5 princípios). A versão anterior assumia Node.js/Fastify e uma constituição de
> "Articles I–IX" que não corresponde ao arquivo atual. `data-model.md` e `contracts/api.yaml`
> são agnósticos de stack e foram preservados.

## Summary

Catálogo de iscas de pesca para o mercado ibérico (PT/ES), com descoberta por busca e filtros,
fichas técnicas detalhadas, favoritos, inventário pessoal ("iscas que possuo"), avaliações e
preços curados de retalhistas. PT-PT é o idioma primário; EN e ES secundários.

**Abordagem técnica**: API REST em **ASP.NET Core (.NET 10 LTS)** sobre **PostgreSQL** (via
EF Core/Npgsql), com **Typesense** para busca/autocomplete facetado, **Supabase Auth** como
broker OIDC (Google + Microsoft MSA + email/senha + linking multi-provedor), **Redis** para
rate limiting e cache, e **Azure Blob + Front Door** para imagens. Frontend em **Next.js 15
(App Router, SSR)** para garantir indexabilidade SEO das páginas de catálogo e detalhe. O
contrato entre frontend e backend é um documento **OpenAPI** versionado, do qual os tipos do
frontend são derivados.

## Technical Context

**Language/Version**:
- Backend: C# 13 / **.NET 10 (LTS)** — resolve o `TODO(STACK_DECISION)` da constituição.
- Frontend: TypeScript 5.x / **Next.js 15** (React 19, App Router).

**Primary Dependencies**:
- Backend: ASP.NET Core (Web API, controllers), **Entity Framework Core + Npgsql**, **Serilog**
  (logging estruturado), **StackExchange.Redis**, cliente **Typesense** (.NET), **Azure SDK**
  (Blob Storage), validação via **FluentValidation**, OpenAPI via **Swashbuckle/Microsoft.AspNetCore.OpenApi**.
- Frontend: Next.js 15, React 19, **openapi-typescript** (tipos derivados do contrato),
  `@supabase/ssr` (sessão server-side), TanStack Query (estado de servidor/otimismo).

**Storage**:
- **Azure Database for PostgreSQL Flexible Server** (fonte de verdade) — região West Europe (RGPD).
- **Azure Blob Storage** (imagens de iscas) + Azure Front Door (CDN).
- **Azure Cache for Redis** (rate limiting, sessões, cache de autocomplete).
- **Typesense Cloud** (índice de busca facetada — sincronizado write-through a partir da API).

**Testing**:
- Backend: **xUnit** (unidade) + **WebApplicationFactory** + **Testcontainers** (integração com
  Postgres real) para fluxos de API.
- Frontend: **Vitest** + React Testing Library (componentes/hooks).
- Cruzando a fronteira (Princípio IV): **Playwright** (E2E) cobrindo caminho feliz + erros principais.

**Target Platform**: Azure West Europe. API em **Azure Container Apps** (ou App Service); Next.js em
Azure (Container Apps/Static Web Apps) ou Vercel. Decisão de hosting do frontend em `research.md`.

**Project Type**: Aplicação web (frontend + backend separados).

**Performance Goals** (da spec, NFR):
- Listagem de catálogo: p95 < 200ms (Typesense, com filtro de locale).
- Página de detalhe: LCP < 2.5s em 4G móvel (SSR).
- Autocomplete: primeira sugestão < 150ms após a tecla.

**Constraints**:
- HTTPS + HSTS; OAuth state validado (anti-CSRF).
- Rate limiting: 100 req/min por IP (não autenticado), 300 req/min por utilizador (autenticado).
- Sem PII em logs além do ID de utilizador com hash (Princípio II + NFR segurança).
- WCAG 2.1 AA em páginas públicas; UI de filtro/busca navegável por teclado (Princípio V).
- Residência de dados na UE (RGPD).

**Scale/Scope**: ≥ 500 iscas, ≥ 50 marcas, ≥ 20 espécies no lançamento; ~25 endpoints de API;
~10 ecrãs; 3 locales (PT-PT 100%, EN 100%, ES 80% no lançamento).

**NEEDS CLARIFICATION** (não bloqueantes — ver `research.md` para recomendação):
- US-01: paginação vs. infinite scroll (preferência de UX).
- Hosting do frontend: Azure vs. Vercel.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

Gates derivados de `/.specify/memory/constitution.md` (v1.1.x — 5 princípios).

### Gate I — Simplicidade Primeiro (YAGNI) — NON-NEGOTIABLE
- [x] ≤ 3 projetos top-level? → **Sim**: `apps/api` (backend), `apps/web` (frontend), `infra` (Bicep).
- [x] Sem camadas prematuras? → API começa como **projeto único** (`Infolure.Api`) com organização
  por *vertical slices* (Features/Catalog, Features/Auth, …). Split em Domain/Infrastructure só se
  e quando justificar (registrado em Complexity Tracking).
- [x] Sem future-proofing além do spec? → Colunas forward-compat (`lat/lng`, `deleted_at`,
  `lure_price_history`) existem no schema mas **sem code paths** para features v2.

### Gate II — Observabilidade por Padrão — NON-NEGOTIABLE
- [x] Logging estruturado? → **Serilog** (JSON), com middleware de **correlation id** por requisição.
- [x] Toda fronteira de rede logada (início/fim/resultado + latência)? → Requisições HTTP de entrada,
  e chamadas a Postgres, Typesense, Redis, Blob e Supabase logadas com latência.
- [x] Erros com contexto, sem segredos/PII? → ID de utilizador com hash; sem email/nome em logs.

### Gate III — Contratos Explícitos (Frontend ↔ Backend)
- [x] Contrato versionado antes do consumo? → **OpenAPI** (`contracts/api.yaml`) é a fonte de verdade;
  o backend ASP.NET Core o gera/valida, e os tipos do Next.js são **derivados via openapi-typescript**.
- [x] Breaking changes seguem SemVer + migração? → API versionada em `/v1`; mudanças incompatíveis → `/v2`.

### Gate IV — Qualidade Testável
- [x] Lógica de negócio testada? → xUnit em regras (sync de popularidade, validações, dedupe de review).
- [x] Fluxos frontend↔backend com integração/E2E? → WebApplicationFactory + Testcontainers (API),
  Playwright (E2E) cobrindo caminho feliz + erros principais por user story.

### Gate V — Experiência do Utilizador Consistente
- [x] Estados de loading/empty/error explícitos? → Já exigidos pela spec (US-01 empty state,
  US-02 no-results); aplicados a cada ecrã com I/O.
- [x] Erros compreensíveis (sem stack traces) + acessibilidade? → WCAG 2.1 AA, navegação por teclado.

**Resultado**: todos os gates passam. Itens de complexidade externa justificados abaixo.

## Project Structure

### Documentation (this feature)

```text
specs/001-lure-catalog-mvp/
├── plan.md              # Este arquivo
├── research.md          # Fase 0 — decisões de stack .NET/Next.js
├── data-model.md        # DDL PostgreSQL (preservado — agnóstico de stack)
├── quickstart.md        # Fase 1 — guia de validação ponta-a-ponta
├── contracts/
│   └── api.yaml         # OpenAPI 3.1 (preservado — contrato Princípio III)
└── tasks.md             # Fase 2 (gerado por /speckit-tasks — STALE, regenerar)
```

### Source Code (repository root)

```text
apps/
├── api/                              # Backend ASP.NET Core (.NET 10 LTS)
│   ├── src/
│   │   └── Infolure.Api/             # Host único: endpoints, middleware, DI
│   │       ├── Features/             # Vertical slices
│   │       │   ├── Catalog/          # lures, species, brands, suggest
│   │       │   ├── Auth/             # sync de utilizador, validação de JWT Supabase
│   │       │   ├── Favorites/
│   │       │   ├── Inventory/
│   │       │   └── Reviews/
│   │       ├── Infrastructure/       # EF Core DbContext, Typesense, Redis, Blob clients
│   │       ├── Observability/        # Serilog config + correlation-id middleware
│   │       └── Program.cs
│   └── tests/
│       ├── Infolure.UnitTests/       # xUnit
│       └── Infolure.IntegrationTests/# WebApplicationFactory + Testcontainers
├── web/                              # Frontend Next.js 15 (App Router, TS)
│   ├── app/                          # rotas (SSR catalog/detail, client filters)
│   ├── components/
│   ├── lib/                          # api client (tipos gerados via openapi-typescript)
│   └── tests/                        # Vitest + Playwright (E2E)
└── packages/
    └── api-types/                    # tipos TS derivados de contracts/api.yaml

infra/                                # Bicep (Postgres, Redis, Blob, Front Door, Container Apps)
```

**Structure Decision**: aplicação web com backend e frontend separados. Mantêm-se **3 projetos
top-level** (`apps/api`, `apps/web`, `infra`) para satisfazer o Gate I. O backend é um **único
projeto C#** com organização por vertical slices em vez do split clássico Domain/Infrastructure/Api —
escolha deliberada de simplicidade (YAGNI) para um MVP; refatorar para camadas separadas só quando
o tamanho justificar. `packages/api-types` existe apenas para hospedar os tipos gerados do contrato
(Princípio III) e não é um projeto de runtime.

## Complexity Tracking

> Serviços geridos externos adicionam dependências; justificados por NFRs/spec e pelo Princípio I
> (preferir serviço gerido a construir auth/busca à mão).

| Decisão | Complexidade adicionada | Justificativa / Alternativa simples rejeitada |
|---|---|---|
| Typesense ao lado do Postgres | Média | Busca facetada + autocomplete < 150ms (NFR). SQL `ILIKE` não escala nem ranqueia por relevância. |
| Supabase Auth (broker OIDC) | Baixa | Google + MSA + email/senha + linking multi-provedor + refresh, prontos. Construir isto em ASP.NET Core Identity seria muito mais código (viola YAGNI). Azure AD B2C/Entra está fora por NG6. |
| Redis | Baixa | Rate limiting distribuído (NFR) + cache de autocomplete. Necessário com múltiplas instâncias de API. |
| Next.js (SSR) em vez de SPA | Média | Indexabilidade SEO das páginas de catálogo/detalhe (US-03, G5). Uma SPA pura não atende ao requisito de SSR/SSG. |

## Phase 0 — Outline & Research

**Output**: [research.md](research.md) — resolve as escolhas de stack ao migrar de Node.js para
.NET (ORM, auth com .NET, rate limiting nativo, cliente Typesense .NET, sync write-through,
geração de tipos a partir do OpenAPI) e as duas `NEEDS CLARIFICATION` (paginação vs. infinite
scroll; hosting do frontend), com Decisão / Rationale / Alternativas para cada.

## Phase 1 — Design & Contracts

**Prerequisites**: `research.md` completo.

1. **Data model** → [data-model.md](data-model.md): **preservado**. DDL PostgreSQL é a fonte
   canônica do schema; será materializado via **migrations do EF Core** (code-first mapeado ao
   schema, ou migrations baseadas no DDL existente).
2. **Contracts** → [contracts/api.yaml](contracts/api.yaml): **preservado** (OpenAPI 3.1). É o
   contrato do Princípio III; o backend ASP.NET Core deve servir/validar contra ele e o Next.js
   gera tipos a partir dele. Endpoints cobrem catálogo, auth-sync, favoritos, inventário e reviews.
3. **Quickstart** → [quickstart.md](quickstart.md): guia de validação ponta-a-ponta (subir Postgres
   + Redis + Typesense locais, rodar a API .NET, rodar o Next.js, e os cenários que provam US-01…US-08).
4. **Agent context**: `CLAUDE.md` atualizado entre os marcadores SPECKIT para apontar para este plano.

## Phase 2 — Tasks

`/speckit-tasks` regenera `tasks.md`. **A `tasks.md` atual está obsoleta** (foi gerada para a stack
Node.js/Fastify) e MUST ser regenerada após este re-plano.
