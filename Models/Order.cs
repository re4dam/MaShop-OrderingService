using OrderingService.Events;

namespace OrderingService.Models;

/*
 * This class represents the Order Aggregate for the Event Sourcing architecture.
 * It is an in-memory Aggregate Root and has NO Entity Framework dependencies.
 * It rebuilds its state by replaying past events in the Apply() method.
 */
public class Order
{
    public Guid Id { get; private set; }
    public DateTime OrderDate { get; private set; }
    public Guid UserId { get; private set; }
    public decimal TotalAmount { get; private set; }
    public OrderStatus Status { get; set; }

    // ReadOnly list for items, though they are purely state elements here
    private readonly List<OrderItem> _orderItems = new();
    public IReadOnlyCollection<OrderItem> OrderItems => _orderItems.AsReadOnly();

    // The list of events that have not yet been saved to EventStoreDB
    private readonly List<object> _uncommittedEvents = new();

    public IReadOnlyList<object> GetUncommittedEvents() => _uncommittedEvents.AsReadOnly();

    public void ClearUncommittedEvents() => _uncommittedEvents.Clear();

    // Required for rehydration
    public Order() { }

    public void LoadFromHistory(IEnumerable<object> history)
    {
        foreach (var @event in history)
        {
            ApplyEvent(@event, false);
        }
    }

    private void ApplyEvent(object @event, bool isNew)
    {
        switch (@event)
        {
            case OrderPlacedEvent e:
                Id = e.OrderId;
                UserId = e.UserId;
                OrderDate = e.OrderDate;
                Status = OrderStatus.Pending;
                TotalAmount = e.TotalAmount;
                foreach (var item in e.Items)
                {
                    _orderItems.Add(new OrderItem
                    {
                        Id = Guid.NewGuid(),
                        OrderId = Id,
                        ProductId = item.ProductId,
                        Quantity = item.Quantity,
                        UnitPrice = item.UnitPrice
                    });
                }
                break;
            case OrderPaymentConfirmedEvent e:
                Status = OrderStatus.Paid;
                break;
            case OrderShippedEvent e:
                Status = OrderStatus.Shipped;
                break;
            case OrderCancelledEvent e:
                Status = OrderStatus.Cancelled;
                break;
        }

        if (isNew)
        {
            _uncommittedEvents.Add(@event);
        }
    }

    // Creating a new Order triggers an OrderPlacedEvent
    public Order(Guid id, Guid userId, string userName, List<(Guid ProductId, int Quantity, decimal UnitPrice)> items)
    {
        var totalAmount = items.Sum(i => i.Quantity * i.UnitPrice);
        var itemEvents = items.Select(i => new OrderItemEvent(i.ProductId, i.Quantity, i.UnitPrice)).ToList();

        var @event = new OrderPlacedEvent(id, userId, userName, itemEvents, totalAmount, DateTime.UtcNow);
        
        ApplyEvent(@event, true);
    }

    public void MarkAsPaid()
    {
        if (Status == OrderStatus.Pending)
        {
            ApplyEvent(new OrderPaymentConfirmedEvent(Id), true);
        }
    }

    public void MarkAsShipped()
    {
        if (Status == OrderStatus.Paid)
        {
            ApplyEvent(new OrderShippedEvent(Id), true);
        }
    }

    public void Cancel()
    {
        if (Status != OrderStatus.Shipped && Status != OrderStatus.Cancelled)
        {
            var itemEvents = _orderItems.Select(i => new OrderItemEvent(i.ProductId, i.Quantity, i.UnitPrice)).ToList();
            ApplyEvent(new OrderCancelledEvent(Id, itemEvents), true);
        }
    }
}