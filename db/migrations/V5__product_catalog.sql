-- V5__product_catalog.sql
-- Product data & expiry defaults (DATABASE_PLAN.md §3, part 2).
-- `product_catalog` is the Open Food Facts cache ("cache, don't mirror"
-- — filled in lazily as users scan, never bulk-imported). `expiry_overrides`
-- is the owner-configurable layer on top of `product_types.default_expiry_days`,
-- implementing the most-specific-wins fallback chain (product+container
-- match -> product-only match -> container default -> global fallback).

CREATE TABLE product_catalog (
    id                 UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    barcode            TEXT NOT NULL UNIQUE,
    product_type_id    UUID REFERENCES product_types(id),
    name               TEXT NOT NULL,
    image_url          TEXT,
    source             TEXT NOT NULL DEFAULT 'open_food_facts',
    raw_lookup_data    JSONB,
    cached_at          TIMESTAMPTZ NOT NULL DEFAULT now(),
    created_at         TIMESTAMPTZ NOT NULL DEFAULT now(),
    updated_at         TIMESTAMPTZ NOT NULL DEFAULT now(),
    deleted_at         TIMESTAMPTZ
);

CREATE TABLE expiry_overrides (
    id                  UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    fridge_id           UUID NOT NULL REFERENCES fridges(id),
    container_type      TEXT CHECK (container_type IN ('fridge', 'freezer', 'pantry')),
    product_type_id     UUID REFERENCES product_types(id),
    expiry_days         INT NOT NULL,
    created_by_user_id  UUID NOT NULL REFERENCES users(id),
    created_at          TIMESTAMPTZ NOT NULL DEFAULT now(),
    updated_at          TIMESTAMPTZ NOT NULL DEFAULT now(),
    deleted_at          TIMESTAMPTZ
);
-- COALESCE is needed because Postgres treats NULL <> NULL in a plain
-- unique constraint (two rows with the same fridge_id and both
-- container_type IS NULL would otherwise NOT collide) — this collapses
-- nulls into a real sentinel so "one override per specificity tier" is
-- actually enforced.
CREATE UNIQUE INDEX uq_expiry_overrides_scope
    ON expiry_overrides(fridge_id, COALESCE(container_type, ''), COALESCE(product_type_id, '00000000-0000-0000-0000-000000000000'))
    WHERE deleted_at IS NULL;
