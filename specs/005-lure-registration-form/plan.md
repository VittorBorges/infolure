# Implementation Plan: Formulário de Registo e Edição de Iscas

**Branch**: `005-lure-registration-form` | **Date**: 2026-06-18 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `specs/005-lure-registration-form/spec.md`

## Summary

Disponibilizar no backoffice admin um formulário completo para **registar e editar iscas** com
todas as propriedades. A feature evolui o modelo da 001/002 em três pontos: (1) a isca passa a ter
uma **lista de tamanhos**, cada um com rótulo + comprimento (mm) + peso (g); (2) cada **cor** passa a
ter uma **lista aberta de códigos hex** (cada hex opcionalmente rotulado com a cor de base, ex.:
"verde"), substituindo o par fixo `hex_primary`/`hex_secondary`; (3) cada cor tem uma **foto
opcional**. Backend estende os endpoints admin existentes (`POST`/`PATCH /v1/admin/lures`) para
escrita transacional das coleções, acrescenta upload de foto via Azure Blob, e o frontend ganha um
formulário cliente (Next.js App Router + `@infolure/design-system`) com listas dinâmicas e validação.

## Technical Context

**Language/Version**: C# / .NET 10 (LTS) no backend; TypeScript 5 / Next.js 16 (App Router, React 19)
no frontend.

**Primary Dependencies**: ASP.NET Core Web API, EF Core 10 + Npgsql, FluentValidation, Serilog,
`Azure.Storage.Blobs` (já presente no `.csproj`); Next.js server actions, `@infolure/design-system`,
`openapi-typescript` (tipos gerados a partir do contrato).

**Storage**: PostgreSQL (catálogo); Azure Blob Storage (West Europe) para fotos de cor.

**Testing**: xUnit + `WebApplicationFactory` + Testcontainers (Postgres real) no backend; Playwright
E2E no frontend.

**Target Platform**: API em servidor Linux; web SSR (admin sob `/admin`, isolado).

**Project Type**: Web — monorepo npm workspaces: `apps/api` (.NET), `apps/web` (Next.js),
`packages/design-system`.

**Performance Goals**: gravação do formulário (sem upload) < 1s p95; upload de foto com feedback de
progresso/estado. Sem alvos de throughput específicos (volume de edição é baixo, dezenas de editores).

**Constraints**: JSON em snake_case (`JsonNamingPolicy.SnakeCaseLower`); contrato OpenAPI é a fonte
de verdade (Princípio III); acesso restrito por `AuthExtensions.AdminPolicy` (role admin); logging
estruturado com correlation-id (Princípio II); estados de loading/erro/sucesso explícitos (Princípio V).

**Scale/Scope**: catálogo na ordem de milhares de iscas; uma isca com várias dezenas de tamanhos/cores
no limite superior. Sem migração de dados (catálogo ainda sem cores/tamanhos reais).

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

- **I. Simplicidade Primeiro (YAGNI)** — ✅ com ressalvas registadas. Reutilizam-se endpoints,
  serviços e padrões existentes (admin CRUD, server actions, design system). Três acréscimos de
  complexidade são rastreados em *Complexity Tracking*: (a) nova tabela filha `lure_sizes` (cores
  ficam só em `lure_colors`, com os hex numa coluna JSONB — sem tabela filha de hex); (b) `lure_sizes`
  como fonte única de peso (remove escalares de `lures`), o que obriga a ajustar o indexador/catálogo
  da 001; (c) serviço de upload Blob.
  **Não** se adiciona `react-hook-form`/`zod` — mantém-se o padrão nativo `useState`/`useTransition`.
- **II. Observabilidade por Padrão** — ✅. Os novos endpoints herdam o `CorrelationIdMiddleware` e
  Serilog; o serviço de upload regista início/fim/resultado e latência; erros de validação são
  logados com contexto (sem PII/segredos).
- **III. Contratos Explícitos** — ✅. O contrato OpenAPI admin é estendido em
  `contracts/admin-lures-api.yaml` antes da implementação; os tipos do frontend são regenerados
  (`npm run gen:admin-api-types`). As mudanças no modelo de cor e a remoção dos escalares de peso são
  breaking face à 001, mas **sem dados a migrar** (catálogo vazio) — documentado no contrato e em
  `research.md`. O parâmetro público de filtro por peso mantém-se; só muda a sua origem (indexação a
  partir de `lure_sizes`).
- **IV. Qualidade Testável** — ✅. Testes de integração xUnit cobrem criar/editar com tamanhos+cores,
  validação de hex inválido, foto opcional, e preservação de campos não alterados; E2E Playwright
  cobre o caminho feliz do formulário e os erros de validação.
- **V. Experiência do Usuário Consistente** — ✅. O formulário trata loading/erro/sucesso, mensagens
  compreensíveis (incl. qual hex é inválido), e acessibilidade básica (labels, navegação por teclado)
  via componentes do design system.

**Resultado**: PASS. As violações de simplicidade estão justificadas e rastreadas abaixo.

