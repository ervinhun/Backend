using System.ComponentModel.DataAnnotations;

namespace BusinessLogic.Dtos.Notification;

public class SaveNotificationSettingsRequest : IValidatableObject
{
    /// <summary>Days before an item's expiry date that its warning goes out.</summary>
    [Range(1, 365)]
    public int WarningDaysBefore { get; set; }

    /// <summary>Days before an item's expiry date that its danger notification goes out.</summary>
    [Range(0, 365)]
    public int DangerDaysBefore { get; set; }

    /// <summary>all_members, adder_and_owners or custom.</summary>
    [Required, AllowedValues("all_members", "adder_and_owners", "custom")]
    public string RecipientsScope { get; set; } = string.Empty;

    /// <summary>The members to notify when the scope is custom; ignored for the other scopes.</summary>
    public List<Guid> RecipientUserIds { get; set; } = [];

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (DangerDaysBefore >= WarningDaysBefore)
            yield return new ValidationResult(
                "The danger notification must come after the warning, so DangerDaysBefore must be less than WarningDaysBefore.",
                [nameof(DangerDaysBefore)]
            );
        if (RecipientsScope == "custom" && RecipientUserIds.Count == 0)
            yield return new ValidationResult(
                "Pick at least one recipient for a custom scope.",
                [nameof(RecipientUserIds)]
            );
    }
}
