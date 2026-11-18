using DataAccess.Entities;
using DataAccess.Models.Item;
using DataAccess.Repositories;
using Microsoft.EntityFrameworkCore;
using Tests.Database;

namespace Tests.DataAccess;

public class ItemRepositoryTests(PostgresFixture database) : DatabaseTest(database)
{
    private ItemRepository Repository => new(Database.CreateDbContext());

    private async Task<(User Owner, Fridge Fridge, Container Container)> AddFridgeAsync(
        string code = "CODE0001"
    )
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
            CreatedByUserId = user.Id,
        };
        db.Fridges.Add(fridge);
        await db.SaveChangesAsync();
        var container = await AddContainerAsync(fridge, "Top shelf");
        return (user, fridge, container);
    }

    private async Task<Container> AddContainerAsync(Fridge fridge, string name)
    {
        await using var db = Database.CreateDbContext();
        var container = new Container
        {
            FridgeId = fridge.Id,
            Name = name,
            ContainerType = ContainerType.Fridge,
        };
        db.Containers.Add(container);
        await db.SaveChangesAsync();
        return container;
    }

    private static Item NewItem(
        Container container,
        User user,
        string name = "Milk",
        int expiresInDays = 5
    ) =>
        new()
        {
            ContainerId = container.Id,
            Name = name,
            AddedByUserId = user.Id,
            AddedAt = new DateOnly(2030, 1, 1),
            ExpiryDate = new DateOnly(2030, 1, 1).AddDays(expiresInDays),
        };

    [Fact]
    public async Task Add_SavesTheItem_WithDatabaseDefaults()
    {
        var (owner, _, container) = await AddFridgeAsync();
        var item = NewItem(container, owner);
        item.Amount = "2";
        item.Unit = "l";
        item.Comment = "Organic";

        await Repository.AddAsync(item);

        Assert.NotEqual(Guid.Empty, item.Id);
        await using var db = Database.CreateDbContext();
        var stored = await db.Items.SingleAsync();
        Assert.Equal(
            ("Milk", "2", "l", "Organic"),
            (stored.Name, stored.Amount, stored.Unit, stored.Comment)
        );
        Assert.Equal(ItemStatus.Active, stored.Status);
        Assert.Equal(new DateOnly(2030, 1, 6), stored.ExpiryDate);
        Assert.NotEqual(default, stored.CreatedAt);
    }

    [Fact]
    public async Task GetForFridge_ListsThatFridgesItemsWithTheStatus_SoonestToExpireFirst()
    {
        var (owner, home, shelf) = await AddFridgeAsync();
        var (_, _, elsewhere) = await AddFridgeAsync("CODE0002");
        var drawer = await AddContainerAsync(home, "Drawer");
        await Repository.AddAsync(NewItem(shelf, owner, "Later", 9));
        await Repository.AddAsync(NewItem(shelf, owner, "Sooner", 2));
        await Repository.AddAsync(NewItem(drawer, owner, "Middle", 5));
        await Repository.AddAsync(NewItem(elsewhere, owner, "Not here", 1));
        var eaten = NewItem(shelf, owner, "Eaten", 1);
        eaten.Status = ItemStatus.Consumed;
        await Repository.AddAsync(eaten);

        var active = await Repository.GetForFridgeAsync(home.Id, ItemStatus.Active);
        var inDrawer = await Repository.GetForFridgeAsync(home.Id, ItemStatus.Active, drawer.Id);
        var consumed = await Repository.GetForFridgeAsync(home.Id, ItemStatus.Consumed);

        Assert.Equal(["Sooner", "Middle", "Later"], active.Select(i => i.Name));
        Assert.Equal(["Middle"], inDrawer.Select(i => i.Name));
        Assert.Equal(["Eaten"], consumed.Select(i => i.Name));
        Assert.Empty(await Repository.GetForFridgeAsync(home.Id, ItemStatus.Removed));
    }

    [Fact]
    public async Task Get_ItemOfAnotherFridge_ReturnsNull()
    {
        var (owner, home, shelf) = await AddFridgeAsync();
        var (_, cabin, _) = await AddFridgeAsync("CODE0002");
        var item = NewItem(shelf, owner);
        await Repository.AddAsync(item);

        Assert.NotNull(await Repository.GetAsync(home.Id, item.Id));
        Assert.Null(await Repository.GetAsync(cabin.Id, item.Id));
        Assert.Null(await Repository.GetAsync(home.Id, Guid.NewGuid()));
    }

    [Fact]
    public async Task Update_ReplacesTheDetails_OnlyInTheItemsOwnFridge()
    {
        var (owner, home, shelf) = await AddFridgeAsync();
        var (_, cabin, _) = await AddFridgeAsync("CODE0002");
        var drawer = await AddContainerAsync(home, "Drawer");
        var item = NewItem(shelf, owner);
        await Repository.AddAsync(item);
        var changes = new ItemChanges(
            drawer.Id,
            "Oat milk",
            null,
            "1",
            "l",
            "https://example.com/oat.png",
            new DateOnly(2031, 2, 3),
            "Barista"
        );

        Assert.True(await Repository.UpdateAsync(home.Id, item.Id, changes));
        Assert.False(
            await Repository.UpdateAsync(cabin.Id, item.Id, changes with { Name = "Hijacked" })
        );
        Assert.False(await Repository.UpdateAsync(home.Id, Guid.NewGuid(), changes));

        var stored = (await Repository.GetAsync(home.Id, item.Id))!;
        Assert.Equal(drawer.Id, stored.ContainerId);
        Assert.Equal(("Oat milk", "1", "l"), (stored.Name, stored.Amount, stored.Unit));
        Assert.Equal("https://example.com/oat.png", stored.ImageUrl);
        Assert.Equal(new DateOnly(2031, 2, 3), stored.ExpiryDate);
        Assert.Equal("Barista", stored.Comment);
        Assert.Equal(owner.Id, stored.AddedByUserId);
        Assert.Equal(ItemStatus.Active, stored.Status);
    }

    [Fact]
    public async Task SetStatus_ChangesTheStatus_OnlyInTheItemsOwnFridge()
    {
        var (owner, home, shelf) = await AddFridgeAsync();
        var (_, cabin, _) = await AddFridgeAsync("CODE0002");
        var item = NewItem(shelf, owner);
        await Repository.AddAsync(item);

        Assert.False(await Repository.SetStatusAsync(cabin.Id, item.Id, ItemStatus.Consumed));
        Assert.False(await Repository.SetStatusAsync(home.Id, Guid.NewGuid(), ItemStatus.Consumed));
        Assert.True(await Repository.SetStatusAsync(home.Id, item.Id, ItemStatus.Consumed));

        Assert.Equal(ItemStatus.Consumed, (await Repository.GetAsync(home.Id, item.Id))!.Status);
    }

    [Fact]
    public async Task Delete_HidesTheItem_ButKeepsTheRow_AndCannotBeRepeated()
    {
        var (owner, home, shelf) = await AddFridgeAsync();
        var (_, cabin, _) = await AddFridgeAsync("CODE0002");
        var item = NewItem(shelf, owner);
        await Repository.AddAsync(item);

        Assert.False(await Repository.DeleteAsync(cabin.Id, item.Id, DateTimeOffset.UtcNow));
        Assert.True(await Repository.DeleteAsync(home.Id, item.Id, DateTimeOffset.UtcNow));
        Assert.False(await Repository.DeleteAsync(home.Id, item.Id, DateTimeOffset.UtcNow));

        Assert.Null(await Repository.GetAsync(home.Id, item.Id));
        Assert.Empty(await Repository.GetForFridgeAsync(home.Id, ItemStatus.Active));
        await using var db = Database.CreateDbContext();
        Assert.Equal(1, await db.Items.IgnoreQueryFilters().CountAsync(i => i.DeletedAt != null));
    }

    [Fact]
    public async Task ItemsOfADeletedContainer_AreHidden()
    {
        var (owner, home, shelf) = await AddFridgeAsync();
        var item = NewItem(shelf, owner);
        await Repository.AddAsync(item);
        await using (var db = Database.CreateDbContext())
            await db.Containers.ExecuteUpdateAsync(s =>
                s.SetProperty(c => c.DeletedAt, DateTimeOffset.UtcNow)
            );

        Assert.Null(await Repository.GetAsync(home.Id, item.Id));
        Assert.Empty(await Repository.GetForFridgeAsync(home.Id, ItemStatus.Active));
        Assert.False(await Repository.SetStatusAsync(home.Id, item.Id, ItemStatus.Removed));
    }
}
