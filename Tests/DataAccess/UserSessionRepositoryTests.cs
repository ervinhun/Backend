using DataAccess.Entities;
using DataAccess.Repositories;
using Tests.Database;

namespace Tests.DataAccess;

public class UserSessionRepositoryTests(PostgresFixture database) : DatabaseTest(database)
{
    private static readonly DateTimeOffset Now = new(2030, 1, 1, 0, 0, 0, TimeSpan.Zero);

    private UserSessionRepository Repository => new(Database.CreateDbContext());

    private async Task<UserSession> AddSessionAsync(string tokenHash = "hash-1")
    {
        await using var db = Database.CreateDbContext();
        var user = new User
        {
            Email = $"{Guid.NewGuid():N}@example.com",
            DefaultDisplayName = "Anna",
        };
        db.Users.Add(user);
        await db.SaveChangesAsync();

        return await Repository.AddAsync(
            new UserSession
            {
                UserId = user.Id,
                RefreshTokenHash = tokenHash,
                RefreshTokenExpiresAt = Now.AddDays(30),
            }
        );
    }

    [Fact]
    public async Task Add_StoresTheSessionAndFindsItByRefreshTokenHash()
    {
        var session = await AddSessionAsync();

        var found = await Repository.GetByRefreshTokenHashAsync("hash-1");

        Assert.Equal(session.Id, found!.Id);
        Assert.True(await Repository.IsSignedInAsync(session.Id, session.UserId));
    }

    [Fact]
    public async Task GetByRefreshTokenHash_UnknownHash_ReturnsNull()
    {
        await AddSessionAsync();

        Assert.Null(await Repository.GetByRefreshTokenHashAsync("other"));
    }

    [Fact]
    public async Task IsSignedIn_WrongUser_ReturnsFalse()
    {
        var session = await AddSessionAsync();

        Assert.False(await Repository.IsSignedInAsync(session.Id, Guid.NewGuid()));
    }

    [Fact]
    public async Task TryRotate_WithCurrentHash_ReplacesTheTokenOnce()
    {
        var session = await AddSessionAsync();

        var first = await Repository.TryRotateRefreshTokenAsync(
            session.Id,
            "hash-1",
            "hash-2",
            Now.AddDays(60),
            Now
        );
        var replay = await Repository.TryRotateRefreshTokenAsync(
            session.Id,
            "hash-1",
            "hash-3",
            Now.AddDays(60),
            Now
        );

        Assert.True(first);
        Assert.False(replay);
        Assert.Null(await Repository.GetByRefreshTokenHashAsync("hash-1"));
        var rotated = await Repository.GetByRefreshTokenHashAsync("hash-2");
        Assert.Equal(Now, rotated!.LastSeenAt);
        Assert.Equal(Now.AddDays(60), rotated.RefreshTokenExpiresAt);
    }

    [Fact]
    public async Task TryRotate_WithWrongHash_ChangesNothing()
    {
        var session = await AddSessionAsync();

        Assert.False(
            await Repository.TryRotateRefreshTokenAsync(
                session.Id,
                "wrong",
                "hash-2",
                Now.AddDays(60),
                Now
            )
        );
        Assert.NotNull(await Repository.GetByRefreshTokenHashAsync("hash-1"));
    }

    [Fact]
    public async Task SignOut_EndsTheSessionAndClearsItsToken_AndIsRepeatable()
    {
        var session = await AddSessionAsync();

        await Repository.SignOutAsync(session.Id, Now);
        await Repository.SignOutAsync(session.Id, Now);

        Assert.False(await Repository.IsSignedInAsync(session.Id, session.UserId));
        Assert.Null(await Repository.GetByRefreshTokenHashAsync("hash-1"));
        Assert.False(
            await Repository.TryRotateRefreshTokenAsync(
                session.Id,
                "hash-1",
                "hash-2",
                Now.AddDays(60),
                Now
            )
        );
    }
}
