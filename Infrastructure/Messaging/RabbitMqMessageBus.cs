using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using SharedKernel.Messaging;
using SharedKernel.CQRS;

namespace Infrastructure.Messaging;

/// <summary>
/// RabbitMQ implementation of the message bus
/// </summary>
public sealed class RabbitMqMessageBus : IMessageBus, IAsyncDisposable
{
    private const string ExchangeName = "medicine.events";
    private const string QueuePrefix = "thanos.";

    private readonly string _connectionString;
    private readonly ILogger<RabbitMqMessageBus> _logger;

    private IConnection? _connection;
    private IModel? _publishChannel;
    private readonly Dictionary<string, IModel> _consumerChannels = new();
    private readonly SemaphoreSlim _connectionLock = new(1, 1);

    public RabbitMqMessageBus(
        string connectionString,
        ILogger<RabbitMqMessageBus> logger)
    {
        _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    // -------------------- COMMAND SEND --------------------

    public async Task SendAsync<TCommand>(
        TCommand command,
        CancellationToken cancellationToken = default)
        where TCommand : class, ICommand
    {
        if (command is null)
            throw new ArgumentNullException(nameof(command));

        var queueName = GetQueueName<TCommand>();

        _logger.LogInformation(
            "Sending command {CommandType} → Queue {QueueName}",
            typeof(TCommand).Name,
            queueName);

        await PublishAsync(queueName, command, cancellationToken);
    }

    // -------------------- PUBLISH --------------------

    public async Task PublishAsync<T>(
        string queueName,
        T message,
        CancellationToken cancellationToken = default)
        where T : class
    {
        if (_publishChannel is null || _connection is null || !_connection.IsOpen)
            await ConnectAsync();

        if (_publishChannel is null)
            throw new InvalidOperationException("Publish channel not initialized");

        EnsureTopology(_publishChannel, queueName);

        var json = JsonSerializer.Serialize(message);
        var body = Encoding.UTF8.GetBytes(json);

        var properties = _publishChannel.CreateBasicProperties();
        properties.Persistent = true;
        properties.ContentType = "application/json";

        _publishChannel.BasicPublish(
            exchange: ExchangeName,
            routingKey: queueName,
            mandatory: true, // 🔥 critical
            basicProperties: properties,
            body: body);

        _logger.LogInformation(
            "Published message → Exchange {Exchange}, RoutingKey {RoutingKey}",
            ExchangeName,
            queueName);
    }

    // -------------------- SUBSCRIBE --------------------

    public async Task SubscribeAsync<T>(
        string queueName,
        Func<T, Task> handler,
        CancellationToken cancellationToken = default)
        where T : class
    {
        if (_connection is null || !_connection.IsOpen)
            await ConnectAsync();

        if (_connection is null)
            throw new InvalidOperationException("Connection not initialized");

        var channel = _connection.CreateModel();
        _consumerChannels[queueName] = channel;

        EnsureTopology(channel, queueName);

        channel.BasicQos(0, 1, false);

        var consumer = new EventingBasicConsumer(channel);

        consumer.Received += (_, ea) =>
        {
            _ = Task.Run(async () =>
            {
                try
                {
                    var json = Encoding.UTF8.GetString(ea.Body.ToArray());
                    var message = JsonSerializer.Deserialize<T>(json);

                    if (message is null)
                        throw new InvalidOperationException("Deserialization returned null");

                    await handler(message);
                    channel.BasicAck(ea.DeliveryTag, false);

                    _logger.LogInformation(
                        "ACK message {DeliveryTag} from {Queue}",
                        ea.DeliveryTag,
                        queueName);
                }
                catch (Exception ex)
                {
                    _logger.LogError(
                        ex,
                        "Error processing message from {Queue}",
                        queueName);

                    channel.BasicNack(ea.DeliveryTag, false, true);
                }
            });
        };

        channel.BasicConsume(queueName, autoAck: false, consumer);

        _logger.LogInformation(
            "Subscribed → Queue {QueueName}",
            queueName);
    }

    // -------------------- CONNECTION --------------------

    public async Task ConnectAsync()
    {
        await _connectionLock.WaitAsync();

        try
        {
            if (_connection is { IsOpen: true })
                return;

            var factory = new ConnectionFactory
            {
                Uri = new Uri(_connectionString),
                AutomaticRecoveryEnabled = true,
                NetworkRecoveryInterval = TimeSpan.FromSeconds(10)
            };

            _connection = factory.CreateConnection();
            _publishChannel = _connection.CreateModel();

            // Detect unroutable messages
            _publishChannel.BasicReturn += (_, args) =>
            {
                _logger.LogError(
                    "Message RETURNED! Exchange:{Exchange} RoutingKey:{RoutingKey} Reply:{Reply}",
                    args.Exchange,
                    args.RoutingKey,
                    args.ReplyText);
            };

            _logger.LogInformation("RabbitMQ connected");
        }
        finally
        {
            _connectionLock.Release();
        }
    }

    public async Task DisconnectAsync()
    {
        await _connectionLock.WaitAsync();

        try
        {
            foreach (var ch in _consumerChannels.Values)
            {
                ch.Close();
                ch.Dispose();
            }

            _consumerChannels.Clear();

            _publishChannel?.Close();
            _publishChannel?.Dispose();

            _connection?.Close();
            _connection?.Dispose();

            _logger.LogInformation("Disconnected from RabbitMQ");
        }
        finally
        {
            _connectionLock.Release();
        }
    }

    // -------------------- TOPOLOGY --------------------

    private static void EnsureTopology(IModel channel, string queueName)
    {
        try
        {
            // Declare exchange - use ExchangeDeclarePassive to check if it exists first
            try
            {
                channel.ExchangeDeclarePassive(ExchangeName);
            }
            catch
            {
                // Exchange doesn't exist, create it
                channel.ExchangeDeclare(
                    exchange: ExchangeName,
                    type: ExchangeType.Direct,
                    durable: true,
                    autoDelete: false,
                    arguments: null);
            }

            // Declare queue
            channel.QueueDeclare(
                queue: queueName,
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: null);

            // Bind queue to exchange
            channel.QueueBind(
                queue: queueName,
                exchange: ExchangeName,
                routingKey: queueName);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                $"Failed to ensure topology for queue {queueName}. " +
                $"If exchange '{ExchangeName}' exists with different settings, please delete it from RabbitMQ Management UI first.",
                ex);
        }
    }

