using medicine_command_worker_host.Domain.Events;

namespace medicine_command_worker_host.Domain.Common;

/// <summary>
/// Base class for all aggregate roots following DDD principles
/// </summary>
public abstract class AggregateRoot
{
    private readonly List<DomainEvent> _domainEvents = new();

    /// <summary>
    /// Unique identifier for the aggregate
    /// </summary>
    public string Id { get; protected set; } = string.Empty;

    /// <summary>
    /// When the aggregate was created
    /// </summary>
    public DateTime CreatedAt { get; protected set; } = DateTime.UtcNow;

    /// <summary>
    /// When the aggregate was last updated
    /// </summary>
    public DateTime? UpdatedAt { get; protected set; }

    /// <summary>
  /// Version for optimistic concurrency control
    /// </summary>
    public int Version { get; protected set; }

    /// <summary>
    /// Domain events that occurred during this operation
    /// </summary>
    public IReadOnlyCollection<DomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    /// <summary>
    /// Adds a domain event to be published
    /// </summary>
    protected void AddDomainEvent(DomainEvent domainEvent)
  {
        _domainEvents.Add(domainEvent);
    }

    /// <summary>
    /// Clears all domain events (after they've been published)
    /// </summary>
    public void ClearDomainEvents()
    {
        _domainEvents.Clear();
    }

    /// <summary>
    /// Marks the aggregate as updated
  /// </summary>
    protected void MarkAsUpdated()
  {
 UpdatedAt = DateTime.UtcNow;
  Version++;
    }
}

/// <summary>
/// Base class for entities within an aggregate
/// </summary>
public abstract class Entity
{
    public string Id { get; protected set; } = string.Empty;
    public DateTime CreatedAt { get; protected set; } = DateTime.UtcNow;

    protected Entity(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
         throw new ArgumentException("Entity ID cannot be empty", nameof(id));

        Id = id;
    }

    protected Entity()
    {
        // For ORM
    }

    public override bool Equals(object? obj)
    {
 if (obj is not Entity other)
   return false;

        if (ReferenceEquals(this, other))
         return true;

        if (GetType() != other.GetType())
            return false;

   return Id == other.Id;
    }

    public override int GetHashCode()
    {
        return Id.GetHashCode();
    }

    public static bool operator ==(Entity? a, Entity? b)
    {
  if (a is null && b is null)
  return true;

   if (a is null || b is null)
        return false;

  return a.Equals(b);
 }

    public static bool operator !=(Entity? a, Entity? b)
    {
      return !(a == b);
    }
}
