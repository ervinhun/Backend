namespace DataAccess.Entities;

/// <summary>Maps the <c>fridge_notification_recipients</c> table: a member picked to get notifications when the scope is custom.</summary>
public class FridgeNotificationRecipient
{
    public Guid Id { get; set; }
    public Guid FridgeId { get; set; }
    public Guid UserId { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    /// <summary>Set when the recipient is unpicked or the fridge is deleted.</summary>
    public DateTimeOffset? DeletedAt { get; set; }
}
