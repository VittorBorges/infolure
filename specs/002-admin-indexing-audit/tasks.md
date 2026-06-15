---
description: "Task list — Feature 002: Admin, Indexação e Base Auditável"
---

# Tasks: Painel de Administração, Controlo de Indexação e Base Auditável

**Input**: Design documents from `specs/002-admin-indexing-audit/`

**Prerequisites**: [plan.md](plan.md), [spec.md](spec.md), [research.md](research.md),
[data-model.md](data-model.md), [contracts/admin-api.yaml](contracts/admin-api.yaml)

**Tests**: Incluídos — a Constituição (Princípio IV / Gate IV) e os critérios SC-002/SC-007/SC-009 +
[quickstart.md](quickstart.md) exigem testes de integração, regressão e E2E.

**Organization**: Tarefas agrupadas por user story (US-01…US-04) para implementação e teste
independentes. Caminhos: backend `apps/api/src/Infolure.Api/`, testes
`apps/api/tests/Infolure.IntegrationTests/`, frontend `apps/web/`.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Pode correr em paralelo (ficheiros diferentes, sem dependências por concluir)
- **[Story]**: US1=ciclo de vida, US2=backoffice, US3=indexação, US4=auditoria

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Preparar ambiente, tipos e scaffolding de testes.

- [x] T001 [P] Ler e registar as APIs do Next.js 16 para `robots`/`sitemap`/`generateMetadata` e a convenção `middleware`→`proxy` a partir de `apps/web/node_modules/next/dist/docs/`, anexando notas em [research.md](research.md) (§5)
- [x] T002 [P] Adicionar script `gen:admin-api-types` em `apps/web/package.json` gerando `apps/web/lib/admin-api-types.ts` a partir de `specs/002-admin-indexing-audit/contracts/admin-api.yaml`
- [x] T003 [P] Criar pastas de teste `apps/api/tests/Infolure.IntegrationTests/Lifecycle/`, `.../Admin/` (existe), `.../Seo/` e `apps/web/tests/e2e/` (specs admin/indexação)

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Base auditável transversal + autenticação admin. **Bloqueia todas as user stories.**

**⚠️ CRITICAL**: Nenhuma user story começa antes desta fase estar completa e a suite a verde.

- [x] T004 Criar interface `IAuditable` (IsActive, Source, DeletedAt, CreatedAt, UpdatedAt) em `apps/api/src/Infolure.Api/Infrastructure/Persistence/Auditing/IAuditable.cs`
- [x] T005 Implementar `IAuditable` em todas as entidades de domínio em `apps/api/src/Infolure.Api/Infrastructure/Persistence/Entities/Catalog.cs`, `Users.cs`, `Content.cs` (reutilizar `User.DeletedAt` existente)
- [x] T006 Implementar `AuditSaveChangesInterceptor` (carimba CreatedAt/UpdatedAt, default Source=`manual`, converte `Deleted`→soft-delete `DeletedAt=now`) em `apps/api/src/Infolure.Api/Infrastructure/Persistence/Auditing/AuditSaveChangesInterceptor.cs`
- [x] T007 Aplicar global query filter `DeletedAt == null` por convenção (loop em `Model.GetEntityTypes()`) e registar o interceptor em `apps/api/src/Infolure.Api/Infrastructure/Persistence/AppDbContext.cs` + registo de DI em `Program.cs`
- [x] T008 Criar entidades `AppSetting` (singleton) e `AdminAuditEntry` + DbSets + configuração (check singleton, índices de auditoria) em `Infrastructure/Persistence/Entities/` e `AppDbContext.cs`
- [x] T009 [P] Adicionar `IsIndexable` à entidade `Lure` + mapeamento (default true) em `Infrastructure/Persistence/Entities/Catalog.cs` e `AppDbContext.cs`
- [x] T010 Gerar migration EF Core: colunas auditáveis em todas as tabelas + `app_settings` (linha inicial `seo_indexing_enabled=true`) + `lures.is_indexable` + `admin_audit_log`, com backfill `source='automation'` para o catálogo semeado, em `Infrastructure/Persistence/Migrations/` (ver [data-model.md](data-model.md))
- [x] T011 [P] Implementar `ActiveUserMiddleware` (rejeita utilizador `!IsActive`/`DeletedAt!=null` com 401, cache Redis `user:active:{id}` TTL≤60s + invalidação) e fazer `AdminPolicy` derivar a role da BD em `Infrastructure/Auth/ActiveUserMiddleware.cs` + `AuthExtensions.cs` + pipeline em `Program.cs`
- [x] T012 Regressão: ajustar queries existentes afetadas pelo query filter (Catalog/Reviews/Profile) e correr `dotnet test` até a suite da Feature 001 (15 integração) ficar verde (SC-009)

