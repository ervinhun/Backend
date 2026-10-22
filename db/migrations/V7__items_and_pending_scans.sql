-- V7__items_and_pending_scans.sql
-- Items & the pending-scan flow (DATABASE_PLAN.md §4, continued).
-- `items` is the tracked inventory itself. `pending_scans` is ESP32
-- barcode scans awaiting user confirmation (capped at 10 active rows
-- per fridge at the application layer, not here) — it comes second
-- since it references `items.id` (confirmed_item_id) once a scan is
-- turned into a real item.

CREATE TABLE items (
    id                   UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    container_id         UUID NOT NULL REFERENCES containers(id),
    product_catalog_id   UUID REFERENCES product_catalog(id),
    name                 TEXT NOT NULL,
    category_id          UUID REFERENCES product_categories(id),
    amount               TEXT,
    unit                 TEXT,
    image_url            TEXT,
    added_by_user_id     UUID NOT NULL REFERENCES users(id),
    added_at             DATE NOT NULL DEFAULT current_date,
    expiry_date          DATE NOT NULL,
    comment              TEXT,
    status               TEXT NOT NULL DEFAULT 'active' CHECK (status IN ('active', 'consumed', 'expired', 'removed')),
    created_at           TIMESTAMPTZ NOT NULL DEFAULT now(),
    updated_at           TIMESTAMPTZ NOT NULL DEFAULT now(),
    deleted_at           TIMESTAMPTZ
);
CREATE INDEX idx_items_container_id ON items(container_id) WHERE deleted_at IS NULL;
CREATE INDEX idx_items_expiry_date ON items(expiry_date) WHERE deleted_at IS NULL AND status = 'active';

CREATE TABLE pending_scans (
    id                   UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    fridge_id            UUID NOT NULL REFERENCES fridges(id),
    esp32_device_id      UUID REFERENCES esp32_devices(id),
    barcode              TEXT NOT NULL,
    product_catalog_id   UUID REFERENCES product_catalog(id),
    status               TEXT NOT NULL DEFAULT 'pending' CHECK (status IN ('pending', 'confirmed', 'dismissed', 'expired_unconfirmed')),
    confirmed_item_id    UUID REFERENCES items(id),
    created_at           TIMESTAMPTZ NOT NULL DEFAULT now(),
    updated_at           TIMESTAMPTZ NOT NULL DEFAULT now(),
    deleted_at           TIMESTAMPTZ
);
CREATE INDEX idx_pending_scans_fridge_status ON pending_scans(fridge_id, status) WHERE deleted_at IS NULL;
