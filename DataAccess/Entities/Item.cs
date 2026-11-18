namespace DataAccess.Entities;

/// <summary>Maps the <c>items</c> table (db/migrations/V7__items_and_pending_scans.sql): something stored in a container of a fridge.</summary>
public class Item
{
    public Guid Id { get; set; }
    public Guid ContainerId { get; set; }
    public Guid? ProductCatalogId { get; set; }
    public string Name { get; set; } = string.Empty;
    public Guid? CategoryId { get; set; }

    /// <summary>Free text such as "500" or "2"; <see cref="Unit"/> says what it counts.</summary>
    public string? Amount { get; set; }

    public string? Unit { get; set; }
    public string? ImageUrl { get; set; }
    public Guid AddedByUserId { get; set; }
    public DateOnly AddedAt { get; set; }
    public DateOnly ExpiryDate { get; set; }
    public string? Comment { get; set; }
    public ItemStatus Status { get; set; } = ItemStatus.Active;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    /// <summary>Set when the item is deleted.</summary>
    public DateTimeOffset? DeletedAt { get; set; }
}
