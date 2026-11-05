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
                        .SetProperty(i => i.Comment, changes.Comment),
                ct
            ) == 1;

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
