# Data Model — Feature 005: Formulário de Registo e Edição de Iscas

Dialeto PostgreSQL, schema `public`, naming snake_case. Estende o modelo da feature 001. Aplicado por
uma migration EF Core nova (ver `plan.md` → Migrations). **Sem migração de dados** (catálogo ainda
sem cores/tamanhos reais).

---

## Alterações face à 001

### Nova: `lure_sizes` (lista de tamanhos da isca)

```sql
CREATE TABLE lure_sizes (
  id          UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  lure_id     UUID NOT NULL REFERENCES lures(id) ON DELETE CASCADE,
  code        TEXT,                          -- código curto do tamanho (ex.: SKU/ref interna)
  label       TEXT NOT NULL,                 -- designação do fabricante, ex.: "100SP"
  length_mm   NUMERIC(6,1),                  -- comprimento numérico (mm)
  weight_g    NUMERIC(6,2) NOT NULL,         -- peso (g) — obrigatório por tamanho
  sort_order  SMALLINT NOT NULL DEFAULT 0,
  -- colunas auditáveis (IAuditable), como nas restantes entidades
  is_active   BOOLEAN NOT NULL DEFAULT true,
  source      TEXT NOT NULL DEFAULT 'manual',
  deleted_at  TIMESTAMPTZ
);
CREATE INDEX idx_lure_sizes_lure ON lure_sizes(lure_id);
```

- Cardinalidade: **Isca 1 → N Tamanhos** (mínimo 1). Cascade ao apagar a isca.
- `lure_sizes` é a **fonte única** de peso/comprimento. As colunas escalares
  `lures.weight_g`/`length_mm` são **removidas** (ver abaixo e `research.md` D2).

### Alterada: `lure_colors` (lista de hex numa coluna JSONB, sem tabela filha)

```sql
ALTER TABLE lure_colors DROP COLUMN hex_primary;
ALTER TABLE lure_colors DROP COLUMN hex_secondary;
ALTER TABLE lure_colors ADD COLUMN hex_codes JSONB NOT NULL DEFAULT '[]';
-- mantém: id, lure_id, name_pt, name_en, pattern (+ colunas auditáveis)
```

- `hex_codes` guarda um **array ordenado** de objetos `{ "hex": "#00ff00", "label": "verde" }`. A
  composição de cores ("verde e amarelo") e os códigos HTML vivem todos nesta coluna — **não há
  tabela filha**, a cor fica só em `lure_colors`.
- Cada `hex` casa `^#([0-9a-fA-F]{3}|[0-9a-fA-F]{6})$` (validado no `LureWriteValidator`, já que o
  conteúdo é JSONB); `label` é a cor de base, opcional.

### Alterada: `lures` (remove escalares de peso/comprimento)

```sql
DROP INDEX IF EXISTS idx_lures_weight;
ALTER TABLE lures DROP COLUMN weight_g;
ALTER TABLE lures DROP COLUMN length_mm;
```

- O peso/comprimento passam a viver exclusivamente em `lure_sizes`. A busca/listagem da feature 001
  (que filtrava por `lures.weight_g`) passa a indexar os pesos derivados de `lure_sizes` (ver
  `research.md` D2 e tarefas de atualização do indexador/catálogo). Sem dados reais, não há backfill.

### Reutilizada: `lure_images` (foto por cor)

Sem alteração de schema. A foto opcional de cada cor usa `lure_images` com `color_id` definido. Para
esta feature considera-se **uma** foto por cor (a app garante no máximo uma linha por `color_id`).

---

## Entidades (EF Core)

- **Lure** (`lures`) — perde `WeightG`/`LengthMm` (movidos para `lure_sizes`); ganha coleção `Sizes`.
- **LureSize** (`lure_sizes`) — NOVA. `Id`, `LureId`, `Code`, `Label`, `LengthMm`, `WeightG`,
  `SortOrder` + auditáveis. Pertence a uma `Lure`.
- **LureColor** (`lure_colors`) — perde `HexPrimary`/`HexSecondary`; ganha `HexCodes` como coluna
  JSONB (array de `{hex, label}`) mapeada via owned-collection EF ou conversor JSON. Sem entidade
  filha.
- **LureImage** (`lure_images`) — inalterada; usada como foto da cor (`ColorId`).
- **LureTranslation** (`lure_translations`) — inalterada; guarda `Description` (campo "descrição").

Mapeamento EF (`AppDbContext.OnModelCreating`): adicionar `DbSet<LureSize>` com FK
`OnDelete(Cascade)` e índice acima; configurar `LureColor.HexCodes` como coleção JSONB (owned/JSON
column, sem `DbSet` próprio). A validação de formato dos hex faz-se no `LureWriteValidator` (o
conteúdo é JSONB).

---

## Regras de validação (derivadas dos FRs)

| Regra | Origem | Onde |
|-------|--------|------|
| `slug`, nome (pt), `lure_type` obrigatórios | FR-001, FR-011 | Validator + DB |
| Slug único | Edge case (colisão) | DB unique + validação amigável |
| ≥1 tamanho; cada tamanho com `weight_g` e `label` | FR-003, FR-003a | Validator |
| Cada `hex` casa `^#([0-9a-fA-F]{3}|[0-9a-fA-F]{6})$` | FR-008, FR-009 | Validator (JSONB) + UI |
| Cor válida exige nome **ou** ≥1 hex (não vazia) | Assumptions | Validator |
| Foto opcional; ausência é válida | FR-007 | — |
| Tipo/tamanho de ficheiro de foto dentro do permitido | Edge case | `BlobUploadService` |
| Hex duplicado na mesma cor é **permitido** (pode ter textura diferente) | Edge case | — |

---

## Relações (resumo)

```text
Lure 1───N LureSize  (code, label, length_mm, weight_g)
Lure 1───N LureColor (hex_codes JSONB: array de {hex, label})
Lure 1───N LureImage (ColorId opcional → foto da cor)
Lure 1───N LureTranslation (Description)
(peso/comprimento vivem só em LureSize — lures.weight_g/length_mm removidos)
```
