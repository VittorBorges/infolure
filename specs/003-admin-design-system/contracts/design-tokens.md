# Contrato de Design — Tokens & Inventário de Componentes (Admin)

Contrato interno do frontend para a área `/admin`. Define os **tokens de tema** (a "interface" entre
a paleta e os componentes) e o **inventário de componentes shadcn** com o mapeamento às páginas
existentes. Não é um contrato de rede — o contrato de API mantém-se em
`specs/002-admin-indexing-audit/contracts/admin-api.yaml`.

## 1. Tokens de tema (tema claro fixo) — Paleta "Azul SaaS + Verde fresco"

Definidos como CSS variables em `apps/web/app/admin/admin.css` e expostos ao Tailwind via
`@theme inline`. **Sem** variante `.dark` e **sem** `@media (prefers-color-scheme)` (FR-002/SC-004).
Valores concretos escolhidos (paleta A). Equivalência Tailwind entre parênteses.

| Token (shadcn)            | Papel                                   | Valor (hex)         | Tailwind     |
|---------------------------|-----------------------------------------|---------------------|--------------|
| `--background`            | Fundo da área admin                     | `#FFFFFF`           | white        |
| `--foreground`            | Texto principal                         | `#0F172A`           | slate-900    |
| `--card`                  | Superfície de cartões                   | `#FFFFFF`           | white        |
| `--card-foreground`       | Texto sobre cartões                     | `#0F172A`           | slate-900    |
| `--primary`               | Ações primárias, links                  | `#2563EB`           | blue-600     |
| `--primary-foreground`    | Texto sobre primário                    | `#FFFFFF`           | white        |
| `--secondary`             | Superfície secundária / botão neutro    | `#F1F5F9`           | slate-100    |
| `--secondary-foreground`  | Texto sobre secundário                  | `#0F172A`           | slate-900    |
| `--muted`                 | Áreas atenuadas (cabeçalhos de tabela)  | `#F1F5F9`           | slate-100    |
| `--muted-foreground`      | Texto secundário / metadados            | `#64748B`           | slate-500    |
| `--accent`                | Hover neutro de componentes (shadcn)    | `#F1F5F9`           | slate-100    |
| `--success`               | **Estado positivo** (badge "ativo")     | `#16A34A`           | green-600    |
| `--success-foreground`    | Texto sobre verde                       | `#FFFFFF`           | white        |
| `--destructive`           | Ação irreversível (eliminação RGPD)     | `#DC2626`           | red-600      |
| `--border`                | Limites                                 | `#E2E8F0`           | slate-200    |
| `--input`                 | Bordas de campos                        | `#E2E8F0`           | slate-200    |
| `--ring`                  | Anel de foco de teclado                 | `#2563EB`           | blue-600     |
| `--radius`                | Raio de cantos (moderno)                | `0.625rem` (10px)   | —            |

**Regras de uso semântico** (FR-003/FR-007/SC-005):
- Ação primária e elementos interativos → **azul** (`--primary`); anel de foco também azul (`--ring`).
- Estados positivos (ativo, indexável, sucesso) → **verde** (`--success`) em `Badge`/realces.
- `--accent` fica neutro (slate-100) para que os hovers dos componentes shadcn (ghost/outline)
  permaneçam discretos; o verde é aplicado intencionalmente via `--success`, não em todos os hovers.
- Apenas ações destrutivas irreversíveis → vermelho (`--destructive`); nunca para "desativar"
  reversível.
- Contraste de texto normal ≥ AA (SC-006). (`#0F172A` sobre `#FFFFFF` ≈ 17:1; `#FFFFFF` sobre
  `#2563EB` ≈ 5.2:1 — ambos passam AA.)

### 1b. Estilo de layout — "Sidebar moderna com ícones"

- **Sidebar** fixa à esquerda (largura ~240px), fundo branco com borda direita (`--border`),
  itens de navegação com **ícone (`lucide-react`) + rótulo**; item ativo destacado a azul
  (texto/realce `--primary`) com fundo `--secondary` subtil.
- **Cabeçalho** fino no topo da área de conteúdo (título da secção + identificação do utilizador).
- **Dashboard** em **cartões** (`Card`) com cantos arredondados (`--radius`) e **sombra subtil**
  (ex.: `shadow-sm`), espaçamento generoso.
- Aparência geral: limpa, tipo dashboard SaaS moderno; tabelas com cabeçalho em `--muted` e linhas
  com separador `--border`.

## 2. Inventário de componentes shadcn (mínimo viável)

| Componente | Origem        | Onde é usado |
|------------|---------------|--------------|
| `button`   | shadcn        | Filtros, ações de linha, paginação, formulários |
| `card`     | shadcn        | Cartões de métricas do dashboard |
| `table`    | shadcn        | Listagem de recursos e tabela de auditoria |
| `input`    | shadcn        | Campo de pesquisa, campos de formulário |
| `select`   | shadcn (Radix)| Filtro `include` (recursos) e `action` (auditoria) |
| `label`    | shadcn        | Rótulos de campos de formulário |
| `dialog`   | shadcn (Radix)| Modal de aviso RGPD em `RowActions` |
| `badge`    | shadcn        | Estados: ativo/inativo, eliminado, indexável, pendente |
| `cn()`     | `lib/utils.ts`| Helper de composição de classes (clsx + tailwind-merge) |

## 3. Mapeamento página → componentes (preservando comportamento)

| Página/arquivo (apps/web)        | Antes (inline)                        | Depois (design system) |
|----------------------------------|---------------------------------------|------------------------|
| `app/admin/layout.tsx`           | `<aside>`/`<nav>` com style inline    | Shell + nav com tokens; importa `admin.css`; **gating de sessão inalterado** |
| `app/admin/page.tsx`             | `Card` local com style inline         | `Card` shadcn + `Badge` para estados; **métricas e estados de erro/403 mantidos** |
| `app/admin/[resource]/page.tsx`  | `<table>`/`<input>`/`<select>` inline | `Table`/`Input`/`Select`/`Button`/`Badge`; **filtros `q`/`include`/`page` e paginação mantidos** |
| `app/admin/audit/page.tsx`       | `<table>`/`<select>` inline           | `Table`/`Select`/`Button`; **filtro `action` e paginação mantidos** |
| `components/admin/RowActions.tsx`| `<button>`/`<div>` aviso inline       | `Button`/`Dialog`/`Badge`; **server actions e fluxo RGPD (soft-delete vs erase) inalterados** |
| `components/ui/States.tsx`       | style inline                          | Estados loading/empty/error no design system (Princípio V) |

## 4. Invariantes (o que NÃO pode mudar)

- Endpoints, parâmetros e formas de resposta consumidos (`adminFetch`, server actions) — inalterados.
- Comportamento de gating/redirect para `/login` quando sem sessão.
- Semântica do fluxo RGPD: distinção visível entre **soft-delete (reversível)** e
  **eliminação RGPD (irreversível)**.
- Frontend público: zero alterações de aparência/comportamento.
