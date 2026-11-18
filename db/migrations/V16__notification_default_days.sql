-- V16__notification_default_days.sql
-- Default to a 7-day warning and a 2-day danger notification. Only new rows are
-- affected; fridges that already saved settings keep theirs.

ALTER TABLE fridge_notification_settings
    ALTER COLUMN warning_days_before SET DEFAULT 7,
    ALTER COLUMN danger_days_before SET DEFAULT 2;
