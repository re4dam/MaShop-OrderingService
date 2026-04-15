using Microsoft.AspNetCore.Mvc;
using MediatR;
using OrderingService.Commands;
using OrderingService.Queries;
using OrderingService.DTOs.OrderDTO;
using OrderingService.Models;

namespace OrderingService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class OrdersController : ControllerBase
{
    private readonly IMediator _mediator;

    public OrdersController(IMediator mediator)
    {
        _mediator = mediator;
    }

    // GET: api/orders
    [HttpGet]
    public async Task<ActionResult<IEnumerable<OrderSummary>>> GetOrders()
    {
        var result = await _mediator.Send(new GetOrderSummariesQuery());
        return Ok(result);
    }

    // GET: api/orders/{id}
    [HttpGet("{id}")]
    public async Task<ActionResult<OrderSummary>> GetOrder(Guid id)
    {
        var result = await _mediator.Send(new GetOrderSummaryByIdQuery(id));
        if (result == null) return NotFound();
        return Ok(result);
    }

    // GET: api/orders/user/{userId}
    [HttpGet("user/{userId}")]
    public async Task<ActionResult<IEnumerable<OrderSummary>>> GetOrdersByUser(Guid userId)
    {
        var result = await _mediator.Send(new GetOrderSummariesByUserIdQuery(userId));
        return Ok(result);
    }

    // POST: api/orders
    [HttpPost]
    public async Task<ActionResult<OrderResponseDto>> CreateOrder(OrderRequestDto requestDto)
    {
        var result = await _mediator.Send(new PlaceOrderCommand(requestDto.UserId, requestDto.OrderItems));
        return CreatedAtAction(nameof(GetOrder), new { id = result.Id }, result);
    }

    // POST: api/orders/{id}/confirm-payment
    [HttpPost("{id}/confirm-payment")]
    public async Task<IActionResult> ConfirmPayment(Guid id)
    {
        var result = await _mediator.Send(new ConfirmPaymentCommand(id));
        if (!result) return BadRequest("Could not confirm payment.");
        return NoContent();
    }

    // POST: api/orders/{id}/ship
    [HttpPost("{id}/ship")]
    public async Task<IActionResult> ShipOrder(Guid id)
    {
        var result = await _mediator.Send(new ShipOrderCommand(id));
        if (!result) return BadRequest("Could not ship order.");
        return NoContent();
    }
}
