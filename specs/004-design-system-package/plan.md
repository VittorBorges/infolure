# Implementation Plan: Design System Partilhado + Storybook

**Branch**: `004-design-system-package` | **Date**: 2026-06-18 | **Spec**: [spec.md](spec.md)

**Input**: Feature specification from `/specs/004-design-system-package/spec.md`

## Summary

Promover os componentes shadcn/ui e os tokens criados na feature 003 (hoje em `apps/web`, scoped ao `/admin`) a um **pacote partilhado** `packages/design-system`, com **build próprio** (ESM + tipos), **tokens como fonte única** (`@theme` da paleta branco/azul/verde) e um **Storybook** que documenta os componentes. O monorepo passa a usar **npm workspaces** (hoje informal, sem `package.json` na raiz). O backoffice admin migra para consumir o pacote sem regressões; o frontend público fica **habilitado** a consumir o pacote com uma **adoção-piloto** numa página. Backend (`apps/api`) não é tocado.

O ponto técnico central é o **Tailwind v4 num monorepo**: as utilities são geradas por scanning do código. O pacote exporta os **tokens** (`@theme`) como fonte de verdade e os **componentes**; cada app que consome o pacote **gera as suas próprias utilities** escaneando o pacote (diretiva `@source`), o que mantém uma única biblioteca por app (FR-009) sem CSS duplicado, e os tokens centralizados (FR-003/SC-004).

## Technical Context

**Language/Version**: TypeScript 5.x; React 19.2; Node (versão do ambiente atual). Backend .NET 10 **não tocado**.

**Primary Dependencies**: Existentes — Next.js 16.2, Tailwind v4, shadcn/ui (Radix, cva, clsx, tailwind-merge, lucide-react). **Novas**: gestão de monorepo via **npm workspaces** (sem ferramenta extra tipo turbo/nx), **tsup** (build do pacote: ESM + `.d.ts`, esbuild por baixo), **Storybook** (builder Vite, React).

**Storage**: N/A — feature de frontend/ferramentas; sem BD, EF Core ou contratos de API.

**Testing**: Playwright (E2E) existente em `apps/web` deve permanecer verde (paridade do admin, SC-002, e público inalterado, FR-011). Build do pacote (`tsup`) e build do Storybook como gates. Teste de propagação de token (SC-004).

**Target Platform**: Navegadores modernos (app Next.js); Storybook local/estático para a equipa.

**Project Type**: Monorepo web — `apps/api` (.NET, intocado) + `apps/web` (Next.js, consumidor) + **novo** `packages/design-system` (biblioteca).

**Performance Goals**: Sem regressão de carregamento; biblioteca de UI carregada uma vez por app (FR-009/SC-006); o admin mantém o CSS code-split por rota da 003.

**Constraints**: Paridade visual/funcional do admin (SC-002); público inalterado fora do piloto (FR-011); tema claro branco/azul/verde mantido; acessibilidade básica preservada (FR-010).

**Scale/Scope**: ~8 componentes base + tokens a mover; 1 app a migrar (admin); 1 catálogo Storybook; 1 adoção-piloto pública.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

- **I. Simplicidade Primeiro (YAGNI) — NON-NEGOTIABLE**: ⚠️ Justificado. Introduz workspaces + pacote + build (tsup) + Storybook. Justificação: é precisamente o objetivo da feature (reutilização + fonte única + catálogo, pedido explícito). Escolhem-se as opções **mais simples** que cumprem: **npm workspaces** (já usamos npm; sem turbo/nx), **tsup** (zero-config sobre esbuild), e o Storybook só com o necessário. Ver *Complexity Tracking*.
- **II. Observabilidade por Padrão — NON-NEGOTIABLE**: ✅ Sem novas fronteiras de rede; nenhum logging alterado.
- **III. Contratos Explícitos**: ✅ Nenhuma alteração ao contrato de API. Surge um **contrato interno** novo — a API pública do pacote (exports + tokens) — documentado em `contracts/`.
- **IV. Qualidade Testável**: ✅ Critérios verificáveis: build do pacote e do Storybook passam; Playwright do admin verde (paridade); mudança de token propaga-se (SC-004). 
- **V. Experiência do Usuário Consistente**: ✅ A feature **avança** o princípio — fonte única de componentes/tokens e catálogo documentado; acessibilidade da 003 preservada (FR-010).

