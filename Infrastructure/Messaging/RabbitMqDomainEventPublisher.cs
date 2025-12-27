using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using SharedKernel.Domain.Events;
using SharedKernel.Messaging;

namespace Infrastructure.Messaging;

/// <summary>
/// RabbitMQ implementation for publishing domain events to fanout exchange
/// </summary>
public class RabbitMqDomainEventPublisher : IDomainEventPublisher
{
    private const string DomainEventsExchange = "thanos.domain.events";

    private readonly string _connectionString;
    private readonly ILogger<RabbitMqDomainEventPublisher> _logger;
    private IConnection? _connection;
    private IModel? _channel;
    private readonly SemaphoreSlim _lock = new(1, 1);

    public RabbitMqDomainEventPublisher(
        string connectionString,
      ILogger<RabbitMqDomainEventPublisher> logger)
    {
        _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task PublishAsync<TEvent>(TEvent domainEvent, CancellationToken cancellationToken = default)
        where TEvent : DomainEvent
    {
        if (domainEvent == null)
     throw new ArgumentNullException(nameof(domainEvent));

     await EnsureConnectionAsync();

        if (_channel == null)
            throw new InvalidOperationException("Channel not initialized");

     try
        {
            // Ensure fanout exchange exists
          _channel.ExchangeDeclare(
          exchange: DomainEventsExchange,
            type: ExchangeType.Fanout,
   durable: true,
     autoDelete: false,
                arguments: null);

     var eventType = domainEvent.GetType().Name;
  var json = JsonSerializer.Serialize(domainEvent, domainEvent.GetType());
            var body = Encoding.UTF8.GetBytes(json);

          var properties = _channel.CreateBasicProperties();
 properties.Persistent = true;
            properties.ContentType = "application/json";
            properties.Type = eventType;
            properties.MessageId = domainEvent.EventId.ToString();
    properties.Timestamp = new AmqpTimestamp(
         new DateTimeOffset(domainEvent.OccurredOn).ToUnixTimeSeconds());

  // Publish to fanout exchange (no routing key needed)
       _channel.BasicPublish(
      exchange: DomainEventsExchange,
                routingKey: string.Empty,  // Fanout ignores routing key
           mandatory: false,
   basicProperties: properties,
          body: body);

      _logger.LogInformation(
           "?? Published domain event: {EventType} (ID: {EventId}) to fanout exchange {Exchange}",
      eventType,
 domainEvent.EventId,
          DomainEventsExchange);
        }
 catch (Exception ex)
        {
            _logger.LogError(ex,
          "? Failed to publish domain event: {EventType}",
                domainEvent.GetType().Name);
            throw;
        }
    }

    public async Task PublishManyAsync(IEnumerable<DomainEvent> domainEvents, CancellationToken cancellationToken = default)
    {
      if (domainEvents == null)
        throw new ArgumentNullException(nameof(domainEvents));

        var eventsList = domainEvents.ToList();
   if (!eventsList.Any())
 return;

        _logger.LogInformation(
            "?? Publishing {Count} domain events to fanout exchange {Exchange}",
         eventsList.Count,
            DomainEventsExchange);

        foreach (var domainEvent in eventsList)
        {
 await PublishAsync(domainEvent, cancellationToken);
        }

        _logger.LogInformation(
            "? Successfully published {Count} domain events",
         eventsList.Count);
    }

    private async Task EnsureConnectionAsync()
    {
   if (_connection is { IsOpen: true } && _channel is { IsOpen: true })
 return;

        await _lock.WaitAsync();

  try
        {
        if (_connection is { IsOpen: true } && _channel is { IsOpen: true })
    return;

var factory = new ConnectionFactory
            {
    Uri = new Uri(_connectionString),
                AutomaticRecoveryEnabled = true,
    NetworkRecoveryInterval = TimeSpan.FromSeconds(10)
     };

   _connection = factory.CreateConnection();
       _channel = _connection.CreateModel();

            _logger.LogInformation("? Connected to RabbitMQ for domain event publishing (Exchange: {Exchange})", DomainEventsExchange);
     }
        finally
        {
    _lock.Release();
        }
    }

  public void Dispose()
    {
        _channel?.Close();
        _channel?.Dispose();
  _connection?.Close();
        _connection?.Dispose();
        _lock.Dispose();
    }
}
