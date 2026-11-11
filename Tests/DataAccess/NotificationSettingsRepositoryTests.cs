using DataAccess.Entities;
using DataAccess.Repositories;
using Microsoft.EntityFrameworkCore;
using Tests.Database;

namespace Tests.DataAccess;

public class NotificationSettingsRepositoryTests(PostgresFixture database) : DatabaseTest(database)
{
    private NotificationSettingsRepository Repository => new(Database.CreateDbContext());

    private async Task<(Fridge Fridge, User Owner)> AddFridgeAsync()
    {
        await using var db = Database.CreateDbContext();
        var owner = await AddUserAsync(db, "Anna");
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
        await db.SaveChangesAsync();
        return (fridge, owner);
    }

    private async Task<User> AddMemberAsync(Fridge fridge, string name = "Ben")
    {
        await using var db = Database.CreateDbContext();
        var user = await AddUserAsync(db, name);
        db.FridgeMembers.Add(new FridgeMember { FridgeId = fridge.Id, UserId = user.Id });
        await db.SaveChangesAsync();
        return user;
    }

    private static async Task<User> AddUserAsync(global::DataAccess.AppDbContext db, string name)
    {
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

    [Fact]
    public async Task Get_ForAFridgeThatNeverSavedSettings_IsNull()
    {
        var (fridge, _) = await AddFridgeAsync();

        Assert.Null(await Repository.GetAsync(fridge.Id));
    }

    [Fact]
    public async Task Save_CreatesTheSettings_WithDatabaseDefaultsForTheTimestamps()
    {
        var (fridge, _) = await AddFridgeAsync();

        await Repository.SaveAsync(fridge.Id, 5, 2, RecipientsScope.AdderAndOwners, [], DateTimeOffset.UtcNow);

        var stored = (await Repository.GetAsync(fridge.Id))!;
        Assert.NotEqual(Guid.Empty, stored.Settings.Id);
        Assert.Equal(5, stored.Settings.WarningDaysBefore);
        Assert.Equal(2, stored.Settings.DangerDaysBefore);
        Assert.Equal(RecipientsScope.AdderAndOwners, stored.Settings.RecipientsScope);
        Assert.NotEqual(default, stored.Settings.CreatedAt);
        Assert.Empty(stored.RecipientUserIds);
    }

    [Fact]
    public async Task Save_ASecondTime_ReplacesTheSettingsInsteadOfAddingARow()
    {
        var (fridge, _) = await AddFridgeAsync();
        await Repository.SaveAsync(fridge.Id, 5, 2, RecipientsScope.AllMembers, [], DateTimeOffset.UtcNow);

        await Repository.SaveAsync(fridge.Id, 10, 3, RecipientsScope.AdderAndOwners, [], DateTimeOffset.UtcNow);

        var stored = (await Repository.GetAsync(fridge.Id))!;
        Assert.Equal(10, stored.Settings.WarningDaysBefore);
        Assert.Equal(3, stored.Settings.DangerDaysBefore);
        Assert.Equal(RecipientsScope.AdderAndOwners, stored.Settings.RecipientsScope);
        await using var db = Database.CreateDbContext();
        Assert.Single(db.NotificationSettings.Where(s => s.FridgeId == fridge.Id));
    }

    [Fact]
    public async Task Save_StoresTheRecipients_AndSoftDeletesTheOnesLeftOut()
    {
        var (fridge, owner) = await AddFridgeAsync();
        var ben = await AddMemberAsync(fridge);
        await Repository.SaveAsync(fridge.Id, 6, 1, RecipientsScope.Custom, [owner.Id, ben.Id], DateTimeOffset.UtcNow);

        await Repository.SaveAsync(fridge.Id, 6, 1, RecipientsScope.Custom, [ben.Id], DateTimeOffset.UtcNow);

        var stored = (await Repository.GetAsync(fridge.Id))!;
        Assert.Equal([ben.Id], stored.RecipientUserIds);
        await using var db = Database.CreateDbContext();
        Assert.NotNull(
            db.NotificationRecipients.IgnoreQueryFilters()
                .Single(r => r.UserId == owner.Id)
                .DeletedAt
        );
    }

    [Fact]
    public async Task Save_APreviouslyRemovedRecipientAgain_AddsThemBack()
    {
        var (fridge, owner) = await AddFridgeAsync();
        await Repository.SaveAsync(fridge.Id, 6, 1, RecipientsScope.Custom, [owner.Id], DateTimeOffset.UtcNow);
        await Repository.SaveAsync(fridge.Id, 6, 1, RecipientsScope.AllMembers, [], DateTimeOffset.UtcNow);

        await Repository.SaveAsync(fridge.Id, 6, 1, RecipientsScope.Custom, [owner.Id], DateTimeOffset.UtcNow);

        Assert.Equal([owner.Id], (await Repository.GetAsync(fridge.Id))!.RecipientUserIds);
    }

    [Fact]
    public async Task Get_LeavesOutRecipientsWhoNoLongerBelongToTheFridge()
    {
        var (fridge, owner) = await AddFridgeAsync();
        var ben = await AddMemberAsync(fridge);
        await Repository.SaveAsync(fridge.Id, 6, 1, RecipientsScope.Custom, [owner.Id, ben.Id], DateTimeOffset.UtcNow);
        await using (var db = Database.CreateDbContext())
        {
            var member = db.FridgeMembers.Single(m => m.UserId == ben.Id);
            member.DeletedAt = DateTimeOffset.UtcNow;
            await db.SaveChangesAsync();
        }

        Assert.Equal([owner.Id], (await Repository.GetAsync(fridge.Id))!.RecipientUserIds);
    }

    [Fact]
    public async Task DeletingTheFridge_RemovesItsSettingsAndRecipients()
    {
        var (fridge, owner) = await AddFridgeAsync();
        await Repository.SaveAsync(fridge.Id, 6, 1, RecipientsScope.Custom, [owner.Id], DateTimeOffset.UtcNow);

        await new FridgeRepository(Database.CreateDbContext(), TimeProvider.System)
            .DeleteAsync(fridge.Id, DateTimeOffset.UtcNow);

        Assert.Null(await Repository.GetAsync(fridge.Id));
    }
}
