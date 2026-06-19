# Feature Specification: Melhorias ao Formulário de Iscas (indexação global, CRUD de marcas, marca por nome, "tamanho"→"configuração" com anzol por configuração, múltiplas fotos)

**Feature Branch**: `006-lure-form-enhancements`

**Created**: 2026-06-19

**Status**: Draft

**Input**: User description: "nas iscas remover a opção de indexar ou não, adicionar no painel administrativo uma opção para ligar tudo ou desligar tudo. na isca, onde tem Marca, criar um componente para buscar uma marca pelo nome em vez de ter o UUID exposto. na isca, passar as informações sobre anzol para a mesma lista de tamanhos (cada tamanho com tamanho/quantidade/tipo de anzol). as fotos das cores: poder ter uma lista de fotos não só uma (com teste, pois foto > 1 MB dava problema)."

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Controlo global de indexação SEO (Priority: P1)

O administrador deixa de poder marcar cada isca individualmente como indexável/não-indexável. Em vez
disso, no painel administrativo existe um único interruptor para **ligar tudo** ou **desligar tudo** a
indexação SEO de todo o catálogo.

**Why this priority**: Simplifica o modelo (remove uma propriedade por isca) e dá um controlo único e
claro. É independente das restantes mudanças.

**Independent Test**: No painel, alternar o interruptor global e confirmar que afeta a indexação SEO
de todo o catálogo; confirmar que o formulário/lista de iscas já não mostra o controlo por isca.

**Acceptance Scenarios**:

1. **Given** o painel administrativo, **When** o admin desliga a indexação global, **Then** o catálogo
   deixa de ser indexável para SEO (sem depender de qualquer marca por isca).
2. **Given** o painel administrativo, **When** o admin liga a indexação global, **Then** o catálogo
   volta a ser elegível para indexação SEO.
3. **Given** o formulário/lista de iscas, **When** o editor o abre, **Then** não existe qualquer opção
   de indexar/não-indexar por isca.

---

### User Story 2 - Gerir marcas (CRUD de marcas) (Priority: P1)

O administrador gere as marcas do catálogo através de um CRUD no backoffice: **criar**, **listar**,
**editar** e **eliminar** marcas (com nome e demais propriedades). Estas marcas são as que ficam
disponíveis para seleção no formulário da isca.

**Why this priority**: A seleção de marca na isca (US3) só tem valor se existir uma forma de gerir as
marcas. É um pré-requisito funcional e entrega valor por si (manter o catálogo de marcas correto).

**Independent Test**: No painel, criar uma marca nova, editá-la, vê-la na listagem e eliminá-la,
confirmando cada operação; a marca criada fica disponível para seleção numa isca.

**Acceptance Scenarios**:

1. **Given** o painel de marcas, **When** o admin cria uma marca com nome, **Then** a marca passa a
   existir e aparece na listagem.
2. **Given** uma marca existente, **When** o admin edita o nome/propriedades e grava, **Then** as
   alterações persistem.
3. **Given** uma marca existente, **When** o admin a elimina, **Then** deixa de estar disponível para
   novas associações (segue a política de eliminação já usada no backoffice).
4. **Given** uma marca recém-criada, **When** o editor abre o formulário de uma isca, **Then** essa
   marca pode ser encontrada pela busca por nome (US3).

---

### User Story 3 - Selecionar a marca por nome na isca (Priority: P1)

No formulário da isca, o editor escolhe a marca **procurando pelo nome** num componente de
busca/seleção (autocomplete) sobre as marcas geridas em US2, sem nunca ver nem introduzir o UUID.

**Why this priority**: O campo atual (UUID em texto) é inutilizável na prática; a seleção por nome é
essencial para registar/editar iscas de forma realista.

**Independent Test**: No formulário, escrever parte do nome de uma marca, escolher um resultado da
lista, gravar, e confirmar que a isca fica associada à marca correta — sem o editor ter visto o UUID.

**Acceptance Scenarios**:

1. **Given** o campo de marca, **When** o editor escreve parte de um nome, **Then** surge uma lista de
   marcas correspondentes para escolher.
2. **Given** uma marca selecionada, **When** o editor grava, **Then** a isca fica associada a essa
   marca.
3. **Given** uma isca em edição com marca já associada, **When** o formulário abre, **Then** o nome da
   marca atual aparece pré-selecionado (não o UUID).
4. **Given** o editor não escolhe nenhuma marca, **When** grava, **Then** a isca é gravada sem marca
   (a marca é opcional).

---

### User Story 4 - Renomear "tamanho" para "configuração" e anzol por configuração (Priority: P2)

