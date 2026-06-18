# Feature Specification: Formulário de Registo e Edição de Iscas

**Feature Branch**: `005-lure-registration-form`

**Created**: 2026-06-18

**Status**: Draft

**Input**: User description: "adicionar formulário para registar iscas, todos as propriedades devem estar disponiveis para inserir e editar. Relacionado a isca, adicionar o campo tamanho, adicionar o campo descrição e uma lista de cores. cada cor pode não ser só uma cor, exemplo a isca pode ser verde e amarelo, para cada cor pode ter ou não uma foto. cada cor, para alem da cor, e da foto, quero ter uma lista de codigos html para a cor da isca."

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Registar uma nova isca com todas as propriedades (Priority: P1)

Um editor do backoffice abre o formulário de registo de isca, preenche todas as propriedades
disponíveis da isca (incluindo nome, tamanho e descrição) e grava. A isca passa a existir no
catálogo e fica disponível para edição posterior.

**Why this priority**: É o coração da feature — sem a capacidade de criar uma isca com os seus
dados, nada mais tem valor. Entrega valor isolado: permite popular o catálogo manualmente.

**Independent Test**: Pode ser testada por completo abrindo o formulário vazio, preenchendo os
campos obrigatórios e opcionais, gravando, e confirmando que a isca aparece persistida com todos
os valores introduzidos.

**Acceptance Scenarios**:

1. **Given** o editor está no formulário de nova isca, **When** preenche os campos obrigatórios e
   grava, **Then** a isca é criada e o sistema confirma o sucesso identificando a isca criada.
2. **Given** o editor deixa em branco um campo obrigatório, **When** tenta gravar, **Then** o
   sistema impede a gravação e indica claramente qual campo falta.
3. **Given** o editor adiciona vários tamanhos à isca (cada um com o seu peso) e preenche a
   "descrição", **When** grava, **Then** todos os tamanhos/pesos e a descrição ficam associados à
   isca e visíveis ao reabrir o registo.

---

### User Story 2 - Editar uma isca existente (Priority: P1)

Um editor abre uma isca já registada, vê todas as propriedades preenchidas com os valores atuais,
altera qualquer uma delas (incluindo tamanho, descrição e cores) e grava as alterações.

**Why this priority**: Registar sem poder corrigir torna o catálogo frágil. Edição é tão essencial
quanto criação para manter os dados corretos ao longo do tempo.

**Independent Test**: Pode ser testada selecionando uma isca existente, confirmando que o formulário
abre pré-preenchido, alterando valores, gravando e verificando que as alterações persistiram sem
afetar campos não tocados.

**Acceptance Scenarios**:

1. **Given** uma isca existente, **When** o editor abre o formulário de edição, **Then** todos os
   campos surgem preenchidos com os valores atuais.
2. **Given** o editor altera um ou mais campos, **When** grava, **Then** apenas os campos alterados
   mudam e os restantes mantêm o valor anterior.
3. **Given** o editor abandona a edição sem gravar, **When** sai do formulário, **Then** nenhuma
   alteração é persistida.

---

### User Story 3 - Gerir a lista de cores de uma isca (Priority: P2)

Para uma isca, o editor adiciona uma ou mais "cores". Cada cor pode ser composta por mais do que
uma cor de base (ex.: verde e amarelo), pode ter ou não uma foto associada, e tem uma lista de
códigos HTML (hex) que representam as cores dessa variante.

**Why this priority**: As cores enriquecem a isca mas a isca já tem valor sem elas (US1/US2). É um
incremento independente sobre a base de registo/edição.

**Independent Test**: Pode ser testada numa isca já criada, adicionando uma cor com múltiplas cores
de base, anexando (ou não) uma foto, introduzindo vários códigos hex, gravando e confirmando que a
variante persiste com todos os seus elementos.

**Acceptance Scenarios**:

1. **Given** uma isca em edição, **When** o editor adiciona uma cor com duas cores de base (ex.:
   "verde e amarelo"), **Then** a variante fica registada com ambas.
2. **Given** uma cor em edição, **When** o editor introduz vários códigos HTML (hex), **Then** todos
   os códigos ficam associados a essa cor.
3. **Given** uma cor sem foto, **When** o editor grava, **Then** a cor é aceite sem foto.
4. **Given** uma cor com foto, **When** o editor anexa uma imagem e grava, **Then** a foto fica
   associada a essa cor.
5. **Given** o editor introduz um código HTML inválido (não é um hex válido), **When** tenta gravar,
   **Then** o sistema impede e indica o código inválido.
6. **Given** uma isca com várias cores, **When** o editor remove uma cor, **Then** essa cor (e a sua
   foto e códigos) deixa de estar associada à isca.

---

### Edge Cases

- O que acontece quando o editor adiciona uma cor sem qualquer código hex nem foto? (Variante "vazia"
  — ver Assumptions: exige pelo menos um nome/código.)
- Como reage o sistema a um código HTML duplicado dentro da mesma cor?
- O que acontece se a foto exceder o tamanho/formato permitido?
- Como é tratado o abandono do formulário com alterações por gravar (perda acidental de dados)?
- O que acontece ao tentar gravar uma isca cujo identificador (ex.: slug/nome) colide com outra
  existente?
- Como se comporta o formulário ao gravar uma isca com muitas cores e fotos (latência/feedback)?

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: O sistema MUST disponibilizar um formulário onde o editor pode registar uma nova isca
  com todas as propriedades da isca disponíveis para inserção.
- **FR-002**: O sistema MUST permitir editar uma isca existente, apresentando todas as propriedades
  pré-preenchidas com os valores atuais.
