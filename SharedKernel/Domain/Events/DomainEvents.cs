namespace SharedKernel.Domain.Events;

/// <summary>
/// Base class for all domain events
/// </summary>
public abstract record DomainEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTime OccurredOn { get; init; } = DateTime.UtcNow;
}

public record MedicineCreatedEvent(
    string MedicineId,
    string Name,
    string GenericName,
    string Manufacturer,
    decimal Price,
    int StockQuantity
) : DomainEvent;

public record MedicineStockUpdatedEvent(
    string MedicineId,
    int OldStock,
    int NewStock,
    string Reason
) : DomainEvent;

public record MedicinePriceChangedEvent(
 string MedicineId,
    decimal OldPrice,
    decimal NewPrice,
    string Reason
) : DomainEvent;

public record MedicineAvailabilityChangedEvent(
    string MedicineId,
    bool IsAvailable,
    string Reason
) : DomainEvent;

public record MedicineUpdatedEvent(
    string MedicineId,
    string Name,
    Dictionary<string, object> ChangedProperties
) : DomainEvent;
