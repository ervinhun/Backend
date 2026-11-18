using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using BusinessLogic.Dtos.Auth;
using BusinessLogic.Dtos.Fridge;
using DataAccess;
using DataAccess.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Tests.Database;

namespace Tests.Api;

/// <summary>The real API and services on Postgres: who may report a shown notification, and what gets recorded.</summary>
public class NotificationDispatchesEndpointsTests : DatabaseTest
{
    private readonly ApiFactory _factory;
    private readonly HttpClient _client;

    public NotificationDispatchesEndpointsTests(PostgresFixture database)
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

    private Task<HttpResponseMessage> PostAsync(AuthResponse caller, Guid fridgeId, object body)
    {
        var request = new HttpRequestMessage(
            HttpMethod.Post,
            $"/api/fridges/{fridgeId}/notification-dispatches"
        )
        {
            Content = JsonContent.Create(body),
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", caller.AccessToken);
        return _client.SendAsync(request);
    }

    private async Task<(Guid FridgeId, Guid ItemId)> CreateFridgeWithItemAsync(AuthResponse owner)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/fridges")
        {
            Content = JsonContent.Create(new { name = "Home" }),
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", owner.AccessToken);
        var fridge = (await (await _client.SendAsync(request)).Content.ReadFromJsonAsync<FridgeResponse>())!;

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var container = new Container
        {
            FridgeId = fridge.Id,
            Name = "Shelf",
            ContainerType = ContainerType.Fridge,
        };
        db.Containers.Add(container);
        await db.SaveChangesAsync();
        var item = new Item
        {
            ContainerId = container.Id,
            Name = "Milk",
            AddedByUserId = owner.User.Id,
            AddedAt = new DateOnly(2030, 1, 1),
            ExpiryDate = new DateOnly(2030, 1, 6),
        };
        db.Items.Add(item);
        await db.SaveChangesAsync();
        return (fridge.Id, item.Id);
    }

    private static object Report(Guid itemId, string stage = "warning") =>
        new { sentAt = DateTimeOffset.UtcNow, items = new[] { new { itemId, stage } } };

    [Fact]
    public async Task Member_ReportsAShownNotification_AndItIsRecorded()
    {
        var anna = await RegisterAsync("Anna");
        var (fridgeId, itemId) = await CreateFridgeWithItemAsync(anna);

        var response = await PostAsync(anna, fridgeId, Report(itemId, "expiry"));

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var row = await db.NotificationDispatches.SingleAsync();
        Assert.Equal(itemId, row.ItemId);
        Assert.Equal(NotificationStage.Expiry, row.Stage);
    }

    [Fact]
    public async Task NonMember_Gets404()
    {
        var anna = await RegisterAsync("Anna");
        var eve = await RegisterAsync("Eve");
        var (fridgeId, itemId) = await CreateFridgeWithItemAsync(anna);

        var response = await PostAsync(eve, fridgeId, Report(itemId));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task UnknownItem_Gets404()
    {
        var anna = await RegisterAsync("Anna");
        var (fridgeId, _) = await CreateFridgeWithItemAsync(anna);

        var response = await PostAsync(anna, fridgeId, Report(Guid.NewGuid()));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Theory]
    [InlineData("danger")]
    [InlineData("")]
    public async Task InvalidStage_Gets400(string stage)
    {
        var anna = await RegisterAsync("Anna");
        var (fridgeId, itemId) = await CreateFridgeWithItemAsync(anna);

        var response = await PostAsync(anna, fridgeId, Report(itemId, stage));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task MissingSentAt_Gets400()
    {
        var anna = await RegisterAsync("Anna");
        var (fridgeId, itemId) = await CreateFridgeWithItemAsync(anna);

        var response = await PostAsync(
            anna,
            fridgeId,
            new { items = new[] { new { itemId, stage = "warning" } } }
        );

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task NoItems_Gets400()
    {
        var anna = await RegisterAsync("Anna");
        var (fridgeId, _) = await CreateFridgeWithItemAsync(anna);

        var response = await PostAsync(
            anna,
            fridgeId,
            new { sentAt = DateTimeOffset.UtcNow, items = Array.Empty<object>() }
        );

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
