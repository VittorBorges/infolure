---
description: "Task list — Feature 003: Design System do Backoffice Admin"
---

# Tasks: Design System do Backoffice Admin

**Input**: Design documents from `/specs/003-admin-design-system/`

**Prerequisites**: [plan.md](plan.md), [spec.md](spec.md), [research.md](research.md), [contracts/design-tokens.md](contracts/design-tokens.md), [quickstart.md](quickstart.md)

**Tests**: A spec não pede TDD. Inclui-se apenas um **smoke E2E** do painel (Princípio IV / research §5) e a garantia de que a suite Playwright existente permanece verde — nada mais.

**Organization**: Tarefas agrupadas por user story (US1 → US2 → US3), em ordem de prioridade. Âmbito 100% em `apps/web`; o backend `apps/api` **não é tocado**.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Pode correr em paralelo (ficheiros diferentes, sem dependências por concluir)
- **[Story]**: A que user story a tarefa pertence (US1, US2, US3)
- Caminhos de ficheiro são relativos à raiz do repositório

---

## Phase 1: Setup (Infraestrutura partilhada — Tailwind v4 + shadcn/ui)

**Purpose**: Introduzir o toolchain do design system no `apps/web` (research §1, §3, §4).

- [x] T001 Instalar Tailwind v4 e criar o PostCSS plugin: `npm install -D tailwindcss @tailwindcss/postcss` em `apps/web` e criar `apps/web/postcss.config.mjs` com `{ plugins: { '@tailwindcss/postcss': {} } }` (conforme docs Next 16 — `node_modules/next/dist/docs/01-app/01-getting-started/11-css.md`)
- [x] T002 [P] Instalar dependências do shadcn/ui em `apps/web`: `class-variance-authority`, `clsx`, `tailwind-merge`, `lucide-react` e os primitivos Radix necessários (`@radix-ui/react-dialog`, `@radix-ui/react-select`, `@radix-ui/react-label`), garantindo compatibilidade com `react@19.2.4` (registar em [quickstart.md](quickstart.md) se exigir `--legacy-peer-deps`)
- [x] T003 [P] Criar a config do shadcn `apps/web/components.json` (style, alias `@/*` já existente, base color neutra) e o helper `apps/web/lib/utils.ts` com `cn()` (clsx + tailwind-merge)

---

## Phase 2: Foundational (Pré-requisitos bloqueantes)

**Purpose**: Tema, isolamento ao admin e componentes base — sem isto nenhuma US pode ser implementada.

**⚠️ CRITICAL**: Nenhuma user story começa antes desta fase estar completa.

- [x] T004 Criar `apps/web/app/admin/admin.css` com `@import 'tailwindcss'` + tokens de tema via `@theme inline` usando a paleta **"Azul SaaS + Verde fresco"** com os valores exatos de [contracts/design-tokens.md](contracts/design-tokens.md) §1 (primário `#2563EB`, sucesso `#16A34A`, fundo `#FFFFFF`, texto `#0F172A`, borda `#E2E8F0`, ring `#2563EB`, radius `0.625rem`; **sem** bloco `.dark` e **sem** `@media (prefers-color-scheme)`)
- [x] T005 Importar `admin.css` **apenas** em `apps/web/app/admin/layout.tsx` (isolamento por rota — research §2); confirmar que `apps/web/app/layout.tsx`/`globals.css` ficam intactos (não importar Tailwind no root)
- [x] T006 [P] Adicionar os componentes base shadcn em `apps/web/components/ui/` — `button`, `card`, `table`, `input`, `select`, `label`, `dialog`, `badge` (via `npx shadcn@latest add ...`; se o CLI falhar no Next 16, copiar manualmente da documentação — research §3)
- [x] T007 Validar o toolchain: `npm run build` em `apps/web` compila; classes Tailwind aplicam em `/admin` e o CSS é code-split (não carrega nas rotas públicas)

**Checkpoint**: Base do design system pronta — as user stories podem começar.

---

## Phase 3: User Story 1 — Painel admin com aparência consistente (Priority: P1) 🎯 MVP

