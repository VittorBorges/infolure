---
description: "Task list — Feature 005: Formulário de Registo e Edição de Iscas"
---

# Tasks: Formulário de Registo e Edição de Iscas

**Input**: Design documents from `specs/005-lure-registration-form/`

**Prerequisites**: [plan.md](./plan.md), [spec.md](./spec.md), [research.md](./research.md), [data-model.md](./data-model.md), [contracts/admin-lures-api.yaml](./contracts/admin-lures-api.yaml)

**Tests**: INCLUÍDOS — exigidos pela constituição (Princípio IV) e pelo quickstart (xUnit + Playwright).

**Organization**: Tarefas agrupadas por user story para implementação/teste independentes.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Pode correr em paralelo (ficheiros diferentes, sem dependências por concluir)
- **[Story]**: US1 (registo, P1), US2 (edição, P1), US3 (cores, P2)

## Path Conventions

- Backend: `apps/api/src/Infolure.Api/`, testes em `apps/api/tests/Infolure.IntegrationTests/`
- Frontend: `apps/web/` (App Router em `app/`, componentes em `components/admin/`, helpers em `lib/`)

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Configuração transversal antes do trabalho de domínio.

- [ ] T001 Configurar definições de Azure Blob (`Azure:Blob:ConnectionString` e container de fotos) em `apps/api/src/Infolure.Api/appsettings.Development.json` + user-secrets, e documentar Azurite para dev local
- [x] T002 [P] Criar helper de validação/normalização de hex em `apps/web/lib/hex.ts` (regex `^#([0-9a-fA-F]{3}|[0-9a-fA-F]{6})$`, normaliza para minúsculas; **não** deduplica — duplicados permitidos)

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Modelo, persistência e serviço de escrita transacional partilhados por TODAS as stories.

**⚠️ CRITICAL**: Nenhuma user story pode começar antes desta fase.

