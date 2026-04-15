using OrderingService.DTOs.OrderItemDTO;

namespace OrderingService.DTOs.OrderDTO;

public class OrderResponseDto
{
    public Guid Id { get; set; }
    public DateTime OrderDate { get; set; }
    public Guid UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public string Status { get; set; } = string.Empty;
    public List<OrderItemResponseDto> OrderItems { get; set; } = new();
}