**Goal**: Esqueleto do painel (layout/navegação) e dashboard apresentados no design system (tema claro, branco/azul/verde).

**Independent Test**: Abrir `/admin` autenticado e confirmar dashboard + navegação no novo tema, sem regressão nas métricas nem no gating de sessão.

- [x] T008 [US1] Refatorar `apps/web/app/admin/layout.tsx`: **sidebar moderna com ícones** (`lucide-react`: ícone + rótulo por item, ~240px, item ativo a azul `--primary` com fundo `--secondary`) + cabeçalho fino no topo, conforme [contracts/design-tokens.md](contracts/design-tokens.md) §1b, **preservando** o gating de sessão e o redirect para `/login?returnUrl=/admin`
- [x] T009 [US1] Refatorar `apps/web/app/admin/page.tsx` (dashboard): substituir o `Card` inline por `Card` shadcn (cantos `--radius`, sombra subtil `shadow-sm`, espaçamento generoso — estilo SaaS) + `Badge` (verde `--success` em estados positivos), **mantendo** as métricas e o estado de erro/403 legível (sem stack trace)
- [x] T010 [P] [US1] Estados loading/empty/error do admin no design system. **Desvio**: o `components/ui/States.tsx` é partilhado com páginas públicas (iscas/favoritos/inventário) que **não** carregam Tailwind — convertê-lo regrediria o público (FR-008). Em vez disso, cada página admin renderiza os seus estados com componentes do design system (ex.: dashboard usa `Card` no erro/403). `States.tsx` mantém-se inalterado.
- [x] T011 [US1] Smoke E2E em `apps/web/tests/e2e/admin-design.spec.ts`: sem sessão → redirect para `/login` (corre sempre); render autenticado de dashboard + navegação guardado por `E2E_ADMIN_STORAGE` (skip sem credenciais, à semelhança de `indexing.spec.ts`)

**Checkpoint**: US1 funcional e testável de forma isolada — MVP visível.

---

## Phase 4: User Story 2 — Gestão de conteúdos com componentes uniformes (Priority: P2)

**Goal**: CRUD por recurso (iscas/marcas/espécies/utilizadores) + ações de linha + fluxo RGPD no design system, preservando todas as operações.

**Independent Test**: Em cada recurso, a listagem/filtros/paginação, os distintivos de estado, as ações de linha e o aviso RGPD usam o design system e funcionam como antes.

- [x] T012 [US2] Refatorar `apps/web/app/admin/[resource]/page.tsx`: `Table`/`Input`/`Select`/`Button` do design system, **mantendo** os filtros `q`/`include`, a paginação e o tratamento de erro/403
- [x] T013 [US2] Refatorar `apps/web/components/admin/RowActions.tsx`: botões → `Button` do design system, **preservando** as server actions (`setActiveAction`, `softDeleteAction`, `restoreAction`, `setIndexableAction`, `eraseUserAction`) e o tratamento de 409
- [x] T014 [US2] Converter o aviso RGPD de `RowActions.tsx` num `Dialog` shadcn, mantendo a distinção visível entre **soft-delete (reversível)** e **eliminação RGPD (irreversível, só `users`, em `--destructive`)** — depende de T013 (mesmo ficheiro)
- [x] T015 [P] [US2] Aplicar `Badge` com semântica de cor nos estados de registo (ativo/inativo, eliminado, indexável) na listagem `[resource]`, com verde para estados positivos (FR-007/SC-005)
- [x] T021 [US2] **Formulário de edição da isca** (adição de âmbito a pedido do utilizador, resolve C1 do analyze): nova rota `apps/web/app/admin/[resource]/[id]/page.tsx` + `components/admin/LureEditForm.tsx` + server action `updateLureAction` em `lib/admin-actions.ts`. Edita os campos suportados pelo backend (`PATCH /v1/admin/lures/{id}` → `status`, `weight_g`); link "Editar" na listagem de iscas. Peso em branco = mantém (sem GET de registo único no backend).

**Checkpoint**: US1 e US2 funcionam de forma independente.

---

