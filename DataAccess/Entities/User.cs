namespace DataAccess.Entities;

/// <summary>Maps the <c>users</c> table (db/migrations/V2__users.sql).</summary>
public class User
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;

    /// <summary>Null for accounts that sign in through an external provider.</summary>
    public string? PasswordHash { get; set; }

    public string AuthProvider { get; set; } = AuthProviders.Password;
    public string DefaultDisplayName { get; set; } = string.Empty;
    public string Locale { get; set; } = "en";
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }
}

public static class AuthProviders
{
    public const string Password = "password";
}
