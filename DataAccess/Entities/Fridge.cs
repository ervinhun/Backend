namespace DataAccess.Entities;

/// <summary>Maps the <c>fridges</c> table (db/migrations/V3__fridges_and_members.sql): the sharing and ownership unit.</summary>
public class Fridge
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;

    /// <summary>Unique across all fridges, including deleted ones.</summary>
    public string InviteCode { get; set; } = string.Empty;

    /// <summary>
    /// The column defaults in V3__fridges_and_members.sql, which a test compares them with. They are always saved,
    /// so a fridge can be given any limit, 0 included. FridgeService gives the fridges it creates
    /// the limits of IFridgeLimitDefaults instead.
    /// </summary>
    public const int DefaultMaxMembers = 15;

    public const int DefaultMaxContainers = 10;

    public int MaxMembers { get; set; } = DefaultMaxMembers;
    public int MaxContainers { get; set; } = DefaultMaxContainers;

    public Guid CreatedByUserId { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    /// <summary>Set when the fridge is deleted.</summary>
    public DateTimeOffset? DeletedAt { get; set; }
}
