<!--
SYNC IMPACT REPORT
==================
Version change: 1.1.0 → 1.1.1
Rationale (latest amendment): Refinamento da stack após o plano da feature 001 — frontend
precisado como Next.js (React/TS, SSR), backend fixado em .NET 10 LTS, banco em PostgreSQL.
PATCH — esclarecimento/concretização, sem alterar princípios.

History:
  - 1.0.0 → Initial ratification of the project constitution (MAJOR baseline).
  - 1.1.0 → Restrições Técnicas & Stack concretizadas (.NET + React/TS).
  - 1.1.1 → Stack refinada pelo plano 001 (Next.js, .NET 10 LTS, PostgreSQL).

Modified principles: N/A (initial adoption)
Added principles:
  - I. Simplicidade Primeiro (YAGNI) — NON-NEGOTIABLE
  - II. Observabilidade por Padrão — NON-NEGOTIABLE
  - III. Contratos Explícitos (Frontend ↔ Backend)
  - IV. Qualidade Testável
  - V. Experiência do Usuário Consistente
Added sections:
  - Restrições Técnicas & Stack
  - Fluxo de Desenvolvimento & Quality Gates
  - Governance
Removed sections: none

Templates requiring updates:
  - .specify/templates/plan-template.md ✅ aligned (Constitution Check is dynamic; no edit needed)
  - .specify/templates/spec-template.md ✅ aligned (no principle-driven mandatory section changes)
  - .specify/templates/tasks-template.md ✅ aligned (task categories already cover testing/observability)
  - .specify/templates/checklist-template.md ✅ aligned (generic)
  - .specify/templates/commands/*.md ⚠ n/a (directory does not exist)

Follow-up TODOs:
  - (Resolvido em 1.1.1) Versão LTS do .NET (.NET 10), banco (PostgreSQL) e frontend (Next.js)
    fixados pelo plano da feature 001.
  - Regenerar specs/001-lure-catalog-mvp/tasks.md com /speckit-tasks: a tasks.md atual foi
    gerada para a stack Node.js/Fastify e está obsoleta após o re-plano.
-->

# infolure Constitution

## Core Principles

### I. Simplicidade Primeiro (YAGNI) — NON-NEGOTIABLE
Toda solução MUST começar pela forma mais simples que resolve o problema atual e comprovado.
Abstrações, camadas, dependências e configurações novas MUST ser justificadas por uma necessidade
real e presente — não por uma necessidade hipotética futura ("You Aren't Gonna Need It").
Qualquer complexidade adicional (novo serviço, nova lib, novo padrão arquitetural) MUST ser
explicitamente justificada na seção "Complexity Tracking" do plano; se não couber justificativa,
a opção mais simples vence.

**Rationale**: O projeto está em estágio inicial e sem stack travada. Manter superfície pequena
reduz custo de manutenção, acelera entrega e preserva liberdade de decisão futura. Complexidade
introduzida cedo é a mais cara de remover.

### II. Observabilidade por Padrão — NON-NEGOTIABLE
Todo comportamento relevante MUST ser observável sem necessidade de um debugger anexado.
- Logging estruturado (formato chave-valor ou JSON) MUST ser usado no backend; logs MUST incluir
  um identificador de correlação por requisição/fluxo.
- Erros MUST ser registrados com contexto suficiente para reproduzir a falha (entrada relevante,
  estado, stack), sem vazar segredos ou dados sensíveis.
- Toda fronteira de rede (requisições do frontend, chamadas a serviços externos) MUST registrar
  início, fim e resultado (sucesso/erro + latência).

**Rationale**: Sendo full-stack, falhas atravessam fronteiras (browser → API → dependências).
Sem rastreabilidade ponta-a-ponta, diagnosticar problemas em produção vira adivinhação.
Observabilidade é requisito de entrega, não um extra.

### III. Contratos Explícitos (Frontend ↔ Backend)
A interface entre frontend e backend MUST ser definida por um contrato explícito e versionado
(schema/tipos compartilhados, especificação de API ou equivalente) antes da implementação do
consumo. Mudanças que quebram o contrato MUST seguir versionamento semântico e MUST documentar
o caminho de migração. Frontend e backend MUST NOT assumir formatos não declarados no contrato.

**Rationale**: O ponto de maior atrito em apps full-stack é a borda cliente-servidor. Um contrato
explícito permite que as duas pontas evoluam em paralelo e transforma incompatibilidades em erros
detectáveis em vez de bugs em runtime.

### IV. Qualidade Testável
Toda funcionalidade MUST ser projetada para ser verificável de forma automatizada. Lógica de
negócio MUST ter testes; fluxos que cruzam a fronteira frontend↔backend MUST ter ao menos um teste
de integração ou end-to-end cobrindo o caminho feliz e os principais erros. Um critério de
aceitação de uma feature não é "compila", e sim "tem teste verde que prova o comportamento".

**Rationale**: Testes são a rede que sustenta a Simplicidade (permite refatorar sem medo) e a
prova viva dos Contratos. Sem verificação automatizada, a confiança nas mudanças decai com o tempo.

### V. Experiência do Usuário Consistente
Por ser uma aplicação voltada a usuários finais, a UI MUST apresentar comportamento consistente e
previsível: estados de carregamento, vazio e erro MUST ser tratados explicitamente em cada tela
que faz I/O; mensagens de erro MUST ser compreensíveis para o usuário (sem stack traces cruas).
Acessibilidade básica (navegação por teclado, contraste, rótulos) SHOULD ser respeitada e MUST
NOT ser ignorada sem justificativa.

**Rationale**: A qualidade percebida do produto vive na UI. Tratar estados de borda e erros de
forma consistente é o que separa um protótipo de um produto utilizável.

## Restrições Técnicas & Stack

A stack do projeto está definida da seguinte forma:

- **Backend**: .NET (ASP.NET Core, Web API). Versão LTS fixada em **.NET 10 (LTS)** pelo plano da
  feature 001; MUST ser mantida consistente em todo o projeto.
- **Frontend**: React via **Next.js** (App Router). TypeScript MUST ser usado (em vez de
  JavaScript puro) para sustentar o Princípio III — os tipos compartilhados são parte do contrato
  explícito frontend↔backend. SSR/SSG MUST ser usado em páginas que exigem indexabilidade.
- **Contrato de API**: a borda entre React e .NET MUST ser descrita por um contrato versionado.
  Recomenda-se OpenAPI/Swagger gerado a partir do backend ASP.NET Core como fonte de verdade,
  com os tipos do frontend derivados desse contrato (Princípio III).
- **Banco de dados**: **PostgreSQL** (Azure Database for PostgreSQL Flexible Server), fixado pelo
  plano da feature 001. **Gerenciador de pacotes/bundler do frontend**: ferramentas nativas do
  Next.js (npm + Turbopack/webpack do framework).

Regras gerais que permanecem válidas sobre a stack:

- Backend e frontend MUST ter ecossistema de testes automatizados e logging estruturado
  configurados (pré-requisito dos Princípios II e IV) — ex.: logging estruturado nativo do
  .NET (`ILogger` / Serilog) no backend.
- Dependências de terceiros MUST ser adicionadas com parcimônia (Princípio I): preferir a
  biblioteca padrão (.NET BCL / APIs nativas do React) quando suficientes.
- Segredos e credenciais MUST NOT ser versionados; configuração sensível MUST vir de variáveis
  de ambiente, user-secrets do .NET ou cofre equivalente.

## Fluxo de Desenvolvimento & Quality Gates

- O fluxo segue o Spec Kit: `specify` → `clarify` → `plan` → `tasks` → `analyze` → `implement`.
- Todo plano (`plan.md`) MUST passar pelo **Constitution Check**: as gates são derivadas destes
  princípios. Violações MUST ser justificadas em "Complexity Tracking" ou o plano MUST ser revisto.
- Antes de marcar uma tarefa como concluída: testes relevantes MUST estar verdes (Princípio IV) e
  os caminhos de I/O introduzidos MUST estar logados (Princípio II).
- Code review (humano ou automatizado) MUST verificar conformidade com os princípios desta
  constituição, não apenas correção funcional.

## Governance

Esta constituição supersede outras práticas em caso de conflito. Em qualquer divergência entre uma
decisão pontual e um princípio aqui declarado, o princípio prevalece até ser formalmente emendado.

- **Emendas**: alterações MUST ser documentadas (o que mudou e por quê), versionadas e acompanhadas
  da atualização do campo "Last Amended" e, quando aplicável, de um plano de migração.
- **Versionamento**: segue SemVer.
  - MAJOR: remoção ou redefinição incompatível de princípios/governança.
  - MINOR: novo princípio/seção ou expansão material de orientação.
  - PATCH: esclarecimentos, correções de redação, refinamentos não semânticos.
- **Conformidade**: todos os PRs/reviews MUST verificar aderência aos princípios. Complexidade não
  justificada é motivo válido para bloquear uma mudança.
- **Orientação de runtime**: usar `CLAUDE.md` e o plano atual como guia operacional do agente;
  esses documentos MUST permanecer coerentes com esta constituição.

**Version**: 1.1.1 | **Ratified**: 2026-06-13 | **Last Amended**: 2026-06-13
