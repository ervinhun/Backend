using DataAccess.Entities;
using DataAccess.Interfaces;
using DataAccess.Repositories;
using Microsoft.EntityFrameworkCore;
using Tests.Database;

namespace Tests.DataAccess;

public class ContainerRepositoryTests(PostgresFixture database) : DatabaseTest(database)
{
    private ContainerRepository Repository => new(Database.CreateDbContext());

    private async Task<User> AddUserAsync(string email = "anna@example.com")
    {
        await using var db = Database.CreateDbContext();
        var user = new User
        {
            Email = email,
            PasswordHash = "hash",
            DefaultDisplayName = "Anna",
        };
        db.Users.Add(user);
        await db.SaveChangesAsync();
        return user;
    }

    private async Task<Fridge> AddFridgeAsync(
        User owner,
        int maxContainers = 3,
        string code = "CODE0001"
    )
    {
        await using var db = Database.CreateDbContext();
        var fridge = new Fridge
        {
            Name = "Home",
            InviteCode = code,
            MaxContainers = maxContainers,
            CreatedByUserId = owner.Id,
        };
        db.Fridges.Add(fridge);
        await db.SaveChangesAsync();
        return fridge;
    }

    private static Container NewContainer(
        Guid fridgeId,
        string name = "Top shelf",
        ContainerType type = ContainerType.Fridge
    ) =>
        new()
        {
            FridgeId = fridgeId,
            Name = name,
            ContainerType = type,
        };

    [Fact]
    public async Task Add_SavesTheContainer_WithDatabaseDefaults()
    {
        var fridge = await AddFridgeAsync(await AddUserAsync());
        var container = NewContainer(fridge.Id, "Garage freezer", ContainerType.Freezer);

        var result = await Repository.AddAsync(container);

        Assert.Equal(new AddContainerResult(AddContainerStatus.Added, 3), result);
        var stored = (await Repository.GetAsync(fridge.Id, container.Id))!;
        Assert.NotEqual(Guid.Empty, stored.Id);
        Assert.Equal("Garage freezer", stored.Name);
        Assert.Equal(ContainerType.Freezer, stored.ContainerType);
        Assert.NotEqual(default, stored.CreatedAt);
    }

    [Theory]
    [InlineData(ContainerType.Fridge, "fridge")]
    [InlineData(ContainerType.Freezer, "freezer")]
    [InlineData(ContainerType.Pantry, "pantry")]
    public async Task ContainerType_IsStoredInLowerCase(ContainerType type, string column)
    {
        var fridge = await AddFridgeAsync(await AddUserAsync());
        await Repository.AddAsync(NewContainer(fridge.Id, type: type));

        await using var db = Database.CreateDbContext();
        var raw = await db
            .Database.SqlQuery<string>($"SELECT container_type AS \"Value\" FROM containers")
            .SingleAsync();
        Assert.Equal(column, raw);
    }

    [Fact]
    public async Task Add_AtTheFridgesLimit_ReturnsFull_AndSavesNothing()
    {
        var fridge = await AddFridgeAsync(await AddUserAsync(), maxContainers: 2);
        await Repository.AddAsync(NewContainer(fridge.Id, "One"));
        await Repository.AddAsync(NewContainer(fridge.Id, "Two"));

        var result = await Repository.AddAsync(NewContainer(fridge.Id, "Three"));

        Assert.Equal(new AddContainerResult(AddContainerStatus.Full, 2), result);
        Assert.Equal(
            ["One", "Two"],
            (await Repository.GetForFridgeAsync(fridge.Id)).Select(c => c.Name)
        );
    }

    [Fact]
    public async Task Add_WithALimitOfZero_AlwaysReturnsFull()
    {
        var fridge = await AddFridgeAsync(await AddUserAsync(), maxContainers: 0);

        var result = await Repository.AddAsync(NewContainer(fridge.Id));

        Assert.Equal(new AddContainerResult(AddContainerStatus.Full, 0), result);
    }

    [Fact]
    public async Task Add_LimitIsPerFridge_AndDeletingAContainerFreesItsSlot()
    {
        var anna = await AddUserAsync();
        var home = await AddFridgeAsync(anna, maxContainers: 1, code: "CODE0001");
        var cabin = await AddFridgeAsync(anna, maxContainers: 1, code: "CODE0002");
        var first = NewContainer(home.Id, "Home fridge");
        await Repository.AddAsync(first);

        Assert.Equal(
            AddContainerStatus.Added,
            (await Repository.AddAsync(NewContainer(cabin.Id, "Cabin fridge"))).Status
        );
        Assert.Equal(
            AddContainerStatus.Full,
            (await Repository.AddAsync(NewContainer(home.Id, "Another"))).Status
        );
        await Repository.DeleteAsync(home.Id, first.Id, DateTimeOffset.UtcNow);

        Assert.Equal(
            AddContainerStatus.Added,
            (await Repository.AddAsync(NewContainer(home.Id, "Another"))).Status
        );
    }

    [Fact]
    public async Task Add_UnknownOrDeletedFridge_ReturnsFridgeNotFound()
    {
        var fridge = await AddFridgeAsync(await AddUserAsync());
        await using (var db = Database.CreateDbContext())
            await db.Fridges.ExecuteUpdateAsync(s =>
                s.SetProperty(f => f.DeletedAt, DateTimeOffset.UtcNow)
            );

        Assert.Equal(
            new AddContainerResult(AddContainerStatus.FridgeNotFound, 0),
            await Repository.AddAsync(NewContainer(fridge.Id))
        );
        Assert.Equal(
            AddContainerStatus.FridgeNotFound,
            (await Repository.AddAsync(NewContainer(Guid.NewGuid()))).Status
        );
    }

