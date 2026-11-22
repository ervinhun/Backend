namespace DataAccess.Entities;

/// <summary>Maps the <c>fridge_notification_settings</c> table (db/migrations/V8__notifications.sql): when and to whom a fridge's expiry notifications go.</summary>
public class FridgeNotificationSettings
{
    public Guid Id { get; set; }
    public Guid FridgeId { get; set; }

    /// <summary>Days before an item's expiry date that its warning goes out.</summary>
    public int WarningDaysBefore { get; set; } = 7;

    /// <summary>Days before an item's expiry date that its danger notification goes out.</summary>
    public int DangerDaysBefore { get; set; } = 2;

    /// <summary>The time of day, in the device's own time zone, that the app shows the reminders.</summary>
    public TimeOnly NotificationTime { get; set; } = new(16, 0);

    public RecipientsScope RecipientsScope { get; set; } = RecipientsScope.AllMembers;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    /// <summary>Set when the fridge is deleted.</summary>
    public DateTimeOffset? DeletedAt { get; set; }
}
