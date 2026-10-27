namespace BusinessLogic.Dtos.Fridge;

public class FridgeMemberResponse
{
    public Guid UserId { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public bool IsOwner { get; set; }
    public bool CanPut { get; set; }
    public bool CanUse { get; set; }
    public DateTimeOffset? AccessExpiresAt { get; set; }
    public DateTimeOffset JoinedAt { get; set; }
}
