namespace DataAccess.Interfaces;

public enum AddFridgeStatus
{
    Added,

    /// <summary>The user already owns as many fridges as they may.</summary>
    LimitReached,
}
