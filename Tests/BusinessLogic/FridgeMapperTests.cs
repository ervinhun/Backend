using BusinessLogic.Mappers;
using DataAccess.Entities;

namespace Tests.BusinessLogic;

public class FridgeMapperTests
{
    [Fact]
    public void MemberToResponse_PrefersTheFridgeDisplayName_ThenTheUsersDefault()
    {
        var user = new User { DefaultDisplayName = "Anna" };

        Assert.Equal(
            "Mom",
            new FridgeMember { DisplayName = "Mom", User = user }
                .ToResponse()
                .DisplayName
        );
        Assert.Equal(
            "Anna",
            new FridgeMember { User = user }
                .ToResponse()
                .DisplayName
        );
    }

    [Fact]
    public void MemberToResponse_WithoutAUserOrDisplayName_Throws()
    {
        Assert.Throws<InvalidOperationException>(() => new FridgeMember().ToResponse());
    }

    [Fact]
    public void MemberToResponse_WithADisplayName_NeedsNoUser()
    {
        Assert.Equal(
            "Mom",
            new FridgeMember { DisplayName = "Mom" }
                .ToResponse()
                .DisplayName
        );
    }
}
