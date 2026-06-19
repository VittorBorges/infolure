---
description: "Task list — Feature 006: Melhorias ao Formulário de Iscas"
---

# Tasks: Melhorias ao Formulário de Iscas (006)

**Input**: Design documents from `specs/006-lure-form-enhancements/`

**Prerequisites**: [plan.md](./plan.md), [spec.md](./spec.md), [research.md](./research.md), [data-model.md](./data-model.md), [contracts/admin-api-delta.yaml](./contracts/admin-api-delta.yaml)

**Tests**: INCLUÍDOS (constituição Princípio IV + FR-012).

**Organization**: por user story. US1–US3 = P1; US4–US5 = P2. O **rename** "tamanho"→"configuração"
(FR-006a) é cross-cutting e fica na fase Foundational para todo o código ficar coerente; a parte
de comportamento da US4 (anzol por configuração) fica na sua fase.

## Path Conventions

- Backend: `apps/api/src/Infolure.Api/`, testes em `apps/api/tests/Infolure.IntegrationTests/`
- Frontend: `apps/web/` (App Router em `app/`, componentes em `components/admin/`, helpers em `lib/`)

---

## Phase 1: Setup

- [x] T001 Configurar `apps/web/next.config.ts` com o limite de corpo dos Server Actions = `'5mb'` (corrige o bug de upload > 1 MB — US5/FR-010). **F3**: confirmar a chave/localização correta nos docs do Next 16 em `node_modules/next/dist/docs/` (pode **não** ser sob `experimental`) antes de assumir; validar que o upload > 1 MB deixa de falhar.

---

## Phase 2: Foundational (Blocking Prerequisites)

**⚠️ CRITICAL**: inclui o rename cross-cutting + mudanças de modelo. Nenhuma user story começa antes.

- [x] T002 Renomear entidade `LureSize`→`LureConfiguration` e `Lure.Sizes`→`Lure.Configurations`, adicionar `HookSize`/`HookType`/`HookCount` a `LureConfiguration`, e remover `HookSize`/`HookType`/`HookCount` e `IsIndexable` de `Lure` em `apps/api/src/Infolure.Api/Infrastructure/Persistence/Entities/Catalog.cs`
- [x] T003 Atualizar `apps/api/src/Infolure.Api/Infrastructure/Persistence/AppDbContext.cs`: renomear `DbSet<LureConfiguration>`, índice e FK; mapear as novas colunas de anzol (HexCodes inalterado)
- [x] T004 Gerar e aplicar migration EF Core `LureConfigurationsHooksAndGlobalIndexing` em `apps/api/.../Migrations/` (rename `lure_sizes`→`lure_configurations` + índice; `+hook_size/hook_type/hook_count`; `DROP` `lures.hook_size/hook_type/hook_count` e `lures.is_indexable`)
- [x] T005 Renomear/ajustar DTOs em `apps/api/src/Infolure.Api/Features/Admin/AdminDtos.cs`: `SizeInput`→`ConfigurationInput` (+`hook_size/hook_type/hook_count`), `AdminLureSizeDto`→`AdminLureConfigurationDto`, `ColorInput.PhotoUrl`→`PhotoUrls` (lista), remover `IsIndexable` de `AdminLureDetailDto`, adicionar `BrandWrite`/`BrandDetail`
- [x] T006 Atualizar `apps/api/src/Infolure.Api/Features/Admin/LureWriteService.cs` para o rename (configurations) e remover qualquer manuseio de `is_indexable` (persistência de anzol/fotos detalhada em US4/US5)
- [x] T007 Atualizar a leitura pública em `apps/api/src/Infolure.Api/Features/Catalog/` (`CatalogDtos.cs`, `LureDetailService.cs`): `sizes`→`configurations`, expor anzol por configuração e `photos[]` por cor; manter `primary_image_url` e `weight_g`/`length_mm` derivados (compat.)
- [x] T007a **F1**: atualizar os consumidores públicos dos campos movidos — `apps/web/app/iscas/[slug]/page.tsx` (e quaisquer componentes/`lib/catalog.ts`) que leiam anzol/comprimento ao nível da isca — para a nova forma (anzol por configuração) ou manter compat. via derivados; ajustar testes E2E públicos afetados (`apps/web/tests/e2e/lure-detail.spec.ts`)
- [x] T008 Ajustar `apps/api/src/Infolure.Api/Infrastructure/Search/LureIndexer.cs` ao novo nome (peso a partir de `Configurations`)
- [x] T009 [P] Frontend: renomear `SizeListField.tsx`→`ConfigurationListField.tsx` e atualizar `apps/web/lib/admin-actions.ts` (`LureWritePayload`: `sizes`→`configurations`, `photo_url`→`photo_urls[]`) e referências em `LureForm.tsx`; remover usos de `is_indexable`
- [x] T010 Atualizar `apps/api/tests/Infolure.IntegrationTests/Admin/LureWriteTests.cs` para `configurations` e garantir `dotnet test` verde (baseline do rename)

**Checkpoint**: código coerente com "configuração"; suite verde antes das stories.

