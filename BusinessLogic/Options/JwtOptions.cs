using System.ComponentModel.DataAnnotations;

namespace BusinessLogic.Options;

/// <summary>Bound from the "Jwt" config section (env: Jwt__Secret, Jwt__Issuer, ...).</summary>
public class JwtOptions
{
    public const string SectionName = "Jwt";

    /// <summary>HMAC-SHA256 signing key. Must be at least 32 bytes (256 bits).</summary>
    [Required, MinLength(32)]
    public string Secret { get; set; } = string.Empty;

    [Required]
    public string Issuer { get; set; } = string.Empty;

    [Required]
    public string Audience { get; set; } = string.Empty;

    [Range(1, 60 * 24 * 30)]
    public int AccessTokenMinutes { get; set; } = 60;
}
