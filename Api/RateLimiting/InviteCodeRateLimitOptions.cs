using System.ComponentModel.DataAnnotations;

namespace Api.RateLimiting;

/// <summary>Bound from "RateLimiting:InviteCode" (env: RateLimiting__InviteCode__PermitLimit, ...).</summary>
public class InviteCodeRateLimitOptions
{
    public const string SectionName = "RateLimiting:InviteCode";

    /// <summary>Joins and invite code replacements one signed-in user may make per window.</summary>
    [Range(1, 10_000)]
    public int PermitLimit { get; set; } = 5;

    [Range(1, 3600)]
    public int WindowSeconds { get; set; } = 60;
}
