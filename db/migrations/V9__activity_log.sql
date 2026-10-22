-- V9__activity_log.sql
-- Activity log (DATABASE_PLAN.md §6). Owner-only audit trail —
-- intentionally append-only from the app's perspective, no updated_at-
-- driven edits expected, but the standard columns are kept for schema
-- consistency and in case a correction/redaction is ever needed.

CREATE TABLE activity_log_entries (
    id          UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    fridge_id   UUID NOT NULL REFERENCES fridges(id),
    user_id     UUID NOT NULL REFERENCES users(id),
    item_id     UUID REFERENCES items(id),
    action      TEXT NOT NULL,
    details     JSONB,
    created_at  TIMESTAMPTZ NOT NULL DEFAULT now(),
    updated_at  TIMESTAMPTZ NOT NULL DEFAULT now(),
    deleted_at  TIMESTAMPTZ
);
CREATE INDEX idx_activity_log_fridge_id ON activity_log_entries(fridge_id) WHERE deleted_at IS NULL;
