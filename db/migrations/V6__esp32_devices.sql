-- V6__esp32_devices.sql
-- Part of Items & the pending-scan flow (DATABASE_PLAN.md §4). Split
-- into its own file, ahead of V7, because `pending_scans` (V7) has a
-- foreign key to this table.

CREATE TABLE esp32_devices (
    id            UUID PRIMARY KEY,  -- the device's own factory-programmed GUID, not server-generated
    fridge_id     UUID REFERENCES fridges(id),
    name          TEXT,
    paired_at     TIMESTAMPTZ,
    last_seen_at  TIMESTAMPTZ,
    created_at    TIMESTAMPTZ NOT NULL DEFAULT now(),
    updated_at    TIMESTAMPTZ NOT NULL DEFAULT now(),
    deleted_at    TIMESTAMPTZ
);
CREATE INDEX idx_esp32_devices_fridge_id ON esp32_devices(fridge_id) WHERE deleted_at IS NULL;
