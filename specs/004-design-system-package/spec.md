# Feature Specification: Design System Partilhado + Storybook

**Feature Branch**: `004-design-system-package`

**Created**: 2026-06-18

**Status**: Draft

**Input**: User description: "Montar um design system a sério para o projeto: promover os componentes shadcn/ui e os tokens (hoje embebidos na app e limitados ao admin) a um pacote partilhado no monorepo, com build próprio, tokens centralizados (paleta branco/azul/verde) e Storybook, reutilizável por admin e público."

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Pacote de design system reutilizável (Priority: P1)

Como programador da equipa, quero importar os componentes de interface e os tokens de design a partir de um **pacote partilhado único** do monorepo (em vez de cópias embebidas numa app), para que qualquer parte do produto use exatamente os mesmos componentes e a mesma paleta, com uma única fonte de verdade.

**Why this priority**: É a fatia fundadora — sem o pacote partilhado, com build e pontos de importação estáveis, nada mais é reutilizável. Entrega valor imediato ao consolidar o que a feature 003 criou (componentes shadcn + paleta azul/verde) num artefacto partilhável, e prova-se migrando o backoffice admin para consumir o pacote sem regressões.

**Independent Test**: Migrar o backoffice admin para importar os componentes e tokens do pacote partilhado; confirmar que o painel continua a funcionar e a apresentar-se exatamente como antes, e que não restam cópias locais desses componentes na app.

**Acceptance Scenarios**:

1. **Given** o pacote de design system publicado no monorepo, **When** uma aplicação importa um componente (ex.: botão, cartão, tabela), **Then** recebe o componente do pacote com o estilo e comportamento corretos.
2. **Given** o backoffice admin migrado, **When** é aberto, **Then** apresenta-se e funciona igual ao estado da feature 003 (zero regressões visuais ou funcionais).
3. **Given** uma alteração a um token de cor no pacote, **When** as aplicações são reconstruídas, **Then** a mudança reflete-se em todos os consumidores do pacote (fonte única de verdade).
4. **Given** o pacote, **When** é construído, **Then** produz um artefacto consumível (componentes + estilos + definições de tipos) sem depender do código interno de nenhuma app.

---

### User Story 2 - Catálogo visual documentado (Storybook) (Priority: P2)

Como programador ou designer, quero um **catálogo visual navegável** que mostre cada componente do design system com as suas variantes e estados (ex.: botão primário/sucesso/destrutivo, cartão, tabela, distintivos, diálogo), para descobrir o que existe, ver como se comporta e usá-lo corretamente sem ler o código-fonte.

**Why this priority**: Multiplica o valor do pacote — torna-o descobrível e documentado, reduz uso incorreto e duplicação, e estabelece a base para documentação e verificação visual. Depende do pacote (US1) existir.

**Independent Test**: Abrir o catálogo localmente e confirmar que lista os componentes do pacote, cada um com as suas variantes/estados renderizados e interativos, refletindo a paleta branco/azul/verde.

**Acceptance Scenarios**:

1. **Given** o catálogo a correr, **When** é aberto, **Then** lista os componentes do design system organizados de forma navegável.
2. **Given** um componente no catálogo, **When** é selecionado, **Then** mostra as suas variantes e estados principais renderizados com os tokens corretos.
3. **Given** o catálogo, **When** um componente é alterado no pacote, **Then** o catálogo reflete a alteração após reconstrução.

---

### User Story 3 - Disponível também ao frontend público (Priority: P3)

Como programador, quero que o frontend público (catálogo, detalhe, perfis) possa **consumir o mesmo design system** que o admin, para que, ao longo do tempo, todo o produto convirja para uma identidade visual consistente a partir de uma base comum.

**Why this priority**: Concretiza a promessa de "reutilizável por admin e público", mas é a fatia menos urgente: o valor estrutural está no pacote (US1) e na sua documentação (US2). O redesenho completo do frontend público é trabalho à parte; aqui basta tornar o design system **disponível e adotável** no público e provar com uma adoção-piloto.

**Independent Test**: Numa página pública escolhida como piloto, substituir um elemento de UI por um componente do pacote partilhado e confirmar que renderiza corretamente com os tokens, sem quebrar a página.

**Acceptance Scenarios**:

1. **Given** o pacote partilhado, **When** uma página pública importa um componente do design system, **Then** ele renderiza com o estilo correto nessa página.
2. **Given** a adoção-piloto numa página pública, **When** a página é aberta, **Then** funciona e apresenta-se corretamente, sem regressões nas restantes páginas públicas.

