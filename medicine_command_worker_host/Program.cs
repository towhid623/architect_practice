using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using SharedKernel.Messaging;
using SharedKernel.CQRS;
using SharedKernel.Commands.Medicine;
using SharedKernel.Common;
using SharedKernel.DTOs.Medicine;
using SharedKernel.Configuration;
using Infrastructure.Messaging;
using medicine_command_worker_host.Services;
using medicine_command_worker_host.Handlers;
using medicine_command_worker_host.Infrastructure.Repositories;

var builder = Host.CreateApplicationBuilder(args);

// Configure logging
builder.Logging.ClearProviders();
builder.Logging.AddConsole();

// Configure MongoDB Client
var mongoConnectionString = builder.Configuration["MongoDB:ConnectionString"]
    ?? throw new InvalidOperationException("MongoDB connection string not configured");

builder.Services.AddSingleton<IMongoClient>(sp => new MongoClient(mongoConnectionString));

// Register Domain Layer - Repository
builder.Services.AddScoped<medicine_command_worker_host.Domain.Repositories.IMedicineRepository, MedicineAggregateRepository>();

// Register Application Layer - Command Handler
builder.Services.AddScoped<ICommandHandler<CreateMedicineCommand, Result<MedicineResponse>>, CreateMedicineCommandHandler>();

// Configure RabbitMQ
builder.Services.Configure<RabbitMqSettings>(
    builder.Configuration.GetSection(RabbitMqSettings.SectionName));

// Register RabbitMQ Message Bus
builder.Services.AddSingleton<IMessageBus, RabbitMqMessageBus>();

// Register Background Consumer Service
builder.Services.AddHostedService<CreateMedicineConsumerService>();

var host = builder.Build();

// Initialize RabbitMQ connection
try
{
    var messageBus = host.Services.GetRequiredService<IMessageBus>();
    await messageBus.ConnectAsync();
    Console.WriteLine("✅ Successfully connected to RabbitMQ");
}
catch (Exception ex)
{
    Console.WriteLine($"❌ Failed to connect to RabbitMQ: {ex.Message}");
    throw;
}

Console.WriteLine();
Console.WriteLine("═══════════════════════════════════════════════════════");
Console.WriteLine("🚀 Medicine Command Worker Host - DDD Event-Driven");
Console.WriteLine("═══════════════════════════════════════════════════════");
Console.WriteLine();
Console.WriteLine("📋 Queue: thanos.medicine");
Console.WriteLine("🏗️  Architecture: Domain-Driven Design");
Console.WriteLine("📨 Pattern: Event-Driven Command Processing");
Console.WriteLine();
Console.WriteLine("✅ Ready to process CreateMedicineCommand");
Console.WriteLine("Press Ctrl+C to stop");
Console.WriteLine();

await host.RunAsync();
