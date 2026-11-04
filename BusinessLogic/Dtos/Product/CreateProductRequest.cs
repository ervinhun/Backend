using System.ComponentModel.DataAnnotations;

namespace BusinessLogic.Dtos.Product;

public class CreateProductRequest
{
    /// <summary>The text of the scanned barcode as the app read it; it is stored as given, only trimmed.</summary>
    [Required, StringLength(64, MinimumLength = 1)]
    public string Barcode { get; set; } = string.Empty;

    [Required, StringLength(200, MinimumLength = 1)]
    public string Name { get; set; } = string.Empty;

    public Guid? ProductTypeId { get; set; }

    /// <summary>An http or https URL; an empty string counts as none.</summary>
    [RegularExpression(@"^https?://\S+$", ErrorMessage = "ImageUrl must be an http or https URL."), StringLength(2000)]
    public string? ImageUrl { get; set; }
}