**Checkpoint**: Base auditável ativa, soft-delete global a funcionar, admin gating pronto.

---

## Phase 3: User Story 1 — Ciclo de vida e proveniência (Priority: P1) 🎯 MVP

**Goal**: Superfícies públicas respeitam IsActive/DeletedAt e o estado do **pai verdadeiro**
(marca→isca); a relação fraca isca↔espécie não oculta iscas.

**Independent Test**: Ao nível de dados/serviço, marcar isca/marca como inativa ou eliminada e
confirmar que desaparece do catálogo/busca/detalhe/índice; restaurar e confirmar reaparecimento;
espécie inativa não oculta a isca.

### Tests for User Story 1

- [x] T013 [P] [US1] Teste de integração: registos inativos/eliminados são excluídos de catálogo, busca e detalhe; e independência de estado (FR-006: isca `archived` com `is_active=true` vs `published` com `is_active=false`) em `apps/api/tests/Infolure.IntegrationTests/Lifecycle/VisibilityTests.cs`
- [x] T014 [P] [US1] Teste de integração: marca inativa/eliminada oculta as iscas (cascata de pai, FR-003a); espécie inativa/eliminada **não** oculta (relação fraca, FR-003b) em `.../Lifecycle/CascadeVisibilityTests.cs`
- [x] T015 [P] [US1] Teste de integração: restore repõe o `is_active` anterior (FR-004) e backfill de `source` correto em `.../Lifecycle/RestoreAndSourceTests.cs`

### Implementation for User Story 1

- [x] T016 [US1] Aplicar visibilidade por pai (marca ativa e não eliminada) nas queries públicas em `apps/api/src/Infolure.Api/Features/Catalog/` (LureListService + LureDetailService)
- [x] T017 [US1] Filtrar espécies-alvo inativas/eliminadas da resposta de detalhe e dos facets em `Features/Catalog/` (sem ocultar a isca)
- [x] T018 [US1] `LureIndexer`: indexar apenas `published+active+não-eliminado`; remover do índice ao desativar/eliminar em `Infrastructure/Search/LureIndexer.cs`
- [x] T019 [US1] Garantir que o `Seeder` marca dados semeados com `Source='automation'` em `Infrastructure/Seed/Seeder.cs`

**Checkpoint**: Ciclo de vida totalmente funcional e testável de forma independente.

---

## Phase 4: User Story 2 — Backoffice: dashboard e CRUD (Priority: P1)

**Goal**: Painel admin com dashboard de cadastros + CRUD de todas as entidades (incl. dados
pessoais), gating de role, bloqueios de segurança, avisos RGPD e escrita de auditoria.

**Independent Test**: Autenticar como admin, ver dashboard, criar/editar/desativar/eliminar/restaurar
registos; não-admin recebe 403; utilizador desativado deixa de autenticar.

### Tests for User Story 2

- [ ] T020 [P] [US2] Teste de integração: não-admin recebe 403 em `/v1/admin/*` em `apps/api/tests/Infolure.IntegrationTests/Admin/AdminGatingTests.cs`
- [ ] T021 [P] [US2] Teste de integração: CRUD por recurso (list filtro/paginação, create, patch, soft-delete, restore, toggle active) para `lures` e `users`, incluindo efeitos colaterais (reindex Typesense em `lures`) em `.../Admin/AdminCrudTests.cs`
- [ ] T022 [P] [US2] Teste de integração: bloqueios FR-013 (último admin / auto-desativação) → 409 em `.../Admin/AdminSafeguardsTests.cs`
- [ ] T023 [P] [US2] Teste de integração: utilizador desativado → 401 na requisição seguinte (FR-013a) em `.../Admin/InactiveUserAuthTests.cs`
- [ ] T024 [P] [US2] Teste de integração: `GET /v1/admin/dashboard` devolve as métricas esperadas em `.../Admin/DashboardTests.cs`
- [ ] T025 [P] [US2] Teste de integração: operação sobre dados pessoais grava auditoria com `changes` antes→depois (FR-020a) em `.../Admin/AuditWriteTests.cs`

### Implementation for User Story 2 (backend)

