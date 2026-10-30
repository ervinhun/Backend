using DataAccess.Entities;
using DataAccess.Exceptions;
using DataAccess.Interfaces;
using DataAccess.Models.Fridge;
using DataAccess.Repositories;
using Microsoft.EntityFrameworkCore;
using Tests.Database;

namespace Tests.DataAccess;

public class FridgeRepositoryTests(PostgresFixture database) : DatabaseTest(database)
{
    // For the tests that aren't about the limit on owned fridges.
    private const int NoLimit = int.MaxValue;

    private FridgeRepository Repository => new(Database.CreateDbContext(), TimeProvider.System);

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

    private async Task<Fridge> AddFridgeAsync(
        User owner,
        string name = "Home",
        string inviteCode = "CODE0001"
    )
    {
        var fridge = new Fridge
        {
            Name = name,
            InviteCode = inviteCode,
            CreatedByUserId = owner.Id,
        };
        await Repository.AddAsync(
            fridge,
            new FridgeMember { UserId = owner.Id, IsOwner = true },
            NoLimit
        );
        return fridge;
    }

    private async Task AddMemberAsync(Fridge fridge, User user, string? displayName = null)
    {
        await using var db = Database.CreateDbContext();
        db.FridgeMembers.Add(
            new FridgeMember
            {
                FridgeId = fridge.Id,
                UserId = user.Id,
                DisplayName = displayName,
            }
        );
        await db.SaveChangesAsync();
    }

    [Fact]
    public async Task Add_SavesFridgeAndOwner_WithDatabaseDefaults()
    {
        var anna = await AddUserAsync("anna@example.com");

        var fridge = await AddFridgeAsync(anna);

        Assert.NotEqual(Guid.Empty, fridge.Id);
        Assert.Equal(15, fridge.MaxMembers);
        Assert.Equal(10, fridge.MaxContainers);
        Assert.True(fridge.CreatedAt > DateTimeOffset.UtcNow.AddMinutes(-1));

        var stored = await Repository.GetAsync(fridge.Id, anna.Id);
        Assert.True(stored!.Member.IsOwner);
        Assert.True(stored.Member.CanPut);
        Assert.True(stored.Member.CanUse);
        Assert.Equal(1, stored.MemberCount);
    }

    [Fact]
    public async Task EntityDefaults_MatchTheColumnDefaultsInTheMigration()
    {
        var anna = await AddUserAsync("anna@example.com");
        await using var db = Database.CreateDbContext();
        await db.Database.ExecuteSqlAsync(
            $"INSERT INTO fridges (name, invite_code, created_by_user_id) VALUES ('Raw', 'RAWCODE1', {anna.Id})"
        );

        var raw = await db.Fridges.SingleAsync(f => f.InviteCode == "RAWCODE1");

        Assert.Equal(Fridge.DefaultMaxMembers, raw.MaxMembers);
        Assert.Equal(Fridge.DefaultMaxContainers, raw.MaxContainers);
    }

    [Fact]
    public async Task Add_SavesTheLimitsItIsGiven_ZeroIncluded()
    {
        var anna = await AddUserAsync("anna@example.com");

        var fridge = new Fridge
        {
            Name = "Closed",
            InviteCode = "CODE0001",
            CreatedByUserId = anna.Id,
            MaxMembers = 0,
            MaxContainers = 3,
        };
        await Repository.AddAsync(
            fridge,
            new FridgeMember { UserId = anna.Id, IsOwner = true },
            NoLimit
        );

        var stored = (await Repository.GetAsync(fridge.Id, anna.Id))!.Fridge;
        Assert.Equal(0, stored.MaxMembers);
        Assert.Equal(3, stored.MaxContainers);
    }

