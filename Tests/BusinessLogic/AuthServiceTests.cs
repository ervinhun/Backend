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
    private readonly InMemoryUserSessionRepository _sessions = new();
    private readonly FakeTimeProvider _time = new(DateTimeOffset.UtcNow);
    private readonly FakeExternalTokenValidator _external = new();
    private readonly AuthService _service;

    public AuthServiceTests()
    {
        var tokens = new JwtTokenService(Microsoft.Extensions.Options.Options.Create(TestJwt.Options), _time);
        _service = new AuthService(_users, new PasswordHasher<User>(), tokens, _sessions, _time, _external);
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

    private UserSession SignedIn => Assert.Single(_sessions.All, d => d.DeletedAt is null);

    [Fact]
    public async Task Login_CreatesSession_WithRefreshTokenStoredOnlyAsHash()
    {
        var response = await _service.RegisterAsync(Register());

        var session = Assert.Single(_sessions.All);
        Assert.False(string.IsNullOrEmpty(response.RefreshToken));
        Assert.NotNull(session.RefreshTokenHash);
        Assert.NotEqual(response.RefreshToken, session.RefreshTokenHash);
        Assert.Null(session.PushToken);
        Assert.Equal(response.User.Id, session.UserId);
        Assert.Equal(_time.GetUtcNow().AddDays(TestJwt.Options.RefreshTokenDays), response.RefreshTokenExpiresAt);
        Assert.Equal(response.RefreshTokenExpiresAt, session.RefreshTokenExpiresAt);
    }

    [Fact]
    public async Task RegisterAndLogin_StorePhoneModelAndPlatformOnTheirSession()
    {
        await _service.RegisterAsync(new RegisterRequest
        {
            Email = "anna@example.com", Password = "correct-horse", DisplayName = "Anna",
            PhoneModel = " iPhone 15 Pro ", Platform = " ios ",
        });
        await _service.LoginAsync(new LoginRequest
        {
            Email = "anna@example.com", Password = "correct-horse", PhoneModel = "Pixel 8", Platform = "android",
        });
        await _service.LoginAsync(new LoginRequest { Email = "anna@example.com", Password = "correct-horse", Platform = "  " });

        Assert.Equal(
            new (string?, string?)[] { (null, null), ("Pixel 8", "android"), ("iPhone 15 Pro", "ios") },
            _sessions.All.Select(s => (s.PhoneModel, s.Platform)).OrderBy(x => x.PhoneModel, StringComparer.Ordinal).ToArray());
    }

    [Fact]
    public async Task Refresh_RotatesTokens_OnSameSession()
    {
        var first = await _service.RegisterAsync(Register());
        var oldHash = SignedIn.RefreshTokenHash;

        var second = await _service.RefreshAsync(Refresh(first));

        Assert.NotEqual(first.RefreshToken, second.RefreshToken);
        Assert.NotEqual(first.AccessToken, second.AccessToken);
        Assert.Equal(first.User.Id, second.User.Id);
        Assert.NotEqual(oldHash, Assert.Single(_sessions.All).RefreshTokenHash);
    }

    [Fact]
    public async Task Refresh_UsedToken_IsRejected_NewerOneStillWorks()
    {
        var first = await _service.RegisterAsync(Register());
        var second = await _service.RefreshAsync(Refresh(first));

        await Assert.ThrowsAsync<UnauthorizedException>(() => _service.RefreshAsync(Refresh(first)));

        await _service.RefreshAsync(Refresh(second));
        Assert.Null(Assert.Single(_sessions.All).DeletedAt);
    }

    [Fact]
    public async Task Refresh_ExpiredToken_SignsSessionOut()
    {
        var response = await _service.RegisterAsync(Register());
        _time.Advance(TimeSpan.FromDays(TestJwt.Options.RefreshTokenDays) + TimeSpan.FromSeconds(1));

        await Assert.ThrowsAsync<UnauthorizedException>(() => _service.RefreshAsync(Refresh(response)));
        Assert.NotNull(Assert.Single(_sessions.All).DeletedAt);
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
    public async Task Logout_SoftDeletesOnlyThatSession()
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

    private ExternalLoginRequest External(
        string sub = "google-123", string? email = "Anna@Example.com", bool verified = true,
        string provider = AuthProviders.Google, string? displayName = null) =>
        new()
        {
            Provider = provider,
            IdToken = _external.Issue(new ExternalIdentity(provider, sub, email, verified, "Anna G")),
            DisplayName = displayName,
            PhoneModel = "Pixel 9",
            Platform = "android",
        };

    [Fact]
    public async Task External_NewIdentity_CreatesUserWithoutPassword()
    {
        var response = await _service.ExternalLoginAsync(External());

        var stored = Assert.Single(_users.All);
        Assert.Equal("anna@example.com", stored.Email);
        Assert.Null(stored.PasswordHash);
        Assert.Equal(AuthProviders.Google, stored.AuthProvider);
        Assert.Equal("Anna G", stored.DefaultDisplayName);
        var login = Assert.Single(_users.Logins);
        Assert.Equal((stored.Id, "google-123"), (login.UserId, login.ProviderUserId));
        Assert.Equal(stored.Id, response.User.Id);
    }

    [Fact]
    public async Task External_StartsSessionWithDeviceInfo()
    {
        var response = await _service.ExternalLoginAsync(External());

        var session = Assert.Single(_sessions.All);
        Assert.Equal(response.User.Id, session.UserId);
        Assert.Equal(("Pixel 9", "android"), (session.PhoneModel, session.Platform));
        Assert.False(string.IsNullOrEmpty(response.RefreshToken));
    }

    [Fact]
    public async Task External_DisplayNameFromRequest_WinsOverToken()
    {
        // Apple's token has no name; the app sends it on the first sign-in.
        await _service.ExternalLoginAsync(External(sub: "apple-1", provider: AuthProviders.Apple, displayName: " Anna A "));

        Assert.Equal("Anna A", Assert.Single(_users.All).DefaultDisplayName);
    }

    [Fact]
    public async Task External_SameIdentityAgain_ReturnsSameUser()
    {
        var first = await _service.ExternalLoginAsync(External());
        // Email changed at the provider: still matched by sub, not by email.
        var second = await _service.ExternalLoginAsync(External(email: "new@example.com"));

        Assert.Equal(first.User.Id, second.User.Id);
        Assert.Single(_users.All);
        Assert.Single(_users.Logins);
    }

    [Fact]
    public async Task External_VerifiedEmailOfPasswordAccount_LinksAndKeepsPassword()
    {
        var registered = await _service.RegisterAsync(Register());

        var external = await _service.ExternalLoginAsync(External());

        Assert.Equal(registered.User.Id, external.User.Id);
        Assert.Equal(registered.User.Id, Assert.Single(_users.Logins).UserId);
        Assert.Equal(AuthProviders.Password, Assert.Single(_users.All).AuthProvider);
        await _service.LoginAsync(new LoginRequest { Email = "anna@example.com", Password = "correct-horse" });
    }

    [Fact]
    public async Task External_GoogleAndAppleWithSameEmail_LinkToOneUser()
    {
        var google = await _service.ExternalLoginAsync(External());
        var apple = await _service.ExternalLoginAsync(External(sub: "apple-456", provider: AuthProviders.Apple));

        Assert.Equal(google.User.Id, apple.User.Id);
        Assert.Equal(2, _users.Logins.Count);
    }

    [Theory]
    [InlineData("anna@example.com", false)]
    [InlineData(null, true)]
    public async Task External_WithoutVerifiedEmail_ThrowsUnauthorized(string? email, bool verified)
    {
        await _service.RegisterAsync(Register());

        await Assert.ThrowsAsync<UnauthorizedException>(() =>
            _service.ExternalLoginAsync(External(email: email, verified: verified)));
        Assert.Empty(_users.Logins);
    }

    [Fact]
    public async Task External_EmailOfDeletedAccount_ThrowsConflict()
    {
        await _service.RegisterAsync(Register());
        _users.All.Single().DeletedAt = DateTimeOffset.UtcNow;

        await Assert.ThrowsAsync<ConflictException>(() => _service.ExternalLoginAsync(External()));
    }

    [Fact]
    public async Task External_LoginOfDeletedAccount_ThrowsConflict()
    {
        await _service.ExternalLoginAsync(External());
        _users.All.Single().DeletedAt = DateTimeOffset.UtcNow;

        await Assert.ThrowsAsync<ConflictException>(() => _service.ExternalLoginAsync(External(email: "other@example.com")));
    }

    [Fact]
    public async Task External_InactiveLogin_DoesNotSignIn()
    {
        await _service.ExternalLoginAsync(External());
        _users.Logins.Single().IsActive = false;

        // The unlinked identity can't be used, and isn't silently linked again.
        await Assert.ThrowsAsync<ConflictException>(() => _service.ExternalLoginAsync(External()));
    }

    [Fact]
    public async Task External_InvalidToken_ThrowsUnauthorized()
    {
        await Assert.ThrowsAsync<UnauthorizedException>(() => _service.ExternalLoginAsync(
            new ExternalLoginRequest { Provider = AuthProviders.Google, IdToken = "forged" }));
    }
}
