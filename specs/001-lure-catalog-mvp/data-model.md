# Data Model — Feature 001: Lure Catalog MVP

Full DDL in PostgreSQL dialect. All tables in schema `public`.
Extensions required: `pgcrypto` (for `gen_random_uuid()`).

---

## Catalog Domain

```sql
CREATE TABLE brands (
  id         UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  slug       TEXT UNIQUE NOT NULL
);

CREATE TABLE brand_translations (
  brand_id    UUID REFERENCES brands(id) ON DELETE CASCADE,
  locale      TEXT NOT NULL,
  name        TEXT NOT NULL,
  description TEXT,
  PRIMARY KEY (brand_id, locale)
);

CREATE TABLE species (
  id         UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  slug       TEXT UNIQUE NOT NULL,
  family     TEXT,
  water_type TEXT CHECK (water_type IN ('freshwater','saltwater','both'))
);

CREATE TABLE species_translations (
  species_id  UUID REFERENCES species(id) ON DELETE CASCADE,
  locale      TEXT NOT NULL,
  common_name TEXT NOT NULL,
  PRIMARY KEY (species_id, locale)
);

CREATE TABLE lures (
  id               UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  slug             TEXT UNIQUE NOT NULL,
  brand_id         UUID REFERENCES brands(id),
  lure_type        TEXT NOT NULL,
  water_type       TEXT CHECK (water_type IN ('freshwater','saltwater','both')),
  weight_g         NUMERIC(6,2),
  length_mm        NUMERIC(6,1),
  depth_min_m      NUMERIC(5,1),
  depth_max_m      NUMERIC(5,1),
  hook_size        TEXT,
  hook_type        TEXT,
  hook_count       SMALLINT,
  material         TEXT,
  attributes       JSONB DEFAULT '{}',
  price_6m_min_eur NUMERIC(8,2),
  price_6m_max_eur NUMERIC(8,2),
  price_6m_avg_eur NUMERIC(8,2),
  price_6m_updated_at TIMESTAMPTZ,
  status           TEXT DEFAULT 'draft'
                   CHECK (status IN ('draft','published','archived')),
  created_at       TIMESTAMPTZ DEFAULT now(),
  updated_at       TIMESTAMPTZ DEFAULT now()
);

-- Generated columns for hot JSONB attribute filters
ALTER TABLE lures ADD COLUMN jig_head_weight_g NUMERIC
  GENERATED ALWAYS AS ((attributes->>'jig_head_weight_g')::numeric) STORED;

CREATE INDEX idx_lures_type        ON lures(lure_type);
CREATE INDEX idx_lures_water       ON lures(water_type);
CREATE INDEX idx_lures_weight      ON lures(weight_g);
CREATE INDEX idx_lures_brand       ON lures(brand_id);
CREATE INDEX idx_lures_status      ON lures(status);
CREATE INDEX idx_lures_attrs_gin   ON lures USING GIN (attributes);
CREATE INDEX idx_lures_jig_head    ON lures(jig_head_weight_g)
  WHERE lure_type = 'jig' AND jig_head_weight_g IS NOT NULL;

CREATE TABLE lure_translations (
  lure_id     UUID REFERENCES lures(id) ON DELETE CASCADE,
  locale      TEXT NOT NULL,
  name        TEXT NOT NULL,
  description TEXT,
  PRIMARY KEY (lure_id, locale)
);

CREATE TABLE lure_colors (
  id             UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  lure_id        UUID REFERENCES lures(id) ON DELETE CASCADE,
  name_pt        TEXT NOT NULL,
  name_en        TEXT,
  hex_primary    TEXT,
  hex_secondary  TEXT,
  pattern        TEXT
);

CREATE TABLE lure_images (
  id          UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  lure_id     UUID REFERENCES lures(id) ON DELETE CASCADE,
  color_id    UUID REFERENCES lure_colors(id),
  url         TEXT NOT NULL,
  sort_order  SMALLINT DEFAULT 0,
  is_primary  BOOLEAN DEFAULT false
);

CREATE TABLE lure_target_species (
  lure_id     UUID REFERENCES lures(id) ON DELETE CASCADE,
  species_id  UUID REFERENCES species(id) ON DELETE CASCADE,
  confidence  TEXT CHECK (confidence IN ('primary','secondary')),
  PRIMARY KEY (lure_id, species_id)
);

CREATE TABLE lure_retailer_prices (
  id          UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  lure_id     UUID NOT NULL REFERENCES lures(id) ON DELETE CASCADE,
  retailer    TEXT NOT NULL,
  url         TEXT,
  price_eur   NUMERIC(8,2) NOT NULL CHECK (price_eur > 0),
  in_stock    BOOLEAN DEFAULT true,
  updated_at  TIMESTAMPTZ DEFAULT now()
);
```

## User Domain

