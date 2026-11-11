using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using BusinessLogic.Dtos.Auth;
using BusinessLogic.Dtos.Fridge;
using BusinessLogic.Dtos.Notification;
using DataAccess;
using DataAccess.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Tests.Database;

namespace Tests.Api;

/// <summary>The real API and services on Postgres, including who may see and change a fridge's notification settings.</summary>
public class NotificationSettingsEndpointsTests : DatabaseTest
{
    private readonly ApiFactory _factory;
    private readonly HttpClient _client;

    public NotificationSettingsEndpointsTests(PostgresFixture database)
        : base(database)
    {
        _factory = new ApiFactory(database.ConnectionString);
        _client = _factory.CreateClient();
    }

    public override async Task DisposeAsync()
    {
        _client.Dispose();
        await _factory.DisposeAsync();
    }

    private async Task<AuthResponse> RegisterAsync(string displayName)
    {
        var response = await _client.PostAsJsonAsync(
            "/api/auth/register",
            new
            {
                email = $"{Guid.NewGuid():N}@example.com",
                password = "correct-horse",
                displayName,
            }
        );
        return (await response.Content.ReadFromJsonAsync<AuthResponse>())!;
    }

    private Task<HttpResponseMessage> SendAsync(
        AuthResponse? caller,
        HttpMethod method,
        string url,
        object? body = null
    )
    {
        var request = new HttpRequestMessage(method, url);
        if (caller is not null)
            request.Headers.Authorization = new AuthenticationHeaderValue(
                "Bearer",
                caller.AccessToken
            );
        if (body is not null)
            request.Content = JsonContent.Create(body);
        return _client.SendAsync(request);
    }

    private async Task<FridgeResponse> CreateFridgeAsync(AuthResponse owner)
    {
        var response = await SendAsync(owner, HttpMethod.Post, "/api/fridges", new { name = "Home" });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<FridgeResponse>())!;
    }

    private async Task AddMemberAsync(FridgeResponse fridge, AuthResponse user)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.FridgeMembers.Add(new FridgeMember { FridgeId = fridge.Id, UserId = user.User.Id });
        await db.SaveChangesAsync();
    }

    private static string Url(FridgeResponse fridge) =>
        $"/api/fridges/{fridge.Id}/notification-settings";

    private static async Task<ProblemDetails> AssertProblemAsync(
        HttpResponseMessage response,
        int status
    )
    {
        Assert.Equal((HttpStatusCode)status, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.Equal(status, problem!.Status);
        return problem;
    }

    [Fact]
    public async Task Owner_SeesTheDefaults_ThenSavesAndReadsBackTheirOwn()
    {
        var anna = await RegisterAsync("Anna");
        var fridge = await CreateFridgeAsync(anna);

        var response = await SendAsync(anna, HttpMethod.Get, Url(fridge));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var defaults = (await response.Content.ReadFromJsonAsync<NotificationSettingsResponse>())!;
        Assert.Equal(6, defaults.WarningDaysBefore);
        Assert.Equal(1, defaults.DangerDaysBefore);
        Assert.Equal("all_members", defaults.RecipientsScope);

        response = await SendAsync(
            anna,
            HttpMethod.Put,
            Url(fridge),
            new
            {
                warningDaysBefore = 10,
                dangerDaysBefore = 3,
                recipientsScope = "adder_and_owners",
            }
        );
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        response = await SendAsync(anna, HttpMethod.Get, Url(fridge));
        var saved = (await response.Content.ReadFromJsonAsync<NotificationSettingsResponse>())!;
        Assert.Equal(fridge.Id, saved.FridgeId);
        Assert.Equal(10, saved.WarningDaysBefore);
        Assert.Equal(3, saved.DangerDaysBefore);
        Assert.Equal("adder_and_owners", saved.RecipientsScope);
    }

    [Fact]
    public async Task Owner_PicksCustomRecipients()
    {
        var anna = await RegisterAsync("Anna");
        var ben = await RegisterAsync("Ben");
        var fridge = await CreateFridgeAsync(anna);
        await AddMemberAsync(fridge, ben);

        var response = await SendAsync(
            anna,
            HttpMethod.Put,
            Url(fridge),
            new
            {
                warningDaysBefore = 6,
                dangerDaysBefore = 1,
                recipientsScope = "custom",
                recipientUserIds = new[] { ben.User.Id },
            }
        );

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var saved = (await response.Content.ReadFromJsonAsync<NotificationSettingsResponse>())!;
        Assert.Equal("custom", saved.RecipientsScope);
        Assert.Equal([ben.User.Id], saved.RecipientUserIds);
    }

    [Fact]
    public async Task Member_CanSeeTheSettings_ButNotChangeThem()
    {
        var anna = await RegisterAsync("Anna");
        var ben = await RegisterAsync("Ben");
        var fridge = await CreateFridgeAsync(anna);
        await AddMemberAsync(fridge, ben);

        Assert.Equal(HttpStatusCode.OK, (await SendAsync(ben, HttpMethod.Get, Url(fridge))).StatusCode);
        var response = await SendAsync(
            ben,
            HttpMethod.Put,
            Url(fridge),
            new { warningDaysBefore = 6, dangerDaysBefore = 1, recipientsScope = "all_members" }
        );

        await AssertProblemAsync(response, 403);
    }

    [Fact]
    public async Task NonMember_GetsNotFound()
    {
        var anna = await RegisterAsync("Anna");
        var eve = await RegisterAsync("Eve");
        var fridge = await CreateFridgeAsync(anna);

        await AssertProblemAsync(await SendAsync(eve, HttpMethod.Get, Url(fridge)), 404);
        await AssertProblemAsync(
            await SendAsync(
                eve,
                HttpMethod.Put,
                Url(fridge),
                new { warningDaysBefore = 6, dangerDaysBefore = 1, recipientsScope = "all_members" }
            ),
            404
        );
    }

    [Fact]
    public async Task ACustomRecipientOutsideTheFridge_IsAConflict()
    {
        var anna = await RegisterAsync("Anna");
        var eve = await RegisterAsync("Eve");
        var fridge = await CreateFridgeAsync(anna);

        var response = await SendAsync(
            anna,
            HttpMethod.Put,
            Url(fridge),
            new
            {
                warningDaysBefore = 6,
                dangerDaysBefore = 1,
                recipientsScope = "custom",
                recipientUserIds = new[] { eve.User.Id },
            }
        );

        await AssertProblemAsync(response, 409);
    }

    [Theory]
    [InlineData(6, 6, "all_members")]
    [InlineData(0, 0, "all_members")]
    [InlineData(6, 1, "nobody")]
    [InlineData(6, 1, "custom")]
    public async Task InvalidSettings_AreRejected(int warning, int danger, string scope)
    {
        var anna = await RegisterAsync("Anna");
        var fridge = await CreateFridgeAsync(anna);

        var response = await SendAsync(
            anna,
            HttpMethod.Put,
            Url(fridge),
            new { warningDaysBefore = warning, dangerDaysBefore = danger, recipientsScope = scope }
        );

        await AssertProblemAsync(response, 400);
    }

    [Fact]
    public async Task WithoutAToken_Returns401()
    {
        var anna = await RegisterAsync("Anna");
        var fridge = await CreateFridgeAsync(anna);

        Assert.Equal(
            HttpStatusCode.Unauthorized,
            (await SendAsync(null, HttpMethod.Get, Url(fridge))).StatusCode
        );
    }
}
