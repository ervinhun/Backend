using DataAccess.Entities;
using DataAccess.Exceptions;
using DataAccess.Interfaces;
using DataAccess.Models.Fridge;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace DataAccess.Repositories;

/// <summary>
/// A member whose access_expires_at has passed counts as no member at all: they can't see the fridge,
/// don't use up one of its max_members, and join again like any newcomer.
/// </summary>
public class FridgeRepository(AppDbContext db, TimeProvider timeProvider) : IFridgeRepository
{
    private const string InviteCodeTakenMessage = "A fridge with this invite code already exists.";

    public async Task<AddFridgeStatus> AddAsync(
        Fridge fridge,
        FridgeMember owner,
        int maxOwnedFridges,
        CancellationToken ct = default
    )
    {
        await using var transaction = await db.Database.BeginTransactionAsync(ct);

        // Locks the owner's row until the commit, so two creates can't both see room for one more.
        await db.Database.ExecuteSqlAsync(
            $"SELECT 1 FROM users WHERE id = {owner.UserId} FOR UPDATE",
            ct
        );
        var owned = await db.FridgeMembers.CountAsync(
            m => m.UserId == owner.UserId && m.IsOwner,
            ct
        );
        if (owned >= maxOwnedFridges)
            return AddFridgeStatus.LimitReached;

        owner.Fridge = fridge;
        db.FridgeMembers.Add(owner);
        try
        {
            await db.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);
        }
        catch (DbUpdateException ex)
            when (ex.InnerException
                    is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation }
            )
        {
            // Stop the failed rows from being saved again by the caller's next attempt.
            db.Entry(owner).State = EntityState.Detached;
            db.Entry(fridge).State = EntityState.Detached;
            throw new DuplicateKeyException(InviteCodeTakenMessage, ex);
        }
        return AddFridgeStatus.Added;
    }

    public async Task<IReadOnlyList<FridgeWithMembership>> GetForUserAsync(
        Guid userId,
        CancellationToken ct = default
    ) => await MembershipsOf(userId, timeProvider.GetUtcNow()).ToListAsync(ct);

    public Task<FridgeWithMembership?> GetAsync(
        Guid fridgeId,
        Guid userId,
        CancellationToken ct = default
    ) => MembershipsOf(userId, timeProvider.GetUtcNow(), fridgeId).FirstOrDefaultAsync(ct);

    public async Task<bool> UpdateNameAsync(
        Guid fridgeId,
        string name,
        CancellationToken ct = default
    ) =>
        await db
            .Fridges.Where(f => f.Id == fridgeId)
            .ExecuteUpdateAsync(s => s.SetProperty(f => f.Name, name), ct) == 1;

    public async Task<bool> SetInviteCodeAsync(
        Guid fridgeId,
        string inviteCode,
        CancellationToken ct = default
    )
    {
        try
        {
            return await db
                    .Fridges.Where(f => f.Id == fridgeId)
                    .ExecuteUpdateAsync(s => s.SetProperty(f => f.InviteCode, inviteCode), ct) == 1;
        }
        // ExecuteUpdate doesn't wrap provider exceptions in a DbUpdateException.
        catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.UniqueViolation)
        {
            throw new DuplicateKeyException(InviteCodeTakenMessage, ex);
        }
    }

    public async Task<bool> DeleteAsync(
        Guid fridgeId,
        DateTimeOffset now,
        CancellationToken ct = default
    )
    {
        await using var transaction = await db.Database.BeginTransactionAsync(ct);

        var deleted = await db
            .Fridges.Where(f => f.Id == fridgeId)
            .ExecuteUpdateAsync(s => s.SetProperty(f => f.DeletedAt, now), ct);
        if (deleted == 0)
            return false;

        await db
            .FridgeMembers.Where(m => m.FridgeId == fridgeId)
            .ExecuteUpdateAsync(s => s.SetProperty(m => m.DeletedAt, now), ct);
        await db
            .Containers.Where(c => c.FridgeId == fridgeId)
            .ExecuteUpdateAsync(s => s.SetProperty(c => c.DeletedAt, now), ct);

        await transaction.CommitAsync(ct);
        return true;
    }

    public async Task<IReadOnlyList<FridgeMember>> GetMembersAsync(
        Guid fridgeId,
        CancellationToken ct = default
    ) =>
        await ActiveMembers(timeProvider.GetUtcNow())
            .Where(m => m.FridgeId == fridgeId)
            .OrderByDescending(m => m.IsOwner)
            .ThenBy(m => m.CreatedAt)
            .ToListAsync(ct);

    public Task<FridgeMember?> GetMemberAsync(
        Guid fridgeId,
        Guid userId,
        CancellationToken ct = default
    ) =>
        ActiveMembers(timeProvider.GetUtcNow())
            .FirstOrDefaultAsync(m => m.FridgeId == fridgeId && m.UserId == userId, ct);

    public async Task<JoinFridgeResult> JoinAsync(
        string inviteCode,
        Guid userId,
        CancellationToken ct = default
    )
    {
        await using var transaction = await db.Database.BeginTransactionAsync(ct);

        // Locks the fridge row until the commit, so two joins can't both see a free slot.
        // Raw SQL, so the deleted_at check is spelled out to keep deleted fridges from being locked.
        var fridge = await db
            .Fridges.FromSql(
                $"SELECT * FROM fridges WHERE invite_code = {inviteCode} AND deleted_at IS NULL FOR UPDATE"
            )
            .AsNoTracking()
            .SingleOrDefaultAsync(ct);
        if (fridge is null)
            return new JoinFridgeResult(JoinFridgeStatus.UnknownCode, Guid.Empty);

        var now = timeProvider.GetUtcNow();
        var members = db.FridgeMembers.Where(m =>
            m.FridgeId == fridge.Id && (m.AccessExpiresAt == null || m.AccessExpiresAt > now)
        );
        if (await members.AnyAsync(m => m.UserId == userId, ct))
            return new JoinFridgeResult(JoinFridgeStatus.AlreadyMember, fridge.Id);
        if (await members.CountAsync(ct) >= fridge.MaxMembers)
            return new JoinFridgeResult(JoinFridgeStatus.Full, fridge.Id);

        // An earlier, expired membership would still hold the one-row-per-user unique index.
        await db
            .FridgeMembers.Where(m => m.FridgeId == fridge.Id && m.UserId == userId)
            .ExecuteUpdateAsync(s => s.SetProperty(m => m.DeletedAt, now), ct);

        db.FridgeMembers.Add(new FridgeMember { FridgeId = fridge.Id, UserId = userId });
        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        return new JoinFridgeResult(JoinFridgeStatus.Joined, fridge.Id);
    }

    public async Task<bool> UpdateMemberRightsAsync(
        Guid fridgeId,
        Guid userId,
        bool canPut,
        bool canUse,
        DateTimeOffset? accessExpiresAt,
        CancellationToken ct = default
    ) =>
        await db
            .FridgeMembers.Where(m => m.FridgeId == fridgeId && m.UserId == userId)
            .ExecuteUpdateAsync(
                s =>
                    s.SetProperty(m => m.CanPut, canPut)
                        .SetProperty(m => m.CanUse, canUse)
                        .SetProperty(m => m.AccessExpiresAt, accessExpiresAt),
                ct
            ) == 1;

    public async Task<bool> RemoveMemberAsync(
        Guid fridgeId,
        Guid userId,
        DateTimeOffset now,
        CancellationToken ct = default
    ) =>
        await db
            .FridgeMembers.Where(m => m.FridgeId == fridgeId && m.UserId == userId)
            .ExecuteUpdateAsync(s => s.SetProperty(m => m.DeletedAt, now), ct) == 1;

    // Members whose access hasn't expired, of fridges that haven't been deleted, with their users.
    // Deleted rows are already left out by the query filters on Fridge and FridgeMember.
    private IQueryable<FridgeMember> ActiveMembers(DateTimeOffset now) =>
        db
            .FridgeMembers.AsNoTracking()
            .Include(m => m.User)
            .Where(m =>
                (m.AccessExpiresAt == null || m.AccessExpiresAt > now)
                && db.Fridges.Any(f => f.Id == m.FridgeId)
            );

    // Filtering and ordering happen before the select: EF can't translate them on top of the constructor call.
    private IQueryable<FridgeWithMembership> MembershipsOf(
        Guid userId,
        DateTimeOffset now,
        Guid? fridgeId = null
    ) =>
        from fridgeMember in db.FridgeMembers.AsNoTracking()
        join fridge in db.Fridges.AsNoTracking() on fridgeMember.FridgeId equals fridge.Id
        where
            fridgeMember.UserId == userId
            && (fridgeMember.AccessExpiresAt == null || fridgeMember.AccessExpiresAt > now)
            && (fridgeId == null || fridge.Id == fridgeId)
        orderby fridge.CreatedAt
        select new FridgeWithMembership(
            fridge,
            fridgeMember,
            db.FridgeMembers.Count(x =>
                x.FridgeId == fridge.Id && (x.AccessExpiresAt == null || x.AccessExpiresAt > now)
            )
        );
}
