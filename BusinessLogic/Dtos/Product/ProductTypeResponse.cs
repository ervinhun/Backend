namespace BusinessLogic.Dtos.Product;

public class ProductTypeResponse
{
    public Guid Id { get; set; }
    public Guid? CategoryId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int DefaultExpiryDays { get; set; }

    /// <summary>How long it keeps in each storage; null where there is no recommendation, so use DefaultExpiryDays.</summary>
    public int? PantryExpiryDays { get; set; }

    public int? FridgeExpiryDays { get; set; }
    public int? FreezerExpiryDays { get; set; }
}
