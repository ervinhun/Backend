using System.Collections.Concurrent;
using DataAccess.Entities;
using DataAccess.Interfaces;

namespace Tests.Fakes;

public class InMemoryUserRepository : IUserRepository
{
    private readonly ConcurrentDictionary<Guid, User> _users = new();

    public IReadOnlyCollection<User> All => _users.Values.ToList();

    public Task<User?> GetByEmailAsync(string email, CancellationToken ct = default) =>
        Task.FromResult(_users.Values.FirstOrDefault(u => u.Email == email && u.DeletedAt == null));

    public Task<User?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        Task.FromResult(_users.TryGetValue(id, out var u) && u.DeletedAt == null ? u : null);

    public Task<bool> EmailExistsAsync(string email, CancellationToken ct = default) =>
        Task.FromResult(_users.Values.Any(u => u.Email == email));

    public Task<User> AddAsync(User user, CancellationToken ct = default)
    {
        user.Id = Guid.NewGuid();
        user.CreatedAt = user.UpdatedAt = DateTimeOffset.UtcNow;
        _users[user.Id] = user;
        return Task.FromResult(user);
    }
}
