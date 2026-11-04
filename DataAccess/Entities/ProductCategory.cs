namespace DataAccess.Entities;

/// <summary>Maps the <c>product_categories</c> table (db/migrations/V4__product_types.sql): a flat lookup list.</summary>
public class ProductCategory
{
    public Guid Id { get; set; }

    /// <summary>Unique across all categories, including deleted ones.</summary>
    public string Name { get; set; } = string.Empty;

    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    /// <summary>Set when the category is deleted.</summary>
    public DateTimeOffset? DeletedAt { get; set; }
}
