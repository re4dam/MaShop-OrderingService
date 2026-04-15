namespace OrderingService.Models;

/* 
 * This class represents the Order Aggregate, serving as the primary model for the "Write" side of the system.
 * It is responsible for enforcing business rules, managing order items, and controlling status transitions 
 * to ensure the integrity of an order's lifecycle.
 */
public class Order
{
    public Guid Id { get; private set; }
    public DateTime OrderDate { get; private set; }
    public Guid UserId { get; private set; }
    public User? User { get; private set; }
    public decimal TotalAmount { get; private set; }
    public OrderStatus Status { get; set; }
    private readonly List<OrderItem> _orderItems = new();
    public IReadOnlyCollection<OrderItem> OrderItems => _orderItems.AsReadOnly();

    /* 
     * This private constructor is used by Entity Framework Core to recreate the object from the database.
     */
    private Order() { }

    /* 
     * This constructor initializes a new order with a unique identifier and associates it with a specific user.
     * It sets the initial order date to the current time and the status to 'Pending'.
     */
    public Order(Guid id, Guid userId)
    {
        Id = id;
        UserId = userId;
        OrderDate = DateTime.UtcNow;
        Status = OrderStatus.Pending;
    }

    /* 
     * This method adds a new product to the order. It creates an OrderItem, attaches it to this order,
     * and automatically updates the total amount based on the product's price and quantity.
     */
    public void AddOrderItem(Guid productId, int quantity, decimal unitPrice)
    {
        var item = new OrderItem
        {
            Id = Guid.NewGuid(),
            OrderId = this.Id,
            ProductId = productId,
            Quantity = quantity,
            UnitPrice = unitPrice
        };
        _orderItems.Add(item);
        TotalAmount += unitPrice * quantity;
    }

    /* 
     * This method transitions the order to the 'Paid' status. 
     * It ensures that an order can only be marked as paid if it is currently in the 'Pending' state.
     */
    public void MarkAsPaid()
    {
        if (Status == OrderStatus.Pending)
            Status = OrderStatus.Paid;
    }

    /* 
     * This method transitions the order to the 'Shipped' status.
     * It ensures that an order can only be shipped if payment has already been confirmed (status is 'Paid').
     */
    public void MarkAsShipped()
    {
        if (Status == OrderStatus.Paid)
            Status = OrderStatus.Shipped;
    }

    /* 
     * This method cancels the order. It prevents cancellation if the order has already been shipped,
     * upholding the business rule that shipped goods cannot be cancelled through this flow.
     */
    public void Cancel()
    {
        if (Status != OrderStatus.Shipped)
            Status = OrderStatus.Cancelled;
    }
}