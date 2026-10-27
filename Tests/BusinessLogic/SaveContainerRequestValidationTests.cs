using System.ComponentModel.DataAnnotations;
using BusinessLogic.Dtos.Container;

namespace Tests.BusinessLogic;

public class SaveContainerRequestValidationTests
{
    private static List<ValidationResult> Validate(string name, string type)
    {
        var request = new SaveContainerRequest { Name = name, Type = type };
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(request, new ValidationContext(request), results, true);
        return results;
    }

    [Theory]
    [InlineData("Top shelf", "fridge")]
    [InlineData("Garage", "freezer")]
    [InlineData("Cellar", "pantry")]
    public void ValidRequests_PassValidation(string name, string type) =>
        Assert.Empty(Validate(name, type));

    [Theory]
    [InlineData("", "fridge")]
    [InlineData("Top shelf", "")]
    [InlineData("Top shelf", "cupboard")]
    public void MissingNameOrUnknownType_FailsValidation(string name, string type) =>
        Assert.NotEmpty(Validate(name, type));

    [Fact]
    public void ANameOfMoreThan100Characters_FailsValidation() =>
        Assert.NotEmpty(Validate(new string('a', 101), "fridge"));
}
