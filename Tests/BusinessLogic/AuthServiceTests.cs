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
    private readonly InMemoryUserDeviceRepository _devices = new();
    private readonly FakeTimeProvider _time = new(DateTimeOffset.UtcNow);
    private readonly AuthService _service;

    public AuthServiceTests()
    {
        var tokens = new JwtTokenService(Microsoft.Extensions.Options.Options.Create(TestJwt.Options), _time);
        _service = new AuthService(_users, new PasswordHasher<User>(), tokens, _devices, _time);
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

    private UserDevice SignedIn => Assert.Single(_devices.All, d => d.DeletedAt is null);

    [Fact]
    public async Task Login_CreatesDevice_WithRefreshTokenStoredOnlyAsHash()
    {
        var response = await _service.RegisterAsync(Register());

        var device = Assert.Single(_devices.All);
        Assert.False(string.IsNullOrEmpty(response.RefreshToken));
        Assert.NotNull(device.RefreshTokenHash);
        Assert.NotEqual(response.RefreshToken, device.RefreshTokenHash);
        Assert.Null(device.PushToken);
        Assert.Equal(response.User.Id, device.UserId);
        Assert.Equal(_time.GetUtcNow().AddDays(TestJwt.Options.RefreshTokenDays), response.RefreshTokenExpiresAt);
        Assert.Equal(response.RefreshTokenExpiresAt, device.RefreshTokenExpiresAt);
    }

    [Fact]
    public async Task Refresh_RotatesTokens_OnSameDevice()
    {
        var first = await _service.RegisterAsync(Register());
        var oldHash = SignedIn.RefreshTokenHash;

        var second = await _service.RefreshAsync(Refresh(first));

        Assert.NotEqual(first.RefreshToken, second.RefreshToken);
        Assert.NotEqual(first.AccessToken, second.AccessToken);
        Assert.Equal(first.User.Id, second.User.Id);
        Assert.NotEqual(oldHash, Assert.Single(_devices.All).RefreshTokenHash);
    }

    [Fact]
    public async Task Refresh_UsedToken_IsRejected_NewerOneStillWorks()
    {
        var first = await _service.RegisterAsync(Register());
        var second = await _service.RefreshAsync(Refresh(first));

        await Assert.ThrowsAsync<UnauthorizedException>(() => _service.RefreshAsync(Refresh(first)));

        await _service.RefreshAsync(Refresh(second));
        Assert.Null(Assert.Single(_devices.All).DeletedAt);
    }

    [Fact]
    public async Task Refresh_ExpiredToken_SignsDeviceOut()
    {
        var response = await _service.RegisterAsync(Register());
        _time.Advance(TimeSpan.FromDays(TestJwt.Options.RefreshTokenDays) + TimeSpan.FromSeconds(1));

        await Assert.ThrowsAsync<UnauthorizedException>(() => _service.RefreshAsync(Refresh(response)));
        Assert.NotNull(Assert.Single(_devices.All).DeletedAt);
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
    public async Task Logout_SoftDeletesOnlyThatDevice()
    {
        await _service.RegisterAsync(Register());
        var phone = SignedIn;
        await _service.LoginAsync(new LoginRequest { Email = "anna@example.com", Password = "correct-horse" });

        await _service.LogoutAsync(phone.Id);
        await _service.LogoutAsync(phone.Id);

        Assert.NotNull(phone.DeletedAt);
        Assert.False(phone.IsActive);
        Assert.Null(phone.RefreshTokenHash);
        Assert.NotEqual(phone.Id, SignedIn.Id);
    }
}
