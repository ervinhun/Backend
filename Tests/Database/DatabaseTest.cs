namespace Tests.Database;

[CollectionDefinition(Name)]
public class DatabaseCollection : ICollectionFixture<PostgresFixture>
{
    public const string Name = "Database";
}

/// <summary>
/// Base for tests that use the shared Postgres. The Database collection runs them one at a time,
/// and every test starts from the same empty state, so the order they run in doesn't matter.
/// </summary>
[Collection(DatabaseCollection.Name)]
public abstract class DatabaseTest(PostgresFixture database) : IAsyncLifetime
{
    protected PostgresFixture Database { get; } = database;

    public Task InitializeAsync() => Database.ResetAsync();

    public virtual Task DisposeAsync() => Task.CompletedTask;
}
