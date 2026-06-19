# Implementation Plan: Melhorias ao Formulário de Iscas (006)

**Branch**: `006-lure-form-enhancements` | **Date**: 2026-06-19 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `specs/006-lure-form-enhancements/spec.md`

## Summary

Cinco melhorias ao backoffice de iscas (continuação da 005): (US1) remover a indexação SEO **por
isca** e expor no painel um único interruptor **global** ligar/desligar; (US2) **CRUD de marcas** no
backoffice; (US3) selecionar a marca na isca por **busca de nome** (autocomplete), sem expor UUID;
(US4) **renomear** o conceito "tamanho da isca" → **"configuração da isca"** (`LureSize`→
`LureConfiguration`, `lure_sizes`→`lure_configurations`) em todo o lado, e mover os dados de **anzol**
(tamanho/quantidade/tipo) para **cada configuração**; (US5) cada cor passa a ter **várias fotos** e
corrige-se o upload de fotos **> 1 MB** (limite 5 MB), com teste. O backend já tem o flag global SEO
(`SeoSettingsService`) e operações base de marca; esta feature promove/estende o que falta.

## Technical Context

**Language/Version**: C# / .NET 10 (backend); TypeScript 5 / Next.js 16 (App Router, React 19).

**Primary Dependencies**: ASP.NET Core, EF Core 10 + Npgsql, FluentValidation, Serilog,
`Azure.Storage.Blobs`; Next.js server actions, `@infolure/design-system`.

**Storage**: PostgreSQL; Azure Blob Storage (fotos de cor — já configurado em user-secrets).

**Testing**: xUnit + `WebApplicationFactory` + Postgres dev (:5433); Playwright E2E.

**Target Platform**: API Linux; web SSR (admin `/admin`).

**Project Type**: Web — monorepo (`apps/api`, `apps/web`, `packages/design-system`).

**Performance Goals**: autocomplete de marca responde abaixo de ~300ms p95 em catálogos típicos;
upload de foto até 5 MB com feedback.

**Constraints**: JSON snake_case; contrato OpenAPI como fonte de verdade; admin protegido por
`AdminPolicy`; logging estruturado; estados loading/erro/sucesso explícitos.

**Scale/Scope**: milhares de iscas e marcas; uma cor com várias fotos; uma isca com várias
configurações.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

- **I. Simplicidade (YAGNI)** — ✅. Reutiliza o flag SEO global e o CRUD genérico já existentes; o
  rename é mecânico (sem nova abstração). Acréscimos rastreados em *Complexity Tracking*: rename de
  tabela/entidade, colunas de anzol por configuração, múltiplas fotos por cor, e ajuste do limite de
  payload de upload.
- **II. Observabilidade** — ✅. Endpoints novos herdam `CorrelationIdMiddleware`/Serilog; upload já
  regista resultado/latência.
- **III. Contratos Explícitos** — ✅. Contrato OpenAPI admin estendido em
  `contracts/admin-api-delta.yaml` antes da implementação; remove `is_indexable` por isca e os campos
  de anzol ao nível da isca; renomeia `sizes[]`→`configurations[]`; cor passa a `photo_urls[]`. A API
  pública mantém compatibilidade onde possível (ver research D6).
- **IV. Qualidade Testável** — ✅. Integração: indexação global, CRUD de marca, escrita de isca com
  configurações+anzol, múltiplas fotos; **teste do limite de upload (>1 MB ok, >5 MB recusado)**
  cobrindo FR-012. E2E: toggle global, picker de marca, form.
- **V. UX Consistente** — ✅. Toggle global claro, autocomplete com estado vazio/erro, mensagens de
  upload compreensíveis, a11y via design system.

**Resultado**: PASS. Violações de simplicidade justificadas e rastreadas.

## Project Structure

### Documentation (this feature)

```text
specs/006-lure-form-enhancements/
├── plan.md
├── research.md
├── data-model.md
├── quickstart.md
├── contracts/admin-api-delta.yaml
├── checklists/requirements.md
└── tasks.md            # /speckit-tasks (não criado aqui)
```

### Source Code (repository root)

