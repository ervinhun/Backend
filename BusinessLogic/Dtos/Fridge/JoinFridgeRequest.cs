using System.ComponentModel.DataAnnotations;

namespace BusinessLogic.Dtos.Fridge;

public class JoinFridgeRequest
{
    [Required, StringLength(32, MinimumLength = 1)]
    public string InviteCode { get; set; } = string.Empty;
}
