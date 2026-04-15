using System.Text;
using Microsoft.Extensions.Hosting;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using OrderingService.EventProcessing;

namespace OrderingService.AsyncDataServices;

public class MessageBusSubscriber : BackgroundService
{
    private readonly IConfiguration _configuration;
    private readonly IEventProcessor _eventProcessor;
    private IConnection? _connection;
    private IChannel? _channel;
    private string? _queueName;

    public MessageBusSubscriber(IConfiguration configuration, IEventProcessor eventProcessor)
    {
        _configuration = configuration;
        _eventProcessor = eventProcessor;
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
                await _channel.ExchangeDeclareAsync(exchange: "trigger", type: ExchangeType.Fanout);
                var queueDeclareResponse = await _channel.QueueDeclareAsync();
                _queueName = queueDeclareResponse.QueueName;
                await _channel.QueueBindAsync(queue: _queueName, exchange: "trigger", routingKey: "");
            }

            if (_connection != null)
            {
                _connection.ConnectionShutdownAsync += RabbitMQ_ConnectionShutdown;
            }

            Console.WriteLine("--> Listening on the Message Bus...");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"--> Could not connect to the Message Bus: {ex.Message}");
        }
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        stoppingToken.ThrowIfCancellationRequested();

        await InitializeRabbitMQ();

        if (_channel == null || _queueName == null)
        {
            Console.WriteLine("--> RabbitMQ Channel or Queue is not initialized.");
            return;
        }

        var consumer = new AsyncEventingBasicConsumer(_channel);

        consumer.ReceivedAsync += (ModuleHandle, ea) =>
        {
            Console.WriteLine("--> Event Received!");

            var body = ea.Body;
            var notificationMessage = Encoding.UTF8.GetString(body.ToArray());

            _eventProcessor.ProcessEvent(notificationMessage);
            
            return Task.CompletedTask;
        };

        await _channel.BasicConsumeAsync(queue: _queueName, autoAck: true, consumer: consumer);
    }

    private Task RabbitMQ_ConnectionShutdown(object sender, ShutdownEventArgs e)
    {
        Console.WriteLine("--> Connection Shutdown");
        return Task.CompletedTask;
    }

    public override void Dispose()
    {
        if (_channel?.IsOpen == true)
        {
            _channel.CloseAsync().GetAwaiter().GetResult();
            _connection?.CloseAsync().GetAwaiter().GetResult();
        }

        base.Dispose();
    }
}