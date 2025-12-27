using test_service.Services;
using test_service.Models;

namespace test_service.BackgroundServices;

/// <summary>
/// Background service for consuming messages from RabbitMQ queues
/// This is a production-ready pattern for handling message subscriptions
/// </summary>
public class MessageConsumerService : BackgroundService
{
    private readonly IMessageBus _messageBus;
    private readonly ILogger<MessageConsumerService> _logger;

    public MessageConsumerService(IMessageBus messageBus, ILogger<MessageConsumerService> logger)
    {
        _messageBus = messageBus;
   _logger = logger;
}

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
     _logger.LogInformation("Message Consumer Service is starting");

        try
   {
  // Ensure connection is established
         await _messageBus.ConnectAsync();

 // Subscribe to your queues here
      // Example: Subscribe to an "orders" queue
    await _messageBus.SubscribeAsync<OrderMessage>("orders", async (order) =>
         {
           _logger.LogInformation("Processing order: {OrderId}", order.OrderId);
           
      // Your business logic here
      await ProcessOrderAsync(order);
        
    _logger.LogInformation("Order processed successfully: {OrderId}", order.OrderId);
            }, stoppingToken);

          // You can subscribe to multiple queues
            await _messageBus.SubscribeAsync<NotificationMessage>("notifications", async (notification) =>
         {
                _logger.LogInformation("Processing notification: {Type}", notification.Type);
    
                await ProcessNotificationAsync(notification);
       
         _logger.LogInformation("Notification processed successfully");
}, stoppingToken);

          _logger.LogInformation("Message Consumer Service is running");

     // Keep the service alive
        await Task.Delay(Timeout.Infinite, stoppingToken);
    }
        catch (OperationCanceledException)
 {
  _logger.LogInformation("Message Consumer Service is stopping");
        }
        catch (Exception ex)
   {
_logger.LogError(ex, "Fatal error in Message Consumer Service");
        throw;
        }
    }

    private async Task ProcessOrderAsync(OrderMessage order)
    {
     // Implement your order processing logic here
     // Example: Save to database, send confirmation email, etc.
        await Task.Delay(100); // Simulate processing
  }

    private async Task ProcessNotificationAsync(NotificationMessage notification)
    {
        // Implement your notification processing logic here
// Example: Send email, SMS, push notification, etc.
 await Task.Delay(50); // Simulate processing
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Message Consumer Service is stopping");
        
     // Disconnect from RabbitMQ
     await _messageBus.DisconnectAsync();
        
     await base.StopAsync(cancellationToken);
    }
}
