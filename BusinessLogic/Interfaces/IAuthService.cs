using BusinessLogic.Dtos.Auth;

namespace BusinessLogic.Interfaces;

public interface IAuthService
{
    Task<AuthResponse> RegisterAsync(RegisterRequest request, CancellationToken ct = default);
    Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken ct = default);
    Task<UserDto> GetUserAsync(Guid userId, CancellationToken ct = default);
}
