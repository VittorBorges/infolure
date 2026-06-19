---
description: "Task list for feature 007 — Sessão do utilizador no painel admin"
---

# Tasks: Sessão do utilizador no painel de administração (identidade + terminar sessão)

**Input**: Design documents from `/specs/007-admin-user-session/`

**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/me-api.yaml, quickstart.md

**Tests**: incluídos — exigidos pelo Princípio IV (Qualidade Testável) da constituição e referenciados no
quickstart (teste de integração de `GET /v1/me` + e2e de identidade/logout).

**Organization**: tarefas agrupadas por user story (US1, US2), ambas P1, para implementação e teste
independentes.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: pode correr em paralelo (ficheiros diferentes, sem dependências por concluir)
- **[Story]**: a que user story pertence (US1, US2)

## Path Conventions

Monorepo: backend em `apps/api/src/Infolure.Api/`, testes em `apps/api/tests/`; frontend em `apps/web/`.

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: preparar o ambiente de validação. Não há inicialização de projeto (monorepo já existe).

- [X] T001 Confirmar pré-requisitos de validação: `apps/api` e `apps/web` arrancam localmente (PostgreSQL `infolure-pg` :5433) e existe uma conta com role `admin` (ver `apps/api/src/Infolure.Api/Infrastructure/Seed/Seeder.cs`) para testar identidade e logout.

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: ponto de integração da UI partilhado pelas duas user stories (a zona onde a identidade e o
botão de logout vão viver).

**⚠️ CRITICAL**: bloqueia a UI de US1 e US2.

- [X] T002 [P] Criar o componente cliente esqueleto `apps/web/components/admin/AdminUserMenu.tsx` que recebe props `{ name, email, role }` e renderiza um contentor de identidade (sem obtenção de dados nem logout nesta fase).
- [X] T003 Adicionar uma zona de cabeçalho no `apps/web/app/admin/layout.tsx` que renderiza `AdminUserMenu` acima do conteúdo principal (slot partilhado por US1/US2). Depende de T002.

**Checkpoint**: existe um cabeçalho no painel pronto a receber identidade e ações.

---

## Phase 3: User Story 1 — Ver quem está autenticado (Priority: P1) 🎯 MVP

**Goal**: apresentar, de forma visível em todas as páginas do painel, o nome (ou email) e a função do
utilizador autenticado, sem expor o UUID.

**Independent Test**: autenticar como admin, abrir `/admin` e subpáginas, e confirmar que a identidade
apresentada corresponde à conta em sessão (nome/email + função) e nunca mostra um UUID.

### Tests for User Story 1 ⚠️

- [X] T004 [P] [US1] Teste de integração `apps/api/tests/Infolure.IntegrationTests/Users/MeTests.cs`: `GET /v1/me` → 200 autenticado (devolve `role`, **sem** `id`); 401 sem token. Cobre o contrato `contracts/me-api.yaml`.
- [X] T005 [P] [US1] E2E *skip-gated* `apps/web/tests/e2e/admin-session.spec.ts`: identidade (nome/email + função) visível ao abrir `/admin` e mantém-se ao navegar para uma subpágina.

### Implementation for User Story 1

- [X] T006 [P] [US1] Criar `MeDto` (`email`, `username`, `display_name`, `role`, `avatar_url`) em `apps/api/src/Infolure.Api/Features/Users/` (junto aos DTOs de perfil). Não inclui `id` (FR-004).
- [X] T007 [US1] Implementar `GetMeAsync(sub)` em `apps/api/src/Infolure.Api/Features/Users/ProfileService.cs`: resolve o utilizador pelo claim `sub` e mapeia para `MeDto`. Depende de T006.
- [X] T008 [US1] Adicionar `GET /v1/me` em `apps/api/src/Infolure.Api/Features/Users/ProfileController.cs` (`[Authorize(UserPolicy)]`; 401 se `sub` ausente; logging início/fim/resultado+latência — Princípio II). Depende de T007.
- [X] T009 [P] [US1] Adicionar o tipo `Me` e a função `getMe()` (via `adminFetch`) em `apps/web/lib/admin.ts`, derivados do contrato `me-api.yaml`.
- [X] T010 [US1] No `apps/web/app/admin/layout.tsx`, obter a identidade com `getMe()` (server-side) e passá-la a `AdminUserMenu`; tratar sessão inválida/expirada como não autenticada (redireção para `/login`, FR-010). Depende de T003, T008, T009.
- [X] T011 [US1] No `apps/web/components/admin/AdminUserMenu.tsx`, apresentar o nome (derivação `display_name` → `username` → `email`) e a função como `Badge`; valor neutro se a função for ausente; nunca expor UUID. Depende de T002, T010.

**Checkpoint**: US1 funcional — o painel mostra a identidade correta em todas as páginas (MVP).

---

## Phase 4: User Story 2 — Terminar sessão (Priority: P1)

