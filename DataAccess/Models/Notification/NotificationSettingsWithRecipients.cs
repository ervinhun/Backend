using DataAccess.Entities;

namespace DataAccess.Models.Notification;

/// <param name="RecipientUserIds">The custom recipients who are still members of the fridge.</param>
public record NotificationSettingsWithRecipients(
    FridgeNotificationSettings Settings,
    IReadOnlyList<Guid> RecipientUserIds
);
