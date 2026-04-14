using OrderingService.DTOs.OrderItemDTO;

namespace OrderingService.DTOs.OrderDTO;

public class OrderRequestDto
{
    public Guid UserId { get; set; }
    public List<OrderItemRequestDto> OrderItems { get; set; } = new();
}