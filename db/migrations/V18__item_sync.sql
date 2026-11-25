-- V18__item_sync.sql
-- What the app needs to keep its offline copy of an item in step with this one.
-- expiry_date stays the day it must be eaten by, so reminders need no change;
-- best_before is the printed date of an item whose expiry_date moved forward
-- when it was opened. changed_at is when someone made the change, which for an
-- offline change is earlier than when it got here (updated_at): the later
-- changed_at wins.

ALTER TABLE items
    ADD COLUMN product_type_id  UUID REFERENCES product_types(id),
    ADD COLUMN best_before      DATE,
    ADD COLUMN opened_on        DATE,
    ADD COLUMN changed_at       TIMESTAMPTZ NOT NULL DEFAULT now();

UPDATE items SET changed_at = updated_at;
