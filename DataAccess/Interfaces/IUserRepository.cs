using DataAccess.Entities;

namespace DataAccess.Interfaces;

public interface IUserRepository
{
    /// <summary>Finds a non-deleted user by exact (already normalized) email.</summary>
    Task<User?> GetByEmailAsync(string email, CancellationToken ct = default);

    Task<User?> GetByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>True if any user, including soft-deleted ones, holds this email.</summary>
    Task<bool> EmailExistsAsync(string email, CancellationToken ct = default);

    Task<User> AddAsync(User user, CancellationToken ct = default);
}
