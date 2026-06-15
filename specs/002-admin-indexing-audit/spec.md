# Feature Specification: Painel de Administração, Controlo de Indexação e Base Auditável

**Feature Branch**: `002-admin-indexing-audit`

**Created**: 2026-06-14

**Status**: Draft

**Input**: User description: "Painel de administração com dashboard de cadastros e CRUD de toda a informação registada; mecanismo para os dados não serem indexados na internet; todas as entidades com status separado representando se está ativo, se foi adicionado por automação e se está deletado."

## Clarifications

### Session 2026-06-15

- Q: Visibilidade pública dos "filhos" quando o "pai" está inativo/eliminado? → A: Cascata de **visibilidade** (filhos ocultos do público) sem alterar o estado guardado dos filhos — mas **apenas para relações de pertença verdadeiras**. A marca é pai verdadeiro da isca; a espécie é uma relação **fraca** (espécie-alvo, muitos-para-muitos) e **não** é pai: uma espécie inativa/eliminada não oculta a isca, apenas é removida da lista de espécies-alvo e dos facets.
- Q: Desativar um utilizador afeta o login? → A: Sim — utilizador inativo (ou eliminado) não consegue autenticar-se e as sessões ativas deixam de ser válidas de imediato.
- Q: Como conciliar o soft-delete reversível com a obrigação RGPD de apagar dados pessoais? → A: Dois mecanismos distintos — o fluxo RGPD existente (Feature 001) mantém-se como remoção/anonimização **efetiva e irreversível** dos dados pessoais; o soft-delete do painel é uma ação administrativa **reversível** separada (moderação/suspensão).
- Q: Granularidade do registo de auditoria — guarda valores alterados? → A: Metadados sempre; instantâneo dos campos alterados (antes→depois) **apenas para operações sobre dados pessoais** (contas, favoritos, inventário). Restantes ações guardam só metadados.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Ciclo de vida e proveniência de todos os registos (Priority: P1)

Como administrador da plataforma, preciso que **cada registo** do sistema (iscas, marcas, espécies, traduções, cores, imagens, preços, utilizadores, favoritos, inventário, reviews) carregue de forma uniforme três atributos independentes: se está **ativo**, qual a sua **origem** (introduzido manualmente, por automação, ou em import inicial) e se está **eliminado** (eliminação reversível). As superfícies públicas (catálogo, busca, detalhe, perfis) deixam de mostrar automaticamente o que estiver inativo ou eliminado, sem que cada consulta tenha de o pedir explicitamente.

**Why this priority**: É a fundação dos restantes pilares — o painel de administração e o controlo de indexação assentam neste modelo de estado. Sozinha já entrega valor: permite desativar ou despublicar conteúdo problemático e distinguir dados gerados por automação dos curados à mão, com possibilidade de reverter.

**Independent Test**: Marcar uma isca como inativa e confirmar que desaparece do catálogo/busca/detalhe públicos; eliminá-la (soft-delete) e confirmar que some de todas as listagens; restaurá-la e confirmar que reaparece com o estado anterior; verificar que registos semeados surgem com origem "automação" e os criados no painel com origem "manual".

**Acceptance Scenarios**:

1. **Given** uma isca publicada e ativa, **When** o administrador a marca como inativa, **Then** ela deixa de aparecer no catálogo, na busca e na sua página de detalhe pública, mas continua visível no painel de administração.
2. **Given** um registo qualquer, **When** o administrador o elimina, **Then** o registo desaparece de todas as superfícies (públicas e listagens normais), permanece recuperável e pode ser restaurado ao estado imediatamente anterior.
3. **Given** o catálogo populado por automação/seed, **When** o administrador consulta a origem de um registo, **Then** vê "automação" para os importados e "manual" para os criados no painel.
4. **Given** um registo eliminado, **When** uma consulta pública ou uma listagem normal é executada, **Then** o registo nunca é devolvido, a menos que se peça explicitamente a inclusão de eliminados (apenas no contexto de administração).

---

### User Story 2 - Backoffice de administração: dashboard e gestão de toda a informação (Priority: P1)

