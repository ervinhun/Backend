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
    IUserSessionRepository sessions,
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

        return await StartSessionAsync(user, request.PhoneModel, request.Platform, ct);
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

        return await StartSessionAsync(user, request.PhoneModel, request.Platform, ct);
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
        var session = await sessions.GetByRefreshTokenHashAsync(tokenHash, ct)
            ?? throw new UnauthorizedException(InvalidRefreshMessage);

        // An expired refresh token ends the sign-in, so the session row goes too.
        if (session.RefreshTokenExpiresAt is not { } expiresAt || expiresAt <= now)
        {
            await sessions.SignOutAsync(session.Id, now, ct);
            throw new UnauthorizedException(InvalidRefreshMessage);
        }

        var user = await users.GetByIdAsync(session.UserId, ct)
            ?? throw new UnauthorizedException(InvalidRefreshMessage);

        var refresh = tokenService.CreateRefreshToken();
        // A concurrent request already swapped this token, so it no longer works.
        if (!await sessions.TryRotateRefreshTokenAsync(session.Id, tokenHash, refresh.Hash, refresh.ExpiresAt, now, ct))
            throw new UnauthorizedException(InvalidRefreshMessage);

        return CreateResponse(user, session.Id, refresh);
    }

    public Task LogoutAsync(Guid sessionId, CancellationToken ct = default) =>
        sessions.SignOutAsync(sessionId, timeProvider.GetUtcNow(), ct);

    /// <summary>A new sign-in: a new user_sessions row holding the refresh token.</summary>
    private async Task<AuthResponse> StartSessionAsync(User user, string? phoneModel, string? platform, CancellationToken ct)
    {
        var refresh = tokenService.CreateRefreshToken();
        var session = await sessions.AddAsync(new UserSession
        {
            UserId = user.Id,
            RefreshTokenHash = refresh.Hash,
            RefreshTokenExpiresAt = refresh.ExpiresAt,
            PhoneModel = TrimToNull(phoneModel),
            Platform = TrimToNull(platform),
            LastSeenAt = timeProvider.GetUtcNow(),
        }, ct);

        return CreateResponse(user, session.Id, refresh);
    }

    private AuthResponse CreateResponse(User user, Guid sessionId, (string Token, string Hash, DateTimeOffset ExpiresAt) refresh)
    {
        var (accessToken, expiresAt) = tokenService.CreateAccessToken(user, sessionId);
        return new AuthResponse(accessToken, expiresAt, refresh.Token, refresh.ExpiresAt, user.ToDto());
    }

    private static string NormalizeEmail(string email) => email.Trim().ToLowerInvariant();

    private static string? TrimToNull(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