- [x] T003 Adicionar entidade `LureSize` (Code, Label, LengthMm, WeightG, SortOrder + auditáveis) e a coleção `Lure.Sizes` em `apps/api/src/Infolure.Api/Infrastructure/Persistence/Entities/Catalog.cs`
- [x] T004 Evoluir `LureColor` em `apps/api/src/Infolure.Api/Infrastructure/Persistence/Entities/Catalog.cs`: remover `HexPrimary`/`HexSecondary`, adicionar `HexCodes` (lista de `LureHexCode { Hex, Label }`) mapeável a JSONB
- [x] T005 Remover `WeightG`/`LengthMm` de `Lure` em `Catalog.cs` e o `HasIndex(WeightG)` (`idx_lures_weight`) no `AppDbContext.cs`; mapear `DbSet<LureSize>` com FK `OnDelete(Cascade)` + `idx_lure_sizes_lure` e `LureColor.HexCodes` como coluna JSONB (owned/JSON conversion, default `'[]'`)
- [x] T006 Gerar e aplicar migration EF Core `LureFormSizesAndColorHex` em `apps/api/src/Infolure.Api/Infrastructure/Persistence/Migrations/` (cria `lure_sizes`; em `lure_colors` adiciona `hex_codes` e remove `hex_primary`/`hex_secondary`; **dropa** `lures.weight_g`/`length_mm` e `idx_lures_weight`) — sem backfill (catálogo vazio)
- [x] T007 [P] Definir DTOs ricos (`LureWriteRequest`, `SizeInput`, `ColorInput`, `HexCodeInput`, `LureDetail`) em `apps/api/src/Infolure.Api/Features/Admin/AdminDtos.cs` conforme `contracts/admin-lures-api.yaml`; retirar/atualizar `UpdateLureRequest` antigo (`WeightG` deixa de existir)
- [x] T008 Implementar `LureWriteValidator` (FluentValidation) em `apps/api/src/Infolure.Api/Features/Admin/LureWriteValidator.cs`: obrigatórios (slug, nome, lure_type), slug único, ≥1 tamanho com `weight_g`+`label`, cada `hex` válido (duplicados na mesma cor são permitidos — podem ter textura diferente), cor não vazia (nome OU ≥1 hex)
- [x] T009 Implementar `LureWriteService` em `apps/api/src/Infolure.Api/Features/Admin/LureWriteService.cs`: upsert transacional de lure + `lure_translations` (descrição) + `lure_sizes` + `lure_colors`(+hex_codes) com replace-children **estritamente limitado** a estas coleções (`lure_sizes` é fonte única de peso/comprimento); **NÃO** tocar em `lure_retailer_prices`, `lure_reviews`, imagens gerais (`color_id IS NULL`) nem `is_indexable`; na **edição**, `status` ausente = preservar o atual (nunca despromover); reindex best-effort via `LureIndexer`; registar no DI em `Program.cs`
- [x] T009a Atualizar `LureIndexer` (`apps/api/src/Infolure.Api/Infrastructure/Search/LureIndexer.cs`) para indexar o peso a partir de `lure_sizes` (ex.: min/max por isca) em vez de `lures.weight_g`
- [x] T009b Refatorar TODOS os leitores dos escalares removidos (modelo limpo): API pública `LureDetailDto`/`LureColorDto` (expor `sizes[]` e `hex_codes[]`, remover `weight_g`/`length_mm`/`hex_primary`/`hex_secondary`), `LureListService`/`LureSummaryDto` (peso = min de `lure_sizes`, via doc), `FavoritesService`, `InventoryService`/`InventoryColorDto`, `Seeder` (semear `lure_sizes`+`hex_codes`), `AdminController.UpdateLure`/`UpdateLureRequest`, e o frontend público + testes E2E que consomem estes campos. Manter o parâmetro público de filtro por peso.
- [x] T010 [P] Adicionar `createLureAction` e `updateLureAction` (payload completo) em `apps/web/lib/admin-actions.ts` (POST/PUT via `adminFetch`, `revalidatePath`)
- [x] T011 [P] Criar `LureForm.tsx` base (campos escalares: nome, slug, tipo, marca, water_type, descrição; estados loading/erro/sucesso) em `apps/web/components/admin/LureForm.tsx` usando `@infolure/design-system`

**Checkpoint**: Modelo + serviço de escrita + form base prontos — as user stories podem começar.

---

## Phase 3: User Story 1 — Registar nova isca com todas as propriedades (Priority: P1) 🎯 MVP

**Goal**: Editor cria uma isca com propriedades, lista de tamanhos e descrição, e ela fica persistida.

**Independent Test**: Abrir `/admin/lures/new`, preencher obrigatórios + 2 tamanhos + descrição, gravar, confirmar persistência ao reabrir.

### Tests for User Story 1 ⚠️

- [x] T012 [P] [US1] Teste de integração: `POST /v1/admin/lures` cria isca com tamanhos e descrição (persistidos em `lure_sizes`); cobre busca por peso a partir dos tamanhos indexados em `apps/api/tests/Infolure.IntegrationTests/LureWriteTests.cs`
- [x] T013 [P] [US1] E2E: registar isca em `/admin/lures/new` (caminho feliz + campo obrigatório em falta) em `apps/web/tests/e2e/lure-form.spec.ts`

### Implementation for User Story 1

- [x] T014 [US1] Implementar `POST /v1/admin/lures` em `apps/api/src/Infolure.Api/Features/Admin/AdminController.cs` delegando a `LureWriteService` + `LureWriteValidator` (substitui o `CreateLure` mínimo atual)
- [x] T015 [P] [US1] Criar `SizeListField.tsx` (lista dinâmica: code, label, length_mm, weight_g, reordenável) em `apps/web/components/admin/SizeListField.tsx`
- [x] T016 [US1] Criar página `apps/web/app/admin/lures/new/page.tsx` montando `LureForm` + `SizeListField` e chamando `createLureAction`
- [x] T017 [US1] Validação no cliente (obrigatórios, ≥1 tamanho com peso) com mensagens compreensíveis no `LureForm`

