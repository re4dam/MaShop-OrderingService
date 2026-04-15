using MediatR;
using Microsoft.EntityFrameworkCore;
using OrderingService.Commands;
using OrderingService.Events;
using OrderingService.Data;
using OrderingService.Models;
using OrderingService.DTOs.OrderDTO;
using OrderingService.DTOs.OrderItemDTO;
using OrderingService.AsyncDataServices;

namespace OrderingService.Handlers;

public class OrderCommandHandlers : 
    IRequestHandler<PlaceOrderCommand, OrderResponseDto>,
    IRequestHandler<ConfirmPaymentCommand, bool>,
    IRequestHandler<ShipOrderCommand, bool>
{
    private readonly OrderingDbContext _context;
    private readonly IMessageBusClient _messageBusClient;
    private readonly IMediator _mediator;

    public OrderCommandHandlers(OrderingDbContext context, IMessageBusClient messageBusClient, IMediator mediator)
    {
        _context = context;
        _messageBusClient = messageBusClient;
        _mediator = mediator;
    }

    public async Task<OrderResponseDto> Handle(PlaceOrderCommand request, CancellationToken cancellationToken)
    {
        var user = await _context.Users.FindAsync(new object[] { request.UserId }, cancellationToken);
        if (user == null) throw new Exception("User not found.");

        var order = new Order(Guid.NewGuid(), request.UserId);

        foreach (var itemDto in request.OrderItems)
        {
            var product = await _context.Products.FindAsync(new object[] { itemDto.ProductId }, cancellationToken);
            if (product == null) throw new Exception($"Product with ID {itemDto.ProductId} not found.");

            order.AddOrderItem(itemDto.ProductId, itemDto.Quantity, product.Price);
        }

        _context.Orders.Add(order);
        await _context.SaveChangesAsync(cancellationToken);

        // Populate Read Model via internal event
        await _mediator.Publish(new OrderPlacedEvent(order, user.Name), cancellationToken);

        // Publish to Message Bus
        try
        {
            var orderCreatedDto = new OrderCreatedDto
            {
                OrderId = order.Id,
                Items = order.OrderItems.Select(oi => new OrderCreatedItemDto
                {
                    ProductId = oi.ProductId,
                    Quantity = oi.Quantity
                }).ToList()
            };
            await _messageBusClient.PublishOrderCreated(orderCreatedDto);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"--> Could not send asynchronously: {ex.Message}");
        }

        // Return a response DTO (mapping can be improved)
        return new OrderResponseDto
        {
            Id = order.Id,
            OrderDate = order.OrderDate,
            UserId = order.UserId,
            UserName = user.Name,
            TotalAmount = order.TotalAmount,
            Status = order.Status.ToString(),
            OrderItems = order.OrderItems.Select(oi => new OrderItemResponseDto
            {
                Id = oi.Id,
                ProductId = oi.ProductId,
                Quantity = oi.Quantity,
                UnitPrice = oi.UnitPrice
            }).ToList()
        };
    }

    public async Task<bool> Handle(ConfirmPaymentCommand request, CancellationToken cancellationToken)
    {
        var order = await _context.Orders.FindAsync(new object[] { request.OrderId }, cancellationToken);
        if (order == null) return false;

        order.MarkAsPaid();
        await _context.SaveChangesAsync(cancellationToken);

        await _mediator.Publish(new OrderStatusChangedEvent(order.Id, order.Status), cancellationToken);

        return true;
    }

    public async Task<bool> Handle(ShipOrderCommand request, CancellationToken cancellationToken)
    {
        var order = await _context.Orders.FindAsync(new object[] { request.OrderId }, cancellationToken);
        if (order == null) return false;

        order.MarkAsShipped();
        await _context.SaveChangesAsync(cancellationToken);

        await _mediator.Publish(new OrderStatusChangedEvent(order.Id, order.Status), cancellationToken);

        return true;
    }
}
