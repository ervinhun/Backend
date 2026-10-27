using System.ComponentModel.DataAnnotations;

namespace BusinessLogic.Dtos.Container;

public class SaveContainerRequest
{
    [Required, StringLength(100, MinimumLength = 1)]
    public string Name { get; set; } = string.Empty;

    /// <summary>fridge, freezer or pantry.</summary>
    [Required, AllowedValues("fridge", "freezer", "pantry")]
    public string Type { get; set; } = string.Empty;
}
