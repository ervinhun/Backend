namespace DataAccess.Entities;

/// <summary>
/// Maps the <c>user_sessions</c> table (db/migrations/V2__users.sql, refresh token
/// columns from V13__user_device_refresh_tokens.sql, renamed by
/// V14__rename_user_devices_to_user_sessions.sql). One row per signed-in session;
/// its id is the access token's <c>sid</c>.
/// </summary>
public class UserSession
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }

    /// <summary>Null until the app registers for push notifications.</summary>
    public string? PushToken { get; set; }

    public string? Platform { get; set; }

    /// <summary>The phone model the app reports, e.g. "iPhone 15 Pro". Null until sent.</summary>
    public string? PhoneModel { get; set; }

    public bool IsActive { get; set; } = true;
    public DateTimeOffset? LastSeenAt { get; set; }

    /// <summary>SHA-256 of the refresh token the session holds; the token itself is never stored.</summary>
    public string? RefreshTokenHash { get; set; }

    public DateTimeOffset? RefreshTokenExpiresAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    /// <summary>Set when the session signs out.</summary>
    public DateTimeOffset? DeletedAt { get; set; }
}
