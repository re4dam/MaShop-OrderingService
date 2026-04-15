using MediatR;
using OrderingService.Models;

namespace OrderingService.Events;

public record OrderPlacedEvent(Order Order, string UserName) : INotification;

public record OrderStatusChangedEvent(Guid OrderId, OrderStatus NewStatus) : INotification;
