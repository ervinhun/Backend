using Microsoft.EntityFrameworkCore;

namespace Tests.Database;

public class PostgresFixtureTests(PostgresFixture database) : DatabaseTest(database)
{
    private async Task<int> CountAsync(string table)
    {
        await using var db = Database.CreateDbContext();
        // Table names are test-controlled constants, never user input, so the interpolation is not injectable.
        return await db
            .Database.SqlQueryRaw<int>($"SELECT count(*)::int AS \"Value\" FROM {table}")
            .SingleAsync();
    }

    [Fact]
    public async Task Migrations_AreApplied_WithSeedData()
    {
        Assert.True(await CountAsync("product_categories") > 0);
        Assert.True(await CountAsync("product_types") > 0);
    }

    [Fact]
    public async Task Reset_RemovesTestData_ButKeepsSeedData()
    {
        await using (var db = Database.CreateDbContext())
            await db.Database.ExecuteSqlRawAsync(
                "INSERT INTO users (email, default_display_name) VALUES ('a@example.com', 'A')"
            );
        Assert.Equal(1, await CountAsync("users"));

        await Database.ResetAsync();

        Assert.Equal(0, await CountAsync("users"));
        Assert.True(await CountAsync("product_types") > 0);
    }
}