Como administrador, preciso de uma área de administração protegida onde, ao entrar, vejo um **dashboard de cadastros** (evolução de novos utilizadores ao longo do tempo, contagens de iscas por estado/origem/atividade, reviews por moderar, totais de favoritos e inventário) e a partir da qual consigo **criar, consultar, editar, ativar/desativar e eliminar/restaurar qualquer informação registada na plataforma** — incluindo dados pessoais dos utilizadores (favoritos, inventário e contas). Cada listagem permite filtrar, pesquisar e paginar.

**Why this priority**: É o pedido central da fase — dá à equipa o controlo operacional sobre o conteúdo e os utilizadores. Entrega valor de forma autónoma assim que o ciclo de vida (US-01) existe.

**Independent Test**: Autenticar como administrador, abrir o painel, confirmar que o dashboard apresenta as métricas; criar uma marca; editar uma isca; desativar um utilizador; eliminar e restaurar uma review; confirmar que um utilizador não-administrador não consegue aceder a nenhuma destas funções.

**Acceptance Scenarios**:

1. **Given** um utilizador autenticado **sem** papel de administrador, **When** tenta aceder a qualquer página ou função do painel, **Then** o acesso é recusado.
2. **Given** um administrador autenticado, **When** abre o painel, **Then** vê o dashboard com cadastros de utilizadores em série temporal (recortes de 7 e 30 dias e acumulado), iscas agrupadas por estado/origem/atividade, contagem de reviews por moderar e totais de favoritos e inventário.
3. **Given** uma listagem de qualquer entidade no painel, **When** o administrador aplica filtros e pesquisa, **Then** os resultados são filtrados, pesquisáveis e paginados, incluindo a opção de ver registos inativos ou eliminados.
4. **Given** qualquer entidade, **When** o administrador cria, edita, ativa/desativa, elimina ou restaura um registo, **Then** a alteração é persistida e refletida nas superfícies relevantes (incluindo a remoção/atualização do índice de busca quando aplicável).
5. **Given** uma operação sobre **dados pessoais** (conta, favoritos ou inventário de um utilizador), **When** o administrador a executa, **Then** o sistema exibe um aviso de conformidade com proteção de dados antes de confirmar e regista a ação no registo de auditoria.

---

### User Story 3 - Controlo de indexação por motores de busca (Priority: P2)

Como administrador, preciso de **um interruptor** que determine se os dados da plataforma podem ou não ser indexados por motores de busca, ajustável em tempo real a partir do painel, sem necessidade de novo deploy. O controlo existe a nível **global** (liga/desliga a indexação de todo o catálogo público) e **por isca** (uma isca individual pode ser marcada como não-indexável). As páginas de utilizador (perfis, inventário, favoritos) nunca são indexáveis.

**Why this priority**: Responde à necessidade de "os dados não serem indexados na internet" de forma controlável, e reverte conscientemente a decisão da Feature 001 (que tornou o catálogo indexável) tornando-a num controlo operacional. Depende do painel (US-02) para a interface de controlo.

**Independent Test**: Com a indexação global ligada, confirmar que as instruções para motores de busca permitem indexação e que o mapa do site lista as iscas elegíveis; desligar o interruptor no painel e confirmar, em tempo útil, que as instruções passam a proibir indexação, que o mapa do site fica vazio e que as páginas de detalhe sinalizam "não indexar"; marcar uma isca individual como não-indexável e confirmar que apenas essa é excluída.

**Acceptance Scenarios**:

1. **Given** a indexação global ligada, **When** um motor de busca consulta as instruções de rastreio e o mapa do site, **Then** a indexação é permitida e o mapa lista as iscas publicadas, ativas e indexáveis.
2. **Given** a indexação global, **When** o administrador a desliga no painel, **Then** dentro de um curto intervalo as instruções de rastreio passam a proibir a indexação, o mapa do site deixa de listar conteúdo e cada página de detalhe sinaliza "não indexar".
3. **Given** a indexação global ligada, **When** o administrador marca uma isca específica como não-indexável, **Then** essa isca é excluída do mapa do site e a sua página sinaliza "não indexar", sem afetar as restantes.
4. **Given** qualquer estado de indexação, **When** uma página de utilizador é acedida, **Then** sinaliza sempre "não indexar".

