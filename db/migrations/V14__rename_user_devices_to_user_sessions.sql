-- V14__rename_user_devices_to_user_sessions.sql
-- A row is one sign-in (it holds that sign-in's refresh token and its id
-- is the access token's `sid`), so the table is now called user_sessions.
-- Earlier migrations keep the old name: Flyway checksums them.

ALTER TABLE user_devices RENAME TO user_sessions;

ALTER INDEX idx_user_devices_user_id RENAME TO idx_user_sessions_user_id;
ALTER INDEX idx_user_devices_refresh_token_hash RENAME TO idx_user_sessions_refresh_token_hash;

ALTER TRIGGER trg_user_devices_updated_at ON user_sessions RENAME TO trg_user_sessions_updated_at;

-- Postgres named these after the table when V2 created it.
ALTER TABLE user_sessions RENAME CONSTRAINT user_devices_pkey TO user_sessions_pkey;
ALTER TABLE user_sessions RENAME CONSTRAINT user_devices_user_id_fkey TO user_sessions_user_id_fkey;