O conceito hoje chamado "tamanho da isca" passa a chamar-se **"configuração da isca"** em todo o lado
(modelo, persistência, API e UI). Além disso, as informações de anzol deixam de ser propriedades
únicas da isca e passam a pertencer a **cada configuração**: cada configuração tem o seu **tamanho de
anzol**, **quantidade de anzóis** e **tipo de anzol** (a par de código, rótulo, comprimento e peso).

**Why this priority**: A variante deixou de ser só "tamanho" (agrupa dimensão, peso e anzóis), pelo
que "configuração" descreve-a melhor. Aproxima o modelo da realidade (configurações diferentes da
mesma isca trazem anzóis diferentes). Depende da lista já existente (feature 005).

**Independent Test**: Numa isca com várias configurações, definir anzol distinto por configuração,
gravar e reabrir, confirmando que cada configuração mantém o seu tamanho/quantidade/tipo de anzol; e
confirmar que a UI/dados usam o termo "configuração" (não "tamanho") para a variante.

**Acceptance Scenarios**:

1. **Given** uma configuração da isca, **When** o editor define tamanho/quantidade/tipo de anzol,
   **Then** esses valores ficam associados a essa configuração específica.
2. **Given** uma isca com duas configurações com anzóis diferentes, **When** grava e reabre, **Then**
   cada configuração mantém os seus próprios dados de anzol.
3. **Given** o formulário, **When** o editor o abre, **Then** já não existem campos de anzol ao nível
   da isca (apenas por configuração), e a secção chama-se "Configurações".

---

### User Story 5 - Múltiplas fotos por cor (com suporte a ficheiros maiores) (Priority: P2)

Cada cor passa a ter uma **lista de fotos** (várias), em vez de apenas uma. Além disso, o upload de
fotos com mais de 1 MB — que atualmente falha — passa a funcionar até ao limite de tamanho definido.

**Why this priority**: Enriquece a apresentação das cores e corrige um defeito real de upload. Depende
da gestão de cores (feature 005).

**Independent Test**: Numa cor, carregar várias fotos (incluindo uma com mais de 1 MB), gravar e
reabrir, confirmando que todas as fotos persistem e estão associadas à cor; confirmar que uma foto
acima do limite é recusada com mensagem clara.

**Acceptance Scenarios**:

1. **Given** uma cor, **When** o editor carrega várias fotos, **Then** todas ficam associadas a essa
   cor, por ordem.
2. **Given** uma foto com mais de 1 MB (e dentro do limite definido), **When** o editor a carrega,
   **Then** o upload conclui com sucesso (deixa de falhar).
3. **Given** uma foto acima do limite máximo, **When** o editor tenta carregá-la, **Then** o sistema
   recusa com uma mensagem compreensível (sem erro técnico cru).
4. **Given** uma cor com várias fotos, **When** o editor remove uma foto, **Then** essa foto deixa de
   estar associada à cor e as restantes mantêm-se.

---

### User Story 6 - Gerir espécies (CRUD) e selecionar espécies-alvo por nome (Priority: P1)

O administrador gere as espécies do catálogo através de um CRUD no backoffice: **criar**, **listar**,
**editar** e **eliminar** espécies (nome comum, tipo de água e família). No formulário da isca, o
editor associa as **espécies-alvo** procurando-as **pelo nome** (autocomplete, multi-seleção), com uma
**confiança** opcional (primária/secundária) por espécie — sem nunca ver nem introduzir o UUID. Espelha
o padrão já entregue para marcas (US2/US3).

**Why this priority**: As espécies-alvo são essenciais para o catálogo e os filtros, mas só têm valor
se existir forma de as gerir e de as associar a uma isca de forma realista (por nome, não por UUID). É
o mesmo défice que as marcas tinham antes da US2/US3.

**Independent Test**: No painel, criar uma espécie nova, editá-la, vê-la na listagem e eliminá-la; no
formulário de uma isca, procurar a espécie pelo nome, associá-la como espécie-alvo, gravar e reabrir,
confirmando que fica associada (com a confiança escolhida) — sem o editor ter visto o UUID.

**Acceptance Scenarios**:

1. **Given** o painel de espécies, **When** o admin cria uma espécie com nome comum (e opcionalmente
   tipo de água/família), **Then** a espécie passa a existir e aparece na listagem.
2. **Given** uma espécie existente, **When** o admin edita as propriedades e grava, **Then** as
   alterações persistem.
3. **Given** uma espécie existente, **When** o admin a elimina, **Then** deixa de estar disponível para
   novas associações (segue a política de eliminação já usada no backoffice).
