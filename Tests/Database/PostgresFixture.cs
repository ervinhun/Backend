using DataAccess;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Respawn;
using Respawn.Graph;
using Testcontainers.PostgreSql;

namespace Tests.Database;

/// <summary>
/// One Postgres container per test run, migrated from the real db/migrations files.
/// <see cref="ResetAsync"/> empties the data tables between tests but keeps the seeded reference data.
/// </summary>
public class PostgresFixture : IAsyncLifetime
{
    private static readonly Table[] SeededTables =
    [
        new("product_categories"),
        new("product_types"),
    ];

    // Same major version as docker-compose.yml.
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder("postgres:17").Build();
    private Respawner _respawner = null!;

    public string ConnectionString => _container.GetConnectionString();

    public async Task InitializeAsync()
    {
        await _container.StartAsync();

        await using var connection = await OpenConnectionAsync();
        foreach (var migration in MigrationFiles())
        {
            await using var command = new NpgsqlCommand(
                await File.ReadAllTextAsync(migration),
                connection
            );
            await command.ExecuteNonQueryAsync();
        }

        _respawner = await Respawner.CreateAsync(
            connection,
            new RespawnerOptions
            {
                DbAdapter = DbAdapter.Postgres,
                SchemasToInclude = ["public"],
                TablesToIgnore = SeededTables,
            }
        );
    }

    public async Task ResetAsync()
    {
        await using var connection = await OpenConnectionAsync();
        await _respawner.ResetAsync(connection);
    }

    // A fresh context per call, so each one has its own change tracker like a real request would.
    public AppDbContext CreateDbContext() =>
        new(new DbContextOptionsBuilder<AppDbContext>().UseNpgsql(ConnectionString).Options);

    public Task DisposeAsync() => _container.DisposeAsync().AsTask();

    private async Task<NpgsqlConnection> OpenConnectionAsync()
    {
        var connection = new NpgsqlConnection(ConnectionString);
        await connection.OpenAsync();
        return connection;
    }

    // Numeric order (V2 before V10), as Flyway applies them.
    private static IEnumerable<string> MigrationFiles() =>
        Directory.GetFiles(FindMigrationsDirectory(), "V*__*.sql").OrderBy(MigrationVersion);

    // "V12__add_fridges.sql" -> 12
    private static int MigrationVersion(string file)
    {
        var name = Path.GetFileName(file);
        return int.Parse(name[1..name.IndexOf("__", StringComparison.Ordinal)]);
    }

    private static string FindMigrationsDirectory()
    {
        for (
            var dir = new DirectoryInfo(AppContext.BaseDirectory);
            dir is not null;
            dir = dir.Parent
        )
        {
            var candidate = Path.Combine(dir.FullName, "db", "migrations");
            if (Directory.Exists(candidate))
                return candidate;
        }
        throw new DirectoryNotFoundException(
            "db/migrations was not found above " + AppContext.BaseDirectory
        );
    }
}
