using System.ComponentModel.DataAnnotations;
using DataAccess.Entities;

namespace BusinessLogic.Dtos.Auth;

public class ExternalLoginRequest
{
    [Required, AllowedValues(AuthProviders.Google, AuthProviders.Apple)]
    public string Provider { get; init; } = string.Empty;

    /// <summary>The ID token the app got from Google / Sign in with Apple.</summary>
    [Required, MaxLength(8192)]
    public string IdToken { get; init; } = string.Empty;

    /// <summary>
    /// Used only when this sign-in creates the account. Apple puts no name in its ID token
    /// and gives it to the app only on the very first sign-in, so the app should send it then.
    /// </summary>
    [MaxLength(100)]
    public string? DisplayName { get; init; }

    [MaxLength(10)]
    public string? Locale { get; init; }

    /// <summary>The phone the user is signing in on, e.g. "iPhone 15 Pro". Stored on the new session.</summary>
    [MaxLength(100)]
    public string? PhoneModel { get; init; }

    /// <summary>The phone's OS, e.g. "ios" or "android". Stored on the new session.</summary>
    [MaxLength(20)]
    public string? Platform { get; init; }
}
