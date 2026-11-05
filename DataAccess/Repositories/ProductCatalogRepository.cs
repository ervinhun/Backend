using DataAccess.Entities;
using DataAccess.Exceptions;
using DataAccess.Interfaces;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace DataAccess.Repositories;

public class ProductCatalogRepository(AppDbContext db, TimeProvider timeProvider)
    : IProductCatalogRepository
{
    private const string BarcodeTakenMessage = "A product with this barcode already exists.";

    public async Task<IReadOnlyList<ProductCategory>> GetCategoriesAsync(
        CancellationToken ct = default
    ) => await db.ProductCategories.AsNoTracking().OrderBy(c => c.Name).ToListAsync(ct);

    public async Task<IReadOnlyList<ProductType>> GetTypesAsync(
        Guid? categoryId = null,
        CancellationToken ct = default
    ) =>
        await db
            .ProductTypes.AsNoTracking()
            .Where(t => categoryId == null || t.CategoryId == categoryId)
            .OrderBy(t => t.Name)
            .ToListAsync(ct);

    public Task<bool> ProductTypeExistsAsync(Guid productTypeId, CancellationToken ct = default) =>
        db.ProductTypes.AnyAsync(t => t.Id == productTypeId, ct);

    public Task<CatalogProduct?> GetByBarcodeAsync(string barcode, CancellationToken ct = default) =>
        db.CatalogProducts.AsNoTracking().FirstOrDefaultAsync(p => p.Barcode == barcode, ct);

    public async Task<IReadOnlyList<CatalogProduct>> SearchAsync(
        string? search,
        Guid? productTypeId,
        int limit,
        CancellationToken ct = default
    )
    {
        var products = db.CatalogProducts.AsNoTracking();
        if (productTypeId is not null)
            products = products.Where(p => p.ProductTypeId == productTypeId);
        if (!string.IsNullOrWhiteSpace(search))
            products = products.Where(p =>
                EF.Functions.ILike(p.Name, $"%{EscapeLike(search.Trim())}%", LikeEscape)
            );

        return await products.OrderBy(p => p.Name).ThenBy(p => p.Barcode).Take(limit).ToListAsync(ct);
    }

    public async Task AddAsync(CatalogProduct product, CancellationToken ct = default)
    {
        // A deleted product still holds its barcode in the unique index, so it is brought back with the new data.
        var deleted = await db
            .CatalogProducts.IgnoreQueryFilters()
            .FirstOrDefaultAsync(p => p.Barcode == product.Barcode && p.DeletedAt != null, ct);
        var saved = product;
        if (deleted is null)
            db.CatalogProducts.Add(product);
        else
        {
            deleted.Name = product.Name;
            deleted.ProductTypeId = product.ProductTypeId;
            deleted.ImageUrl = product.ImageUrl;
            deleted.Source = product.Source;
            deleted.CachedAt = timeProvider.GetUtcNow();
            deleted.DeletedAt = null;
            product.Id = deleted.Id;
            saved = deleted;
        }

        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex)
            when (ex.InnerException
                    is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation }
            )
        {
            // Stop the failed row from being saved again by the caller's next attempt.
            db.Entry(saved).State = EntityState.Detached;
            throw new DuplicateKeyException(BarcodeTakenMessage, ex);
        }
    }

    private const string LikeEscape = "\\";

    // So a search for "100%" or "a_b" matches those characters instead of acting as wildcards.
    private static string EscapeLike(string text) =>
        text.Replace(LikeEscape, LikeEscape + LikeEscape)
            .Replace("%", LikeEscape + "%")
            .Replace("_", LikeEscape + "_");
}
