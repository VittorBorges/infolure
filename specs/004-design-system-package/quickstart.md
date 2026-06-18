# Quickstart — Feature 004: Design System Partilhado + Storybook

Guia de validação. Prova que o design system vive num pacote partilhado, que o admin o consome sem
regressões, que o catálogo (Storybook) documenta os componentes e que o público o pode usar (piloto).
Detalhes da API do pacote em [contracts/package-api.md](contracts/package-api.md); os tokens são os da
[feature 003 §1](../003-admin-design-system/contracts/design-tokens.md).

## Pré-requisitos

- Node (versão do ambiente) + npm com **workspaces** (raiz com `package.json`).
- Instalar a partir da raiz do monorepo: `npm install` (liga `@infolure/design-system` ao `apps/web`).
- Backend das features 001/002 a correr apenas se quiseres validar o admin com dados reais (opcional).

> Nota: a introdução de workspaces cria um `package-lock.json` na **raiz**. Correr `npm install` na raiz.

## Build/setup a validar

| # | Passo | Comando | Esperado |
|---|-------|---------|----------|
| B1 | Build do pacote | `npm run build -w @infolure/design-system` | Gera `dist/` com `.js` (ESM) + `.d.ts`; diretivas `'use client'` preservadas (grep) |
| B2 | App compila com o pacote | `npm run build -w web` | `apps/web` compila consumindo `@infolure/design-system`; admin code-split mantém-se |
| B3 | Catálogo | `npm run storybook -w @infolure/design-system` (dev) ou `build-storybook` | Lista todos os componentes com variantes/estados, renderizados com os tokens |
| B4 | Tipos/lint | `tsc --noEmit` em ambos | Sem novos erros |

## Cenários de validação (mapeados às user stories)

| # | Cenário | Como validar | Resultado esperado |
|---|---------|--------------|--------------------|
| US1 | Pacote reutilizável | `import { Button } from '@infolure/design-system'` numa app | Componente renderiza com estilo/comportamento corretos |
| US1 | Admin migrado, paridade | Abrir `/admin` (e subpáginas) | Igual à feature 003 — sidebar, dashboard, tabelas, RGPD, edição da isca; zero regressões |
| US1 | Sem duplicação | `grep -r "components/ui/" apps/web/app apps/web/components` | Sem ocorrências (componentes vivem só no pacote) — SC-001 |
| US1 | Fonte única de tokens | Alterar `--primary` **só** em `packages/design-system/src/tokens.css`, rebuild | O azul muda no admin (e nos consumidores) — SC-004 |
| US1 | Artefacto independente | Inspecionar `dist/` | Componentes + tipos consumíveis sem código de nenhuma app — FR-002 |
| US2 | Catálogo navegável | Abrir o Storybook | Todos os componentes listados, com variantes/estados — SC-003 |
| US3 | Público consome o DS | Abrir a página-piloto pública | Usa um componente do pacote, renderizado com os tokens — SC-005 |
| US3 | Público inalterado | Abrir catálogo/detalhe/perfil públicos | Idênticos ao anterior; sem regressões — FR-011 |
| — | UI carregada uma vez | Inspecionar o bundle da app | Sem duplicação da biblioteca de UI — FR-009/SC-006 |
| — | Acessibilidade | Navegar `/admin` por teclado | Foco azul visível, contraste AA, rótulos — FR-010 |

## Regressão automatizada

- `npx playwright test` em `apps/web` — suite existente **verde**: gating/admin (paridade) e públicos
  (catálogo, detalhe, 404) inalterados (SC-002, FR-011).

## Critérios de saída (Definition of Done)

- [ ] Pacote `@infolure/design-system` construível (`dist`) e ligado por workspaces (FR-001/FR-002).
- [ ] Tokens como fonte única no pacote; mudança propaga-se (FR-003/SC-004).
- [ ] Admin consome só o pacote, sem cópias locais, com paridade (SC-001/SC-002).
- [ ] Storybook lista 100% dos componentes com variantes/estados (FR-006/SC-003).
- [ ] Público pode consumir o DS, com adoção-piloto a funcionar; resto do público inalterado (FR-007/FR-011/SC-005).
- [ ] Sem duplicação da biblioteca de UI (FR-009/SC-006); acessibilidade preservada (FR-010).
- [ ] Playwright verde.
