namespace medicine_command_worker_host.Domain.Events;

/// <summary>
/// Base class for all domain events
/// </summary>
public abstract record DomainEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTime OccurredOn { get; init; } = DateTime.UtcNow;
}

/// <summary>
/// Event raised when a new medicine is created
/// </summary>
public record MedicineCreatedEvent(
    string MedicineId,
    string Name,
    string GenericName,
    string Manufacturer,
    decimal Price,
    int StockQuantity
) : DomainEvent;

/// <summary>
/// Event raised when medicine stock is updated
/// </summary>
public record MedicineStockUpdatedEvent(
    string MedicineId,
    int OldStock,
    int NewStock,
    string Reason
) : DomainEvent;

/// <summary>
/// Event raised when medicine price is changed
/// </summary>
public record MedicinePriceChangedEvent(
    string MedicineId,
    decimal OldPrice,
    decimal NewPrice,
    string Reason
) : DomainEvent;

/// <summary>
/// Event raised when medicine availability changes
/// </summary>
public record MedicineAvailabilityChangedEvent(
    string MedicineId,
bool IsAvailable,
    string Reason
) : DomainEvent;

/// <summary>
/// Event raised when medicine is marked as expired
/// </summary>
public record MedicineExpiredEvent(
    string MedicineId,
    string Name,
    DateTime ExpiryDate
) : DomainEvent;

/// <summary>
/// Event raised when medicine details are updated
/// </summary>
public record MedicineUpdatedEvent(
    string MedicineId,
    string Name,
    Dictionary<string, object> ChangedProperties
) : DomainEvent;

/// <summary>
/// Event raised when medicine is deleted/discontinued
/// </summary>
public record MedicineDeletedEvent(
    string MedicineId,
    string Name,
    string Reason
) : DomainEvent;
