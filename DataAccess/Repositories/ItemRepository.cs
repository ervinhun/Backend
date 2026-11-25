using DataAccess.Entities;
using DataAccess.Interfaces;
using DataAccess.Models.Item;
using Microsoft.EntityFrameworkCore;

namespace DataAccess.Repositories;

public class ItemRepository(AppDbContext db) : IItemRepository
{
    public async Task AddAsync(Item item, CancellationToken ct = default)
    {
        db.Items.Add(item);
        await db.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<Item>> GetForFridgeAsync(
        Guid fridgeId,
        ItemStatus status,
        Guid? containerId = null,
        CancellationToken ct = default
    ) =>
        await InFridge(fridgeId)
            .AsNoTracking()
            .Where(i => i.Status == status && (containerId == null || i.ContainerId == containerId))
            .OrderBy(i => i.ExpiryDate)
            .ThenBy(i => i.CreatedAt)
            .ThenBy(i => i.Id)
            .ToListAsync(ct);

    public Task<Item?> GetAsync(Guid fridgeId, Guid itemId, CancellationToken ct = default) =>
        InFridge(fridgeId).AsNoTracking().FirstOrDefaultAsync(i => i.Id == itemId, ct);

    public async Task<bool> UpdateAsync(
        Guid fridgeId,
        Guid itemId,
        ItemChanges changes,
        CancellationToken ct = default
    ) =>
        await InFridge(fridgeId)
            .Where(i => i.Id == itemId)
            .ExecuteUpdateAsync(
                s =>
                    s.SetProperty(i => i.ContainerId, changes.ContainerId)
                        .SetProperty(i => i.Name, changes.Name)
                        .SetProperty(i => i.CategoryId, changes.CategoryId)
                        .SetProperty(i => i.Amount, changes.Amount)
                        .SetProperty(i => i.Unit, changes.Unit)
                        .SetProperty(i => i.ImageUrl, changes.ImageUrl)
                        .SetProperty(i => i.ExpiryDate, changes.ExpiryDate)
                        .SetProperty(i => i.Comment, changes.Comment)
                        .SetProperty(i => i.ChangedAt, _ => DateTimeOffset.UtcNow),
                ct
            ) == 1;

    public async Task<SyncItemResult> SyncAsync(
        Guid fridgeId,
        Guid userId,
        SyncedItem synced,
        CancellationToken ct = default
    )
    {
        await using var transaction = await db.Database.BeginTransactionAsync(ct);

        // Locks the fridge row until the commit, so two phones can't both add the item or the container it goes in.
        // Raw SQL, so the deleted_at check is spelled out to keep deleted fridges from being locked.
        var fridge = await db
            .Fridges.FromSql(
                $"SELECT * FROM fridges WHERE id = {fridgeId} AND deleted_at IS NULL FOR UPDATE"
            )
            .AsNoTracking()
            .SingleOrDefaultAsync(ct);
        if (fridge is null)
            return new SyncItemResult(SyncItemStatus.FridgeNotFound);

        var containers = await db
            .Containers.Where(c => c.FridgeId == fridgeId)
            .OrderBy(c => c.CreatedAt)
            .ThenBy(c => c.Id)
            .ToListAsync(ct);
        var item = await db
            .Items.IgnoreQueryFilters()
            .FirstOrDefaultAsync(i => i.Id == synced.Id, ct);
        var current = containers.Find(c => c.Id == item?.ContainerId);
        if (item is not null)
        {
            if (current is null || item.DeletedAt is not null || item.Status != ItemStatus.Active)
                return new SyncItemResult(SyncItemStatus.Gone);
            if (item.ChangedAt > synced.ChangedAt)
                return new SyncItemResult(SyncItemStatus.Saved, item);
        }

        var container =
            current?.ContainerType == synced.Storage
                ? current
                : containers.Find(c => c.ContainerType == synced.Storage);
        if (container is null)
        {
            if (containers.Count >= fridge.MaxContainers)
                return new SyncItemResult(
                    SyncItemStatus.ContainersFull,
                    MaxContainers: fridge.MaxContainers
                );
            container = new Container
            {
                FridgeId = fridgeId,
                Name = synced.Storage.ToString(),
                ContainerType = synced.Storage,
            };
            db.Containers.Add(container);
            await db.SaveChangesAsync(ct);
        }

        if (item is null)
        {
            item = new Item
            {
                Id = synced.Id,
                AddedByUserId = userId,
                AddedAt = DateOnly.FromDateTime(synced.ChangedAt.UtcDateTime),
            };
            db.Items.Add(item);
        }
        item.ContainerId = container.Id;
        item.Name = synced.Name;
        if (synced.ProductTypeId is not null)
        {
            item.ProductTypeId = synced.ProductTypeId;
            item.CategoryId = synced.CategoryId;
        }
        item.Amount = synced.Amount;
        // The app's amount says what it counts, so a unit from elsewhere would be shown twice.
        item.Unit = null;
        item.ExpiryDate = synced.ExpiryDate;
        item.BestBefore = synced.BestBefore;
        item.OpenedOn = synced.OpenedOn;
        item.ChangedAt = synced.ChangedAt;

        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        return new SyncItemResult(SyncItemStatus.Saved, item);
    }

    public async Task<bool> SetStatusAsync(
        Guid fridgeId,
        Guid itemId,
        ItemStatus status,
        CancellationToken ct = default
    ) =>
        await InFridge(fridgeId)
            .Where(i => i.Id == itemId)
            .ExecuteUpdateAsync(s => s.SetProperty(i => i.Status, status), ct) == 1;

    public async Task<bool> DeleteAsync(
        Guid fridgeId,
        Guid itemId,
        DateTimeOffset now,
        CancellationToken ct = default
    ) =>
        await InFridge(fridgeId)
            .Where(i => i.Id == itemId)
            .ExecuteUpdateAsync(s => s.SetProperty(i => i.DeletedAt, now), ct) == 1;

    // An item belongs to a fridge through its container, whose query filter hides deleted containers.
    private IQueryable<Item> InFridge(Guid fridgeId) =>
        db.Items.Where(i =>
            db.Containers.Any(c => c.Id == i.ContainerId && c.FridgeId == fridgeId)
        );
}
