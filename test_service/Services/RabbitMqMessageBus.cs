using System.Text;
using System.Text.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace test_service.Services;

/// <summary>
/// RabbitMQ implementation of the message bus
/// </summary>
public class RabbitMqMessageBus : IMessageBus, IAsyncDisposable
{
    private readonly string _connectionString;
    private readonly ILogger<RabbitMqMessageBus> _logger;
    private IConnection? _connection;
    private IModel? _channel;
    private readonly SemaphoreSlim _connectionLock = new(1, 1);

    public RabbitMqMessageBus(string connectionString, ILogger<RabbitMqMessageBus> logger)
    {
    _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
   _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Connects to RabbitMQ
    /// </summary>
    public async Task ConnectAsync()
    {
        await _connectionLock.WaitAsync();
        try
        {
  if (_connection != null && _connection.IsOpen)
     {
        _logger.LogInformation("Already connected to RabbitMQ");
 return;
       }

            _logger.LogInformation("Connecting to RabbitMQ...");

            await Task.Run(() =>
    {
             var factory = new ConnectionFactory
     {
           Uri = new Uri(_connectionString),
       AutomaticRecoveryEnabled = true,
        NetworkRecoveryInterval = TimeSpan.FromSeconds(10)
          };

     _connection = factory.CreateConnection();
        _channel = _connection.CreateModel();
          });

            _logger.LogInformation("Successfully connected to RabbitMQ");
        }
      catch (Exception ex)
    {
  _logger.LogError(ex, "Failed to connect to RabbitMQ");
 throw;
        }
     finally
 {
          _connectionLock.Release();
        }
    }

    /// <summary>
    /// Publishes a message to the specified queue
    /// </summary>
    public async Task PublishAsync<T>(string queueName, T message, CancellationToken cancellationToken = default) where T : class
    {
        if (_channel == null || _connection == null || !_connection.IsOpen)
     {
   await ConnectAsync();
        }

        if (_channel == null)
        {
      throw new InvalidOperationException("Channel is not initialized");
   }

        try
        {
            await Task.Run(() =>
   {
        // Declare queue (idempotent operation)
                _channel.QueueDeclare(
         queue: queueName,
               durable: true,
        exclusive: false,
      autoDelete: false,
      arguments: null
         );

             var json = JsonSerializer.Serialize(message);
      var body = Encoding.UTF8.GetBytes(json);

    var properties = _channel.CreateBasicProperties();
       properties.Persistent = true;
           properties.ContentType = "application/json";

         _channel.BasicPublish(
            exchange: string.Empty,
      routingKey: queueName,
 basicProperties: properties,
                body: body
      );
    }, cancellationToken);

            _logger.LogInformation("Published message to queue {QueueName}", queueName);
        }
        catch (Exception ex)
   {
       _logger.LogError(ex, "Failed to publish message to queue {QueueName}", queueName);
            throw;
        }
    }

    /// <summary>
    /// Subscribes to a queue and processes messages
    /// </summary>
    public async Task SubscribeAsync<T>(string queueName, Func<T, Task> handler, CancellationToken cancellationToken = default) where T : class
 {
        if (_channel == null || _connection == null || !_connection.IsOpen)
        {
     await ConnectAsync();
        }

        if (_channel == null)
{
        throw new InvalidOperationException("Channel is not initialized");
   }

        try
        {
  await Task.Run(() =>
   {
// Declare queue (idempotent operation)
       _channel.QueueDeclare(
     queue: queueName,
          durable: true,
           exclusive: false,
                    autoDelete: false,
        arguments: null
         );

 // Set prefetch count to 1 for fair dispatch
          _channel.BasicQos(
             prefetchSize: 0,
    prefetchCount: 1,
            global: false
                );

var consumer = new EventingBasicConsumer(_channel);

      consumer.Received += async (model, ea) =>
    {
          try
     {
 var body = ea.Body.ToArray();
         var json = Encoding.UTF8.GetString(body);
      var message = JsonSerializer.Deserialize<T>(json);

                  if (message != null)
         {
              await handler(message);
    _channel.BasicAck(ea.DeliveryTag, false);
            _logger.LogInformation("Processed message from queue {QueueName}", queueName);
    }
     else
  {
        _logger.LogWarning("Failed to deserialize message from queue {QueueName}", queueName);
   _channel.BasicNack(ea.DeliveryTag, false, false);
     }
          }
          catch (Exception ex)
        {
                _logger.LogError(ex, "Error processing message from queue {QueueName}", queueName);
             // Negative acknowledgment - message will be requeued
         _channel.BasicNack(ea.DeliveryTag, false, true);
             }
                };

         _channel.BasicConsume(
        queue: queueName,
           autoAck: false,
        consumer: consumer
     );
         }, cancellationToken);

            _logger.LogInformation("Subscribed to queue {QueueName}", queueName);
        }
 catch (Exception ex)
        {
    _logger.LogError(ex, "Failed to subscribe to queue {QueueName}", queueName);
          throw;
        }
    }

    /// <summary>
    /// Disconnects from RabbitMQ
    /// </summary>
    public async Task DisconnectAsync()
    {
        await _connectionLock.WaitAsync();
        try
        {
      await Task.Run(() =>
         {
    if (_channel != null)
        {
           _channel.Close();
            _channel.Dispose();
_channel = null;
      }

              if (_connection != null)
 {
         _connection.Close();
      _connection.Dispose();
   _connection = null;
       }
   });

            _logger.LogInformation("Disconnected from RabbitMQ");
        }
     catch (Exception ex)
        {
            _logger.LogError(ex, "Error while disconnecting from RabbitMQ");
        }
        finally
      {
         _connectionLock.Release();
        }
    }

    public async ValueTask DisposeAsync()
    {
        await DisconnectAsync();
        _connectionLock.Dispose();
     GC.SuppressFinalize(this);
    }
}
