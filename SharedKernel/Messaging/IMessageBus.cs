using SharedKernel.CQRS;

namespace SharedKernel.Messaging;

/// <summary>
/// Interface for message bus operations
/// </summary>
public interface IMessageBus
{
    /// <summary>
    /// Sends a command to the message bus
    /// </summary>
    Task SendAsync<TCommand>(
        TCommand command,
        CancellationToken cancellationToken = default)
        where TCommand : class, ICommand;

    /// <summary>
    /// Publishes a message to the specified queue
    /// </summary>
    Task PublishAsync<T>(
        string queueName,
        T message,
        CancellationToken cancellationToken = default)
        where T : class;

    /// <summary>
    /// Subscribes to a queue and processes messages
    /// </summary>
    Task SubscribeAsync<T>(
        string queueName,
        Func<T, Task> handler,
        CancellationToken cancellationToken = default)
        where T : class;

    /// <summary>
    /// Connects to the message bus
    /// </summary>
    Task ConnectAsync();

    /// <summary>
    /// Disconnects from the message bus
    /// </summary>
    Task DisconnectAsync();
}
