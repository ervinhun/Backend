using BusinessLogic.Dtos.Product;
using BusinessLogic.Exceptions;
using BusinessLogic.Interfaces;
using BusinessLogic.Mappers;
using DataAccess.Entities;
using DataAccess.Exceptions;
using DataAccess.Interfaces;

namespace BusinessLogic.Services;

public class ProductCatalogService(IProductCatalogRepository catalog) : IProductCatalogService
{
    /// <summary>The most products one search returns.</summary>
    public const int MaxSearchResults = 50;

    public async Task<IReadOnlyList<ProductCategoryResponse>> GetCategoriesAsync(
        CancellationToken ct = default
    ) => (await catalog.GetCategoriesAsync(ct)).Select(c => c.ToResponse()).ToList();

    public async Task<IReadOnlyList<ProductTypeResponse>> GetTypesAsync(
        Guid? categoryId,
        CancellationToken ct = default
    ) => (await catalog.GetTypesAsync(categoryId, ct)).Select(t => t.ToResponse()).ToList();

    public async Task<ProductResponse> GetByBarcodeAsync(
        string barcode,
        CancellationToken ct = default
    ) =>
        (
            await catalog.GetByBarcodeAsync(barcode.Trim(), ct)
            ?? throw new NotFoundException("Product not found.")
        ).ToResponse();

    public async Task<IReadOnlyList<ProductResponse>> SearchAsync(
        string? search,
        Guid? productTypeId,
        CancellationToken ct = default
    ) =>
        (await catalog.SearchAsync(search, productTypeId, MaxSearchResults, ct))
            .Select(p => p.ToResponse())
            .ToList();

    public async Task<ProductResponse> CreateAsync(
        CreateProductRequest request,
        CancellationToken ct = default
    )
    {
        var barcode = request.Barcode.Trim();
        if (
            request.ProductTypeId is { } typeId
            && !await catalog.ProductTypeExistsAsync(typeId, ct)
        )
            throw new NotFoundException("Product type not found.");

        // Checked first so the usual duplicate doesn't cost a failed insert; the unique index still decides a race.
        if (await catalog.GetByBarcodeAsync(barcode, ct) is not null)
            throw BarcodeTaken();

        var product = new CatalogProduct
        {
            Barcode = barcode,
            Name = request.Name.Trim(),
            ProductTypeId = request.ProductTypeId,
            ImageUrl = string.IsNullOrWhiteSpace(request.ImageUrl) ? null : request.ImageUrl.Trim(),
            Source = CatalogProduct.ManualSource,
        };
        try
        {
            await catalog.AddAsync(product, ct);
        }
        catch (DuplicateKeyException)
        {
            throw BarcodeTaken();
        }

        return product.ToResponse();
    }

    private static ConflictException BarcodeTaken() =>
        new("A product with this barcode already exists.");
}