    [Fact]
    public async Task Add_DuplicateInviteCode_ThrowsDuplicateKey_AndTheSameRepositoryCanRetry()
    {
        var anna = await AddUserAsync("anna@example.com");
        var repository = Repository;
        await repository.AddAsync(
            new Fridge
            {
                Name = "First",
                InviteCode = "SAMECODE",
                CreatedByUserId = anna.Id,
            },
            new FridgeMember { UserId = anna.Id, IsOwner = true },
            NoLimit
        );

        await Assert.ThrowsAsync<DuplicateKeyException>(() =>
            repository.AddAsync(
                new Fridge
                {
                    Name = "Second",
                    InviteCode = "SAMECODE",
                    CreatedByUserId = anna.Id,
                },
                new FridgeMember { UserId = anna.Id, IsOwner = true },
                NoLimit
            )
        );
        await repository.AddAsync(
            new Fridge
            {
                Name = "Second",
                InviteCode = "OTHERCODE",
                CreatedByUserId = anna.Id,
            },
            new FridgeMember { UserId = anna.Id, IsOwner = true },
            NoLimit
        );

        Assert.Equal(
            ["First", "Second"],
            (await Repository.GetForUserAsync(anna.Id)).Select(x => x.Fridge.Name)
        );
    }

    [Fact]
    public async Task Add_AtTheLimitOfOwnedFridges_ReturnsLimitReached_AndSavesNothing()
    {
        var anna = await AddUserAsync("anna@example.com");
        await AddFridgeAsync(anna, "First", "CODE0001");
        await AddFridgeAsync(anna, "Second", "CODE0002");

        var result = await Repository.AddAsync(
            new Fridge
            {
                Name = "Third",
                InviteCode = "CODE0003",
                CreatedByUserId = anna.Id,
            },
            new FridgeMember { UserId = anna.Id, IsOwner = true },
            maxOwnedFridges: 2
        );

        Assert.Equal(AddFridgeStatus.LimitReached, result);
        Assert.Equal(2, (await Repository.GetForUserAsync(anna.Id)).Count);
    }

    [Fact]
    public async Task Add_LimitCountsOnlyFridgesTheUserOwns_AndDeletingOneFreesASlot()
    {
        var anna = await AddUserAsync("anna@example.com");
        var ben = await AddUserAsync("ben@example.com", "Ben");
        var bens = await AddFridgeAsync(ben, "Bens", "CODE0001");
        await Repository.JoinAsync("CODE0001", anna.Id);
        var annas = await AddFridgeAsync(anna, "Annas", "CODE0002");

        Fridge Another(string code) =>
            new()
            {
                Name = "Another",
                InviteCode = code,
                CreatedByUserId = anna.Id,
            };
        var owner = () => new FridgeMember { UserId = anna.Id, IsOwner = true };

        Assert.Equal(
            AddFridgeStatus.LimitReached,
            (await Repository.AddAsync(Another("CODE0003"), owner(), maxOwnedFridges: 1))
        );
        await Repository.DeleteAsync(annas.Id, DateTimeOffset.UtcNow);
        Assert.Equal(
            AddFridgeStatus.Added,
            (await Repository.AddAsync(Another("CODE0003"), owner(), maxOwnedFridges: 1))
        );

        Assert.Equal(2, (await Repository.GetForUserAsync(anna.Id)).Count);
        Assert.Equal(bens.Id, (await Repository.GetForUserAsync(ben.Id)).Single().Fridge.Id);
    }

    [Fact]
    public async Task Add_ManyAtOnce_NeverPassesTheLimit()
    {
        var anna = await AddUserAsync("anna@example.com");

        var results = await Task.WhenAll(
            Enumerable
                .Range(0, 8)
                .Select(async i =>
                {
                    var result = await Repository.AddAsync(
                        new Fridge
                        {
                            Name = $"Fridge {i}",
                            InviteCode = $"CODE000{i}",
                            CreatedByUserId = anna.Id,
                        },
                        new FridgeMember { UserId = anna.Id, IsOwner = true },
                        maxOwnedFridges: 3
                    );
                    return result == AddFridgeStatus.Added;
                })
        );

        Assert.Equal(3, results.Count(created => created));
        Assert.Equal(3, (await Repository.GetForUserAsync(anna.Id)).Count);
    }

    [Fact]
    public async Task GetForUser_ReturnsOnlyTheUsersFridges_OldestFirst_WithMemberCounts()
    {
        var anna = await AddUserAsync("anna@example.com");
        var ben = await AddUserAsync("ben@example.com", "Ben");
        var first = await AddFridgeAsync(anna, "First", "CODE0001");
        await AddFridgeAsync(anna, "Second", "CODE0002");
        await AddFridgeAsync(ben, "Bens", "CODE0003");
        await AddMemberAsync(first, ben);

        var annas = await Repository.GetForUserAsync(anna.Id);

        Assert.Equal(["First", "Second"], annas.Select(x => x.Fridge.Name));
        Assert.Equal([2, 1], annas.Select(x => x.MemberCount));
        Assert.Equal(2, (await Repository.GetForUserAsync(ben.Id)).Count);
    }

