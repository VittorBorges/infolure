# Feature Specification: Design System do Backoffice Admin

**Feature Branch**: `003-admin-design-system`

**Created**: 2026-06-17

**Status**: Draft

**Input**: User description: "Melhorar o design kit da área de administração adotando shadcn/ui, com tema claro e paleta branco/azul/verde, restrito à administração de conteúdos (dashboard, CRUD por recurso, auditoria e ações de linha), preservando o comportamento funcional existente e sem alterar o frontend público."

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Painel admin com aparência consistente e profissional (Priority: P1)

Um administrador acede ao backoffice e encontra uma interface coesa, de tema claro, com a navegação lateral, o cabeçalho e o ecrã de dashboard apresentados num sistema de design uniforme — fundo branco, ações em azul e indicadores positivos em verde — em substituição do visual atual feito com estilos avulsos.

**Why this priority**: É a fatia fundadora. Estabelece os tokens de cor, a tipografia, o tema claro e os componentes base reutilizáveis (botões, cartões, tabelas) e aplica-os ao "esqueleto" do painel (layout/navegação) e ao dashboard, que é a primeira página vista. Sem esta base, nenhuma outra página pode ser uniformizada. Já entrega valor visível e demonstrável de forma isolada.

**Independent Test**: Aceder a `/admin` autenticado e confirmar que a navegação, o cabeçalho e os cartões de métricas do dashboard usam o novo tema claro (fundo branco, primário azul, acento verde), sem regressões funcionais nas métricas apresentadas nem no gating de sessão.

**Acceptance Scenarios**:

1. **Given** um administrador autenticado, **When** abre `/admin`, **Then** vê o dashboard com os cartões de métricas e a navegação no novo sistema de design (tema claro, paleta branco/azul/verde).
2. **Given** o sistema operativo do utilizador em modo escuro, **When** abre qualquer página do painel admin, **Then** o painel permanece em tema claro (sem dark mode automático).
3. **Given** um utilizador sem sessão, **When** tenta abrir `/admin`, **Then** continua a ser redirecionado para o login (comportamento de gating preservado).
4. **Given** a falha de carregamento das métricas (ex.: 403), **When** o dashboard renderiza, **Then** é apresentado um estado de erro/sem-acesso legível dentro do novo design.

---

### User Story 2 - Gestão de conteúdos com componentes uniformes (Priority: P2)

Um administrador gere iscas, marcas, espécies e utilizadores em páginas de listagem e edição que usam tabelas, formulários, diálogos, distintivos de estado e ações de linha consistentes com o novo design system, mantendo todas as operações já existentes (listar com filtros/paginação, criar, editar, desativar, eliminar/soft-delete, restaurar, toggle de indexação) e o aviso RGPD para dados pessoais.

**Why this priority**: É o uso diário principal do backoffice. Depende dos componentes base definidos na US1 e cobre o maior volume de ecrãs (CRUD por recurso + ações de linha + modal RGPD).

**Independent Test**: Em cada recurso (`/admin/lures`, `/admin/brands`, `/admin/species`, `/admin/users`), confirmar que a listagem, o formulário de criação/edição, os distintivos de estado e as ações de linha usam os componentes do design system, e que todas as operações e o aviso RGPD funcionam como antes.

**Acceptance Scenarios**:

1. **Given** um recurso com registos, **When** o administrador abre a listagem, **Then** os dados aparecem numa tabela do design system com paginação e filtros funcionais.
2. **Given** um formulário de criação/edição, **When** o administrador submete dados inválidos, **Then** os erros de validação são apresentados de forma legível no estilo do design system.
3. **Given** uma operação sobre dados pessoais (utilizadores), **When** o administrador a inicia, **Then** o aviso/modal RGPD é apresentado no novo estilo antes de prosseguir.
4. **Given** o estado de um registo (ex.: ativo/inativo, eliminado, indexável), **When** a listagem renderiza, **Then** o estado é comunicado por distintivos com a semântica de cor adequada (verde para positivo).

---

### User Story 3 - Consulta de auditoria legível (Priority: P3)

Um administrador consulta o registo de auditoria numa página com filtros (ator, ação, período) e tabela paginada apresentados no novo design system, mantendo a capacidade de filtrar e navegar pelos registos como atualmente.

**Why this priority**: Importante para conformidade e investigação, mas de uso menos frequente que o CRUD diário; depende dos mesmos componentes base.

**Independent Test**: Abrir `/admin/audit`, aplicar filtros por ator/ação/período e paginar, confirmando que os controlos e a tabela usam o design system e que os resultados continuam corretos.

**Acceptance Scenarios**:

