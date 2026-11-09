using BusinessLogic.Interfaces;

namespace Tests.Fakes;

/// <summary>Limits a test picks itself, as FeatureHub will one day.</summary>
public class FixedFridgeLimits(int maxMembers, int maxContainers) : IFridgeLimitDefaults
{
    public int MaxMembers => maxMembers;
    public int MaxContainers => maxContainers;
}
