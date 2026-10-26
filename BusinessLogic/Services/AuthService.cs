using BusinessLogic.Dtos.Auth;
using BusinessLogic.Exceptions;
using BusinessLogic.Interfaces;
using BusinessLogic.Mappers;
using DataAccess.Entities;
using DataAccess.Exceptions;
using DataAccess.Interfaces;
using Microsoft.AspNetCore.Identity;

namespace BusinessLogic.Services;

public class AuthService(
    IUserRepository users,
    IPasswordHasher<User> passwordHasher,
    ITokenService tokenService,
    IUserDeviceRepository devices,
    TimeProvider timeProvider) : IAuthService
{
    private const string EmailTakenMessage = "There was an error registering this e-mail, try another one.";
    private const string InvalidRefreshMessage = "Your session has expired. Please sign in again.";

    // Verified against when the email is unknown, so a login for a missing
    // account costs the same as one with a wrong password (no user enumeration by timing).
    private static readonly string DummyPasswordHash =
        new PasswordHasher<User>().HashPassword(new User(), Guid.NewGuid().ToString());

    public async Task<AuthResponse> RegisterAsync(RegisterRequest request, CancellationToken ct = default)
    {
        var email = NormalizeEmail(request.Email);

        if (await users.EmailExistsAsync(email, ct))
            throw new ConflictException(EmailTakenMessage);

        var user = new User
        {
            Email = email,
            AuthProvider = AuthProviders.Password,
            DefaultDisplayName = request.DisplayName.Trim(),
            Locale = string.IsNullOrWhiteSpace(request.Locale) ? "en" : request.Locale.Trim(),
        };
        user.PasswordHash = passwordHasher.HashPassword(user, request.Password);

        try
        {
            user = await users.AddAsync(user, ct);
        }
        catch (DuplicateKeyException)
        {
            // Lost a race with a concurrent registration for the same email.
            throw new ConflictException(EmailTakenMessage);
        }

        return await StartSessionAsync(user, ct);
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken ct = default)
    {
        var user = await users.GetByEmailAsync(NormalizeEmail(request.Email), ct);

        var result = user?.PasswordHash is null
            ? passwordHasher.VerifyHashedPassword(new User(), DummyPasswordHash, request.Password)
            : passwordHasher.VerifyHashedPassword(user, user.PasswordHash, request.Password);

        // A missing account always fails here too: DummyPasswordHash is of a random password.
        if (user?.PasswordHash is null || result == PasswordVerificationResult.Failed)
            throw new UnauthorizedException("Invalid email or password.");

        return await StartSessionAsync(user, ct);
    }

    public async Task<UserDto> GetUserAsync(Guid userId, CancellationToken ct = default)
    {
        var user = await users.GetByIdAsync(userId, ct)
            ?? throw new NotFoundException("User not found.");
        return user.ToDto();
    }

    public async Task<AuthResponse> RefreshAsync(RefreshRequest request, CancellationToken ct = default)
    {
        var now = timeProvider.GetUtcNow();
        var tokenHash = tokenService.HashRefreshToken(request.RefreshToken);
        var device = await devices.GetByRefreshTokenHashAsync(tokenHash, ct)
            ?? throw new UnauthorizedException(InvalidRefreshMessage);

        // An expired refresh token ends the sign-in, so the device row goes too.
        if (device.RefreshTokenExpiresAt is not { } expiresAt || expiresAt <= now)
        {
            await devices.SignOutAsync(device.Id, now, ct);
            throw new UnauthorizedException(InvalidRefreshMessage);
        }

        var user = await users.GetByIdAsync(device.UserId, ct)
            ?? throw new UnauthorizedException(InvalidRefreshMessage);

        var refresh = tokenService.CreateRefreshToken();
        // A concurrent request already swapped this token, so it no longer works.
        if (!await devices.TryRotateRefreshTokenAsync(device.Id, tokenHash, refresh.Hash, refresh.ExpiresAt, now, ct))
            throw new UnauthorizedException(InvalidRefreshMessage);

        return CreateResponse(user, device.Id, refresh);
    }

    public Task LogoutAsync(Guid deviceId, CancellationToken ct = default) =>
        devices.SignOutAsync(deviceId, timeProvider.GetUtcNow(), ct);

    /// <summary>A new sign-in: a new user_devices row holding the refresh token.</summary>
    private async Task<AuthResponse> StartSessionAsync(User user, CancellationToken ct)
    {
        var refresh = tokenService.CreateRefreshToken();
        var device = await devices.AddAsync(new UserDevice
        {
            UserId = user.Id,
            RefreshTokenHash = refresh.Hash,
            RefreshTokenExpiresAt = refresh.ExpiresAt,
            LastSeenAt = timeProvider.GetUtcNow(),
        }, ct);

        return CreateResponse(user, device.Id, refresh);
    }

    private AuthResponse CreateResponse(User user, Guid deviceId, (string Token, string Hash, DateTimeOffset ExpiresAt) refresh)
    {
        var (accessToken, expiresAt) = tokenService.CreateAccessToken(user, deviceId);
        return new AuthResponse(accessToken, expiresAt, refresh.Token, refresh.ExpiresAt, user.ToDto());
    }

    private static string NormalizeEmail(string email) => email.Trim().ToLowerInvariant();
}
