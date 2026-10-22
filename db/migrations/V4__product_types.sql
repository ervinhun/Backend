-- V4__product_types.sql
-- Product data & expiry defaults (DATABASE_PLAN.md §3, part 1).
-- `product_categories` is a flat lookup list; `product_types` holds the
-- per-product-type default shelf life that feeds the expiry fallback
-- chain. `product_catalog` and `expiry_overrides` follow in
-- V5__product_catalog.sql since both reference `product_types`.

CREATE TABLE product_categories (
    id          UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    name        TEXT NOT NULL UNIQUE,
    created_at  TIMESTAMPTZ NOT NULL DEFAULT now(),
    updated_at  TIMESTAMPTZ NOT NULL DEFAULT now(),
    deleted_at  TIMESTAMPTZ
);

CREATE TABLE product_types (
    id                    UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    category_id           UUID REFERENCES product_categories(id),
    name                  TEXT NOT NULL,
    default_expiry_days   INT NOT NULL,
    created_at            TIMESTAMPTZ NOT NULL DEFAULT now(),
    updated_at            TIMESTAMPTZ NOT NULL DEFAULT now(),
    deleted_at            TIMESTAMPTZ
);
CREATE INDEX idx_product_types_category_id ON product_types(category_id) WHERE deleted_at IS NULL;
