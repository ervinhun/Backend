using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using BusinessLogic.Dtos.Auth;
using BusinessLogic.Dtos.Fridge;
using BusinessLogic.Services;
using DataAccess;
using DataAccess.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Tests.Database;

namespace Tests.Api;

/// <summary>The real API and service on Postgres: checks the pieces fit together, including dependency injection.</summary>
public class FridgesEndpointsTests : DatabaseTest
{
    private readonly ApiFactory _factory;
    private readonly HttpClient _client;

    public FridgesEndpointsTests(PostgresFixture database)
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

    private async Task<FridgeResponse> CreateFridgeAsync(AuthResponse owner, string name = "Home")
    {
        var response = await SendAsync(owner, HttpMethod.Post, "/api/fridges", new { name });
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

    [Fact]
    public async Task Create_ThenGetAndList_ReturnsTheFridgeToItsOwner()
    {
        var anna = await RegisterAsync("Anna");

        var response = await SendAsync(
            anna,
            HttpMethod.Post,
            "/api/fridges",
            new { name = "Home" }
        );

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = (await response.Content.ReadFromJsonAsync<FridgeResponse>())!;
        Assert.EndsWith($"/api/fridges/{created.Id}", response.Headers.Location!.AbsolutePath);
        Assert.True(created.IsOwner);
        Assert.NotNull(created.InviteCode);

        var get = await SendAsync(anna, HttpMethod.Get, $"/api/fridges/{created.Id}");
        Assert.Equal(HttpStatusCode.OK, get.StatusCode);
        Assert.Equal("Home", (await get.Content.ReadFromJsonAsync<FridgeResponse>())!.Name);

        var list = await (
            await SendAsync(anna, HttpMethod.Get, "/api/fridges")
        ).Content.ReadFromJsonAsync<List<FridgeResponse>>();
        Assert.Equal(created.Id, Assert.Single(list!).Id);
    }

    [Fact]
    public async Task Create_BeyondTheOwnedFridgeLimit_Returns409Problem()
    {
        var anna = await RegisterAsync("Anna");
        for (var i = 0; i < FridgeService.MaxOwnedFridges; i++)
            await CreateFridgeAsync(anna, $"Fridge {i}");

        var response = await SendAsync(
            anna,
            HttpMethod.Post,
            "/api/fridges",
            new { name = "One too many" }
        );

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        Assert.Equal(
            FridgeService.MaxOwnedFridges,
            (
                await (
                    await SendAsync(anna, HttpMethod.Get, "/api/fridges")
                ).Content.ReadFromJsonAsync<List<FridgeResponse>>()
            )!.Count
        );
    }

    [Fact]
    public async Task Create_InvalidName_Returns400()
    {
        var anna = await RegisterAsync("Anna");

        var response = await SendAsync(anna, HttpMethod.Post, "/api/fridges", new { name = " " });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task WithoutAToken_Returns401()
    {
        var response = await SendAsync(null, HttpMethod.Get, "/api/fridges");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Stranger_GetsNotFound_WithoutLearningTheFridgeExists()
    {
        var anna = await RegisterAsync("Anna");
        var ben = await RegisterAsync("Ben");
        var fridge = await CreateFridgeAsync(anna);

        Assert.Equal(
            HttpStatusCode.NotFound,
            (await SendAsync(ben, HttpMethod.Get, $"/api/fridges/{fridge.Id}")).StatusCode
        );
        Assert.Equal(
            HttpStatusCode.NotFound,
            (await SendAsync(ben, HttpMethod.Delete, $"/api/fridges/{fridge.Id}")).StatusCode
        );
        Assert.Equal(
            HttpStatusCode.NotFound,
            (await SendAsync(ben, HttpMethod.Get, $"/api/fridges/{fridge.Id}/members")).StatusCode
        );
    }

    [Fact]
    public async Task Member_CanRead_ButNotChangeTheFridge()
    {
        var anna = await RegisterAsync("Anna");
        var ben = await RegisterAsync("Ben");
        var fridge = await CreateFridgeAsync(anna);
        await AddMemberAsync(fridge, ben);

        var get = await SendAsync(ben, HttpMethod.Get, $"/api/fridges/{fridge.Id}");
        var seen = await get.Content.ReadFromJsonAsync<FridgeResponse>();
        Assert.Null(seen!.InviteCode);
        Assert.Equal(
            HttpStatusCode.Forbidden,
            (
                await SendAsync(
                    ben,
                    HttpMethod.Put,
                    $"/api/fridges/{fridge.Id}",
                    new { name = "Mine" }
                )
            ).StatusCode
        );
        Assert.Equal(
            HttpStatusCode.Forbidden,
            (
                await SendAsync(ben, HttpMethod.Post, $"/api/fridges/{fridge.Id}/invite-code")
            ).StatusCode
        );
        Assert.Equal(
            HttpStatusCode.Forbidden,
            (await SendAsync(ben, HttpMethod.Delete, $"/api/fridges/{fridge.Id}")).StatusCode
        );
    }

    [Fact]
    public async Task Owner_RenamesRegeneratesTheCode_AndDeletes()
    {
        var anna = await RegisterAsync("Anna");
        var fridge = await CreateFridgeAsync(anna);

        var renamed = await (
            await SendAsync(
                anna,
                HttpMethod.Put,
                $"/api/fridges/{fridge.Id}",
                new { name = "Cabin" }
            )
        ).Content.ReadFromJsonAsync<FridgeResponse>();
        var regenerated = await (
            await SendAsync(anna, HttpMethod.Post, $"/api/fridges/{fridge.Id}/invite-code")
        ).Content.ReadFromJsonAsync<FridgeResponse>();
        var deleted = await SendAsync(anna, HttpMethod.Delete, $"/api/fridges/{fridge.Id}");

        Assert.Equal("Cabin", renamed!.Name);
        Assert.NotEqual(fridge.InviteCode, regenerated!.InviteCode);
        Assert.Equal(HttpStatusCode.NoContent, deleted.StatusCode);
        Assert.Equal(
            HttpStatusCode.NotFound,
            (await SendAsync(anna, HttpMethod.Get, $"/api/fridges/{fridge.Id}")).StatusCode
        );
    }

    [Fact]
    public async Task Join_WithTheInviteCode_AddsTheCaller_UntilTheFridgeIsFull()
    {
        var anna = await RegisterAsync("Anna");
        var ben = await RegisterAsync("Ben");
        var cleo = await RegisterAsync("Cleo");
        var fridge = await CreateFridgeAsync(anna);
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            await db.Fridges.ExecuteUpdateAsync(s => s.SetProperty(f => f.MaxMembers, 2));
        }
        var body = new { inviteCode = fridge.InviteCode };

        var joined = await SendAsync(ben, HttpMethod.Post, "/api/fridges/join", body);
        var full = await SendAsync(cleo, HttpMethod.Post, "/api/fridges/join", body);
        var again = await SendAsync(ben, HttpMethod.Post, "/api/fridges/join", body);
        var unknown = await SendAsync(
            cleo,
            HttpMethod.Post,
            "/api/fridges/join",
            new { inviteCode = "NOSUCHCODE" }
        );

        Assert.Equal(HttpStatusCode.OK, joined.StatusCode);
        Assert.Equal(fridge.Id, (await joined.Content.ReadFromJsonAsync<FridgeResponse>())!.Id);
        Assert.Equal(HttpStatusCode.Conflict, full.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, again.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, unknown.StatusCode);
        Assert.Equal(
            HttpStatusCode.OK,
            (await SendAsync(ben, HttpMethod.Get, $"/api/fridges/{fridge.Id}")).StatusCode
        );
    }

    [Fact]
    public async Task Join_WithoutACode_Returns400()
    {
        var anna = await RegisterAsync("Anna");

        var response = await SendAsync(
            anna,
            HttpMethod.Post,
            "/api/fridges/join",
            new { inviteCode = "" }
        );

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task MemberRights_WithAnExpiryInThePast_Returns400()
    {
        var anna = await RegisterAsync("Anna");
        var ben = await RegisterAsync("Ben");
        var fridge = await CreateFridgeAsync(anna);
        await AddMemberAsync(fridge, ben);

        var response = await SendAsync(
            anna,
            HttpMethod.Put,
            $"/api/fridges/{fridge.Id}/members/{ben.User.Id}",
            new
            {
                canPut = true,
                canUse = true,
                accessExpiresAt = DateTimeOffset.UtcNow.AddDays(-1),
            }
        );

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task MemberWhoseAccessExpired_GetsNotFound_LikeAnyStranger()
    {
        var anna = await RegisterAsync("Anna");
        var ben = await RegisterAsync("Ben");
        var fridge = await CreateFridgeAsync(anna);
        await AddMemberAsync(fridge, ben);
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var past = DateTimeOffset.UtcNow.AddHours(-1);
            await db
                .FridgeMembers.Where(m => m.UserId == ben.User.Id)
                .ExecuteUpdateAsync(s => s.SetProperty(m => m.AccessExpiresAt, past));
        }

        Assert.Equal(
            HttpStatusCode.NotFound,
            (await SendAsync(ben, HttpMethod.Get, $"/api/fridges/{fridge.Id}")).StatusCode
        );
        Assert.Empty(
            (
                await (
                    await SendAsync(ben, HttpMethod.Get, "/api/fridges")
                ).Content.ReadFromJsonAsync<List<FridgeResponse>>()
            )!
        );
    }

    [Fact]
    public async Task MemberRightsAndRemoval_ForSomeoneNotInTheFridge_Return404Problem()
    {
        var anna = await RegisterAsync("Anna");
        var cleo = await RegisterAsync("Cleo");
        var fridge = await CreateFridgeAsync(anna);

        foreach (var notMember in new[] { cleo.User.Id, Guid.NewGuid() })
        {
            var url = $"/api/fridges/{fridge.Id}/members/{notMember}";
            var responses = new[]
            {
                await SendAsync(anna, HttpMethod.Put, url, new { canPut = true, canUse = true }),
                await SendAsync(anna, HttpMethod.Delete, url),
            };

            foreach (var response in responses)
            {
                Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
                Assert.Equal(
                    "application/problem+json",
                    response.Content.Headers.ContentType?.MediaType
                );
                var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
                Assert.Equal(StatusCodes.Status404NotFound, problem!.Status);
            }
        }
    }

    [Fact]
    public async Task MemberRights_ForAMemberWhoseAccessExpired_Return404()
    {
        var anna = await RegisterAsync("Anna");
        var ben = await RegisterAsync("Ben");
        var fridge = await CreateFridgeAsync(anna);
        await AddMemberAsync(fridge, ben);
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var past = DateTimeOffset.UtcNow.AddHours(-1);
            await db
                .FridgeMembers.Where(m => m.UserId == ben.User.Id)
                .ExecuteUpdateAsync(s => s.SetProperty(m => m.AccessExpiresAt, past));
        }

        var response = await SendAsync(
            anna,
            HttpMethod.Put,
            $"/api/fridges/{fridge.Id}/members/{ben.User.Id}",
            new { canPut = true, canUse = true }
        );

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Owner_ManagesMembers_AndMembersCanLeave()
    {
        var anna = await RegisterAsync("Anna");
        var ben = await RegisterAsync("Ben");
        var fridge = await CreateFridgeAsync(anna);
        await AddMemberAsync(fridge, ben);

        var members = await (
            await SendAsync(anna, HttpMethod.Get, $"/api/fridges/{fridge.Id}/members")
        ).Content.ReadFromJsonAsync<List<FridgeMemberResponse>>();
        Assert.Equal(["Anna", "Ben"], members!.Select(m => m.DisplayName));

        var updated = await SendAsync(
            anna,
            HttpMethod.Put,
            $"/api/fridges/{fridge.Id}/members/{ben.User.Id}",
            new { canPut = false, canUse = true }
        );
        Assert.False((await updated.Content.ReadFromJsonAsync<FridgeMemberResponse>())!.CanPut);

        var rightsChange = await SendAsync(
            anna,
            HttpMethod.Put,
            $"/api/fridges/{fridge.Id}/members/{anna.User.Id}",
            new { canPut = false, canUse = false }
        );
        var removal = await SendAsync(
            anna,
            HttpMethod.Delete,
            $"/api/fridges/{fridge.Id}/members/{anna.User.Id}"
        );
        foreach (var conflict in new[] { rightsChange, removal })
        {
            Assert.Equal(HttpStatusCode.Conflict, conflict.StatusCode);
            Assert.Equal(
                "application/problem+json",
                conflict.Content.Headers.ContentType?.MediaType
            );
            var problem = await conflict.Content.ReadFromJsonAsync<ProblemDetails>();
            Assert.Equal(StatusCodes.Status409Conflict, problem!.Status);
            Assert.NotEmpty(problem.Detail!);
        }

        Assert.Equal(
            HttpStatusCode.NoContent,
            (
                await SendAsync(
                    ben,
                    HttpMethod.Delete,
                    $"/api/fridges/{fridge.Id}/members/{ben.User.Id}"
                )
            ).StatusCode
        );
        Assert.Equal(
            HttpStatusCode.NotFound,
            (await SendAsync(ben, HttpMethod.Get, $"/api/fridges/{fridge.Id}")).StatusCode
        );
    }
}
