using BusinessLogic.Dtos.Notification;
using BusinessLogic.Exceptions;
using BusinessLogic.Interfaces;
using DataAccess.Entities;
using DataAccess.Interfaces;

namespace BusinessLogic.Services;

/// <summary>Any member of the fridge can report a notification their device showed; non-members get 404.</summary>
public class NotificationDispatchService(
    INotificationDispatchRepository dispatches,
    IFridgeRepository fridges
) : INotificationDispatchService
{
    public async Task ReportAsync(
        Guid fridgeId,
        Guid userId,
        ReportDispatchesRequest request,
        CancellationToken ct = default
    )
    {
        _ = await fridges.GetMemberAsync(fridgeId, userId, ct)
            ?? throw new NotFoundException("Fridge not found.");

        var reported = request
            .Items.Select(i => (i.ItemId, Enum.Parse<NotificationStage>(i.Stage, ignoreCase: true)))
            .ToList();
        if (!await dispatches.RecordAsync(fridgeId, reported, request.SentAt!.Value, ct))
            throw new NotFoundException("Item not found.");
    }
}