---

## Phase 3: User Story 1 — Indexação SEO global (Priority: P1)

**Goal**: remover indexação por isca; um único interruptor global no painel.

**Independent Test**: alternar o toggle global e ver o sitemap refletir; confirmar ausência de controlo por isca.

### Tests for User Story 1 ⚠️

- [x] T011 [P] [US1] Teste de integração `apps/api/tests/Infolure.IntegrationTests/Seo/GlobalIndexingTests.cs`: `PUT /v1/admin/settings/indexing` liga/desliga e o sitemap (`GET /v1/seo`) reflete; `PUT /v1/admin/lures/{id}/indexable` deixou de existir

### Implementation for User Story 1

- [x] T012 [US1] Backend em `apps/api/src/Infolure.Api/Features/Admin/AdminController.cs`: remover `PUT lures/{id}/indexable`; adicionar `GET /v1/admin/settings/indexing` (estado atual via `SeoSettingsService`)
- [x] T013 [US1] `apps/api/src/Infolure.Api/Features/Seo/SeoSettingsService.cs`: o sitemap deixa de filtrar por `IsIndexable` (passa a `published && active` + flag global)
- [x] T014 [US1] Frontend: criar `apps/web/app/admin/settings/page.tsx` com o toggle global (lê `GET`, chama `PUT`); remover a coluna/badge `is_indexable` e `setIndexableAction` em `apps/web/app/admin/[resource]/page.tsx` e `apps/web/lib/admin-actions.ts`
- [x] T015 [P] [US1] E2E `apps/web/tests/e2e/admin-settings.spec.ts` (skip-gated): alternar indexação global

**Checkpoint**: indexação só global; nada por isca.

---

## Phase 4: User Story 2 — CRUD de marcas (Priority: P1)

**Goal**: criar/listar/editar/eliminar marcas no backoffice.

**Independent Test**: criar, editar, listar e eliminar uma marca pelo painel.

### Tests for User Story 2 ⚠️

- [x] T016 [P] [US2] Teste de integração `apps/api/tests/Infolure.IntegrationTests/Admin/BrandCrudTests.cs`: criar, obter, atualizar e (soft-)eliminar marca

### Implementation for User Story 2

- [x] T017 [US2] `apps/api/src/Infolure.Api/Features/Admin/BrandService.cs` (NOVO) com get/update, e endpoints `GET`/`PUT /v1/admin/brands/{id}` em `AdminController.cs` (create já existe; 409 em slug em conflito)
- [x] T018 [US2] Frontend: `apps/web/components/admin/BrandForm.tsx` (NOVO) e ligação nas páginas `app/admin/[resource]/new/page.tsx` e `[resource]/[id]/page.tsx` para `brands`
- [x] T019 [P] [US2] `apps/web/lib/admin-actions.ts`: `createBrandAction`/`updateBrandAction`

**Checkpoint**: marcas geríveis no painel.

---

## Phase 5: User Story 3 — Selecionar marca por nome (Priority: P1)

**Goal**: escolher a marca na isca por autocomplete, sem UUID.

**Independent Test**: escrever parte do nome, escolher, gravar; ao reabrir, nome pré-selecionado.
**F2**: testável contra as marcas do seed (Marca 01–20); US2 só é pré-requisito para criar marcas novas.

### Tests for User Story 3 ⚠️

- [x] T020 [P] [US3] Teste de integração em `apps/api/tests/Infolure.IntegrationTests/Admin/LureWriteTests.cs`: `GET /v1/admin/lures/{id}` inclui `brand_name`; criar/editar com `brand_id` persiste

### Implementation for User Story 3

- [x] T021 [US3] Backend: incluir `brand_name` em `AdminLureDetailDto` (projeção em `LureWriteService.GetForEditAsync`) em `apps/api/src/Infolure.Api/Features/Admin/`
- [x] T022 [P] [US3] Frontend: `apps/web/components/admin/BrandPicker.tsx` (NOVO) — autocomplete via `GET /v1/admin/brands?q=` (debounce), guarda `brand_id`, mostra nome
- [x] T023 [US3] `apps/web/components/admin/LureForm.tsx`: substituir o input de UUID da marca por `BrandPicker` (pré-preencher pelo nome na edição)
- [x] T024 [P] [US3] E2E em `apps/web/tests/e2e/lure-form.spec.ts` (skip-gated): selecionar marca por nome

**Checkpoint**: marca escolhida por nome, UUID nunca exposto.

---

## Phase 6: User Story 4 — Anzol por configuração (Priority: P2)

**Goal**: cada configuração com tamanho/quantidade/tipo de anzol (rename já feito na Foundational).

**Independent Test**: configurações com anzol distinto persistem; secção chama-se "Configurações".

### Tests for User Story 4 ⚠️

- [x] T025 [P] [US4] Teste de integração em `apps/api/tests/Infolure.IntegrationTests/Admin/LureWriteTests.cs`: criar/editar isca com anzol por configuração (persistido e reaberto, distinto entre configurações)

### Implementation for User Story 4

