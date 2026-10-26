using DataAccess.Entities;

namespace DataAccess.Interfaces;

/// <summary>Signed-in devices (user_devices) and their refresh tokens.</summary>
public interface IUserDeviceRepository
{
    Task<UserDevice> AddAsync(UserDevice device, CancellationToken ct = default);

    /// <summary>Finds a signed-in device whose refresh token has this hash.</summary>
    Task<UserDevice?> GetByRefreshTokenHashAsync(string tokenHash, CancellationToken ct = default);

    /// <summary>
    /// Replaces the device's refresh token if it still holds <paramref name="currentHash"/>.
    /// Returns false if not — including when a concurrent request rotated it first.
    /// </summary>
    Task<bool> TryRotateRefreshTokenAsync(
        Guid deviceId, string currentHash, string newHash, DateTimeOffset expiresAt, DateTimeOffset now,
        CancellationToken ct = default);

    Task<bool> IsSignedInAsync(Guid deviceId, Guid userId, CancellationToken ct = default);

    /// <summary>Soft-deletes the device and clears its refresh token. Signing out twice is a no-op.</summary>
    Task SignOutAsync(Guid deviceId, DateTimeOffset now, CancellationToken ct = default);
}
