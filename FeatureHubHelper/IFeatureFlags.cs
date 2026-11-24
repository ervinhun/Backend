namespace FeatureHubHelper;

public interface IFeatureFlags
{
   /// <summary>True when the flag is on for this context. Missing or non-boolean flags count as off.</summary>
   Task<bool> IsEnabledAsync(
      string featureKey,
      string? requestId = null,
      string? countryName = null,
      string? platform = null,
      string? version = null,
      string? planTier = null);
}