## Project Structure

### Documentation (this feature)

```text
specs/005-lure-registration-form/
├── plan.md              # Este ficheiro
├── research.md          # Fase 0 — decisões de design
├── data-model.md        # Fase 1 — DDL e entidades
├── quickstart.md        # Fase 1 — guia de validação
├── contracts/
│   └── admin-lures-api.yaml   # Fase 1 — contrato OpenAPI (extensão admin)
├── checklists/
│   └── requirements.md  # Checklist de qualidade da spec
└── tasks.md             # Fase 2 — gerado por /speckit-tasks (NÃO criado aqui)
```

### Source Code (repository root)

```text
apps/api/src/Infolure.Api/
├── Infrastructure/Persistence/
│   ├── Entities/Catalog.cs             # +LureSize; Lure: -WeightG/-LengthMm; LureColor: -Hex*, +HexCodes JSONB
│   ├── AppDbContext.cs                 # DbSet<LureSize> + cascade; LureColor.HexCodes JSONB; remove idx_lures_weight
│   ├── Search/LureIndexer.cs          # indexar peso a partir de lure_sizes (min/max)
│   └── Migrations/                     # migration EF Core (lure_sizes; lure_colors.hex_codes; drop weight_g/length_mm)
├── Features/Admin/
│   ├── AdminController.cs              # CreateLure/UpdateLure estendidos; endpoint de upload de foto
│   ├── AdminDtos.cs                    # DTOs ricos (sizes[], colors[]{hex_codes[]}, photo)
│   ├── LureWriteService.cs            # NOVO — escrita transacional da isca + coleções + denorm
│   └── LureWriteValidator.cs         # NOVO — FluentValidation (hex, obrigatórios, slug único)
├── Features/Media/
│   └── BlobUploadService.cs           # NOVO — upload Azure Blob, devolve URL pública
├── Features/Catalog/
│   └── CatalogController.cs           # ajustar filtro público de peso para usar pesos de lure_sizes
└── ...
apps/api/tests/Infolure.IntegrationTests/
└── LureWriteTests.cs                  # NOVO — criar/editar/validação/foto

apps/web/
├── app/admin/lures/
│   ├── new/page.tsx                    # NOVO — registo
│   └── [id]/page.tsx                   # edição (substitui form mínimo atual)
├── components/admin/
│   ├── LureForm.tsx                    # NOVO — formulário principal (create+edit)
│   ├── SizeListField.tsx              # NOVO — lista dinâmica de tamanhos
│   ├── ColorListField.tsx            # NOVO — lista dinâmica de cores
│   ├── HexCodeListField.tsx          # NOVO — lista de hex por cor (+ validação)
│   └── ColorPhotoField.tsx           # NOVO — upload/preview de foto opcional
├── lib/
│   ├── admin-actions.ts               # +createLureAction / updateLureAction (payload completo)
│   ├── api-types.ts / admin-api-types # regenerados do contrato
│   └── hex.ts                          # NOVO — validação/normalização de hex (partilhada UI)
└── tests/e2e/
    └── lure-form.spec.ts              # NOVO — E2E do formulário
```

**Structure Decision**: Mantém-se a arquitetura existente (vertical slices no backend, App Router +
componentes por contexto no frontend, design system partilhado). A feature acrescenta um serviço de
escrita dedicado (`LureWriteService`) por a operação ser transacional sobre várias tabelas — o CRUD
genérico (`AdminResourceService`) não cobre coleções aninhadas.

## Complexity Tracking

| Violation | Why Needed | Simpler Alternative Rejected Because |
|-----------|------------|--------------------------------------|
| Tabela filha `lure_sizes` (fonte única de peso) + ajuste do indexador/catálogo da 001 | A spec exige lista de tamanhos (code+rótulo+mm+g); o utilizador optou por fonte única (sem dados reais), removendo os escalares de `lures` | Denormalizar um representativo em `lures.weight_g` rejeitado pelo utilizador: mantinha duplicação só para não tocar na busca; sem dados, não compensa |
| Coluna `hex_codes JSONB` em `lure_colors` (sem tabela filha) | A spec exige lista aberta de hex por cor; o utilizador pediu que a cor fique só em `lure_colors` | Tabela filha `lure_color_hex_codes` preterida a pedido do utilizador; validação de formato passa para o `LureWriteValidator` |
| Denormalização de tamanho representativo em `lures.weight_g`/`length_mm` | A busca/listagem da 001 (índice `idx_lures_weight`, Typesense) lê estas colunas; mantê-las sincronizadas evita tocar/quebrar a feature 001 nesta entrega | Migrar a busca para as novas tabelas rejeitado: alarga o âmbito para fora do formulário admin e arrisca o contrato público (Princípio III) |
| `BlobUploadService` (Azure Blob) | A spec pede foto opcional por cor; anexar ficheiro exige armazenamento | Guardar só URL externa rejeitado: UX fraca e não verificável; a dependência `Azure.Storage.Blobs` já existe no projeto |
