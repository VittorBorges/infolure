# Research — Feature 003: Design System do Backoffice Admin

Decisões técnicas que resolvem as incógnitas do plano. Formato: Decisão / Rationale / Alternativas.
Fontes verificadas localmente: `apps/web/node_modules/next/dist/docs/01-app/01-getting-started/11-css.md`
e `.../02-guides/tailwind-v3-css.md` (Next 16 documenta o Tailwind v4 como caminho recomendado).

---

## 1. Versão do Tailwind: v4 (não v3)

**Decisão**: Tailwind CSS **v4** via `@import 'tailwindcss'` no CSS + plugin `@tailwindcss/postcss`
no `postcss.config.mjs`. Sem `tailwind.config.js` (config CSS-first com `@theme`).

**Rationale**: É o caminho **oficial** documentado pelo Next 16 (`11-css.md`): `npm install -D
tailwindcss @tailwindcss/postcss`, `postcss.config.mjs` com `'@tailwindcss/postcss': {}`,
`@import 'tailwindcss'` no global CSS. O guia do v3 está marcado como legado ("for broader browser
support"), só recomendado para navegadores muito antigos — não é o nosso caso. O v4 é também a base
atual do shadcn/ui com React 19.

**Alternativas**:
- Tailwind v3 (`@tailwind base/components/utilities` + `tailwind.config.js`) → legado segundo os
  próprios docs; mais ficheiros de config, sem vantagem aqui.
- CSS Modules/inline (estado atual) → não dá um design system consistente (motivo da feature).

---

## 2. Isolamento ao admin: CSS importado só no `app/admin/layout.tsx`

**Decisão**: Criar `app/admin/admin.css` contendo `@import 'tailwindcss'` + os tokens de tema, e
importá-lo **apenas** em `app/admin/layout.tsx`. O root layout (`app/layout.tsx`) continua a importar
só `globals.css` (inalterado). O frontend público **nunca** carrega o CSS do Tailwind.

**Rationale**: O `11-css.md` confirma que "Global styles can be imported into any layout, page, or
component inside the `app` directory" e que o CSS é **code-split por rota** no build de produção
("minimal amount of CSS loaded for a route"). Logo, importar o design system no layout do admin
limita o Preflight (reset agressivo do Tailwind que zera margens, normaliza headings/listas, etc.)
e os tokens às rotas `/admin/*`. Isto satisfaz **FR-008/SC-003 (público inalterado) por construção**,
sem ter de auditar e migrar todas as páginas públicas, e alinha com o Princípio I (menor superfície,
menor risco).

**Alternativas**:
- Importar Tailwind no root layout → o Preflight aplicar-se-ia ao site inteiro; as páginas públicas
  (que misturam estilos inline e defaults do browser) poderiam regredir visualmente. Rejeitado:
  mais risco e mais verificação.
- Desligar o Preflight globalmente → o shadcn assume o Preflight; desligá-lo degrada os componentes.
- Prefixar todas as classes / usar `important` selector → complexidade desnecessária dado que o
  isolamento por rota já resolve.

**Nota de verificação**: confirmar no `next build`/`next dev` que (a) as classes Tailwind funcionam
em `/admin`, e (b) uma página pública (ex.: catálogo) permanece pixel-equivalente. A ordem de CSS
pode diferir entre dev e prod — validar no build (recomendação do próprio doc).

---

## 3. shadcn/ui: instalação e tema (light forçado, branco/azul/verde)

**Decisão**: Adotar shadcn/ui no estilo "copy-in": `components.json` + `lib/utils.ts` (`cn()` =
`clsx` + `tailwind-merge`) + componentes em `components/ui/`. Tentar `npx shadcn@latest init` e
`add`; se o CLI tropeçar no Next 16 (ver AGENTS.md: "this is NOT the Next.js you know"), adicionar
os componentes **manualmente** a partir da documentação (são ficheiros pequenos e autocontidos).
Tema definido por **CSS variables** em `admin.css`, expostas ao Tailwind via `@theme inline`.

Paleta (tema claro fixo — **sem** bloco `.dark` e **sem** `@media (prefers-color-scheme)`):
- `--background`/`--card` → branco; `--foreground` → cinза-escuro near-black para contraste AA.
- `--primary` → **azul** (ações primárias, links, foco). `--primary-foreground` → branco.
- `--accent`/estado positivo/sucesso → **verde** (badges "ativo", confirmações).
- `--destructive` → vermelho (apenas para ações irreversíveis, ex.: eliminação RGPD), mantido
  distinto do verde/azul.
- `--border`/`--input`/`--ring` → derivados neutros + azul no `ring` de foco.

**Rationale**: O modelo copy-in do shadcn mantém os componentes versionados no repo (transparência,
sem dependência de runtime fechada) e permite ajustar à paleta pedida. CSS variables são o mecanismo
nativo do shadcn para temas; fixar só o tema claro cumpre FR-002/SC-004 de forma simples. Adotar
**apenas** os componentes necessários respeita o Princípio I.

**Componentes a adotar** (mínimo viável para as páginas existentes): `button`, `card`, `table`,
`input`, `select`, `label`, `dialog`, `badge`. (Inventário e mapeamento → `contracts/design-tokens.md`.)

**Alternativas**:
- Instalar o kit shadcn completo → contraria YAGNI; só se adota o que as páginas usam.
- Tema via classe `.dark` toggle → desnecessário (requisito é light-only).
- Outras libs (MUI, Mantine, Chakra) → não foram pedidas; maior peso/runtime e fogem ao pedido
  explícito de shadcn/ui.

---

## 4. Compatibilidade React 19 / Next 16

**Decisão**: Usar `lucide-react` para ícones e primitivos Radix UI nas versões compatíveis com
React 19 (peer `react@19`). Os componentes admin client (`RowActions`) mantêm `'use client'`,
`useRouter`, `useTransition` e as server actions atuais — só muda a apresentação.

**Rationale**: Radix UI e lucide-react suportam React 19; o shadcn distribui componentes assentes
nesses primitivos. Não há mudança de arquitetura de dados/efeitos — reduz risco de regressão
(Princípio IV) e mantém o contrato com o backend intacto (Princípio III).

**Alternativas**:
- Headless UI / componentes próprios → mais trabalho, sem o ecossistema do shadcn.

**Nota de verificação**: ao instalar, confirmar que não há conflito de peers com `react@19.2.4`;
se algum primitivo Radix exigir flag de legacy-peer-deps, registar em `quickstart.md`.

---

## 5. Preservação funcional e testes

**Decisão**: A refatoração é **apenas de apresentação**. Mantêm-se: gating de sessão em
`app/admin/layout.tsx`, `adminFetch`/`AdminResult`, as server actions (`setActiveAction`,
`softDeleteAction`, `restoreAction`, `setIndexableAction`, `eraseUserAction`), os filtros
(`q`/`include`/`action`/`page`) e o fluxo RGPD em `RowActions`. Validação: a suite Playwright
existente (`tests/e2e/indexing.spec.ts`) tem de continuar verde; acrescenta-se um **smoke E2E** que
abre `/admin` autenticado e confirma a renderização do dashboard e da navegação no novo tema.

**Rationale**: Princípio IV — o critério de aceitação é "teste verde que prova o comportamento". Como
mudanças visuais não se testam unitariamente de forma barata, ancoramos a garantia na preservação
funcional (E2E) e na validação manual do `quickstart.md`.

**Alternativas**:
- Testes de regressão visual (screenshots) → valiosos mas pesados; fora do mínimo desta feature.
