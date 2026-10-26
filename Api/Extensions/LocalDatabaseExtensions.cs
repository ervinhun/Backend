using System.ComponentModel;
using System.Diagnostics;

namespace Api.Extensions;

public static class LocalDatabaseExtensions
{
    private const string ComposeFileName = "docker-compose.yml";

    /// <summary>
    /// In Development, starts the local Postgres container from docker-compose.yml and applies the
    /// Flyway migrations before the API starts, so `dotnet run` works without starting compose by hand.
    /// Controlled by LocalDatabase:AutoStart (off inside the compose `api` container, which already has its db).
    /// </summary>
    public static WebApplicationBuilder EnsureLocalDatabase(this WebApplicationBuilder builder)
    {
        if (!builder.Environment.IsDevelopment() || !builder.Configuration.GetValue("LocalDatabase:AutoStart", false))
            return builder;

        var composeFile = FindComposeFile(builder.Environment.ContentRootPath)
            ?? throw new InvalidOperationException(
                $"LocalDatabase:AutoStart is on but no {ComposeFileName} was found above {builder.Environment.ContentRootPath}.");

        // `run flyway` starts the `db` service (waiting for its healthcheck), migrates it, and
        // leaves the db running for the API. Re-running it is a no-op once migrations are applied.
        var startInfo = new ProcessStartInfo("docker")
        {
            ArgumentList = { "compose", "-f", composeFile, "run", "--rm", "flyway" },
            WorkingDirectory = Path.GetDirectoryName(composeFile),
        };

        Console.WriteLine("Starting local database and applying migrations (docker compose run --rm flyway)...");
        try
        {
            using var process = Process.Start(startInfo)!;
            process.WaitForExit();
            if (process.ExitCode != 0)
                throw new InvalidOperationException(
                    $"Starting the local database failed (docker compose exited with {process.ExitCode}). " +
                    "Is Docker running and is .env filled in? Set LocalDatabase__AutoStart=false to skip.");
        }
        catch (Win32Exception ex)
        {
            throw new InvalidOperationException(
                "Could not run `docker` to start the local database. Install/start Docker, " +
                "or set LocalDatabase__AutoStart=false and provide your own database.", ex);
        }

        return builder;
    }

    private static string? FindComposeFile(string startDirectory)
    {
        for (var dir = new DirectoryInfo(startDirectory); dir is not null; dir = dir.Parent)
        {
            var candidate = Path.Combine(dir.FullName, ComposeFileName);
            if (File.Exists(candidate))
                return candidate;
        }
        return null;
    }
}
