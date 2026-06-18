---
description: "Task list — Feature 004: Design System Partilhado + Storybook"
---

# Tasks: Design System Partilhado + Storybook

**Input**: Design documents from `/specs/004-design-system-package/`

**Prerequisites**: [plan.md](plan.md), [spec.md](spec.md), [research.md](research.md), [contracts/package-api.md](contracts/package-api.md), [quickstart.md](quickstart.md)

**Tests**: A spec não pede TDD. A garantia é a suite Playwright existente (`apps/web`) permanecer verde (paridade do admin + público inalterado) e os gates de build (pacote, app, Storybook). Sem novos testes unitários.

**Organization**: Tarefas por user story (US1 → US2 → US3). Monorepo: novo `packages/design-system`, `apps/web` consumidor, `apps/api` **não tocado**. Nome do pacote: `@infolure/design-system`.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Pode correr em paralelo (ficheiros diferentes, sem dependências por concluir)
- Caminhos relativos à raiz do repositório

---

## Phase 1: Setup (Workspaces + scaffold do pacote)

**Purpose**: Estabelecer o monorepo com workspaces e o esqueleto do pacote (research §1, §2).

- [x] T001 Criar `package.json` na raiz do repositório com `"private": true` e `"workspaces": ["apps/*", "packages/*"]`; mover/garantir que `apps/web` é um workspace (sem duplicar deps que passam a ser do pacote)
- [x] T002 Criar o esqueleto de `packages/design-system/`: `package.json` (name `@infolure/design-system`, `type: module`, `exports` para `.` / `./tokens.css` conforme [contracts/package-api.md](contracts/package-api.md), script `build`), `tsconfig.json` (extende o base, React 19/JSX), e `src/` vazio
- [x] T003 [P] Adicionar `tsup` como devDep do pacote e criar `packages/design-system/tsup.config.ts` (entradas por componente + barrel, `format: ['esm']`, `dts: true`, preservação de `'use client'`) — research §2
- [x] T004 Correr `npm install` na raiz e confirmar que `@infolure/design-system` fica ligado por symlink ao `apps/web` (workspace resolvido)

---

## Phase 2: Foundational (Mover tokens + componentes + wiring Tailwind)

**Purpose**: Pôr a fonte única de tokens e os componentes no pacote, com o Tailwind das apps a gerá-los. Bloqueia todas as user stories.

**⚠️ CRITICAL**: Nenhuma user story começa antes desta fase.

- [x] T005 Mover os tokens para o pacote: criar `packages/design-system/src/tokens.css` com o `@theme` da paleta da 003 (valores de [003 §1](../003-admin-design-system/contracts/design-tokens.md)); **sem** `.dark`/`@media prefers-color-scheme`
- [x] T006 Mover `apps/web/lib/utils.ts` (`cn()`) para `packages/design-system/src/lib/utils.ts` e os 8 componentes de `apps/web/components/ui/*` para `packages/design-system/src/components/*` (ajustando imports internos para `../lib/utils`)
- [x] T007 Criar o barrel `packages/design-system/src/index.ts` re-exportando todos os componentes + `cn` (API conforme [contracts/package-api.md](contracts/package-api.md) §2)
- [x] T008 Configurar o consumo no `apps/web`: adicionar `"@infolure/design-system": "*"` ao `apps/web/package.json` e `transpilePackages: ['@infolure/design-system']` em `apps/web/next.config.ts` (research §2)
- [x] T009 Reconfigurar o Tailwind do admin: `apps/web/app/admin/admin.css` passa a `@import 'tailwindcss'` + `@import '@infolure/design-system/tokens.css'` + `@source` do `packages/design-system/src` (research §3); remover os tokens duplicados do `admin.css`
- [x] T010 Build do pacote: `npm run build -w @infolure/design-system` produz `dist/` (ESM + `.d.ts`) com diretivas `'use client'` preservadas (verificar por `grep`)

**Checkpoint**: Pacote construído e ligado; tokens centralizados; Tailwind do admin a gerar via `@source`.

---

## Phase 3: User Story 1 — Pacote reutilizável + admin migrado (Priority: P1) 🎯 MVP