**Resultado**: PASS (com justificação em Complexity Tracking).

## Project Structure

### Documentation (this feature)

```text
specs/004-design-system-package/
├── plan.md              # Este ficheiro
├── research.md          # Fase 0 — decisões técnicas
├── data-model.md        # Fase 1 — N/A (sem entidades); documenta a ausência
├── quickstart.md        # Fase 1 — guia de validação
├── contracts/
│   └── package-api.md   # Fase 1 — API pública do pacote (exports + tokens + preset)
└── tasks.md             # Fase 2 (/speckit-tasks — NÃO criado aqui)
```

### Source Code (repository root)

```text
package.json                         # NOVO — raiz: workspaces ["apps/*","packages/*"]
packages/
└── design-system/                   # NOVO pacote @infolure/design-system
    ├── package.json                 # exports (./ , ./tokens.css, ./styles.css), build script (tsup)
    ├── tsup.config.ts               # build ESM + .d.ts
    ├── src/
    │   ├── index.ts                 # barrel: re-exporta todos os componentes + cn()
    │   ├── lib/utils.ts             # cn() (movido de apps/web)
    │   ├── tokens.css               # FONTE ÚNICA dos tokens (@theme; ex-admin.css)
    │   └── components/              # button, card, table, input, select, label, dialog, badge (movidos)
    ├── .storybook/                  # NOVO — config Storybook (builder Vite)
    │   ├── main.ts
    │   └── preview.ts               # importa tokens.css para as stories renderizarem com o tema
    ├── stories/                     # *.stories.tsx por componente (variantes/estados)
    └── dist/                        # artefacto construído (gerado; git-ignored)

apps/web/
├── package.json                     # passa a depender de "@infolure/design-system": "*"
├── app/admin/
│   ├── admin.css                    # passa a @import '@infolure/design-system/tokens.css' + @source do pacote
│   ├── layout.tsx / page.tsx / [resource]/** / audit/**   # imports → '@infolure/design-system'
├── components/admin/                # AdminNav, RowActions, LureEditForm → imports do pacote
├── components/ui/                   # REMOVIDO após mover os 8 componentes DS para o pacote
├── components/States.tsx            # MOVIDO para fora de ui/ — FICA na app (partilhado c/ público, não-DS,
│                                    #   não carrega Tailwind); os seus 3 imports públicos são atualizados
└── app/(pilot)/**                   # adoção-piloto pública (US3): 1 componente do pacote + Tailwind scoped

apps/api/                            # INALTERADO
```

**Structure Decision**: Monorepo com **npm workspaces**. O design system vive em `packages/design-system` (build `tsup` → `dist`, consumido pelas apps; Storybook consome a `src`). O `apps/web` deixa de ter `components/ui/*` e os tokens, passando a importá-los do pacote. O `States.tsx` (estados loading/empty/error) **fica na app** — é partilhado com páginas públicas que não carregam Tailwind e não é um componente do design system.

## Complexity Tracking

| Violation | Why Needed | Simpler Alternative Rejected Because |
|-----------|------------|--------------------------------------|
| Monorepo workspaces + pacote + build (tsup) | Objetivo da feature: fonte única reutilizável por várias apps com artefacto construível (FR-001/FR-002) | Manter cópias por app (estado atual) duplica e diverge — exatamente o problema a resolver |
| Ferramenta npm workspaces (não turbo/nx) | Ligar pacote↔apps com o mínimo; já usamos npm | turbo/nx adicionariam orquestração/caching que ainda não é precisa (YAGNI) |
| Storybook | Catálogo visual documentado pedido explicitamente (FR-006); base p/ verificação visual e futura sync | Documentação manual/markdown não renderiza componentes nem cobre estados — não cumpre FR-006 |
| `@source` do pacote no Tailwind das apps | Tailwind v4 exclui `node_modules` do scan; sem isto as utilities usadas pelo pacote não são geradas | Enviar CSS pré-compilado do pacote duplicaria utilities entre apps e quebraria a fonte única de tokens |
