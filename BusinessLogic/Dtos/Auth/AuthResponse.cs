namespace BusinessLogic.Dtos.Auth;

/// <param name="AccessToken">Short-lived JWT to send as the Bearer token.</param>
/// <param name="ExpiresAt">When <paramref name="AccessToken"/> expires.</param>
/// <param name="RefreshToken">Long-lived opaque token for POST /api/auth/refresh. Single use.</param>
/// <param name="RefreshTokenExpiresAt">When <paramref name="RefreshToken"/> expires.</param>
public record AuthResponse(
    string AccessToken,
    DateTimeOffset ExpiresAt,
    string RefreshToken,
    DateTimeOffset RefreshTokenExpiresAt,
    UserDto User);