    [Fact]
    public async Task Get_NonMember_ReturnsNull()
    {
        var anna = await AddUserAsync("anna@example.com");
        var ben = await AddUserAsync("ben@example.com", "Ben");
        var fridge = await AddFridgeAsync(anna);

        Assert.Null(await Repository.GetAsync(fridge.Id, ben.Id));
        Assert.Null(await Repository.GetAsync(Guid.NewGuid(), anna.Id));
    }

    [Fact]
    public async Task UpdateName_ChangesOnlyTheName_OrReturnsFalseForUnknownFridge()
    {
        var anna = await AddUserAsync("anna@example.com");
        var fridge = await AddFridgeAsync(anna);
        await using (var db = Database.CreateDbContext())
            await db.Fridges.ExecuteUpdateAsync(s =>
                s.SetProperty(f => f.MaxMembers, 40).SetProperty(f => f.MaxContainers, 25)
            );

        Assert.True(await Repository.UpdateNameAsync(fridge.Id, "Renamed"));
        Assert.False(await Repository.UpdateNameAsync(Guid.NewGuid(), "Nope"));

        var stored = (await Repository.GetAsync(fridge.Id, anna.Id))!.Fridge;
        Assert.Equal("Renamed", stored.Name);
        Assert.Equal(40, stored.MaxMembers);
        Assert.Equal(25, stored.MaxContainers);
    }

    [Fact]
    public async Task SetInviteCode_ChangesTheCode_OrReturnsFalseForUnknownFridge()
    {
        var anna = await AddUserAsync("anna@example.com");
        var fridge = await AddFridgeAsync(anna);

        Assert.True(await Repository.SetInviteCodeAsync(fridge.Id, "NEWCODE1"));
        Assert.False(await Repository.SetInviteCodeAsync(Guid.NewGuid(), "NEWCODE2"));

        Assert.Equal(
            "NEWCODE1",
            (await Repository.GetAsync(fridge.Id, anna.Id))!.Fridge.InviteCode
        );
    }

    [Fact]
    public async Task SetInviteCode_CodeOfAnotherFridge_ThrowsDuplicateKey()
    {
        var anna = await AddUserAsync("anna@example.com");
        var first = await AddFridgeAsync(anna, "First", "CODE0001");
        await AddFridgeAsync(anna, "Second", "CODE0002");

        await Assert.ThrowsAsync<DuplicateKeyException>(() =>
            Repository.SetInviteCodeAsync(first.Id, "CODE0002")
        );
    }

    [Fact]
    public async Task Delete_HidesTheFridgeAndItsMembers_ButKeepsTheRows()
    {
        var anna = await AddUserAsync("anna@example.com");
        var ben = await AddUserAsync("ben@example.com", "Ben");
        var fridge = await AddFridgeAsync(anna);
        await AddMemberAsync(fridge, ben);

        Assert.True(await Repository.DeleteAsync(fridge.Id, DateTimeOffset.UtcNow));

        Assert.Null(await Repository.GetAsync(fridge.Id, anna.Id));
        Assert.Empty(await Repository.GetForUserAsync(ben.Id));
        Assert.Null(await Repository.GetMemberAsync(fridge.Id, ben.Id));
        Assert.Empty(await Repository.GetMembersAsync(fridge.Id));
        Assert.False(await Repository.DeleteAsync(fridge.Id, DateTimeOffset.UtcNow));

        await using var db = Database.CreateDbContext();
        Assert.Equal(1, await db.Fridges.IgnoreQueryFilters().CountAsync());
        Assert.Equal(
            2,
            await db.FridgeMembers.IgnoreQueryFilters().CountAsync(m => m.DeletedAt != null)
        );
    }

