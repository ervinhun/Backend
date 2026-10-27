namespace DataAccess.Interfaces;

public enum AddFridgeStatus
{
    Added,

    /// <summary>The user already owns as many fridges as they may.</summary>
    LimitReached,
}

/// <param name="OwnedFridges">How many fridges the user owned before this one.</param>
public record AddFridgeResult(AddFridgeStatus Status, int OwnedFridges);
