using BusinessLogic.Dtos.Fridge;
using DataAccess.Entities;
using DataAccess.Interfaces;
using DataAccess.Models.Fridge;

namespace BusinessLogic.Mappers;

public static class FridgeMapper
{
    public static FridgeResponse ToResponse(this FridgeWithMembership x) =>
        new()
        {
            Id = x.Fridge.Id,
            Name = x.Fridge.Name,
            // The invite code lets anyone join, so only the owner gets to see it.
            InviteCode = x.Member.IsOwner ? x.Fridge.InviteCode : null,
            MaxMembers = x.Fridge.MaxMembers,
            MaxContainers = x.Fridge.MaxContainers,
            MemberCount = x.MemberCount,
            IsOwner = x.Member.IsOwner,
            CanPut = x.Member.CanPut,
            CanUse = x.Member.CanUse,
            CreatedAt = x.Fridge.CreatedAt,
        };

    public static FridgeMemberResponse ToResponse(this FridgeMember member) =>
        new()
        {
            UserId = member.UserId,
            DisplayName = member.DisplayName ?? DefaultDisplayName(member),
            IsOwner = member.IsOwner,
            CanPut = member.CanPut,
            CanUse = member.CanUse,
            AccessExpiresAt = member.AccessExpiresAt,
            JoinedAt = member.CreatedAt,
        };

    // Fails loudly instead of showing a blank name when a repository forgot to load the user.
    private static string DefaultDisplayName(FridgeMember member) =>
        (
            member.User
            ?? throw new InvalidOperationException(
                "The member's User must be loaded to show a default display name."
            )
        ).DefaultDisplayName;
}
