namespace DataAccess.Entities;

/// <summary>
/// Maps the <c>user_devices</c> table (db/migrations/V2__users.sql, refresh token
/// columns from V13__user_device_refresh_tokens.sql). One row per signed-in device;
/// its id is the access token's <c>sid</c>.
/// </summary>
public class UserDevice
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }

    /// <summary>Null until the app registers for push notifications.</summary>
    public string? PushToken { get; set; }

    public string? Platform { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTimeOffset? LastSeenAt { get; set; }

    /// <summary>SHA-256 of the refresh token the device holds; the token itself is never stored.</summary>
    public string? RefreshTokenHash { get; set; }

    public DateTimeOffset? RefreshTokenExpiresAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    /// <summary>Set when the device signs out.</summary>
    public DateTimeOffset? DeletedAt { get; set; }
}