**Checkpoint**: Registo de iscas funcional e testável de forma independente (MVP).

---

## Phase 4: User Story 2 — Editar isca existente (Priority: P1)

**Goal**: Editor abre uma isca pré-preenchida, altera campos e grava sem afetar os não tocados.

**Independent Test**: Abrir `/admin/lures/[id]`, confirmar pré-preenchimento, alterar só a descrição, gravar, verificar que o resto se mantém.

### Tests for User Story 2 ⚠️

- [x] T018 [P] [US2] Teste de integração em `apps/api/tests/Infolure.IntegrationTests/LureWriteTests.cs`: `GET /v1/admin/lures/{id}` devolve tudo; `PUT` que altera um campo preserva os restantes — incl. `status` (não despromove `published`), `is_indexable`, `lure_retailer_prices` e imagens gerais; **round-trip no-op** (GET→PUT sem alterações) não muda nada (SC-005)
- [x] T019 [P] [US2] E2E: editar isca pré-preenchida em `/admin/lures/[id]` em `apps/web/tests/e2e/lure-form.spec.ts`

### Implementation for User Story 2

- [x] T020 [US2] Implementar `GET /v1/admin/lures/{id}` (projeção `LureDetail`) e `PUT /v1/admin/lures/{id}` em `apps/api/src/Infolure.Api/Features/Admin/AdminController.cs` (delega a `LureWriteService`; conflito de slug → 409)
- [x] T021 [US2] Reescrever `apps/web/app/admin/lures/[id]/page.tsx` para carregar a isca e pré-preencher `LureForm` (substitui o `LureEditForm` mínimo de status+weight)
- [x] T022 [US2] Garantir semântica de submissão completa no `LureForm` (envia estado integral; replace-children não perde coleções não editadas)

**Checkpoint**: Registo + edição funcionais e independentes.

---

## Phase 5: User Story 3 — Gerir lista de cores da isca (Priority: P2)

**Goal**: Cada cor com múltiplas cores de base (hex+label), foto opcional e lista de hex; validação de hex.

**Independent Test**: Numa isca, adicionar cor "verde+amarelo" com 2 hex e foto, gravar, reabrir; introduzir hex inválido e ver bloqueio.

### Tests for User Story 3 ⚠️

- [x] T023 [P] [US3] Teste de integração: cor com múltiplos hex persiste; hex inválido → 422 com caminho do campo; foto opcional (com/sem) em `apps/api/tests/Infolure.IntegrationTests/LureWriteTests.cs`
- [x] T024 [P] [US3] E2E: adicionar cor verde+amarelo, vários hex, foto; e caso de hex inválido em `apps/web/tests/e2e/lure-form.spec.ts`

### Implementation for User Story 3

- [x] T025 [P] [US3] Implementar `BlobUploadService` (validação tipo/tamanho, upload Azure Blob, devolve URL pública) em `apps/api/src/Infolure.Api/Features/Media/BlobUploadService.cs` + registo no DI
- [x] T026 [US3] Implementar `POST /v1/admin/media` (multipart) em `apps/api/src/Infolure.Api/Features/Admin/AdminController.cs` usando `BlobUploadService` (415/413 nos limites)
- [x] T027 [US3] Estender `LureWriteService` para persistir `photo_url` de cada cor em `lure_images` (`color_id` definido, máx. uma por cor); o replace de imagens é restrito a `color_id IS NOT NULL` — não apaga imagens gerais da isca — em `apps/api/src/Infolure.Api/Features/Admin/LureWriteService.cs`
- [x] T028 [P] [US3] Criar `ColorListField.tsx` (lista dinâmica de cores: name_pt/en, pattern) em `apps/web/components/admin/ColorListField.tsx`
- [x] T029 [P] [US3] Criar `HexCodeListField.tsx` (lista de `{hex, label}` por cor, validação via `lib/hex.ts`, preview) em `apps/web/components/admin/HexCodeListField.tsx`
- [x] T030 [US3] Criar `ColorPhotoField.tsx` (upload/preview de foto opcional via `/v1/admin/media`) em `apps/web/components/admin/ColorPhotoField.tsx` e integrar `ColorListField` no `LureForm`

