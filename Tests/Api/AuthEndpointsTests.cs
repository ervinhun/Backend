using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using BusinessLogic.Dtos.Auth;
using DataAccess.Interfaces;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Tests.Fakes;

namespace Tests.Api;

public class AuthEndpointsTests : IClassFixture<AuthEndpointsTests.Factory>
{
    public class Factory : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");
            builder.ConfigureAppConfiguration(config => config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = "Host=unused",
                ["Jwt:Secret"] = TestJwt.Options.Secret,
                ["Jwt:Issuer"] = TestJwt.Options.Issuer,
                ["Jwt:Audience"] = TestJwt.Options.Audience,
            }));
            builder.ConfigureServices(services =>
                services.Replace(ServiceDescriptor.Singleton<IUserRepository, InMemoryUserRepository>()));
        }
    }

    private readonly HttpClient _client;

    public AuthEndpointsTests(Factory factory) => _client = factory.CreateClient();

    private static object RegisterBody(string email) =>
        new { email, password = "correct-horse", displayName = "Anna" };

    [Fact]
    public async Task Register_Then_Login_Then_Me()
    {
        var email = $"{Guid.NewGuid():N}@example.com";

        var register = await _client.PostAsJsonAsync("/api/auth/register", RegisterBody(email));
        Assert.Equal(HttpStatusCode.Created, register.StatusCode);

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
}
