-- V17__notification_time.sql
-- The time of day the app shows a fridge's expiry reminders. Per fridge, set by the owner
-- with the other notification settings, so every member hears at the same time.

ALTER TABLE fridge_notification_settings
    ADD COLUMN notification_time TIME NOT NULL DEFAULT '16:00';