4. **Given** o campo de espécies-alvo na isca, **When** o editor escreve parte de um nome, **Then**
   surge uma lista de espécies correspondentes para escolher e adicionar.
5. **Given** uma isca em edição com espécies-alvo já associadas, **When** o formulário abre, **Then** as
   espécies aparecem pré-selecionadas pelo nome (não o UUID), com a respetiva confiança.

---

### Edge Cases

- Indexação global: o que acontece a iscas que estavam marcadas como não-indexáveis ao remover esse
  controlo? (Ver Assumptions: passam a depender apenas do interruptor global.)
- Marca: como reage o componente quando a busca por nome não devolve resultados?
- Marca: nomes de marca duplicados/ambíguos — como distinguir na lista?
- Rename: garantir que o termo "configuração" (variante da isca) não se confunde com a "configuração
  de indexação" global — são conceitos distintos.
- Anzol por configuração: o que acontece aos dados de anzol que existiam ao nível da isca ao migrar
  para por configuração? (Ver Assumptions.)
- Fotos: o que acontece ao tentar carregar um ficheiro que não é imagem, ou acima do limite?
- Fotos: ordenação e remoção numa lista com muitas fotos.
- Espécies: como reage o picker quando a busca por nome não devolve resultados; impedir associar a
  mesma espécie-alvo duas vezes à mesma isca.
- Configuração: peso por configuração é agora **opcional** — quando indicado, deve ser > 0.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: O sistema MUST remover o controlo de indexação SEO **por isca** (deixa de existir a opção
  de indexar/não-indexar uma isca individualmente, no formulário, na lista e no modelo).
- **FR-002**: O painel administrativo MUST disponibilizar um único controlo global para **ligar** ou
  **desligar** a indexação SEO de todo o catálogo.
- **FR-003**: Com a indexação global **desligada**, o catálogo MUST deixar de ser elegível para
  indexação SEO; com ela **ligada**, volta a ser elegível.
- **FR-003a**: O backoffice MUST disponibilizar um CRUD de **marcas**: criar, listar, editar e
  eliminar marcas (incluindo o nome), seguindo as políticas de listagem/eliminação já usadas no
  painel.
- **FR-003b**: As marcas geridas no CRUD MUST ser as que ficam disponíveis para seleção no formulário
  da isca.
- **FR-004**: O formulário da isca MUST permitir escolher a marca através de uma **busca por nome**
  (autocomplete), apresentando marcas correspondentes ao texto introduzido.
- **FR-005**: O editor MUST NOT precisar de ver nem introduzir o identificador interno (UUID) da marca
  em qualquer momento.
- **FR-006**: Ao editar uma isca com marca, o componente MUST apresentar a marca atual já selecionada
  (pelo nome); a marca MUST continuar a ser opcional.
- **FR-006a**: O sistema MUST renomear o conceito de "tamanho da isca" para **"configuração da isca"**
  em todo o lado — entidade, persistência (tabela/colunas), contrato de API e UI — substituindo a
  designação anterior introduzida na feature 005.
- **FR-007**: O sistema MUST associar as informações de anzol (**tamanho de anzol**, **quantidade de
  anzóis**, **tipo de anzol**) a **cada configuração** da isca, e não à isca como um todo.
- **FR-007a**: O **peso** de cada configuração MUST ser **opcional**; quando indicado, MUST ser maior
  que zero. (Ajuste à feature 006: deixa de ser obrigatório.)
- **FR-008**: O formulário MUST remover os campos de anzol ao nível da isca, oferecendo-os por cada
  configuração da lista de configurações.
- **FR-009**: Cada cor MUST suportar uma **lista de fotos** (zero ou mais), com possibilidade de
  adicionar e remover fotos individualmente, mantendo uma ordem.
- **FR-010**: O sistema MUST permitir o upload de fotos com mais de 1 MB (corrigindo a falha atual),
  até um limite máximo de **5 MB** por foto.
- **FR-011**: O sistema MUST recusar, com mensagem compreensível, fotos acima de 5 MB ou de tipo não
  suportado (sem expor erros técnicos crus).
- **FR-012**: O comportamento corrigido de upload (incluindo fotos > 1 MB) MUST ser coberto por um
  teste automatizado que o comprove.
- **FR-013**: As alterações MUST preservar o comportamento de registo/edição já existente (feature
  005) para os restantes campos.
- **FR-014**: O backoffice MUST disponibilizar um CRUD de **espécies**: criar, listar, editar e
  eliminar espécies (nome comum, tipo de água e família), seguindo as políticas de listagem/eliminação
  já usadas no painel.
