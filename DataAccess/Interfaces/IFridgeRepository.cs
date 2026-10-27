using DataAccess.Entities;

namespace DataAccess.Interfaces;

/// <summary>Fridges and their members (fridges, fridge_members). Deleted fridges and members are never returned.</summary>
public interface IFridgeRepository
{
    /// <summary>
    /// Saves the fridge and its owner membership together, filling in the generated ids.
    /// Throws <see cref="Exceptions.DuplicateKeyException"/> if the invite code is taken. Saves nothing and
    /// returns <see cref="AddFridgeStatus.LimitReached"/> if the owner already owns
    /// <paramref name="maxOwnedFridges"/> fridges. Concurrent creates are counted one at a time.
    /// </summary>
    Task<AddFridgeResult> AddAsync(
        Fridge fridge,
        FridgeMember owner,
        int maxOwnedFridges,
        CancellationToken ct = default
    );

    Task<IReadOnlyList<FridgeWithMembership>> GetForUserAsync(
        Guid userId,
        CancellationToken ct = default
    );

    /// <summary>Null if the fridge doesn't exist or the user isn't a member of it.</summary>
    Task<FridgeWithMembership?> GetAsync(
        Guid fridgeId,
        Guid userId,
        CancellationToken ct = default
    );

    /// <summary>Changes only the name; the limits stay as they are. Returns false if the fridge doesn't exist.</summary>
    Task<bool> UpdateNameAsync(Guid fridgeId, string name, CancellationToken ct = default);

    /// <summary>
    /// Returns false if the fridge doesn't exist.
    /// Throws <see cref="Exceptions.DuplicateKeyException"/> if the invite code is taken.
    /// </summary>
    Task<bool> SetInviteCodeAsync(Guid fridgeId, string inviteCode, CancellationToken ct = default);

    /// <summary>Soft-deletes the fridge with all its members and containers. Returns false if the fridge doesn't exist.</summary>
    Task<bool> DeleteAsync(Guid fridgeId, DateTimeOffset now, CancellationToken ct = default);

    /// <summary>The members of a fridge with their users loaded, owner first.</summary>
    Task<IReadOnlyList<FridgeMember>> GetMembersAsync(
        Guid fridgeId,
        CancellationToken ct = default
    );

    /// <summary>The member with their user loaded, or null if the user isn't in the fridge.</summary>
    Task<FridgeMember?> GetMemberAsync(Guid fridgeId, Guid userId, CancellationToken ct = default);

    /// <summary>
    /// Adds the user to the fridge with this invite code, unless they are already in it or it has reached
    /// its max_members. Concurrent joins are counted one at a time, so the limit can't be passed.
    /// </summary>
    Task<JoinFridgeResult> JoinAsync(
        string inviteCode,
        Guid userId,
        CancellationToken ct = default
    );

    /// <summary>Returns false if the user isn't in the fridge.</summary>
    Task<bool> UpdateMemberRightsAsync(
        Guid fridgeId,
        Guid userId,
        bool canPut,
        bool canUse,
        DateTimeOffset? accessExpiresAt,
        CancellationToken ct = default
    );

    /// <summary>Soft-deletes the membership. Returns false if the user isn't in the fridge.</summary>
    Task<bool> RemoveMemberAsync(
        Guid fridgeId,
        Guid userId,
        DateTimeOffset now,
        CancellationToken ct = default
    );
}