---

### User Story 4 - Registo de auditoria das ações administrativas (Priority: P3)

Como responsável pela plataforma, preciso que todas as ações realizadas no painel de administração — em especial sobre dados pessoais e sobre o estado dos registos — fiquem registadas (quem, o quê, quando, sobre que registo), para rastreabilidade e conformidade.

**Why this priority**: Reforça a conformidade e a responsabilização do poder máximo concedido ao painel, mas o painel é operacional sem ela. Depende da existência das ações de administração (US-02).

**Independent Test**: Executar várias ações no painel (criar, editar, desativar, eliminar, restaurar, operar sobre dados pessoais) e confirmar que cada uma gera uma entrada de auditoria com autor, tipo de ação, entidade/registo afetado e momento.

**Acceptance Scenarios**:

1. **Given** um administrador a operar no painel, **When** executa qualquer ação de escrita, **Then** é criada uma entrada de auditoria identificando autor, ação, entidade, identificador do registo e data/hora.
2. **Given** entradas de auditoria existentes, **When** o administrador consulta o histórico, **Then** pode filtrá-lo por autor, tipo de ação e período.

---

### Edge Cases

- **Desativar/eliminar um "pai"**: o que acontece a iscas de uma marca quando a marca é desativada ou eliminada? (Assunção: desativar não cascateia; eliminar um pai ainda referenciado é bloqueado ou avisado — ver Assumptions.)
- **Catálogo público vazio**: se a indexação estiver desligada ou todas as iscas inativas, as superfícies públicas mostram estado vazio coerente, sem erros.
- **Restaurar para um mundo mudado**: restaurar um registo cuja marca/relacionamento foi entretanto eliminado.
- **Conta de utilizador eliminada pelo próprio (RGPD, Feature 001) vs. eliminada por administrador**: ambas convergem no mesmo estado de eliminação reversível, sem duplicar conceitos.
- **Auto-bloqueio**: um administrador desativar/eliminar a própria conta ou remover o último administrador — deve ser impedido.
- **Propagação ao índice de busca**: desativar/eliminar/despublicar uma isca tem de a remover do índice de busca; reativar/restaurar tem de a repor.
- **Latência do interruptor de indexação**: a alteração do estado global deve refletir-se nas instruções de rastreio dentro de um intervalo curto e previsível, mesmo com cache.

## Requirements *(mandatory)*

### Functional Requirements

**Ciclo de vida e proveniência (US-01)**

- **FR-001**: Todos os registos de todas as entidades MUST possuir, de forma uniforme, um estado de **atividade** (ativo/inativo), uma **origem** (manual, automação, import) e um marcador de **eliminação reversível** (eliminado/não-eliminado), além de instantes de criação e de última alteração.
- **FR-002**: As consultas públicas e as listagens normais MUST excluir, por defeito e automaticamente, os registos eliminados.
- **FR-003**: As superfícies públicas (catálogo, busca, detalhe, perfis) MUST excluir registos inativos; uma isca só é pública se estiver publicada, ativa e não eliminada.
- **FR-003a**: A visibilidade pública MUST respeitar em cascata o estado do **pai verdadeiro**: uma isca cuja marca esteja inativa ou eliminada NÃO é apresentada nas superfícies públicas, sem que o estado guardado da própria isca seja alterado. Esta cascata aplica-se apenas a relações de pertença verdadeiras (marca→isca), não a relações fracas.
- **FR-003b**: A relação isca↔espécie é uma associação fraca (espécie-alvo, muitos-para-muitos) e NÃO é uma relação de pertença: uma espécie inativa ou eliminada MUST NOT ocultar as iscas associadas; apenas MUST ser removida da lista de espécies-alvo apresentada e dos facets de filtragem.
- **FR-004**: O sistema MUST permitir restaurar um registo eliminado, repondo-o no estado de atividade anterior à eliminação.
- **FR-005**: Os registos existentes à entrada desta fase MUST ser migrados com estado coerente: ativos, não eliminados, e com origem atribuída (automação para os dados de seed/automação, import para a carga inicial), sem perda de dados.
- **FR-006**: O estado de **atividade** e o de **eliminação** MUST ser conceptualmente independentes do estado editorial de publicação já existente nas iscas (rascunho/publicado/arquivado), coexistindo sem o substituir.

