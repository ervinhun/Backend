namespace DataAccess.Models.Item;

/// <summary>What an update replaces on an item; everything else about it stays.</summary>
public record ItemChanges(
    Guid ContainerId,
    string Name,
    Guid? CategoryId,
    string? Amount,
    string? Unit,
    string? ImageUrl,
    DateOnly ExpiryDate,
    string? Comment
);
