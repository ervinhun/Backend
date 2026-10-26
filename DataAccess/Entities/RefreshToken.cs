namespace DataAccess.Entities;

/// <summary>Maps the <c>refresh_tokens</c> table (db/migrations/V14__refresh_tokens.sql).</summary>
public class RefreshToken
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }

    /// <summary>Shared by every token rotated from one sign-in; the access token's <c>sid</c>.</summary>
    public Guid FamilyId { get; set; }

    /// <summary>SHA-256 of the token the client holds; the token itself is never stored.</summary>
    public string TokenHash { get; set; } = string.Empty;

    public DateTimeOffset ExpiresAt { get; set; }
    public DateTimeOffset? RevokedAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}
