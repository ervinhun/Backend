using BusinessLogic.Interfaces;

namespace BusinessLogic.Services;

/// <summary>Fixed limits, until FeatureHub decides them.</summary>
public class FridgeLimitDefaults : IFridgeLimitDefaults
{
    public const int DefaultMaxMembers = 15;
    public const int DefaultMaxContainers = 10;

    public int MaxMembers => DefaultMaxMembers;
    public int MaxContainers => DefaultMaxContainers;
}
