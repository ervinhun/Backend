-- V13__revoked_tokens.sql
-- Access tokens signed out before they expire (POST /api/auth/logout).
-- JWTs are otherwise valid until `exp`, so every authenticated request
-- checks its token's `jti` against this table. Rows are only useful until
-- the token would have expired anyway; after `expires_at` they can be
-- deleted. Rows are never updated, so there is no updated_at/deleted_at.

CREATE TABLE revoked_tokens (
    jti         UUID PRIMARY KEY,
    user_id     UUID NOT NULL REFERENCES users(id),
    expires_at  TIMESTAMPTZ NOT NULL,
    created_at  TIMESTAMPTZ NOT NULL DEFAULT now()
);
CREATE INDEX idx_revoked_tokens_expires_at ON revoked_tokens(expires_at);
