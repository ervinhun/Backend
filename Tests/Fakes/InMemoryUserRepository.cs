using System.Collections.Concurrent;
using DataAccess.Entities;
using DataAccess.Exceptions;
using DataAccess.Interfaces;

namespace Tests.Fakes;

public class InMemoryUserRepository : IUserRepository
{
    private readonly ConcurrentDictionary<Guid, User> _users = new();
    private readonly ConcurrentBag<UserLogin> _logins = [];

    public IReadOnlyCollection<User> All => _users.Values.ToList();
    public IReadOnlyCollection<UserLogin> Logins => _logins.ToList();

    public Task<User?> GetByEmailAsync(string email, CancellationToken ct = default) =>
        Task.FromResult(_users.Values.FirstOrDefault(u => u.Email == email && u.DeletedAt == null));

    public Task<User?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        Task.FromResult(_users.TryGetValue(id, out var u) && u.DeletedAt == null ? u : null);

    public Task<bool> EmailExistsAsync(string email, CancellationToken ct = default) =>
        Task.FromResult(_users.Values.Any(u => u.Email == email));

    public Task<User?> GetByLoginAsync(string provider, string providerUserId, CancellationToken ct = default)
    {
        var login = FindLogin(provider, providerUserId);
        return login is not { IsActive: true } ? Task.FromResult<User?>(null) : GetByIdAsync(login.UserId, ct);
    }

    public Task<User> AddAsync(User user, CancellationToken ct = default)
    {
        user.Id = Guid.NewGuid();
        user.CreatedAt = user.UpdatedAt = DateTimeOffset.UtcNow;
        foreach (var login in user.Logins)
        {
            login.UserId = user.Id;
            Store(login);
        }
        _users[user.Id] = user;
        return Task.FromResult(user);
    }

    public Task AddLoginAsync(UserLogin login, CancellationToken ct = default)
    {
        Store(login);
        return Task.CompletedTask;
    }

    // Mirrors the uq_user_logins_provider_user unique index.
    private void Store(UserLogin login)
    {
        if (FindLogin(login.Provider, login.ProviderUserId) is not null)
            throw new DuplicateKeyException("This external login is already linked.", new Exception());
        login.Id = Guid.NewGuid();
        _logins.Add(login);
    }

    private UserLogin? FindLogin(string provider, string providerUserId) =>
        _logins.FirstOrDefault(l => l.Provider == provider && l.ProviderUserId == providerUserId && l.DeletedAt == null);
}
