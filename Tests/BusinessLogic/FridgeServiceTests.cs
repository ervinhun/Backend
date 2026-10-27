using BusinessLogic.Dtos.Fridge;
using BusinessLogic.Exceptions;
using BusinessLogic.Interfaces;
using BusinessLogic.Services;
using DataAccess.Entities;
using DataAccess.Exceptions;
using DataAccess.Repositories;
using Microsoft.EntityFrameworkCore;
using Tests.Database;
using Tests.Fakes;

namespace Tests.BusinessLogic;

/// <summary>FridgeService against the real repository and Postgres: it is mostly a thin layer over the queries.</summary>
public class FridgeServiceTests(PostgresFixture database) : DatabaseTest(database)
{
    private FridgeService Service => ServiceWith(new FridgeLimitDefaults());

    private FridgeService ServiceWith(IFridgeLimitDefaults limits) =>
        new(
            new FridgeRepository(Database.CreateDbContext(), TimeProvider.System),
            limits,
            TimeProvider.System
        );

    private (FridgeService Service, CollidingFridgeRepository Repository) ServiceWithCollisions(
        int collisions
    )
    {
        var repository = new CollidingFridgeRepository(
            new FridgeRepository(Database.CreateDbContext(), TimeProvider.System),
            collisions
        );
        return (
            new FridgeService(repository, new FridgeLimitDefaults(), TimeProvider.System),
            repository
        );
    }

    private async Task<User> AddUserAsync(string email, string displayName = "Anna")
    {
        await using var db = Database.CreateDbContext();
        var user = new User
        {
            Email = email,
            PasswordHash = "hash",
            DefaultDisplayName = displayName,
        };
        db.Users.Add(user);
        await db.SaveChangesAsync();
        return user;
    }

    private async Task AddMemberAsync(Guid fridgeId, User user, string? displayName = null)
    {
        await using var db = Database.CreateDbContext();
        db.FridgeMembers.Add(
            new FridgeMember
            {
                FridgeId = fridgeId,
                UserId = user.Id,
                DisplayName = displayName,
            }
        );
        await db.SaveChangesAsync();
    }

    private static SaveFridgeRequest Save(string name) => new() { Name = name };

    [Fact]
    public async Task Create_TrimsTheName_AndMakesTheCallerTheOwner()
    {
        var anna = await AddUserAsync("anna@example.com");

        var fridge = await Service.CreateAsync(anna.Id, Save("  Home  "));

        Assert.Equal("Home", fridge.Name);
        Assert.True(fridge.IsOwner);
        Assert.True(fridge.CanPut);
        Assert.True(fridge.CanUse);
        Assert.Equal(1, fridge.MemberCount);
        Assert.Equal(15, fridge.MaxMembers);
        Assert.Equal(10, fridge.MaxContainers);
        Assert.Equal(fridge.Id, (await Service.GetAsync(fridge.Id, anna.Id)).Id);
    }

    private async Task<(int MaxMembers, int MaxContainers)> StoredLimitsAsync(Guid fridgeId)
    {
        await using var db = Database.CreateDbContext();
        var fridge = await db.Fridges.SingleAsync(f => f.Id == fridgeId);
        return (fridge.MaxMembers, fridge.MaxContainers);
    }

    [Fact]
    public async Task Create_StoresTheLimitsOfTheDefaultsOnTheFridge()
    {
        var anna = await AddUserAsync("anna@example.com");

        var fridge = await ServiceWith(new FixedFridgeLimits(4, 3))
            .CreateAsync(anna.Id, Save("Home"));

        Assert.Equal((4, 3), (fridge.MaxMembers, fridge.MaxContainers));
        Assert.Equal((4, 3), await StoredLimitsAsync(fridge.Id));
    }

    [Fact]
    public async Task Update_KeepsTheLimitsTheFridgeHas_EvenWhenTheDefaultsChanged()
    {
        var anna = await AddUserAsync("anna@example.com");
        var fridge = await ServiceWith(new FixedFridgeLimits(4, 3))
            .CreateAsync(anna.Id, Save("Home"));
        await using (var db = Database.CreateDbContext())
            await db.Fridges.ExecuteUpdateAsync(s =>
                s.SetProperty(f => f.MaxMembers, 40).SetProperty(f => f.MaxContainers, 25)
            );

        var updated = await ServiceWith(new FixedFridgeLimits(6, 2))
            .UpdateAsync(fridge.Id, anna.Id, Save("Renamed"));

        Assert.Equal("Renamed", updated.Name);
        Assert.Equal((40, 25), (updated.MaxMembers, updated.MaxContainers));
        Assert.Equal((40, 25), await StoredLimitsAsync(fridge.Id));
    }

