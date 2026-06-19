# Research — Feature 006

Decisões de design da Fase 0.

## D1 — Indexação SEO: só flag global, remover `is_indexable` por isca

**Decision**: Remover `Lure.IsIndexable` (coluna + endpoint `PUT /v1/admin/lures/{id}/indexable`). A
elegibilidade do sitemap passa a `Status == "published" && IsActive` filtrada pelo **flag global**
`SeoSettingsService` (já existente). No painel, expor o flag via `GET`+`PUT /v1/admin/settings/indexing`.

**Rationale**: O flag global já existe (feature 002); o controlo por isca é redundante e o utilizador
quer um único interruptor. `SeoSettingsService.cs:69` deixa de filtrar por `IsIndexable`.

**Alternatives considered**: manter ambos — rejeitado (redundância, pedido explícito). Sem migração de
valores (FR/Assumptions).

## D2 — CRUD de marcas: estender o existente

**Decision**: Reutilizar o CRUD genérico (list/delete/restore/active) e o `POST /v1/admin/brands`.
Acrescentar `GET /v1/admin/brands/{id}` e `PUT /v1/admin/brands/{id}` (nome/slug) num `BrandService`.
UI: form de marca em `/admin/brands/new` e `/admin/brands/[id]` (no padrão `[resource]`).

**Rationale**: Minimiza superfície nova; alinha com o padrão da 002. Marca = `Brand` + `BrandTranslation`
(nome por locale `pt`).

**Alternatives considered**: controller dedicado completo — desnecessário (YAGNI).

## D3 — Seleção de marca por nome (autocomplete)

**Decision**: `BrandPicker` (client) que consome `GET /v1/admin/brands?q=<nome>` (lista já devolve
`id`+`name`) com debounce. Guarda `brand_id` internamente; mostra só o nome. Na edição, o detalhe da
isca passa a incluir `brand_name` (a par de `brand_id`) para pré-preencher sem nova chamada.

**Rationale**: Endpoint de busca já existe; só falta o componente e expor `brand_name` no detalhe.

**Alternatives considered**: carregar todas as marcas no cliente — rejeitado (não escala).

## D4 — Rename "tamanho" → "configuração"

**Decision**: Renomear em todo o lado: entidade `LureSize`→`LureConfiguration`; tabela
`lure_sizes`→`lure_configurations` (+ índice/FK); navegação `Lure.Sizes`→`Lure.Configurations`; DTOs
`SizeInput`→`ConfigurationInput`, `AdminLureSizeDto`→`AdminLureConfigurationDto`, público
`LureSizeDto`→`LureConfigurationDto` e campo `sizes[]`→`configurations[]`; componentes
`SizeListField`→`ConfigurationListField`. Migração EF de **rename** (preserva dados, embora o catálogo
não tenha dados reais). Nome distinto da "Configuração de Indexação" global.

**Rationale**: Pedido explícito; a variante agrupa dimensão+peso+anzol, "configuração" é mais correto.

**Alternatives considered**: manter "size" no código e só mudar rótulos de UI — rejeitado (incoerência
duradoura entre código, API e UI).

## D5 — Anzol por configuração

**Decision**: Mover `HookSize`/`HookType`/`HookCount` de `Lure` para `lure_configurations` (colunas
`hook_size`, `hook_type`, `hook_count`). Remover do `Lure` e dos DTOs ao nível da isca. Cada item de
`configurations[]` ganha estes três campos (opcionais).

**Rationale**: Modela a realidade (anzol varia por configuração). Sem migração de dados de anzol.

**Alternatives considered**: duplicar (isca + configuração) — rejeitado (duas fontes de verdade).

## D6 — Múltiplas fotos por cor + correção do limite de upload

**Decision**:
- **Modelo**: já suportado por `lure_images` (N linhas por `color_id`); sem mudança de schema. O
  payload de cor passa de `photo_url` (uma) para `photo_urls[]` (lista, ordenada por `sort_order`); o
  `LureWriteService` cria/substitui as imagens de cor a partir da lista.
- **Bug do 1 MB**: a causa é o limite por omissão dos **Server Actions do Next.js (1 MB)**. Corrige-se
  em `next.config.ts` com `experimental.serverActions.bodySizeLimit = '5mb'`. O backend já aceita 5 MB
  (`BlobUploadService` MaxBytes) e o controller tem `RequestSizeLimit(6 MB)`.
- **Teste (FR-012)**: como o Azure Blob não está configurado no ambiente `Testing` (devolve 503), o
  teste de limite incide na **validação de tamanho/tipo** do `BlobUploadService` extraída para um
  método puro testável: 2 MB → aceite (passa o limite); 6 MB → `TooLarge`; tipo inválido →
  `UnsupportedType`. Isto prova que o limite é 5 MB e não 1 MB sem depender do Azure.

**Rationale**: Correção na camada certa (Next), com teste determinístico no backend. A API pública de
detalhe passa a expor `photos[]` por cor mantendo `primary_image_url` derivado.

**Alternatives considered**: configurar Azurite nos testes para upload real — mais setup; preterido a
favor do teste puro da validação (cobre o requisito de forma fiável).
