using BusinessLogic.Dtos.Auth;
using BusinessLogic.Exceptions;
using BusinessLogic.Services;
using DataAccess.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Time.Testing;
using Tests.Fakes;

namespace Tests.BusinessLogic;

public class AuthServiceTests
{
    private readonly InMemoryUserRepository _users = new();
    private readonly InMemoryTokenRepository _tokens = new();
    private readonly FakeTimeProvider _time = new(DateTimeOffset.UtcNow);
    private readonly AuthService _service;

    public AuthServiceTests()
    {
        var tokens = new JwtTokenService(Microsoft.Extensions.Options.Options.Create(TestJwt.Options), _time);
        _service = new AuthService(_users, new PasswordHasher<User>(), tokens, _tokens, _time);
    }

    private static RegisterRequest Register(string email = "Anna@Example.com ", string password = "correct-horse") =>
        new() { Email = email, Password = password, DisplayName = " Anna " };

    [Fact]
    public async Task Register_StoresNormalizedEmailAndHashedPassword()
    {
        var response = await _service.RegisterAsync(Register());

        var stored = Assert.Single(_users.All);
        Assert.Equal("anna@example.com", stored.Email);
        Assert.Equal("Anna", stored.DefaultDisplayName);
        Assert.Equal("en", stored.Locale);
        Assert.NotNull(stored.PasswordHash);
        Assert.DoesNotContain("correct-horse", stored.PasswordHash);
        Assert.False(string.IsNullOrEmpty(response.AccessToken));
        Assert.Equal(stored.Id, response.User.Id);
    }

    [Fact]
    public async Task Register_DuplicateEmail_ThrowsConflict()
    {
        await _service.RegisterAsync(Register());

        await Assert.ThrowsAsync<ConflictException>(() => _service.RegisterAsync(Register("ANNA@example.com")));
    }

    [Fact]
    public async Task Login_WithCorrectPassword_ReturnsToken()
    {
        await _service.RegisterAsync(Register());

        var response = await _service.LoginAsync(new LoginRequest { Email = "anna@EXAMPLE.com", Password = "correct-horse" });

        Assert.False(string.IsNullOrEmpty(response.AccessToken));
        Assert.Equal("anna@example.com", response.User.Email);
    }

    [Fact]
    public async Task Login_WithWrongPassword_ThrowsUnauthorized()
    {
        await _service.RegisterAsync(Register());

        await Assert.ThrowsAsync<UnauthorizedException>(() =>
            _service.LoginAsync(new LoginRequest { Email = "anna@example.com", Password = "wrong-password" }));
    }

    [Fact]
    public async Task Login_UnknownEmail_ThrowsUnauthorized()
    {
        await Assert.ThrowsAsync<UnauthorizedException>(() =>
            _service.LoginAsync(new LoginRequest { Email = "nobody@example.com", Password = "whatever1" }));
    }

    [Fact]
    public async Task Login_SoftDeletedUser_ThrowsUnauthorized()
    {
        await _service.RegisterAsync(Register());
        _users.All.Single().DeletedAt = DateTimeOffset.UtcNow;

        await Assert.ThrowsAsync<UnauthorizedException>(() =>
            _service.LoginAsync(new LoginRequest { Email = "anna@example.com", Password = "correct-horse" }));
    }

    private static RefreshRequest Refresh(AuthResponse response) => new() { RefreshToken = response.RefreshToken };

    [Fact]
    public async Task Login_IssuesRefreshToken_StoredOnlyAsHash()
    {
        var response = await _service.RegisterAsync(Register());

        var stored = Assert.Single(_tokens.RefreshTokens);
        Assert.False(string.IsNullOrEmpty(response.RefreshToken));
        Assert.NotEqual(response.RefreshToken, stored.TokenHash);
        Assert.Equal(response.User.Id, stored.UserId);
        Assert.Equal(_time.GetUtcNow().AddDays(TestJwt.Options.RefreshTokenDays), response.RefreshTokenExpiresAt);
    }

