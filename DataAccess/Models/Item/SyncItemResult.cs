namespace DataAccess.Models.Item;

public enum SyncItemStatus
{
    /// <summary>The item is as the database now has it, which may be someone's later change.</summary>
    Saved,
    FridgeNotFound,

    /// <summary>The item was removed or used up, or is in another fridge.</summary>
    Gone,

    /// <summary>There is no container of that type, and no room for another.</summary>
    ContainersFull,
}

public record SyncItemResult(SyncItemStatus Status, Entities.Item? Item = null, int MaxContainers = 0);
