using System.ComponentModel.DataAnnotations;

namespace Api.RateLimiting;

/// <summary>Bound from "RateLimiting:Auth" (env: RateLimiting__Auth__PermitLimit, ...).</summary>
public class AuthRateLimitOptions
{
    public const string SectionName = "RateLimiting:Auth";

    /// <summary>Requests one client IP may make per window.</summary>
    [Range(1, 10_000)]
    public int PermitLimit { get; set; } = 10;

    [Range(1, 3600)]
    public int WindowSeconds { get; set; } = 60;
}