    [Fact]
    public async Task Join_AFridgeWithTheDefaultLimit_StopsAtFifteenMembers_UntilSomeoneLeaves()
    {
        var anna = await AddUserAsync("anna@example.com");
        var fridge = await Service.CreateAsync(anna.Id, Save("Home"));
        for (var i = 1; i < FridgeLimitDefaults.DefaultMaxMembers; i++)
            await AddMemberAsync(fridge.Id, await AddUserAsync($"member{i}@example.com", $"M{i}"));
        var late = await AddUserAsync("late@example.com", "Late");

        var error = await Assert.ThrowsAsync<ConflictException>(() =>
            Service.JoinAsync(late.Id, Join(fridge.InviteCode!))
        );
        Assert.Contains("limit", error.Message);
        Assert.Equal(15, (await Service.GetAsync(fridge.Id, anna.Id)).MemberCount);

        var leaver = (await Service.GetMembersAsync(fridge.Id)).First(m => !m.IsOwner);
        await Service.RemoveMemberAsync(fridge.Id, leaver.UserId);
        var joined = await Service.JoinAsync(late.Id, Join(fridge.InviteCode!));

        Assert.Equal(15, joined.MemberCount);
    }

    [Fact]
    public async Task Create_BeyondTheOwnedFridgeLimit_ThrowsConflict_UntilOneIsDeleted()
    {
        var anna = await AddUserAsync("anna@example.com");
        FridgeResponse? last = null;
        for (var i = 0; i < FridgeService.MaxOwnedFridges; i++)
            last = await Service.CreateAsync(anna.Id, Save($"Fridge {i}"));

        var error = await Assert.ThrowsAsync<ConflictException>(() =>
            Service.CreateAsync(anna.Id, Save("One too many"))
        );
        await Service.DeleteAsync(last!.Id);
        var created = await Service.CreateAsync(anna.Id, Save("One more"));

        Assert.Contains(FridgeService.MaxOwnedFridges.ToString(), error.Message);
        Assert.Equal("One more", created.Name);
    }

    [Fact]
    public async Task Create_FridgesTheUserOnlyJoinedDoNotCountTowardTheLimit()
    {
        var anna = await AddUserAsync("anna@example.com");
        var ben = await AddUserAsync("ben@example.com", "Ben");
        for (var i = 0; i < FridgeService.MaxOwnedFridges; i++)
        {
            var bens = await Service.CreateAsync(ben.Id, Save($"Bens {i}"));
            await Service.JoinAsync(
                anna.Id,
                new JoinFridgeRequest { InviteCode = bens.InviteCode! }
            );
        }

        var created = await Service.CreateAsync(anna.Id, Save("Annas own"));

        Assert.True(created.IsOwner);
        Assert.Equal(
            FridgeService.MaxOwnedFridges + 1,
            (await Service.GetForUserAsync(anna.Id)).Count
        );
    }

    [Fact]
    public async Task Create_GivesTheOwnerAnEightCharacterInviteCode_AndEveryFridgeItsOwn()
    {
        var anna = await AddUserAsync("anna@example.com");

        var first = await Service.CreateAsync(anna.Id, Save("First"));
        var second = await Service.CreateAsync(anna.Id, Save("Second"));

        Assert.Matches("^[A-HJ-NP-Z2-9]{8}$", first.InviteCode);
        Assert.NotEqual(first.InviteCode, second.InviteCode);
    }

    [Fact]
    public async Task Create_WhenTheCodeIsTaken_TriesANewCode()
    {
        var anna = await AddUserAsync("anna@example.com");
        var (service, repository) = ServiceWithCollisions(collisions: 2);

        var fridge = await service.CreateAsync(anna.Id, Save("Home"));

        Assert.Equal(3, repository.AddCalls);
        Assert.Equal(fridge.Id, (await service.GetAsync(fridge.Id, anna.Id)).Id);
    }

