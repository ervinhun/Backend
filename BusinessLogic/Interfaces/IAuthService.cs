using BusinessLogic.Dtos.Auth;

namespace BusinessLogic.Interfaces;

public interface IAuthService
{
    Task<AuthResponse> RegisterAsync(RegisterRequest request, CancellationToken ct = default);
    Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken ct = default);
    Task<UserDto> GetUserAsync(Guid userId, CancellationToken ct = default);

    /// <summary>
    /// Signs out one access token: it is rejected from now on, while the user's
    /// other tokens (other devices) keep working.
    /// </summary>
    Task LogoutAsync(Guid userId, Guid tokenId, DateTimeOffset expiresAt, CancellationToken ct = default);
}
