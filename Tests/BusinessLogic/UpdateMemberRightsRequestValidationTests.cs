using System.ComponentModel.DataAnnotations;
using BusinessLogic.Dtos.Fridge;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Time.Testing;

namespace Tests.BusinessLogic;

public class UpdateMemberRightsRequestValidationTests
{
    private static readonly DateTimeOffset Now = new(2030, 1, 1, 12, 0, 0, TimeSpan.Zero);

    private static bool IsValid(DateTimeOffset? accessExpiresAt)
    {
        var request = new UpdateMemberRightsRequest { AccessExpiresAt = accessExpiresAt };
        var services = new ServiceCollection()
            .AddSingleton<TimeProvider>(new FakeTimeProvider(Now))
            .BuildServiceProvider();
        return Validator.TryValidateObject(
            request,
            new ValidationContext(request, services, null),
            [],
            validateAllProperties: true
        );
    }

    [Fact]
    public void AccessExpiresAt_Missing_IsValid() => Assert.True(IsValid(null));

    [Fact]
    public void AccessExpiresAt_InTheFuture_IsValid() => Assert.True(IsValid(Now.AddMinutes(1)));

    [Fact]
    public void AccessExpiresAt_Now_IsInvalid() => Assert.False(IsValid(Now));

    [Fact]
    public void AccessExpiresAt_InThePast_IsInvalid() => Assert.False(IsValid(Now.AddMinutes(-1)));

    [Fact]
    public void WithoutATimeProviderService_UsesTheSystemClock()
    {
        var request = new UpdateMemberRightsRequest
        {
            AccessExpiresAt = DateTimeOffset.UtcNow.AddDays(1),
        };

        Assert.True(
            Validator.TryValidateObject(
                request,
                new ValidationContext(request),
                [],
                validateAllProperties: true
            )
        );
    }
}
