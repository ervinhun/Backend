using BusinessLogic.Dtos.Auth;

namespace BusinessLogic.Interfaces;

public interface IAuthService
{
    Task<AuthResponse> RegisterAsync(RegisterRequest request, CancellationToken ct = default);
    Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken ct = default);

    /// <summary>
    /// Signs in with a verified Google or Apple ID token. Creates the account on first
    /// sign-in, or links the provider to an existing account with the same verified email.
    /// </summary>
    Task<AuthResponse> ExternalLoginAsync(ExternalLoginRequest request, CancellationToken ct = default);
    Task<UserDto> GetUserAsync(Guid userId, CancellationToken ct = default);

    /// <summary>Swaps a refresh token for a new access + refresh token pair. The old refresh token stops working.</summary>
    Task<AuthResponse> RefreshAsync(RefreshRequest request, CancellationToken ct = default);

    /// <summary>
    /// Signs out one session: its access and refresh tokens stop working, while the
    /// user's other sessions stay signed in.
    /// </summary>
    Task LogoutAsync(Guid sessionId, CancellationToken ct = default);
}