**Checkpoint**: Todas as user stories independentemente funcionais.

---

## Phase 6: Polish & Cross-Cutting Concerns

**Purpose**: Qualidade transversal, observabilidade e validação final.

- [x] T031 [P] Logging estruturado (correlation-id, latência) nos novos endpoints e no `BlobUploadService` (Princípio II), sem PII/segredos
- [ ] T032 [P] Regenerar tipos do frontend a partir do contrato (`cd apps/web && npm run gen:admin-api-types`) e ajustar usos
- [x] T033 Acessibilidade e estados do `LureForm` (labels, foco/teclado nas listas dinâmicas, estados vazios/erro) — Princípio V
- [ ] T034 Executar `quickstart.md` (cenários 1–4) e validar SC-001..SC-005
- [ ] T035 [P] Atualizar documentação do admin (README/docs) se necessário

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: sem dependências.
- **Foundational (Phase 2)**: depende do Setup — BLOQUEIA todas as user stories.
- **User Stories (Phase 3+)**: dependem da Phase 2.
  - US1 e US2 partilham `LureWriteService`/`LureForm` (foundational); podem depois ser feitas em paralelo.
  - US3 depende do mesmo + acrescenta upload (independente).
- **Polish (Phase 6)**: depois das stories desejadas.

### User Story Dependencies

- **US1 (P1)**: arranca após Foundational. Sem dependência de outras stories.
- **US2 (P1)**: arranca após Foundational. Reutiliza `LureForm`/`LureWriteService`; testável de forma independente.
- **US3 (P2)**: arranca após Foundational. Acrescenta cores/hex/foto; testável de forma independente.

### Within Each User Story

- Testes escritos primeiro e a FALHAR antes da implementação.
- Backend (endpoint/serviço) e componentes de UI antes da integração na página.

### Parallel Opportunities

- Setup: T002 paralelo a T001.
- Foundational: T007, T010, T011 em paralelo (após T003–T006 do modelo).
- Dentro de cada story, os testes [P] e os componentes de UI [P] correm em paralelo.
- Com equipa: após a Phase 2, US1/US2/US3 em paralelo por pessoas diferentes.

---

## Parallel Example: User Story 1

```bash
# Testes da US1 juntos:
Task: "Integração POST cria isca em apps/api/tests/Infolure.IntegrationTests/LureWriteTests.cs"
Task: "E2E registo em apps/web/tests/e2e/lure-form.spec.ts"

# Componentes/endpoint da US1:
Task: "SizeListField.tsx em apps/web/components/admin/SizeListField.tsx"
Task: "POST /v1/admin/lures em AdminController.cs"
```

---

## Implementation Strategy

### MVP First (User Story 1)

1. Phase 1 (Setup) → 2. Phase 2 (Foundational) → 3. Phase 3 (US1) → 4. Validar registo → 5. Demo.

### Incremental Delivery

1. Setup + Foundational → fundação pronta.
2. US1 (registo) → testar → demo (MVP).
3. US2 (edição) → testar → demo.
4. US3 (cores/hex/foto) → testar → demo.

### Parallel Team Strategy

Após a Foundational: Dev A → US1, Dev B → US2, Dev C → US3 (cores + upload).

---

## Notes

- [P] = ficheiros diferentes, sem dependências.
- `LureWriteService` é o ponto central da escrita transacional (sizes + colors); US3 estende-o para fotos.
- `lure_sizes` é a fonte única de peso/comprimento — os escalares `lures.weight_g`/`length_mm` são removidos e a busca da 001 passa a indexar a partir de `lure_sizes` (T009a/T009b).
- Sem migração de dados (catálogo sem cores/tamanhos/iscas reais).
- Commit após cada tarefa ou grupo lógico; parar em cada checkpoint para validar a story.
