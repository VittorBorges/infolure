# Quickstart — Feature 005: Formulário de Registo e Edição de Iscas

Guia de validação end-to-end. Prova que o formulário regista/edita iscas com tamanhos, cores
(lista de hex) e foto opcional. Detalhes de modelo e contrato em [data-model.md](./data-model.md) e
[contracts/admin-lures-api.yaml](./contracts/admin-lures-api.yaml).

## Pré-requisitos

- PostgreSQL a correr (dev local ou Testcontainers para testes).
- Backend `apps/api` e frontend `apps/web` configurados (ver `apps/web/.env` → `NEXT_PUBLIC_API_BASE_URL`).
- Para upload de foto: `Azure:Blob:ConnectionString` configurado (Azurite em dev) — sem isto, o
  upload falha mas o resto do formulário funciona (foto é opcional).
- Utilizador com `role=admin` (JWT Supabase) para autenticar no `/admin`.

## Setup

```bash
# 1. Aplicar a migration (cria lure_sizes; lure_colors: +hex_codes JSONB, -hex_primary/secondary; lures: -weight_g/-length_mm)
cd apps/api/src/Infolure.Api
dotnet ef migrations add LureFormSizesAndColorHex
dotnet ef database update            # ou via startup com RunStartupTasks=true

# 2. Regenerar tipos do frontend a partir do contrato OpenAPI
cd ../../../../apps/web
npm run gen:admin-api-types

# 3. Arrancar backend e frontend
dotnet run --project ../../apps/api/src/Infolure.Api   # API
npm run dev                                            # web (admin em /admin)
```

## Cenário 1 — Registar isca completa (US1)

1. Autenticar como admin e abrir `/admin/lures/new`.
2. Preencher nome, slug, tipo; adicionar **2 tamanhos** (ex.: `90SP / 90mm / 9.5g` e `110SP / 110mm /
   15g`) e a descrição.
3. Gravar.

**Esperado**: 201, redireciona para a edição da isca criada; os tamanhos ficam em `lure_sizes` (fonte
única de peso/comprimento) e a busca por peso passa a derivar deles.

## Cenário 2 — Editar e preservar campos não tocados (US2)

1. Abrir `/admin/lures/{id}` de uma isca existente — todos os campos surgem pré-preenchidos.
2. Alterar só a descrição e gravar.

**Esperado**: 204; ao reabrir, a descrição mudou e tamanhos/cores permanecem iguais (SC-005).

## Cenário 3 — Cor multi-base com lista de hex e foto (US3)

1. Na isca em edição, adicionar uma cor "Tiger".
2. Adicionar **dois** hex: `#00ff00` (label "verde") e `#ffff00` (label "amarelo").
3. (Opcional) anexar uma foto → faz upload via `POST /v1/admin/media`, devolve URL.
4. Gravar e reabrir.

**Esperado**: a cor persiste com os 2 hex (ordenados) e a foto (se anexada). Sem foto, grava na mesma
(FR-007).

## Cenário 4 — Validação de hex inválido (US3 / FR-009)

1. Numa cor, introduzir `#12xz`.
2. Tentar gravar.

**Esperado**: o formulário bloqueia com mensagem a indicar o hex inválido; o backend devolve 422 com
`errors` apontando o caminho do campo (`colors[i].hex_codes[j].hex`). Nada é persistido.

## Verificação automatizada

```bash
# Backend — integração (criar/editar/validação/foto)
cd apps/api
dotnet test --filter LureWriteTests

# Frontend — E2E do formulário
cd ../web
npm run test:e2e -- lure-form.spec.ts
```

**Critérios de aceitação cobertos**: SC-001 (registo < 5 min), SC-002 (sem perda de dados ao
reabrir), SC-003 (hex inválido rejeitado 100%), SC-004 (≥2 cores de base e ≥5 hex), SC-005 (preserva
campos não alterados).
