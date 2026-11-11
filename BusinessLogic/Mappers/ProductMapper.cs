using BusinessLogic.Dtos.Product;
using DataAccess.Entities;

namespace BusinessLogic.Mappers;

public static class ProductMapper
{
    public static ProductResponse ToResponse(this CatalogProduct product) =>
        new()
        {
            Id = product.Id,
            Barcode = product.Barcode,
            Name = product.Name,
            ProductTypeId = product.ProductTypeId,
            ImageUrl = product.ImageUrl,
            Source = product.Source,
        };

    public static ProductTypeResponse ToResponse(this ProductType type) =>
        new()
        {
            Id = type.Id,
            CategoryId = type.CategoryId,
            Name = type.Name,
            DefaultExpiryDays = type.DefaultExpiryDays,
            PantryExpiryDays = type.PantryExpiryDays,
            FridgeExpiryDays = type.FridgeExpiryDays,
            FreezerExpiryDays = type.FreezerExpiryDays,
        };

    public static ProductCategoryResponse ToResponse(this ProductCategory category) =>
        new() { Id = category.Id, Name = category.Name };
}
