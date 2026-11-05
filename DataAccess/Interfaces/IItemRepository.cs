using DataAccess.Entities;
using DataAccess.Models.Item;

namespace DataAccess.Interfaces;

/// <summary>
/// The items in the containers of a fridge (items). Deleted items, and the items of deleted containers
/// and fridges, are never returned.
/// </summary>
public interface IItemRepository
{
    /// <summary>Saves the item, filling in its generated id.</summary>
    Task AddAsync(Item item, CancellationToken ct = default);

    /// <summary>
    /// The fridge's items with this status, soonest to expire first; only those in
    /// <paramref name="containerId"/> when it is given.
    /// </summary>
    Task<IReadOnlyList<Item>> GetForFridgeAsync(
        Guid fridgeId,
        ItemStatus status,
        Guid? containerId = null,
        CancellationToken ct = default
    );

    /// <summary>Null if there is no such item in this fridge.</summary>
    Task<Item?> GetAsync(Guid fridgeId, Guid itemId, CancellationToken ct = default);

    /// <summary>Returns false if there is no such item in this fridge.</summary>
    Task<bool> UpdateAsync(
        Guid fridgeId,
        Guid itemId,
        ItemChanges changes,
        CancellationToken ct = default
    );

    /// <summary>Returns false if there is no such item in this fridge.</summary>
    Task<bool> SetStatusAsync(
        Guid fridgeId,
        Guid itemId,
        ItemStatus status,
        CancellationToken ct = default
    );

    /// <summary>Soft-deletes the item. Returns false if there is no such item in this fridge.</summary>
    Task<bool> DeleteAsync(
        Guid fridgeId,
        Guid itemId,
        DateTimeOffset now,
        CancellationToken ct = default
    );
}
