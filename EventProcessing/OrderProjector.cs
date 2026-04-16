using EventStore.Client;
using System.Text.Json;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using OrderingService.Data;
using OrderingService.Events;
using OrderingService.Models;
using OrderingService.AsyncDataServices;
using OrderingService.DTOs.OrderDTO;

namespace OrderingService.EventProcessing;

public class OrderProjector : BackgroundService
{
    private readonly EventStoreClient _client;
    private readonly IServiceProvider _serviceProvider;
    private readonly IMessageBusClient _messageBusClient;

    public OrderProjector(
        EventStoreClient client, 
        IServiceProvider serviceProvider, 
        IMessageBusClient messageBusClient)
    {
        _client = client;
        _serviceProvider = serviceProvider;
        _messageBusClient = messageBusClient;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await _client.SubscribeToAllAsync(
            FromAll.Start,
            EventAppeared,
            cancellationToken: stoppingToken
        );
    }

    private async Task EventAppeared(StreamSubscription subscription, ResolvedEvent resolvedEvent, CancellationToken cancellationToken)
    {
        if (resolvedEvent.Event.EventType.StartsWith("$")) return;

        var json = System.Text.Encoding.UTF8.GetString(resolvedEvent.Event.Data.ToArray());

        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<OrderingDbContext>();

        try
        {
            switch (resolvedEvent.Event.EventType)
            {
                case nameof(OrderPlacedEvent):
                    {
                        var placedEvent = JsonSerializer.Deserialize<OrderPlacedEvent>(json);
                        if (placedEvent == null) return;

                        // 1. Update Read Model (Idempotent)
                        if (!context.OrderSummaries.Any(o => o.OrderId == placedEvent.OrderId))
                        {
                            var summary = new OrderSummary
                            {
                                Id = Guid.NewGuid(),
                                OrderId = placedEvent.OrderId,
                                UserId = placedEvent.UserId,
                                UserName = placedEvent.UserName,
                                TotalAmount = placedEvent.TotalAmount,
                                Status = OrderStatus.Pending.ToString(),
                                OrderDate = placedEvent.OrderDate
                            };

                            context.OrderSummaries.Add(summary);
                            await context.SaveChangesAsync(cancellationToken);
                        }

                        // 2. Notify ProductService via RabbitMQ
                        var orderCreatedDto = new OrderCreatedDto
                        {
                            OrderId = placedEvent.OrderId,
                            Event = "Order_Created",
                            Items = placedEvent.Items.Select(i => new OrderCreatedItemDto
                            {
                                ProductId = i.ProductId,
                                Quantity = i.Quantity
                            }).ToList()
                        };
                        await _messageBusClient.PublishOrderCreated(orderCreatedDto);
                        break;
                    }
                case nameof(OrderPaymentConfirmedEvent):
                    {
                        var paymentEvent = JsonSerializer.Deserialize<OrderPaymentConfirmedEvent>(json);
                        if (paymentEvent == null) return;

                        var summary = context.OrderSummaries.FirstOrDefault(o => o.OrderId == paymentEvent.OrderId);
                        if (summary != null)
                        {
                            summary.Status = OrderStatus.Paid.ToString();
                            await context.SaveChangesAsync(cancellationToken);
                        }
                        break;
                    }
                case nameof(OrderShippedEvent):
                    {
                        var shippedEvent = JsonSerializer.Deserialize<OrderShippedEvent>(json);
                        if (shippedEvent == null) return;

                        var summary = context.OrderSummaries.FirstOrDefault(o => o.OrderId == shippedEvent.OrderId);
                        if (summary != null)
                        {
                            summary.Status = OrderStatus.Shipped.ToString();
                            await context.SaveChangesAsync(cancellationToken);
                        }
                        break;
                    }
                case nameof(OrderCancelledEvent):
                    {
                        var cancelledEvent = JsonSerializer.Deserialize<OrderCancelledEvent>(json);
                        if (cancelledEvent == null) return;

                        // 1. Update Read Model
                        var summary = context.OrderSummaries.FirstOrDefault(o => o.OrderId == cancelledEvent.OrderId);
                        if (summary != null)
                        {
                            summary.Status = OrderStatus.Cancelled.ToString();
                            await context.SaveChangesAsync(cancellationToken);
                        }

                        // 2. Notify ProductService to Restore Stock
                        var orderCancelledDto = new OrderCreatedDto // Reuse same DTO structure for simplicity
                        {
                            OrderId = cancelledEvent.OrderId,
                            Event = "Order_Cancelled",
                            Items = cancelledEvent.Items.Select(i => new OrderCreatedItemDto
                            {
                                ProductId = i.ProductId,
                                Quantity = i.Quantity
                            }).ToList()
                        };
                        await _messageBusClient.PublishOrderCreated(orderCancelledDto);
                        break;
                    }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"--> Error processing event in projector: {ex.Message}");
        }
    }
}