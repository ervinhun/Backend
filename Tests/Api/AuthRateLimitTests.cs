using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using Tests.Database;

namespace Tests.Api;

public class AuthRateLimitTests(PostgresFixture database) : DatabaseTest(database)
{
    private const int Limit = 3;

    private readonly List<ApiFactory> _factories = [];

    // A new factory per call gives each test its own rate limit budget.
    private HttpClient CreateClient()
    {
        var factory = new ApiFactory(Database.ConnectionString, new Dictionary<string, string?>
        {
            ["RateLimiting:Auth:PermitLimit"] = Limit.ToString(),
            ["RateLimiting:Auth:WindowSeconds"] = "60",
        });
        _factories.Add(factory);
        return factory.CreateClient();
    }

    public override async Task DisposeAsync()
    {
        foreach (var factory in _factories)
            await factory.DisposeAsync();
    }

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
