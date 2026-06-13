# Tasks: Lure Catalog MVP

**Feature**: 001-lure-catalog-mvp
**Input**: Design documents from `specs/001-lure-catalog-mvp/`
**Stack**: backend ASP.NET Core (.NET 10 LTS), frontend Next.js 15 (App Router, TS); PostgreSQL
(EF Core/Npgsql), Typesense, Redis, Supabase Auth, Azure (West Europe).

**Tests**: INCLUÍDOS — a constituição (Princípio IV — Qualidade Testável, NON-NEGOTIABLE) exige
testes para lógica de negócio e ao menos um teste de integração/E2E por fluxo que cruza a fronteira
frontend↔backend.

**Organization**: tarefas agrupadas por user story (mapeadas a US-01…US-08 da spec).

## Format: `[ID] [P?] [Story] Description`

- **[P]**: pode rodar em paralelo (arquivos diferentes, sem dependências)
- **[US#]**: user story da spec (US-01 = [US1], …, US-08 = [US8])
- Caminhos relativos à raiz do repositório

## Path Conventions (do plan.md)

- Backend: `apps/api/src/Infolure.Api/` (host único, vertical slices em `Features/`)
- Backend infra: `apps/api/src/Infolure.Api/Infrastructure/`, observabilidade em `Observability/`
- Testes backend: `apps/api/tests/Infolure.UnitTests/`, `apps/api/tests/Infolure.IntegrationTests/`
- Frontend: `apps/web/app/`, `apps/web/components/`, `apps/web/lib/`, testes em `apps/web/tests/`
- Tipos do contrato: `packages/api-types/`
- IaC: `infra/` (Bicep)

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: inicialização do monorepo e ferramentas.

- [X] T001 Criar estrutura do monorepo (`apps/api`, `apps/web`, `packages/api-types`, `infra`) conforme plan.md
- [X] T002 Inicializar projeto ASP.NET Core (.NET 10) Web API em `apps/api/src/Infolure.Api/`
- [X] T003 [P] Inicializar app Next.js 15 (App Router, TypeScript) em `apps/web/`
- [X] T004 [P] Adicionar dependências NuGet (EF Core, Npgsql, Serilog, StackExchange.Redis, Typesense, Azure.Storage.Blobs, FluentValidation, JwtBearer) em `apps/api/src/Infolure.Api/Infolure.Api.csproj`
- [X] T005 [P] Configurar `.editorconfig` + `dotnet format` (backend) e ESLint/Prettier (frontend) na raiz
- [X] T006 [P] Criar `docker-compose.yml` para Postgres + Redis + Typesense locais (conforme quickstart.md)
- [X] T007 [P] Esqueleto de módulos Bicep (Postgres, Redis, Blob, Front Door, Container Apps) em `infra/`

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: infraestrutura central que MUST estar completa antes de qualquer user story.

**⚠️ CRITICAL**: nenhuma user story começa antes desta fase.

- [X] T008 Configurar Serilog (JSON estruturado) + middleware de correlation-id em `apps/api/src/Infolure.Api/Observability/` (Princípio II)
- [X] T009 Configurar `AppDbContext` (EF Core + Npgsql) em `apps/api/src/Infolure.Api/Infrastructure/Persistence/AppDbContext.cs`
- [X] T010 Criar entidades EF Core + migration inicial espelhando o DDL de data-model.md em `apps/api/src/Infolure.Api/Infrastructure/Persistence/Entities/` (catalog, user e content domains)
- [X] T011 [P] Configurar autenticação JWT Bearer validando JWKS do Supabase + políticas `user`/`admin` em `apps/api/src/Infolure.Api/Infrastructure/Auth/`
- [X] T012 [P] Configurar rate limiting nativo do ASP.NET Core com store Redis (100/min IP anônimo, 300/min por utilizador) em `apps/api/src/Infolure.Api/Infrastructure/RateLimiting/`
- [X] T013 [P] Configurar cliente Typesense + bootstrap da coleção `lures` (schema de data-model.md) em `apps/api/src/Infolure.Api/Infrastructure/Search/`
- [X] T014 [P] Configurar tratamento global de erros (ProblemDetails) com contrato de erro amigável em `apps/api/src/Infolure.Api/Infrastructure/Errors/` (Princípio V)
- [X] T015 [P] Configurar `appsettings` + user-secrets / variáveis de ambiente (sem segredos versionados) **+ HSTS e cabeçalhos de segurança (HTTPS forçado)** em `apps/api/src/Infolure.Api/`
- [X] T016 Configurar geração de OpenAPI (Swashbuckle) e validar contra `contracts/api.yaml` em `apps/api/src/Infolure.Api/`
- [X] T017 [P] Gerar tipos TS de `contracts/api.yaml` (openapi-typescript) em `packages/api-types/` + wrapper de cliente em `apps/web/lib/api.ts` (Princípio III)
- [X] T018 [P] Script de seed (20 marcas, 20 espécies com PT, 50 iscas) em `apps/api/src/Infolure.Api/Infrastructure/Seed/`
- [X] T019 [P] Layout base do Next.js, scaffolding de i18n (PT/EN/ES) e primitivos de UI loading/empty/error em `apps/web/app/` e `apps/web/components/ui/` (Princípio V)

**Checkpoint**: fundação pronta — user stories podem começar.

---

## Phase 3: US-01 — Navegar catálogo com filtros (Priority: P1) 🎯 MVP

**Goal**: visitante anônimo navega e filtra o catálogo sem conta.

**Independent Test**: abrir `/iscas`, aplicar filtros (tipo, água, espécie, peso, marca, profundidade);
resultados atualizam sem reload, estado nos query params, empty state com CTA quando 0 resultados.

### Tests
- [X] T020 [P] [US1] Teste de contrato `listLures` (GET /v1/lures) em `apps/api/tests/Infolure.IntegrationTests/Catalog/ListLuresContractTests.cs`
- [X] T021 [P] [US1] Teste de integração filtro+ordenação do catálogo em `apps/api/tests/Infolure.IntegrationTests/Catalog/BrowseCatalogTests.cs`
- [ ] T022 [P] [US1] Teste E2E navegação/filtro em `apps/web/tests/e2e/browse-catalog.spec.ts`

### Implementation
- [X] T023 [US1] Serviço de listagem (query Typesense + facets para opções de filtro + ordenação popularidade/preço/recente) em `apps/api/src/Infolure.Api/Features/Catalog/LureListService.cs`
- [X] T024 [US1] Endpoint `GET /v1/lures` (filtros, paginação `page`/`page_size`, sort) em `apps/api/src/Infolure.Api/Features/Catalog/CatalogEndpoints.cs`
- [X] T025 [P] [US1] Página `/iscas` (SSR shell + filtros client-side) em `apps/web/app/iscas/page.tsx`
- [X] T026 [P] [US1] Painel de filtros com estado sincronizado na URL em `apps/web/components/catalog/FilterPanel.tsx`
- [X] T027 [P] [US1] Controles de ordenação (popularidade/preço/recente) em `apps/web/components/catalog/SortControls.tsx`
- [X] T028 [P] [US1] Componente de card de isca (imagem, marca, tipo, peso, espécies, preço médio, **contagem global de favoritos**) em `apps/web/components/catalog/LureCard.tsx`
- [X] T029 [US1] Paginação + empty state ("limpar filtros") na página de catálogo em `apps/web/app/iscas/page.tsx`

**Checkpoint**: catálogo navegável e filtrável de forma independente.

---

## Phase 4: US-02 — Buscar iscas (Priority: P1)

**Goal**: visitante busca por nome/marca/modelo com autocomplete.

**Independent Test**: digitar ≥ 2 chars; autocomplete < 150ms; resultados por relevância; combina com filtros; no-results state mostra a query.

### Tests
- [X] T030 [P] [US2] Teste de contrato `suggestLures` (GET /v1/lures/suggest) em `apps/api/tests/Infolure.IntegrationTests/Catalog/SuggestContractTests.cs`
- [X] T031 [P] [US2] Teste de integração busca+filtro combinados em `apps/api/tests/Infolure.IntegrationTests/Catalog/SearchTests.cs`

### Implementation
- [X] T032 [US2] Serviço de autocomplete (Typesense instant search, cache Redis) em `apps/api/src/Infolure.Api/Features/Catalog/SuggestService.cs`
- [X] T033 [US2] Endpoint `GET /v1/lures/suggest` em `apps/api/src/Infolure.Api/Features/Catalog/CatalogEndpoints.cs`
- [X] T034 [US2] Suporte ao parâmetro `q` (busca dentro de resultados filtrados; campos de busca = `name_*`, `brand_name`, `model_ref`) em `apps/api/src/Infolure.Api/Features/Catalog/LureListService.cs`
- [X] T035 [P] [US2] Barra de busca com autocomplete (debounce ≥ 200ms) no header em `apps/web/components/search/SearchBar.tsx`
- [X] T036 [P] [US2] No-results state (mostra query, sugere ampliar filtros) em `apps/web/components/catalog/NoResults.tsx`

**Checkpoint**: busca e autocomplete funcionais junto ao catálogo.

---

## Phase 5: US-03 — Página de detalhe da isca (Priority: P1)

**Goal**: visitante vê a ficha técnica completa, indexável por motores de busca.

**Independent Test**: abrir `/iscas/:slug`; ficha completa; HTML renderizado no servidor (SSR); canonical com slug; secção de preços oculta sem dados.

### Tests
- [X] T037 [P] [US3] Teste de contrato `getLure` (GET /v1/lures/{slug}) em `apps/api/tests/Infolure.IntegrationTests/Catalog/GetLureContractTests.cs`
- [ ] T038 [P] [US3] Teste E2E detalhe + SSR/SEO em `apps/web/tests/e2e/lure-detail.spec.ts`

### Implementation
- [X] T039 [US3] Serviço de detalhe (lure + cores + imagens + espécies-alvo + preços de retalhistas + agregado de reviews) em `apps/api/src/Infolure.Api/Features/Catalog/LureDetailService.cs`
- [X] T040 [US3] Endpoint `GET /v1/lures/{slug}` em `apps/api/src/Infolure.Api/Features/Catalog/CatalogEndpoints.cs`
- [X] T041 [US3] Página de detalhe `/iscas/[slug]` SSR (meta/title, Open Graph, structured data `Product`, canonical, breadcrumb) em `apps/web/app/iscas/[slug]/page.tsx`
- [X] T042 [P] [US3] Galeria de imagens + swatches de cores em `apps/web/components/detail/Gallery.tsx`
- [X] T043 [P] [US3] Secção de preços (média 6m, min/max, até 3 retalhistas; oculta se sem dados) em `apps/web/components/detail/PricingSection.tsx`

**Checkpoint**: páginas de detalhe completas e indexáveis. **MVP read-path completo (US-01+US-02+US-03).**

---

## Phase 6: US-04 — Autenticação (Priority: P2)

**Goal**: visitante cria conta / faz login com Google, Microsoft MSA ou email/senha.

**Independent Test**: login Google/MSA/email funciona; primeiro login OAuth pede username único (3–20); sessão persiste; linking de 2º provedor nas settings.

### Tests
- [ ] T044 [P] [US4] Teste de integração `POST /v1/auth/sync` (cria `users`, vincula provider) em `apps/api/tests/Infolure.IntegrationTests/Auth/AuthSyncTests.cs`
- [ ] T045 [P] [US4] Teste E2E login + seleção de username em `apps/web/tests/e2e/auth.spec.ts`

### Implementation
- [ ] T046 [US4] Adicionar `POST /v1/auth/sync` a `contracts/api.yaml` (webhook Supabase → cria utilizador)
- [ ] T047 [US4] Endpoint `POST /v1/auth/sync` + serviço (cria `users`, `user_auth_providers`) em `apps/api/src/Infolure.Api/Features/Auth/AuthSyncService.cs`
- [ ] T048 [US4] Integração Supabase Auth no Next.js (sessão server-side via `@supabase/ssr`, **validação do parâmetro `state` OAuth — anti-CSRF**) em `apps/web/lib/auth.ts`
- [ ] T049 [P] [US4] Fluxo de sign-in Google + Microsoft MSA em `apps/web/app/(auth)/login/page.tsx`
- [ ] T050 [P] [US4] Fluxo email + senha (registo, login, reset) em `apps/web/app/(auth)/`
- [ ] T051 [US4] Ecrã de seleção de username no primeiro login OAuth em `apps/web/app/(auth)/escolher-username/page.tsx`
- [ ] T052 [P] [US4] Componente de linking multi-provedor em `apps/web/components/settings/AuthProviders.tsx` (consumido pela página de settings, T076)

**Checkpoint**: autenticação completa — habilita features personalizadas (US-05…US-08).

---

## Phase 7: US-05 — Favoritos (Priority: P3)

**Goal**: utilizador autenticado favorita iscas.

**Independent Test**: toggle otimista no card/detalhe; anônimo redireciona p/ login com return URL; "Meus Favoritos" lista com mesmo filtro/sort do catálogo; contagem global no card.

### Tests
- [ ] T053 [P] [US5] Testes de contrato favoritos (list/add/remove) em `apps/api/tests/Infolure.IntegrationTests/Favorites/FavoritesContractTests.cs`
- [ ] T054 [P] [US5] Teste E2E favoritar/desfavoritar em `apps/web/tests/e2e/favorites.spec.ts`

### Implementation
- [ ] T055 [US5] Serviço de favoritos (+ recálculo de `popularity_score`) em `apps/api/src/Infolure.Api/Features/Favorites/FavoritesService.cs`
- [ ] T056 [US5] Endpoints `GET /v1/me/favorites`, `POST`/`DELETE /v1/me/favorites/{lureId}` em `apps/api/src/Infolure.Api/Features/Favorites/FavoritesEndpoints.cs`
- [ ] T057 [US5] Reindex Typesense ao mudar favorito (popularity) em `apps/api/src/Infolure.Api/Features/Favorites/FavoritesService.cs`
- [ ] T058 [P] [US5] Botão de favorito otimista (auth-gated, redirect com return URL) em `apps/web/components/catalog/FavoriteButton.tsx`
- [ ] T059 [P] [US5] Página `/conta/favoritos` em `apps/web/app/conta/favoritos/page.tsx`

**Checkpoint**: favoritos funcionais.

---

## Phase 8: US-06 — Inventário (Priority: P3)

**Goal**: utilizador marca iscas como "possuo" com quantidade/condição/notas.

**Independent Test**: adicionar com quantidade (1–99)/condição/notas; editar; remover; especificar cores; "Meu Inventário" agrupado por tipo; contagem no perfil.

### Tests
- [ ] T060 [P] [US6] Testes de contrato inventário (list/add/update/delete) em `apps/api/tests/Infolure.IntegrationTests/Inventory/InventoryContractTests.cs`
- [ ] T061 [P] [US6] Teste E2E inventário em `apps/web/tests/e2e/inventory.spec.ts`

### Implementation
- [ ] T062 [US6] Serviço de inventário (validações de quantidade/condição/notas ≤ 200) em `apps/api/src/Infolure.Api/Features/Inventory/InventoryService.cs`
- [ ] T063 [US6] Endpoints `GET`/`POST /v1/me/inventory`, `PATCH`/`DELETE /v1/me/inventory/{entryId}` em `apps/api/src/Infolure.Api/Features/Inventory/InventoryEndpoints.cs`
- [ ] T064 [P] [US6] Modal "Adicionar ao inventário" (qty/condição/cor/notas) em `apps/web/components/inventory/AddToInventoryModal.tsx`
- [ ] T065 [P] [US6] Página `/conta/inventario` agrupada por tipo em `apps/web/app/conta/inventario/page.tsx`

**Checkpoint**: inventário funcional.

---

## Phase 9: US-08 — Avaliações (Priority: P4)

**Goal**: utilizador autenticado avalia (1–5 estrelas) e comenta iscas.

**Independent Test**: rating obrigatório, texto ≤ 1000 chars; uma review por user/isca (edit/delete); ordenadas por recente; "útil" 1 voto/user.

### Tests
- [ ] T066 [P] [US8] Testes de contrato reviews (list/create) em `apps/api/tests/Infolure.IntegrationTests/Reviews/ReviewsContractTests.cs`
- [ ] T067 [P] [US8] Teste de integração regra "uma review por user/isca" + agregado em `apps/api/tests/Infolure.UnitTests/Reviews/ReviewRulesTests.cs`

### Implementation
- [ ] T068 [US8] Adicionar `PATCH`/`DELETE /v1/lures/{slug}/reviews/{reviewId}` e `POST /v1/reviews/{reviewId}/helpful` a `contracts/api.yaml`
- [ ] T069 [US8] Serviço de reviews (uma por user/isca, agregado avg+distribuição, voto útil) em `apps/api/src/Infolure.Api/Features/Reviews/ReviewsService.cs`
- [ ] T070 [US8] Endpoints reviews (list/create/edit/delete/helpful) em `apps/api/src/Infolure.Api/Features/Reviews/ReviewsEndpoints.cs`
- [ ] T071 [P] [US8] Lista + formulário de review na página de detalhe em `apps/web/components/detail/Reviews.tsx`
- [ ] T072 [P] [US8] Rating agregado (média + distribuição) no detalhe em `apps/web/components/detail/RatingSummary.tsx`

**Checkpoint**: avaliações funcionais.

---

## Phase 10: US-07 — Perfil de utilizador (Priority: P4)

**Goal**: perfil público + settings; sem PII pública.

**Independent Test**: `/u/:username` mostra username, avatar, membro desde, contagens; sem email/nome real; settings atualizam nome/avatar.

### Tests
- [ ] T073 [P] [US7] Teste de integração perfil público (sem PII) em `apps/api/tests/Infolure.IntegrationTests/Users/ProfileTests.cs`

### Implementation
- [ ] T074 [US7] Adicionar `GET /v1/users/{username}` a `contracts/api.yaml` + endpoint/serviço em `apps/api/src/Infolure.Api/Features/Users/ProfileService.cs`
- [ ] T075 [P] [US7] Página de perfil público `/u/[username]` em `apps/web/app/u/[username]/page.tsx`
- [ ] T076 [P] [US7] Página de settings (nome, avatar) que **compõe** o componente `AuthProviders` (T052) em `apps/web/app/conta/settings/page.tsx`
- [ ] T077 [US7] Fluxo RGPD "Apagar conta" (soft-delete `deleted_at`, nulificar PII, email de confirmação) em `apps/api/src/Infolure.Api/Features/Users/AccountDeletionService.cs`

**Checkpoint**: todas as user stories funcionais de forma independente.

---

## Phase 11: Polish & Cross-Cutting Concerns

**Purpose**: melhorias que afetam múltiplas stories, backoffice e prontidão para lançamento.

### Backoffice (admin, `role = 'admin'`)
- [ ] T078 [P] CRUD de iscas com validação dos campos obrigatórios em `apps/web/app/(admin)/iscas/` + endpoints admin em `apps/api/src/Infolure.Api/Features/Admin/`
- [ ] T079 [P] Gestão de marcas e espécies (admin) em `apps/web/app/(admin)/`
- [ ] T080 [P] Gestão de `lure_retailer_prices` (recalcula `price_6m_*` ao salvar) em `apps/api/src/Infolure.Api/Features/Admin/RetailerPriceService.cs`
- [ ] T081 [P] Moderação de reviews (hide/show) em `apps/web/app/(admin)/reviews/`

### Cross-cutting
- [ ] T082 Job noturno de `popularity_score` (favorites + inventory) + reindex Typesense em `apps/api/src/Infolure.Api/Infrastructure/Jobs/PopularityJob.cs`
- [ ] T083 [P] i18n: PT-PT 100%, EN 100%, ES 80% em `apps/web/messages/` + revisão por nativo PT-PT
- [ ] T084 [P] Cookie banner (consentimento só para analytics) em `apps/web/components/CookieBanner.tsx`
- [ ] T085 Lighthouse CI (LCP < 2.5s, CLS < 0.1) em catálogo e detalhe + auditoria WCAG 2.1 AA
- [ ] T086 Load test (200 utilizadores concorrentes, p95 API < 200ms) + validação de latência Typesense p95 < 80ms
- [ ] T087 Security review (OWASP Top 10, validação de rate limiting, fluxo de auth, HSTS)
- [ ] T088 Provisionar infra Azure via Bicep (Postgres, Redis, Blob, Front Door, Container Apps) em `infra/` + pipelines GitHub Actions
- [ ] T089 Popular catálogo de produção (≥ 500 iscas, ≥ 50 marcas, ≥ 20 espécies) e checklist de cutover staging→produção
- [ ] T090 Executar validação completa do `quickstart.md`

---

## Dependencies & Execution Order

### Phase Dependencies
- **Setup (Phase 1)**: sem dependências.
- **Foundational (Phase 2)**: depende do Setup — **bloqueia todas as user stories**.
- **US-01/02/03 (Phases 3–5, P1)**: dependem da Foundational. Formam o MVP read-path. US-02 e US-03 reutilizam o serviço/endpoints de catálogo de US-01.
- **US-04 (Phase 6, P2)**: depende da Foundational. **Pré-requisito de US-05, US-06, US-08, US-07** (features autenticadas).
- **US-05, US-06 (Phases 7–8, P3)**: dependem de US-04 (auth) e do read-path (cards/detalhe).
- **US-08 (Phase 9, P4)**: depende de US-04 e US-03 (página de detalhe).
- **US-07 (Phase 10, P4)**: depende de US-04; contagens dependem de US-05/US-06.
- **Polish (Phase 11)**: depende das stories desejadas estarem completas.

### Within Each User Story
- Testes (de contrato/integração) escritos e a FALHAR antes da implementação (Princípio IV).
- Entidades/migrations (Foundational) → serviços → endpoints → UI → integração.

### Parallel Opportunities
- Setup: T003–T007 em paralelo após T001/T002.
- Foundational: T011–T015, T017–T019 em paralelo (arquivos distintos) após T008–T010.
- Após a Foundational: o read-path (US-01→02→03) pode ser feito por uma equipa enquanto Auth (US-04) por outra.
- Concluída US-04, US-05/US-06 podem correr em paralelo; US-08 e US-07 em paralelo.
- Dentro de cada story, tarefas marcadas [P] (componentes/arquivos distintos) correm juntas.

---

## Parallel Example: US-01 (catálogo)

```bash
# Testes da US-01 juntos:
Task: "T020 Teste de contrato listLures em .../Catalog/ListLuresContractTests.cs"
Task: "T021 Teste de integração filtro+ordenação em .../Catalog/BrowseCatalogTests.cs"
Task: "T022 Teste E2E navegação/filtro em apps/web/tests/e2e/browse-catalog.spec.ts"

# Componentes de UI da US-01 juntos:
Task: "T026 FilterPanel.tsx"
Task: "T027 SortControls.tsx"
Task: "T028 LureCard.tsx"
```

---

## Implementation Strategy

### MVP First (read-path: US-01 + US-02 + US-03)
1. Phase 1 (Setup) → Phase 2 (Foundational).
2. Phases 3–5 (catálogo, busca, detalhe) — **catálogo público navegável e indexável**.
3. **PARAR e VALIDAR**: testar o read-path de forma independente; deploy/demo (MVP).

### Incremental Delivery
1. Setup + Foundational → fundação pronta.
2. + US-01/02/03 → MVP público (sem login).
3. + US-04 (auth) → habilita personalização.
4. + US-05 (favoritos) → demo.
5. + US-06 (inventário) → demo.
6. + US-08 (reviews) e US-07 (perfil) → demo.
7. Polish + backoffice + launch readiness.

---

## Notes
- [P] = arquivos diferentes, sem dependências.
- [US#] mapeia a tarefa à user story da spec (US-01…US-08) para rastreabilidade.
- Verificar que os testes falham antes de implementar (Princípio IV).
- `contracts/api.yaml` é a fonte de verdade (Princípio III): endpoints novos (auth/sync, edição/delete de review, voto útil, perfil público) MUST ser adicionados ao contrato **antes** da implementação — tarefas T046, T068, T074.
- Commit atômico por tarefa ou grupo lógico.
- Total: **90 tarefas**.