- [ ] T026 [US2] Implementar `AdminAuditInterceptor` (`ISaveChangesInterceptor`): quando existe um `IAdminActionContext` ativo, emite **uma entrada em `admin_audit_log` por entidade escrita** (create/update/soft-delete/restore/toggle), garantindo FR-020/SC-007 por construção, em `Infrastructure/Persistence/Auditing/AdminAuditInterceptor.cs`
- [ ] T027 [US2] Endpoints admin **por recurso** assentes numa base partilhada (`AdminResourceServiceBase`: list/filtro/paginação/soft-delete/restore/toggle-active), **preservando os efeitos colaterais** (reindex Typesense em `lures`, recálculo de preços, moderação de reviews) em `Features/Admin/` (conforme `contracts/admin-api.yaml`)
- [ ] T027b [US2] Reconciliar o `AdminController` atual: migrar `brands`/`species`/`lures`/`prices`/`reviews-moderation` para o padrão de recurso, **eliminando rotas duplicadas** com o CRUD; manter a moderação de reviews como endpoint dedicado e **excluir `reviews` do patch genérico** (M1) em `Features/Admin/AdminController.cs`
- [ ] T028 [US2] `DashboardService` + `GET /v1/admin/dashboard` (cadastros 7/30d + série, iscas por status/source/active, reviews pendentes, totais) em `Features/Admin/DashboardService.cs`
- [ ] T029 [US2] Bloqueios de segurança (último admin, auto-eliminação/desativação) na camada de serviço em `Features/Admin/`
- [ ] T030 [US2] Definir o `IAdminActionContext` (scoped, populado pela autenticação admin) e marcar os tipos de **dados pessoais** (users/favorites/inventory) para que o `AdminAuditInterceptor` inclua o snapshot `changes` antes→depois (FR-020a) em `Infrastructure/Persistence/Auditing/` + `Features/Admin/`
- [ ] T031 [US2] Reindex Typesense nas operações admin que afetam visibilidade em `Features/Admin/` (reutilizar `LureIndexer`)

### Implementation for User Story 2 (frontend)

- [ ] T032 [P] [US2] Proteção da rota `/admin` por role admin (convenção `proxy`/middleware conforme docs Next 16) em `apps/web/`
- [ ] T033 [US2] Layout do painel + dashboard com estados loading/empty/error em `apps/web/app/admin/page.tsx`
- [ ] T034 [P] [US2] Componentes genéricos data-table + form em `apps/web/components/admin/`
- [ ] T035 [US2] Páginas CRUD por entidade (list/create/edit) em `apps/web/app/admin/[resource]/`
- [ ] T036 [US2] Modal de aviso RGPD antes de operações sobre dados pessoais em `apps/web/components/admin/`
- [ ] T036b [US2] Disponibilizar no painel a **eliminação RGPD efetiva** (remoção/anonimização irreversível, reutilizando o fluxo da Feature 001), distinta e claramente rotulada face ao soft-delete reversível (FR-012a), em `apps/web/app/admin/[resource]/` (users)

**Checkpoint**: Backoffice operacional e independente; US-01 e US-02 funcionam.

---

## Phase 5: User Story 3 — Controlo de indexação (Priority: P2)

**Goal**: Interruptor global + `is_indexable` por isca, refletidos em runtime em
robots/sitemap/metadata; perfis sempre noindex.

**Independent Test**: Alternar o flag global e confirmar (< 60s) `robots.txt`, `sitemap.xml` e
`noindex` no detalhe; marcar isca como não-indexável e confirmar exclusão pontual.

### Tests for User Story 3

- [ ] T037 [P] [US3] Teste de integração: `PUT /v1/admin/settings/indexing` invalida cache; `GET /v1/seo` reflete; sitemap só lista elegíveis em `apps/api/tests/Infolure.IntegrationTests/Seo/IndexingToggleTests.cs`
- [ ] T038 [P] [US3] Teste E2E: toggle OFF → robots disallow + sitemap vazio + detalhe noindex; perfis sempre noindex em `apps/web/tests/e2e/indexing.spec.ts`

### Implementation for User Story 3

- [ ] T039 [US3] `SeoSettingsService` (ler/gravar `app_settings`, cache Redis TTL≤60s + invalidação) em `Features/Seo/SeoSettingsService.cs`
- [ ] T040 [US3] `GET /v1/seo` (flag + dados de sitemap) em `Features/Seo/SeoController.cs`
- [ ] T041 [US3] `PUT /v1/admin/settings/indexing` (atualiza + invalida cache + auditoria `settings_update`) em `Features/Admin/`
- [ ] T042 [P] [US3] `app/robots.ts` dinâmico (Allow/Disallow conforme flag) em `apps/web/app/robots.ts`
- [ ] T043 [P] [US3] `app/sitemap.ts` dinâmico (iscas published+active+não-eliminadas+indexable) em `apps/web/app/sitemap.ts`
- [ ] T044 [US3] `generateMetadata` do detalhe: `noindex` quando flag off ou `is_indexable=false` em `apps/web/app/iscas/[slug]/page.tsx`
- [ ] T045 [P] [US3] Perfis sempre `noindex` em `apps/web/app/u/[username]/`
- [ ] T046 [US3] Toggle `is_indexable` por isca na UI do painel em `apps/web/app/admin/[resource]/` (lures)

