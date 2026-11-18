using BusinessLogic.Dtos.Notification;

namespace BusinessLogic.Interfaces;

public interface INotificationItemsService
{
    Task<NotificationItemsResponse> GetAsync(
        Guid fridgeId,
        Guid userId,
        CancellationToken ct = default
    );
}
