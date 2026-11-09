namespace DataAccess.Entities;

/// <summary>Maps the <c>product_catalog</c> table (db/migrations/V5__product_catalog.sql): a product known by its barcode.</summary>
public class CatalogProduct
{
    /// <summary>The <see cref="Source"/> of products added by users rather than looked up elsewhere.</summary>
    public const string ManualSource = "manual";

    public Guid Id { get; set; }

    /// <summary>Unique across all products, including deleted ones.</summary>
    public string Barcode { get; set; } = string.Empty;

    public Guid? ProductTypeId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
    public string Source { get; set; } = ManualSource;
    public DateTimeOffset CachedAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    /// <summary>Set when the product is deleted.</summary>
    public DateTimeOffset? DeletedAt { get; set; }
}
