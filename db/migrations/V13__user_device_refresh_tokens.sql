-- V13__user_device_refresh_tokens.sql
-- Each sign-in now creates a user_devices row that holds that device's
-- refresh token (POST /api/auth/refresh), and the row's id is the access
-- token's `sid` claim. Logging out soft-deletes the row, which also ends
-- its access tokens: every authenticated request checks that its `sid`
-- row is still there. A device can sign in before it has a push token,
-- so push_token/platform become optional.
--
-- Only a SHA-256 hash of the refresh token is stored. Each refresh
-- replaces it, so a refresh token works once.

ALTER TABLE user_devices
    ALTER COLUMN push_token DROP NOT NULL,
    ALTER COLUMN platform DROP NOT NULL,
    ADD COLUMN refresh_token_hash        TEXT,
    ADD COLUMN refresh_token_expires_at  TIMESTAMPTZ;

CREATE UNIQUE INDEX idx_user_devices_refresh_token_hash
    ON user_devices(refresh_token_hash) WHERE refresh_token_hash IS NOT NULL;