1. **Given** registos de auditoria existentes, **When** o administrador aplica um filtro por ação e período, **Then** a tabela mostra os resultados filtrados no estilo do design system.
2. **Given** muitos registos, **When** o administrador navega entre páginas, **Then** a paginação funciona e mantém os filtros aplicados.

---

### Edge Cases

- O que acontece ao frontend público (catálogo, detalhe, perfis) quando o design system é introduzido? Deve permanecer visualmente e funcionalmente inalterado.
- Como são comunicados estados de carregamento, vazio e erro em cada página admin? Devem ter tratamento visual coerente no design system.
- O que acontece à acessibilidade (contraste, foco de teclado) com a nova paleta? O contraste de texto/ações deve cumprir um nível legível.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: O backoffice admin (dashboard, CRUD por recurso, auditoria, ações de linha e diálogos) MUST apresentar-se através de um sistema de design único e consistente, substituindo os estilos avulsos atuais.
- **FR-002**: A área admin MUST usar exclusivamente tema claro, sem ativar dark mode automático com base na preferência do sistema operativo.
- **FR-003**: A paleta visual MUST usar branco como fundo predominante, azul como cor primária de ações/elementos interativos e verde como cor de acento e de estados positivos.
- **FR-004**: O sistema MUST preservar integralmente o comportamento funcional já existente do backoffice: gating de sessão, carregamento de dados, listagem com filtros e paginação, criação, edição, desativação, soft-delete, restauro, toggle de indexação, moderação e eliminação RGPD efetiva.
- **FR-005**: O aviso/modal de RGPD antes de operações sobre dados pessoais MUST manter-se e ser apresentado no novo estilo.
- **FR-006**: Estados de carregamento, vazio e erro (incluindo sem-acesso/403) MUST ser apresentados de forma coerente no novo design em todas as páginas admin.
- **FR-007**: Estados de registos (ativo/inativo, eliminado, indexável, pendente) MUST ser comunicados visualmente com semântica de cor consistente.
- **FR-008**: As alterações MUST ficar restritas à área de administração; o frontend público MUST permanecer visual e funcionalmente inalterado.
- **FR-009**: A interface admin é otimizada para **desktop**; responsividade para tablet/telemóvel está **fora de âmbito** (o backoffice é uma ferramenta de trabalho em ecrã grande). Não há requisito de adaptação a ecrãs estreitos.
- **FR-010**: O texto e os elementos interativos MUST manter contraste e foco de teclado legíveis com a nova paleta (acessibilidade básica).

### Key Entities *(include if feature involves data)*

Não aplicável — esta feature não introduz nem altera entidades de dados. Atua apenas sobre a apresentação das páginas e componentes de administração já existentes.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: 100% das páginas e componentes da área admin (dashboard, CRUD dos quatro recursos, auditoria, ações de linha e modal RGPD) são apresentados através do novo sistema de design, sem estilos avulsos remanescentes.
- **SC-002**: Todas as operações funcionais do backoffice existentes antes desta feature continuam a funcionar após a refatoração (zero regressões funcionais verificáveis).
- **SC-003**: O frontend público mantém-se inalterado — nenhuma página pública muda de aparência ou comportamento em consequência desta feature.
- **SC-004**: A área admin apresenta-se sempre em tema claro, independentemente da preferência de modo do sistema operativo, em 100% das páginas admin.
- **SC-005**: Os elementos de ação primária surgem em azul e os estados positivos em verde de forma consistente em todas as páginas admin.
- **SC-006**: Texto principal e ações cumprem um rácio de contraste legível (nível AA para texto normal) na nova paleta.

## Assumptions

- A biblioteca de componentes escolhida é o **shadcn/ui** (conforme pedido explícito do utilizador), o que implica introduzir e configurar Tailwind CSS no `apps/web`, que ainda não existe no projeto.
- A introdução do Tailwind/design system é feita sem regredir o frontend público existente (catálogo, detalhe, perfis, definições), que continua com a sua abordagem de estilo atual.
- O "tema claro forçado" aplica-se à área admin; não é objetivo desta feature redesenhar o frontend público.
- O backoffice é usado em **desktop**; responsividade para ecrãs pequenos (tablet/telemóvel) está fora de âmbito.
- Os recursos geridos no CRUD mantêm-se os já existentes: iscas, marcas, espécies e utilizadores.
- A autenticação/autorização admin (gating de sessão no frontend e verificação de role no backend por requisição) mantém-se inalterada — esta feature não toca na lógica de auth.
- Nenhuma alteração de contrato de API é necessária; a feature é puramente de apresentação no frontend.