**Backoffice e dashboard (US-02)**

- **FR-007**: O painel de administração MUST ser acessível apenas a utilizadores com papel de administrador; qualquer acesso não-administrador MUST ser recusado, tanto na interface como nas operações subjacentes.
- **FR-008**: O painel MUST apresentar um dashboard com: novos cadastros de utilizadores em série temporal com recortes de 7 e 30 dias e total acumulado; contagem de iscas por estado, origem e atividade; número de reviews por moderar; e totais de favoritos e de inventário.
- **FR-009**: O painel MUST permitir operações de criar, consultar, editar, ativar/desativar e eliminar/restaurar sobre **todas** as entidades registadas, incluindo dados pessoais dos utilizadores (contas, favoritos e inventário).
- **FR-010**: Cada listagem do painel MUST suportar filtragem, pesquisa e paginação, e MUST permitir incluir opcionalmente registos inativos e eliminados.
- **FR-011**: Alterações feitas no painel que afetem conteúdo público MUST refletir-se nas superfícies correspondentes, incluindo a atualização ou remoção do índice de busca quando aplicável.
- **FR-012**: Operações sobre dados pessoais MUST apresentar um aviso de conformidade com proteção de dados antes da confirmação.
- **FR-012a**: O soft-delete reversível do painel e a eliminação RGPD ("direito ao esquecimento") MUST ser mecanismos distintos: o soft-delete oculta de forma recuperável; a eliminação RGPD existente (Feature 001) MUST permanecer como remoção/anonimização efetiva e irreversível dos dados pessoais. O painel MUST disponibilizar/encaminhar para a eliminação RGPD efetiva quando o objetivo for cumprir o direito ao esquecimento.
- **FR-013**: O sistema MUST impedir que um administrador elimine/desative a própria conta de forma a perder o acesso, e MUST impedir a remoção do último administrador.
- **FR-013a**: Um utilizador inativo ou eliminado MUST NOT conseguir autenticar-se, e quaisquer sessões ativas desse utilizador MUST deixar de ser válidas de imediato após a desativação/eliminação.

**Controlo de indexação (US-03)**

- **FR-014**: O sistema MUST disponibilizar um interruptor global, gerível em tempo real a partir do painel, que determina se o catálogo público pode ser indexado por motores de busca.
- **FR-015**: O sistema MUST permitir marcar iscas individuais como não-indexáveis, independentemente do estado global.
- **FR-016**: Quando a indexação global está desligada, as instruções de rastreio MUST proibir a indexação, o mapa do site MUST ficar sem conteúdo e as páginas de detalhe MUST sinalizar "não indexar".
- **FR-017**: Quando a indexação global está ligada, o mapa do site MUST listar apenas iscas publicadas, ativas, não eliminadas e indexáveis.
- **FR-018**: As páginas de utilizador (perfis, inventário, favoritos) MUST sinalizar sempre "não indexar", independentemente do interruptor global.
- **FR-019**: Uma alteração ao interruptor global MUST refletir-se nas instruções de rastreio e no mapa do site dentro de um intervalo curto e previsível, mesmo havendo cache.

**Auditoria (US-04)**

- **FR-020**: O sistema MUST registar cada ação de escrita realizada no painel, capturando autor, tipo de ação, entidade e identificador do registo afetado e o momento.
- **FR-020a**: Para operações sobre **dados pessoais** (contas, favoritos, inventário), o registo de auditoria MUST incluir, além dos metadados, um instantâneo dos campos alterados (valores antes→depois). As restantes ações guardam apenas os metadados.
- **FR-021**: O sistema MUST permitir consultar o histórico de auditoria, com filtragem por autor, tipo de ação e período.