- [x] T026 [US4] `apps/api/src/Infolure.Api/Features/Admin/LureWriteService.cs` (+`LureWriteValidator.cs`): persistir/validar `hook_size`/`hook_type`/`hook_count` por configuração
- [x] T027 [US4] `apps/web/components/admin/ConfigurationListField.tsx`: adicionar campos de anzol (tamanho/quantidade/tipo) por configuração e remover quaisquer campos de anzol ao nível da isca em `LureForm.tsx`; rotular a secção "Configurações"

**Checkpoint**: anzol por configuração funcional.

---

## Phase 7: User Story 5 — Múltiplas fotos por cor + upload > 1 MB (Priority: P2)

**Goal**: várias fotos por cor; fotos > 1 MB funcionam (limite 5 MB), com teste.

**Independent Test**: carregar várias fotos (incl. > 1 MB) numa cor; recusar > 5 MB; remover uma foto.

### Tests for User Story 5 ⚠️

- [x] T028 [P] [US5] Teste `apps/api/tests/Infolure.IntegrationTests/Media/MediaUploadTests.cs`: validação de limite — 2 MB aceite, 6 MB `TooLarge`, tipo inválido `UnsupportedType` (FR-012, prova que o limite é 5 MB e não 1 MB)
- [x] T029 [P] [US5] Teste de integração em `LureWriteTests.cs`: cor persiste **várias** fotos (`photo_urls[]`), por ordem; remover uma mantém as restantes

### Implementation for User Story 5

- [x] T030 [US5] `apps/api/src/Infolure.Api/Features/Media/BlobUploadService.cs`: extrair a validação tamanho/tipo para um método puro testável (limite 5 MB)
- [x] T031 [US5] `apps/api/src/Infolure.Api/Features/Admin/LureWriteService.cs`: persistir **múltiplas** imagens por cor a partir de `photo_urls[]` (replace restrito a `color_id IS NOT NULL`)
- [x] T032 [US5] `apps/web/components/admin/ColorPhotosField.tsx` (renomear/estender de `ColorPhotoField.tsx`): upload de várias fotos, lista com pré-visualização e remoção; `LureForm`/`ColorListField` usam `photo_urls[]`. **F4**: manter mensagens claras para 413 (>5 MB), 415 (tipo inválido) e 503 (não configurado).
- [x] T033 [P] [US5] E2E em `apps/web/tests/e2e/lure-form.spec.ts` (skip-gated): carregar foto > 1 MB e várias fotos numa cor

**Checkpoint**: fotos múltiplas e upload > 1 MB a funcionar.

---

## Phase 8: Polish & Cross-Cutting

- [x] T034 [P] Regenerar/ajustar tipos do frontend onde aplicável (`apps/web/lib/`) face ao delta de contrato
- [x] T035 [P] Logging estruturado nos novos endpoints (`brands/{id}`, `settings/indexing`) em `AdminController.cs`/`BrandService.cs`
- [x] T036 Acessibilidade/estados dos novos componentes (`BrandPicker`, toggle de settings, `ColorPhotosField`) — Princípio V
- [ ] T037 Executar `quickstart.md` (cenários 1–5) e a suite completa `dotnet test` verde
- [ ] T038 [P] Atualizar documentação do admin (README/docs) se necessário

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (1)** → **Foundational (2, inclui rename + migration)** bloqueia todas as stories.
- **US1, US2** (P1) independentes entre si após a Foundational. **US3** (P1) depende de US2 (precisa de marcas para escolher) e do `BrandPicker`.
- **US4, US5** (P2) dependem da Foundational; independentes entre si.
- **Polish (8)** após as stories desejadas.

### User Story Dependencies

- US1: após Foundational. Sem dependência de outras.
- US2: após Foundational. Sem dependência de outras.
- US3: após Foundational + **US2** (marcas existem para selecionar).
- US4: após Foundational (rename já aplicado).
- US5: após Foundational.

### Parallel Opportunities

- Foundational: T009 (frontend rename) em paralelo às mudanças backend após T002–T005.
- Dentro de cada story, testes [P] e componentes de UI [P] em paralelo.
- Com equipa: após a Foundational, US1 e US2 em paralelo; US4/US5 em paralelo.

---

## Parallel Example: User Story 2

```bash
Task: "BrandCrudTests em apps/api/tests/Infolure.IntegrationTests/Admin/BrandCrudTests.cs"
Task: "createBrandAction/updateBrandAction em apps/web/lib/admin-actions.ts"
```

---

## Implementation Strategy

### MVP First

1. Setup + Foundational (rename + migration) → 2. US1 (indexação global) → validar → demo.

### Incremental Delivery

1. Foundational → 2. US1 → 3. US2 → 4. US3 (marca por nome) → 5. US4 (anzol/config) → 6. US5 (fotos).

---

## Notes

- O rename é cross-cutting (Foundational); as stories assumem o código já em "configuração".
- US5 corrige o bug do 1 MB em `next.config.ts` (T001) + valida o limite 5 MB no backend (T028).
- Sem migração de dados de negócio; a migration faz rename de tabela e drop/add de colunas.
