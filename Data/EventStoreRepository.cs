using EventStore.Client;
using System.Text.Json;
using OrderingService.Models;
using OrderingService.Events;

namespace OrderingService.Data;

public class EventStoreRepository
{
    private readonly EventStoreClient _client;

    public EventStoreRepository(EventStoreClient client)
    {
        _client = client;
    }

    public async Task SaveAsync(Order order)
    {
        var events = order.GetUncommittedEvents();
        if (!events.Any()) return;

        var streamName = $"Order-{order.Id}";
        
        var eventData = events.Select(e =>
        {
            var eventType = e.GetType().Name;
            var json = JsonSerializer.Serialize(e);
            return new EventData(Uuid.NewUuid(), eventType, System.Text.Encoding.UTF8.GetBytes(json));
        });

        await _client.AppendToStreamAsync(streamName, StreamState.Any, eventData);
        order.ClearUncommittedEvents();
    }

    public async Task<Order?> GetByIdAsync(Guid id)
    {
        var streamName = $"Order-{id}";
        var order = new Order();
        
        try 
        {
            var readResult = _client.ReadStreamAsync(Direction.Forwards, streamName, StreamPosition.Start);
            if (await readResult.ReadState == ReadState.StreamNotFound)
                return null;

            var events = new List<object>();
            await foreach (var @event in readResult)
            {
                var eventTypeStr = @event.Event.EventType;
                var json = System.Text.Encoding.UTF8.GetString(@event.Event.Data.ToArray());
                
                object? e = eventTypeStr switch
                {
                    nameof(OrderPlacedEvent) => JsonSerializer.Deserialize<OrderPlacedEvent>(json),
                    nameof(OrderPaymentConfirmedEvent) => JsonSerializer.Deserialize<OrderPaymentConfirmedEvent>(json),
                    nameof(OrderShippedEvent) => JsonSerializer.Deserialize<OrderShippedEvent>(json),
                    _ => throw new Exception($"Unknown event type: {eventTypeStr}")
                };

                if (e != null)
                {
                    events.Add(e);
                }
            }

            order.LoadFromHistory(events);
            return order;
        }
        catch (StreamNotFoundException)
        {
            return null;
        }
    }
}