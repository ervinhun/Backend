using FeatureHubHelper;

namespace Tests.Fakes;

public class FakeFeatureFlags : IFeatureFlags
{
    public bool Enabled { get; set; } = true;
    public string? LastFeature { get; private set; }
    public string? LastRequestId { get; private set; }
    public string? LastCountry { get; private set; }

    public Task<bool> IsEnabledAsync(string featureKey, string? requestId = null, string? countryName = null,
        string? platform = null, string? version = null, string? planTier = null)
    {
        LastFeature = featureKey;
        LastRequestId = requestId;
        LastCountry = countryName;
        return Task.FromResult(Enabled);
    }
}
