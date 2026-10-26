-- V14__refresh_tokens.sql
-- Long-lived refresh tokens (POST /api/auth/refresh), so the app can get a
-- new access token without the user signing in again. Only a SHA-256 hash
-- of each token is stored. Every refresh revokes the token it used and
-- issues a new one in the same family (one family = one sign-in on one
-- device); presenting an already-revoked token revokes the whole family.

CREATE TABLE refresh_tokens (
    id          UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id     UUID NOT NULL REFERENCES users(id),
    -- Shared by every token rotated from the same sign-in; also the access
    -- token's `sid` claim, so logout can revoke the family.
    family_id   UUID NOT NULL,
    token_hash  TEXT NOT NULL UNIQUE,
    expires_at  TIMESTAMPTZ NOT NULL,
    revoked_at  TIMESTAMPTZ,
    created_at  TIMESTAMPTZ NOT NULL DEFAULT now(),
    updated_at  TIMESTAMPTZ NOT NULL DEFAULT now()
);
CREATE INDEX idx_refresh_tokens_family_id ON refresh_tokens(family_id) WHERE revoked_at IS NULL;

-- V11 gathers the earlier tables' triggers but has already run, so this
-- table's lives here.
CREATE TRIGGER trg_refresh_tokens_updated_at BEFORE UPDATE ON refresh_tokens
    FOR EACH ROW EXECUTE FUNCTION set_updated_at();
