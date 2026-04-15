using System.Text;
using System.Text.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using OrderingService.DTOs.OrderDTO;

namespace OrderingService.AsyncDataServices;

public interface IMessageBusClient
{
    Task PublishOrderCreated(OrderCreatedDto orderCreatedDto);
}

public class MessageBusClient : IMessageBusClient, IAsyncDisposable, IDisposable
{
    private readonly IConfiguration _configuration;
    private IConnection? _connection;
    private IChannel? _channel;
    private const string ExchangeName = "trigger";

    public MessageBusClient(IConfiguration configuration)
    {
        _configuration = configuration;
        InitializeRabbitMQ().GetAwaiter().GetResult();
    }

    private async Task InitializeRabbitMQ()
    {
        var factory = new ConnectionFactory()
        {
            HostName = _configuration["RabbitMQHost"] ?? "localhost",
            Port = int.Parse(_configuration["RabbitMQPort"] ?? "5672")
        };

        try
        {
            _connection = await factory.CreateConnectionAsync();
            _channel = await _connection.CreateChannelAsync();

            if (_channel != null)
            {
                await _channel.ExchangeDeclareAsync(exchange: ExchangeName, type: ExchangeType.Fanout);
            }

            if (_connection != null)
            {
                _connection.ConnectionShutdownAsync += RabbitMQ_ConnectionShutdown;
            }

            Console.WriteLine("--> Connected to Message Bus (Publisher)");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"--> Could not connect to the Message Bus: {ex.Message}");
        }
    }

    private Task RabbitMQ_ConnectionShutdown(object sender, ShutdownEventArgs e)
    {
        Console.WriteLine("--> RabbitMQ Connection Shutdown");
        return Task.CompletedTask;
    }

    public async Task PublishOrderCreated(OrderCreatedDto orderCreatedDto)
    {
        var message = JsonSerializer.Serialize(orderCreatedDto);

        if (_connection != null && _connection.IsOpen)
        {
            Console.WriteLine("--> RabbitMQ Connection Open, sending message...");
            await SendMessage(message);
        }
        else
        {
            Console.WriteLine("--> RabbitMQ connection is closed, not sending");
        }
    }

    private async Task SendMessage(string message)
    {
        var body = Encoding.UTF8.GetBytes(message);

        if (_channel != null)
        {
            await _channel.BasicPublishAsync(exchange: ExchangeName,
                            routingKey: string.Empty,
                            body: body);
            Console.WriteLine($"--> We have sent {message}");
        }
    }

    public void Dispose()
    {
        DisposeAsync().AsTask().GetAwaiter().GetResult();
    }

    public async ValueTask DisposeAsync()
    {
        Console.WriteLine("MessageBus Disposed");
        if (_channel?.IsOpen == true)
        {
            await _channel.CloseAsync();
            await _connection!.CloseAsync();
        }
    }
}