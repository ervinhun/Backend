using System.ComponentModel.DataAnnotations;

namespace Api.RateLimiting;

/// <summary>Bound from "RateLimiting:FeatureToggles" (env: RateLimiting__FeatureToggles__PermitLimit, ...).</summary>
public class FeatureToggleRateLimitOptions
{
    public const string SectionName = "RateLimiting:FeatureToggles";

    /// <summary>Flag lookups one signed-in user may make per window. An app start reads a handful, so 30 a minute is plenty.</summary>
    [Range(1, 10_000)]
    public int PermitLimit { get; set; } = 30;

    [Range(1, 3600)]
    public int WindowSeconds { get; set; } = 60;
}
