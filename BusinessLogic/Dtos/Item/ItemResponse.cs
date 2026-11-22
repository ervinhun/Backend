namespace BusinessLogic.Dtos.Item;

public class ItemResponse
{
    public Guid Id { get; set; }
    public Guid ContainerId { get; set; }
    public Guid? ProductCatalogId { get; set; }
    public string Name { get; set; } = string.Empty;
    public Guid? CategoryId { get; set; }
    public string? Amount { get; set; }
    public string? Unit { get; set; }
    public string? ImageUrl { get; set; }
    public Guid AddedByUserId { get; set; }
    public DateOnly AddedAt { get; set; }
    public DateOnly ExpiryDate { get; set; }
    public string? Comment { get; set; }

    /// <summary>active, consumed, expired or removed.</summary>
    public string Status { get; set; } = string.Empty;

    public DateTimeOffset CreatedAt { get; set; }
}
