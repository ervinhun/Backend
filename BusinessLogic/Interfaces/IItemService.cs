using BusinessLogic.Dtos.Item;

namespace BusinessLogic.Interfaces;

public interface IItemService
{
    Task<ItemResponse> CreateAsync(
        Guid fridgeId,
        Guid userId,
        CreateItemRequest request,
        CancellationToken ct = default
    );

    /// <param name="status">active, consumed, expired or removed; active when null.</param>
    Task<IReadOnlyList<ItemResponse>> GetForFridgeAsync(
        Guid fridgeId,
        Guid userId,
        Guid? containerId,
        string? status,
        CancellationToken ct = default
    );
    Task<ItemResponse> GetAsync(
        Guid fridgeId,
        Guid itemId,
        Guid userId,
        CancellationToken ct = default
    );
    Task<ItemResponse> UpdateAsync(
        Guid fridgeId,
        Guid itemId,
        Guid userId,
        UpdateItemRequest request,
        CancellationToken ct = default
    );

    /// <summary>
    /// Adds the item with this id, or changes it unless it was changed later than the request says: then it returns
    /// that later version. Throws GoneException if the item was removed, as removal wins over a change.
    /// </summary>
    Task<ItemResponse> SyncAsync(
        Guid fridgeId,
        Guid itemId,
        Guid userId,
        SyncItemRequest request,
        CancellationToken ct = default
    );
    Task<ItemResponse> SetStatusAsync(
        Guid fridgeId,
        Guid itemId,
        Guid userId,
        SetItemStatusRequest request,
        CancellationToken ct = default
    );
    Task DeleteAsync(Guid fridgeId, Guid itemId, Guid userId, CancellationToken ct = default);
}
