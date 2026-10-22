-- V1__extensions.sql
-- Foundational setup used by every table below: the UUID generator and
-- the one trigger function every table's `updated_at` column relies on.
-- See DATABASE_PLAN.md's "GUID generation" note if targeting an older
-- Postgres without pgcrypto (swap for `uuid-ossp` / `uuid_generate_v4()`
-- consistently across every V*__*.sql file, not just this one).

CREATE EXTENSION IF NOT EXISTS pgcrypto;

-- Bumps `updated_at` on every UPDATE, server-side, never client-trusted.
-- Attached to each table by V11__triggers.sql, once every table exists.
CREATE OR REPLACE FUNCTION set_updated_at()
RETURNS TRIGGER AS $$
BEGIN
    NEW.updated_at = now();
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;
