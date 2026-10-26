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
    IRevokedTokenRepository revokedTokens) : IAuthService
{
    private const string EmailTakenMessage = "An account with this email already exists.";

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

        return CreateResponse(user);
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

        return CreateResponse(user);
    }

    public async Task<UserDto> GetUserAsync(Guid userId, CancellationToken ct = default)
    {
        var user = await users.GetByIdAsync(userId, ct)
            ?? throw new NotFoundException("User not found.");
        return user.ToDto();
    }

    public Task LogoutAsync(Guid userId, Guid tokenId, DateTimeOffset expiresAt, CancellationToken ct = default) =>
        revokedTokens.RevokeAsync(new RevokedToken { Jti = tokenId, UserId = userId, ExpiresAt = expiresAt }, ct);

    private AuthResponse CreateResponse(User user)
    {
        var (token, expiresAt) = tokenService.CreateAccessToken(user);
        return new AuthResponse(token, expiresAt, user.ToDto());
    }

    private static string NormalizeEmail(string email) => email.Trim().ToLowerInvariant();
}
