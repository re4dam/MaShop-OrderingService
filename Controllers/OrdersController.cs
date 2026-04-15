using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OrderingService.Data;
using OrderingService.Models;
using OrderingService.DTOs.OrderDTO;
using OrderingService.DTOs.OrderItemDTO;
using OrderingService.AsyncDataServices;

namespace OrderingService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class OrdersController : ControllerBase
{
    private readonly OrderingDbContext _context;
    private readonly IMessageBusClient _messageBusClient;

    public OrdersController(OrderingDbContext context, IMessageBusClient messageBusClient)
    {
        _context = context;
        _messageBusClient = messageBusClient;
    }

    // GET: api/orders
    [HttpGet]
    public async Task<ActionResult<IEnumerable<OrderResponseDto>>> GetOrders()
    {
        var orders = await _context.Orders
            .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
            .Include(o => o.User)
            .ToListAsync();

        return Ok(orders.Select(o => MapToResponseDto(o)));
    }

    // GET: api/orders/{id}
    [HttpGet("{id}")]
    public async Task<ActionResult<OrderResponseDto>> GetOrder(Guid id)
    {
        var order = await _context.Orders
            .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
            .Include(o => o.User)
            .FirstOrDefaultAsync(o => o.Id == id);

        if (order == null) return NotFound();
        return Ok(MapToResponseDto(order));
    }

    // GET: api/orders/user/{userId}
    [HttpGet("user/{userId}")]
    public async Task<ActionResult<IEnumerable<OrderResponseDto>>> GetOrdersByUser(Guid userId)
    {
        var orders = await _context.Orders
            .Where(o => o.UserId == userId)
            .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
            .Include(o => o.User)
            .ToListAsync();

        return Ok(orders.Select(o => MapToResponseDto(o)));
    }

    // POST: api/orders
    [HttpPost]
    public async Task<ActionResult<OrderResponseDto>> CreateOrder(OrderRequestDto requestDto)
    {
        var user = await _context.Users.FindAsync(requestDto.UserId);
        if (user == null) return BadRequest("User not found.");

        var order = new Order
        {
            Id = Guid.NewGuid(),
            UserId = requestDto.UserId,
            OrderDate = DateTime.UtcNow,
            Status = OrderStatus.Pending,
            OrderItems = new List<OrderItem>()
        };

        decimal totalAmount = 0;

        foreach (var itemDto in requestDto.OrderItems)
        {
            var product = await _context.Products.FindAsync(itemDto.ProductId);
            if (product == null) return BadRequest($"Product with ID {itemDto.ProductId} not found.");

            var unitPrice = product.Price;
            var itemTotal = unitPrice * itemDto.Quantity;
            totalAmount += itemTotal;

            order.OrderItems.Add(new OrderItem
            {
                Id = Guid.NewGuid(),
                OrderId = order.Id,
                ProductId = itemDto.ProductId,
                Quantity = itemDto.Quantity,
                UnitPrice = unitPrice
            });
        }

        order.TotalAmount = totalAmount;

        _context.Orders.Add(order);
        await _context.SaveChangesAsync();

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

        // Reload to include relations for the response
        var result = await _context.Orders
            .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
            .Include(o => o.User)
            .FirstAsync(o => o.Id == order.Id);

        return CreatedAtAction(nameof(GetOrder), new { id = order.Id }, MapToResponseDto(result));
    }

    // PATCH: api/orders/{id}/cancel
    [HttpPatch("{id}/cancel")]
    public async Task<IActionResult> CancelOrder(Guid id)
    {
        var order = await _context.Orders.FindAsync(id);
        if (order == null) return NotFound();

        if (order.Status == OrderStatus.Cancelled) return BadRequest("Order is already cancelled.");
        if (order.Status == OrderStatus.Shipped) return BadRequest("Cannot cancel a shipped order.");

        order.Status = OrderStatus.Cancelled;
        await _context.SaveChangesAsync();

        return NoContent();
    }

    // DELETE: api/orders/{id}
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteOrder(Guid id)
    {
        var order = await _context.Orders.FindAsync(id);
        if (order == null) return NotFound();

        _context.Orders.Remove(order);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    private static OrderResponseDto MapToResponseDto(Order order)
    {
        return new OrderResponseDto
        {
            Id = order.Id,
            OrderDate = order.OrderDate,
            UserId = order.UserId,
            UserName = order.User?.Name ?? "Unknown",
            TotalAmount = order.TotalAmount,
            Status = order.Status.ToString(),
            OrderItems = order.OrderItems.Select(oi => new OrderItemResponseDto
            {
                Id = oi.Id,
                ProductId = oi.ProductId,
                ProductName = oi.Product?.Name ?? "Unknown Product",
                Quantity = oi.Quantity,
                UnitPrice = oi.UnitPrice
            }).ToList()
        };
    }
}