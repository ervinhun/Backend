using System.ComponentModel.DataAnnotations;

namespace BusinessLogic.Dtos.Item;

public class SetItemStatusRequest
{
    /// <summary>active, consumed or removed; expired is only set by the system.</summary>
    [Required, AllowedValues("active", "consumed", "removed")]
    public string Status { get; set; } = string.Empty;
}
