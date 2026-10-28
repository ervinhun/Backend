using BusinessLogic.Dtos.Fridge;

namespace BusinessLogic.Interfaces;

public interface IFridgeService
{
    Task<FridgeResponse> CreateAsync(
        Guid userId,
        SaveFridgeRequest request,
        CancellationToken ct = default
    );
    Task<IReadOnlyList<FridgeResponse>> GetForUserAsync(
        Guid userId,
        CancellationToken ct = default
    );
    Task<FridgeResponse> GetAsync(Guid fridgeId, Guid userId, CancellationToken ct = default);
    Task<FridgeResponse> UpdateAsync(
        Guid fridgeId,
        Guid userId,
        SaveFridgeRequest request,
        CancellationToken ct = default
    );
    Task DeleteAsync(Guid fridgeId, Guid userId, CancellationToken ct = default);
    Task<FridgeResponse> RegenerateInviteCodeAsync(
        Guid fridgeId,
        Guid userId,
        CancellationToken ct = default
    );

    /// <summary>Adds the user to the fridge the invite code belongs to. Throws if the code is unknown or the fridge is full.</summary>
    Task<FridgeResponse> JoinAsync(
        Guid userId,
        JoinFridgeRequest request,
        CancellationToken ct = default
    );

    Task<IReadOnlyList<FridgeMemberResponse>> GetMembersAsync(
        Guid fridgeId,
        Guid userId,
        CancellationToken ct = default
    );

    /// <summary>Owner only. <paramref name="memberId"/> is the member whose rights change.</summary>
    Task<FridgeMemberResponse> UpdateMemberRightsAsync(
        Guid fridgeId,
        Guid userId,
        Guid memberId,
        UpdateMemberRightsRequest request,
        CancellationToken ct = default
    );

    /// <summary>Members can remove themselves; only the owner can remove others.</summary>
    Task RemoveMemberAsync(Guid fridgeId, Guid userId, Guid memberId, CancellationToken ct = default);
}