```sql
CREATE TABLE users (
  id            UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  email         TEXT UNIQUE,
  username      TEXT UNIQUE,
  display_name  TEXT,
  avatar_url    TEXT,
  locale        TEXT DEFAULT 'pt',
  role          TEXT DEFAULT 'user' CHECK (role IN ('user','admin')),
  created_at    TIMESTAMPTZ DEFAULT now(),
  last_login_at TIMESTAMPTZ,
  deleted_at    TIMESTAMPTZ
);

CREATE TABLE user_auth_providers (
  id            UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  user_id       UUID REFERENCES users(id) ON DELETE CASCADE,
  provider      TEXT NOT NULL,
  provider_uid  TEXT NOT NULL,
  email         TEXT,
  linked_at     TIMESTAMPTZ DEFAULT now(),
  UNIQUE (provider, provider_uid)
);

CREATE TABLE user_lure_favorites (
  user_id    UUID REFERENCES users(id) ON DELETE CASCADE,
  lure_id    UUID REFERENCES lures(id) ON DELETE CASCADE,
  created_at TIMESTAMPTZ DEFAULT now(),
  PRIMARY KEY (user_id, lure_id)
);

CREATE TABLE user_lure_inventory (
  id         UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  user_id    UUID REFERENCES users(id) ON DELETE CASCADE,
  lure_id    UUID REFERENCES lures(id) ON DELETE CASCADE,
  color_id   UUID REFERENCES lure_colors(id),
  quantity   SMALLINT DEFAULT 1 CHECK (quantity > 0),
  condition  TEXT CHECK (condition IN ('new','good','used','lost')),
  notes      TEXT CHECK (char_length(notes) <= 200),
  added_at   TIMESTAMPTZ DEFAULT now(),
  UNIQUE (user_id, lure_id, color_id)
);

CREATE INDEX idx_favorites_user ON user_lure_favorites(user_id);
CREATE INDEX idx_inventory_user ON user_lure_inventory(user_id);
```

## Content Domain

```sql
CREATE TABLE lure_reviews (
  id            UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  lure_id       UUID REFERENCES lures(id) ON DELETE CASCADE,
  user_id       UUID REFERENCES users(id) ON DELETE SET NULL,
  rating        SMALLINT NOT NULL CHECK (rating BETWEEN 1 AND 5),
  body          TEXT CHECK (char_length(body) <= 1000),
  locale        TEXT DEFAULT 'pt',
  helpful_count INT DEFAULT 0,
  status        TEXT DEFAULT 'published'
                CHECK (status IN ('pending','published','hidden')),
  created_at    TIMESTAMPTZ DEFAULT now(),
  UNIQUE (lure_id, user_id)
);

CREATE TABLE review_helpful_votes (
  review_id  UUID REFERENCES lure_reviews(id) ON DELETE CASCADE,
  user_id    UUID REFERENCES users(id) ON DELETE CASCADE,
  created_at TIMESTAMPTZ DEFAULT now(),
  PRIMARY KEY (review_id, user_id)
);

CREATE INDEX idx_reviews_lure ON lure_reviews(lure_id, status);
CREATE INDEX idx_reviews_user ON lure_reviews(user_id);
```

---

## Typesense Collection Schema

```json
{
  "name": "lures",
  "fields": [
    { "name": "id",              "type": "string" },
    { "name": "slug",            "type": "string" },
    { "name": "name_pt",         "type": "string" },
    { "name": "name_en",         "type": "string", "optional": true },
    { "name": "name_es",         "type": "string", "optional": true },
    { "name": "brand_name",      "type": "string", "facet": true },
    { "name": "lure_type",       "type": "string", "facet": true },
    { "name": "water_type",      "type": "string", "facet": true },
    { "name": "weight_g",        "type": "float",  "optional": true },
    { "name": "depth_min_m",     "type": "float",  "optional": true },
    { "name": "depth_max_m",     "type": "float",  "optional": true },
    { "name": "target_species",  "type": "string[]", "facet": true },
    { "name": "price_6m_avg_eur","type": "float",  "optional": true },
    { "name": "status",          "type": "string" },
    { "name": "popularity_score","type": "int32" }
  ],
  "default_sorting_field": "popularity_score"
}
```

`popularity_score` = favorites count + inventory count. Recalculated nightly by a
background job.

---

## Notes on Forward Compatibility

The following columns/tables exist in the schema but have **no application code** in v1.
They are present to avoid future breaking migrations:

| Element | Purpose in v2+ |
|---|---|
| `users.deleted_at` | RGPD soft-delete; erasure flow in v1 but no automatic hard-delete job until v2. |
| `users.role` | Admin flag used in v1 backoffice; RBAC expansion in v2. |
| `lure_retailer_prices` table | Price tracking UI in v3; in v1 it's just a link store. |
| `lure_price_history` | Not created in v1. Schema designed so it slots in as additive migration. |
| `fishing_sessions`, `catches` | Not created in v1. lat/lng pattern established here. |
