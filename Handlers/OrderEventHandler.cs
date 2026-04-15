using MediatR;
using Microsoft.EntityFrameworkCore;
using OrderingService.Events;
using OrderingService.Data;
using OrderingService.Models;

namespace OrderingService.Handlers;

public class OrderEventHandler : 
    INotificationHandler<OrderPlacedEvent>,
    INotificationHandler<OrderStatusChangedEvent>
{
    private readonly IServiceScopeFactory _scopeFactory;

    public OrderEventHandler(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    public async Task Handle(OrderPlacedEvent notification, CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<OrderingDbContext>();

        var summary = new OrderSummary
        {
            Id = Guid.NewGuid(),
            OrderId = notification.Order.Id,
            UserId = notification.Order.UserId,
            UserName = notification.UserName,
            TotalAmount = notification.Order.TotalAmount,
            Status = notification.Order.Status.ToString(),
            OrderDate = notification.Order.OrderDate
        };

        context.OrderSummaries.Add(summary);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task Handle(OrderStatusChangedEvent notification, CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<OrderingDbContext>();

        var summary = await context.OrderSummaries
            .FirstOrDefaultAsync(s => s.OrderId == notification.OrderId, cancellationToken);

        if (summary != null)
        {
            summary.Status = notification.NewStatus.ToString();
            await context.SaveChangesAsync(cancellationToken);
        }
    }
}