---

### Edge Cases

- Como se evita duplicação de dependências (ex.: a biblioteca de UI carregada duas vezes) quando várias apps consomem o pacote?
- O que acontece aos tokens partilhados se uma app precisar de uma variação local — há um mecanismo de extensão sem quebrar a fonte única?
- Como é que o catálogo trata um componente sem estados/variantes documentados — fica visível como incompleto em vez de ausente?
- Como se garante que migrar o admin para o pacote não altera nada visualmente (paridade pixel)?

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: O sistema MUST disponibilizar um **pacote de design system partilhado** no monorepo, com um conjunto de componentes de interface reutilizáveis e um conjunto de tokens de design.
- **FR-002**: O pacote MUST ser **construível** num artefacto consumível (componentes, estilos e definições de tipos) independente do código interno de qualquer aplicação.
- **FR-003**: Os **tokens de design** (incluindo a paleta branco/azul/verde e o tema claro definidos na feature 003) MUST residir no pacote como **fonte única de verdade**, consumida por todas as aplicações.
- **FR-004**: O **backoffice admin** MUST passar a consumir os componentes e tokens a partir do pacote partilhado, **sem regressões** visuais nem funcionais face ao estado da feature 003.
- **FR-005**: Não MUST restar cópias locais, na aplicação, dos componentes/tokens que passaram para o pacote (eliminação da duplicação).
- **FR-006**: O sistema MUST fornecer um **catálogo visual navegável** dos componentes do pacote, mostrando as principais variantes e estados de cada um.
- **FR-007**: O frontend público MUST poder **importar e usar** componentes e tokens do pacote partilhado (disponibilidade), com pelo menos uma **adoção-piloto** numa página pública a demonstrá-lo.
- **FR-008**: Uma alteração a um token ou componente no pacote MUST propagar-se a todos os consumidores após reconstrução (consistência a partir da fonte única).
- **FR-009**: O sistema MUST evitar a duplicação da biblioteca de UI/dependências quando múltiplas aplicações consomem o pacote.
- **FR-010**: Os componentes do pacote MUST manter a acessibilidade básica já garantida na feature 003 (foco de teclado visível, contraste de texto ≥ AA, rótulos em campos).
- **FR-011**: As alterações ao frontend público fora da adoção-piloto MUST permanecer inalteradas nesta feature (o redesenho completo do público está fora de âmbito).

### Key Entities *(include if feature involves data)*

Não aplicável — esta feature é de arquitetura de frontend e ferramentas; não introduz nem altera entidades de dados, schema ou contratos de API.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: 100% dos componentes de interface e tokens hoje em `apps/web` (feature 003) passam a residir no pacote partilhado, sem cópias duplicadas remanescentes.
- **SC-002**: O backoffice admin consome exclusivamente o pacote partilhado e mantém paridade visual e funcional com a feature 003 (zero regressões verificáveis).
- **SC-003**: O catálogo visual lista 100% dos componentes do pacote, cada um com as suas principais variantes/estados renderizados.
- **SC-004**: Alterar um token de cor num único local reflete-se em todos os consumidores após reconstrução (verificável por uma mudança de teste).
- **SC-005**: Pelo menos uma página pública usa, em produção, um componente do pacote partilhado (adoção-piloto), sem regressões nas restantes páginas públicas.
- **SC-006**: A biblioteca de UI subjacente é carregada uma única vez por aplicação (sem duplicação de dependências).

## Assumptions

- A base de componentes é a já adotada na feature 003 — **shadcn/ui** sobre **Tailwind CSS v4** — promovida a pacote partilhado; não se troca de biblioteca de UI.
- O catálogo visual é um **Storybook** (pedido explícito do utilizador).
- O pacote vive no monorepo (ex.: `packages/design-system`) e o monorepo passa a usar **workspaces** para o ligar às aplicações (`apps/web`, e disponível para futuras apps).
- O **redesenho completo do frontend público** está fora de âmbito; esta feature entrega o pacote, a documentação e a **disponibilidade** ao público com uma adoção-piloto. Uma migração total do público pode ser uma feature futura.
- O **backend** (`apps/api`, .NET) **não é tocado** — feature exclusivamente de frontend/ferramentas.
- A versão do produto continua a seguir o versionamento **por feature** (`specs/NNN-*`); a versão do pacote no monorepo é independente e não representa a versão do produto.
- O tema mantém-se **claro** com a paleta branco/azul/verde da feature 003; não se introduz dark mode nesta feature.
