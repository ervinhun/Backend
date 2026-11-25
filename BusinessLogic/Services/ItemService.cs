using BusinessLogic.Dtos.Item;
using BusinessLogic.Exceptions;
using BusinessLogic.Interfaces;
using BusinessLogic.Mappers;
using DataAccess.Entities;
using DataAccess.Interfaces;
using DataAccess.Models.Item;

namespace BusinessLogic.Services;

/// <summary>
/// Every member of the fridge can see its items; the owner and members who may put things in can add, change
/// and delete them, and the owner and members who may use things can mark them consumed or removed.
/// </summary>
public class ItemService(
    IItemRepository items,
    IContainerRepository containers,
    IFridgeRepository fridges,
    IProductCatalogRepository catalog,
    TimeProvider timeProvider
) : IItemService
{
    private const string FridgeNotFoundMessage = "Fridge not found.";
    private const string ItemNotFoundMessage = "Item not found.";
    private const string ContainerNotFoundMessage = "Container not found.";

    private enum Access
    {
        View,
        Put,
        Use,
    }

    public async Task<ItemResponse> CreateAsync(
        Guid fridgeId,
        Guid userId,
        CreateItemRequest request,
        CancellationToken ct = default
    )
    {
        await AuthorizeAsync(fridgeId, userId, Access.Put, ct);
        var containerId = request.ContainerId!.Value;
        await RequireContainerAsync(fridgeId, containerId, ct);
        await RequireCategoryAsync(request.CategoryId, ct);

        var product = request.ProductCatalogId is { } productId
            ? await catalog.GetProductAsync(productId, ct)
                ?? throw new NotFoundException("Product not found.")
            : null;
        var type = product?.ProductTypeId is { } typeId
            ? await catalog.GetTypeAsync(typeId, ct)
            : null;

        var name =
            Clean(request.Name)
            ?? product?.Name
            ?? throw new BadRequestException("A name is required when no product is given.");

        var today = Today();
        var expiryDate =
            request.ExpiryDate
            ?? (
                type is null
                    ? throw new BadRequestException(
                        "An expiry date is required unless the product's type has a default shelf life."
                    )
                    : today.AddDays(type.DefaultExpiryDays)
            );

        var item = new Item
        {
            ContainerId = containerId,
            ProductCatalogId = product?.Id,
            Name = name,
            CategoryId = request.CategoryId ?? type?.CategoryId,
            Amount = Clean(request.Amount),
            Unit = Clean(request.Unit),
            ImageUrl = Clean(request.ImageUrl) ?? product?.ImageUrl,
            AddedByUserId = userId,
            AddedAt = today,
            ExpiryDate = expiryDate,
            Comment = Clean(request.Comment),
        };

        await items.AddAsync(item, ct);
        return item.ToResponse();
    }

    public async Task<IReadOnlyList<ItemResponse>> GetForFridgeAsync(
        Guid fridgeId,
        Guid userId,
        Guid? containerId,
        string? status,
        CancellationToken ct = default
    )
    {
        await AuthorizeAsync(fridgeId, userId, Access.View, ct);
        var wanted =
            status is null ? ItemStatus.Active
            : Enum.TryParse<ItemStatus>(status, ignoreCase: true, out var parsed) ? parsed
            : throw new BadRequestException(
                "The status must be active, consumed, expired or removed."
            );

        return (await items.GetForFridgeAsync(fridgeId, wanted, containerId, ct))
            .Select(i => i.ToResponse())
            .ToList();
    }

    public async Task<ItemResponse> GetAsync(
        Guid fridgeId,
        Guid itemId,
        Guid userId,
        CancellationToken ct = default
    )
    {
        await AuthorizeAsync(fridgeId, userId, Access.View, ct);

        return await FindAsync(fridgeId, itemId, ct);
    }

    public async Task<ItemResponse> UpdateAsync(
        Guid fridgeId,
        Guid itemId,
        Guid userId,
        UpdateItemRequest request,
        CancellationToken ct = default
    )
    {
        await AuthorizeAsync(fridgeId, userId, Access.Put, ct);
        await RequireContainerAsync(fridgeId, request.ContainerId!.Value, ct);
        await RequireCategoryAsync(request.CategoryId, ct);

        var changes = new ItemChanges(
            request.ContainerId.Value,
            request.Name.Trim(),
            request.CategoryId,
            Clean(request.Amount),
            Clean(request.Unit),
            Clean(request.ImageUrl),
            request.ExpiryDate!.Value,
            Clean(request.Comment)
        );
        if (!await items.UpdateAsync(fridgeId, itemId, changes, ct))
            throw new NotFoundException(ItemNotFoundMessage);

        return await FindAsync(fridgeId, itemId, ct);
    }

    public async Task<ItemResponse> SyncAsync(
        Guid fridgeId,
        Guid itemId,
        Guid userId,
        SyncItemRequest request,
        CancellationToken ct = default
    )
    {
        await AuthorizeAsync(fridgeId, userId, Access.Put, ct);

        // The type may have been deleted while the phone was offline. The food is still worth keeping.
        var type = request.ProductTypeId is { } typeId ? await catalog.GetTypeAsync(typeId, ct) : null;
        var now = timeProvider.GetUtcNow();
        var changedAt = request.ChangedAt!.Value.ToUniversalTime();

        var result = await items.SyncAsync(
            fridgeId,
            userId,
            new SyncedItem(
                itemId,
                Enum.Parse<ContainerType>(request.Storage, ignoreCase: true),
                request.Name.Trim(),
                type?.Id,
                type?.CategoryId,
                Clean(request.Amount),
                request.ExpiryDate!.Value,
                request.BestBefore,
                request.OpenedOn,
                changedAt > now ? now : changedAt
            ),
            ct
        );

        return result.Status switch
        {
            SyncItemStatus.Saved => result.Item!.ToResponse(),
            SyncItemStatus.FridgeNotFound => throw new NotFoundException(FridgeNotFoundMessage),
            SyncItemStatus.Gone => throw new GoneException("This item has been removed."),
            SyncItemStatus.ContainersFull => throw new ConflictException(
                $"This fridge has reached its limit of {result.MaxContainers} containers."
            ),
            _ => throw new InvalidOperationException($"Unexpected sync result {result.Status}."),
        };
    }

    public async Task<ItemResponse> SetStatusAsync(
        Guid fridgeId,
        Guid itemId,
        Guid userId,
        SetItemStatusRequest request,
        CancellationToken ct = default
    )
    {
        await AuthorizeAsync(fridgeId, userId, Access.Use, ct);

        if (!await items.SetStatusAsync(fridgeId, itemId, request.ToItemStatus(), ct))
            throw new NotFoundException(ItemNotFoundMessage);

        return await FindAsync(fridgeId, itemId, ct);
    }

    public async Task DeleteAsync(
        Guid fridgeId,
        Guid itemId,
        Guid userId,
        CancellationToken ct = default
    )
    {
        await AuthorizeAsync(fridgeId, userId, Access.Put, ct);

        if (!await items.DeleteAsync(fridgeId, itemId, timeProvider.GetUtcNow(), ct))
            throw new NotFoundException(ItemNotFoundMessage);
    }

    private DateOnly Today() => DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime);

    private async Task<ItemResponse> FindAsync(Guid fridgeId, Guid itemId, CancellationToken ct) =>
        (
            await items.GetAsync(fridgeId, itemId, ct)
            ?? throw new NotFoundException(ItemNotFoundMessage)
        ).ToResponse();

    private async Task RequireContainerAsync(Guid fridgeId, Guid containerId, CancellationToken ct)
    {
        if (await containers.GetAsync(fridgeId, containerId, ct) is null)
            throw new NotFoundException(ContainerNotFoundMessage);
    }

    private async Task RequireCategoryAsync(Guid? categoryId, CancellationToken ct)
    {
        if (categoryId is { } id && !await catalog.CategoryExistsAsync(id, ct))
            throw new NotFoundException("Category not found.");
    }

    private static string? Clean(string? text) =>
        string.IsNullOrWhiteSpace(text) ? null : text.Trim();

    // Non-members get 404 so the existence of a fridge is not leaked.
    private async Task AuthorizeAsync(
        Guid fridgeId,
        Guid userId,
        Access access,
        CancellationToken ct
    )
    {
        var member =
            await fridges.GetMemberAsync(fridgeId, userId, ct)
            ?? throw new NotFoundException(FridgeNotFoundMessage);
        if (member.IsOwner)
            return;
        if (access == Access.Put && !member.CanPut)
            throw new ForbiddenException("You are not allowed to change this fridge's items.");
        if (access == Access.Use && !member.CanUse)
            throw new ForbiddenException("You are not allowed to use this fridge's items.");
    }
}
