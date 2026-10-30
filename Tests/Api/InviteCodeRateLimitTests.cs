using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using BusinessLogic.Dtos.Auth;
using BusinessLogic.Dtos.Fridge;
using Tests.Database;

namespace Tests.Api;

public class InviteCodeRateLimitTests : DatabaseTest
{
    private const int Limit = 3;

    private readonly ApiFactory _factory;
    private readonly HttpClient _client;

    public InviteCodeRateLimitTests(PostgresFixture database)
        : base(database)
    {
        _factory = new ApiFactory(
            database.ConnectionString,
            new Dictionary<string, string?>
            {
                ["RateLimiting:InviteCode:PermitLimit"] = Limit.ToString(),
                ["RateLimiting:InviteCode:WindowSeconds"] = "60",
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

    private Task<HttpResponseMessage> SendAsync(
        AuthResponse caller,
        HttpMethod method,
        string url,
        object? body = null
    )
    {
        var request = new HttpRequestMessage(method, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", caller.AccessToken);
        if (body is not null)
            request.Content = JsonContent.Create(body);
        return _client.SendAsync(request);
    }

    private static object WrongCode => new { inviteCode = "NOSUCHCODE" };

    [Fact]
    public async Task Join_OverTheLimit_Returns429WithRetryAfter()
    {
        var anna = await RegisterAsync();
        for (var i = 0; i < Limit; i++)
            Assert.Equal(
                HttpStatusCode.NotFound,
                (await SendAsync(anna, HttpMethod.Post, "/api/fridges/join", WrongCode)).StatusCode
            );

        var blocked = await SendAsync(anna, HttpMethod.Post, "/api/fridges/join", WrongCode);

        Assert.Equal(HttpStatusCode.TooManyRequests, blocked.StatusCode);
        Assert.True(blocked.Headers.RetryAfter?.Delta > TimeSpan.Zero);
    }

    [Fact]
    public async Task Join_AndRegenerate_ShareOneBudget()
    {
        var anna = await RegisterAsync();
        var fridge = (
            await (
                await SendAsync(anna, HttpMethod.Post, "/api/fridges", new { name = "Home" })
            ).Content.ReadFromJsonAsync<FridgeResponse>()
        )!;
        await SendAsync(anna, HttpMethod.Post, "/api/fridges/join", WrongCode);
        await SendAsync(anna, HttpMethod.Post, "/api/fridges/join", WrongCode);
        await SendAsync(anna, HttpMethod.Post, $"/api/fridges/{fridge.Id}/RegenerateInviteCode");

        var blocked = await SendAsync(
            anna,
            HttpMethod.Post,
            $"/api/fridges/{fridge.Id}/RegenerateInviteCode"
        );

        Assert.Equal(HttpStatusCode.TooManyRequests, blocked.StatusCode);
    }

    [Fact]
    public async Task EachUser_HasTheirOwnBudget_EvenOnTheSameIp()
    {
        var anna = await RegisterAsync();
        var ben = await RegisterAsync();
        for (var i = 0; i < Limit; i++)
            await SendAsync(anna, HttpMethod.Post, "/api/fridges/join", WrongCode);

        var annaBlocked = await SendAsync(anna, HttpMethod.Post, "/api/fridges/join", WrongCode);
        var benAllowed = await SendAsync(ben, HttpMethod.Post, "/api/fridges/join", WrongCode);

        Assert.Equal(HttpStatusCode.TooManyRequests, annaBlocked.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, benAllowed.StatusCode);
    }

    [Fact]
    public async Task OtherFridgeEndpoints_AreNotLimited()
    {
        var anna = await RegisterAsync();
        for (var i = 0; i < Limit; i++)
            await SendAsync(anna, HttpMethod.Post, "/api/fridges/join", WrongCode);

        Assert.Equal(
            HttpStatusCode.Created,
            (
                await SendAsync(anna, HttpMethod.Post, "/api/fridges", new { name = "Home" })
            ).StatusCode
        );
        Assert.Equal(
            HttpStatusCode.OK,
            (await SendAsync(anna, HttpMethod.Get, "/api/fridges")).StatusCode
        );
    }
}
