-- V11__triggers.sql
-- Every table's `updated_at` trigger, gathered in one place instead of
-- next to each CREATE TABLE — the easiest file to scan when checking
-- "does this table actually have the trigger" or auditing all of them
-- at once. Runs after every table in V2-V10 exists and after
-- set_updated_at() is defined in V1.

CREATE TRIGGER trg_users_updated_at BEFORE UPDATE ON users
    FOR EACH ROW EXECUTE FUNCTION set_updated_at();

CREATE TRIGGER trg_user_sessions_updated_at BEFORE UPDATE ON user_sessions
    FOR EACH ROW EXECUTE FUNCTION set_updated_at();

CREATE TRIGGER trg_fridges_updated_at BEFORE UPDATE ON fridges
    FOR EACH ROW EXECUTE FUNCTION set_updated_at();

CREATE TRIGGER trg_containers_updated_at BEFORE UPDATE ON containers
    FOR EACH ROW EXECUTE FUNCTION set_updated_at();

CREATE TRIGGER trg_fridge_members_updated_at BEFORE UPDATE ON fridge_members
    FOR EACH ROW EXECUTE FUNCTION set_updated_at();

CREATE TRIGGER trg_product_categories_updated_at BEFORE UPDATE ON product_categories
    FOR EACH ROW EXECUTE FUNCTION set_updated_at();

CREATE TRIGGER trg_product_types_updated_at BEFORE UPDATE ON product_types
    FOR EACH ROW EXECUTE FUNCTION set_updated_at();

CREATE TRIGGER trg_product_catalog_updated_at BEFORE UPDATE ON product_catalog
    FOR EACH ROW EXECUTE FUNCTION set_updated_at();

CREATE TRIGGER trg_expiry_overrides_updated_at BEFORE UPDATE ON expiry_overrides
    FOR EACH ROW EXECUTE FUNCTION set_updated_at();

CREATE TRIGGER trg_esp32_devices_updated_at BEFORE UPDATE ON esp32_devices
    FOR EACH ROW EXECUTE FUNCTION set_updated_at();

CREATE TRIGGER trg_items_updated_at BEFORE UPDATE ON items
    FOR EACH ROW EXECUTE FUNCTION set_updated_at();

CREATE TRIGGER trg_pending_scans_updated_at BEFORE UPDATE ON pending_scans
    FOR EACH ROW EXECUTE FUNCTION set_updated_at();

CREATE TRIGGER trg_fridge_notification_settings_updated_at BEFORE UPDATE ON fridge_notification_settings
    FOR EACH ROW EXECUTE FUNCTION set_updated_at();

CREATE TRIGGER trg_fridge_notification_recipients_updated_at BEFORE UPDATE ON fridge_notification_recipients
    FOR EACH ROW EXECUTE FUNCTION set_updated_at();

CREATE TRIGGER trg_notification_dispatches_updated_at BEFORE UPDATE ON notification_dispatches
    FOR EACH ROW EXECUTE FUNCTION set_updated_at();

CREATE TRIGGER trg_activity_log_entries_updated_at BEFORE UPDATE ON activity_log_entries
    FOR EACH ROW EXECUTE FUNCTION set_updated_at();

CREATE TRIGGER trg_user_subscriptions_updated_at BEFORE UPDATE ON user_subscriptions
    FOR EACH ROW EXECUTE FUNCTION set_updated_at();
