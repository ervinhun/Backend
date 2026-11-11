namespace BusinessLogic.Dtos.Notification;

public class NotificationSettingsResponse
{
    public Guid FridgeId { get; set; }
    public int WarningDaysBefore { get; set; }
    public int DangerDaysBefore { get; set; }

    /// <summary>all_members, adder_and_owners or custom.</summary>
    public string RecipientsScope { get; set; } = string.Empty;

    /// <summary>The members picked to get notifications; empty unless the scope is custom.</summary>
    public IReadOnlyList<Guid> RecipientUserIds { get; set; } = [];
}
