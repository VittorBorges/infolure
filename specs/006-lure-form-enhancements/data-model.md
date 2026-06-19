# Data Model — Feature 006

Dialeto PostgreSQL, snake_case. Estende/renomeia o modelo da 005. Uma migration EF Core.
**Sem migração de dados de negócio** (catálogo sem dados reais relevantes), mas o rename de tabela
preserva eventuais linhas.

---

## Rename: `lure_sizes` → `lure_configurations` (+ anzol)

```sql
ALTER TABLE lure_sizes RENAME TO lure_configurations;
ALTER INDEX idx_lure_sizes_lure RENAME TO idx_lure_configurations_lure;
-- (a FK e a check ck_lure_sizes_source são renomeadas pela migration EF)

ALTER TABLE lure_configurations ADD COLUMN hook_size  TEXT;
ALTER TABLE lure_configurations ADD COLUMN hook_type  TEXT;
ALTER TABLE lure_configurations ADD COLUMN hook_count SMALLINT;
```

Colunas finais de `lure_configurations`: `id, lure_id, code, label, length_mm, weight_g, hook_size,
hook_type, hook_count, sort_order, is_active, source, deleted_at`.

**Entidade**: `LureConfiguration` (antes `LureSize`) + `Lure.Configurations` (antes `Lure.Sizes`).

## Alterada: `lures` (remove anzol e indexação por isca)

```sql
ALTER TABLE lures DROP COLUMN hook_size;
ALTER TABLE lures DROP COLUMN hook_type;
ALTER TABLE lures DROP COLUMN hook_count;
ALTER TABLE lures DROP COLUMN is_indexable;
```

- `Lure` perde `HookSize`/`HookType`/`HookCount` (movidos para a configuração) e `IsIndexable`
  (substituído pelo flag global).

## Fotos por cor: sem mudança de schema

- `lure_images` já suporta **N linhas por `color_id`**. Múltiplas fotos por cor = várias linhas
  (ordem por `sort_order`). O `LureWriteService` passa a aceitar `photo_urls[]` por cor e a fazer
  replace das imagens dessa cor (escopo `color_id IS NOT NULL`, como na 005).

## Inalterado / reutilizado

- **Configuração de Indexação (global)**: `app_settings.seo_indexing_enabled` + `SeoSettingsService`
  (feature 002). Promovido ao painel; sem alteração de schema.
- **Marca**: `brands` + `brand_translations` (nome `pt`). CRUD: create já existe; +get/update.
- **Cor da Isca**: `lure_colors` (+ `hex_codes` JSONB da 005) — inalterada.

---

## Regras de validação (derivadas dos FRs)

| Regra | Origem | Onde |
|-------|--------|------|
| ≥1 configuração; cada uma com `label` e `weight_g` > 0 | FR-007/013 | `LureWriteValidator` |
| Anzol (`hook_*`) opcional por configuração | FR-007 | — |
| `hex` válidos por cor (duplicados permitidos) | 005 | `LureWriteValidator` |
| Foto ≤ 5 MB, tipo JPEG/PNG/WebP | FR-010/011 | `BlobUploadService` (validação testável) |
| Marca selecionada existe (ou nenhuma) | FR-006 | seleção por id resolvido do picker |
| Nome de marca obrigatório no CRUD | FR-003a | `BrandService`/validação |

---

## Relações (resumo)

```text
Lure 1───N LureConfiguration (code, label, length_mm, weight_g, hook_size, hook_type, hook_count)
Lure 1───N LureColor (hex_codes JSONB)
LureColor 1───N LureImage (color_id) — várias fotos por cor
Lure N───1 Brand (opcional; selecionada por nome na UI)
AppSetting.seo_indexing_enabled — flag global (substitui Lure.is_indexable)
```
