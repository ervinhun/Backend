using System.ComponentModel.DataAnnotations;

namespace BusinessLogic.Dtos.Item;

/// <summary>Replaces the details of an item; its product, who added it and its status stay as they are.</summary>
public class UpdateItemRequest
{
    /// <summary>The container the item is in; a different one moves it there.</summary>
    [Required]
    public Guid? ContainerId { get; set; }

    [Required, StringLength(200, MinimumLength = 1)]
    public string Name { get; set; } = string.Empty;

    public Guid? CategoryId { get; set; }

    [StringLength(50)]
    public string? Amount { get; set; }

    [StringLength(50)]
    public string? Unit { get; set; }

    /// <summary>An http or https URL; an empty string counts as none.</summary>
    [
        RegularExpression(
            @"^https?://\S+$",
            ErrorMessage = "ImageUrl must be an http or https URL."
        ),
        StringLength(2000)
    ]
    public string? ImageUrl { get; set; }

    [Required]
    public DateOnly? ExpiryDate { get; set; }

    [StringLength(1000)]
    public string? Comment { get; set; }
}
