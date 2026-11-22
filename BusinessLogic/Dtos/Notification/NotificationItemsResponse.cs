namespace BusinessLogic.Dtos.Notification;

/// <summary>What the app needs to schedule a fridge's reminders for one member.</summary>
public class NotificationItemsResponse
{
    public Guid FridgeId { get; set; }
    public int WarningDaysBefore { get; set; }
    public int DangerDaysBefore { get; set; }

    /// <summary>The time of day the app shows the reminders.</summary>
    public TimeOnly NotificationTime { get; set; }

    /// <summary>The active items this member is to be reminded about, after the fridge's recipient scope; empty if they are not a recipient.</summary>
    public IReadOnlyList<NotificationItemResponse> Items { get; set; } = [];
}

public class NotificationItemResponse
{
    public Guid Id { get; set; }
    public DateOnly ExpiryDate { get; set; }

    /// <summary>
    /// True if the item's whole shelf life, from the day it was added to its expiry date, is no longer than
    /// the warning period. It would be warned about the moment it is added, so only the danger notice applies.
    /// </summary>
    public bool SkipWarning { get; set; }
}