**Goal**: permitir terminar a sessão a partir do painel, invalidando-a e encaminhando o utilizador para
fora das áreas protegidas.

**Independent Test**: estando autenticado, acionar "Terminar sessão" e confirmar que é redirecionado
para `/login` e que aceder de novo a `/admin` exige nova autenticação.

### Tests for User Story 2 ⚠️

- [X] T012 [US2] Estender `apps/web/tests/e2e/admin-session.spec.ts`: acionar "Terminar sessão" → redireção para `/login`; após logout, aceder a `/admin` exige nova autenticação (SC-004). (Mesmo ficheiro que T005 — sequencial.)

### Implementation for User Story 2

- [X] T013 [P] [US2] Criar a ação de logout no cliente em `apps/web/lib/auth-actions.ts`: `getSupabaseBrowserClient().auth.signOut()` e redireção para `/login`; devolve resultado para tratamento de erro.
- [X] T014 [US2] Adicionar o botão "Terminar sessão" ao `apps/web/components/admin/AdminUserMenu.tsx` (client): invoca o logout; indicação de progresso; botão desativado enquanto decorre (sem acionamentos repetidos); mensagem compreensível em falha sem estado ambíguo (FR-008/FR-009; Princípio V). Depende de T011, T013.

**Checkpoint**: US1 e US2 funcionais — identidade visível e logout operacional.

---

## Phase 5: Polish & Cross-Cutting Concerns

- [X] T015 [P] Acessibilidade do `AdminUserMenu` (rótulos claros, foco visível, navegação e ativação por teclado) — FR-011 / Princípio V.
- [X] T016 [P] Confirmar logging estruturado de `GET /v1/me` (início/fim/resultado+latência) e ausência de PII sensível/segredos nos logs — Princípio II.
- [X] T017 Atualizar os tipos derivados do contrato no frontend (`apps/web/lib/api-types.ts`) para incluir `GET /v1/me`, se aplicável ao pipeline de tipos.
- [X] T018 Executar `specs/007-admin-user-session/quickstart.md` (cenários 1–4) e garantir a suite de testes (`dotnet test` + e2e) verde.

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: sem dependências.
- **Foundational (Phase 2)**: depende do Setup; bloqueia a UI das user stories.
- **User Stories (Phase 3–4)**: dependem do Foundational. US1 e US2 são ambas P1.
- **Polish (Phase 5)**: depois de US1 e US2.

### User Story Dependencies

- **US1 (P1)**: após Foundational. Independente de US2.
- **US2 (P1)**: após Foundational. Independente em lógica de US1, mas o botão vive no mesmo componente
  `AdminUserMenu.tsx` (T014 estende o que T011 produz) — coordenar edições do ficheiro.

### Within Each User Story

- Testes escritos primeiro e a falhar antes da implementação.
- Backend: DTO → service → endpoint (T006 → T007 → T008).
- Frontend: lib (`getMe`) → consumo no layout → apresentação no componente (T009 → T010 → T011).

### Parallel Opportunities

- US1: T004 e T005 (testes, ficheiros distintos) em paralelo; T006 e T009 em paralelo (backend vs
  frontend); o backend (T006→T008) e o frontend lib (T009) podem avançar em paralelo até T010 juntar.
- Polish: T015 e T016 em paralelo.
- ⚠️ T011 (US1) e T014 (US2) tocam o **mesmo** ficheiro (`AdminUserMenu.tsx`) — **não** paralelizar.

---

## Parallel Example: User Story 1

```bash
# Testes de US1 em paralelo (ficheiros distintos):
Task: "Integração GET /v1/me em apps/api/tests/Infolure.IntegrationTests/Users/MeTests.cs"
Task: "E2E identidade visível em apps/web/tests/e2e/admin-session.spec.ts"

# Backend e frontend-lib em paralelo:
Task: "Criar MeDto em apps/api/src/Infolure.Api/Features/Users/"
Task: "Tipo Me + getMe() em apps/web/lib/admin.ts"
```

---

## Implementation Strategy

### MVP First (User Story 1)

1. Phase 1 (Setup) → 2. Phase 2 (Foundational) → 3. Phase 3 (US1) → **validar**: identidade visível no
   painel → demo. Este é o MVP.

### Incremental Delivery

1. Setup + Foundational → cabeçalho pronto.
2. US1 → identidade visível (MVP) → validar/demo.
3. US2 → logout operacional → validar/demo.
4. Polish (a11y, logging, tipos, quickstart).

---

## Notes

- [P] = ficheiros diferentes, sem dependências; [Story] mapeia a tarefa à user story.
- Verificar que os testes falham antes de implementar.
- Sem migrations: a feature é de leitura e reutiliza a tabela `users` e a sessão Supabase.
- Evitar: editar `AdminUserMenu.tsx` em paralelo entre US1 (T011) e US2 (T014).
