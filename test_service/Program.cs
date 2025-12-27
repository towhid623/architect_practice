using test_service.Configuration;
using test_service.Data;
using SharedKernel.Messaging;
using Infrastructure.Messaging;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();

// Configure Swagger/OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.CustomSchemaIds(type => type.FullName);
});

// Configure MongoDB
var mongoDbSettings = builder.Configuration.GetSection(MongoDbSettings.SectionName).Get<MongoDbSettings>();
if (mongoDbSettings == null || string.IsNullOrEmpty(mongoDbSettings.ConnectionString))
{
    throw new InvalidOperationException("MongoDB configuration is missing or invalid");
}

builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    options.UseMongoDB(mongoDbSettings.ConnectionString, mongoDbSettings.DatabaseName);
});

// Configure RabbitMQ - Get connection string directly
var rabbitMqConnectionString = builder.Configuration["RabbitMq:ConnectionString"]
    ?? throw new InvalidOperationException("RabbitMQ connection string not configured");

// Register RabbitMQ Message Bus with connection string
builder.Services.AddSingleton<IMessageBus>(sp =>
{
    var logger = sp.GetRequiredService<ILogger<RabbitMqMessageBus>>();
    return new RabbitMqMessageBus(rabbitMqConnectionString, logger);
});

var app = builder.Build();

// Initialize MongoDB
using (var scope = app.Services.CreateScope())
{
    try
    {
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        // This will create the database and apply any pending migrations
        await dbContext.Database.EnsureCreatedAsync();
        app.Logger.LogInformation("? MongoDB initialized");
    }
    catch (Exception ex)
    {
        app.Logger.LogError(ex, "? MongoDB initialization failed");
    }
}

// Initialize RabbitMQ
try
{
    var messageBus = app.Services.GetRequiredService<IMessageBus>();
    await messageBus.ConnectAsync();
    app.Logger.LogInformation("? RabbitMQ connected");
}
catch (Exception ex)
{
    app.Logger.LogError(ex, "? RabbitMQ connection failed");
}

// Configure HTTP request pipeline
app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "Medicine API v1");
    options.RoutePrefix = string.Empty;
});

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Logger.LogInformation("?? Medicine API Ready - DDD Event-Driven Architecture");

app.Run();
