using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using SharedKernel.Messaging;
using SharedKernel.CQRS;
using SharedKernel.Commands.Medicine;
using SharedKernel.Common;
using SharedKernel.DTOs.Medicine;
using SharedKernel.Repositories;
using Infrastructure.Messaging;
using Infrastructure.Repositories;
using medicine_command_worker_host.Services;
using medicine_command_worker_host.Handlers;

var builder = Host.CreateApplicationBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddConsole();

// Configure MongoDB
var mongoConnectionString = builder.Configuration["MongoDB:ConnectionString"]
    ?? throw new InvalidOperationException("MongoDB connection string not configured");

builder.Services.AddSingleton<IMongoClient>(sp => new MongoClient(mongoConnectionString));

// Register Repository (Infrastructure layer)
builder.Services.AddScoped<IMedicineRepository, MedicineRepository>();

// Configure RabbitMQ - Get connection string directly
var rabbitMqConnectionString = builder.Configuration["RabbitMq:ConnectionString"]
    ?? throw new InvalidOperationException("RabbitMQ connection string not configured");

// Register RabbitMQ Message Bus (for commands)
builder.Services.AddSingleton<IMessageBus>(sp =>
{
    var logger = sp.GetRequiredService<ILogger<RabbitMqMessageBus>>();
    return new RabbitMqMessageBus(rabbitMqConnectionString, logger);
});

// Register Domain Event Publisher (for domain events - fanout exchange)
builder.Services.AddSingleton<IDomainEventPublisher>(sp =>
{
    var logger = sp.GetRequiredService<ILogger<RabbitMqDomainEventPublisher>>();
    return new RabbitMqDomainEventPublisher(rabbitMqConnectionString, logger);
});

// Register Command Handler (Application layer)
builder.Services.AddScoped<ICommandHandler<CreateMedicineCommand, Result<MedicineResponse>>, CreateMedicineCommandHandler>();

// Register Command Consumer Service (Write Side - CQRS)
builder.Services.AddHostedService<CreateMedicineConsumerService>();

var host = builder.Build();

// Initialize RabbitMQ
try
{
    var messageBus = host.Services.GetRequiredService<IMessageBus>();
    await messageBus.ConnectAsync();
    Console.WriteLine("✅ Connected to RabbitMQ (Commands)");
}
catch (Exception ex)
{
    Console.WriteLine($"❌ RabbitMQ connection failed: {ex.Message}");
    throw;
}

Console.WriteLine();
Console.WriteLine("═══════════════════════════════════════════════════════");
Console.WriteLine("✍️  Medicine Command Worker - CQRS Write Side");
Console.WriteLine("═══════════════════════════════════════════════════════");
Console.WriteLine();
Console.WriteLine("📦 SharedKernel: Domain Model, Repository Interface");
Console.WriteLine("🏗️ Infrastructure: Repository Implementation");
Console.WriteLine("⚙️  Application: Command Handlers");
Console.WriteLine();
Console.WriteLine("📋 Command Queue: thanos.medicine");
Console.WriteLine("📢 Event Exchange: thanos.domain.events (fanout)");
Console.WriteLine();
Console.WriteLine("✅ Ready to process commands");
Console.WriteLine("✅ Ready to publish domain events");
Console.WriteLine();
Console.WriteLine("Press Ctrl+C to stop");
Console.WriteLine();

await host.RunAsync();