    [Fact]
    public async Task Add_ManyAtOnce_NeverPassesTheLimit()
    {
        var fridge = await AddFridgeAsync(await AddUserAsync(), maxContainers: 3);

        var results = await Task.WhenAll(
            Enumerable
                .Range(0, 8)
                .Select(i => Repository.AddAsync(NewContainer(fridge.Id, $"C{i}")))
        );

        Assert.Equal(3, results.Count(r => r.Status == AddContainerStatus.Added));
        Assert.Equal(5, results.Count(r => r.Status == AddContainerStatus.Full));
        Assert.Equal(3, (await Repository.GetForFridgeAsync(fridge.Id)).Count);
    }

    [Fact]
    public async Task GetForFridge_ListsOnlyThatFridgesContainers_OldestFirst()
    {
        var anna = await AddUserAsync();
        var home = await AddFridgeAsync(anna, maxContainers: 5, code: "CODE0001");
        var cabin = await AddFridgeAsync(anna, maxContainers: 5, code: "CODE0002");
        await Repository.AddAsync(NewContainer(home.Id, "First"));
        await Repository.AddAsync(NewContainer(cabin.Id, "Elsewhere"));
        await Repository.AddAsync(NewContainer(home.Id, "Second"));

        var containers = await Repository.GetForFridgeAsync(home.Id);

        Assert.Equal(["First", "Second"], containers.Select(c => c.Name));
    }

    [Fact]
    public async Task Get_ContainerOfAnotherFridge_ReturnsNull()
    {
        var anna = await AddUserAsync();
        var home = await AddFridgeAsync(anna, code: "CODE0001");
        var cabin = await AddFridgeAsync(anna, code: "CODE0002");
        var container = NewContainer(home.Id);
        await Repository.AddAsync(container);

        Assert.NotNull(await Repository.GetAsync(home.Id, container.Id));
        Assert.Null(await Repository.GetAsync(cabin.Id, container.Id));
        Assert.Null(await Repository.GetAsync(home.Id, Guid.NewGuid()));
    }

    [Fact]
    public async Task Update_ChangesNameAndType_OnlyInTheContainersOwnFridge()
    {
        var anna = await AddUserAsync();
        var home = await AddFridgeAsync(anna, code: "CODE0001");
        var cabin = await AddFridgeAsync(anna, code: "CODE0002");
        var container = NewContainer(home.Id);
        await Repository.AddAsync(container);

        Assert.True(
            await Repository.UpdateAsync(home.Id, container.Id, "Cellar", ContainerType.Pantry)
        );
        Assert.False(
            await Repository.UpdateAsync(cabin.Id, container.Id, "Hijacked", ContainerType.Freezer)
        );
        Assert.False(
            await Repository.UpdateAsync(home.Id, Guid.NewGuid(), "Nope", ContainerType.Freezer)
        );

        var stored = (await Repository.GetAsync(home.Id, container.Id))!;
        Assert.Equal("Cellar", stored.Name);
        Assert.Equal(ContainerType.Pantry, stored.ContainerType);
    }

    [Fact]
    public async Task Delete_HidesTheContainer_ButKeepsTheRow_AndCannotBeRepeated()
    {
        var anna = await AddUserAsync();
        var home = await AddFridgeAsync(anna, code: "CODE0001");
        var cabin = await AddFridgeAsync(anna, code: "CODE0002");
        var container = NewContainer(home.Id);
        await Repository.AddAsync(container);

        Assert.False(await Repository.DeleteAsync(cabin.Id, container.Id, DateTimeOffset.UtcNow));
        Assert.True(await Repository.DeleteAsync(home.Id, container.Id, DateTimeOffset.UtcNow));
        Assert.False(await Repository.DeleteAsync(home.Id, container.Id, DateTimeOffset.UtcNow));

        Assert.Null(await Repository.GetAsync(home.Id, container.Id));
        Assert.Empty(await Repository.GetForFridgeAsync(home.Id));
        await using var db = Database.CreateDbContext();
        Assert.Equal(
            1,
            await db.Containers.IgnoreQueryFilters().CountAsync(c => c.DeletedAt != null)
        );
    }

    [Fact]
    public async Task DeletingTheFridge_DeletesItsContainersToo()
    {
        var anna = await AddUserAsync();
        var home = await AddFridgeAsync(anna, code: "CODE0001");
        var cabin = await AddFridgeAsync(anna, code: "CODE0002");
        await Repository.AddAsync(NewContainer(home.Id));
        await Repository.AddAsync(NewContainer(cabin.Id));
        var fridges = new FridgeRepository(Database.CreateDbContext(), TimeProvider.System);

        Assert.True(await fridges.DeleteAsync(home.Id, DateTimeOffset.UtcNow));

        Assert.Empty(await Repository.GetForFridgeAsync(home.Id));
        Assert.Single(await Repository.GetForFridgeAsync(cabin.Id));
        await using var db = Database.CreateDbContext();
        Assert.Equal(
            1,
            await db.Containers.IgnoreQueryFilters().CountAsync(c => c.DeletedAt != null)
        );
    }
}
