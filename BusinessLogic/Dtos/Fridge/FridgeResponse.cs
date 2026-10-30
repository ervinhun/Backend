namespace BusinessLogic.Dtos.Fridge;

public class FridgeResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;

    /// <summary>Only filled in for owners.</summary>
    public string? InviteCode { get; set; }

    public int MaxMembers { get; set; }
    public int MaxContainers { get; set; }
    public int MemberCount { get; set; }

    // The caller's own rights in this fridge.
    public bool IsOwner { get; set; }
    public bool CanPut { get; set; }
    public bool CanUse { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
}
