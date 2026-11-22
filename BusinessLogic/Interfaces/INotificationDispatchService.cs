using BusinessLogic.Dtos.Notification;

namespace BusinessLogic.Interfaces;

public interface INotificationDispatchService
{
    Task ReportAsync(
        Guid fridgeId,
        Guid userId,
        ReportDispatchesRequest request,
        CancellationToken ct = default
    );
}
