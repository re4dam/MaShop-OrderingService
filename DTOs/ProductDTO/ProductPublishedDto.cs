namespace OrderingService.DTOs.ProductDTO;

public class ProductPublishedDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int UnitsInStock { get; set; }
    public string Event { get; set; } = string.Empty;
}