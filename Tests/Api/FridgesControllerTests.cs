using System.Security.Claims;
using Api.Controllers;
using BusinessLogic.Dtos.Fridge;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.IdentityModel.JsonWebTokens;
using Tests.Fakes;

namespace Tests.Api;

public class FridgesControllerTests
{
    private readonly Guid _fridgeId = Guid.NewGuid();
    private readonly Guid _ownerId = Guid.NewGuid();
    private readonly Guid _memberId = Guid.NewGuid();
    private readonly Guid _strangerId = Guid.NewGuid();
    private readonly FakeFridgeService _service = new();

    public FridgesControllerTests()
    {
        _service.AddMember(_fridgeId, _ownerId, isOwner: true);
        _service.AddMember(_fridgeId, _memberId);
    }

    private FridgesController ControllerFor(Guid userId) =>
        new(_service)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(
                        new ClaimsIdentity(
                            [new Claim(JwtRegisteredClaimNames.Sub, userId.ToString())],
                            "test"
                        )
                    ),
                },
            },
        };

    private static void AssertStatus(int expected, IActionResult? result) =>
        Assert.Equal(expected, Assert.IsAssignableFrom<IStatusCodeActionResult>(result).StatusCode);

    [Fact]
    public async Task Create_ValidRequest_ReturnsCreatedWithFridge()
    {
        var result = await ControllerFor(_ownerId)
            .Create(new SaveFridgeRequest { Name = "Home" }, CancellationToken.None);

        var created = Assert.IsType<CreatedAtActionResult>(result.Result);
        var fridge = Assert.IsType<FridgeResponse>(created.Value);
        Assert.Equal(nameof(FridgesController.Get), created.ActionName);
        Assert.Equal(fridge.Id, created.RouteValues!["id"]);
        Assert.Equal("Home", fridge.Name);
    }

    [Fact]
    public async Task Join_PassesTheCodeToTheService_ForAnyUser()
    {
        var result = await ControllerFor(_strangerId)
            .Join(new JoinFridgeRequest { InviteCode = "ABCD2345" }, CancellationToken.None);

        Assert.Equal("Joined", result.Value!.Name);
        Assert.Contains(nameof(FakeFridgeService.JoinAsync), _service.Calls);
    }

    [Fact]
    public async Task Actions_PassTheRequestsCancellationTokenOn()
    {
        using var cts = new CancellationTokenSource();

        await ControllerFor(_ownerId).GetMine(cts.Token);
        Assert.Equal(cts.Token, _service.LastToken);

        await ControllerFor(_ownerId)
            .Update(_fridgeId, new SaveFridgeRequest { Name = "Renamed" }, cts.Token);
        Assert.Equal(cts.Token, _service.LastToken);

        await ControllerFor(_ownerId).RemoveMember(_fridgeId, _memberId, cts.Token);
        Assert.Equal(cts.Token, _service.LastToken);
    }

    [Fact]
    public async Task GetMine_ReturnsFridgesOfCurrentUser()
    {
        var result = await ControllerFor(_ownerId).GetMine(CancellationToken.None);

        Assert.Single(result);
        Assert.Contains(nameof(FakeFridgeService.GetForUserAsync), _service.Calls);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Get_Member_ReturnsFridge(bool asOwner)
    {
        var result = await ControllerFor(asOwner ? _ownerId : _memberId)
            .Get(_fridgeId, CancellationToken.None);

        Assert.Equal(_fridgeId, result.Value!.Id);
    }

    [Fact]
    public async Task Get_NonMember_ReturnsNotFound()
    {
        var result = await ControllerFor(_strangerId).Get(_fridgeId, CancellationToken.None);

        Assert.IsType<NotFoundResult>(result.Result);
        Assert.Empty(_service.Calls);
    }

    [Fact]
    public async Task Get_UnknownFridge_ReturnsNotFound()
    {
        var result = await ControllerFor(_ownerId).Get(Guid.NewGuid(), CancellationToken.None);

        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task Update_Owner_ReturnsUpdatedFridge()
    {
        var result = await ControllerFor(_ownerId)
            .Update(_fridgeId, new SaveFridgeRequest { Name = "Renamed" }, CancellationToken.None);

        Assert.Equal("Renamed", result.Value!.Name);
    }

    [Fact]
    public async Task Update_NonOwnerMember_ReturnsForbidden()
    {
        var result = await ControllerFor(_memberId)
            .Update(_fridgeId, new SaveFridgeRequest { Name = "Renamed" }, CancellationToken.None);

        AssertStatus(StatusCodes.Status403Forbidden, result.Result);
        Assert.Empty(_service.Calls);
    }

    [Fact]
    public async Task Update_NonMember_ReturnsNotFound()
    {
        var result = await ControllerFor(_strangerId)
            .Update(_fridgeId, new SaveFridgeRequest { Name = "Renamed" }, CancellationToken.None);

        Assert.IsType<NotFoundResult>(result.Result);
        Assert.Empty(_service.Calls);
    }

    [Fact]
    public async Task Delete_Owner_ReturnsNoContentAndDeletes()
    {
        var result = await ControllerFor(_ownerId).Delete(_fridgeId, CancellationToken.None);

        Assert.IsType<NoContentResult>(result);
        Assert.Contains(nameof(FakeFridgeService.DeleteAsync), _service.Calls);
    }

    [Fact]
    public async Task Delete_NonOwnerMember_ReturnsForbiddenAndKeepsFridge()
    {
        var result = await ControllerFor(_memberId).Delete(_fridgeId, CancellationToken.None);

        AssertStatus(StatusCodes.Status403Forbidden, result);
        Assert.Empty(_service.Calls);
    }

    [Fact]
    public async Task Delete_NonMember_ReturnsNotFound()
    {
        var result = await ControllerFor(_strangerId).Delete(_fridgeId, CancellationToken.None);

        Assert.IsType<NotFoundResult>(result);
        Assert.Empty(_service.Calls);
    }

    [Fact]
    public async Task RegenerateInviteCode_Owner_ReturnsFridgeWithNewCode()
    {
        var result = await ControllerFor(_ownerId)
            .RegenerateInviteCode(_fridgeId, CancellationToken.None);

        Assert.Equal("NEWCODE", result.Value!.InviteCode);
    }

    [Fact]
    public async Task RegenerateInviteCode_NonOwnerMember_ReturnsForbidden()
    {
        var result = await ControllerFor(_memberId)
            .RegenerateInviteCode(_fridgeId, CancellationToken.None);

        AssertStatus(StatusCodes.Status403Forbidden, result.Result);
        Assert.Empty(_service.Calls);
    }

    [Fact]
    public async Task RegenerateInviteCode_NonMember_ReturnsNotFound()
    {
        var result = await ControllerFor(_strangerId)
            .RegenerateInviteCode(_fridgeId, CancellationToken.None);

        Assert.IsType<NotFoundResult>(result.Result);
        Assert.Empty(_service.Calls);
    }

    [Fact]
    public async Task GetMembers_Member_ReturnsAllMembers()
    {
        var result = await ControllerFor(_memberId).GetMembers(_fridgeId, CancellationToken.None);

        var members = Assert.IsAssignableFrom<IReadOnlyList<FridgeMemberResponse>>(
            Assert.IsType<OkObjectResult>(result.Result).Value
        );
        Assert.Equal(2, members.Count);
    }

    [Fact]
    public async Task GetMembers_NonMember_ReturnsNotFound()
    {
        var result = await ControllerFor(_strangerId).GetMembers(_fridgeId, CancellationToken.None);

        Assert.IsType<NotFoundResult>(result.Result);
        Assert.Empty(_service.Calls);
    }

    [Fact]
    public async Task UpdateMemberRights_OwnerChangesMember_ReturnsUpdatedMember()
    {
        var request = new UpdateMemberRightsRequest { CanPut = false, CanUse = true };

        var result = await ControllerFor(_ownerId)
            .UpdateMemberRights(_fridgeId, _memberId, request, CancellationToken.None);

        Assert.False(result.Value!.CanPut);
        Assert.True(result.Value.CanUse);
    }

    [Fact]
    public async Task UpdateMemberRights_NonOwnerMember_ReturnsForbidden()
    {
        var request = new UpdateMemberRightsRequest { CanPut = true, CanUse = true };

        var result = await ControllerFor(_memberId)
            .UpdateMemberRights(_fridgeId, _memberId, request, CancellationToken.None);

        AssertStatus(StatusCodes.Status403Forbidden, result.Result);
        Assert.Empty(_service.Calls);
    }

    [Fact]
    public async Task UpdateMemberRights_NonMemberCaller_ReturnsNotFound()
    {
        var request = new UpdateMemberRightsRequest { CanPut = true, CanUse = true };

        var result = await ControllerFor(_strangerId)
            .UpdateMemberRights(_fridgeId, _memberId, request, CancellationToken.None);

        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task RemoveMember_OwnerRemovesMember_ReturnsNoContent()
    {
        var result = await ControllerFor(_ownerId)
            .RemoveMember(_fridgeId, _memberId, CancellationToken.None);

        Assert.IsType<NoContentResult>(result);
        Assert.Contains(nameof(FakeFridgeService.RemoveMemberAsync), _service.Calls);
    }

    [Fact]
    public async Task RemoveMember_MemberLeavesFridge_ReturnsNoContent()
    {
        var result = await ControllerFor(_memberId)
            .RemoveMember(_fridgeId, _memberId, CancellationToken.None);

        Assert.IsType<NoContentResult>(result);
        Assert.Contains(nameof(FakeFridgeService.RemoveMemberAsync), _service.Calls);
    }

    [Fact]
    public async Task RemoveMember_MemberRemovesSomeoneElse_ReturnsForbidden()
    {
        var result = await ControllerFor(_memberId)
            .RemoveMember(_fridgeId, _ownerId, CancellationToken.None);

        AssertStatus(StatusCodes.Status403Forbidden, result);
        Assert.Empty(_service.Calls);
    }

    [Fact]
    public async Task RemoveMember_NonMemberCaller_ReturnsNotFound()
    {
        var result = await ControllerFor(_strangerId)
            .RemoveMember(_fridgeId, _memberId, CancellationToken.None);

        Assert.IsType<NotFoundResult>(result);
        Assert.Empty(_service.Calls);
    }
}