    [Fact]
    public async Task Create_WhenEveryCodeIsTaken_GivesUpAfterFiveAttempts()
    {
        var anna = await AddUserAsync("anna@example.com");
        var (service, repository) = ServiceWithCollisions(collisions: int.MaxValue);

        await Assert.ThrowsAsync<DuplicateKeyException>(() =>
            service.CreateAsync(anna.Id, Save("Home"))
        );

        Assert.Equal(5, repository.AddCalls);
        Assert.Empty(await service.GetForUserAsync(anna.Id));
    }

    [Fact]
    public async Task RegenerateInviteCode_WhenTheCodeIsTaken_TriesANewCode()
    {
        var anna = await AddUserAsync("anna@example.com");
        var fridge = await Service.CreateAsync(anna.Id, Save("Home"));
        var (service, repository) = ServiceWithCollisions(collisions: 2);

        var regenerated = await service.RegenerateInviteCodeAsync(fridge.Id, anna.Id);

        Assert.Equal(3, repository.SetInviteCodeCalls);
        Assert.NotEqual(fridge.InviteCode, regenerated.InviteCode);
    }

    [Fact]
    public async Task RegenerateInviteCode_WhenEveryCodeIsTaken_GivesUpAfterFiveAttempts()
    {
        var anna = await AddUserAsync("anna@example.com");
        var fridge = await Service.CreateAsync(anna.Id, Save("Home"));
        var (service, repository) = ServiceWithCollisions(collisions: int.MaxValue);

        await Assert.ThrowsAsync<DuplicateKeyException>(() =>
            service.RegenerateInviteCodeAsync(fridge.Id, anna.Id)
        );

        Assert.Equal(5, repository.SetInviteCodeCalls);
        Assert.Equal(fridge.InviteCode, (await Service.GetAsync(fridge.Id, anna.Id)).InviteCode);
    }

    [Fact]
    public async Task Get_AsAMember_HidesTheInviteCode()
    {
        var anna = await AddUserAsync("anna@example.com");
        var ben = await AddUserAsync("ben@example.com", "Ben");
        var fridge = await Service.CreateAsync(anna.Id, Save("Home"));
        await AddMemberAsync(fridge.Id, ben);

        var asMember = await Service.GetAsync(fridge.Id, ben.Id);
        var asOwner = await Service.GetAsync(fridge.Id, anna.Id);

        Assert.Null(asMember.InviteCode);
        Assert.False(asMember.IsOwner);
        Assert.Equal(2, asMember.MemberCount);
        Assert.NotNull(asOwner.InviteCode);
    }

    [Fact]
    public async Task Get_AsNonMember_ThrowsNotFound()
    {
        var anna = await AddUserAsync("anna@example.com");
        var ben = await AddUserAsync("ben@example.com", "Ben");
        var fridge = await Service.CreateAsync(anna.Id, Save("Home"));

        await Assert.ThrowsAsync<NotFoundException>(() => Service.GetAsync(fridge.Id, ben.Id));
    }

    [Fact]
    public async Task GetForUser_ListsOnlyTheirFridges()
    {
        var anna = await AddUserAsync("anna@example.com");
        var ben = await AddUserAsync("ben@example.com", "Ben");
        await Service.CreateAsync(anna.Id, Save("Annas"));
        await Service.CreateAsync(ben.Id, Save("Bens"));

        var fridges = await Service.GetForUserAsync(anna.Id);

        Assert.Equal("Annas", Assert.Single(fridges).Name);
    }

    [Fact]
    public async Task Update_RenamesTheFridge()
    {
        var anna = await AddUserAsync("anna@example.com");
        var fridge = await Service.CreateAsync(anna.Id, Save("Home"));

        var updated = await Service.UpdateAsync(fridge.Id, anna.Id, Save(" Cabin "));

        Assert.Equal("Cabin", updated.Name);
    }

    [Fact]
    public async Task Update_UnknownFridge_ThrowsNotFound()
    {
        var anna = await AddUserAsync("anna@example.com");

        await Assert.ThrowsAsync<NotFoundException>(() =>
            Service.UpdateAsync(Guid.NewGuid(), anna.Id, Save("Cabin"))
        );
    }

    [Fact]
    public async Task RegenerateInviteCode_ReplacesTheCode()
    {
        var anna = await AddUserAsync("anna@example.com");
        var fridge = await Service.CreateAsync(anna.Id, Save("Home"));

        var regenerated = await Service.RegenerateInviteCodeAsync(fridge.Id, anna.Id);

        Assert.NotEqual(fridge.InviteCode, regenerated.InviteCode);
        Assert.Equal(
            regenerated.InviteCode,
            (await Service.GetAsync(fridge.Id, anna.Id)).InviteCode
        );
    }

