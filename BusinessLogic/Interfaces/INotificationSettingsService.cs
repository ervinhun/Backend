using BusinessLogic.Dtos.Notification;

namespace BusinessLogic.Interfaces;

public interface INotificationSettingsService
{
    Task<NotificationSettingsResponse> GetAsync(
        Guid fridgeId,
        Guid userId,
        CancellationToken ct = default
    );
    Task<NotificationSettingsResponse> UpdateAsync(
        Guid fridgeId,
        Guid userId,
        SaveNotificationSettingsRequest request,
        CancellationToken ct = default
    );
}
