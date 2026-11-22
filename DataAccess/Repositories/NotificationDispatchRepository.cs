using DataAccess.Entities;
using DataAccess.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace DataAccess.Repositories;

public class NotificationDispatchRepository(AppDbContext db) : INotificationDispatchRepository
{
    public async Task<bool> RecordAsync(
        Guid fridgeId,
        IReadOnlyCollection<(Guid ItemId, NotificationStage Stage)> dispatches,
        DateTimeOffset sentAt,
        CancellationToken ct = default
    )
    {
        var itemIds = dispatches.Select(d => d.ItemId).Distinct().ToList();
        // An item belongs to a fridge through its container, whose query filter hides deleted containers.
        var inFridge = await db
            .Items.Where(i =>
                itemIds.Contains(i.Id)
                && db.Containers.Any(c => c.Id == i.ContainerId && c.FridgeId == fridgeId)
            )
            .CountAsync(ct);
        if (inFridge != itemIds.Count)
            return false;

        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        // Every member's phone reports at the same reminder time, so two reports for one item and stage
        // can arrive together; the upsert lets both succeed instead of one hitting the unique index.
        // In a fixed order, so two phones reporting the same items can't wait on each other's row locks.
        foreach (var (itemId, stage) in dispatches.Distinct().OrderBy(d => d.ItemId).ThenBy(d => d.Stage))
            await db.Database.ExecuteSqlInterpolatedAsync(
                $"""
                INSERT INTO notification_dispatches (item_id, stage, sent, devices_notified_count, sent_at)
                VALUES ({itemId}, {stage.ToString().ToLowerInvariant()}, true, 1, {sentAt})
                ON CONFLICT (item_id, stage) WHERE deleted_at IS NULL
                DO UPDATE SET devices_notified_count = notification_dispatches.devices_notified_count + 1
                """,
                ct
            );
        await transaction.CommitAsync(ct);
        return true;
    }
}
