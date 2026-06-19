# Implementation Plan: Sessão do utilizador no painel de administração (identidade + terminar sessão)

**Branch**: `007-admin-user-session` | **Date**: 2026-06-19 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `/specs/007-admin-user-session/spec.md`

## Summary

Mostrar de forma persistente, no painel `/admin`, a identidade do utilizador autenticado (nome ou
email + função) e oferecer um botão para terminar sessão. Abordagem: um novo endpoint de leitura
**`GET /v1/me`** no backend (fonte de verdade para nome/email/função, resolvido pelo claim `sub` do
JWT) consumido server-side pelo layout do painel; um componente cliente (`AdminUserMenu`) que apresenta
a identidade e faz **logout via Supabase `signOut`** no browser, redirecionando para `/login`. Reutiliza
a autenticação Supabase e a proteção de rota já existentes; sem alterações de schema.

## Technical Context

**Language/Version**: C# / **.NET 10** (backend, ASP.NET Core); **TypeScript** / **Next.js 16** (App Router) no frontend

**Primary Dependencies**: ASP.NET Core, EF Core (backend); `@supabase/ssr` (sessão/JWT), `@infolure/design-system` (Button/Badge), App Router (frontend)

**Storage**: PostgreSQL — tabela `users` (Email/Username/DisplayName/Role); **leitura apenas**, sem migration

**Testing**: xUnit + WebApplicationFactory (integração backend); Playwright e2e *skip-gated* (frontend)

**Target Platform**: Web (browser) + servidor Linux (Azure West Europe)

**Project Type**: Web application (monorepo `apps/api` + `apps/web`)

**Performance Goals**: identidade visível no carregamento do painel; logout concluído < 3 s (SC-003)

**Constraints**: reutilizar a sessão/autenticação Supabase existentes; nunca expor UUID (FR-004); controlos acessíveis por teclado e rotulados (FR-011 / Princípio V); sem segredos no cliente

**Scale/Scope**: 1 endpoint backend novo (`GET /v1/me`), 1 componente cliente novo + alteração ao layout admin; poucos utilizadores administradores

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

- **I. Simplicidade Primeiro (YAGNI)**: ✅ Um único endpoint de leitura e um componente; reutiliza auth,
  proteção de rota e `adminFetch` existentes. Sem novas dependências, sem schema novo. O endpoint
  `GET /v1/me` é justificado por necessidade real (a função é autoritativa no backend e não deve ser
  inferida de claims não declarados — ver Princípio III).
- **II. Observabilidade por Padrão**: ✅ `GET /v1/me` regista início/fim/resultado+latência e erros com
  contexto (sem PII sensível além do necessário); o resultado do logout (sucesso/erro) é observável no
  cliente. Sem novos segredos em logs.
- **III. Contratos Explícitos**: ✅ `GET /v1/me` é adicionado ao contrato OpenAPI (`contracts/me-api.yaml`)
  como fonte de verdade; os tipos do frontend derivam do contrato. O frontend não assume claims do JWT
  para a função — usa o contrato.
- **IV. Qualidade Testável**: ✅ Teste de integração para `GET /v1/me` (200 autenticado com função;
  401 sem token) e e2e *skip-gated* para identidade visível + logout que invalida a sessão.
- **V. Experiência do Utilizador Consistente**: ✅ Estados de carregamento/erro do logout tratados; botão
  desativado durante o processo; mensagens compreensíveis; rótulos e navegação por teclado.

**Resultado**: PASS — sem violações. "Complexity Tracking" não aplicável.

## Project Structure

### Documentation (this feature)

```text
specs/007-admin-user-session/
├── plan.md              # Este ficheiro (/speckit-plan)
├── research.md          # Fase 0 (/speckit-plan)
├── data-model.md        # Fase 1 (/speckit-plan)
├── quickstart.md        # Fase 1 (/speckit-plan)
├── contracts/           # Fase 1 (/speckit-plan)
│   └── me-api.yaml
├── checklists/
│   └── requirements.md  # criado em /speckit-specify
└── tasks.md             # Fase 2 (/speckit-tasks — NÃO criado por /speckit-plan)
```

### Source Code (repository root)

```text
apps/api/src/Infolure.Api/
└── Features/Users/
    ├── ProfileController.cs      # + GET /v1/me (Authorize UserPolicy; resolve por claim "sub")
    ├── ProfileService.cs         # + GetMeAsync(sub) → MeDto
    └── (DTOs)                    # + MeDto (email, username, display_name, role, avatar_url)

apps/api/tests/Infolure.IntegrationTests/
└── Users/MeTests.cs              # GET /v1/me: 200 autenticado, 401 sem token

apps/web/
├── app/admin/layout.tsx          # busca GET /v1/me (server) e renderiza o cabeçalho com identidade
├── components/admin/
│   └── AdminUserMenu.tsx (NOVO)  # client: mostra nome/email + função (Badge) + botão "Terminar sessão"
├── lib/
│   ├── admin.ts                  # reutilizar adminFetch; tipo Me
│   └── auth-actions.ts (NOVO?)   # logout no cliente (Supabase signOut) + redireção /login
└── tests/e2e/admin-session.spec.ts (skip-gated)  # identidade visível + logout
```

**Structure Decision**: Web application no monorepo existente. Backend em vertical slice
`Features/Users` (estende o `ProfileController` já existente); frontend no App Router, encaixando a
identidade no `app/admin/layout.tsx` (zona de cabeçalho acima do conteúdo) via um componente cliente
novo. Reutiliza `adminFetch`, `getSupabaseBrowserClient` e o design system — nada de estrutura nova.

## Complexity Tracking

> Não aplicável — Constitution Check passou sem violações.
