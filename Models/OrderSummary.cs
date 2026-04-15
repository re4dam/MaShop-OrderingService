namespace OrderingService.Models;

/* 
 * This class represents the Order Summary Read Model, specifically designed for the "Read" side of the system.
 * It provides a flattened, high-performance representation of an order to be used for fast queries 
 * and displaying order lists without loading the full Order Aggregate.
 */
public class OrderSummary
{
    public Guid Id { get; set; }
    public Guid OrderId { get; set; }
    public Guid UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime OrderDate { get; set; }
}