using System.ComponentModel.DataAnnotations;

namespace Api.RateLimiting;

/// <summary>Bound from "RateLimiting:Catalog" (env: RateLimiting__Catalog__PermitLimit, ...).</summary>
public class CatalogRateLimitOptions
{
    public const string SectionName = "RateLimiting:Catalog";

    /// <summary>Product category and type lookups one user, or one IP without a token, may make per window.</summary>
    [Range(1, 10_000)]
    public int PermitLimit { get; set; } = 30;

    [Range(1, 3600)]
    public int WindowSeconds { get; set; } = 60;
}
