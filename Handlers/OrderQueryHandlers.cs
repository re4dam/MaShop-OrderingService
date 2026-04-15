using MediatR;
using Microsoft.EntityFrameworkCore;
using OrderingService.Queries;
using OrderingService.Data;
using OrderingService.Models;

namespace OrderingService.Handlers;

public class OrderQueryHandlers : 
    IRequestHandler<GetOrderSummariesQuery, IEnumerable<OrderSummary>>,
    IRequestHandler<GetOrderSummaryByIdQuery, OrderSummary?>,
    IRequestHandler<GetOrderSummariesByUserIdQuery, IEnumerable<OrderSummary>>
{
    private readonly OrderingDbContext _context;

    public OrderQueryHandlers(OrderingDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<OrderSummary>> Handle(GetOrderSummariesQuery request, CancellationToken cancellationToken)
    {
        return await _context.OrderSummaries.ToListAsync(cancellationToken);
    }

    public async Task<OrderSummary?> Handle(GetOrderSummaryByIdQuery request, CancellationToken cancellationToken)
    {
        return await _context.OrderSummaries.FirstOrDefaultAsync(s => s.OrderId == request.OrderId, cancellationToken);
    }

    public async Task<IEnumerable<OrderSummary>> Handle(GetOrderSummariesByUserIdQuery request, CancellationToken cancellationToken)
    {
        return await _context.OrderSummaries
            .Where(s => s.UserId == request.UserId)
            .ToListAsync(cancellationToken);
    }
}
