namespace DataAccess.Entities;

/// <summary>Maps the <c>revoked_tokens</c> table (db/migrations/V13__revoked_tokens.sql).</summary>
public class RevokedToken
{
    /// <summary>The signed-out access token's <c>jti</c> claim.</summary>
    public Guid Jti { get; set; }

    public Guid UserId { get; set; }

    /// <summary>When the token would have expired; the row is useless after this.</summary>
    public DateTimeOffset ExpiresAt { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
}
