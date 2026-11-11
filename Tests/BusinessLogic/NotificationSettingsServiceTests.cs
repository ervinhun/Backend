using BusinessLogic.Dtos.Notification;
using BusinessLogic.Exceptions;
using BusinessLogic.Services;
using DataAccess.Entities;
using DataAccess.Repositories;
using Tests.Database;

namespace Tests.BusinessLogic;

/// <summary>NotificationSettingsService against the real repositories and Postgres.</summary>
public class NotificationSettingsServiceTests(PostgresFixture database) : DatabaseTest(database)
{
    private NotificationSettingsService Service =>
        new(
            new NotificationSettingsRepository(Database.CreateDbContext()),
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

    private async Task<Fridge> AddFridgeAsync()
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
        await db.SaveChangesAsync();
        return fridge;
    }

    private async Task<User> AddMemberAsync(Fridge fridge)
    {
        var user = await AddUserAsync("Ben");
        await using var db = Database.CreateDbContext();
        db.FridgeMembers.Add(new FridgeMember { FridgeId = fridge.Id, UserId = user.Id });
        await db.SaveChangesAsync();
        return user;
    }

    private static SaveNotificationSettingsRequest Save(
        string scope = "all_members",
        int warning = 6,
        int danger = 1,
        params Guid[] recipients
    ) =>
        new()
        {
            WarningDaysBefore = warning,
            DangerDaysBefore = danger,
            RecipientsScope = scope,
            RecipientUserIds = [.. recipients],
        };

    [Fact]
    public async Task Get_BeforeAnythingWasSaved_AnswersWithTheDefaults()
    {
        var fridge = await AddFridgeAsync();

        var settings = await Service.GetAsync(fridge.Id, fridge.CreatedByUserId);

        Assert.Equal(fridge.Id, settings.FridgeId);
        Assert.Equal(6, settings.WarningDaysBefore);
        Assert.Equal(1, settings.DangerDaysBefore);
        Assert.Equal("all_members", settings.RecipientsScope);
        Assert.Empty(settings.RecipientUserIds);
    }

    [Fact]
    public async Task Update_SavesTheSettings_AndEveryMemberCanReadThem()
    {
        var fridge = await AddFridgeAsync();
        var ben = await AddMemberAsync(fridge);

        var saved = await Service.UpdateAsync(
            fridge.Id,
            fridge.CreatedByUserId,
            Save("adder_and_owners", warning: 10, danger: 2)
        );

        Assert.Equal(10, saved.WarningDaysBefore);
        Assert.Equal(2, saved.DangerDaysBefore);
        Assert.Equal("adder_and_owners", saved.RecipientsScope);
        var seenByBen = await Service.GetAsync(fridge.Id, ben.Id);
        Assert.Equal(10, seenByBen.WarningDaysBefore);
    }

    [Fact]
    public async Task Update_WithACustomScope_StoresTheRecipients_WithoutDuplicates()
    {
        var fridge = await AddFridgeAsync();
        var ben = await AddMemberAsync(fridge);

        var saved = await Service.UpdateAsync(
            fridge.Id,
            fridge.CreatedByUserId,
            Save("custom", recipients: [ben.Id, ben.Id])
        );

        Assert.Equal("custom", saved.RecipientsScope);
        Assert.Equal([ben.Id], saved.RecipientUserIds);
    }

    [Fact]
    public async Task Update_SwitchingAwayFromCustom_ClearsTheRecipients()
    {
        var fridge = await AddFridgeAsync();
        var ben = await AddMemberAsync(fridge);
        await Service.UpdateAsync(fridge.Id, fridge.CreatedByUserId, Save("custom", recipients: ben.Id));

        var saved = await Service.UpdateAsync(
            fridge.Id,
            fridge.CreatedByUserId,
            Save("all_members", recipients: ben.Id)
        );

        Assert.Empty(saved.RecipientUserIds);
    }

    [Fact]
    public async Task Update_WithARecipientWhoIsNotInTheFridge_ThrowsConflict_AndSavesNothing()
    {
        var fridge = await AddFridgeAsync();
        var stranger = await AddUserAsync("Eve");

        await Assert.ThrowsAsync<ConflictException>(() =>
            Service.UpdateAsync(
                fridge.Id,
                fridge.CreatedByUserId,
                Save("custom", warning: 9, recipients: stranger.Id)
            )
        );

        Assert.Equal(
            6,
            (await Service.GetAsync(fridge.Id, fridge.CreatedByUserId)).WarningDaysBefore
        );
    }

    [Fact]
    public async Task Update_ByAMemberWhoIsNotTheOwner_ThrowsForbidden()
    {
        var fridge = await AddFridgeAsync();
        var ben = await AddMemberAsync(fridge);

        await Assert.ThrowsAsync<ForbiddenException>(() =>
            Service.UpdateAsync(fridge.Id, ben.Id, Save())
        );
    }

    [Fact]
    public async Task NonMembers_GetNotFound_ForReadingAndChanging()
    {
        var fridge = await AddFridgeAsync();
        var stranger = await AddUserAsync("Eve");

        await Assert.ThrowsAsync<NotFoundException>(() => Service.GetAsync(fridge.Id, stranger.Id));
        await Assert.ThrowsAsync<NotFoundException>(() =>
            Service.UpdateAsync(fridge.Id, stranger.Id, Save())
        );
    }
}
