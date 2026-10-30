using System.ComponentModel.DataAnnotations;

namespace BusinessLogic.Dtos.Fridge;

public class SaveFridgeRequest
{
    [Required, StringLength(100, MinimumLength = 1)]
    public string Name { get; set; } = string.Empty;
}
