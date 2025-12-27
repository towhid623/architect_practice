using SharedKernel.Domain.Events;

namespace SharedKernel.Messaging;

/// <summary>
/// Interface for publishing domain events
/// </summary>
public interface IDomainEventPublisher
{
    /// <summary>
    /// Publishes a domain event to the event bus (fanout exchange)
  /// </summary>
    Task PublishAsync<TEvent>(TEvent domainEvent, CancellationToken cancellationToken = default)
   where TEvent : DomainEvent;

    /// <summary>
    /// Publishes multiple domain events
    /// </summary>
    Task PublishManyAsync(IEnumerable<DomainEvent> domainEvents, CancellationToken cancellationToken = default);
}
