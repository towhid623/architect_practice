using SharedKernel.Domain.Events;

namespace SharedKernel.Domain.Common;

/// <summary>
/// Base class for all aggregate roots following DDD principles
/// </summary>
public abstract class AggregateRoot
{
    private readonly List<DomainEvent> _domainEvents = new();

    public string Id { get; protected set; } = string.Empty;
    public DateTime CreatedAt { get; protected set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; protected set; }
    public int Version { get; protected set; }

    public IReadOnlyCollection<DomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    protected void AddDomainEvent(DomainEvent domainEvent)
 {
        _domainEvents.Add(domainEvent);
    }

    public void ClearDomainEvents()
    {
        _domainEvents.Clear();
 }

    protected void MarkAsUpdated()
    {
        UpdatedAt = DateTime.UtcNow;
        Version++;
    }
}
