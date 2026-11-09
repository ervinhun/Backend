using Microsoft.EntityFrameworkCore.Infrastructure;

namespace DataAccess.Entities;

/// <summary>Maps the <c>product_categories</c> table (db/migrations/V4__product_types.sql): a flat lookup list.</summary>
public class ProductCategory
{
    private readonly ILazyLoader? _lazyLoader;
    private ICollection<ProductType>? _productTypes;

    public ProductCategory() { }

    // EF passes the loader in when it creates the entity, which is what makes ProductTypes lazy.
    private ProductCategory(ILazyLoader lazyLoader) => _lazyLoader = lazyLoader;

    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    /// <summary>Set when the category is deleted.</summary>
    public DateTimeOffset? DeletedAt { get; set; }

    /// <summary>
    /// Loaded from the database the first time it is read, on a category that EF tracks (so not after AsNoTracking);
    /// empty on one that was created by hand. Deleted types are left out.
    /// </summary>
    public ICollection<ProductType> ProductTypes
    {
        get => _lazyLoader.Load(this, ref _productTypes) ?? [];
        set => _productTypes = value;
    }
}