**Goal**: Admin consome exclusivamente o pacote, sem cópias locais, com paridade visual/funcional.

**Independent Test**: Abrir `/admin` e subpáginas; igual à 003; `grep` não encontra `components/ui/` em `apps/web`.

- [x] T011 [US1] Reescrever os imports do admin para o pacote: em `apps/web/app/admin/page.tsx`, `[resource]/page.tsx`, `[resource]/[id]/page.tsx`, `audit/page.tsx` trocar `'../../components/ui/x'` → `'@infolure/design-system'`
- [x] T012 [US1] Reescrever os imports nos componentes admin: `apps/web/components/admin/RowActions.tsx`, `LureEditForm.tsx` (e `AdminNav.tsx` se usar `cn`) → `'@infolure/design-system'`
- [x] T013 [US1] Mover `apps/web/components/ui/States.tsx` → `apps/web/components/States.tsx` (fica na app — partilhado com público, não-DS) e **atualizar os 3 imports públicos** (`app/iscas/page.tsx`, `app/conta/favoritos/page.tsx`, `app/conta/inventario/page.tsx`); depois eliminar `apps/web/components/ui/` (os 8 componentes DS) e `apps/web/lib/utils.ts` (movidos para o pacote). Confirmar por `grep` que `lib/utils.ts` não tem consumidores não-DS antes de remover (D1)
- [x] T014 [US1] `npm run build -w web` compila consumindo o pacote; `tsc --noEmit` limpo; confirmar `grep -r "components/ui/" apps/web/app apps/web/components` sem ocorrências de componentes DS (SC-001)
- [x] T015 [US1] Validar paridade: `npx playwright test` (admin gating + públicos) verde; inspeção visual do `/admin` igual à 003 (SC-002)

**Checkpoint**: US1 funcional — MVP: design system num pacote, admin migrado sem regressões.

---

## Phase 4: User Story 2 — Catálogo Storybook (Priority: P2)

**Goal**: Catálogo visual navegável de todos os componentes com variantes/estados.

**Independent Test**: Abrir o Storybook e confirmar todos os componentes com as suas variantes renderizados com os tokens.

- [x] T016 [US2] Instalar e configurar Storybook (builder Vite, React) em `packages/design-system/.storybook/main.ts`; integrar Tailwind v4 (`@tailwindcss/postcss`/plugin Vite) — research §4
- [x] T017 [US2] `packages/design-system/.storybook/preview.ts`: importar `tokens.css` + `@import 'tailwindcss'` com `@source` dos componentes/stories para renderizar com o tema
- [x] T018 [P] [US2] Stories de controlos: `button.stories.tsx` (todas as variantes/tamanhos), `badge.stories.tsx` (todas as variantes), `input.stories.tsx`, `label.stories.tsx` em `packages/design-system/stories/`
- [x] T019 [P] [US2] Stories de estrutura/overlay: `card.stories.tsx`, `table.stories.tsx`, `select.stories.tsx`, `dialog.stories.tsx` em `packages/design-system/stories/`
- [x] T020 [US2] Adicionar scripts `storybook` / `build-storybook` ao pacote; validar que `build-storybook` gera o catálogo com 100% dos componentes (SC-003)

**Checkpoint**: US1 + catálogo documentado.

---

## Phase 5: User Story 3 — Disponibilidade ao público + adoção-piloto (Priority: P3)

**Goal**: Público pode consumir o DS; uma página-piloto usa um componente do pacote, resto do público inalterado.

**Independent Test**: Abrir a página-piloto (usa componente do pacote, estilizado); catálogo/detalhe/perfil públicos idênticos.

- [x] T021 [US3] Escolher a página-piloto e criar um CSS de entrada próprio do seu segmento (`@import 'tailwindcss'` + `@import '@infolure/design-system/tokens.css'` + `@source`), importado **só** nesse segmento (isolamento por rota — research §6)
- [x] T022 [US3] Substituir **um** elemento de UI dessa página por um componente do pacote (ex.: `Button`/`Card`), confirmando render estilizado (FR-007/SC-005)
- [x] T023 [US3] Confirmar que o resto do público fica inalterado: `npx playwright test` (catálogo/detalhe/404) verde + inspeção (FR-011)

