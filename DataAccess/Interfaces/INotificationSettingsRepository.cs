using DataAccess.Entities;
using DataAccess.Models.Notification;

namespace DataAccess.Interfaces;

/// <summary>A fridge's notification settings and custom recipients (fridge_notification_settings, fridge_notification_recipients). Deleted rows are never returned.</summary>
public interface INotificationSettingsRepository
{
    /// <summary>Null if the fridge has never saved any settings.</summary>
    Task<NotificationSettingsWithRecipients?> GetAsync(
        Guid fridgeId,
        CancellationToken ct = default
    );

    /// <summary>
    /// Creates or replaces the fridge's settings and its custom recipients together; recipients not listed
    /// are soft-deleted at <paramref name="now"/>.
    /// </summary>
    Task SaveAsync(
        Guid fridgeId,
        int warningDaysBefore,
        int dangerDaysBefore,
        RecipientsScope scope,
        IReadOnlyCollection<Guid> recipientUserIds,
        DateTimeOffset now,
        CancellationToken ct = default
    );
}
