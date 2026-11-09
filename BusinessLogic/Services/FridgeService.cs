using System.Diagnostics;
using System.Security.Cryptography;
using BusinessLogic.Dtos.Fridge;
using BusinessLogic.Exceptions;
using BusinessLogic.Interfaces;
using BusinessLogic.Mappers;
using DataAccess.Entities;
using DataAccess.Exceptions;
using DataAccess.Interfaces;
using DataAccess.Models.Fridge;

namespace BusinessLogic.Services;

public class FridgeService(
    IFridgeRepository fridges,
    IFridgeLimitDefaults limits,
    TimeProvider timeProvider
) : IFridgeService
{
    private const string FridgeNotFoundMessage = "Fridge not found.";
    private const string MemberNotFoundMessage = "Member not found.";

    // Without 0/O and 1/I, so a code read out loud or off a screenshot can't be misread.
    private const string InviteCodeAlphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
    private const int InviteCodeLength = 8;
    private const int InviteCodeAttempts = 5;

    /// <summary>How many fridges one user may own; ones they only joined don't count.</summary>
    public const int MaxOwnedFridges = 5;

    public async Task<FridgeResponse> CreateAsync(
        Guid userId,
        SaveFridgeRequest request,
        CancellationToken ct = default
    )
    {
        var name = request.Name.Trim();

        return await RetryOnDuplicateCodeAsync(() => AddFridgeAsync(userId, name, ct));
    }

    public async Task<IReadOnlyList<FridgeResponse>> GetForUserAsync(
        Guid userId,
        CancellationToken ct = default
    ) => (await fridges.GetForUserAsync(userId, ct)).Select(x => x.ToResponse()).ToList();

    public async Task<FridgeResponse> GetAsync(
        Guid fridgeId,
        Guid userId,
        CancellationToken ct = default
    ) =>
        (
            await fridges.GetAsync(fridgeId, userId, ct)
            ?? throw new NotFoundException(FridgeNotFoundMessage)
        ).ToResponse();

    public async Task<FridgeResponse> UpdateAsync(
        Guid fridgeId,
        Guid userId,
        SaveFridgeRequest request,
        CancellationToken ct = default
    )
    {
        await RequireMemberAsync(fridgeId, userId, ownerOnly: true, ct);

        if (!await fridges.UpdateNameAsync(fridgeId, request.Name.Trim(), ct))
            throw new NotFoundException(FridgeNotFoundMessage);

        return await GetAsync(fridgeId, userId, ct);
    }

    public async Task DeleteAsync(Guid fridgeId, Guid userId, CancellationToken ct = default)
    {
        await RequireMemberAsync(fridgeId, userId, ownerOnly: true, ct);

        if (!await fridges.DeleteAsync(fridgeId, timeProvider.GetUtcNow(), ct))
            throw new NotFoundException(FridgeNotFoundMessage);
    }

    public async Task<FridgeResponse> RegenerateInviteCodeAsync(
        Guid fridgeId,
        Guid userId,
        CancellationToken ct = default
    )
    {
        await RequireMemberAsync(fridgeId, userId, ownerOnly: true, ct);

        await RetryOnDuplicateCodeAsync(() => SetNewInviteCodeAsync(fridgeId, ct));

        return await GetAsync(fridgeId, userId, ct);
    }

    public async Task<FridgeResponse> JoinAsync(
        Guid userId,
        JoinFridgeRequest request,
        CancellationToken ct = default
    )
    {
        // Codes are generated upper case; people type them in any case.
        var result = await fridges.JoinAsync(
            request.InviteCode.Trim().ToUpperInvariant(),
            userId,
            ct
        );

        return result.Status switch
        {
            JoinFridgeStatus.Joined => await GetAsync(result.FridgeId, userId, ct),
            JoinFridgeStatus.UnknownCode => throw new NotFoundException(
                "No fridge has this invite code."
            ),
            JoinFridgeStatus.AlreadyMember => throw new ConflictException(
                "You are already a member of this fridge."
            ),
            JoinFridgeStatus.Full => throw new ConflictException(
                "This fridge has reached its member limit."
            ),
            _ => throw new InvalidOperationException($"Unexpected join result {result.Status}."),
        };
    }

    public async Task<IReadOnlyList<FridgeMemberResponse>> GetMembersAsync(
        Guid fridgeId,
        Guid userId,
        CancellationToken ct = default
    )
    {
        await RequireMemberAsync(fridgeId, userId, ownerOnly: false, ct);

        return (await fridges.GetMembersAsync(fridgeId, ct)).Select(m => m.ToResponse()).ToList();
    }

    public async Task<FridgeMemberResponse> UpdateMemberRightsAsync(
        Guid fridgeId,
        Guid userId,
        Guid memberId,
        UpdateMemberRightsRequest request,
        CancellationToken ct = default
    )
    {
        await RequireMemberAsync(fridgeId, userId, ownerOnly: true, ct);

        var member = await GetNonOwnerAsync(
            fridgeId,
            memberId,
            "The owner's rights cannot be changed.",
            ct
        );

        if (
            !await fridges.UpdateMemberRightsAsync(
                fridgeId,
                memberId,
                request.CanPut,
                request.CanUse,
                request.AccessExpiresAt,
                ct
            )
        )
            throw new NotFoundException(MemberNotFoundMessage);

        // The member was just read and the update is a plain set, so answer with it instead of reading it again.
        member.CanPut = request.CanPut;
        member.CanUse = request.CanUse;
        member.AccessExpiresAt = request.AccessExpiresAt;
        return member.ToResponse();
    }

    public async Task RemoveMemberAsync(
        Guid fridgeId,
        Guid userId,
        Guid memberId,
        CancellationToken ct = default
    )
    {
        var caller = await RequireMemberAsync(fridgeId, userId, ownerOnly: false, ct);
        if (memberId != userId && !caller.IsOwner)
            throw new ForbiddenException("Only the fridge owner can remove other members.");

        await GetNonOwnerAsync(
            fridgeId,
            memberId,
            "The owner cannot be removed; delete the fridge instead.",
            ct
        );

        if (!await fridges.RemoveMemberAsync(fridgeId, memberId, timeProvider.GetUtcNow(), ct))
            throw new NotFoundException(MemberNotFoundMessage);
    }

    // Non-members get 404 so the existence of a fridge is not leaked.
    private async Task<FridgeMember> RequireMemberAsync(
        Guid fridgeId,
        Guid userId,
        bool ownerOnly,
        CancellationToken ct
    )
    {
        var member =
            await fridges.GetMemberAsync(fridgeId, userId, ct)
            ?? throw new NotFoundException(FridgeNotFoundMessage);
        if (ownerOnly && !member.IsOwner)
            throw new ForbiddenException("Only the fridge owner can do this.");

        return member;
    }

    // The owner holds the fridge, so their rights and membership stay as they are. Someone who isn't in it at all is a 404.
    private async Task<FridgeMember> GetNonOwnerAsync(
        Guid fridgeId,
        Guid userId,
        string ownerMessage,
        CancellationToken ct
    )
    {
        var member =
            await fridges.GetMemberAsync(fridgeId, userId, ct)
            ?? throw new NotFoundException(MemberNotFoundMessage);
        if (member.IsOwner)
            throw new ConflictException(ownerMessage);

        return member;
    }

    private async Task<FridgeResponse> AddFridgeAsync(
        Guid userId,
        string name,
        CancellationToken ct
    )
    {
        var fridge = new Fridge
        {
            Name = name,
            InviteCode = NewInviteCode(),
            MaxMembers = limits.MaxMembers,
            MaxContainers = limits.MaxContainers,
            CreatedByUserId = userId,
        };
        var owner = new FridgeMember { UserId = userId, IsOwner = true };

        var status = await fridges.AddAsync(fridge, owner, MaxOwnedFridges, ct);
        if (status == AddFridgeStatus.LimitReached)
            throw new ConflictException(
                $"You can own at most {MaxOwnedFridges} fridges. Delete one to create another."
            );

        return new FridgeWithMembership(fridge, owner, MemberCount: 1).ToResponse();
    }

    private async Task<string> SetNewInviteCodeAsync(Guid fridgeId, CancellationToken ct)
    {
        var inviteCode = NewInviteCode();
        if (!await fridges.SetInviteCodeAsync(fridgeId, inviteCode, ct))
            throw new NotFoundException(FridgeNotFoundMessage);

        return inviteCode;
    }

    // Codes are random, so one can land on another fridge's code. That is very rare: 32^8 is about
    // 10^12 codes. Each retry draws a fresh code; after the last attempt the failure is reported.
    private static async Task<T> RetryOnDuplicateCodeAsync<T>(Func<Task<T>> attempt)
    {
        for (var attemptNumber = 1; attemptNumber <= InviteCodeAttempts; attemptNumber++)
        {
            try
            {
                return await attempt();
            }
            catch (DuplicateKeyException) when (attemptNumber < InviteCodeAttempts)
            {
                // The code collided with another fridge's; loop around and draw a new one.
            }
        }

        throw new UnreachableException("The last attempt either returns or throws.");
    }

    private static string NewInviteCode() =>
        new(RandomNumberGenerator.GetItems<char>(InviteCodeAlphabet, InviteCodeLength));
}