**Checkpoint**: Todas as user stories funcionais; promessa "admin e público" provada.

---

## Phase 6: Polish & Cross-Cutting Concerns

**Purpose**: Provar a fonte única, evitar duplicação e validar tudo.

- [x] T024 [P] Prova da fonte única (SC-004/FR-008): (a) alterar `--primary` só em `packages/design-system/src/tokens.css`, rebuild e confirmar o azul mudar no admin; (b) confirmar que uma alteração a um **componente** do pacote (ex.: classe no `Button`) se reflete nos consumidores após rebuild — depois reverter ambas as alterações
- [x] T025 [P] Confirmar ausência de duplicação da biblioteca de UI (FR-009/SC-006): inspecionar resolução de deps (`npm ls react @radix-ui/react-dialog` na raiz → versões únicas/deduped)
- [x] T026 [P] Acessibilidade preservada (FR-010): foco azul, contraste AA e rótulos continuam no admin via componentes do pacote
- [x] T027 Validação completa do [quickstart.md](quickstart.md): builds B1–B4 + cenários US1–US3 + Playwright verde + `.gitignore` para `packages/design-system/dist` e `storybook-static`
- [x] T028 [P] Atualizar docs: README do pacote (uso + `transpilePackages` + `@source`); nota de versionamento (versão do pacote ≠ versão do produto)

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: sem dependências — começa de imediato.
- **Foundational (Phase 2)**: depende do Setup — **BLOQUEIA** todas as user stories.
- **US1 (Phase 3)**: depende da Phase 2. É o MVP.
- **US2 (Phase 4)**: depende da Phase 2 (precisa do pacote/tokens); independente da US1.
- **US3 (Phase 5)**: depende da Phase 2; independente de US1/US2.
- **Polish (Phase 6)**: depende das user stories desejadas.

### User Story Dependencies

- **US1 (P1)**: após Phase 2 — migra o admin. MVP.
- **US2 (P2)**: após Phase 2 — Storybook sobre o pacote; não depende da migração do admin.
- **US3 (P3)**: após Phase 2 — piloto público; não depende de US1/US2.

### Within Each User Story

- T011/T012 (reescrever imports) antes de T013 (apagar cópias) antes de T014/T015 (validar).
- T016/T017 (config Storybook) antes de T018/T019 (stories) antes de T020 (validar catálogo).
- T021 (CSS isolado) antes de T022 (usar componente) antes de T023 (validar).

### Parallel Opportunities

- Setup: T003 em paralelo com T002 (após T002 existir o package.json).
- US2: T018 e T019 (ficheiros de stories distintos) em paralelo, após T016/T017.
- Polish: T024, T025, T026, T028 em paralelo; T027 por último.

---

## Parallel Example: User Story 2

```bash
# Após T016/T017 (config Storybook):
Task: "Stories de controlos (button/badge/input/label)"     # T018 [P]
Task: "Stories de estrutura/overlay (card/table/select/dialog)" # T019 [P]
```

---

## Implementation Strategy

### MVP First (US1)

1. Phase 1 (Setup) → 2. Phase 2 (Foundational, CRÍTICA) → 3. Phase 3 (US1) → **PARAR e VALIDAR**: admin migrado, paridade verde, sem cópias locais. Já entrega a fonte única reutilizável.

### Incremental Delivery

1. Setup + Foundational → pacote pronto.
2. US1 → admin migrado, paridade → MVP.
3. US2 → Storybook → catálogo documentado.
4. US3 → piloto público → promessa "admin e público".
5. Polish → fonte única provada, sem duplicação, validação final.

---

## Notes

- [P] = ficheiros diferentes, sem dependências.
- Backend `apps/api` **inalterado**; sem migrations nem alterações de contrato de API.
- Invariante: **público inalterado fora do piloto** (FR-011) e **paridade do admin** (SC-002).
- A versão do pacote é independente da versão do produto (versionamento por feature).
- Commit após cada fase/grupo lógico; validar a cada checkpoint.