    [Fact]
    public async Task GetMembers_ListsTheOwnerFirst_WithTheNameTheyGoBy()
    {
        var anna = await AddUserAsync("anna@example.com");
        var ben = await AddUserAsync("ben@example.com", "Ben");
        var cleo = await AddUserAsync("cleo@example.com", "Cleo");
        var fridge = await AddFridgeAsync(anna);
        await AddMemberAsync(fridge, ben, displayName: "Benny");
        await AddMemberAsync(fridge, cleo);

        var members = await Repository.GetMembersAsync(fridge.Id);

        Assert.Equal([anna.Id, ben.Id, cleo.Id], members.Select(m => m.UserId));
        Assert.Equal(
            ["Anna", "Cleo"],
            members.Where(m => m.DisplayName is null).Select(m => m.User!.DefaultDisplayName)
        );
        Assert.Equal("Benny", members[1].DisplayName);
    }

    [Fact]
    public async Task GetMembers_ListsOnlyThatFridgesMembers_AndNoneOnceTheFridgeIsDeleted()
    {
        var anna = await AddUserAsync("anna@example.com");
        var ben = await AddUserAsync("ben@example.com", "Ben");
        var home = await AddFridgeAsync(anna, "Home", "CODE0001");
        var cabin = await AddFridgeAsync(ben, "Cabin", "CODE0002");
        await AddMemberAsync(home, ben);

        Assert.Equal(
            [anna.Id, ben.Id],
            (await Repository.GetMembersAsync(home.Id)).Select(m => m.UserId)
        );
        Assert.Equal([ben.Id], (await Repository.GetMembersAsync(cabin.Id)).Select(m => m.UserId));
        Assert.Null(await Repository.GetMemberAsync(cabin.Id, anna.Id));

        // Only the fridge row is marked deleted, so the members must be hidden through it.
        await using (var db = Database.CreateDbContext())
            await db
                .Fridges.Where(f => f.Id == home.Id)
                .ExecuteUpdateAsync(s => s.SetProperty(f => f.DeletedAt, DateTimeOffset.UtcNow));
        Assert.Empty(await Repository.GetMembersAsync(home.Id));
        Assert.Null(await Repository.GetMemberAsync(home.Id, ben.Id));
        Assert.Equal([ben.Id], (await Repository.GetMembersAsync(cabin.Id)).Select(m => m.UserId));
    }

    [Fact]
    public async Task GetMember_OutsideTheFridge_ReturnsNull()
    {
        var anna = await AddUserAsync("anna@example.com");
        var ben = await AddUserAsync("ben@example.com", "Ben");
        var fridge = await AddFridgeAsync(anna);

        Assert.NotNull(await Repository.GetMemberAsync(fridge.Id, anna.Id));
        Assert.Null(await Repository.GetMemberAsync(fridge.Id, ben.Id));
    }

    private async Task SetAccessExpiryAsync(Fridge fridge, User user, TimeSpan fromNow)
    {
        await using var db = Database.CreateDbContext();
        var expiresAt = DateTimeOffset.UtcNow + fromNow;
        await db
            .FridgeMembers.Where(m => m.FridgeId == fridge.Id && m.UserId == user.Id)
            .ExecuteUpdateAsync(s => s.SetProperty(m => m.AccessExpiresAt, expiresAt));
    }

    [Fact]
    public async Task ExpiredMember_IsNoLongerInTheFridge()
    {
        var anna = await AddUserAsync("anna@example.com");
        var ben = await AddUserAsync("ben@example.com", "Ben");
        var fridge = await AddFridgeAsync(anna);
        await AddMemberAsync(fridge, ben);
        await SetAccessExpiryAsync(fridge, ben, TimeSpan.FromHours(-1));

        Assert.Null(await Repository.GetMemberAsync(fridge.Id, ben.Id));
        Assert.Null(await Repository.GetAsync(fridge.Id, ben.Id));
        Assert.Empty(await Repository.GetForUserAsync(ben.Id));
        Assert.Equal(
            [anna.Id],
            (await Repository.GetMembersAsync(fridge.Id)).Select(m => m.UserId)
        );
        Assert.Equal(1, (await Repository.GetAsync(fridge.Id, anna.Id))!.MemberCount);
    }

    [Fact]
    public async Task MemberWhoseAccessHasNotExpiredYet_IsStillInTheFridge()
    {
        var anna = await AddUserAsync("anna@example.com");
        var ben = await AddUserAsync("ben@example.com", "Ben");
        var fridge = await AddFridgeAsync(anna);
        await AddMemberAsync(fridge, ben);
        await SetAccessExpiryAsync(fridge, ben, TimeSpan.FromHours(1));

        Assert.NotNull(await Repository.GetMemberAsync(fridge.Id, ben.Id));
        Assert.Equal(2, (await Repository.GetAsync(fridge.Id, ben.Id))!.MemberCount);
    }

