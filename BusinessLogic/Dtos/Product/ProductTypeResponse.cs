namespace BusinessLogic.Dtos.Product;

public class ProductTypeResponse
{
    public Guid Id { get; set; }
    public Guid? CategoryId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int DefaultExpiryDays { get; set; }
}
