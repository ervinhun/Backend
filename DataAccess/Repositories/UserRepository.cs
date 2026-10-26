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

    public async Task<User> AddAsync(User user, CancellationToken ct = default)
    {
        db.Users.Add(user);
        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            throw new DuplicateKeyException("A user with this email already exists.", ex);
        }
        return user;
    }
}
