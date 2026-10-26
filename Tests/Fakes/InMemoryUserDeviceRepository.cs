using System.Collections.Concurrent;
using DataAccess.Entities;
using DataAccess.Interfaces;

namespace Tests.Fakes;

public class InMemoryUserDeviceRepository : IUserDeviceRepository
{
    private readonly ConcurrentDictionary<Guid, UserDevice> _devices = new();
    private readonly object _lock = new();

    public IReadOnlyCollection<UserDevice> All => _devices.Values.ToList();

    public Task<UserDevice> AddAsync(UserDevice device, CancellationToken ct = default)
    {
        device.Id = Guid.NewGuid();
        _devices[device.Id] = device;
        return Task.FromResult(device);
    }

    public Task<UserDevice?> GetByRefreshTokenHashAsync(string tokenHash, CancellationToken ct = default) =>
        Task.FromResult(_devices.Values.FirstOrDefault(d =>
            d.DeletedAt is null && d.RefreshTokenHash == tokenHash));

    public Task<bool> TryRotateRefreshTokenAsync(
        Guid deviceId, string currentHash, string newHash, DateTimeOffset expiresAt, DateTimeOffset now,
        CancellationToken ct = default)
    {
        lock (_lock)
        {
            if (!_devices.TryGetValue(deviceId, out var d) || d.DeletedAt is not null || d.RefreshTokenHash != currentHash)
                return Task.FromResult(false);
            d.RefreshTokenHash = newHash;
            d.RefreshTokenExpiresAt = expiresAt;
            d.LastSeenAt = now;
            return Task.FromResult(true);
        }
    }

    public Task<bool> IsSignedInAsync(Guid deviceId, Guid userId, CancellationToken ct = default) =>
        Task.FromResult(_devices.TryGetValue(deviceId, out var d) && d.UserId == userId && d.DeletedAt is null);

    public Task SignOutAsync(Guid deviceId, DateTimeOffset now, CancellationToken ct = default)
    {
        lock (_lock)
        {
            if (_devices.TryGetValue(deviceId, out var d) && d.DeletedAt is null)
            {
                d.DeletedAt = now;
                d.IsActive = false;
                d.RefreshTokenHash = null;
                d.RefreshTokenExpiresAt = null;
            }
        }
        return Task.CompletedTask;
    }
}
