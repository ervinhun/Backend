using BusinessLogic.Exceptions;
using BusinessLogic.Services;
using DataAccess.Entities;
using DataAccess.Repositories;
using Tests.Database;

namespace Tests.BusinessLogic;

/// <summary>NotificationItemsService against the real repositories and Postgres.</summary>
public class NotificationItemsServiceTests(PostgresFixture database) : DatabaseTest(database)
{
    private NotificationItemsService Service =>
        new(
            new NotificationSettingsRepository(Database.CreateDbContext()),
            new ItemRepository(Database.CreateDbContext()),
            new FridgeRepository(Database.CreateDbContext(), TimeProvider.System),
            TimeProvider.System
        );

    private async Task<User> AddUserAsync(string name)
    {
        await using var db = Database.CreateDbContext();
        var user = new User
        {
            Email = $"{Guid.NewGuid():N}@example.com",
            PasswordHash = "hash",
            DefaultDisplayName = name,
        };
        db.Users.Add(user);
        await db.SaveChangesAsync();
        return user;
    }

    private async Task<(Fridge Fridge, User Owner, Container Container)> AddFridgeAsync()
    {
        var owner = await AddUserAsync("Anna");
        await using var db = Database.CreateDbContext();
        var fridge = new Fridge
        {
            Name = "Home",
            InviteCode = "CODE0001",
            CreatedByUserId = owner.Id,
        };
        db.Fridges.Add(fridge);
        await db.SaveChangesAsync();
        db.FridgeMembers.Add(
            new FridgeMember
            {
                FridgeId = fridge.Id,
                UserId = owner.Id,
                IsOwner = true,
            }
        );
        var container = new Container
        {
            FridgeId = fridge.Id,
            Name = "Shelf",
            ContainerType = ContainerType.Fridge,
        };
        db.Containers.Add(container);
        await db.SaveChangesAsync();
        return (fridge, owner, container);
    }

    private async Task<User> AddMemberAsync(Fridge fridge)
    {
        var user = await AddUserAsync("Ben");
        await using var db = Database.CreateDbContext();
        db.FridgeMembers.Add(new FridgeMember { FridgeId = fridge.Id, UserId = user.Id });
        await db.SaveChangesAsync();
        return user;
    }

    private async Task<Item> AddItemAsync(
        Container container,
        User addedBy,
        int shelfLifeDays = 20,
        ItemStatus status = ItemStatus.Active
    )
    {
        await using var db = Database.CreateDbContext();
        var item = new Item
        {
            ContainerId = container.Id,
            Name = "Milk",
            AddedByUserId = addedBy.Id,
            AddedAt = new DateOnly(2030, 1, 1),
            ExpiryDate = new DateOnly(2030, 1, 1).AddDays(shelfLifeDays),
            Status = status,
        };
        db.Items.Add(item);
        await db.SaveChangesAsync();
        return item;
    }

    private async Task SaveSettingsAsync(
        Fridge fridge,
        RecipientsScope scope,
        params Guid[] recipients
    )
    {
        var repository = new NotificationSettingsRepository(Database.CreateDbContext());
        await repository.SaveAsync(fridge.Id, 7, 2, scope, recipients, DateTimeOffset.UtcNow);
    }

    [Fact]
    public async Task BeforeAnythingWasSaved_AnswersWithTheDefaults_AndAllActiveItems()
    {
        var (fridge, owner, container) = await AddFridgeAsync();
        var milk = await AddItemAsync(container, owner);
        await AddItemAsync(container, owner, status: ItemStatus.Consumed);

        var response = await Service.GetAsync(fridge.Id, owner.Id);

        Assert.Equal(7, response.WarningDaysBefore);
        Assert.Equal(2, response.DangerDaysBefore);
        Assert.Equal(new TimeOnly(16, 0), response.NotificationTime);
        Assert.Equal([milk.Id], response.Items.Select(i => i.Id));
        Assert.Equal(milk.ExpiryDate, response.Items.Single().ExpiryDate);
    }

    [Theory]
    [InlineData(3, true)]
    [InlineData(7, true)]
    [InlineData(8, false)]
    public async Task SkipWarning_WhenTheShelfLifeIsNoLongerThanTheWarningPeriod(
        int shelfLifeDays,
        bool skip
    )
    {
        var (fridge, owner, container) = await AddFridgeAsync();
        await AddItemAsync(container, owner, shelfLifeDays);

        var response = await Service.GetAsync(fridge.Id, owner.Id);

        Assert.Equal(skip, response.Items.Single().SkipWarning);
    }

    [Fact]
    public async Task AdderAndOwners_GivesAMemberOnlyTheirOwnItems_AndTheOwnerAll()
    {
        var (fridge, owner, container) = await AddFridgeAsync();
        var ben = await AddMemberAsync(fridge);
        var annas = await AddItemAsync(container, owner);
        var bens = await AddItemAsync(container, ben);
        await SaveSettingsAsync(fridge, RecipientsScope.AdderAndOwners);

        Assert.Equal([bens.Id], (await Service.GetAsync(fridge.Id, ben.Id)).Items.Select(i => i.Id));
        Assert.Equal(
            [annas.Id, bens.Id],
            (await Service.GetAsync(fridge.Id, owner.Id)).Items.Select(i => i.Id).Order()
        );
    }

    [Fact]
    public async Task Custom_GivesItemsOnlyToThePickedMembers()
    {
        var (fridge, owner, container) = await AddFridgeAsync();
        var ben = await AddMemberAsync(fridge);
        await AddItemAsync(container, owner);
        await SaveSettingsAsync(fridge, RecipientsScope.Custom, ben.Id);

        Assert.Single((await Service.GetAsync(fridge.Id, ben.Id)).Items);
        Assert.Empty((await Service.GetAsync(fridge.Id, owner.Id)).Items);
    }

    [Fact]
    public async Task ANonMember_GetsNotFound()
    {
        var (fridge, _, _) = await AddFridgeAsync();
        var stranger = await AddUserAsync("Eve");

        await Assert.ThrowsAsync<NotFoundException>(() => Service.GetAsync(fridge.Id, stranger.Id));
    }
}
