namespace DataAccess.Entities;

/// <summary>Maps the <c>fridge_members</c> table: one row per user in a fridge, with that user's rights.</summary>
public class FridgeMember
{
    public Guid Id { get; set; }
    public Guid FridgeId { get; set; }
    public Guid UserId { get; set; }

    /// <summary>Overrides the user's default display name inside this fridge. Null to use the default.</summary>
    public string? DisplayName { get; set; }

    public bool IsOwner { get; set; }

    // Default to true in C#; a HasDefaultValue(true) would make EF skip sending false.
    public bool CanPut { get; set; } = true;
    public bool CanUse { get; set; } = true;

    /// <summary>Null for access that never expires.</summary>
    public DateTimeOffset? AccessExpiresAt { get; set; }

    public Guid? InvitedByUserId { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    /// <summary>Set when the member leaves, is removed, or the fridge is deleted.</summary>
    public DateTimeOffset? DeletedAt { get; set; }

    /// <summary>Set when adding a new fridge together with its owner; not loaded by queries.</summary>
    public Fridge? Fridge { get; set; }

    /// <summary>Only loaded by the queries that need the member's name.</summary>
    public User? User { get; set; }
}
