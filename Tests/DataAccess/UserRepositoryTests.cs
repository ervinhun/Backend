using DataAccess.Entities;
using DataAccess.Exceptions;
using DataAccess.Repositories;
using Tests.Database;

namespace Tests.DataAccess;

public class UserRepositoryTests(PostgresFixture database) : DatabaseTest(database)
{
    private UserRepository Repository => new(Database.CreateDbContext());

    private static User NewUser(string email = "anna@example.com") =>
        new()
        {
            Email = email,
            PasswordHash = "hash",
            DefaultDisplayName = "Anna",
        };

    [Fact]
    public async Task Add_GeneratesIdAndTimestampsInTheDatabase()
    {
        var user = await Repository.AddAsync(NewUser());

        Assert.NotEqual(Guid.Empty, user.Id);
        Assert.True(user.CreatedAt > DateTimeOffset.UtcNow.AddMinutes(-1));
        Assert.Null(user.DeletedAt);
    }

    [Fact]
    public async Task GetByEmail_AndById_FindTheStoredUser()
    {
        var added = await Repository.AddAsync(NewUser());

        Assert.Equal(added.Id, (await Repository.GetByEmailAsync("anna@example.com"))!.Id);
        Assert.Equal("anna@example.com", (await Repository.GetByIdAsync(added.Id))!.Email);
    }

    [Fact]
    public async Task Get_UnknownUser_ReturnsNull()
    {
        Assert.Null(await Repository.GetByEmailAsync("nobody@example.com"));
        Assert.Null(await Repository.GetByIdAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task Get_SoftDeletedUser_ReturnsNull()
    {
        var added = await Repository.AddAsync(NewUser());
        await using (var db = Database.CreateDbContext())
        {
            var user = await db.Users.FindAsync(added.Id);
            user!.DeletedAt = DateTimeOffset.UtcNow;
            await db.SaveChangesAsync();
        }

        Assert.Null(await Repository.GetByEmailAsync("anna@example.com"));
        Assert.Null(await Repository.GetByIdAsync(added.Id));
    }

    [Fact]
    public async Task EmailExists_CountsSoftDeletedUsersToo()
    {
        var added = await Repository.AddAsync(NewUser());
        await using (var db = Database.CreateDbContext())
        {
            var user = await db.Users.FindAsync(added.Id);
            user!.DeletedAt = DateTimeOffset.UtcNow;
            await db.SaveChangesAsync();
        }

        Assert.True(await Repository.EmailExistsAsync("anna@example.com"));
        Assert.False(await Repository.EmailExistsAsync("other@example.com"));
    }

    [Fact]
    public async Task Add_DuplicateEmail_ThrowsDuplicateKeyException()
    {
        await Repository.AddAsync(NewUser());

        await Assert.ThrowsAsync<DuplicateKeyException>(() => Repository.AddAsync(NewUser()));
    }

    [Fact]
    public async Task Reset_BetweenTests_LeavesNoUsersBehind()
    {
        await using var db = Database.CreateDbContext();

        Assert.Empty(db.Users);
    }
}
