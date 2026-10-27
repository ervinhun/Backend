using BusinessLogic.Dtos.Container;
using BusinessLogic.Exceptions;
using BusinessLogic.Services;
using DataAccess.Entities;
using DataAccess.Repositories;
using Microsoft.EntityFrameworkCore;
using Tests.Database;

namespace Tests.BusinessLogic;

/// <summary>ContainerService against the real repository and Postgres.</summary>
public class ContainerServiceTests(PostgresFixture database) : DatabaseTest(database)
{
    private ContainerService Service =>
        new(new ContainerRepository(Database.CreateDbContext()), TimeProvider.System);

    private async Task<Fridge> AddFridgeAsync(int maxContainers = 10, string code = "CODE0001")
    {
        await using var db = Database.CreateDbContext();
        var user = new User
        {
            Email = $"{Guid.NewGuid():N}@example.com",
            PasswordHash = "hash",
            DefaultDisplayName = "Anna",
        };
        db.Users.Add(user);
        await db.SaveChangesAsync();
        var fridge = new Fridge
        {
            Name = "Home",
            InviteCode = code,
            MaxContainers = maxContainers,
            CreatedByUserId = user.Id,
        };
        db.Fridges.Add(fridge);
        await db.SaveChangesAsync();
        return fridge;
    }

    private static SaveContainerRequest Save(string name, string type = "fridge") =>
        new() { Name = name, Type = type };

    [Fact]
    public async Task Create_TrimsTheName_AndAnswersWithTheContainer()
    {
        var fridge = await AddFridgeAsync();

        var container = await Service.CreateAsync(fridge.Id, Save("  Garage  ", "freezer"));

        Assert.NotEqual(Guid.Empty, container.Id);
        Assert.Equal(fridge.Id, container.FridgeId);
        Assert.Equal("Garage", container.Name);
        Assert.Equal("freezer", container.Type);
        Assert.NotEqual(default, container.CreatedAt);
        Assert.Equal(container.Id, (await Service.GetAsync(fridge.Id, container.Id)).Id);
    }

    [Theory]
    [InlineData("fridge")]
    [InlineData("Freezer")]
    [InlineData("PANTRY")]
    public async Task Create_AcceptsTheTypeInAnyCase(string type)
    {
        var fridge = await AddFridgeAsync();

        var container = await Service.CreateAsync(fridge.Id, Save("Somewhere", type));

        Assert.Equal(type.ToLowerInvariant(), container.Type);
    }

    [Fact]
    public async Task Create_BeyondTheFridgesLimit_ThrowsConflict_UntilOneIsDeleted()
    {
        var fridge = await AddFridgeAsync(maxContainers: 2);
        await Service.CreateAsync(fridge.Id, Save("One"));
        var second = await Service.CreateAsync(fridge.Id, Save("Two"));

        var error = await Assert.ThrowsAsync<ConflictException>(() =>
            Service.CreateAsync(fridge.Id, Save("Three"))
        );
        await Service.DeleteAsync(fridge.Id, second.Id);
        await Service.CreateAsync(fridge.Id, Save("Three"));

        Assert.Equal("This fridge has reached its limit of 2 containers.", error.Message);
        Assert.Equal(
            ["One", "Three"],
            (await Service.GetForFridgeAsync(fridge.Id)).Select(c => c.Name)
        );
    }

    [Fact]
    public async Task Create_ForAFridgeThatDoesNotExist_ThrowsNotFound()
    {
        var error = await Assert.ThrowsAsync<NotFoundException>(() =>
            Service.CreateAsync(Guid.NewGuid(), Save("Nowhere"))
        );

        Assert.Equal("Fridge not found.", error.Message);
    }

    [Fact]
    public async Task GetForFridge_ListsTheContainersInTheOrderTheyWereAdded()
    {
        var fridge = await AddFridgeAsync();
        var other = await AddFridgeAsync(code: "CODE0002");
        await Service.CreateAsync(fridge.Id, Save("First"));
        await Service.CreateAsync(other.Id, Save("Elsewhere"));
        await Service.CreateAsync(fridge.Id, Save("Second", "pantry"));

        var containers = await Service.GetForFridgeAsync(fridge.Id);

        Assert.Equal(["First", "Second"], containers.Select(c => c.Name));
        Assert.Equal(["fridge", "pantry"], containers.Select(c => c.Type));
    }

    [Fact]
    public async Task Get_UnknownContainer_OrOneOfAnotherFridge_ThrowsNotFound()
    {
        var fridge = await AddFridgeAsync();
        var other = await AddFridgeAsync(code: "CODE0002");
        var container = await Service.CreateAsync(fridge.Id, Save("Mine"));

        var unknown = await Assert.ThrowsAsync<NotFoundException>(() =>
            Service.GetAsync(fridge.Id, Guid.NewGuid())
        );
        await Assert.ThrowsAsync<NotFoundException>(() => Service.GetAsync(other.Id, container.Id));

        Assert.Equal("Container not found.", unknown.Message);
    }

    [Fact]
    public async Task Update_ChangesNameAndType()
    {
        var fridge = await AddFridgeAsync();
        var container = await Service.CreateAsync(fridge.Id, Save("Old"));

        var updated = await Service.UpdateAsync(
            fridge.Id,
            container.Id,
            Save(" Cellar ", "pantry")
        );

        Assert.Equal("Cellar", updated.Name);
        Assert.Equal("pantry", updated.Type);
        Assert.Equal(container.CreatedAt, updated.CreatedAt);
    }

    [Fact]
    public async Task Update_UnknownContainer_ThrowsNotFound()
    {
        var fridge = await AddFridgeAsync();

        var error = await Assert.ThrowsAsync<NotFoundException>(() =>
            Service.UpdateAsync(fridge.Id, Guid.NewGuid(), Save("Nope"))
        );

        Assert.Equal("Container not found.", error.Message);
    }

    [Fact]
    public async Task Delete_RemovesTheContainer_AndCannotBeRepeated()
    {
        var fridge = await AddFridgeAsync();
        var container = await Service.CreateAsync(fridge.Id, Save("Gone soon"));

        await Service.DeleteAsync(fridge.Id, container.Id);

        Assert.Empty(await Service.GetForFridgeAsync(fridge.Id));
        var error = await Assert.ThrowsAsync<NotFoundException>(() =>
            Service.DeleteAsync(fridge.Id, container.Id)
        );
        Assert.Equal("Container not found.", error.Message);
        await using var db = Database.CreateDbContext();
        var row = await db.Containers.IgnoreQueryFilters().SingleAsync();
        Assert.NotNull(row.DeletedAt);
    }
}