- **FR-015**: As espécies geridas no CRUD MUST ser as que ficam disponíveis para associação como
  **espécies-alvo** no formulário da isca.
- **FR-016**: O formulário da isca MUST permitir associar **várias** espécies-alvo através de uma
  **busca por nome** (autocomplete), com uma **confiança** opcional (primária/secundária) por espécie,
  sem nunca expor nem exigir o UUID; ao editar, as espécies já associadas MUST aparecer pré-selecionadas
  pelo nome.

### Key Entities *(include if feature involves data)*

- **Configuração de Indexação (global)**: estado único (ligado/desligado) que determina a
  elegibilidade de indexação SEO de todo o catálogo. Substitui o controlo por isca.
- **Isca (Lure)**: perde a propriedade de indexação por isca e os campos de anzol ao nível da isca;
  passa a referenciar a marca apenas por associação (selecionada por nome na UI).
- **Configuração da Isca (Lure Configuration)**: a variante antes chamada "Tamanho da Isca" (feature
  005), agora **renomeada**. Além de código/rótulo/comprimento e **peso (opcional)**, passa a ter
  **tamanho de anzol**, **quantidade de anzóis** e **tipo de anzol**. (Nome distinto da "Configuração de
  Indexação" global.)
- **Marca (Brand)**: gerida por um CRUD no backoffice (criar/listar/editar/eliminar) e pesquisável por
  nome para seleção no formulário da isca.
- **Espécie (Species)**: gerida por um CRUD no backoffice (criar/listar/editar/eliminar — nome comum,
  tipo de água, família) e pesquisável por nome para associação como espécie-alvo no formulário da isca.
- **Espécie-alvo (Target Species)**: associação entre isca e espécie, com uma confiança opcional
  (primária/secundária); selecionada por nome na UI.
- **Foto da Cor (Color Photo)**: cada cor tem uma lista ordenada de fotos (em vez de uma só).

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Um administrador consegue ligar/desligar a indexação SEO de todo o catálogo numa única
  ação no painel.
- **SC-002**: Não existe, em nenhum ecrã do backoffice, um controlo de indexação por isca.
- **SC-003**: Um administrador consegue criar/editar/eliminar marcas no painel, e uma marca criada
  fica imediatamente disponível para seleção numa isca.
- **SC-003a**: Um editor consegue associar a marca correta a uma isca procurando pelo nome, em menos
  de 15 segundos, sem nunca ver um UUID.
- **SC-004**: Cada configuração de uma isca pode ter dados de anzol distintos, preservados 100% após
  gravar e reabrir; o termo "configuração" substitui "tamanho" em toda a UI e dados.
- **SC-005**: É possível carregar várias fotos por cor, incluindo pelo menos uma com mais de 1 MB, com
  100% de sucesso até ao limite definido; fotos acima do limite são recusadas com mensagem clara.

## Assumptions

- Esta feature é **continuação da 005**, no backoffice/admin (editores/administradores autenticados).
- Já existe um estado global de indexação SEO no sistema (feature 002); esta feature **promove-o** ao
  painel como o único controlo e **remove** o controlo por isca (`is_indexable`).
- Ao remover o controlo por isca, a elegibilidade de indexação passa a depender apenas do interruptor
  global (e do estado de publicação já existente). Não há dados a preservar do `is_indexable` (sem
  necessidade de migração de valores por isca).
- Esta feature inclui o **CRUD de marcas** no backoffice; o componente na isca **seleciona** marcas
  geridas por esse CRUD. (O backend já tem operações base de marca da feature 002; a UI de
  criação/edição de marca é acrescentada aqui.)
- **Rename "tamanho" → "configuração"**: aplica-se ao **código e à base de dados** dentro desta
  feature 006 (entidade `LureSize`→`LureConfiguration`, tabela `lure_sizes`→`lure_configurations`,
  DTOs/contrato e componentes de UI), incluindo migração de schema de rename. Os documentos
  históricos da feature 005 ficam como estão. O nome escolhido é **"Configuração da isca"** para o
  distinguir da "Configuração de Indexação" global (US1).
- Os dados de anzol que hoje existem ao nível da isca não precisam de ser migrados para as
  configurações (catálogo ainda sem dados reais relevantes); passam a ser definidos por configuração
  de raiz.
- O limite máximo de tamanho de foto é **5 MB** por ficheiro; o problema atual do 1 MB é um limite a
  corrigir (provável limite de payload na camada de submissão), não o limite pretendido.
- Tipos de imagem suportados seguem os formatos web usuais (ex.: JPEG/PNG/WebP).
- O formulário continua a reutilizar o design system partilhado `@infolure/design-system`.
