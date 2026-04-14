namespace OrderingService.Models;

public class Product
{
    public Guid Id { get; set; } // Matches the Id from ProductService
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int Quantity { get; set; } // Local stock count or reserved quantity
}