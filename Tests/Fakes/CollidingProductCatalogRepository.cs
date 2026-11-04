using DataAccess.Entities;
using DataAccess.Exceptions;
using DataAccess.Interfaces;

namespace Tests.Fakes;

/// <summary>
/// Wraps the real repository but hides every barcode from lookups and fails every add, like a
/// second request that saved the same barcode between the service's check and its insert.
/// </summary>
public class CollidingProductCatalogRepository(IProductCatalogRepository inner)
    : IProductCatalogRepository
{
    public Task<IReadOnlyList<ProductCategory>> GetCategoriesAsync(CancellationToken ct = default) =>
        inner.GetCategoriesAsync(ct);

    public Task<IReadOnlyList<ProductType>> GetTypesAsync(
        Guid? categoryId = null,
        CancellationToken ct = default
    ) => inner.GetTypesAsync(categoryId, ct);

    public Task<bool> ProductTypeExistsAsync(Guid productTypeId, CancellationToken ct = default) =>
        inner.ProductTypeExistsAsync(productTypeId, ct);

    public Task<CatalogProduct?> GetByBarcodeAsync(string barcode, CancellationToken ct = default) =>
        Task.FromResult<CatalogProduct?>(null);

    public Task<IReadOnlyList<CatalogProduct>> SearchAsync(
        string? search,
        Guid? productTypeId,
        int limit,
        CancellationToken ct = default
    ) => inner.SearchAsync(search, productTypeId, limit, ct);

    public Task AddAsync(CatalogProduct product, CancellationToken ct = default) =>
        throw new DuplicateKeyException("taken", new Exception());
}
