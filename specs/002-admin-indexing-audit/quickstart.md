# Quickstart — Feature 002: Admin, Indexação e Base Auditável

Guia de validação ponta-a-ponta. Prova os três pilares da UI/API até à BD, Redis e Typesense.
Detalhes de schema e endpoints em [data-model.md](data-model.md) e
[contracts/admin-api.yaml](contracts/admin-api.yaml) — não duplicados aqui.

## Pré-requisitos

- Serviços locais a correr: `docker compose up -d` (Postgres :5433, Redis :6379, Typesense :8108).
- Migration aplicada: `dotnet ef database update` (com a connection string da porta 5433 — ver nota
  abaixo) e API a correr (`dotnet run`) com seed/índice.
- Frontend: `npm run gen:api-types` (inclui o novo contrato) + `npm run dev` (:3000).
- Um utilizador com `role = 'admin'` na BD (promover um utilizador de teste).

> Nota da Feature 001: o `dotnet ef` não apanha o ambiente Development (porta 5433); passar a
> connection string explicitamente, ex.: `ConnectionStrings__Postgres="Host=localhost;Port=5433;
> Database=infolure;Username=postgres;Password=dev" dotnet ef database update`.

## Cenários de validação (mapeados às user stories)

| # | Cenário | Como validar | Resultado esperado |
|---|---|---|---|
| US-01 | Ciclo de vida — desativar isca | `PUT /v1/admin/lures/{id}/active {is_active:false}` | A isca some do catálogo/busca/detalhe públicos e do índice Typesense; continua no painel |
| US-01 | Soft-delete + restore | `DELETE /v1/admin/lures/{id}` depois `POST .../restore` | Some de todas as listagens; reaparece com o `is_active` anterior |
| US-01 | Cascata por pai (marca) | Desativar a marca de uma isca | A isca deixa de ser pública (FR-003a); o seu `is_active` próprio não muda |
| US-01 | Relação fraca (espécie) | Desativar/eliminar uma espécie-alvo | A isca **continua** pública; a espécie cai da lista/facets (FR-003b) |
| US-01 | Origem | Listar com `?source=automation` vs `manual` | Dados de seed → `automation`; criados no painel → `manual` |
| US-02 | Acesso negado a não-admin | Chamar `/v1/admin/*` sem role admin | 403 em todas as rotas (FR-007) |
| US-02 | Dashboard | `GET /v1/admin/dashboard` | Cadastros 7/30d + série, iscas por status/source/active, reviews pendentes, totais |
| US-02 | CRUD + filtros | `GET /v1/admin/lures?q=...&include=all&page=1` | Resultados filtrados/paginados, com inativos/eliminados quando pedido |
| US-02 | Dados pessoais + RGPD | Editar inventário/favorito/conta no painel | Aviso RGPD antes de confirmar; entrada de auditoria com antes→depois |
| US-02 | Bloqueios | Desativar a própria conta / o último admin | 409 (FR-013) |
| US-02 | Utilizador inativo não autentica | Desativar um utilizador com sessão ativa | Próxima requisição autenticada → 401 (FR-013a) |
| US-03 | Toggle global OFF | `PUT /v1/admin/settings/indexing {enabled:false}` | Em < 60s: `robots.txt` proíbe, `sitemap.xml` vazio, detalhe com `noindex` (SC-005) |
| US-03 | Toggle global ON | `{enabled:true}` | `robots.txt` permite; `sitemap.xml` lista iscas published+active+indexable |
| US-03 | Não-indexável por isca | `PATCH /v1/admin/lures/{id} {is_indexable:false}` | Só essa isca sai do sitemap e fica `noindex`; restantes intactas |
| US-03 | Perfis sempre noindex | Abrir `/u/:username` | Meta `noindex` independentemente do flag global (FR-018) |
| US-04 | Auditoria | `GET /v1/admin/audit?action=delete` | Cada ação de escrita tem entrada (autor/ação/entidade/registo/momento) |

## Verificações no frontend

```bash
curl -s http://localhost:3000/robots.txt        # Allow/Disallow conforme flag
curl -s http://localhost:3000/sitemap.xml        # vazio quando OFF; iscas elegíveis quando ON
curl -s http://localhost:3000/iscas/<slug> | grep -i 'noindex'   # presente quando OFF ou is_indexable=false
```

## Gates de qualidade (Princípios II e IV)

- [ ] **Regressão**: a suite verde da 001 (15 integração + 6 E2E) continua a passar após o global
      query filter (SC-009).
- [ ] 0 registos eliminados/inativos em qualquer superfície pública (SC-002).
- [ ] Migração sem perda de linhas; contagens antes/depois iguais; origens atribuídas (SC-008).
- [ ] Toda ação de escrita do admin gera entrada de auditoria (SC-007).
- [ ] Toggle de indexação reflete-se em < 60s (SC-005).
- [ ] Logs estruturados sem PII; instantâneos de dados pessoais só no `admin_audit_log`.

> ⚠️ Next.js 16: confirmar a API de `robots.ts`/`sitemap.ts`/`generateMetadata` e a convenção de
> proteção de rotas (`middleware`→`proxy`) em `node_modules/next/dist/docs/` antes de codar
> (`apps/web/AGENTS.md`).

> Detalhes de implementação (corpos de serviços/controllers, migration completa, suites de teste)
> pertencem a `tasks.md` e à fase de implementação.
