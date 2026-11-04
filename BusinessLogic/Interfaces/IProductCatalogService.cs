using BusinessLogic.Dtos.Product;

namespace BusinessLogic.Interfaces;

public interface IProductCatalogService
{
    Task<IReadOnlyList<ProductCategoryResponse>> GetCategoriesAsync(CancellationToken ct = default);

    Task<IReadOnlyList<ProductTypeResponse>> GetTypesAsync(
        Guid? categoryId,
        CancellationToken ct = default
    );

    /// <summary>Throws if no product has this barcode.</summary>
    Task<ProductResponse> GetByBarcodeAsync(string barcode, CancellationToken ct = default);

    /// <summary>At most <see cref="Services.ProductCatalogService.MaxSearchResults"/> products.</summary>
    Task<IReadOnlyList<ProductResponse>> SearchAsync(
        string? search,
        Guid? productTypeId,
        CancellationToken ct = default
    );

    /// <summary>Throws if the barcode is already in the catalog or the product type is unknown.</summary>
    Task<ProductResponse> CreateAsync(CreateProductRequest request, CancellationToken ct = default);
}