## Phase 5: User Story 3 — Consulta de auditoria legível (Priority: P3)

**Goal**: Página de auditoria com filtros e tabela paginada no design system.

**Independent Test**: Abrir `/admin/audit`, filtrar por ação e paginar, com controlos/tabela no design system e resultados corretos.

- [x] T016 [US3] Refatorar `apps/web/app/admin/audit/page.tsx`: `Table`/`Select`/`Button` do design system, **mantendo** o filtro `action`, a paginação (que preserva filtros) e o tratamento de erro/403

**Checkpoint**: Todas as user stories independentes e funcionais.

---

## Phase 6: Polish & Cross-Cutting Concerns

**Purpose**: Acessibilidade, limpeza e validação final transversais.

- [x] T017 [P] Passagem de acessibilidade na área admin: foco de teclado visível (anel azul `--ring`), contraste de texto ≥ AA (SC-006), rótulos em campos de formulário (`label`)
- [x] T018 [P] Remover quaisquer estilos inline remanescentes nas páginas/componentes admin (SC-001 — nenhum `style={...}` restante em `app/admin/**` e `components/admin/**`)
- [x] T019 Validação do [quickstart.md](quickstart.md): cenários US1–US3 + **frontend público inalterado** (catálogo/detalhe/perfil) + `npx playwright test` verde, incluindo a suite existente `tests/e2e/indexing.spec.ts` (SC-002/SC-003)
- [x] T020 [P] Gates de qualidade: `npm run lint` e `tsc --noEmit` sem novos erros em `apps/web`

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: sem dependências — começa de imediato.
- **Foundational (Phase 2)**: depende do Setup — **BLOQUEIA** todas as user stories.
- **User Stories (Phase 3–5)**: dependem da Phase 2. Depois disso, US1/US2/US3 são independentes entre si (podem ser paralelizadas se houver capacidade).
- **Polish (Phase 6)**: depende das user stories desejadas estarem concluídas.

### User Story Dependencies

- **US1 (P1)**: arranca após a Phase 2 — sem dependências de outras stories. É o MVP.
- **US2 (P2)**: arranca após a Phase 2 — independente de US1 (usa os mesmos componentes base).
- **US3 (P3)**: arranca após a Phase 2 — independente de US1/US2.

### Within Each User Story

- T014 depende de T013 (mesmo ficheiro `RowActions.tsx`).
- T012 e T015 tocam o mesmo ficheiro `[resource]/page.tsx` — T015 marcada [P] por ser aditiva (badges), mas se houver conflito de edição, sequenciar após T012.

### Parallel Opportunities

- Setup: T002 e T003 em paralelo (após T001).
- Foundational: T006 em paralelo com T004/T005 (ficheiros distintos); T007 só no fim.
- US1: T010 ([P]) em paralelo com T008/T009.
- Polish: T017, T018 e T020 em paralelo; T019 por último.

---

## Parallel Example: User Story 1

```bash
# Após a Phase 2, dentro da US1:
Task: "Refatorar layout.tsx (shell/nav)"          # T008
Task: "Refatorar page.tsx (dashboard)"            # T009
Task: "Migrar components/ui/States.tsx"           # T010 [P]
```

---

## Implementation Strategy

### MVP First (User Story 1)

1. Phase 1 (Setup) → 2. Phase 2 (Foundational, CRÍTICA) → 3. Phase 3 (US1) → **PARAR e VALIDAR** o painel/dashboard isoladamente → demo do MVP visual.

### Incremental Delivery

1. Setup + Foundational → base pronta.
2. US1 → testar → demo (MVP).
3. US2 → testar → demo (CRUD + RGPD).
4. US3 → testar → demo (auditoria).
5. Polish → acessibilidade + validação final + suite verde.

---

## Notes

- [P] = ficheiros diferentes, sem dependências.
- Backend `apps/api` **inalterado**; sem migrations nem alterações de contrato de API.
- Invariante crítica em todas as fases: **frontend público sem qualquer alteração** (FR-008/SC-003).
- Commit após cada tarefa ou grupo lógico; validar a cada checkpoint.
