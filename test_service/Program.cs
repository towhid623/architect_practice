using test_service.Configuration;
using test_service.Data;
using SharedKernel.Messaging;
using SharedKernel.Configuration;
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

// Configure RabbitMQ
builder.Services.Configure<RabbitMqSettings>(
    builder.Configuration.GetSection(RabbitMqSettings.SectionName));

// Register RabbitMQ Message Bus as Singleton
builder.Services.AddSingleton<IMessageBus, RabbitMqMessageBus>();

var app = builder.Build();

// Initialize MongoDB
using (var scope = app.Services.CreateScope())
{
    try
    {
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        // This will create the database and apply any pending migrations
        await dbContext.Database.EnsureCreatedAsync();
        app.Logger.LogInformation("? MongoDB database initialized successfully");
    }
    catch (Exception ex)
    {
        app.Logger.LogError(ex, "? Failed to initialize MongoDB database");
    }
}

// Initialize RabbitMQ connection
try
{
    var messageBus = app.Services.GetRequiredService<IMessageBus>();
    await messageBus.ConnectAsync();
    app.Logger.LogInformation("? Successfully connected to RabbitMQ");
}
catch (Exception ex)
{
    app.Logger.LogError(ex, "? Failed to connect to RabbitMQ. Message bus features will not work.");
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

app.Logger.LogInformation("?? Medicine API started - Ready to accept medicine creation commands");

app.Run();
