using System.Net;
using System.Net.Http.Json;
using DataAccess.Interfaces;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Tests.Fakes;

namespace Tests.Api;

public class AuthRateLimitTests
{
    private const int Limit = 3;

    private static HttpClient CreateClient() => new WebApplicationFactory<Program>()
        .WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Testing");
            builder.ConfigureAppConfiguration(config => config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = "Host=unused",
                ["Jwt:Secret"] = TestJwt.Options.Secret,
                ["Jwt:Issuer"] = TestJwt.Options.Issuer,
                ["Jwt:Audience"] = TestJwt.Options.Audience,
                ["RateLimiting:Auth:PermitLimit"] = Limit.ToString(),
                ["RateLimiting:Auth:WindowSeconds"] = "60",
            }));
            builder.ConfigureServices(services =>
            {
                services.Replace(ServiceDescriptor.Singleton<IUserRepository, InMemoryUserRepository>());
                services.Replace(ServiceDescriptor.Singleton<IUserSessionRepository, InMemoryUserSessionRepository>());
            });
        })
        .CreateClient();

    private static object WrongLogin => new { email = "anna@example.com", password = "wrong-password" };

    [Fact]
    public async Task Login_OverTheLimit_Returns429WithRetryAfter()
    {
        var client = CreateClient();
        for (var i = 0; i < Limit; i++)
            Assert.Equal(HttpStatusCode.Unauthorized, (await client.PostAsJsonAsync("/api/auth/login", WrongLogin)).StatusCode);

        var blocked = await client.PostAsJsonAsync("/api/auth/login", WrongLogin);

        Assert.Equal(HttpStatusCode.TooManyRequests, blocked.StatusCode);
        Assert.True(blocked.Headers.RetryAfter?.Delta > TimeSpan.Zero);
        var problem = await blocked.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.Equal(429, problem!.Status);
    }

    [Fact]
    public async Task LoginAndRegister_ShareOneBudget()
    {
        var client = CreateClient();
        for (var i = 0; i < Limit; i++)
            await client.PostAsJsonAsync("/api/auth/login", WrongLogin);

        var register = await client.PostAsJsonAsync("/api/auth/register",
            new { email = $"{Guid.NewGuid():N}@example.com", password = "correct-horse", displayName = "Anna" });

        Assert.Equal(HttpStatusCode.TooManyRequests, register.StatusCode);
    }

    [Fact]
    public async Task OtherEndpoints_AreNotLimited()
    {
        var client = CreateClient();
        for (var i = 0; i < Limit; i++)
            await client.PostAsJsonAsync("/api/auth/login", WrongLogin);

        // Still answers normally (401: no token), not 429.
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/auth/me")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized,
            (await client.PostAsJsonAsync("/api/auth/refresh", new { refreshToken = "x" })).StatusCode);
    }
}
