# Research — Feature 001: Lure Catalog MVP (.NET + Next.js)

**Date**: 2026-06-13
**Context**: Re-plano da stack (Node.js/Fastify → **.NET**; frontend mantém-se em **Next.js**).
Resolve as `NEEDS CLARIFICATION` do `plan.md` e documenta as decisões de tecnologia. Formato:
**Decisão / Rationale / Alternativas consideradas**.

---

## 1. Versão e runtime do backend

- **Decisão**: C# 13 sobre **.NET 10 (LTS)**, ASP.NET Core Web API com **controllers** organizados
  por vertical slice.
- **Rationale**: .NET 10 é o LTS vigente (lançado nov/2025), com suporte estendido — alinha à regra
  da constituição de fixar uma versão LTS. Controllers dão estrutura testável clara para ~25 endpoints.
- **Alternativas**: Minimal APIs (mais enxutas, mas a organização por grupos fica menos óbvia com
  este número de endpoints); .NET 9 (STS, janela de suporte mais curta) — rejeitado.

## 2. Acesso a dados / ORM

- **Decisão**: **Entity Framework Core + Npgsql** (provider PostgreSQL). Schema canônico em
  `data-model.md`; materializado por **migrations do EF Core**.
- **Rationale**: ORM idiomático do .NET, tipado, com migrations integradas — substitui Drizzle/Flyway
  do plano anterior. Npgsql é o provider Postgres maduro padrão.
- **Alternativas**: **Dapper** (mais leve/rápido, mas sem migrations nem tracking; mais código manual);
  EF Core foi preferido pela produtividade no MVP. Pontos quentes de leitura podem cair para SQL puro
  via Dapper se o profiling justificar (YAGNI até lá).

## 3. Autenticação

- **Decisão**: manter **Supabase Auth** como broker OIDC. O backend .NET **valida o JWT** do Supabase
  via endpoint JWKS (`Microsoft.AspNetCore.Authentication.JwtBearer`). Um webhook do Supabase chama
  `POST /v1/auth/sync` para criar a linha `users` no Postgres no primeiro login.
- **Rationale**: Supabase Auth entrega Google + Microsoft MSA + email/senha + linking multi-provedor +
  refresh tokens prontos (US-04). É independente da linguagem do backend — o .NET só valida JWTs.
  Construir isto em ASP.NET Core Identity seria muito mais código e violaria o Princípio I (YAGNI).
- **Alternativas**: **ASP.NET Core Identity + OIDC nativo** (controle total, mas implementar MSA +
  linking + refresh à mão é caro); **Azure AD B2C / Entra External ID** — rejeitado por **NG6** (sem
  Azure AD/Entra). Auth0/Clerk — equivalentes pagos; Supabase mantido para minimizar churn.

## 4. Busca e autocomplete

- **Decisão**: **Typesense Cloud**, com cliente .NET (`typesense-dotnet`). Sincronização
  **write-through**: toda mutação de isca na API re-indexa o documento no Typesense.
- **Rationale**: busca facetada + relevância + autocomplete < 150ms (NFR). Write-through é mais simples
  que CDC/event-driven para o volume v1 (Princípio I).
- **Alternativas**: PostgreSQL full-text (`tsvector`) — adequado a texto, mas faceting e ranking de
  autocomplete ficam aquém; Elasticsearch/OpenSearch — operacionalmente mais pesado que Typesense gerido.

## 5. Rate limiting e cache

- **Decisão**: **rate limiting nativo do ASP.NET Core** (`Microsoft.AspNetCore.RateLimiting`) com
  store distribuído em **Redis** (StackExchange.Redis). Limites: 100 req/min por IP (anônimo),
  300 req/min por utilizador (autenticado). Redis também guarda cache de autocomplete.
- **Rationale**: middleware de rate limiting é parte do framework (sem dependência extra de código);
  Redis garante contagem consistente entre múltiplas instâncias da API (NFR).
- **Alternativas**: rate limiting em memória (quebra com >1 instância) — rejeitado; AspNetCoreRateLimit
  (lib de terceiros) — desnecessária dado o suporte nativo.

## 6. Observabilidade (Princípio II)

- **Decisão**: **Serilog** com sink JSON estruturado; **middleware de correlation-id** propagado em
  todas as respostas e logs; log de cada fronteira de rede (entrada HTTP + chamadas a Postgres/Typesense/
  Redis/Blob/Supabase) com duração. ID de utilizador **com hash** nos logs; sem email/nome.
- **Rationale**: satisfaz o Princípio II (NON-NEGOTIABLE) e o NFR de "sem PII nos logs".
- **Alternativas**: `ILogger` nativo apenas (sink estruturado e enrichers do Serilog são mais ergonômicos);
  OpenTelemetry tracing — desejável, mas adiado para além do MVP (YAGNI) salvo necessidade.

## 7. Contrato Frontend ↔ Backend (Princípio III)

- **Decisão**: **OpenAPI** (`contracts/api.yaml`) é a fonte de verdade. O backend gera/valida o spec
  (Swashbuckle / `Microsoft.AspNetCore.OpenApi`); o frontend gera tipos TypeScript via
  **openapi-typescript** em `packages/api-types`, consumidos pelo cliente de API do Next.js.
- **Rationale**: tipos derivados do contrato eliminam divergência cliente-servidor e transformam
  incompatibilidades em erros de compilação (Princípio III + IV).
- **Alternativas**: gerar cliente completo (NSwag/openapi-generator) — mais pesado; apenas tipos +
  fetch fino foi preferido pela simplicidade.

## 8. NEEDS CLARIFICATION — Paginação vs. infinite scroll (US-01)

- **Decisão (recomendada)**: **paginação clássica** com `page`/`page_size` na listagem de catálogo,
  com URL-synced state (já exigido para shareability).
- **Rationale**: páginas indexáveis (G5/US-03 SEO) e estado partilhável são mais robustos com paginação;
  infinite scroll prejudica SSR/SEO e o botão "voltar". Aberto a revisão de UX no design do frontend.
- **Alternativas**: infinite scroll (melhor em mobile, pior para SEO/partilha) — pode ser híbrido
  ("carregar mais" + páginas indexáveis) numa iteração futura.

## 9. NEEDS CLARIFICATION — Hosting do frontend

- **Decisão (recomendada)**: **Azure Container Apps** para o Next.js (mesma região West Europe da API
  e dados — RGPD/latência), atrás do Azure Front Door.
- **Rationale**: mantém todo o stack numa única nuvem/região (residência de dados, observabilidade e
  rede coerentes). Vercel daria DX superior mas adiciona um fornecedor e potencial saída de região UE.
- **Alternativas**: **Vercel** (melhor DX/edge para Next.js) — reconsiderar se a equipa priorizar DX
  sobre consolidação em Azure; Azure Static Web Apps — limitado para SSR pesado.

---

## Resumo das decisões

| Área | Decisão |
|---|---|
| Backend | ASP.NET Core (.NET 10 LTS), controllers, vertical slices |
| ORM | EF Core + Npgsql, migrations |
| Auth | Supabase Auth (broker OIDC) + validação de JWT no .NET |
| Busca | Typesense Cloud, sync write-through |
| Rate limit / cache | RateLimiting nativo + Redis |
| Observabilidade | Serilog JSON + correlation-id |
| Contrato | OpenAPI → tipos TS via openapi-typescript |
| Listagem | Paginação clássica, URL-synced |
| Hosting FE | Azure Container Apps (West Europe) |

Todas as `NEEDS CLARIFICATION` do `plan.md` foram resolvidas (itens 8 e 9 com recomendação;
ambos não bloqueiam o início da implementação).
