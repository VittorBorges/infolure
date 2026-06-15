# Data Model — Feature 002: Admin, Indexação e Base Auditável

Alterações ao schema da Feature 001 (dialeto PostgreSQL, schema `public`). Materializado via
**migration EF Core**. Ver [data-model.md da 001](../001-lure-catalog-mvp/data-model.md) para o schema base.

---

## 1. Colunas auditáveis (transversais)

Aplicadas a **todas** as tabelas de domínio (interface `IAuditable`). Tabelas com chave própria e
tabelas-ligação (traduções, cores, imagens, espécies-alvo, preços, auth-providers, favoritos,
inventário, reviews, votos) incluídas.

```sql
-- Aplicar a cada tabela de domínio:
ALTER TABLE <tabela>
  ADD COLUMN is_active   boolean      NOT NULL DEFAULT true,
  ADD COLUMN source      text         NOT NULL DEFAULT 'manual',
  ADD COLUMN deleted_at  timestamptz  NULL,
  ADD COLUMN created_at  timestamptz  NOT NULL DEFAULT now(),   -- onde ainda não existe
  ADD COLUMN updated_at  timestamptz  NOT NULL DEFAULT now();   -- onde ainda não existe

ALTER TABLE <tabela>
  ADD CONSTRAINT ck_<tabela>_source CHECK (source IN ('manual','automation','import'));

-- Índice parcial para acelerar as queries públicas (excluem eliminados):
CREATE INDEX ix_<tabela>_not_deleted ON <tabela> (id) WHERE deleted_at IS NULL;
```

**Notas**:
- `users.deleted_at` **já existe** (RGPD, Feature 001) — reutilizar; acrescentar apenas `is_active`,
  `source`, e `updated_at` se faltar. ⚠️ Distinguir: o `deleted_at` por soft-delete do painel é
  reversível; a eliminação RGPD efetiva é tratada à parte (FR-012a).
- `lures`, `lure_reviews`, etc. já têm `created_at`/`updated_at`/`created_at` parciais — não duplicar.
- `is_active` e `deleted_at` são **ortogonais** ao `lures.status` editorial (`draft|published|archived`).

### Regras de estado (semântica)

| Campo | Valores | Significado |
|---|---|---|
| `is_active` | `true`/`false` | Ativo/inativo (visível ao público se também publicado e não eliminado). |
| `source` | `manual`/`automation`/`import` | Origem do registo. |
| `deleted_at` | `null`/timestamp | Soft-delete reversível (null = vivo). |

**Visibilidade pública de uma isca** (FR-003, FR-003a):
```
status = 'published' AND is_active = true AND deleted_at IS NULL
AND (brand_id IS NULL OR EXISTS (
      SELECT 1 FROM brands b
      WHERE b.id = lures.brand_id AND b.is_active = true AND b.deleted_at IS NULL))
```
A relação isca↔espécie (`lure_target_species`) é **fraca**: espécie inativa/eliminada **não** oculta a
isca; apenas é filtrada da lista de espécies-alvo e dos facets (FR-003b).

## 2. Configurações da aplicação (singleton)

```sql
CREATE TABLE app_settings (
  id                    smallint PRIMARY KEY DEFAULT 1,
  seo_indexing_enabled  boolean  NOT NULL DEFAULT true,
  updated_at            timestamptz NOT NULL DEFAULT now(),
  updated_by            uuid NULL REFERENCES users(id) ON DELETE SET NULL,
  CONSTRAINT ck_app_settings_singleton CHECK (id = 1)
);

INSERT INTO app_settings (id, seo_indexing_enabled) VALUES (1, true)
  ON CONFLICT (id) DO NOTHING;   -- linha inicial preserva a indexação ligada da 001
```

O valor de `seo_indexing_enabled` é cacheado em Redis (`seo:indexing_enabled`, TTL ≤ 60s) e
invalidado na escrita (SC-005). `app_settings` **não** recebe colunas auditáveis (não é entidade de
domínio).

## 3. Indexabilidade por isca

```sql
ALTER TABLE lures
  ADD COLUMN is_indexable boolean NOT NULL DEFAULT true;
```

Uma isca entra no `sitemap.xml` e é indexável apenas se for publicamente visível (regra acima) **e**
`is_indexable = true` **e** `seo_indexing_enabled = true`.

## 4. Registo de auditoria de administração

```sql
CREATE TABLE admin_audit_log (
  id            uuid PRIMARY KEY DEFAULT gen_random_uuid(),
  actor_user_id uuid NOT NULL REFERENCES users(id) ON DELETE SET NULL,
  action        text NOT NULL,            -- create | update | activate | deactivate | delete | restore | moderate | settings_update
  entity_type   text NOT NULL,            -- ex.: 'lure', 'user', 'user_lure_inventory'
  entity_id     text NOT NULL,            -- id do registo afetado (texto: suporta PKs compostas)
  is_personal_data boolean NOT NULL DEFAULT false,
  changes       jsonb NULL,               -- instantâneo {before,after} APENAS p/ dados pessoais (FR-020a)
  created_at    timestamptz NOT NULL DEFAULT now(),
  CONSTRAINT ck_audit_action CHECK (action IN
    ('create','update','activate','deactivate','delete','restore','moderate','settings_update'))
);

CREATE INDEX ix_audit_actor   ON admin_audit_log (actor_user_id, created_at DESC);
CREATE INDEX ix_audit_entity  ON admin_audit_log (entity_type, entity_id);
CREATE INDEX ix_audit_action  ON admin_audit_log (action, created_at DESC);
```

`changes` é preenchido só quando `is_personal_data = true` (contas, favoritos, inventário). Retenção
mínima 12 meses (assumption; expurgo fora de âmbito). `admin_audit_log` não recebe colunas auditáveis.

## 5. Backfill (na migration) — SC-008

```sql
-- Defaults já preenchem is_active=true, source='manual', deleted_at=null nos registos existentes.
-- Reclassificar a origem dos dados semeados/automação:
UPDATE <tabelas de catálogo semeadas> SET source = 'automation';   -- seed/scraping da 001
-- (cargas iniciais manuais de catálogo, se distinguíveis, podem usar 'import')
-- users e dados pessoais permanecem source = 'manual'.
```

Validação: contagem total de linhas por tabela antes/depois MUST ser igual; nenhuma linha com
`deleted_at` definido pela migration; todas com `source` válido.

## 6. Resumo de impacto por tabela

| Tabela | +auditável | Outras alterações |
|---|---|---|
| brands, brand_translations | ✅ | — |
| species, species_translations | ✅ | — |
| lures | ✅ | +`is_indexable` |
| lure_translations, lure_colors, lure_images, lure_target_species, lure_retailer_prices | ✅ | — |
| users | ✅ (reutiliza `deleted_at`) | role já existe |
| user_auth_providers, user_lure_favorites, user_lure_inventory | ✅ | — |
| lure_reviews, review_helpful_votes | ✅ | `status` de review mantém-se |
| **app_settings** (nova) | ❌ | singleton |
| **admin_audit_log** (nova) | ❌ | — |
