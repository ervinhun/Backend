using BusinessLogic.Dtos.Auth;
using BusinessLogic.Exceptions;
using BusinessLogic.Services;
using DataAccess.Entities;
using Microsoft.AspNetCore.Identity;
using Tests.Fakes;

namespace Tests.BusinessLogic;

public class AuthServiceTests
{
    private readonly InMemoryUserRepository _users = new();
    private readonly AuthService _service;

    public AuthServiceTests()
    {
        var tokens = new JwtTokenService(Microsoft.Extensions.Options.Options.Create(TestJwt.Options), TimeProvider.System);
        _service = new AuthService(_users, new PasswordHasher<User>(), tokens);
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
}
