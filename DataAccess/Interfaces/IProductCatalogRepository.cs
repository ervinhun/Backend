using DataAccess.Entities;

namespace DataAccess.Interfaces;

/// <summary>The product catalog and its lookup lists (product_catalog, product_types, product_categories). Deleted rows are never returned.</summary>
public interface IProductCatalogRepository
{
    /// <summary>Ordered by name.</summary>
    Task<IReadOnlyList<ProductCategory>> GetCategoriesAsync(CancellationToken ct = default);

    /// <summary>Ordered by name; only the types of that category when <paramref name="categoryId"/> is given.</summary>
    Task<IReadOnlyList<ProductType>> GetTypesAsync(
        Guid? categoryId = null,
        CancellationToken ct = default
    );

    Task<bool> ProductTypeExistsAsync(Guid productTypeId, CancellationToken ct = default);

    /// <summary>Null if no product has this barcode.</summary>
    Task<CatalogProduct?> GetByBarcodeAsync(string barcode, CancellationToken ct = default);

    /// <summary>
    /// Products whose name contains <paramref name="search"/> (any case), of the given product type,
    /// ordered by name. Both filters are optional. At most <paramref name="limit"/> products.
    /// </summary>
    Task<IReadOnlyList<CatalogProduct>> SearchAsync(
        string? search,
        Guid? productTypeId,
        int limit,
        CancellationToken ct = default
    );

    /// <summary>
    /// Saves the product, filling in the generated id. A deleted product with the same barcode is restored with
    /// the new data instead, keeping its id. Throws <see cref="Exceptions.DuplicateKeyException"/> if the barcode is taken.
    /// </summary>
    Task AddAsync(CatalogProduct product, CancellationToken ct = default);
}
