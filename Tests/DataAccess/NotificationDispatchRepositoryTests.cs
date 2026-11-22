using DataAccess.Entities;
using DataAccess.Repositories;
using Microsoft.EntityFrameworkCore;
using Tests.Database;

namespace Tests.DataAccess;

public class NotificationDispatchRepositoryTests(PostgresFixture database) : DatabaseTest(database)
{
    private NotificationDispatchRepository Repository => new(Database.CreateDbContext());

    private async Task<(Fridge Fridge, Item Item)> AddFridgeWithItemAsync(string code = "CODE0001")
    {
        await using var db = Database.CreateDbContext();
        var user = new User
        {
            Email = $"{code}@example.com",
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
            AddedByUserId = user.Id,
            AddedAt = new DateOnly(2030, 1, 1),
            ExpiryDate = new DateOnly(2030, 1, 6),
        };
        db.Items.Add(item);
        await db.SaveChangesAsync();
        return (fridge, item);
    }

    [Fact]
    public async Task Record_CreatesOneRowPerItemAndStage()
    {
        var (fridge, item) = await AddFridgeWithItemAsync();
        var sentAt = new DateTimeOffset(2030, 1, 3, 16, 0, 0, TimeSpan.Zero);

        Assert.True(
            await Repository.RecordAsync(
                fridge.Id,
                [(item.Id, NotificationStage.Warning), (item.Id, NotificationStage.Expiry)],
                sentAt
            )
        );

        await using var db = Database.CreateDbContext();
        var rows = await db.NotificationDispatches.ToListAsync();
        Assert.Equal(
            [NotificationStage.Warning, NotificationStage.Expiry],
            rows.Select(r => r.Stage).Order()
        );
        Assert.All(rows, r =>
        {
            Assert.True(r.Sent);
            Assert.Equal(1, r.DevicesNotifiedCount);
            Assert.Equal(sentAt, r.SentAt);
        });
    }

    [Fact]
    public async Task Record_TheSameStageAgain_AddsADevice_AndKeepsTheFirstSentAt()
    {
        var (fridge, item) = await AddFridgeWithItemAsync();
        var first = new DateTimeOffset(2030, 1, 3, 16, 0, 0, TimeSpan.Zero);
        await Repository.RecordAsync(fridge.Id, [(item.Id, NotificationStage.Warning)], first);

        await Repository.RecordAsync(
            fridge.Id,
            [(item.Id, NotificationStage.Warning)],
            first.AddMinutes(5)
        );

        await using var db = Database.CreateDbContext();
        var row = await db.NotificationDispatches.SingleAsync();
        Assert.Equal(2, row.DevicesNotifiedCount);
        Assert.Equal(first, row.SentAt);
    }

    [Fact]
    public async Task Record_AnItemFromAnotherFridge_SavesNothing()
    {
        var (fridge, item) = await AddFridgeWithItemAsync("CODE0001");
        var (_, foreign) = await AddFridgeWithItemAsync("CODE0002");

        Assert.False(
            await Repository.RecordAsync(
                fridge.Id,
                [(item.Id, NotificationStage.Warning), (foreign.Id, NotificationStage.Warning)],
                DateTimeOffset.UtcNow
            )
        );

        await using var db = Database.CreateDbContext();
        Assert.Empty(db.NotificationDispatches);
    }
}
