namespace BusinessLogic.Dtos.Product;

public class ProductResponse
{
    public Guid Id { get; set; }
    public string Barcode { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public Guid? ProductTypeId { get; set; }
    public string? ImageUrl { get; set; }
    public string Source { get; set; } = string.Empty;
}
