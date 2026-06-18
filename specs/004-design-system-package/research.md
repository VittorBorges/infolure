# Research — Feature 004: Design System Partilhado + Storybook

Decisões técnicas que resolvem as incógnitas do plano. Formato: Decisão / Rationale / Alternativas.
Verificado localmente: `transpilePackages` documentado no Next 16
(`apps/web/node_modules/next/dist/docs/.../transpilePackages.md`) e a diretiva **`@source`** suportada
em `tailwindcss@4.3.1`.

---

## 1. Gestão do monorepo: npm workspaces

**Decisão**: Criar um `package.json` na raiz com `"private": true` e
`"workspaces": ["apps/*", "packages/*"]`. O pacote será `@infolure/design-system` em
`packages/design-system`. O `apps/web` declara `"@infolure/design-system": "*"` e o npm liga por
symlink no `node_modules` da raiz.

**Rationale**: Já usamos **npm** (`apps/web/package-lock.json`); workspaces são nativos e suficientes
para ligar pacote↔app. Não introduz orquestração nem caching que ainda não precisamos (Princípio I).
A pasta `packages/` já existe (tem `api-types`, atualmente um ficheiro solto não consumido), pelo que
a estrutura é natural.

**Alternativas**:
- **turbo / nx** → orquestração e cache de builds que são overkill para 1 pacote + 1 app (YAGNI).
- **pnpm/yarn workspaces** → trocar de gestor sem ganho; o npm já cá está.

**Nota**: passa a haver um `package-lock.json` na raiz (workspaces). Documentar no quickstart.

---

## 2. Build do pacote: tsup (ESM + tipos), com preservação de `'use client'`

**Decisão**: O pacote constrói com **tsup** (esbuild) para `dist/` em **ESM** com **`.d.ts`**, um
entry por componente (preservando a fronteira de módulos). O `exports` do `package.json` aponta para
`dist`. **Preservar as diretivas `'use client'`** dos componentes interativos (button usa Slot;
dialog/select/label são Radix client) — tsup com preservação de diretivas — e, como rede de
segurança, o `apps/web` declara `transpilePackages: ['@infolure/design-system']` no `next.config.ts`.

**Rationale**: tsup é zero-config sobre esbuild e emite ESM + tipos num passo, satisfazendo FR-002
(artefacto consumível, independente de qualquer app) e preparando uma futura sync externa
(`/design-sync` espera um `dist`). O ponto sensível em React 19/RSC é a **diretiva `'use client'`**:
se não for preservada no build, os componentes Radix quebram como Server Components. `transpilePackages`
garante que o Next reprocessa o pacote e respeita as fronteiras client/server.

**Alternativas**:
- **Consumir a `src` diretamente via `transpilePackages`** (sem build) → simples, mas não entrega o
  artefacto construível (FR-002) nem serve consumidores externos/sync. Mantemos o build.
- **Rollup/Vite library mode** → mais configuração para o mesmo resultado.

**Gate**: `npm run build -w @infolure/design-system` produz `dist` com `.js` + `.d.ts` e as diretivas
`'use client'` intactas (verificável por `grep` no `dist`).

---

## 3. Tailwind v4 no monorepo: tokens centralizados + `@source` (o ponto crítico)

**Decisão**: O pacote é a **fonte única dos tokens**: `src/tokens.css` contém o `@theme` com a paleta
branco/azul/verde (migrado do `admin.css` da 003) e é exportado como `@infolure/design-system/tokens.css`.
Cada app **gera as suas próprias utilities** escaneando o pacote: no CSS da app (ex.: o `admin.css`
passa a) `@import 'tailwindcss';`, `@import '@infolure/design-system/tokens.css';` e
`@source '../../../packages/design-system/src';` (ou o caminho do `dist`). As utilities saem do
Tailwind de cada app; os tokens vêm do pacote.

**Rationale**: No Tailwind v4 as utilities são geradas por **scanning de conteúdo** e o `node_modules`
é **excluído por omissão** — logo, as classes usadas só dentro do pacote **não** seriam geradas pela
app sem `@source`. Esta abordagem cumpre **FR-003/SC-004** (tokens numa só fonte → mudar uma cor
propaga a todos os consumidores) e **FR-009/SC-006** (cada app gera um único CSS, sem duplicação da
biblioteca de utilities entre apps). Mantém também o **isolamento por rota** da 003 (o `@import` vive
no `admin.css`, carregado só no `/admin`).