### Key Entities *(include if feature involves data)*

- **Atributos de ciclo de vida (transversais)**: conjunto uniforme aplicado a cada entidade existente — atividade (ativo/inativo), origem (manual/automação/import), eliminação reversível (com instante de eliminação) e instantes de criação/alteração.
- **Definições da aplicação (singleton)**: estado de configuração operacional da plataforma, contendo, entre outros, o interruptor global de indexação.
- **Indexabilidade da isca**: atributo por isca que indica se pode ser apresentada a motores de busca.
- **Registo de auditoria**: histórico de ações administrativas — autor, tipo de ação, entidade e registo afetado, momento; para operações sobre dados pessoais inclui ainda um instantâneo dos campos alterados (antes→depois).
- **Entidades existentes abrangidas** (recebem os atributos de ciclo de vida): marcas e respetivas traduções; espécies e traduções; iscas e respetivas traduções, cores, imagens, espécies-alvo e preços de retalhista; utilizadores e respetivos fornecedores de autenticação, favoritos e inventário; reviews e votos de utilidade.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: 100% das entidades registadas expõem os três atributos de estado (atividade, origem, eliminação), verificável por inspeção do modelo de dados e por testes.
- **SC-002**: 0 registos eliminados ou inativos aparecem em qualquer superfície pública, comprovado por testes de regressão sobre catálogo, busca, detalhe e perfis.
- **SC-003**: Um administrador consegue localizar e editar qualquer registo da plataforma a partir do painel em menos de 30 segundos a partir do dashboard.
- **SC-004**: 100% das tentativas de acesso ao painel por não-administradores são recusadas.
- **SC-005**: Desligar o interruptor de indexação reflete-se nas instruções de rastreio públicas em menos de 60 segundos.
- **SC-006**: Após desligar a indexação global, 0 páginas públicas anunciam "indexar" e o mapa do site fica sem conteúdo.
- **SC-007**: 100% das ações de escrita no painel produzem uma entrada de auditoria correspondente.
- **SC-008**: A migração de dados existentes conclui sem perda de registos e com estado coerente (todos ativos, não eliminados, origem atribuída), verificável por contagens antes/depois.
- **SC-009**: A suite de testes existente (integração e E2E) permanece verde após a introdução do filtro global de eliminação.

## Assumptions

- **Eliminação reversível por defeito**: a "eliminação" no painel é sempre reversível (soft-delete). A purga permanente/definitiva fica fora do âmbito desta fase.
- **Não-cascata na desativação**: desativar um registo "pai" (ex.: marca) não desativa automaticamente os filhos; eliminar um pai ainda referenciado por filhos ativos é avisado/bloqueado em vez de cascatear silenciosamente.
- **RGPD vs. soft-delete (mecanismos distintos)**: o soft-delete do painel é reversível (moderação/suspensão); a eliminação RGPD da Feature 001 mantém-se como remoção/anonimização efetiva e irreversível dos dados pessoais. Não são unificados (ver FR-012a).
- **Recortes do dashboard**: por defeito, os cadastros são apresentados em 7 dias, 30 dias e acumulado; outros recortes ficam para iteração futura.
- **Latência de indexação**: assume-se cache de curta duração para o estado global, com invalidação na alteração, garantindo o intervalo de 60 segundos do SC-005.
- **Papel de administrador**: reutiliza-se o mecanismo de papéis e a política de administração já existentes na plataforma; não se introduz um novo sistema de permissões granular nesta fase.
- **Retenção de auditoria**: as entradas de auditoria são retidas por, no mínimo, 12 meses; a política de expurgo fica para iteração futura.
- **Idioma do painel**: o painel de administração segue o idioma principal da plataforma (PT-PT); internacionalização completa do backoffice fica fora do âmbito.
- **Stack inalterada**: reutilizam-se as tecnologias e a infraestrutura já em uso na plataforma; esta fase não introduz novos serviços externos.
