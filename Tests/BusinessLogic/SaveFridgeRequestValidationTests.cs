using System.ComponentModel.DataAnnotations;
using BusinessLogic.Dtos.Fridge;

namespace Tests.BusinessLogic;

public class SaveFridgeRequestValidationTests
{
    private static bool IsValid(string? name)
    {
        var request = new SaveFridgeRequest { Name = name! };
        return Validator.TryValidateObject(
            request,
            new ValidationContext(request),
            [],
            validateAllProperties: true
        );
    }

    [Theory]
    [InlineData("Home")]
    [InlineData("A")]
    public void Name_WithinLimits_IsValid(string name) => Assert.True(IsValid(name));

    [Fact]
    public void Name_AtMaxLength_IsValid() => Assert.True(IsValid(new string('a', 100)));

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void Name_MissingOrEmpty_IsInvalid(string? name) => Assert.False(IsValid(name));

    [Fact]
    public void Name_OverMaxLength_IsInvalid() => Assert.False(IsValid(new string('a', 101)));
}
