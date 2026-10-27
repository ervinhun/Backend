using BusinessLogic.Validators;

namespace BusinessLogic.Dtos.Fridge;

public class UpdateMemberRightsRequest
{
    public bool CanPut { get; set; }
    public bool CanUse { get; set; }

    /// <summary>
    /// When the member's access ends; from then on they are treated as having left the fridge.
    /// Null for access that never expires.
    /// </summary>
    [FutureDate]
    public DateTimeOffset? AccessExpiresAt { get; set; }
}
