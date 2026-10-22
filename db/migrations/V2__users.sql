-- V2__users.sql
-- Identity & Users (DATABASE_PLAN.md §1). The account itself, plus one
-- row per installed app instance so notifications can report how many
-- devices were actually reached.

CREATE TABLE users (
    id                    UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    email                 TEXT NOT NULL UNIQUE,
    password_hash         TEXT,
    auth_provider         TEXT NOT NULL DEFAULT 'password',
    default_display_name  TEXT NOT NULL,
    locale                TEXT NOT NULL DEFAULT 'en',
    created_at            TIMESTAMPTZ NOT NULL DEFAULT now(),
    updated_at            TIMESTAMPTZ NOT NULL DEFAULT now(),
    deleted_at            TIMESTAMPTZ
);

CREATE TABLE user_devices (
    id            UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id       UUID NOT NULL REFERENCES users(id),
    push_token    TEXT NOT NULL,
    platform      TEXT NOT NULL,
    is_active     BOOLEAN NOT NULL DEFAULT true,
    last_seen_at  TIMESTAMPTZ,
    created_at    TIMESTAMPTZ NOT NULL DEFAULT now(),
    updated_at    TIMESTAMPTZ NOT NULL DEFAULT now(),
    deleted_at    TIMESTAMPTZ
);
CREATE INDEX idx_user_devices_user_id ON user_devices(user_id) WHERE deleted_at IS NULL;
