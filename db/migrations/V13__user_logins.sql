-- V13__user_logins.sql
-- External sign-in identities (Google, Apple). One row per provider
-- account linked to a user, so one user can sign in with a password,
-- Google and Apple at once. Shaped like user_sessions: users.password_hash
-- still covers password login; users.auth_provider records how the
-- account was first created.

CREATE TABLE user_logins (
    id                UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id           UUID NOT NULL REFERENCES users(id),
    -- The provider's stable user id (the ID token's `sub`), not the email,
    -- which can change or be hidden behind Apple's private relay.
    provider_user_id  TEXT NOT NULL,
    provider          TEXT NOT NULL CHECK (provider IN ('google', 'apple')),
    -- false = unlinked: the provider can no longer be used to sign in.
    is_active         BOOLEAN NOT NULL DEFAULT true,
    created_at        TIMESTAMPTZ NOT NULL DEFAULT now(),
    updated_at        TIMESTAMPTZ NOT NULL DEFAULT now(),
    deleted_at        TIMESTAMPTZ
);
CREATE UNIQUE INDEX uq_user_logins_provider_user ON user_logins(provider, provider_user_id) WHERE deleted_at IS NULL;
CREATE INDEX idx_user_logins_user_id ON user_logins(user_id) WHERE deleted_at IS NULL;

-- V11 gathers the earlier tables' triggers but has already run, so this
-- table's lives here.
CREATE TRIGGER trg_user_logins_updated_at BEFORE UPDATE ON user_logins
    FOR EACH ROW EXECUTE FUNCTION set_updated_at();
