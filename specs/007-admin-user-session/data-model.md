# Data Model — Feature 007 (Sessão do utilizador no painel admin)

**Sem alterações de schema.** A feature é de leitura: reutiliza a tabela `users` existente e a sessão
Supabase. Não há migrations.

---

## Entidades (existentes, reutilizadas)

### User (`users`) — leitura apenas

Campos relevantes para a identidade apresentada no painel:

| Campo | Tipo | Uso na feature |
|-------|------|----------------|
| `id` | uuid | **NÃO** exposto na UI (FR-004) |
| `email` | text? | identificador legível de recurso (FR-002) |
| `username` | text? | identificador legível (fallback/handle) |
| `display_name` | text? | nome preferido para apresentação (FR-002) |
| `avatar_url` | text? | opcional (não obrigatório nesta feature) |
| `role` | text (`user`\|`admin`) | função apresentada no painel (FR-003) |

Resolução: o utilizador é obtido pelo claim `sub` do JWT (mesmo padrão do `PATCH`/`DELETE /v1/me`).

### Sessão (Supabase Auth) — gerida no cliente

Estado de autenticação (token/validade) gerido por `@supabase/ssr`. O logout (`signOut`) invalida-a;
a proteção de rota existente trata a ausência/expiração (redireção para `/login`; backend 401/403).

---

## Projeção de saída — `MeDto` (novo DTO, sem persistência)

Forma devolvida por `GET /v1/me` (snake_case na serialização):

| Campo | Origem | Notas |
|-------|--------|-------|
| `email` | `User.Email` | pode ser nulo |
| `username` | `User.Username` | pode ser nulo |
| `display_name` | `User.DisplayName` | pode ser nulo |
| `role` | `User.Role` | `user` \| `admin` |
| `avatar_url` | `User.AvatarUrl` | opcional |

**Nome a apresentar (regra de derivação na UI, FR-002)**: `display_name` → senão `username` → senão
`email`. Nunca o `id`.

---

## Regras de validação / comportamento (derivadas dos FRs)

| Regra | Origem | Onde |
|-------|--------|------|
| Identidade só para sessão válida; senão tratado como não autenticado | FR-010 | layout `/admin` + `Authorize` no endpoint |
| Nunca expor UUID como identidade | FR-004 | `MeDto` não inclui `id`; UI usa nome/email |
| `GET /v1/me` exige autenticação (401 sem token) | FR-006/010 | `[Authorize(UserPolicy)]` |
| Logout invalida a sessão e impede acesso subsequente | FR-006 | Supabase `signOut` + proteção de rota |
| Função apresentada (valor neutro se ausente) | FR-003 / edge case | UI (`Badge`) |
