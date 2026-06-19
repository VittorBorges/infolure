# Quickstart — Feature 007 (Sessão do utilizador no painel admin)

Guia de validação end-to-end. Prova que a identidade aparece no painel e que o logout funciona.
Detalhes de implementação ficam em `tasks.md`; aqui só cenários executáveis e resultados esperados.

## Pré-requisitos

- Backend a correr (`apps/api`) com PostgreSQL local (container `infolure-pg`, porta 5433) — ver
  `CLAUDE.md` para a connection string de desenvolvimento.
- Frontend a correr (`apps/web`, Next.js) com Supabase Auth configurado.
- Uma conta de **administrador** (role `admin`) com sessão iniciável via `/login`.

## Cenário 1 — `GET /v1/me` devolve a identidade (contrato)

Referência: [contracts/me-api.yaml](./contracts/me-api.yaml), [data-model.md](./data-model.md).

1. Obter um JWT de uma sessão autenticada (via login no frontend ou fluxo de teste).
2. Chamar `GET /v1/me` com `Authorization: Bearer <jwt>`.
3. **Esperado**: `200` com `{ email, username, display_name, role, avatar_url }`; **sem** campo `id`.
4. Chamar `GET /v1/me` **sem** token → **esperado**: `401`.

## Cenário 2 — Identidade visível no painel (US1 / FR-001..FR-004, FR-010)

1. Iniciar sessão como administrador e abrir `/admin`.
2. **Esperado**: o cabeçalho do painel mostra o nome (ou email, se sem nome) e a função (`admin`).
3. Navegar para `/admin/lures`, `/admin/species`, etc.
4. **Esperado**: a identidade mantém-se visível e consistente em todas as páginas; nunca aparece um UUID.

## Cenário 3 — Terminar sessão (US2 / FR-005..FR-009, SC-003/SC-004)

1. No painel, acionar o botão **"Terminar sessão"**.
2. **Esperado**: indicação de progresso; o botão não pode ser acionado repetidamente; em < 3 s a sessão
   é encerrada e o utilizador é encaminhado para `/login`.
3. Após o logout, tentar abrir `/admin` (ou uma subpágina) diretamente.
4. **Esperado**: o acesso é recusado e é pedida nova autenticação (sem acesso indevido).

## Cenário 4 — Sessão expirada/sem nome (edge cases)

1. Com uma conta **sem `display_name`/`username`**, abrir `/admin`.
2. **Esperado**: é apresentado o **email** como identificador (nunca o UUID).
3. Com uma sessão inválida/expirada, abrir `/admin`.
4. **Esperado**: tratado como não autenticado (redireção para `/login`), sem mostrar identidade obsoleta.

## Validação automatizada (referência)

- Backend: teste de integração `GET /v1/me` — `200` autenticado (com `role`) e `401` sem token
  (`apps/api/tests/Infolure.IntegrationTests/Users/MeTests.cs`).
- Frontend: e2e *skip-gated* — identidade visível no painel e logout que redireciona e bloqueia acesso
  (`apps/web/tests/e2e/admin-session.spec.ts`).
