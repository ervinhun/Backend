using System.ComponentModel.DataAnnotations;

namespace BusinessLogic.Dtos.Item;

public class CreateItemRequest
{
    [Required]
    public Guid? ContainerId { get; set; }

    /// <summary>A product from the catalog. Fills in whatever of the name, image, category and expiry date is left out.</summary>
    public Guid? ProductCatalogId { get; set; }

    /// <summary>Required unless a product is given.</summary>
    [StringLength(200, MinimumLength = 1)]
    public string? Name { get; set; }

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

    /// <summary>Required unless the product's type has a default shelf life, which then counts from today.</summary>
    public DateOnly? ExpiryDate { get; set; }

    [StringLength(1000)]
    public string? Comment { get; set; }
}