    [Fact]
    public async Task Join_ExpiredMembersDoNotUseUpTheLimit()
    {
        var anna = await AddUserAsync("anna@example.com");
        var ben = await AddUserAsync("ben@example.com", "Ben");
        var cleo = await AddUserAsync("cleo@example.com", "Cleo");
        var fridge = await AddFridgeAsync(anna);
        await SetMaxMembersAsync(fridge, 2);
        await AddMemberAsync(fridge, ben);
        await SetAccessExpiryAsync(fridge, ben, TimeSpan.FromHours(-1));

        Assert.Equal(
            JoinFridgeStatus.Joined,
            (await Repository.JoinAsync("CODE0001", cleo.Id)).Status
        );
    }

    [Fact]
    public async Task Join_AfterAccessExpired_StartsAFreshMembership()
    {
        var anna = await AddUserAsync("anna@example.com");
        var ben = await AddUserAsync("ben@example.com", "Ben");
        var fridge = await AddFridgeAsync(anna);
        await AddMemberAsync(fridge, ben);
        await SetAccessExpiryAsync(fridge, ben, TimeSpan.FromHours(-1));

        var result = await Repository.JoinAsync("CODE0001", ben.Id);

        Assert.Equal(JoinFridgeStatus.Joined, result.Status);
        Assert.Null((await Repository.GetMemberAsync(fridge.Id, ben.Id))!.AccessExpiresAt);
        await using var db = Database.CreateDbContext();
        var rows = await db
            .FridgeMembers.IgnoreQueryFilters()
            .Where(m => m.UserId == ben.Id)
            .ToListAsync();
        Assert.Equal(1, rows.Count(m => m.DeletedAt == null));
        Assert.Equal(1, rows.Count(m => m.DeletedAt != null));
    }

    private async Task SetMaxMembersAsync(Fridge fridge, int maxMembers)
    {
        await using var db = Database.CreateDbContext();
        await db
            .Fridges.Where(f => f.Id == fridge.Id)
            .ExecuteUpdateAsync(s => s.SetProperty(f => f.MaxMembers, maxMembers));
    }

    [Fact]
    public async Task Join_AddsTheUserAsAMemberWithDefaultRights()
    {
        var anna = await AddUserAsync("anna@example.com");
        var ben = await AddUserAsync("ben@example.com", "Ben");
        var fridge = await AddFridgeAsync(anna);

        var result = await Repository.JoinAsync("CODE0001", ben.Id);

        Assert.Equal(new JoinFridgeResult(JoinFridgeStatus.Joined, fridge.Id), result);
        var member = await Repository.GetMemberAsync(fridge.Id, ben.Id);
        Assert.False(member!.IsOwner);
        Assert.True(member.CanPut);
        Assert.True(member.CanUse);
        Assert.Equal(2, (await Repository.GetAsync(fridge.Id, anna.Id))!.MemberCount);
    }

    [Fact]
    public async Task Join_UnknownOrDeletedFridgeCode_ReturnsUnknownCode()
    {
        var anna = await AddUserAsync("anna@example.com");
        var ben = await AddUserAsync("ben@example.com", "Ben");
        var fridge = await AddFridgeAsync(anna);
        await Repository.DeleteAsync(fridge.Id, DateTimeOffset.UtcNow);

        Assert.Equal(
            JoinFridgeStatus.UnknownCode,
            (await Repository.JoinAsync("CODE0001", ben.Id)).Status
        );
        Assert.Equal(
            JoinFridgeStatus.UnknownCode,
            (await Repository.JoinAsync("NOSUCHCODE", ben.Id)).Status
        );
    }

    [Fact]
    public async Task Join_AlreadyAMember_ReturnsAlreadyMember_EvenWhenFull()
    {
        var anna = await AddUserAsync("anna@example.com");
        var fridge = await AddFridgeAsync(anna);
        await SetMaxMembersAsync(fridge, 1);

        var result = await Repository.JoinAsync("CODE0001", anna.Id);

        Assert.Equal(JoinFridgeStatus.AlreadyMember, result.Status);
    }

