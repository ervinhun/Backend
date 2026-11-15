using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using BusinessLogic.Dtos.Auth;
using Tests.Database;

namespace Tests.Api;

public class CatalogRateLimitTests : DatabaseTest
{
    private const int Limit = 3;

    private readonly ApiFactory _factory;
    private readonly HttpClient _client;

    public CatalogRateLimitTests(PostgresFixture database)
        : base(database)
    {
        _factory = new ApiFactory(
            database.ConnectionString,
            new Dictionary<string, string?>
            {
                ["RateLimiting:Catalog:PermitLimit"] = Limit.ToString(),
                ["RateLimiting:Catalog:WindowSeconds"] = "60",
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
            new
            {
                email = $"{Guid.NewGuid():N}@example.com",
                password = "correct-horse",
                displayName = "Anna",
            }
        );
        return (await response.Content.ReadFromJsonAsync<AuthResponse>())!;
    }

    private Task<HttpResponseMessage> GetAsync(string url, AuthResponse? caller = null)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        if (caller is not null)
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", caller.AccessToken);
        return _client.SendAsync(request);
    }

    [Fact]
    public async Task Anonymous_OverTheLimit_Returns429WithRetryAfter()
    {
        for (var i = 0; i < Limit; i++)
            Assert.Equal(HttpStatusCode.OK, (await GetAsync("/api/product-types")).StatusCode);

        var blocked = await GetAsync("/api/product-types");

        Assert.Equal(HttpStatusCode.TooManyRequests, blocked.StatusCode);
        Assert.True(blocked.Headers.RetryAfter?.Delta > TimeSpan.Zero);
    }

    [Fact]
    public async Task CategoriesAndTypes_ShareOneBudget()
    {
        await GetAsync("/api/product-categories");
        await GetAsync("/api/product-types");
        await GetAsync("/api/product-categories");

        Assert.Equal(HttpStatusCode.TooManyRequests, (await GetAsync("/api/product-types")).StatusCode);
    }

    [Fact]
    public async Task SignedInUsers_HaveTheirOwnBudget_ApartFromTheirIp()
    {
        var anna = await RegisterAsync();
        for (var i = 0; i < Limit; i++)
            await GetAsync("/api/product-types");

        var anonymousBlocked = await GetAsync("/api/product-types");
        var annaAllowed = await GetAsync("/api/product-types", anna);

        Assert.Equal(HttpStatusCode.TooManyRequests, anonymousBlocked.StatusCode);
        Assert.Equal(HttpStatusCode.OK, annaAllowed.StatusCode);
    }
}