**Alternativas**:
- **Enviar CSS pré-compilado do pacote** (utilities embutidas) → duplicaria utilities entre apps e
  acoplaria o CSS aos tokens no momento do build do pacote, quebrando a fonte única. Rejeitado.
- **Preset de Tailwind partilhado** (`@config`) → no v4 o `@theme` em CSS já é o mecanismo idiomático;
  um preset JS é desnecessário.

**Gate**: alterar `--primary` **só** em `tokens.css` muda o azul no admin após rebuild (prova SC-004).

---

## 4. Storybook: builder Vite, dentro do pacote, a carregar os tokens

**Decisão**: Storybook (**builder Vite**, framework React) em `packages/design-system/.storybook`.
O `preview` importa `@infolure/design-system/tokens.css` + `@import 'tailwindcss'` com `@source` das
stories/componentes, para as stories renderizarem com o tema real. Uma `*.stories.tsx` por componente,
cobrindo variantes/estados (ex.: button: default/success/destructive/outline/secondary/ghost/link;
badge: success/secondary/muted/destructive; dialog; table; etc.).

**Rationale**: Cumpre FR-006/SC-003 (catálogo navegável com variantes/estados). O builder Vite integra
Tailwind v4 via `@tailwindcss/postcss`/plugin Vite sem o peso do Webpack. Ficar **dentro do pacote**
mantém as stories junto dos componentes e posiciona o repo para uma futura `/design-sync` limpa
(que prefere o `.storybook` no pacote do design system).

**Alternativas**:
- **App Storybook dedicada** (`apps/storybook`) → mais boilerplate; desnecessário com 1 pacote.
- **Builder Webpack** → mais lento, sem vantagem.

**Gate**: `npm run build-storybook` (ou `storybook dev`) lista todos os componentes com as suas stories.

---

## 5. Migração do admin sem regressões (paridade)

**Decisão**: Mover `apps/web/components/ui/*` (os 8 componentes) e `lib/utils.ts` para o pacote;
**apagar** as cópias locais. Reescrever os imports do admin de `'../../components/ui/x'` para
`'@infolure/design-system'` (barrel) — incluindo `AdminNav`, `RowActions`, `LureEditForm` e as páginas.
O `admin.css` deixa de definir os tokens e passa a `@import` os do pacote + `@source`. O `States.tsx`
**fica na app** (é partilhado com páginas públicas que não carregam Tailwind; não é um componente do
design system).

**Rationale**: Cumpre SC-001 (sem cópias duplicadas) e SC-002 (paridade) — como o código dos
componentes e os tokens são os mesmos, a aparência mantém-se. A verificação é a suite Playwright do
admin permanecer verde + inspeção visual (quickstart).

**Alternativas**:
- **Manter cópias e só "espelhar"** → contraria o objetivo e SC-001.
- **Mover também o `States.tsx`** → regrediria as páginas públicas (sem Tailwind), como na 003.

**Gate**: `grep` não encontra `components/ui/` em `apps/web`; Playwright do admin verde.

---

## 6. Disponibilidade ao público + adoção-piloto (sem regredir o resto)

**Decisão**: Tornar o pacote importável no frontend público e fazer **uma** adoção-piloto: substituir
**um** elemento de UI numa página pública por um componente do pacote, carregando os tokens + Tailwind
**apenas no âmbito dessa página** (mesma técnica de isolamento por rota da 003 — um CSS de entrada
próprio importado no segmento da página-piloto). As restantes páginas públicas ficam **intocadas**
(FR-011).

**Rationale**: Cumpre FR-007/SC-005 (público pode consumir o DS, provado por piloto) sem o risco de
um redesenho global do público (fora de âmbito). O isolamento por rota evita aplicar o Preflight do
Tailwind ao público todo, protegendo as páginas existentes.

**Alternativas**:
- **Ativar Tailwind globalmente no root layout** → arriscaria regressões em todo o público
  (Preflight), exatamente o que a 003 evitou. Rejeitado.
- **Adiar US3** → mas a disponibilidade + piloto é barata e prova a promessa "admin e público".

**Nota de verificação**: confirmar no `next build` que a página-piloto renderiza estilizada e que
catálogo/detalhe/perfil públicos permanecem idênticos (Playwright público verde).
