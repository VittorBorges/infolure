# Quickstart — Feature 006

Guia de validação end-to-end. Detalhes em [data-model.md](./data-model.md) e
[contracts/admin-api-delta.yaml](./contracts/admin-api-delta.yaml).

## Pré-requisitos

- Postgres (:5433), Typesense, Redis a correr; API (`apps/api`) e web (`apps/web`).
- Azure Blob configurado em user-secrets (feature 005) para upload real de fotos.
- Sessão admin (Supabase) para o backoffice.

## Setup

```bash
# 1. Migration (rename lure_sizes→lure_configurations; +hook cols; drop lure hook_*/is_indexable)
cd apps/api/src/Infolure.Api
dotnet ef migrations add LureConfigurationsHooksAndGlobalIndexing
dotnet ef database update

# 2. Tipos do frontend (se aplicável) e arranque
cd ../../../../apps/web && npm run dev      # admin em /admin
cd ../../apps/api/src/Infolure.Api && dotnet run
```

## Cenário 1 — Indexação global (US1)

1. Abrir `/admin/settings` → alternar o interruptor global de indexação.
2. Confirmar via `GET /v1/seo` que o sitemap reflete o estado (ligado: lista iscas publicadas;
   desligado: catálogo não indexável).
3. Abrir o formulário/lista de iscas → **não** existe controlo de indexação por isca.

**Esperado**: SC-001/SC-002.

## Cenário 2 — CRUD de marcas (US2)

1. `/admin/brands/new` → criar "Rapala" → aparece na listagem `/admin/brands`.
2. Editar o nome em `/admin/brands/{id}` → persiste.
3. Eliminar → segue a política de soft-delete do painel.

**Esperado**: SC-003.

## Cenário 3 — Selecionar marca por nome (US3)

1. Em `/admin/lures/new`, no campo Marca, escrever "Rap" → surge "Rapala" → selecionar.
2. Gravar e reabrir → a marca aparece pré-selecionada pelo **nome** (nunca UUID).

**Esperado**: SC-003a.

## Cenário 4 — Configurações com anzol (US4)

1. Numa isca, adicionar 2 **configurações** (a secção chama-se "Configurações", não "Tamanhos") com
   anzol distinto (tamanho/quantidade/tipo) em cada.
2. Gravar e reabrir → cada configuração mantém o seu anzol.
3. Confirmar que não há campos de anzol ao nível da isca.

**Esperado**: SC-004.

## Cenário 5 — Múltiplas fotos por cor + upload > 1 MB (US5)

1. Numa cor, carregar **várias** fotos, incluindo uma **> 1 MB** (≤ 5 MB) → todas concluem.
2. Tentar uma foto **> 5 MB** → recusada com mensagem clara.
3. Remover uma foto → as restantes mantêm-se.

**Esperado**: SC-005.

## Verificação automatizada

```bash
cd apps/api
dotnet test --filter "LureWriteTests|BrandCrudTests|GlobalIndexingTests|MediaUploadTests"
# MediaUploadTests cobre FR-012: 2 MB aceite (limite 5 MB), 6 MB recusado, tipo inválido recusado.

cd ../web
npm run test:e2e -- lure-form.spec.ts
```

**Critérios cobertos**: SC-001..SC-005 e FR-012 (upload > 1 MB funciona; > 5 MB recusado).
