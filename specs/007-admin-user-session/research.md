# Research — Feature 007 (Sessão do utilizador no painel admin)

Decisões técnicas para a feature, com base no fluxo de autenticação já existente (Supabase Auth +
proteção de rota no layout `/admin` + `adminFetch` com JWT). Não há NEEDS CLARIFICATION em aberto.

---

## §1 — Origem da identidade (nome/email/função)

- **Decisão**: criar um endpoint **`GET /v1/me`** no backend que devolve `{ email, username,
  display_name, role, avatar_url }`, resolvido a partir do claim `sub` do JWT (mesmo padrão do
  `PATCH`/`DELETE /v1/me` já existentes no `ProfileController`). O painel consome-o **server-side** no
  `app/admin/layout.tsx` via `adminFetch` (que já anexa o JWT).
- **Rationale**: a **função (role)** é autoritativa na base de dados (`users.role`), não num claim
  fiável do JWT do cliente. O Princípio III proíbe o frontend de assumir formatos/claims não
  declarados no contrato. Um endpoint de leitura único é a forma mais simples e correta de obter
  nome+email+função de forma consistente (FR-001..FR-003, FR-010).
- **Alternativas consideradas**:
  - *Ler o email do `session.user` e a role de um claim do JWT no cliente*: evita um endpoint, mas
    acopla o frontend a claims não contratados e pode divergir da role real na BD. Rejeitado (III/IV).
  - *Reutilizar `GET /v1/admin/users?q=` e filtrar o próprio*: indireto, expõe dados de outros e não é
    semântico para "o meu perfil". Rejeitado (I/V).

## §2 — Mecanismo de terminar sessão (logout)

- **Decisão**: logout **no cliente** com `getSupabaseBrowserClient().auth.signOut()` (padrão já usado em
  `components/settings/SettingsForm.tsx`), seguido de redireção para **`/login`**. O botão fica
  desativado enquanto decorre e mostra estado; em erro, apresenta mensagem compreensível e não deixa a
  UI em estado ambíguo (FR-006..FR-009).
- **Rationale**: a sessão é gerida pelo Supabase no browser; `signOut` invalida o token local e a
  sessão. A proteção de rota existente (layout server-side redireciona para `/login` sem sessão; backend
  responde 401/403) garante que, após o logout, qualquer acesso a área protegida exige nova autenticação
  (FR-006/SC-004) — sem necessidade de lógica nova de invalidação.
- **Alternativas consideradas**:
  - *Logout server-side via server action*: a sessão Supabase vive no cliente (cookies geridos por
    `@supabase/ssr`); o `signOut` no browser é o caminho suportado e mais simples. Mantém-se a opção de,
    no futuro, limpar cookies no servidor se necessário. Rejeitado por agora (YAGNI).
  - *Redirecionar para `/` (início) em vez de `/login`*: a spec pede "página de entrada/autenticação";
    `/login` é mais explícito para um backoffice. Decisão: `/login`.

## §3 — Local da UI no painel

- **Decisão**: adicionar uma **zona de cabeçalho** no `app/admin/layout.tsx` (acima/junto ao conteúdo
  principal) que renderiza um componente cliente novo **`AdminUserMenu`** com nome/email, a função (como
  `Badge`) e o botão "Terminar sessão". O layout (server) passa a identidade obtida em §1 como props.
- **Rationale**: o layout envolve todas as páginas protegidas → cumpre FR-001/SC-001 (visível em 100%
  das páginas) com um único ponto de integração. Reutiliza o design system (`Button`, `Badge`).
- **Alternativas consideradas**:
  - *Colocar dentro do `AdminNav` (sidebar)*: viável, mas o cabeçalho é mais convencional para a
    identidade/logout e não polui a navegação. Aceitável como alternativa de layout.
  - *Dropdown/Avatar*: o design system **não** exporta `DropdownMenu`/`Avatar` hoje. Para não introduzir
    dependências (Princípio I), usa-se identidade inline + botão. Um dropdown pode vir depois.

## §4 — Estados, acessibilidade e observabilidade

- **Decisão**: tratar estados de carregamento e erro do logout no cliente (Princípio V); o botão e o
  bloco de identidade têm rótulos acessíveis e são operáveis por teclado (FR-011). O `GET /v1/me`
  regista início/fim/resultado+latência (Princípio II) à semelhança dos restantes endpoints.
- **Rationale**: alinhamento direto com os princípios II e V e com os requisitos FR-008/FR-009/FR-011.

## §5 — Contrato e tipos

- **Decisão**: documentar `GET /v1/me` em `contracts/me-api.yaml` (schema `Me`) e derivar o tipo do
  frontend a partir do contrato (Princípio III). Sem alterações de schema de base de dados.
- **Rationale**: mantém a borda frontend↔backend explícita e versionada.
