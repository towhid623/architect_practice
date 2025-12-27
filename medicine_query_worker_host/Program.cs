using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using medicine_query_worker_host.Services;

var builder = Host.CreateApplicationBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddConsole();

// Register Domain Event Consumer Service
builder.Services.AddHostedService<DomainEventConsumerService>();

var host = builder.Build();

Console.WriteLine();
Console.WriteLine("═══════════════════════════════════════════════════════");
Console.WriteLine("🔍 Medicine Query Worker - CQRS Read Side");
Console.WriteLine("═══════════════════════════════════════════════════════");
Console.WriteLine();
Console.WriteLine("📖 Purpose: Listen to domain events and update read models");
Console.WriteLine("🎧 Exchange: thanos.domain.events (fanout)");
Console.WriteLine("📋 Queue: thanos.medicine.query.domain.events");
Console.WriteLine();
Console.WriteLine("✅ Ready to consume domain events");
Console.WriteLine("✅ Ready to update read models");
Console.WriteLine();
Console.WriteLine("Press Ctrl+C to stop");
Console.WriteLine();

await host.RunAsync();
