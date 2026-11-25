using DataAccess;
using Microsoft.EntityFrameworkCore;

namespace Tests.Database;

public class DbInitializerTests(PostgresFixture database) : DatabaseTest(database)
{
    private const string SeededType = "Milk (plain or flavored)";

    [Fact]
    public async Task Seed_WithNothingToAdd_DoesNotLockTheTables()
    {
        // Conflicts with the lock the seeding takes, so it would wait here until the timeout.
        await using var blocker = Database.CreateDbContext();
        await using var transaction = await blocker.Database.BeginTransactionAsync();
        await blocker.Database.ExecuteSqlRawAsync(
            "LOCK TABLE product_categories, product_types IN ROW EXCLUSIVE MODE");

        await using var db = Database.CreateDbContext();
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        await DbInitializer.SeedAsync(db, timeout.Token);
    }

    [Fact]
    public async Task Seed_WithAMissingType_AddsOnlyThatOne()
    {
        await using var db = Database.CreateDbContext();
        var before = await db.ProductTypes.IgnoreQueryFilters().CountAsync();
        Assert.Equal(1, await db.ProductTypes.Where(t => t.Name == SeededType).ExecuteDeleteAsync());

        await DbInitializer.SeedAsync(db);

        Assert.Equal(before, await db.ProductTypes.IgnoreQueryFilters().CountAsync());
        Assert.True(await db.ProductTypes.AnyAsync(t => t.Name == SeededType));
    }

    [Fact]
    public async Task Seed_LeavesADeletedTypeDeleted()
    {
        await using var db = Database.CreateDbContext();
        var type = db.ProductTypes.Where(t => t.Name == SeededType);
        await type.ExecuteUpdateAsync(s => s.SetProperty(t => t.DeletedAt, DateTimeOffset.UtcNow));

        try
        {
            await DbInitializer.SeedAsync(db);

            Assert.False(await type.AnyAsync());
        }
        finally
        {
            await db.ProductTypes.IgnoreQueryFilters().Where(t => t.Name == SeededType)
                .ExecuteUpdateAsync(s => s.SetProperty(t => t.DeletedAt, (DateTimeOffset?)null));
        }
    }
}
