using MediatR;
using System.Text.Json.Serialization;

namespace OrderingService.Events;

// Base Event record
public abstract record OrderEvent;

public record OrderPlacedEvent(
    Guid OrderId,
    Guid UserId,
    string UserName,
    List<OrderItemEvent> Items,
    decimal TotalAmount,
    DateTime OrderDate) : OrderEvent, INotification;

public record OrderItemEvent(Guid ProductId, int Quantity, decimal UnitPrice);

public record OrderPaymentConfirmedEvent(Guid OrderId) : OrderEvent, INotification;

public record OrderShippedEvent(Guid OrderId) : OrderEvent, INotification;

public record OrderCancelledEvent(Guid OrderId, List<OrderItemEvent> Items) : OrderEvent, INotification;
