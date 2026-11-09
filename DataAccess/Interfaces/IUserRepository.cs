using DataAccess.Entities;

namespace DataAccess.Interfaces;

public interface IUserRepository
{
    /// <summary>Finds a non-deleted user by exact (already normalized) email.</summary>
    Task<User?> GetByEmailAsync(string email, CancellationToken ct = default);

    Task<User?> GetByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>True if any user, including soft-deleted ones, holds this email.</summary>
    Task<bool> EmailExistsAsync(string email, CancellationToken ct = default);

    /// <summary>Finds the non-deleted user an active, non-deleted external login belongs to.</summary>
    Task<User?> GetByLoginAsync(string provider, string providerUserId, CancellationToken ct = default);

    /// <summary>Inserts the user together with any entries in <see cref="User.Logins"/>.</summary>
    Task<User> AddAsync(User user, CancellationToken ct = default);

    /// <summary>Links an external login to an existing user.</summary>
    Task AddLoginAsync(UserLogin login, CancellationToken ct = default);
}
