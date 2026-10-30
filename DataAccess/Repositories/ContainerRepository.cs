using DataAccess.Entities;
using DataAccess.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace DataAccess.Repositories;

public class ContainerRepository(AppDbContext db) : IContainerRepository
{
    public async Task<AddContainerResult> AddAsync(
        Container container,
        CancellationToken ct = default
    )
    {
        await using var transaction = await db.Database.BeginTransactionAsync(ct);

        // Locks the fridge row until the commit, so two adds can't both see a free slot.
        // Raw SQL, so the deleted_at check is spelled out to keep deleted fridges from being locked.
        var fridge = await db
            .Fridges.FromSql(
                $"SELECT * FROM fridges WHERE id = {container.FridgeId} AND deleted_at IS NULL FOR UPDATE"
            )
            .AsNoTracking()
            .SingleOrDefaultAsync(ct);
        if (fridge is null)
            return new AddContainerResult(AddContainerStatus.FridgeNotFound, 0);

        var count = await db.Containers.CountAsync(c => c.FridgeId == fridge.Id, ct);
        if (count >= fridge.MaxContainers)
            return new AddContainerResult(AddContainerStatus.Full, fridge.MaxContainers);

        db.Containers.Add(container);
        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        return new AddContainerResult(AddContainerStatus.Added, fridge.MaxContainers);
    }

    public async Task<IReadOnlyList<Container>> GetForFridgeAsync(
        Guid fridgeId,
        CancellationToken ct = default
    ) =>
        await db
            .Containers.AsNoTracking()
            .Where(c => c.FridgeId == fridgeId)
            .OrderBy(c => c.CreatedAt)
            .ThenBy(c => c.Id)
            .ToListAsync(ct);

    public Task<Container?> GetAsync(
        Guid fridgeId,
        Guid containerId,
        CancellationToken ct = default
    ) =>
        db
            .Containers.AsNoTracking()
            .FirstOrDefaultAsync(c => c.FridgeId == fridgeId && c.Id == containerId, ct);

    public async Task<bool> UpdateAsync(
        Guid fridgeId,
        Guid containerId,
        string name,
        ContainerType containerType,
        CancellationToken ct = default
    ) =>
        await db
            .Containers.Where(c => c.FridgeId == fridgeId && c.Id == containerId)
            .ExecuteUpdateAsync(
                s =>
                    s.SetProperty(c => c.Name, name)
                        .SetProperty(c => c.ContainerType, containerType),
                ct
            ) == 1;

    public async Task<bool> DeleteAsync(
        Guid fridgeId,
        Guid containerId,
        DateTimeOffset now,
        CancellationToken ct = default
    ) =>
        await db
            .Containers.Where(c => c.FridgeId == fridgeId && c.Id == containerId)
            .ExecuteUpdateAsync(s => s.SetProperty(c => c.DeletedAt, now), ct) == 1;
}
