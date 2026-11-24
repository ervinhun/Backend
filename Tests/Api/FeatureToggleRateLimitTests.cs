using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using BusinessLogic.Dtos.Auth;
using Tests.Database;

namespace Tests.Api;

public class FeatureToggleRateLimitTests : DatabaseTest
{
    private const int Limit = 2;

    private readonly ApiFactory _factory;
    private readonly HttpClient _client;

    public FeatureToggleRateLimitTests(PostgresFixture database)
        : base(database)
    {
        _factory = new ApiFactory(
            database.ConnectionString,
            new Dictionary<string, string?>
            {
                ["RateLimiting:FeatureToggles:PermitLimit"] = Limit.ToString(),
                ["RateLimiting:FeatureToggles:WindowSeconds"] = "60",
            }
        );
        _client = _factory.CreateClient();
    }

    public override async Task DisposeAsync()
    {
        _client.Dispose();
        await _factory.DisposeAsync();
    }

    private async Task<AuthResponse> RegisterAsync()
    {
        var response = await _client.PostAsJsonAsync(
            "/api/auth/register",
            new { email = $"{Guid.NewGuid():N}@example.com", password = "correct-horse", displayName = "Anna" });
        return (await response.Content.ReadFromJsonAsync<AuthResponse>())!;
    }

    private Task<HttpResponseMessage> GetFlagAsync(AuthResponse caller)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "/feature-toggles/createContainer");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", caller.AccessToken);
        return _client.SendAsync(request);
    }

    [Fact]
    public async Task OverTheLimit_Returns429WithRetryAfter_ForThatUserOnly()
    {
        var anna = await RegisterAsync();
        var ben = await RegisterAsync();

        for (var i = 0; i < Limit; i++)
            Assert.Equal(HttpStatusCode.OK, (await GetFlagAsync(anna)).StatusCode);

        var limited = await GetFlagAsync(anna);
        Assert.Equal(HttpStatusCode.TooManyRequests, limited.StatusCode);
        Assert.True(limited.Headers.RetryAfter is not null);

        Assert.Equal(HttpStatusCode.OK, (await GetFlagAsync(ben)).StatusCode);
    }
}
