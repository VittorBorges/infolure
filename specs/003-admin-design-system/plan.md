# Implementation Plan: Design System do Backoffice Admin

**Branch**: `003-admin-design-system` | **Date**: 2026-06-17 | **Spec**: [spec.md](spec.md)

**Input**: Feature specification from `/specs/003-admin-design-system/spec.md`

## Summary

Adotar o **shadcn/ui** sobre **Tailwind CSS v4** para uniformizar a apresentação da área de administração (`apps/web/app/admin`), substituindo os estilos inline atuais por um sistema de design coeso, de **tema claro forçado** e paleta **branco (fundo) / azul (primário) / verde (acento e estados positivos)**.

A abordagem-chave é **isolar o Tailwind/shadcn ao subárvore admin**: a folha de estilos do design system (`@import 'tailwindcss'` + tokens de tema) é importada **apenas em `app/admin/layout.tsx`**, não no root layout. Nas rotas públicas a folha nunca é carregada, pelo que o Preflight (reset do Tailwind) e os tokens **não tocam no frontend público** — satisfazendo FR-008/SC-003 por construção e respeitando o Princípio I (superfície mínima). Toda a lógica funcional existente (gating de sessão, `adminFetch`, server actions de CRUD, filtros de auditoria, fluxo RGPD) é preservada; apenas a camada de apresentação muda.

## Technical Context

**Language/Version**: TypeScript 5.x (frontend); o backend .NET 10 **não é tocado** nesta feature.

**Primary Dependencies**: Next.js 16.2.9 (App Router), React 19.2.4. **Novas**: `tailwindcss@4` + `@tailwindcss/postcss`, `shadcn/ui` (componentes copiados para o repo) e os seus pares — `class-variance-authority`, `clsx`, `tailwind-merge`, `lucide-react` (ícones) e primitivos Radix UI conforme os componentes adotados (`@radix-ui/react-dialog`, `@radix-ui/react-select`, `@radix-ui/react-label`).

**Storage**: N/A — feature puramente de apresentação; sem alteração de BD, EF Core ou migrations.

**Testing**: Playwright (E2E) já configurado em `apps/web/tests/e2e/`. A regressão visual/funcional é coberta por smoke E2E do painel + a suite existente (`indexing.spec.ts`) que deve permanecer verde.

**Target Platform**: Navegadores modernos em **desktop** (o backoffice é ferramenta de trabalho em ecrã grande; responsividade para tablet/telemóvel está fora de âmbito); SSR/RSC do Next.js App Router.

**Project Type**: Web app (monorepo: `apps/web` frontend + `apps/api` backend). Esta feature atua só em `apps/web`.

**Performance Goals**: Sem regressão percetível de carregamento; o CSS do admin é code-split por rota (carregado apenas em `/admin/*`), pelo que o público não paga o custo do Tailwind.

**Constraints**: Tema claro forçado (sem `prefers-color-scheme` na área admin); frontend público visualmente inalterado; contraste AA para texto normal; navegação por teclado preservada.

**Scale/Scope**: ~5 páginas/áreas admin (layout+nav, dashboard, CRUD `[resource]` dos 4 recursos, auditoria) + `RowActions`/fluxo RGPD; ~8 componentes base shadcn (button, card, table, input, select, label, dialog, badge).

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

- **I. Simplicidade Primeiro (YAGNI) — NON-NEGOTIABLE**: ⚠️ Justificado. Introduz novas dependências (Tailwind v4 + shadcn/Radix). Justificação: o shadcn/ui foi pedido explicitamente e é o caminho mais simples para um design system consistente face a CSS manual; adotam-se **apenas** os componentes necessários (não o kit inteiro) e o alcance é isolado ao admin. Ver *Complexity Tracking*.
- **II. Observabilidade por Padrão — NON-NEGOTIABLE**: ✅ Sem novas fronteiras de rede; `adminFetch` e o seu tratamento de erro mantêm-se. Nenhum log alterado.
- **III. Contratos Explícitos**: ✅ Nenhuma alteração ao contrato de API (`contracts/admin-api.yaml` da 002 permanece a fonte de verdade). O único "contrato" novo é o de tokens de design (interno ao frontend) — documentado em `contracts/`.
- **IV. Qualidade Testável**: ✅ A preservação funcional é verificável: a suite Playwright existente deve continuar verde e adiciona-se um smoke E2E de renderização do painel. Critério de aceitação = "zero regressões funcionais com teste verde".
- **V. Experiência do Usuário Consistente**: ✅ A feature **avança** este princípio — estados de loading/empty/error coerentes, contraste e foco de teclado tratados explicitamente na nova paleta.

**Resultado**: PASS (com 1 justificação registada em Complexity Tracking).

## Project Structure

### Documentation (this feature)

```text
specs/003-admin-design-system/
├── plan.md              # Este ficheiro
├── research.md          # Fase 0 — decisões técnicas
├── data-model.md        # Fase 1 — N/A (sem entidades); documenta a ausência
├── quickstart.md        # Fase 1 — guia de validação
├── contracts/
│   └── design-tokens.md # Fase 1 — contrato de tokens + inventário de componentes
└── tasks.md             # Fase 2 (/speckit-tasks — NÃO criado aqui)
```

### Source Code (repository root)

```text
apps/web/
├── app/
│   ├── globals.css                 # (inalterado — continua a servir o público)
│   └── admin/
│       ├── admin.css               # NOVO — @import 'tailwindcss' + tokens; importado SÓ aqui
│       ├── layout.tsx              # refatorado (shell/nav no design system; importa admin.css)
│       ├── page.tsx                # dashboard refatorado (Card/Badge)
│       ├── [resource]/page.tsx     # listagem/filtros/paginação no design system (Table/Input/Select/Button)
│       └── audit/page.tsx          # consulta de auditoria no design system
├── components/
│   ├── ui/                         # NOVO — componentes shadcn (button, card, table, input, select, label, dialog, badge)
│   │   └── States.tsx              # estados loading/empty/error migrados para o design system
│   └── admin/
│       └── RowActions.tsx          # ações de linha + fluxo RGPD no design system (Button/Dialog/Badge)
├── lib/
│   └── utils.ts                    # NOVO — helper cn() (clsx + tailwind-merge)
├── components.json                 # NOVO — config shadcn
├── postcss.config.mjs              # NOVO — plugin @tailwindcss/postcss
└── package.json                    # novas devDeps/deps

apps/api/                           # INALTERADO nesta feature
```

**Structure Decision**: Monorepo web app; alterações confinadas a `apps/web`. O design system vive em `components/ui/` (convenção shadcn) e o seu CSS é importado exclusivamente pelo `app/admin/layout.tsx`, garantindo o isolamento face às rotas públicas que continuam a usar `app/globals.css` + estilos inline/CSS Modules.

## Complexity Tracking

| Violation | Why Needed | Simpler Alternative Rejected Because |
|-----------|------------|--------------------------------------|
| Novas dependências: Tailwind v4 + shadcn/ui + primitivos Radix | Pedido explícito do utilizador; design system consistente, acessível e manutenível para o backoffice | CSS manual / estilos inline (estado atual) não escala, é inconsistente e não dá acessibilidade/foco/contraste de raiz; reescrever à mão um sistema equivalente seria mais código e mais frágil que adotar a biblioteca padrão da comunidade |
| Folha Tailwind dedicada ao admin (não global) | Garantir FR-008/SC-003 (público inalterado) sem auditar/migrar todo o frontend público | Importar o Tailwind no root layout aplicaria o Preflight a todo o site, arriscando regressões visuais no público — mais risco e mais trabalho de verificação |
