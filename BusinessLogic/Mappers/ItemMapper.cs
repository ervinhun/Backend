using BusinessLogic.Dtos.Item;
using DataAccess.Entities;

namespace BusinessLogic.Mappers;

public static class ItemMapper
{
    public static ItemResponse ToResponse(this Item item) =>
        new()
        {
            Id = item.Id,
            ContainerId = item.ContainerId,
            ProductCatalogId = item.ProductCatalogId,
            Name = item.Name,
            CategoryId = item.CategoryId,
            Amount = item.Amount,
            Unit = item.Unit,
            ImageUrl = item.ImageUrl,
            AddedByUserId = item.AddedByUserId,
            AddedAt = item.AddedAt,
            ExpiryDate = item.ExpiryDate,
            Comment = item.Comment,
            Status = item.Status.ToString().ToLowerInvariant(),
            CreatedAt = item.CreatedAt,
        };

    /// <summary>The request has been validated, so the status is one of its allowed names.</summary>
    public static ItemStatus ToItemStatus(this SetItemStatusRequest request) =>
        Enum.Parse<ItemStatus>(request.Status, ignoreCase: true);
}
