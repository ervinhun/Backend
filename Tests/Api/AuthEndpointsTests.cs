using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using BusinessLogic.Dtos.Auth;
using DataAccess;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Tests.Database;

namespace Tests.Api;

public class AuthEndpointsTests : DatabaseTest
{
    private readonly ApiFactory _factory;
    private readonly HttpClient _client;

    public AuthEndpointsTests(PostgresFixture database) : base(database)
    {
        _factory = new ApiFactory(database.ConnectionString);
        _client = _factory.CreateClient();
    }

    public override async Task DisposeAsync()
    {
        _client.Dispose();
        await _factory.DisposeAsync();
    }

    private static object RegisterBody(string email) =>
        new { email, password = "correct-horse", displayName = "Anna" };

    [Fact]
    public async Task Register_Then_Login_Then_Me()
    {
        var email = $"{Guid.NewGuid():N}@example.com";

        var register = await _client.PostAsJsonAsync("/api/auth/register", RegisterBody(email));
        Assert.Equal(HttpStatusCode.OK, register.StatusCode);

        var login = await _client.PostAsJsonAsync("/api/auth/login", new { email, password = "correct-horse" });
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        var auth = await login.Content.ReadFromJsonAsync<AuthResponse>();

        var meRequest = new HttpRequestMessage(HttpMethod.Get, "/api/auth/me");
        meRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", auth!.AccessToken);
        var me = await _client.SendAsync(meRequest);

        Assert.Equal(HttpStatusCode.OK, me.StatusCode);
        var user = await me.Content.ReadFromJsonAsync<UserDto>();
        Assert.Equal(email, user!.Email);
    }

    [Fact]
    public async Task Register_DuplicateEmail_Returns409()
    {
        var email = $"{Guid.NewGuid():N}@example.com";
        await _client.PostAsJsonAsync("/api/auth/register", RegisterBody(email));

        var second = await _client.PostAsJsonAsync("/api/auth/register", RegisterBody(email));

        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
    }

    [Fact]
    public async Task Register_InvalidBody_Returns400()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/register",
            new { email = "not-an-email", password = "short", displayName = "" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Login_WrongPassword_Returns401()
    {
        var email = $"{Guid.NewGuid():N}@example.com";
        await _client.PostAsJsonAsync("/api/auth/register", RegisterBody(email));

        var response = await _client.PostAsJsonAsync("/api/auth/login", new { email, password = "wrong-password" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("not-a-jwt")]
    public async Task Me_WithoutValidToken_Returns401(string? token)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/auth/me");
        if (token is not null)
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Me_AfterUserSoftDeleted_Returns401()
    {
        var email = $"{Guid.NewGuid():N}@example.com";
        var register = await _client.PostAsJsonAsync("/api/auth/register", RegisterBody(email));
        var auth = await register.Content.ReadFromJsonAsync<AuthResponse>();

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            await db.Users.Where(u => u.Email == email)
                .ExecuteUpdateAsync(s => s.SetProperty(u => u.DeletedAt, DateTimeOffset.UtcNow));
        }

        var request = new HttpRequestMessage(HttpMethod.Get, "/api/auth/me");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", auth!.AccessToken);
        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    private async Task<HttpStatusCode> SendAsync(HttpMethod method, string url, string? token)
    {
        var request = new HttpRequestMessage(method, url);
        if (token is not null)
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return (await _client.SendAsync(request)).StatusCode;
    }

    [Fact]
    public async Task Logout_SignsOutOnlyThatSession()
    {
        var email = $"{Guid.NewGuid():N}@example.com";
        var phone = await (await _client.PostAsJsonAsync("/api/auth/register", RegisterBody(email)))
            .Content.ReadFromJsonAsync<AuthResponse>();
        var tablet = await (await _client.PostAsJsonAsync("/api/auth/login", new { email, password = "correct-horse" }))
            .Content.ReadFromJsonAsync<AuthResponse>();

        Assert.Equal(HttpStatusCode.NoContent, await SendAsync(HttpMethod.Post, "/api/auth/logout", phone!.AccessToken));

        Assert.Equal(HttpStatusCode.Unauthorized, await SendAsync(HttpMethod.Get, "/api/auth/me", phone.AccessToken));
        Assert.Equal(HttpStatusCode.Unauthorized, await SendAsync(HttpMethod.Post, "/api/auth/logout", phone.AccessToken));
        Assert.Equal(HttpStatusCode.OK, await SendAsync(HttpMethod.Get, "/api/auth/me", tablet!.AccessToken));
    }

    [Fact]
    public async Task Logout_WithoutToken_Returns401()
    {
        Assert.Equal(HttpStatusCode.Unauthorized, await SendAsync(HttpMethod.Post, "/api/auth/logout", null));
    }

    [Fact]
    public async Task Refresh_GivesWorkingTokens_AndOldRefreshTokenStopsWorking()
    {
        var email = $"{Guid.NewGuid():N}@example.com";
        var first = await (await _client.PostAsJsonAsync("/api/auth/register", RegisterBody(email)))
            .Content.ReadFromJsonAsync<AuthResponse>();

        var refresh = await _client.PostAsJsonAsync("/api/auth/refresh", new { refreshToken = first!.RefreshToken });
        Assert.Equal(HttpStatusCode.OK, refresh.StatusCode);
        var second = await refresh.Content.ReadFromJsonAsync<AuthResponse>();

        Assert.Equal(HttpStatusCode.OK, await SendAsync(HttpMethod.Get, "/api/auth/me", second!.AccessToken));
        var reused = await _client.PostAsJsonAsync("/api/auth/refresh", new { refreshToken = first.RefreshToken });
        Assert.Equal(HttpStatusCode.Unauthorized, reused.StatusCode);
    }

    [Fact]
    public async Task Logout_AlsoEndsRefreshTokens()
    {
        var email = $"{Guid.NewGuid():N}@example.com";
        var auth = await (await _client.PostAsJsonAsync("/api/auth/register", RegisterBody(email)))
            .Content.ReadFromJsonAsync<AuthResponse>();

        Assert.Equal(HttpStatusCode.NoContent, await SendAsync(HttpMethod.Post, "/api/auth/logout", auth!.AccessToken));

        var refresh = await _client.PostAsJsonAsync("/api/auth/refresh", new { refreshToken = auth.RefreshToken });
        Assert.Equal(HttpStatusCode.Unauthorized, refresh.StatusCode);
    }
}
