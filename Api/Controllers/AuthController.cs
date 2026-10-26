using System.Security.Claims;
using BusinessLogic.Dtos.Auth;
using BusinessLogic.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.JsonWebTokens;

namespace Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController(IAuthService authService) : ControllerBase
{
    [HttpPost(nameof(Register))]
    [AllowAnonymous]
    public async Task<ActionResult<AuthResponse>> Register(RegisterRequest request, CancellationToken ct) =>
        await authService.RegisterAsync(request, ct);

    [HttpPost(nameof(Login))]
    [AllowAnonymous]
    public async Task<ActionResult<AuthResponse>> Login(LoginRequest request, CancellationToken ct) =>
        await authService.LoginAsync(request, ct);

    /// <summary>Returns the user the bearer token belongs to.</summary>
    [HttpGet(nameof(Me))]
    [Authorize]
    public async Task<ActionResult<UserDto>> Me(CancellationToken ct)
    {
        if (!Guid.TryParse(User.FindFirstValue(JwtRegisteredClaimNames.Sub), out var userId))
            return Unauthorized();

        return await authService.GetUserAsync(userId, ct);
    }

    /// <summary>Gets a new access token (and refresh token) without signing in again.</summary>
    [HttpPost(nameof(Refresh))]
    [AllowAnonymous]
    public async Task<ActionResult<AuthResponse>> Refresh(RefreshRequest request, CancellationToken ct) =>
        await authService.RefreshAsync(request, ct);

    /// <summary>Signs out this device: its access and refresh tokens stop working. Other devices stay signed in.</summary>
    [HttpPost(nameof(Logout))]
    [Authorize]
    public async Task<IActionResult> Logout(CancellationToken ct)
    {
        if (!Guid.TryParse(User.FindFirstValue(JwtRegisteredClaimNames.Sub), out var userId)
            || !Guid.TryParse(User.FindFirstValue(JwtRegisteredClaimNames.Jti), out var tokenId)
            || !long.TryParse(User.FindFirstValue(JwtRegisteredClaimNames.Exp), out var exp))
            return Unauthorized();

        Guid? sessionId = Guid.TryParse(User.FindFirstValue(JwtRegisteredClaimNames.Sid), out var sid) ? sid : null;
        await authService.LogoutAsync(userId, tokenId, sessionId, DateTimeOffset.FromUnixTimeSeconds(exp), ct);
        return NoContent();
    }
}