```text
apps/api/src/Infolure.Api/
├── Infrastructure/Persistence/
│   ├── Entities/Catalog.cs        # LureSize→LureConfiguration (+HookSize/HookCount/HookType);
│   │                              # Lure: -HookSize/-HookType/-HookCount, -IsIndexable
│   ├── AppDbContext.cs            # DbSet/keys/índices renomeados; flag SEO inalterado
│   └── Migrations/               # rename lure_sizes→lure_configurations; +hook cols; drop lure hook/is_indexable
├── Features/Admin/
│   ├── AdminController.cs         # remove PUT lures/{id}/indexable; +GET settings/indexing;
│   │                             # +GET/PUT brands/{id}; CreateBrand reutilizado
│   ├── AdminDtos.cs              # SizeInput→ConfigurationInput (+hooks); ColorInput.photo_urls[];
│   │                             # AdminLure* renomeados; +Brand DTOs; remove is_indexable do detail
│   ├── LureWriteService.cs       # configurações (+anzol) e múltiplas fotos por cor; remove is_indexable
│   ├── LureWriteValidator.cs     # valida configurations[] (peso/label); fotos
│   └── BrandService.cs           # NOVO — get/update de marca (create já existe)
├── Features/Catalog/             # CatalogDtos/LureDetailService: sizes→configurations, hooks por config
├── Features/Seo/                 # SeoSettingsService: remove filtro IsIndexable do sitemap
└── ...
apps/api/tests/Infolure.IntegrationTests/
├── LureWriteTests.cs             # atualizar para "configurations"+anzol+fotos[]
├── BrandCrudTests.cs            # NOVO
├── GlobalIndexingTests.cs       # NOVO
└── MediaUploadTests.cs          # NOVO — limite 5 MB (>1 MB ok, >5 MB recusado)

apps/web/
├── next.config.ts               # experimental.serverActions.bodySizeLimit = '5mb' (corrige bug 1 MB)
├── app/admin/
│   ├── settings/page.tsx        # NOVO — toggle global de indexação
│   └── [resource]/...           # brands ganham form (new/[id]); lures sem campo is_indexable
├── components/admin/
│   ├── BrandForm.tsx            # NOVO — CRUD de marca
│   ├── BrandPicker.tsx          # NOVO — autocomplete de marca por nome
│   ├── ConfigurationListField.tsx  # renomeado de SizeListField (+ anzol)
│   ├── ColorPhotosField.tsx     # renomeado/estendido de ColorPhotoField (lista de fotos)
│   └── LureForm.tsx             # usa BrandPicker, ConfigurationListField, fotos[]; sem is_indexable
└── lib/admin-actions.ts         # remove setIndexableAction; +setGlobalIndexing/brand actions;
                                 # createLure/updateLure usam configurations[] + photo_urls[]
```

**Structure Decision**: mantém a arquitetura das features 002–005. O rename é mecânico e amplo; a
escrita continua no `LureWriteService`. Marca ganha um serviço dedicado pequeno (`BrandService`) para
get/update, reutilizando o CRUD genérico para list/delete/restore/active.

## Complexity Tracking

| Violation | Why Needed | Simpler Alternative Rejected Because |
|-----------|------------|--------------------------------------|
| Rename `lure_sizes`→`lure_configurations` (tabela/entidade/DTOs/UI) | Pedido explícito; "configuração" descreve melhor a variante (dimensão+peso+anzol) | Manter "tamanho" rejeitado pelo utilizador; alias parcial rejeitado (ambiguidade duradoura) |
| Anzol movido de `lures` para `lure_configurations` | A spec exige anzol por configuração; tamanhos diferentes têm anzóis diferentes | Manter anzol na isca rejeitado: não modela a realidade pedida |
| Múltiplas fotos por cor (N linhas `lure_images` por cor) | Spec FR-009 | Uma foto por cor rejeitado (requisito explícito) |
| Subir limite de payload de upload (server actions 1 MB→5 MB) | Defeito real: fotos > 1 MB falham na submissão | Manter 1 MB rejeitado: é o bug a corrigir |
