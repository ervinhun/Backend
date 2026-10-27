-- V3__fridges_and_members.sql
-- Fridges & Membership (DATABASE_PLAN.md §2). `fridges` is the
-- sharing/ownership unit; `containers` is the physical fridge/freezer/
-- pantry inside it; `fridge_members` is the core sharing/permissions
-- table, one row per (user, fridge).

CREATE TABLE fridges (
    id                  UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    name                TEXT NOT NULL,
    invite_code         TEXT NOT NULL UNIQUE,
    max_members         INT NOT NULL DEFAULT 15,
    max_containers      INT NOT NULL DEFAULT 10,
    created_by_user_id  UUID NOT NULL REFERENCES users(id),
    created_at          TIMESTAMPTZ NOT NULL DEFAULT now(),
    updated_at          TIMESTAMPTZ NOT NULL DEFAULT now(),
    deleted_at          TIMESTAMPTZ
);

CREATE TABLE containers (
    id              UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    fridge_id       UUID NOT NULL REFERENCES fridges(id),
    name            TEXT NOT NULL,
    container_type  TEXT NOT NULL CHECK (container_type IN ('fridge', 'freezer', 'pantry')),
    created_at      TIMESTAMPTZ NOT NULL DEFAULT now(),
    updated_at      TIMESTAMPTZ NOT NULL DEFAULT now(),
    deleted_at      TIMESTAMPTZ
);
CREATE INDEX idx_containers_fridge_id ON containers(fridge_id) WHERE deleted_at IS NULL;

CREATE TABLE fridge_members (
    id                  UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    fridge_id           UUID NOT NULL REFERENCES fridges(id),
    user_id             UUID NOT NULL REFERENCES users(id),
    display_name        TEXT,
    is_owner            BOOLEAN NOT NULL DEFAULT false,
    can_put             BOOLEAN NOT NULL DEFAULT true,
    can_use             BOOLEAN NOT NULL DEFAULT true,
    access_expires_at   TIMESTAMPTZ,
    invited_by_user_id  UUID REFERENCES users(id),
    created_at          TIMESTAMPTZ NOT NULL DEFAULT now(),
    updated_at          TIMESTAMPTZ NOT NULL DEFAULT now(),
    deleted_at          TIMESTAMPTZ
);
CREATE UNIQUE INDEX uq_fridge_members_fridge_user ON fridge_members(fridge_id, user_id) WHERE deleted_at IS NULL;
CREATE INDEX idx_fridge_members_user_id ON fridge_members(user_id) WHERE deleted_at IS NULL;