    [Fact]
    public async Task RegenerateInviteCode_UnknownFridge_ThrowsNotFound()
    {
        var anna = await AddUserAsync("anna@example.com");

        await Assert.ThrowsAsync<NotFoundException>(() =>
            Service.RegenerateInviteCodeAsync(Guid.NewGuid(), anna.Id)
        );
    }

    private static JoinFridgeRequest Join(string code) => new() { InviteCode = code };

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Join_WithTheInviteCode_MakesTheCallerAMember(bool typedSloppily)
    {
        var anna = await AddUserAsync("anna@example.com");
        var ben = await AddUserAsync("ben@example.com", "Ben");
        var fridge = await Service.CreateAsync(anna.Id, Save("Home"));
        var code = typedSloppily
            ? $"  {fridge.InviteCode!.ToLowerInvariant()} "
            : fridge.InviteCode!;

        var joined = await Service.JoinAsync(ben.Id, Join(code));

        Assert.Equal(fridge.Id, joined.Id);
        Assert.False(joined.IsOwner);
        Assert.Null(joined.InviteCode);
        Assert.Equal(2, joined.MemberCount);
    }

    [Fact]
    public async Task Join_UnknownCode_ThrowsNotFound()
    {
        var ben = await AddUserAsync("ben@example.com", "Ben");

        var error = await Assert.ThrowsAsync<NotFoundException>(() =>
            Service.JoinAsync(ben.Id, Join("NOSUCHCODE"))
        );

        Assert.Equal("No fridge has this invite code.", error.Message);
    }

    [Fact]
    public async Task Join_WhenAlreadyAMember_ThrowsConflict()
    {
        var anna = await AddUserAsync("anna@example.com");
        var fridge = await Service.CreateAsync(anna.Id, Save("Home"));

        var error = await Assert.ThrowsAsync<ConflictException>(() =>
            Service.JoinAsync(anna.Id, Join(fridge.InviteCode!))
        );

        Assert.Equal("You are already a member of this fridge.", error.Message);
    }

    [Fact]
    public async Task Join_FridgeAtItsMemberLimit_ThrowsConflict()
    {
        var anna = await AddUserAsync("anna@example.com");
        var ben = await AddUserAsync("ben@example.com", "Ben");
        var cleo = await AddUserAsync("cleo@example.com", "Cleo");
        var fridge = await Service.CreateAsync(anna.Id, Save("Home"));
        await using (var db = Database.CreateDbContext())
            await db.Fridges.ExecuteUpdateAsync(s => s.SetProperty(f => f.MaxMembers, 2));
        await Service.JoinAsync(ben.Id, Join(fridge.InviteCode!));

        var error = await Assert.ThrowsAsync<ConflictException>(() =>
            Service.JoinAsync(cleo.Id, Join(fridge.InviteCode!))
        );

        Assert.Contains("limit", error.Message);
        Assert.Equal(2, (await Service.GetAsync(fridge.Id, anna.Id)).MemberCount);
    }

    [Fact]
    public async Task Delete_RemovesTheFridgeForEveryone_AndCannotBeRepeated()
    {
        var anna = await AddUserAsync("anna@example.com");
        var ben = await AddUserAsync("ben@example.com", "Ben");
        var fridge = await Service.CreateAsync(anna.Id, Save("Home"));
        await AddMemberAsync(fridge.Id, ben);

        await Service.DeleteAsync(fridge.Id);

        Assert.Empty(await Service.GetForUserAsync(anna.Id));
        Assert.Empty(await Service.GetForUserAsync(ben.Id));
        Assert.Null(await Service.GetMemberAsync(fridge.Id, ben.Id));
        await Assert.ThrowsAsync<NotFoundException>(() => Service.DeleteAsync(fridge.Id));
    }

    [Fact]
    public async Task GetMembers_NamesEachMember_OwnerFirst()
    {
        var anna = await AddUserAsync("anna@example.com");
        var ben = await AddUserAsync("ben@example.com", "Ben");
        var fridge = await Service.CreateAsync(anna.Id, Save("Home"));
        await AddMemberAsync(fridge.Id, ben);

        var members = await Service.GetMembersAsync(fridge.Id);

        Assert.Equal(["Anna", "Ben"], members.Select(m => m.DisplayName));
        Assert.Equal([true, false], members.Select(m => m.IsOwner));
    }

