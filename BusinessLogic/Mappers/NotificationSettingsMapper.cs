using BusinessLogic.Dtos.Notification;
using DataAccess.Entities;
using DataAccess.Models.Notification;

namespace BusinessLogic.Mappers;

public static class NotificationSettingsMapper
{
    public static NotificationSettingsResponse ToResponse(
        this NotificationSettingsWithRecipients stored
    ) =>
        new()
        {
            FridgeId = stored.Settings.FridgeId,
            WarningDaysBefore = stored.Settings.WarningDaysBefore,
            DangerDaysBefore = stored.Settings.DangerDaysBefore,
            RecipientsScope = stored.Settings.RecipientsScope.ToWireName(),
            RecipientUserIds = stored.RecipientUserIds,
        };

    public static string ToWireName(this RecipientsScope scope) =>
        scope switch
        {
            RecipientsScope.AllMembers => "all_members",
            RecipientsScope.AdderAndOwners => "adder_and_owners",
            _ => "custom",
        };

    /// <summary>The request has been validated, so the scope is one of the three names.</summary>
    public static RecipientsScope ToScope(this SaveNotificationSettingsRequest request) =>
        request.RecipientsScope switch
        {
            "all_members" => RecipientsScope.AllMembers,
            "adder_and_owners" => RecipientsScope.AdderAndOwners,
            _ => RecipientsScope.Custom,
        };
}
