using MediatR;
using OrderingService.DTOs.OrderDTO;
using OrderingService.DTOs.OrderItemDTO;

namespace OrderingService.Commands;

public record PlaceOrderCommand(Guid UserId, List<OrderItemRequestDto> OrderItems) : IRequest<OrderResponseDto>;

public record ConfirmPaymentCommand(Guid OrderId) : IRequest<bool>;

public record ShipOrderCommand(Guid OrderId) : IRequest<bool>;

public record CancelOrderCommand(Guid OrderId) : IRequest<bool>;
