using BusinessLogic.Dtos.Auth;

namespace BusinessLogic.Interfaces;

public interface IAuthService
{
    Task<AuthResponse> RegisterAsync(RegisterRequest request, CancellationToken ct = default);
    Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken ct = default);
    Task<UserDto> GetUserAsync(Guid userId, CancellationToken ct = default);

    /// <summary>Swaps a refresh token for a new access + refresh token pair. The old refresh token stops working.</summary>
    Task<AuthResponse> RefreshAsync(RefreshRequest request, CancellationToken ct = default);

    /// <summary>
    /// Signs out one device: its access and refresh tokens stop working, while the
    /// user's other devices stay signed in.
    /// </summary>
    Task LogoutAsync(Guid deviceId, CancellationToken ct = default);
}
