using DataAccess.Entities;

namespace DataAccess.Interfaces;

/// <summary>Which item notifications went out, and to how many devices (notification_dispatches). One row per item and stage.</summary>
public interface INotificationDispatchRepository
{
    /// <summary>
    /// Records one device showing each notification. The first report of an item's stage creates its row
    /// at <paramref name="sentAt"/>; later ones add a device to it. Returns false, saving nothing, if any item
    /// is not in this fridge.
    /// </summary>
    Task<bool> RecordAsync(
        Guid fridgeId,
        IReadOnlyCollection<(Guid ItemId, NotificationStage Stage)> dispatches,
        DateTimeOffset sentAt,
        CancellationToken ct = default
    );
}
