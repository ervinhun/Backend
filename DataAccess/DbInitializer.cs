using System.Text.Json;
using DataAccess.Entities;
using Microsoft.EntityFrameworkCore;

namespace DataAccess;

/// <summary>
/// Adds the food types from the USDA FoodKeeper data (Seed/foodkeeper.json) that the database doesn't have yet.
/// Runs on every startup, after the Flyway migrations. Rows are matched by category and name, deleted ones
/// included, so types that were edited or deleted are left as they are.
/// </summary>
public static class DbInitializer
{
    public static async Task SeedAsync(AppDbContext db, CancellationToken ct = default)
    {
        await using var stream = typeof(DbInitializer).Assembly.GetManifestResourceStream("foodkeeper.json")
            ?? throw new InvalidOperationException("The embedded resource 'foodkeeper.json' is missing from DataAccess.");
        var seed = await JsonSerializer.DeserializeAsync<List<SeedCategory>>(
            stream, JsonSerializerOptions.Web, ct) ?? [];

        // On most startups there is nothing to add, and no need to lock.
        var existingTypes = await GetExistingTypesAsync(db, ct);
        if (seed.All(c => c.Types.All(t => existingTypes.Contains((c.Name, t.Name)))))
            return;

        await using var transaction = await db.Database.BeginTransactionAsync(ct);

        // Makes an instance starting at the same time wait here, then find everything already added.
        await db.Database.ExecuteSqlRawAsync(
            "LOCK TABLE product_categories, product_types IN SHARE ROW EXCLUSIVE MODE", ct);

        // Read again under the lock: another instance may have added them while this one waited.
        existingTypes = await GetExistingTypesAsync(db, ct);
        var categories = await db.ProductCategories.IgnoreQueryFilters().ToDictionaryAsync(c => c.Name, ct);

        foreach (var seedCategory in seed)
        {
            if (!categories.TryGetValue(seedCategory.Name, out var category))
            {
                category = new ProductCategory { Name = seedCategory.Name };
                db.ProductCategories.Add(category);
            }

            foreach (var seedType in seedCategory.Types)
            {
                if (existingTypes.Contains((seedCategory.Name, seedType.Name)))
                    continue;

                db.ProductTypes.Add(new ProductType
                {
                    Category = category,
                    Name = seedType.Name,
                    DefaultExpiryDays = seedType.FridgeDays ?? seedType.PantryDays ?? seedType.FreezerDays
                        ?? throw new InvalidDataException($"Seeded type '{seedType.Name}' has no expiry days."),
                    PantryExpiryDays = seedType.PantryDays,
                    FridgeExpiryDays = seedType.FridgeDays,
                    FreezerExpiryDays = seedType.FreezerDays,
                });
            }
        }

        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
    }

    private static async Task<HashSet<(string Category, string Name)>> GetExistingTypesAsync(
        AppDbContext db, CancellationToken ct) =>
        (await db.ProductTypes.IgnoreQueryFilters()
            .Where(t => t.Category != null)
            .Select(t => new { Category = t.Category!.Name, t.Name })
            .ToListAsync(ct))
        .Select(t => (t.Category, t.Name))
        .ToHashSet();

    private sealed record SeedCategory(string Name, List<SeedType> Types);

    private sealed record SeedType(string Name, int? PantryDays, int? FridgeDays, int? FreezerDays);
}
