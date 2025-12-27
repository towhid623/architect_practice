using test_service.Configuration;
using test_service.Services;
using test_service.Data;
using Microsoft.EntityFrameworkCore;
// using test_service.BackgroundServices;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

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

// Configure RabbitMQ
var rabbitMqSettings = builder.Configuration.GetSection(RabbitMqSettings.SectionName).Get<RabbitMqSettings>();
if (rabbitMqSettings == null || string.IsNullOrEmpty(rabbitMqSettings.ConnectionString))
{
    throw new InvalidOperationException("RabbitMQ configuration is missing or invalid");
}

// Register RabbitMQ Message Bus as Singleton
builder.Services.AddSingleton<IMessageBus>(sp =>
{
    var logger = sp.GetRequiredService<ILogger<RabbitMqMessageBus>>();
    return new RabbitMqMessageBus(rabbitMqSettings.ConnectionString, logger);
});

// Optional: Register Background Service for message consumption
// Uncomment this line to enable automatic message consumption on startup
// builder.Services.AddHostedService<MessageConsumerService>();

var app = builder.Build();

// Initialize MongoDB - Create database and collections if they don't exist
using (var scope = app.Services.CreateScope())
{
    try
    {
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        // This will create the database and apply any pending migrations
        await dbContext.Database.EnsureCreatedAsync();
        app.Logger.LogInformation("MongoDB database initialized successfully");
    }
    catch (Exception ex)
    {
        app.Logger.LogError(ex, "Failed to initialize MongoDB database");
    }
}

// Initialize RabbitMQ connection after the app is built
try
{
    var messageBus = app.Services.GetRequiredService<IMessageBus>();
    await messageBus.ConnectAsync();
    app.Logger.LogInformation("Successfully connected to RabbitMQ");
}
catch (Exception ex)
{
    app.Logger.LogError(ex, "Failed to connect to RabbitMQ during startup. The application will continue but message bus features may not work.");
}

// Configure the HTTP request pipeline.
app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "Test Service API v1");
    options.RoutePrefix = string.Empty; // Set Swagger UI at the app's root (http://localhost:5000/)
});

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
