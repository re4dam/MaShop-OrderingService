using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using OrderingService.Data;
using OrderingService.DTOs.UserDTO;
using OrderingService.DTOs.ProductDTO;
using OrderingService.Models;

namespace OrderingService.EventProcessing;

public class EventProcessor : IEventProcessor
{
    private readonly IServiceScopeFactory _scopeFactory;

    public EventProcessor(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    public void ProcessEvent(string message)
    {
        var eventType = DetermineEvent(message);

        switch (eventType)
        {
            case EventType.UserPublished:
                AddUser(message);
                break;
            case EventType.ProductPublished:
                AddProduct(message);
                break;
            default:
                break;
        }
    }

    private EventType DetermineEvent(string notificationMessage)
    {
        Console.WriteLine("--> Determining Event");

        var eventType = JsonSerializer.Deserialize<GenericEventDto>(notificationMessage);

        switch (eventType?.Event)
        {
            case "User_Published":
                Console.WriteLine("--> User Published Event Detected");
                return EventType.UserPublished;
            case "Product_Published":
                Console.WriteLine("--> Product Published Event Detected");
                return EventType.ProductPublished;
            default:
                Console.WriteLine("--> Could not determine event type");
                return EventType.Undetermined;
        }
    }

    private void AddUser(string userPublishedMessage)
    {
        using (var scope = _scopeFactory.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<OrderingDbContext>();

            var userPublishedDto = JsonSerializer.Deserialize<UserPublishedDto>(userPublishedMessage);

            try
            {
                var user = new User
                {
                    Id = userPublishedDto!.Id,
                    Name = userPublishedDto.Name,
                    Address = userPublishedDto.Address,
                    Contact = userPublishedDto.Contact
                };

                if (!dbContext.Users.Any(u => u.Id == user.Id))
                {
                    dbContext.Users.Add(user);
                    dbContext.SaveChanges();
                    Console.WriteLine("--> User added to OrderingService database");
                }
                else
                {
                    Console.WriteLine("--> User already exists...");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"--> Could not add user to DB: {ex.Message}");
            }
        }
    }

    private void AddProduct(string productPublishedMessage)
    {
        using (var scope = _scopeFactory.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<OrderingDbContext>();

            var productPublishedDto = JsonSerializer.Deserialize<ProductPublishedDto>(productPublishedMessage);

            try
            {
                var product = new Product
                {
                    Id = productPublishedDto!.Id,
                    Name = productPublishedDto.Name,
                    Price = productPublishedDto.Price,
                    Quantity = productPublishedDto.UnitsInStock
                };

                if (!dbContext.Products.Any(p => p.Id == product.Id))
                {
                    dbContext.Products.Add(product);
                    dbContext.SaveChanges();
                    Console.WriteLine("--> Product added to OrderingService database");
                }
                else
                {
                    Console.WriteLine("--> Product already exists...");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"--> Could not add product to DB: {ex.Message}");
            }
        }
    }
}

enum EventType
{
    UserPublished,
    ProductPublished,
    Undetermined
}

class GenericEventDto
{
    public string Event { get; set; } = string.Empty;
}