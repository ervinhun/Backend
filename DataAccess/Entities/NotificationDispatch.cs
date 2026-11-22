namespace DataAccess.Entities;

/// <summary>Maps the <c>notification_dispatches</c> table: that an item's warning or expiry notification went out.</summary>
public class NotificationDispatch
{
    public Guid Id { get; set; }
    public Guid ItemId { get; set; }
    public NotificationStage Stage { get; set; }
    public bool Sent { get; set; }

    /// <summary>How many devices reported showing it.</summary>
    public int DevicesNotifiedCount { get; set; }

    /// <summary>When the first device showed it.</summary>
    public DateTimeOffset? SentAt { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    /// <summary>Set when the dispatch is deleted.</summary>
    public DateTimeOffset? DeletedAt { get; set; }
}
