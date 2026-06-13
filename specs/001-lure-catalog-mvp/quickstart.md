# Quickstart — Feature 001: Lure Catalog MVP

Guia de validação ponta-a-ponta. Prova que o catálogo funciona da UI (Next.js) até à API (.NET),
Postgres, Typesense e auth. Detalhes de schema e endpoints estão em [data-model.md](data-model.md)
e [contracts/api.yaml](contracts/api.yaml) — não duplicados aqui.

## Pré-requisitos

- **.NET 10 SDK** (LTS)
- **Node.js 20+** (para Next.js 15)
- **Docker** (Postgres + Redis + Typesense locais)
- Projeto **Supabase** (dev) com Google + Microsoft habilitados, ou chaves de teste

## Setup de serviços locais

```bash
# Postgres, Redis e Typesense via containers (dev)
docker run -d --name infolure-pg   -e POSTGRES_PASSWORD=dev -p 5432:5432 postgres:16
docker run -d --name infolure-redis -p 6379:6379 redis:7
docker run -d --name infolure-ts   -e TYPESENSE_API_KEY=devkey -p 8108:8108 \
  typesense/typesense:27.0 --data-dir /data
```

## Backend (.NET)

```bash
cd apps/api/src/Infolure.Api
dotnet restore
dotnet ef database update          # aplica migrations (schema de data-model.md)
dotnet run                         # API em https://localhost:5001 (ajustar appsettings)
```

- Seed de dev: 20 marcas, 20 espécies (com PT), 50 iscas — via script de seed do projeto.
- OpenAPI servido em `/swagger` deve corresponder a `contracts/api.yaml`.

## Frontend (Next.js)

```bash
cd apps/web
npm install
npm run gen:api-types              # gera tipos de contracts/api.yaml (openapi-typescript)
npm run dev                        # app em http://localhost:3000
```

## Cenários de validação (mapeados às user stories)

| # | Cenário | Como validar | Resultado esperado |
|---|---|---|---|
| US-01 | Navegar catálogo com filtros (anônimo) | Abrir `/iscas`, aplicar filtros tipo/espécie/peso | Resultados atualizam sem reload; estado nos query params; empty state com CTA "limpar filtros" quando 0 resultados |
| US-02 | Buscar por nome/marca/modelo | Digitar ≥ 2 chars na busca | Autocomplete < 150ms; resultados por relevância; combina com filtros |
| US-03 | Página de detalhe indexável | Abrir `/iscas/:slug`; `curl` da página | Ficha técnica completa; HTML renderizado no servidor (SSR); canonical com slug; secção de preços oculta se sem dados |
| US-04 | Login Google / MSA / email | Fluxo de sign-in; primeiro login OAuth | Login funciona; pede username (3–20, único); sessão persiste; linking de 2º provedor nas settings |
| US-05 | Favoritar isca | Tocar no coração (autenticado e anônimo) | Toggle otimista; anônimo redireciona p/ login com return URL; "Meus Favoritos" lista com mesmo filtro/sort |
| US-06 | Inventário "possuo" | Adicionar com quantidade/condição/notas | Edição e remoção funcionam; cores variantes; "Meu Inventário" agrupado por tipo |
| US-07 | Perfil público | Abrir `/u/:username` | Mostra username, avatar, membro desde, contagens; sem email/nome real |
| US-08 | Avaliar isca | Submeter rating 1–5 + texto | Uma review por user/isca (edit/delete); ordenadas por recente; "útil" 1 voto/user |

## Gates de qualidade (Princípios II e IV)

- [ ] Logs estruturados (Serilog) com correlation-id visível por requisição; sem PII.
- [ ] Testes verdes: `dotnet test` (unidade + integração) e `npm test` / Playwright (E2E).
- [ ] Rate limiting responde 429 ao exceder 100/min (anônimo) ou 300/min (autenticado).
- [ ] Performance: listagem p95 < 200ms; autocomplete primeira sugestão < 150ms.

> Detalhes de implementação (corpos de serviços/controllers, migrations completas, suites de teste)
> pertencem a `tasks.md` e à fase de implementação, não a este guia.