    [Fact]
    public async Task Join_FullFridge_ReturnsFull_UntilSomeoneLeaves()
    {
        var anna = await AddUserAsync("anna@example.com");
        var ben = await AddUserAsync("ben@example.com", "Ben");
        var cleo = await AddUserAsync("cleo@example.com", "Cleo");
        var fridge = await AddFridgeAsync(anna);
        await SetMaxMembersAsync(fridge, 2);
        await Repository.JoinAsync("CODE0001", ben.Id);

        Assert.Equal(
            JoinFridgeStatus.Full,
            (await Repository.JoinAsync("CODE0001", cleo.Id)).Status
        );
        Assert.Null(await Repository.GetMemberAsync(fridge.Id, cleo.Id));

        await Repository.RemoveMemberAsync(fridge.Id, ben.Id, DateTimeOffset.UtcNow);

        Assert.Equal(
            JoinFridgeStatus.Joined,
            (await Repository.JoinAsync("CODE0001", cleo.Id)).Status
        );
    }

    [Fact]
    public async Task Join_AFormerMember_CanJoinAgain()
    {
        var anna = await AddUserAsync("anna@example.com");
        var ben = await AddUserAsync("ben@example.com", "Ben");
        var fridge = await AddFridgeAsync(anna);
        await Repository.JoinAsync("CODE0001", ben.Id);
        await Repository.RemoveMemberAsync(fridge.Id, ben.Id, DateTimeOffset.UtcNow);

        var result = await Repository.JoinAsync("CODE0001", ben.Id);

        Assert.Equal(JoinFridgeStatus.Joined, result.Status);
    }

    [Fact]
    public async Task Join_ManyAtOnce_NeverPassesTheLimit()
    {
        var anna = await AddUserAsync("anna@example.com");
        var fridge = await AddFridgeAsync(anna);
        await SetMaxMembersAsync(fridge, 3);
        var joiners = new List<User>();
        for (var i = 0; i < 8; i++)
            joiners.Add(await AddUserAsync($"user{i}@example.com", $"User {i}"));

        var results = await Task.WhenAll(
            joiners.Select(u => Repository.JoinAsync("CODE0001", u.Id))
        );

        Assert.Equal(2, results.Count(r => r.Status == JoinFridgeStatus.Joined));
        Assert.Equal(6, results.Count(r => r.Status == JoinFridgeStatus.Full));
        Assert.Equal(3, (await Repository.GetAsync(fridge.Id, anna.Id))!.MemberCount);
    }

    [Fact]
    public async Task UpdateMemberRights_ChangesRights_OrReturnsFalseForNonMember()
    {
        var anna = await AddUserAsync("anna@example.com");
        var ben = await AddUserAsync("ben@example.com", "Ben");
        var fridge = await AddFridgeAsync(anna);
        await AddMemberAsync(fridge, ben);
        var expires = DateTimeOffset.UtcNow.AddDays(7);

        Assert.True(
            await Repository.UpdateMemberRightsAsync(
                fridge.Id,
                ben.Id,
                canPut: false,
                canUse: true,
                expires
            )
        );
        Assert.False(
            await Repository.UpdateMemberRightsAsync(fridge.Id, Guid.NewGuid(), true, true, null)
        );

        var member = await Repository.GetMemberAsync(fridge.Id, ben.Id);
        Assert.False(member!.CanPut);
        Assert.True(member.CanUse);
        Assert.Equal(expires, member.AccessExpiresAt!.Value, TimeSpan.FromMilliseconds(1));
    }

    [Fact]
    public async Task RemoveMember_TakesThemOutOfTheFridge_OnlyOnce()
    {
        var anna = await AddUserAsync("anna@example.com");
        var ben = await AddUserAsync("ben@example.com", "Ben");
        var fridge = await AddFridgeAsync(anna);
        await AddMemberAsync(fridge, ben);

        Assert.True(await Repository.RemoveMemberAsync(fridge.Id, ben.Id, DateTimeOffset.UtcNow));
        Assert.False(await Repository.RemoveMemberAsync(fridge.Id, ben.Id, DateTimeOffset.UtcNow));

        Assert.Null(await Repository.GetMemberAsync(fridge.Id, ben.Id));
        Assert.Empty(await Repository.GetForUserAsync(ben.Id));
        Assert.Equal(1, (await Repository.GetAsync(fridge.Id, anna.Id))!.MemberCount);
    }
}
