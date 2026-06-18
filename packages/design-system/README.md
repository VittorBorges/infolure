# @infolure/design-system

Design system partilhado do Infolure (Feature 004). Componentes **shadcn/ui** sobre **Tailwind v4**,
tema claro fixo com a paleta **branco / azul (`#2563EB`) / verde (`#16A34A`)**. Fonte única de
componentes e tokens, consumida pelo backoffice admin e (piloto) pelo frontend público.

## Conteúdo

- **Componentes** (`src/components/`): `Button`, `Card`, `Table`, `Input`, `Select`, `Label`,
  `Dialog`, `Badge` — barril em `src/index.ts`. Helper `cn()`.
- **Tokens** (`src/tokens.css`): `@theme` com a paleta/tema (fonte única de verdade).
- **Catálogo**: Storybook (`npm run storybook` / `build-storybook`).

## Como consumir numa app do monorepo

1. Dependência (workspace): `"@infolure/design-system": "*"`.
2. **Next.js**: `transpilePackages: ['@infolure/design-system']` no `next.config.ts`.
3. **CSS de entrada** da app (ou do segmento de rota onde se usa o DS):

   ```css
   @import 'tailwindcss';
   @import 'tw-animate-css';                       /* animações de Dialog/Select */
   @import '@infolure/design-system/tokens.css';   /* tokens (fonte única) */
   @source '<caminho-relativo>/packages/design-system/src';  /* gera as utilities do pacote */
   ```

   > O `@source` é **obrigatório**: o Tailwind v4 exclui `node_modules` do scan, por isso sem ele
   > as classes usadas pelos componentes do pacote não são geradas.

4. Importar e usar:

   ```tsx
   import { Button, Card, Badge } from '@infolure/design-system';
   ```

## Idioma de estilo

Utilitários Tailwind com tokens semânticos: `bg-primary` (azul), `bg-success` (verde),
`bg-destructive` (vermelho, só ações irreversíveis), `text-muted-foreground`, `border`, `ring-ring`
(foco azul), `rounded-lg` (`--radius`). Os componentes expõem variantes via props
(`<Button variant="success">`, `<Badge variant="muted">`).

## Build

- `npm run build` → `dist/` (ESM + `.d.ts`). A diretiva `"use client"` é garantida no topo do bundle
  por `scripts/postbuild.mjs` (o esbuild remove diretivas de módulo ao bundlar).

## Versionamento

A versão deste pacote é **independente** da versão do produto (que segue o versionamento por feature
`specs/NNN-*`). Não representa a versão do produto.
