-- V14__product_type_storage_expiry.sql
-- Per-storage shelf life for product_types, filled from the USDA FoodKeeper
-- data by DbInitializer (DataAccess/Seed/foodkeeper.json). Null where
-- FoodKeeper has no recommendation for that storage (e.g. don't freeze);
-- default_expiry_days stays the fallback when a column is null.

ALTER TABLE product_types
    ADD COLUMN pantry_expiry_days   INT,
    ADD COLUMN fridge_expiry_days   INT,
    ADD COLUMN freezer_expiry_days  INT;

-- DbInitializer matches seeded types by category and name.
CREATE UNIQUE INDEX uq_product_types_category_name
    ON product_types(category_id, name)
    WHERE deleted_at IS NULL;
