namespace DataAccess.Entities;

/// <summary>
/// Maps the <c>product_types</c> table (db/migrations/V4__product_types.sql, V14__product_type_storage_expiry.sql):
/// a kind of food and how many days it keeps.
/// </summary>
public class ProductType
{
    public Guid Id { get; set; }
    public Guid? CategoryId { get; set; }
    public string Name { get; set; } = string.Empty;

    /// <summary>Used when the storage-specific value below is null.</summary>
    public int DefaultExpiryDays { get; set; }

    /// <summary>Null where there is no recommendation for that storage.</summary>
    public int? PantryExpiryDays { get; set; }

    public int? FridgeExpiryDays { get; set; }
    public int? FreezerExpiryDays { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    /// <summary>Set when the type is deleted.</summary>
    public DateTimeOffset? DeletedAt { get; set; }

    /// <summary>Set when seeding a type together with its new category; not loaded by queries.</summary>
    public ProductCategory? Category { get; set; }
}
