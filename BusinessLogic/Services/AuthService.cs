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
    ITokenRepository tokens,
    TimeProvider timeProvider) : IAuthService
{
    private const string EmailTakenMessage = "An account with this email already exists.";
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
        var stored = await tokens.GetRefreshTokenByHashAsync(tokenService.HashRefreshToken(request.RefreshToken), ct);

        if (stored is null || stored.ExpiresAt <= now)
            throw new UnauthorizedException(InvalidRefreshMessage);

        // Each refresh token works once. Seeing one again (or losing a race for it)
        // means it may have been copied, so end that whole sign-in on every holder.
        if (stored.RevokedAt is not null || !await tokens.TryRevokeRefreshTokenAsync(stored.Id, now, ct))
        {
            await tokens.RevokeRefreshTokenFamilyAsync(stored.FamilyId, now, ct);
            throw new UnauthorizedException(InvalidRefreshMessage);
        }

        var user = await users.GetByIdAsync(stored.UserId, ct)
            ?? throw new UnauthorizedException(InvalidRefreshMessage);
        return await IssueTokensAsync(user, stored.FamilyId, ct);
    }

    public async Task LogoutAsync(
        Guid userId, Guid tokenId, Guid? sessionId, DateTimeOffset expiresAt, CancellationToken ct = default)
    {
        await tokens.RevokeAccessTokenAsync(
            new RevokedToken { Jti = tokenId, UserId = userId, ExpiresAt = expiresAt }, ct);

        // Also end the sign-in's refresh tokens, or the app could just refresh back in.
        if (sessionId is { } familyId)
            await tokens.RevokeRefreshTokenFamilyAsync(familyId, timeProvider.GetUtcNow(), ct);
    }

    /// <summary>A new sign-in on a device: a new refresh token family.</summary>
    private Task<AuthResponse> StartSessionAsync(User user, CancellationToken ct) =>
        IssueTokensAsync(user, Guid.NewGuid(), ct);

    private async Task<AuthResponse> IssueTokensAsync(User user, Guid familyId, CancellationToken ct)
    {
        var refresh = tokenService.CreateRefreshToken();
        await tokens.AddRefreshTokenAsync(new RefreshToken
        {
            UserId = user.Id,
            FamilyId = familyId,
            TokenHash = refresh.Hash,
            ExpiresAt = refresh.ExpiresAt,
        }, ct);

        var (accessToken, expiresAt) = tokenService.CreateAccessToken(user, familyId);
        return new AuthResponse(accessToken, expiresAt, refresh.Token, refresh.ExpiresAt, user.ToDto());
    }

    private static string NormalizeEmail(string email) => email.Trim().ToLowerInvariant();
}
