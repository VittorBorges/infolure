# Feature Specification: Sessão do utilizador no painel de administração (identidade + terminar sessão)

**Feature Branch**: `007-admin-user-session`

**Created**: 2026-06-19

**Status**: Draft

**Input**: User description: "no painel de administração mostrar informação de quem está logado, ter um botão para fazer logout"

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Ver quem está autenticado (Priority: P1)

Ao usar o painel de administração, o administrador vê de forma permanente e visível **quem está
autenticado** — o seu nome (ou, na ausência deste, o email) e a sua **função** (ex.: administrador).
Isto confirma com que conta está a operar, evitando ações na conta errada.

**Why this priority**: É a base da feature e entrega valor imediato (saber com que identidade se está a
operar é um requisito de confiança e segurança num backoffice). É independente do botão de logout.

**Independent Test**: Autenticar no painel e confirmar que a identidade apresentada corresponde à conta
em sessão (nome/email e função corretos); confirmar que a informação se mantém visível ao navegar entre
secções do painel.

**Acceptance Scenarios**:

1. **Given** um administrador autenticado, **When** abre qualquer página do painel, **Then** vê o seu
   nome (ou email, se não houver nome) e a sua função apresentados de forma visível.
2. **Given** um administrador autenticado, **When** navega entre secções do painel, **Then** a
   identidade apresentada mantém-se consistente e corresponde sempre à conta em sessão.
3. **Given** uma conta sem nome de apresentação definido, **When** abre o painel, **Then** o sistema
   apresenta o email como identificador legível (nunca o identificador interno/UUID).

---

### User Story 2 - Terminar sessão (Priority: P1)

A partir do painel, o administrador pode **terminar a sessão** através de um botão claramente
identificável. Após terminar, deixa de ter acesso às áreas protegidas e é levado para fora do painel.

**Why this priority**: Terminar sessão é essencial para a segurança em qualquer backoffice
(especialmente em equipamentos partilhados). Embora ligada à US1, entrega valor por si só.

**Independent Test**: Estando autenticado, acionar o botão de terminar sessão e confirmar que a sessão
deixa de ser válida — uma tentativa de aceder a uma área protegida exige nova autenticação.

**Acceptance Scenarios**:

1. **Given** um administrador autenticado, **When** aciona o botão de terminar sessão, **Then** a sessão
   é encerrada e é encaminhado para fora das áreas protegidas (página de entrada/autenticação).
2. **Given** uma sessão terminada, **When** o administrador tenta abrir uma área protegida do painel,
   **Then** o acesso é recusado e é-lhe pedido para autenticar novamente.
3. **Given** o administrador aciona o botão, **When** o encerramento está em curso, **Then** recebe uma
   indicação visual de progresso e o botão não pode ser acionado repetidamente em simultâneo.
4. **Given** o encerramento de sessão falha (ex.: indisponibilidade momentânea), **When** ocorre o erro,
   **Then** é apresentada uma mensagem compreensível e a sessão não fica num estado ambíguo.

---

### Edge Cases

- **Sessão expirada/inválida**: se a sessão já não for válida quando o painel é aberto, o sistema deve
  tratá-lo como não autenticado (sem mostrar identidade obsoleta) e pedir autenticação.
- **Conta sem nome**: apresentar o email; nunca expor o identificador interno (UUID).
- **Função desconhecida/ausente**: apresentar um valor neutro (ex.: "—") sem quebrar a interface.
- **Logout com falha de rede**: não deixar a interface a indicar "autenticado" se a sessão local já foi
  invalidada, nem o contrário; a mensagem de erro deve permitir nova tentativa.
- **Vários separadores abertos**: terminar sessão num separador não deve deixar outro separador a operar
  indefinidamente como autenticado em áreas protegidas.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: O painel de administração MUST apresentar, de forma visível em todas as páginas
  protegidas, a identidade do utilizador autenticado.
- **FR-002**: A identidade apresentada MUST incluir um identificador legível — o **nome** de
  apresentação, ou o **email** quando o nome não existir.
- **FR-003**: O painel MUST apresentar a **função** do utilizador autenticado (ex.: administrador).
- **FR-004**: O sistema MUST NOT expor identificadores internos (UUID) como identidade visível ao
  utilizador.
- **FR-005**: O painel MUST disponibilizar um controlo (botão) claramente identificável para **terminar
  a sessão**.
- **FR-006**: Ao terminar a sessão, o sistema MUST invalidar a sessão atual de forma a impedir acesso
  subsequente a áreas protegidas sem nova autenticação.
- **FR-007**: Após terminar a sessão, o utilizador MUST ser encaminhado para fora das áreas protegidas
  (página de entrada/autenticação).
- **FR-008**: Enquanto o encerramento de sessão decorre, o sistema MUST impedir acionamentos repetidos e
  dar indicação de progresso.
- **FR-009**: Se o encerramento de sessão falhar, o sistema MUST apresentar uma mensagem compreensível
  (sem expor erros técnicos crus) e não deixar a sessão num estado ambíguo.
- **FR-010**: A identidade apresentada MUST corresponder sempre à conta efetivamente em sessão; se a
  sessão for inválida/expirada, o painel MUST tratá-la como não autenticada.
- **FR-011**: O controlo de identidade e de terminar sessão MUST ser acessível por teclado e legível por
  tecnologias de apoio (rótulos claros), em linha com os princípios de acessibilidade do projeto.

### Key Entities *(include if feature involves data)*

- **Sessão do utilizador**: representa o estado de autenticação atual (a quem pertence e se é válida).
  Determina o acesso às áreas protegidas; é o que o botão "terminar sessão" invalida.
- **Identidade do utilizador**: dados legíveis apresentados no painel — nome (ou email) e função. Não
  inclui identificadores internos.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: 100% das páginas protegidas do painel mostram a identidade do utilizador autenticado.
- **SC-002**: Um administrador consegue identificar com que conta está a operar em menos de 5 segundos
  após abrir o painel, sem ações adicionais.
- **SC-003**: Terminar a sessão fica concluído em menos de 3 segundos na maioria dos casos, com o
  utilizador encaminhado para fora das áreas protegidas.
- **SC-004**: Após terminar a sessão, 100% das tentativas de aceder a áreas protegidas exigem nova
  autenticação (nenhum acesso indevido).
- **SC-005**: A identidade apresentada nunca contém identificadores internos (UUID) — 0 ocorrências.

## Assumptions

- O painel já está protegido por autenticação e por uma função de administrador; esta feature **reutiliza
  a sessão e a autenticação existentes**, não introduz um novo mecanismo de login.
- A identidade a apresentar é a já disponível para a conta em sessão (nome/email e função); não é
  necessário recolher novos dados de perfil.
- A informação de identidade é apresentada no cabeçalho/zona de navegação do painel (local persistente e
  visível em todas as páginas).
- Após terminar a sessão, o destino é a página de entrada/autenticação já existente.
- O âmbito limita-se ao painel de administração (backoffice); o site público fica fora de âmbito.
- "Terminar sessão" encerra a sessão atual do utilizador; a gestão avançada de múltiplas
  sessões/dispositivos está fora de âmbito desta feature.
