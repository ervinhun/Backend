using System.ComponentModel.DataAnnotations;

namespace BusinessLogic.Dtos.Auth;

public class RegisterRequest
{
    [Required, EmailAddress, MaxLength(254)]
    public string Email { get; init; } = string.Empty;

    [Required, MinLength(8), MaxLength(128)]
    public string Password { get; init; } = string.Empty;

    [Required, MinLength(1), MaxLength(100)]
    public string DisplayName { get; init; } = string.Empty;

    [MaxLength(10)]
    public string? Locale { get; init; }

    /// <summary>The phone the user is signing in on, e.g. "iPhone 15 Pro". Stored on the new session.</summary>
    [MaxLength(100)]
    public string? PhoneModel { get; init; }

    /// <summary>The phone's OS, e.g. "ios" or "android". Stored on the new session.</summary>
    [MaxLength(20)]
    public string? Platform { get; init; }
}
