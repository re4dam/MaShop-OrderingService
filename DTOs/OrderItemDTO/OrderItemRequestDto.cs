namespace OrderingService.DTOs.OrderItemDTO;

public class OrderItemRequestDto
{
    public Guid ProductId { get; set; }
    public int Quantity { get; set; }
}