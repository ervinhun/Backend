-- V8__notifications.sql
-- Notifications (DATABASE_PLAN.md §5). Settings (owner-configurable,
-- per fridge) kept separate from the dispatch log (per item, did it
-- actually go out and to how many devices).

CREATE TABLE fridge_notification_settings (
    id                    UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    fridge_id             UUID NOT NULL UNIQUE REFERENCES fridges(id),
    warning_days_before   INT NOT NULL DEFAULT 6,
    danger_days_before    INT NOT NULL DEFAULT 1,
    recipients_scope      TEXT NOT NULL DEFAULT 'all_members' CHECK (recipients_scope IN ('all_members', 'adder_and_owners', 'custom')),
    created_at            TIMESTAMPTZ NOT NULL DEFAULT now(),
    updated_at            TIMESTAMPTZ NOT NULL DEFAULT now(),
    deleted_at            TIMESTAMPTZ
);

CREATE TABLE fridge_notification_recipients (
    id          UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    fridge_id   UUID NOT NULL REFERENCES fridges(id),
    user_id     UUID NOT NULL REFERENCES users(id),
    created_at  TIMESTAMPTZ NOT NULL DEFAULT now(),
    updated_at  TIMESTAMPTZ NOT NULL DEFAULT now(),
    deleted_at  TIMESTAMPTZ
);
CREATE UNIQUE INDEX uq_fridge_notification_recipients ON fridge_notification_recipients(fridge_id, user_id) WHERE deleted_at IS NULL;

CREATE TABLE notification_dispatches (
    id                       UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    item_id                  UUID NOT NULL REFERENCES items(id),
    stage                    TEXT NOT NULL CHECK (stage IN ('warning', 'expiry')),
    sent                     BOOLEAN NOT NULL DEFAULT false,
    devices_notified_count   INT NOT NULL DEFAULT 0,
    sent_at                  TIMESTAMPTZ,
    created_at               TIMESTAMPTZ NOT NULL DEFAULT now(),
    updated_at               TIMESTAMPTZ NOT NULL DEFAULT now(),
    deleted_at               TIMESTAMPTZ
);
CREATE UNIQUE INDEX uq_notification_dispatches_item_stage ON notification_dispatches(item_id, stage) WHERE deleted_at IS NULL;