- **FR-003**: O sistema MUST permitir associar a uma isca uma **lista de tamanhos** (um ou mais),
  gerível no formulário de registo/edição. Cada item da lista representa uma variante de tamanho da
  isca.
- **FR-003a**: Cada tamanho da lista MUST ter, disponíveis para inserção e edição: um **rótulo** de
  tamanho (designação do fabricante, ex.: "100SP"), um **comprimento numérico** (em mm) e um **peso**
  (em g).
- **FR-004**: O sistema MUST incluir um campo "descrição" da isca, disponível para inserção e edição.
- **FR-005**: O sistema MUST permitir associar a uma isca uma lista de cores (zero ou mais), gerível
  no mesmo formulário de registo/edição.
- **FR-006**: Cada cor MUST poder ser composta por mais do que uma cor de base (ex.: verde e
  amarelo), e não apenas uma.
- **FR-007**: Cada cor MUST poder ter, opcionalmente, uma foto associada — a ausência de foto é
  válida.
- **FR-008**: Cada cor MUST suportar uma lista de códigos HTML (hex) que representam as cores dessa
  variante.
- **FR-009**: O sistema MUST validar que cada código introduzido é um código de cor HTML (hex)
  válido antes de gravar, e MUST indicar claramente qualquer código inválido.
- **FR-010**: O sistema MUST permitir adicionar e remover cores de uma isca, e adicionar/remover
  códigos hex dentro de cada cor.
- **FR-010a**: O sistema MUST permitir adicionar e remover tamanhos da lista de tamanhos de uma isca.
- **FR-011**: O sistema MUST validar os campos obrigatórios antes de gravar e MUST comunicar de
  forma compreensível ao editor qualquer campo em falta ou inválido.
- **FR-012**: O sistema MUST persistir todas as propriedades introduzidas (incluindo tamanho,
  descrição, cores, fotos e códigos hex) de forma que sejam recuperadas integralmente ao reabrir a
  isca para edição.
- **FR-013**: O sistema MUST garantir que gravar uma isca não altera propriedades que o editor não
  modificou.
- **FR-014**: O sistema MUST tratar explicitamente os estados de carregamento, erro e sucesso do
  formulário, apresentando mensagens compreensíveis (sem expor detalhes técnicos crus).

### Key Entities *(include if feature involves data)*

- **Isca (Lure)**: o produto do catálogo. Para esta feature ganha relevância a "descrição" e a lista
  de tamanhos, além das restantes propriedades já existentes. Tem um ou mais Tamanhos e zero ou mais
  Cores.
- **Tamanho da Isca (Size Variant)**: uma variante de tamanho de uma isca, composta por um rótulo
  de tamanho (designação do fabricante), um comprimento numérico (mm) e um peso (g). Uma isca tem um
  ou mais. Pertence a uma única isca.
- **Cor da Isca (Color Variant)**: uma variante de cor de uma isca. Pode ser composta por mais do
  que uma cor de base, tem opcionalmente uma foto e contém uma lista de Códigos HTML. Pertence a uma
  única isca.
- **Código HTML (Hex Code)**: um código de cor HTML (hex) pertencente a uma Cor da Isca. Uma Cor
  pode ter vários.
- **Foto da Cor (Color Photo)**: imagem opcional associada a uma Cor da Isca.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Um editor consegue registar uma nova isca com vários tamanhos/pesos, descrição e pelo
  menos uma cor com múltiplos códigos hex em menos de 5 minutos, sem assistência.
- **SC-002**: 100% das propriedades introduzidas no formulário são recuperadas corretamente ao
  reabrir a isca para edição (nenhuma perda de dados).
- **SC-003**: Códigos HTML inválidos são rejeitados em 100% dos casos antes da gravação, com
  indicação clara de qual código está incorreto.
- **SC-004**: Uma cor pode conter pelo menos duas cores de base e pelo menos cinco códigos hex sem
  degradar a usabilidade do formulário.
- **SC-005**: A edição de uma isca preserva 100% das propriedades não alteradas pelo editor.

## Assumptions

- Esta feature destina-se ao **backoffice/admin** (editores/administradores autenticados), em linha
  com as features 002/003/004; não é um fluxo de utilizador final público.
- O catálogo de iscas já existe (feature 001); esta feature acrescenta/expõe propriedades de
  registo e edição sobre o modelo existente, em vez de criar o catálogo de raiz.
- **(Resolvido)** "Tamanho" e "peso" **não são valores únicos** da isca: uma isca tem uma **lista de
  tamanhos**, e cada tamanho é composto por **rótulo + comprimento (mm) + peso (g)**. Isto evolui os
  campos únicos `length_mm`/`weight_g` da feature 001 para uma lista de variantes. Como o catálogo
  ainda não tem dados reais, não há migração a fazer.
- "Descrição" é um texto livre descritivo da isca.
- **(Resolvido)** O modelo de cor adota uma **lista aberta de códigos hex por variante**. Como o
  catálogo ainda não tem cores reais, não há migração de dados a fazer a partir do par fixo
  `hex_primary`/`hex_secondary` da feature 001.
- Cada cor tem **no máximo uma** foto opcional (o pedido refere "uma foto").
- Uma cor exige pelo menos um identificador útil (nome da cor ou pelo menos um código hex) para ser
  considerada válida; cores totalmente vazias não são gravadas.
- **Códigos hex duplicados dentro da mesma cor são permitidos** (a mesma cor pode repetir-se com
  textura diferente); o sistema não os rejeita nem deduplica.
- Validação de hex aceita os formatos HTML usuais (ex.: `#RGB`, `#RRGGBB`); a normalização exata
  segue prática padrão da web.
- O formulário reutiliza o design system partilhado `@infolure/design-system` (feature 004) para
  manter consistência de UI.