    // -------------------- QUEUE NAME --------------------

    private static string GetQueueName<T>()
        => GetQueueNameFromType(typeof(T));

    private static string GetQueueNameFromType(Type type)
    {
        var name = type.Name;
  
        // Remove "Command" or "Query" suffix
        if (name.EndsWith("Command"))
 name = name.Substring(0, name.Length - 7); // Remove "Command"
        else if (name.EndsWith("Query"))
            name = name.Substring(0, name.Length - 5); // Remove "Query"
    
      // Remove action prefix (Create, Update, Delete, Get, Search, etc.)
   if (name.StartsWith("Create"))
            name = name.Substring(6); // Remove "Create"
        else if (name.StartsWith("Update"))
            name = name.Substring(6); // Remove "Update"
        else if (name.StartsWith("Delete"))
          name = name.Substring(6); // Remove "Delete"
        else if (name.StartsWith("Get"))
 name = name.Substring(3); // Remove "Get"
     else if (name.StartsWith("Search"))
          name = name.Substring(6); // Remove "Search"
    
        // Convert to lowercase with dots: Medicine → medicine
        var sb = new StringBuilder();
  for (int i = 0; i < name.Length; i++)
     {
   if (i > 0 && char.IsUpper(name[i]))
       sb.Append('.');
  
            sb.Append(char.ToLowerInvariant(name[i]));
        }
        
        // Format: thanos.{entity}
      // Example: CreateMedicineCommand → thanos.medicine
        return $"{QueuePrefix}{sb}";
    }

    // -------------------- DISPOSE --------------------

    public async ValueTask DisposeAsync()
    {
        await _connectionLock.WaitAsync();

        try
        {
            foreach (var ch in _consumerChannels.Values)
            {
                ch.Close();
                ch.Dispose();
            }

            _consumerChannels.Clear();

            _publishChannel?.Close();
            _publishChannel?.Dispose();

            _connection?.Close();
            _connection?.Dispose();
        }
        finally
        {
            _connectionLock.Release();
            _connectionLock.Dispose();
        }
    }
}