    [Fact]
    public async Task Refresh_RotatesTokens_InSameFamily()
    {
        var first = await _service.RegisterAsync(Register());

        var second = await _service.RefreshAsync(Refresh(first));

        Assert.NotEqual(first.RefreshToken, second.RefreshToken);
        Assert.NotEqual(first.AccessToken, second.AccessToken);
        Assert.Equal(first.User.Id, second.User.Id);
        Assert.Single(_tokens.RefreshTokens.Select(t => t.FamilyId).Distinct());
        Assert.Single(_tokens.RefreshTokens, t => t.RevokedAt is null);
    }

    [Fact]
    public async Task Refresh_ReusedToken_RevokesWholeFamily()
    {
        var first = await _service.RegisterAsync(Register());
        var second = await _service.RefreshAsync(Refresh(first));

        // Someone replays the already-used first token...
        await Assert.ThrowsAsync<UnauthorizedException>(() => _service.RefreshAsync(Refresh(first)));

        // ...so the legitimate newer token is dead too.
        await Assert.ThrowsAsync<UnauthorizedException>(() => _service.RefreshAsync(Refresh(second)));
        Assert.All(_tokens.RefreshTokens, t => Assert.NotNull(t.RevokedAt));
    }

    [Fact]
    public async Task Refresh_OtherSignInsAreUnaffectedByReuse()
    {
        var phone = await _service.RegisterAsync(Register());
        var tablet = await _service.LoginAsync(new LoginRequest { Email = "anna@example.com", Password = "correct-horse" });
        await _service.RefreshAsync(Refresh(phone));
        await Assert.ThrowsAsync<UnauthorizedException>(() => _service.RefreshAsync(Refresh(phone)));

        await _service.RefreshAsync(Refresh(tablet));
    }

    [Fact]
    public async Task Refresh_ExpiredToken_ThrowsUnauthorized()
    {
        var response = await _service.RegisterAsync(Register());
        _time.Advance(TimeSpan.FromDays(TestJwt.Options.RefreshTokenDays) + TimeSpan.FromSeconds(1));

        await Assert.ThrowsAsync<UnauthorizedException>(() => _service.RefreshAsync(Refresh(response)));
    }

    [Fact]
    public async Task Refresh_UnknownToken_ThrowsUnauthorized()
    {
        await Assert.ThrowsAsync<UnauthorizedException>(() =>
            _service.RefreshAsync(new RefreshRequest { RefreshToken = "made-up" }));
    }

    [Fact]
    public async Task Refresh_DeletedUser_ThrowsUnauthorized()
    {
        var response = await _service.RegisterAsync(Register());
        _users.All.Single().DeletedAt = _time.GetUtcNow();

        await Assert.ThrowsAsync<UnauthorizedException>(() => _service.RefreshAsync(Refresh(response)));
    }

    [Fact]
    public async Task Logout_RevokesAccessToken_AndThatSignInsRefreshTokens()
    {
        var phone = await _service.RegisterAsync(Register());
        var tablet = await _service.LoginAsync(new LoginRequest { Email = "anna@example.com", Password = "correct-horse" });
        var phoneFamily = _tokens.RefreshTokens.First(t => t.RevokedAt is null).FamilyId;
        var tokenId = Guid.NewGuid();
        var expiresAt = _time.GetUtcNow().AddMinutes(30);

        await _service.LogoutAsync(phone.User.Id, tokenId, phoneFamily, expiresAt);
        await _service.LogoutAsync(phone.User.Id, tokenId, phoneFamily, expiresAt);

        var revoked = Assert.Single(_tokens.RevokedAccessTokens);
        Assert.Equal((tokenId, expiresAt), (revoked.Jti, revoked.ExpiresAt));
        Assert.All(_tokens.RefreshTokens.Where(t => t.FamilyId == phoneFamily), t => Assert.NotNull(t.RevokedAt));
        Assert.Single(_tokens.RefreshTokens, t => t.RevokedAt is null);
    }
}
