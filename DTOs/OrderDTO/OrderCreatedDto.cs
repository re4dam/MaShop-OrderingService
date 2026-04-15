namespace OrderingService.DTOs.OrderDTO;

public class OrderCreatedDto
{
    public Guid OrderId { get; set; }
    public List<OrderCreatedItemDto> Items { get; set; } = new();
    public string Event { get; set; } = "Order_Created";
}

public class OrderCreatedItemDto
{
    public Guid ProductId { get; set; }
    public int Quantity { get; set; }
}