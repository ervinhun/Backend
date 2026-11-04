namespace DataAccess.Entities;

/// <summary>Maps the <c>product_types</c> table (db/migrations/V4__product_types.sql): a kind of product with a default shelf life.</summary>
public class ProductType
{
    public Guid Id { get; set; }
    public Guid? CategoryId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int DefaultExpiryDays { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    /// <summary>Set when the product type is deleted.</summary>
    public DateTimeOffset? DeletedAt { get; set; }
}
