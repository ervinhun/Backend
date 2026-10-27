namespace DataAccess.Interfaces;

public enum JoinFridgeStatus
{
    Joined,

    /// <summary>No fridge has this invite code.</summary>
    UnknownCode,

    AlreadyMember,

    /// <summary>The fridge already has as many members as its max_members allows.</summary>
    Full,
}

/// <param name="FridgeId">The fridge the code belongs to; empty for <see cref="JoinFridgeStatus.UnknownCode"/>.</param>
public record JoinFridgeResult(JoinFridgeStatus Status, Guid FridgeId);
