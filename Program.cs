using Microsoft.EntityFrameworkCore;
using OrderingService.Data;
using OrderingService.EventProcessing;
using OrderingService.AsyncDataServices;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// Register DbContext (Using SQL Server)
builder.Services.AddDbContext<OrderingDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Register Processor and Subscriber
builder.Services.AddSingleton<IEventProcessor, EventProcessor>();
builder.Services.AddSingleton<IMessageBusClient, MessageBusClient>();
builder.Services.AddHostedService<MessageBusSubscriber>();

// Register EventStoreDB
var eventStoreConnectionString = builder.Configuration.GetConnectionString("EventStoreConnection") ?? "esdb://localhost:2113?tls=false&keepAliveTimeout=10000&keepAliveInterval=10000";
builder.Services.AddSingleton(new EventStore.Client.EventStoreClient(EventStore.Client.EventStoreClientSettings.Create(eventStoreConnectionString)));
builder.Services.AddScoped<EventStoreRepository>();

// Register Projector
builder.Services.AddHostedService<OrderProjector>();

// Register MediatR
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(Program).Assembly));

var app = builder.Build();

// Configure the HTTP request pipeline.
// if (app.Environment.IsDevelopment())
// {
//     app.UseSwagger();
//     app.UseSwaggerUI();
// }

app.UseAuthorization();
app.MapControllers();

app.Run();