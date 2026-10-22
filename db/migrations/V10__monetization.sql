-- V10__monetization.sql
-- Monetization (DATABASE_PLAN.md §7). Dormant field, feature-flagged:
-- implemented now, inactive until FeatureHub flips the flag. Deliberately
-- minimal — this must not shape the core schema beyond this one table.

CREATE TABLE user_subscriptions (
    id             UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id        UUID NOT NULL UNIQUE REFERENCES users(id),
    tier           TEXT NOT NULL DEFAULT 'free' CHECK (tier IN ('free', 'paid')),
    activated_at   TIMESTAMPTZ,
    created_at     TIMESTAMPTZ NOT NULL DEFAULT now(),
    updated_at     TIMESTAMPTZ NOT NULL DEFAULT now(),
    deleted_at     TIMESTAMPTZ
);
