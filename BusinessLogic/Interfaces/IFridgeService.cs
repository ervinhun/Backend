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
    Task DeleteAsync(Guid fridgeId, CancellationToken ct = default);
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
        CancellationToken ct = default
    );
    Task<FridgeMemberResponse?> GetMemberAsync(
        Guid fridgeId,
        Guid userId,
        CancellationToken ct = default
    );
    Task<FridgeMemberResponse> UpdateMemberRightsAsync(
        Guid fridgeId,
        Guid userId,
        UpdateMemberRightsRequest request,
        CancellationToken ct = default
    );
    Task RemoveMemberAsync(Guid fridgeId, Guid userId, CancellationToken ct = default);
}
