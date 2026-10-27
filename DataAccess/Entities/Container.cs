namespace DataAccess.Entities;

/// <summary>Maps the <c>containers</c> table (db/migrations/V3__fridges_and_members.sql): a fridge, freezer or pantry inside a fridge.</summary>
public class Container
{
    public Guid Id { get; set; }
    public Guid FridgeId { get; set; }
    public string Name { get; set; } = string.Empty;
    public ContainerType ContainerType { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    /// <summary>Set when the container is deleted.</summary>
    public DateTimeOffset? DeletedAt { get; set; }
}
