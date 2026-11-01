namespace DataAccess.Entities;

/// <summary>Maps the <c>user_logins</c> table (db/migrations/V13__user_logins.sql).</summary>
public class UserLogin
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }

    /// <summary>The provider's stable user id (the ID token's <c>sub</c>).</summary>
    public string ProviderUserId { get; set; } = string.Empty;

    /// <summary>One of <see cref="AuthProviders.Google"/> or <see cref="AuthProviders.Apple"/>.</summary>
    public string Provider { get; set; } = string.Empty;

    /// <summary>False once unlinked: the provider can no longer be used to sign in.</summary>
    public bool IsActive { get; set; } = true;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }
}
