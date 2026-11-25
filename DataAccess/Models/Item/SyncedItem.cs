using DataAccess.Entities;

namespace DataAccess.Models.Item;

/// <summary>An item as it is on a phone. A null <see cref="ProductTypeId"/> leaves the type and category as they were.</summary>
public record SyncedItem(
    Guid Id,
    ContainerType Storage,
    string Name,
    Guid? ProductTypeId,
    Guid? CategoryId,
    string? Amount,
    DateOnly ExpiryDate,
    DateOnly? BestBefore,
    DateOnly? OpenedOn,
    DateTimeOffset ChangedAt
);
