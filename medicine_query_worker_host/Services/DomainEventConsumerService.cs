using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;
using SharedKernel.Domain.Events;

namespace medicine_query_worker_host.Services;

/// <summary>
/// Background service that listens to domain events from fanout exchange
/// This service updates read models based on domain events (CQRS pattern)
/// </summary>
public class DomainEventConsumerService : BackgroundService
{
    private const string DomainEventsExchange = "thanos.domain.events";
    private const string QueueName = "thanos.medicine.query.domain.events";

    private readonly string _connectionString;
    private readonly ILogger<DomainEventConsumerService> _logger;
    private IConnection? _connection;
    private IModel? _channel;

    public DomainEventConsumerService(
        IConfiguration configuration,
        ILogger<DomainEventConsumerService> logger)
    {
        _connectionString = configuration["RabbitMq:ConnectionString"]
?? throw new InvalidOperationException("RabbitMQ connection string not configured");
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("?? Domain Event Consumer Service starting (Query Side - CQRS)...");

        try
        {
         await InitializeRabbitMqAsync();

            if (_channel == null)
                throw new InvalidOperationException("Channel not initialized");

      // Declare fanout exchange
         _channel.ExchangeDeclare(
      exchange: DomainEventsExchange,
              type: ExchangeType.Fanout,
        durable: true,
         autoDelete: false,
          arguments: null);

  // Declare queue for this consumer
        _channel.QueueDeclare(
     queue: QueueName,
                durable: true,
          exclusive: false,
     autoDelete: false,
                arguments: null);

        // Bind queue to fanout exchange (no routing key needed)
            _channel.QueueBind(
       queue: QueueName,
           exchange: DomainEventsExchange,
        routingKey: string.Empty);  // Fanout ignores routing key

  var consumer = new EventingBasicConsumer(_channel);

 consumer.Received += async (model, ea) =>
     {
 try
        {
            var body = ea.Body.ToArray();
          var json = Encoding.UTF8.GetString(body);
         var eventType = ea.BasicProperties.Type;
var messageId = ea.BasicProperties.MessageId;

      _logger.LogInformation(
             "?? Received domain event: {EventType} (MessageId: {MessageId})",
 eventType,
     messageId);

              // Handle event - Update read model
          await HandleDomainEventAsync(eventType, json, stoppingToken);

     _channel.BasicAck(ea.DeliveryTag, false);

        _logger.LogInformation(
         "? Processed domain event: {EventType}",
           eventType);
             }
     catch (Exception ex)
     {
   _logger.LogError(ex,
         "? Error processing domain event");

          _channel.BasicNack(ea.DeliveryTag, false, true);
                }
   };

        _channel.BasicConsume(
              queue: QueueName,
              autoAck: false,
         consumer: consumer);

       _logger.LogInformation(
      "? Query Side listening on exchange: {Exchange}, queue: {Queue}",
          DomainEventsExchange,
           QueueName);

     await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("?? Domain Event Consumer stopping...");
    }
     catch (Exception ex)
   {
        _logger.LogError(ex, "?? Fatal error in Domain Event Consumer");
   throw;
   }
    }

    private async Task HandleDomainEventAsync(string eventType, string json, CancellationToken cancellationToken)
{
        await Task.Run(() =>
        {
            _logger.LogInformation("?? Updating Read Model for {EventType}", eventType);

            // Handle different event types to update read models
       switch (eventType)
        {
   case nameof(MedicineCreatedEvent):
        var createdEvent = JsonSerializer.Deserialize<MedicineCreatedEvent>(json);
           if (createdEvent != null)
        {
      _logger.LogInformation(
           "?? [READ MODEL] Medicine Created: {Name} (ID: {MedicineId})",
  createdEvent.Name,
     createdEvent.MedicineId);
         
            // TODO: Update read model/database
             // await _readModelRepository.CreateAsync(createdEvent);
      // await _searchIndexService.IndexAsync(createdEvent);
         // await _cacheService.InvalidateAsync(createdEvent.MedicineId);
        }
           break;

          case nameof(MedicineStockUpdatedEvent):
          var stockEvent = JsonSerializer.Deserialize<MedicineStockUpdatedEvent>(json);
        if (stockEvent != null)
     {
   _logger.LogInformation(
    "?? [READ MODEL] Stock Updated: {MedicineId} - {OldStock} ? {NewStock}",
      stockEvent.MedicineId,
  stockEvent.OldStock,
          stockEvent.NewStock);
               
    // TODO: Update read model stock
               // await _readModelRepository.UpdateStockAsync(stockEvent);
     // await _cacheService.InvalidateAsync(stockEvent.MedicineId);
          }
         break;

            case nameof(MedicinePriceChangedEvent):
           var priceEvent = JsonSerializer.Deserialize<MedicinePriceChangedEvent>(json);
                    if (priceEvent != null)
   {
  _logger.LogInformation(
 "?? [READ MODEL] Price Changed: {MedicineId} - ${OldPrice} ? ${NewPrice}",
     priceEvent.MedicineId,
  priceEvent.OldPrice,
            priceEvent.NewPrice);
     
     // TODO: Update read model price
         // await _readModelRepository.UpdatePriceAsync(priceEvent);
         // await _cacheService.InvalidateAsync(priceEvent.MedicineId);
     }
        break;

     case nameof(MedicineAvailabilityChangedEvent):
        var availEvent = JsonSerializer.Deserialize<MedicineAvailabilityChangedEvent>(json);
             if (availEvent != null)
  {
         _logger.LogInformation(
      "?? [READ MODEL] Availability Changed: {MedicineId} - {IsAvailable}",
            availEvent.MedicineId,
   availEvent.IsAvailable ? "Available" : "Unavailable");
       
    // TODO: Update read model availability
             // await _readModelRepository.UpdateAvailabilityAsync(availEvent);
       // await _searchIndexService.UpdateAsync(availEvent);
        }
      break;

    case nameof(MedicineUpdatedEvent):
  var updatedEvent = JsonSerializer.Deserialize<MedicineUpdatedEvent>(json);
          if (updatedEvent != null)
    {
    _logger.LogInformation(
          "?? [READ MODEL] Medicine Updated: {Name} (ID: {MedicineId})",
              updatedEvent.Name,
          updatedEvent.MedicineId);
  
   // TODO: Update read model
       // await _readModelRepository.UpdateAsync(updatedEvent);
    // await _searchIndexService.ReindexAsync(updatedEvent.MedicineId);
  }
         break;

    default:
         _logger.LogWarning("?? Unknown event type: {EventType}", eventType);
     break;
            }
        }, cancellationToken);
    }

    private async Task InitializeRabbitMqAsync()
    {
      var factory = new ConnectionFactory
        {
 Uri = new Uri(_connectionString),
            AutomaticRecoveryEnabled = true,
            NetworkRecoveryInterval = TimeSpan.FromSeconds(10)
        };

        _connection = factory.CreateConnection();
      _channel = _connection.CreateModel();

        await Task.CompletedTask;
    }

    public override void Dispose()
    {
        _channel?.Close();
        _channel?.Dispose();
  _connection?.Close();
        _connection?.Dispose();
   base.Dispose();
    }
}
