using MediatR;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using OrderingService.Commands;
using OrderingService.Data;
using OrderingService.DTOs.OrderDTO;
using OrderingService.DTOs.OrderItemDTO;

namespace OrderingService.Handlers;

public class OrderCommandHandlers : 
    IRequestHandler<PlaceOrderCommand, OrderResponseDto>,
    IRequestHandler<ConfirmPaymentCommand, bool>,
    IRequestHandler<ShipOrderCommand, bool>
{
    private readonly EventStoreRepository _repository;
    private readonly string _connectionString;

    public OrderCommandHandlers(EventStoreRepository repository, IConfiguration configuration)
    {
        _repository = repository;
        _connectionString = configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Missing connection string");
    }

    public async Task<OrderResponseDto> Handle(PlaceOrderCommand request, CancellationToken cancellationToken)
    {
        string userName = "Unknown User";
        var itemsWithDetails = new List<(Guid ProductId, int Quantity, decimal UnitPrice)>();

        // Fetch User and Product information using ADO.NET (No Entity Framework)
        using (var connection = new SqlConnection(_connectionString))
        {
            await connection.OpenAsync(cancellationToken);

            // Fetch user
            using (var userCmd = new SqlCommand("SELECT Name FROM Users WHERE Id = @Id", connection))
            {
                userCmd.Parameters.AddWithValue("@Id", request.UserId);
                var result = await userCmd.ExecuteScalarAsync(cancellationToken);
                if (result == null) throw new Exception("User not found.");
                userName = (string)result;
            }

            // Fetch products
            foreach (var item in request.OrderItems)
            {
                using (var prodCmd = new SqlCommand("SELECT Price FROM Products WHERE Id = @Id", connection))
                {
                    prodCmd.Parameters.AddWithValue("@Id", item.ProductId);
                    var priceResult = await prodCmd.ExecuteScalarAsync(cancellationToken);
                    if (priceResult == null) throw new Exception($"Product with ID {item.ProductId} not found.");
                    
                    decimal price = (decimal)priceResult;
                    itemsWithDetails.Add((item.ProductId, item.Quantity, price));
                }
            }
        }

        var orderId = Guid.NewGuid();
        var order = new OrderingService.Models.Order(orderId, request.UserId, userName, itemsWithDetails);

        await _repository.SaveAsync(order);

        // Map to Response DTO
        return new OrderResponseDto
        {
            Id = order.Id,
            OrderDate = order.OrderDate,
            UserId = order.UserId,
            UserName = userName,
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
        var order = await _repository.GetByIdAsync(request.OrderId);
        if (order == null) return false;

        order.MarkAsPaid();
        await _repository.SaveAsync(order);
        return true;
    }

    public async Task<bool> Handle(ShipOrderCommand request, CancellationToken cancellationToken)
    {
        var order = await _repository.GetByIdAsync(request.OrderId);
        if (order == null) return false;

        order.MarkAsShipped();
        await _repository.SaveAsync(order);
        return true;
    }
}