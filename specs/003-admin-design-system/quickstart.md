# Quickstart — Feature 003: Design System do Backoffice Admin

Guia de validação. Prova que o painel admin passa a usar o design system (tema claro, branco/azul/
verde) **sem** regressões funcionais e **sem** afetar o frontend público. Detalhes de tokens e
componentes em [contracts/design-tokens.md](contracts/design-tokens.md); o modelo de dados/contrato
de API mantém-se nos artefactos da Feature 002.

## Pré-requisitos

- Backend a correr (`apps/api`) com a BD/seed das features 001/002 — **inalterado** nesta feature.
- Frontend: dependências instaladas (`npm install` em `apps/web`, já com Tailwind v4 + shadcn) e
  `npm run dev` (:3000).
- Um utilizador com `role = 'admin'` na BD e sessão Supabase válida.

> Nota: se a instalação dos primitivos Radix gerar conflito de peers com `react@19.2.4`, registar
> aqui o comando usado (ex.: `npm install --legacy-peer-deps`).

## Build/setup a validar

| # | Passo | Comando | Esperado |
|---|-------|---------|----------|
| B1 | Tailwind v4 ativo | `npm run dev` | Sem erros de PostCSS; classes utilitárias aplicam em `/admin` |
| B2 | Build de produção | `npm run build` | Compila; CSS do admin é code-split (não entra nas rotas públicas) |
| B3 | Lint/types | `npm run lint` / `tsc --noEmit` | Sem novos erros |

## Cenários de validação (mapeados às user stories)

| # | Cenário | Como validar | Resultado esperado |
|---|---------|--------------|--------------------|
| US1 | Dashboard no design system | Abrir `/admin` autenticado | Cartões de métricas e navegação com tema claro, fundo branco, ações em azul; métricas corretas |
| US1 | Light forçado | Pôr o SO em modo escuro e reabrir `/admin` | O painel permanece em tema claro (sem dark) |
| US1 | Gating preservado | Abrir `/admin` sem sessão | Redireciona para `/login?returnUrl=/admin` |
| US1 | Estado de erro/403 | Aceder com utilizador não-admin | Mensagem "Sem acesso…" legível no novo estilo (não stack trace) |
| US2 | Listagem por recurso | `/admin/lures` (e `brands`/`species`/`users`) | Tabela do design system; filtros `q`/`include` e paginação funcionam |
| US2 | Distintivos de estado | Listagem com registos ativos/inativos/eliminados | `Badge` com cor correta (verde = ativo/positivo) |
| US2 | Ações de linha | Desativar/ativar, soft-delete, restaurar, toggle indexável (iscas) | Ações funcionam (refresh da lista); erros 409 mostram aviso legível |
| US2 | Fluxo RGPD | Em `/admin/users`, iniciar eliminação | Abre `Dialog` RGPD distinguindo soft-delete (reversível) de eliminação RGPD (irreversível) |
| US3 | Auditoria | `/admin/audit`, filtrar por ação e paginar | Tabela e filtros no design system; resultados corretos; paginação mantém filtros |
| — | **Público inalterado** | Abrir catálogo, detalhe de isca e perfil público | Aparência e comportamento idênticos ao anterior (sem efeito do Tailwind/Preflight) |
| — | Acessibilidade básica | Navegar `/admin` só com teclado | Foco visível (anel azul); contraste de texto ≥ AA |

## Regressão automatizada

- `npx playwright test` em `apps/web` — a suite existente (`tests/e2e/indexing.spec.ts`) **deve
  permanecer verde** (prova que o toggle de indexação e o público não regrediram).
- Smoke E2E novo: abrir `/admin` autenticado e confirmar a presença do dashboard/navegação.

## Critérios de saída (Definition of Done)

- [ ] Todas as páginas/componentes admin do inventário usam o design system (SC-001) — sem estilos inline remanescentes.
- [ ] Zero regressões funcionais (SC-002) — suite Playwright verde + cenários US1–US3 OK.
- [ ] Frontend público inalterado (SC-003).
- [ ] Tema claro em 100% das páginas admin, independente do SO (SC-004).
- [ ] Azul = ações / Verde = positivo, de forma consistente (SC-005).
- [ ] Contraste de texto normal ≥ AA (SC-006).
