-- V15__user_sessions_phone_model.sql
-- The phone model each sign-in came from (e.g. "iPhone 15 Pro"), so a
-- user's sessions can be told apart. Optional, like platform.

ALTER TABLE user_sessions
    ADD COLUMN phone_model TEXT;