**Checkpoint**: Indexação controlável; US-01..US-03 funcionam.

---

## Phase 6: User Story 4 — Consulta do registo de auditoria (Priority: P3)

**Goal**: Consultar o histórico de auditoria (escrito na US-02) com filtros por autor, ação e período.

**Independent Test**: Após ações no painel, `GET /v1/admin/audit?action=delete` devolve as entradas
correspondentes; UI permite filtrar.

### Tests for User Story 4

- [ ] T047 [P] [US4] Teste de integração: `GET /v1/admin/audit` filtra por actor/action/período + paginação em `apps/api/tests/Infolure.IntegrationTests/Admin/AuditQueryTests.cs`

### Implementation for User Story 4

- [ ] T048 [US4] `GET /v1/admin/audit` (filtros + paginação) em `Features/Admin/AuditController.cs`
- [ ] T049 [US4] UI de consulta de auditoria (tabela filtrável) em `apps/web/app/admin/audit/`

**Checkpoint**: Todas as user stories funcionais e independentes.

---

## Phase 7: Polish & Cross-Cutting Concerns

- [ ] T050 [P] Executar a validação completa do [quickstart.md](quickstart.md) (15 cenários)
- [ ] T051 [P] Verificar logs estruturados sem PII e que snapshots de dados pessoais só vivem no `admin_audit_log` (Princípio II)
- [ ] T052 Correr a suite completa (`dotnet test` + `npx playwright test`) e confirmar verde, incluindo regressão da 001 (SC-009)
- [ ] T053 [P] Atualizar contrato/docs se a implementação divergir de `contracts/admin-api.yaml`

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: sem dependências — pode começar já.
- **Foundational (Phase 2)**: depende do Setup — **BLOQUEIA todas as user stories**.
- **US-01 (Phase 3)**: depende da Foundational. É o MVP.
- **US-02 (Phase 4)**: depende da Foundational; consome a base auditável e o gating. Independente da US-01 ao nível de teste (manipulação direta de dados).
- **US-03 (Phase 5)**: depende da Foundational (precisa de `is_indexable` e do gating admin para o toggle). A escrita do flag usa a auditoria da US-02 (degrada bem se ausente).
- **US-04 (Phase 6)**: depende da escrita de auditoria da US-02.
- **Polish (Phase 7)**: depende das user stories desejadas.

### User Story Dependencies

- US-01 (P1): após Foundational. Sem dependências de outras stories.
- US-02 (P1): após Foundational. Independentemente testável.
- US-03 (P2): após Foundational. `settings_update` regista auditoria se a US-02 existir.
- US-04 (P3): após US-02 (lê o que a US-02 escreve).

### Parallel Opportunities

- Setup: T001, T002, T003 em paralelo.
- Foundational: T009 e T011 em paralelo com a linha T004→T008→T010 (T011 toca auth, ficheiros distintos).
- Dentro de cada story, todos os testes `[P]` correm em paralelo; modelos/ficheiros distintos `[P]` também.
- Com equipa: após a Foundational, US-01 e US-02 podem ser desenvolvidas em paralelo.

---

## Parallel Example: User Story 1

```bash
# Testes da US-01 juntos:
Task: "VisibilityTests.cs (T013)"
Task: "CascadeVisibilityTests.cs (T014)"
Task: "RestoreAndSourceTests.cs (T015)"
```

---

## Implementation Strategy

### MVP First (US-01)

1. Phase 1 (Setup) → 2. Phase 2 (Foundational, CRÍTICO) → 3. Phase 3 (US-01)
4. **PARAR e VALIDAR**: testar US-01 isoladamente (soft-delete/inativo/cascata) → SC-002/SC-009 verdes.

### Incremental Delivery

Foundational → US-01 (MVP) → US-02 (backoffice) → US-03 (indexação) → US-04 (auditoria). Cada story
acrescenta valor sem quebrar as anteriores.

---

## Notes

- [P] = ficheiros diferentes, sem dependências. [Story] mapeia a tarefa à user story.
- A escrita de auditoria vive na US-02; a US-04 é apenas consulta.
- Verificar que os testes falham antes de implementar (Princípio IV).
- A migration (T010) é o ponto de maior risco — validar contagens antes/depois (SC-008).
- Commit atómico por tarefa ou grupo lógico.
