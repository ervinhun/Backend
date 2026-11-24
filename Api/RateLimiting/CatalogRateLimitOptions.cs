using System.ComponentModel.DataAnnotations;

namespace Api.RateLimiting;

/// <summary>Bound from "RateLimiting:Catalog" (env: RateLimiting__Catalog__PermitLimit, ...).</summary>
public class CatalogRateLimitOptions
{
    public const string SectionName = "RateLimiting:Catalog";

    /// <summary>
    /// Product category and type lookups one user may make per window. Without a token it is one device
    /// (<see cref="RateLimitHeaders.DeviceId"/>), or one IP if that header is missing too.
    /// </summary>
    [Range(1, 10_000)]
    public int PermitLimit { get; set; } = 30;

    /// <summary>
    /// Lookups without a token one IP may make per window, all its devices together. Device ids can be made up, so
    /// this is what stops a client that sends a new one each time.
    /// </summary>
    [Range(1, 10_000)]
    public int AnonymousIpPermitLimit { get; set; } = 300;

    [Range(1, 3600)]
    public int WindowSeconds { get; set; } = 60;
}
