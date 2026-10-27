using System.ComponentModel.DataAnnotations;

namespace BusinessLogic.Validators;

/// <summary>
/// A date that is still to come, by the <see cref="TimeProvider"/> of the validation context's services
/// (the system clock if it has none). Null is valid; pair with [Required] where a date is mandatory.
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public sealed class FutureDateAttribute : ValidationAttribute
{
    public FutureDateAttribute()
        : base("The date must be in the future.") { }

    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        var now = (
            validationContext.GetService(typeof(TimeProvider)) as TimeProvider
            ?? TimeProvider.System
        ).GetUtcNow();

        return value is null || (value is DateTimeOffset date && date > now)
            ? ValidationResult.Success
            : new ValidationResult(
                FormatErrorMessage(validationContext.DisplayName),
                validationContext.MemberName is { } member ? [member] : null
            );
    }
}
