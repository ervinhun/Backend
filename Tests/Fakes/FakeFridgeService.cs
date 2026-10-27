using BusinessLogic.Dtos.Fridge;
using BusinessLogic.Interfaces;

namespace Tests.Fakes;

public class FakeFridgeService : IFridgeService
{
    public Dictionary<(Guid FridgeId, Guid UserId), FridgeMemberResponse> Members { get; } = new();
    public List<string> Calls { get; } = [];

    /// <summary>The token the last recorded call was given.</summary>
    public CancellationToken LastToken { get; private set; }

    public void AddMember(Guid fridgeId, Guid userId, bool isOwner = false) =>
        Members[(fridgeId, userId)] = new FridgeMemberResponse
        {
            UserId = userId,
            DisplayName = "Name",
            IsOwner = isOwner,
            CanPut = true,
            CanUse = true,
            JoinedAt = DateTimeOffset.UtcNow,
        };

    public Task<FridgeResponse> CreateAsync(
        Guid userId,
        SaveFridgeRequest request,
        CancellationToken ct = default
    )
    {
        Record(nameof(CreateAsync), ct);
        return Task.FromResult(Fridge(Guid.NewGuid(), request.Name));
    }

    public Task<IReadOnlyList<FridgeResponse>> GetForUserAsync(
        Guid userId,
        CancellationToken ct = default
    )
    {
        Record(nameof(GetForUserAsync), ct);
        return Task.FromResult<IReadOnlyList<FridgeResponse>>([Fridge(Guid.NewGuid(), "Mine")]);
    }

    public Task<FridgeResponse> GetAsync(Guid fridgeId, Guid userId, CancellationToken ct = default)
    {
        Record(nameof(GetAsync), ct);
        return Task.FromResult(Fridge(fridgeId, "Fridge"));
    }

    public Task<FridgeResponse> UpdateAsync(
        Guid fridgeId,
        Guid userId,
        SaveFridgeRequest request,
        CancellationToken ct = default
    )
    {
        Record(nameof(UpdateAsync), ct);
        return Task.FromResult(Fridge(fridgeId, request.Name));
    }

    public Task DeleteAsync(Guid fridgeId, CancellationToken ct = default)
    {
        Record(nameof(DeleteAsync), ct);
        return Task.CompletedTask;
    }

    public Task<FridgeResponse> RegenerateInviteCodeAsync(
        Guid fridgeId,
        Guid userId,
        CancellationToken ct = default
    )
    {
        Record(nameof(RegenerateInviteCodeAsync), ct);
        var fridge = Fridge(fridgeId, "Fridge");
        fridge.InviteCode = "NEWCODE";
        return Task.FromResult(fridge);
    }

    public Task<FridgeResponse> JoinAsync(
        Guid userId,
        JoinFridgeRequest request,
        CancellationToken ct = default
    )
    {
        Record(nameof(JoinAsync), ct);
        return Task.FromResult(Fridge(Guid.NewGuid(), "Joined"));
    }

    public Task<IReadOnlyList<FridgeMemberResponse>> GetMembersAsync(
        Guid fridgeId,
        CancellationToken ct = default
    )
    {
        Record(nameof(GetMembersAsync), ct);
        return Task.FromResult<IReadOnlyList<FridgeMemberResponse>>(
            Members.Where(m => m.Key.FridgeId == fridgeId).Select(m => m.Value).ToList()
        );
    }

    public Task<FridgeMemberResponse?> GetMemberAsync(
        Guid fridgeId,
        Guid userId,
        CancellationToken ct = default
    ) => Task.FromResult(Members.GetValueOrDefault((fridgeId, userId)));

    public Task<FridgeMemberResponse> UpdateMemberRightsAsync(
        Guid fridgeId,
        Guid userId,
        UpdateMemberRightsRequest request,
        CancellationToken ct = default
    )
    {
        Record(nameof(UpdateMemberRightsAsync), ct);
        var member = Members[(fridgeId, userId)];
        member.CanPut = request.CanPut;
        member.CanUse = request.CanUse;
        member.AccessExpiresAt = request.AccessExpiresAt;
        return Task.FromResult(member);
    }

    public Task RemoveMemberAsync(Guid fridgeId, Guid userId, CancellationToken ct = default)
    {
        Record(nameof(RemoveMemberAsync), ct);
        return Task.CompletedTask;
    }

    private void Record(string call, CancellationToken ct)
    {
        Calls.Add(call);
        LastToken = ct;
    }

    private static FridgeResponse Fridge(Guid id, string name) =>
        new()
        {
            Id = id,
            Name = name,
            MaxMembers = 50,
            MaxContainers = 10,
            MemberCount = 1,
            IsOwner = true,
            CanPut = true,
            CanUse = true,
            CreatedAt = DateTimeOffset.UtcNow,
        };
}
