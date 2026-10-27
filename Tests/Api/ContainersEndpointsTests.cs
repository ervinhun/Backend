using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using BusinessLogic.Dtos.Auth;
using BusinessLogic.Dtos.Container;
using BusinessLogic.Dtos.Fridge;
using DataAccess;
using DataAccess.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Tests.Database;

namespace Tests.Api;

/// <summary>The real API and services on Postgres, including who may see and change a fridge's containers.</summary>
public class ContainersEndpointsTests : DatabaseTest
{
    private readonly ApiFactory _factory;
    private readonly HttpClient _client;

    public ContainersEndpointsTests(PostgresFixture database)
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
        var response = await SendAsync(
            owner,
            HttpMethod.Post,
            "/api/fridges",
            new { name = "Home" }
        );
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<FridgeResponse>())!;
    }

    private async Task AddMemberAsync(FridgeResponse fridge, AuthResponse user, bool canPut = true)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.FridgeMembers.Add(
            new FridgeMember
            {
                FridgeId = fridge.Id,
                UserId = user.User.Id,
                CanPut = canPut,
            }
        );
        await db.SaveChangesAsync();
    }

    private static string Url(FridgeResponse fridge, Guid? containerId = null) =>
        $"/api/fridges/{fridge.Id}/containers" + (containerId is null ? "" : $"/{containerId}");

    private async Task<ContainerResponse> CreateContainerAsync(
        AuthResponse caller,
        FridgeResponse fridge,
        string name = "Top shelf",
        string type = "fridge"
    )
    {
        var response = await SendAsync(caller, HttpMethod.Post, Url(fridge), new { name, type });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<ContainerResponse>())!;
    }

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
    public async Task NewFridges_StartWithTheDefaultLimits()
    {
        var anna = await RegisterAsync("Anna");

        var fridge = await CreateFridgeAsync(anna);

        Assert.Equal(15, fridge.MaxMembers);
        Assert.Equal(10, fridge.MaxContainers);
    }

    [Fact]
    public async Task Owner_CreatesListsRenamesAndDeletesContainers()
    {
        var anna = await RegisterAsync("Anna");
        var fridge = await CreateFridgeAsync(anna);

        var response = await SendAsync(
            anna,
            HttpMethod.Post,
            Url(fridge),
            new { name = "Garage", type = "freezer" }
        );

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = (await response.Content.ReadFromJsonAsync<ContainerResponse>())!;
        Assert.EndsWith(Url(fridge, created.Id), response.Headers.Location!.AbsolutePath);
        Assert.Equal(
            ("Garage", "freezer", fridge.Id),
            (created.Name, created.Type, created.FridgeId)
        );

        var list = await (
            await SendAsync(anna, HttpMethod.Get, Url(fridge))
        ).Content.ReadFromJsonAsync<List<ContainerResponse>>();
        Assert.Equal(created.Id, Assert.Single(list!).Id);

        var renamed = await SendAsync(
            anna,
            HttpMethod.Put,
            Url(fridge, created.Id),
            new { name = "Cellar", type = "pantry" }
        );
        Assert.Equal(HttpStatusCode.OK, renamed.StatusCode);
        var body = (await renamed.Content.ReadFromJsonAsync<ContainerResponse>())!;
        Assert.Equal(("Cellar", "pantry"), (body.Name, body.Type));

        Assert.Equal(
            HttpStatusCode.NoContent,
            (await SendAsync(anna, HttpMethod.Delete, Url(fridge, created.Id))).StatusCode
        );
        await AssertProblemAsync(
            await SendAsync(anna, HttpMethod.Get, Url(fridge, created.Id)),
            StatusCodes.Status404NotFound
        );
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

    [Fact]
    public async Task Stranger_GetsNotFound_ForEveryAction()
    {
        var anna = await RegisterAsync("Anna");
        var cleo = await RegisterAsync("Cleo");
        var fridge = await CreateFridgeAsync(anna);
        var container = await CreateContainerAsync(anna, fridge);
        var body = new { name = "Mine now", type = "fridge" };

        var responses = new[]
        {
            await SendAsync(cleo, HttpMethod.Get, Url(fridge)),
            await SendAsync(cleo, HttpMethod.Get, Url(fridge, container.Id)),
            await SendAsync(cleo, HttpMethod.Post, Url(fridge), body),
            await SendAsync(cleo, HttpMethod.Put, Url(fridge, container.Id), body),
            await SendAsync(cleo, HttpMethod.Delete, Url(fridge, container.Id)),
        };

        foreach (var response in responses)
            await AssertProblemAsync(response, StatusCodes.Status404NotFound);
        Assert.Single(
            (
                await (
                    await SendAsync(anna, HttpMethod.Get, Url(fridge))
                ).Content.ReadFromJsonAsync<List<ContainerResponse>>()
            )!
        );
    }

    [Fact]
    public async Task MemberWhoseAccessExpired_GetsNotFound()
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

        await AssertProblemAsync(
            await SendAsync(ben, HttpMethod.Get, Url(fridge)),
            StatusCodes.Status404NotFound
        );
    }

    [Fact]
    public async Task MemberWhoMayPut_ChangesContainers()
    {
        var anna = await RegisterAsync("Anna");
        var ben = await RegisterAsync("Ben");
        var fridge = await CreateFridgeAsync(anna);
        await AddMemberAsync(fridge, ben, canPut: true);

        var created = await CreateContainerAsync(ben, fridge, "Bens shelf");
        var updated = await SendAsync(
            ben,
            HttpMethod.Put,
            Url(fridge, created.Id),
            new { name = "Renamed", type = "pantry" }
        );
        var deleted = await SendAsync(ben, HttpMethod.Delete, Url(fridge, created.Id));

        Assert.Equal(HttpStatusCode.OK, updated.StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, deleted.StatusCode);
    }

    [Fact]
    public async Task MemberWhoMayNotPut_CanReadContainers_ButNotChangeThem()
    {
        var anna = await RegisterAsync("Anna");
        var ben = await RegisterAsync("Ben");
        var fridge = await CreateFridgeAsync(anna);
        await AddMemberAsync(fridge, ben, canPut: false);
        var container = await CreateContainerAsync(anna, fridge);
        var body = new { name = "Nope", type = "fridge" };

        Assert.Equal(
            HttpStatusCode.OK,
            (await SendAsync(ben, HttpMethod.Get, Url(fridge))).StatusCode
        );
        Assert.Equal(
            HttpStatusCode.OK,
            (await SendAsync(ben, HttpMethod.Get, Url(fridge, container.Id))).StatusCode
        );
        var forbidden = new[]
        {
            await SendAsync(ben, HttpMethod.Post, Url(fridge), body),
            await SendAsync(ben, HttpMethod.Put, Url(fridge, container.Id), body),
            await SendAsync(ben, HttpMethod.Delete, Url(fridge, container.Id)),
        };

        foreach (var response in forbidden)
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Equal(
            "Top shelf",
            (
                await (
                    await SendAsync(anna, HttpMethod.Get, Url(fridge, container.Id))
                ).Content.ReadFromJsonAsync<ContainerResponse>()
            )!.Name
        );
    }

    [Fact]
    public async Task ContainerOfAnotherFridge_IsNotFound_EvenForAMemberOfBoth()
    {
        var anna = await RegisterAsync("Anna");
        var home = await CreateFridgeAsync(anna);
        var cabin = await CreateFridgeAsync(anna);
        var container = await CreateContainerAsync(anna, home);

        await AssertProblemAsync(
            await SendAsync(anna, HttpMethod.Get, Url(cabin, container.Id)),
            StatusCodes.Status404NotFound
        );
        await AssertProblemAsync(
            await SendAsync(
                anna,
                HttpMethod.Put,
                Url(cabin, container.Id),
                new { name = "Moved", type = "fridge" }
            ),
            StatusCodes.Status404NotFound
        );
        await AssertProblemAsync(
            await SendAsync(anna, HttpMethod.Delete, Url(cabin, container.Id)),
            StatusCodes.Status404NotFound
        );
    }

    [Fact]
    public async Task CreatingBeyondTheLimit_Returns409Problem_UntilOneIsDeleted()
    {
        var anna = await RegisterAsync("Anna");
        var fridge = await CreateFridgeAsync(anna);
        ContainerResponse? last = null;
        for (var i = 0; i < fridge.MaxContainers; i++)
            last = await CreateContainerAsync(anna, fridge, $"Container {i}");

        var tooMany = await SendAsync(
            anna,
            HttpMethod.Post,
            Url(fridge),
            new { name = "One too many", type = "fridge" }
        );

        var problem = await AssertProblemAsync(tooMany, StatusCodes.Status409Conflict);
        Assert.Contains("10", problem.Detail);
        await SendAsync(anna, HttpMethod.Delete, Url(fridge, last!.Id));
        await CreateContainerAsync(anna, fridge, "Fits again");
    }

    [Theory]
    [InlineData("", "fridge")]
    [InlineData("Shelf", "")]
    [InlineData("Shelf", "cupboard")]
    public async Task InvalidRequests_Return400(string name, string type)
    {
        var anna = await RegisterAsync("Anna");
        var fridge = await CreateFridgeAsync(anna);
        var container = await CreateContainerAsync(anna, fridge);

        var create = await SendAsync(anna, HttpMethod.Post, Url(fridge), new { name, type });
        var update = await SendAsync(
            anna,
            HttpMethod.Put,
            Url(fridge, container.Id),
            new { name, type }
        );

        Assert.Equal(HttpStatusCode.BadRequest, create.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, update.StatusCode);
    }

    [Fact]
    public async Task DeletingTheFridge_TakesItsContainersWithIt()
    {
        var anna = await RegisterAsync("Anna");
        var fridge = await CreateFridgeAsync(anna);
        await CreateContainerAsync(anna, fridge);

        await SendAsync(anna, HttpMethod.Delete, $"/api/fridges/{fridge.Id}");

        await AssertProblemAsync(
            await SendAsync(anna, HttpMethod.Get, Url(fridge)),
            StatusCodes.Status404NotFound
        );
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        Assert.Empty(await db.Containers.ToListAsync());
    }
}