    [Fact]
    public async Task GetMembers_PrefersTheNameAMemberGoesByOverTheirAccountName()
    {
        var anna = await AddUserAsync("anna@example.com");
        var ben = await AddUserAsync("ben@example.com", "Ben");
        var fridge = await Service.CreateAsync(anna.Id, Save("Home"));
        await AddMemberAsync(fridge.Id, ben, displayName: "Benny");

        var members = await Service.GetMembersAsync(fridge.Id);
        var member = await Service.GetMemberAsync(fridge.Id, ben.Id);

        Assert.Equal(["Anna", "Benny"], members.Select(m => m.DisplayName));
        Assert.Equal("Benny", member!.DisplayName);
    }

    [Fact]
    public async Task UpdateMemberRights_ReturnsTheMemberWithTheirNewRights()
    {
        var anna = await AddUserAsync("anna@example.com");
        var ben = await AddUserAsync("ben@example.com", "Ben");
        var fridge = await Service.CreateAsync(anna.Id, Save("Home"));
        await AddMemberAsync(fridge.Id, ben);
        var expires = DateTimeOffset.UtcNow.AddDays(1);

        var member = await Service.UpdateMemberRightsAsync(
            fridge.Id,
            ben.Id,
            new UpdateMemberRightsRequest
            {
                CanPut = false,
                CanUse = true,
                AccessExpiresAt = expires,
            }
        );

        Assert.Equal("Ben", member.DisplayName);
        Assert.False(member.CanPut);
        Assert.True(member.CanUse);
        Assert.Equal(expires, member.AccessExpiresAt!.Value, TimeSpan.FromMilliseconds(1));
    }

    [Fact]
    public async Task UpdateMemberRights_NonMember_ThrowsNotFound()
    {
        var anna = await AddUserAsync("anna@example.com");
        var fridge = await Service.CreateAsync(anna.Id, Save("Home"));

        await Assert.ThrowsAsync<NotFoundException>(() =>
            Service.UpdateMemberRightsAsync(
                fridge.Id,
                Guid.NewGuid(),
                new UpdateMemberRightsRequest { CanPut = true, CanUse = true }
            )
        );
    }

    [Fact]
    public async Task UpdateMemberRights_Owner_ThrowsConflict()
    {
        var anna = await AddUserAsync("anna@example.com");
        var fridge = await Service.CreateAsync(anna.Id, Save("Home"));

        await Assert.ThrowsAsync<ConflictException>(() =>
            Service.UpdateMemberRightsAsync(
                fridge.Id,
                anna.Id,
                new UpdateMemberRightsRequest { CanPut = false, CanUse = false }
            )
        );
        Assert.True((await Service.GetMemberAsync(fridge.Id, anna.Id))!.CanPut);
    }

    [Fact]
    public async Task RemoveMember_Owner_ThrowsConflict_AndNonMemberThrowsNotFound()
    {
        var anna = await AddUserAsync("anna@example.com");
        var fridge = await Service.CreateAsync(anna.Id, Save("Home"));

        await Assert.ThrowsAsync<ConflictException>(() =>
            Service.RemoveMemberAsync(fridge.Id, anna.Id)
        );
        await Assert.ThrowsAsync<NotFoundException>(() =>
            Service.RemoveMemberAsync(fridge.Id, Guid.NewGuid())
        );
        Assert.NotNull(await Service.GetMemberAsync(fridge.Id, anna.Id));
    }

    [Fact]
    public async Task RemoveMember_TakesThemOutOfTheFridge()
    {
        var anna = await AddUserAsync("anna@example.com");
        var ben = await AddUserAsync("ben@example.com", "Ben");
        var fridge = await Service.CreateAsync(anna.Id, Save("Home"));
        await AddMemberAsync(fridge.Id, ben);

        await Service.RemoveMemberAsync(fridge.Id, ben.Id);

        Assert.Null(await Service.GetMemberAsync(fridge.Id, ben.Id));
        await Assert.ThrowsAsync<NotFoundException>(() =>
            Service.RemoveMemberAsync(fridge.Id, ben.Id)
        );
    }
}
