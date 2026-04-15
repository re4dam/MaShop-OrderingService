using MediatR;
using OrderingService.Models;

namespace OrderingService.Queries;

public record GetOrderSummariesQuery() : IRequest<IEnumerable<OrderSummary>>;

public record GetOrderSummaryByIdQuery(Guid OrderId) : IRequest<OrderSummary?>;

public record GetOrderSummariesByUserIdQuery(Guid UserId) : IRequest<IEnumerable<OrderSummary>>;
