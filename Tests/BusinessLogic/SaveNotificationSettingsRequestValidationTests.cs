using System.ComponentModel.DataAnnotations;
using BusinessLogic.Dtos.Notification;

namespace Tests.BusinessLogic;

public class SaveNotificationSettingsRequestValidationTests
{
    private static List<ValidationResult> Validate(
        int warning,
        int danger,
        string scope,
        params Guid[] recipients
    )
    {
        var request = new SaveNotificationSettingsRequest
        {
            WarningDaysBefore = warning,
            DangerDaysBefore = danger,
            RecipientsScope = scope,
            RecipientUserIds = [.. recipients],
        };
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(request, new ValidationContext(request), results, true);
        return results;
    }

    [Theory]
    [InlineData(6, 1, "all_members")]
    [InlineData(7, 0, "adder_and_owners")]
    [InlineData(365, 364, "all_members")]
    public void ValidRequests_PassValidation(int warning, int danger, string scope) =>
        Assert.Empty(Validate(warning, danger, scope));

    [Fact]
    public void ACustomScopeWithRecipients_PassesValidation() =>
        Assert.Empty(Validate(6, 1, "custom", Guid.NewGuid()));

    [Theory]
    [InlineData(0, 0, "all_members")]
    [InlineData(366, 1, "all_members")]
    [InlineData(6, -1, "all_members")]
    [InlineData(6, 1, "")]
    [InlineData(6, 1, "everyone")]
    public void OutOfRangeDaysOrUnknownScope_FailsValidation(int warning, int danger, string scope) =>
        Assert.NotEmpty(Validate(warning, danger, scope));

    [Theory]
    [InlineData(3, 3)]
    [InlineData(3, 5)]
    public void ADangerThatIsNotAfterTheWarning_FailsValidation(int warning, int danger) =>
        Assert.NotEmpty(Validate(warning, danger, "all_members"));

    [Fact]
    public void ACustomScopeWithoutRecipients_FailsValidation() =>
        Assert.NotEmpty(Validate(6, 1, "custom"));
}
