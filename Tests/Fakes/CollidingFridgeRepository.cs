using DataAccess.Entities;
using DataAccess.Exceptions;
using DataAccess.Interfaces;

namespace Tests.Fakes;

/// <summary>
/// Wraps a real repository and makes the first <paramref name="collisions"/> attempts to save an invite code
/// fail as if another fridge already had it. Real codes are random, so a test can't cause that on its own.
/// </summary>
public class CollidingFridgeRepository(IFridgeRepository inner, int collisions) : IFridgeRepository
{
    public int AddCalls { get; private set; }
    public int SetInviteCodeCalls { get; private set; }

    public Task<AddFridgeResult> AddAsync(
        Fridge fridge,
        FridgeMember owner,
        int maxOwnedFridges,
        CancellationToken ct = default
    ) =>
        ++AddCalls <= collisions
            ? throw Collision()
            : inner.AddAsync(fridge, owner, maxOwnedFridges, ct);

    public Task<bool> SetInviteCodeAsync(
        Guid fridgeId,
        string inviteCode,
        CancellationToken ct = default
    ) =>
        ++SetInviteCodeCalls <= collisions
            ? throw Collision()
            : inner.SetInviteCodeAsync(fridgeId, inviteCode, ct);

    public Task<IReadOnlyList<FridgeWithMembership>> GetForUserAsync(
        Guid userId,
        CancellationToken ct = default
    ) => inner.GetForUserAsync(userId, ct);

    public Task<FridgeWithMembership?> GetAsync(
        Guid fridgeId,
        Guid userId,
        CancellationToken ct = default
    ) => inner.GetAsync(fridgeId, userId, ct);

    public Task<bool> UpdateNameAsync(Guid fridgeId, string name, CancellationToken ct = default) =>
        inner.UpdateNameAsync(fridgeId, name, ct);

    public Task<bool> DeleteAsync(
        Guid fridgeId,
        DateTimeOffset now,
        CancellationToken ct = default
    ) => inner.DeleteAsync(fridgeId, now, ct);

    public Task<IReadOnlyList<FridgeMember>> GetMembersAsync(
        Guid fridgeId,
        CancellationToken ct = default
    ) => inner.GetMembersAsync(fridgeId, ct);

    public Task<FridgeMember?> GetMemberAsync(
        Guid fridgeId,
        Guid userId,
        CancellationToken ct = default
    ) => inner.GetMemberAsync(fridgeId, userId, ct);

    public Task<JoinFridgeResult> JoinAsync(
        string inviteCode,
        Guid userId,
        CancellationToken ct = default
    ) => inner.JoinAsync(inviteCode, userId, ct);

    public Task<bool> UpdateMemberRightsAsync(
        Guid fridgeId,
        Guid userId,
        bool canPut,
        bool canUse,
        DateTimeOffset? accessExpiresAt,
        CancellationToken ct = default
    ) => inner.UpdateMemberRightsAsync(fridgeId, userId, canPut, canUse, accessExpiresAt, ct);

    public Task<bool> RemoveMemberAsync(
        Guid fridgeId,
        Guid userId,
        DateTimeOffset now,
        CancellationToken ct = default
    ) => inner.RemoveMemberAsync(fridgeId, userId, now, ct);

    private static DuplicateKeyException Collision() =>
        new("A fridge with this invite code already exists.", new Exception("simulated"));
}
