namespace DataAccess.Interfaces;

public enum AddContainerStatus
{
    Added,

    /// <summary>The fridge doesn't exist (any more).</summary>
    FridgeNotFound,

    /// <summary>The fridge already has as many containers as its max_containers allows.</summary>
    Full,
}

/// <param name="MaxContainers">The fridge's limit; 0 for <see cref="AddContainerStatus.FridgeNotFound"/>.</param>
public record AddContainerResult(AddContainerStatus Status, int MaxContainers);
