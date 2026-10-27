-- V2__users.sql
-- Identity & Users (DATABASE_PLAN.md §1). The account itself, plus one
-- row per signed-in session (its refresh token, and the push token
-- notifications use to report how many devices were actually reached).

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

-- One row per sign-in. It holds that session's refresh token (only a
-- SHA-256 hash; each refresh replaces it, so a refresh token works once),
-- and its id is the access token's `sid` claim. Logging out soft-deletes
-- the row, which also ends its access tokens. A session can start before
-- the app has a push token, so push_token/platform/phone_model are optional.
CREATE TABLE user_sessions (
    id                        UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id                   UUID NOT NULL REFERENCES users(id),
    push_token                TEXT,
    platform                  TEXT,
    phone_model               TEXT,
    is_active                 BOOLEAN NOT NULL DEFAULT true,
    last_seen_at              TIMESTAMPTZ,
    refresh_token_hash        TEXT,
    refresh_token_expires_at  TIMESTAMPTZ,
    created_at                TIMESTAMPTZ NOT NULL DEFAULT now(),
    updated_at                TIMESTAMPTZ NOT NULL DEFAULT now(),
    deleted_at                TIMESTAMPTZ
);
CREATE INDEX idx_user_sessions_user_id ON user_sessions(user_id) WHERE deleted_at IS NULL;
CREATE UNIQUE INDEX idx_user_sessions_refresh_token_hash
    ON user_sessions(refresh_token_hash) WHERE refresh_token_hash IS NOT NULL;
