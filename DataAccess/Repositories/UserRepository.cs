using DataAccess.Entities;
using DataAccess.Exceptions;
using DataAccess.Interfaces;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace DataAccess.Repositories;

public class UserRepository(AppDbContext db) : IUserRepository
{
    public Task<User?> GetByEmailAsync(string email, CancellationToken ct = default) =>
        db.Users.AsNoTracking()
            .FirstOrDefaultAsync(u => u.Email == email && u.DeletedAt == null, ct);

    public Task<User?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        db.Users.AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == id && u.DeletedAt == null, ct);

    // users.email is UNIQUE across soft-deleted rows too, so check all of them.
    public Task<bool> EmailExistsAsync(string email, CancellationToken ct = default) =>
        db.Users.AnyAsync(u => u.Email == email, ct);

    public Task<User?> GetByLoginAsync(string provider, string providerUserId, CancellationToken ct = default) =>
        db.UserLogins.AsNoTracking()
            .Where(l => l.Provider == provider && l.ProviderUserId == providerUserId && l.IsActive && l.DeletedAt == null)
            .Join(db.Users.Where(u => u.DeletedAt == null), l => l.UserId, u => u.Id, (_, u) => u)
            .FirstOrDefaultAsync(ct);

    public async Task<User> AddAsync(User user, CancellationToken ct = default)
    {
        db.Users.Add(user);
        await SaveAsync("A user with this email or external login already exists.", ct);
        return user;
    }

    public async Task AddLoginAsync(UserLogin login, CancellationToken ct = default)
    {
        db.UserLogins.Add(login);
        await SaveAsync("This external login is already linked.", ct);
    }

    private async Task SaveAsync(string duplicateMessage, CancellationToken ct)
    {
        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            throw new DuplicateKeyException(duplicateMessage, ex);
        }
    }
}
