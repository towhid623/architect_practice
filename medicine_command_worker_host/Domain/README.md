# ??? Medicine Aggregate Root - DDD Implementation

## Overview

A complete **Domain-Driven Design (DDD)** implementation of the Medicine Aggregate Root with:
- ? Rich domain model with business logic
- ? Domain events for integration
- ? Value objects for type safety
- ? Invariant protection
- ? Encapsulation and immutability
- ? Repository pattern
- ? Domain services

## Architecture

```
Domain/
??? Common/
?   ??? AggregateRoot.cs     # Base class for aggregates
??? Events/
?   ??? DomainEvents.cs     # All domain events
??? ValueObjects/
?   ??? MedicineValueObjects.cs   # Value objects (Money, Strength, etc.)
??? Repositories/
?   ??? IMedicineRepository.cs    # Repository interface
??? Services/
?   ??? MedicineDomainService.cs  # Domain service
??? MedicineAggregateRoot.cs      # Medicine aggregate root
```

## Domain Model

### Aggregate Root: MedicineAggregateRoot

The Medicine aggregate root is the entry point for all medicine-related operations.

#### Key Principles

1. **Encapsulation**: All fields are private, exposed through read-only properties
2. **Invariant Protection**: Business rules enforced in methods
3. **Domain Events**: Operations raise events for integration
4. **Factory Method**: Use `Create()` instead of constructor
5. **Rich Behavior**: Business logic in domain methods, not anemic model

### Value Objects

#### Strength
```csharp
var strength = Strength.Parse("500mg");
Console.WriteLine(strength.DisplayValue); // "500mg"
```

Properties:
- `Value`: Numeric value (e.g., 500)
- `Unit`: Unit of measurement (e.g., "mg")
- `DisplayValue`: Formatted string

#### Money
```csharp
var price = new Money(19.99m, "USD");
var doubled = price.Multiply(2);
```

Properties:
- `Amount`: Decimal value
- `Currency`: ISO currency code

Methods:
- `Add(Money)`: Add two amounts
- `Subtract(Money)`: Subtract amount
- `Multiply(decimal)`: Multiply by factor

#### StockQuantity
```csharp
var stock = new StockQuantity(available: 100, lowStockThreshold: 20);
Console.WriteLine(stock.IsLowStock); // false
Console.WriteLine(stock.IsOutOfStock); // false
```

Properties:
- `Available`: Current stock
- `LowStockThreshold`: Minimum stock level
- `IsLowStock`: Is below threshold
- `IsOutOfStock`: No stock available

Methods:
- `Add(int)`: Add stock
- `Remove(int)`: Remove stock (validates sufficient stock)

#### Manufacturer
```csharp
var manufacturer = new Manufacturer("Pfizer", "USA");
Console.WriteLine(manufacturer); // "Pfizer (USA)"
```

#### StorageRequirements
```csharp
var storage = new StorageRequirements(
    "Store in cool, dry place",
    minTemperature: 15,
    maxTemperature: 25,
requiresRefrigeration: false);
```

## Domain Events

All operations raise domain events for:
- Integration with other bounded contexts
- Audit trail
- Event sourcing (if needed)
- Notifications

### Available Events

| Event | When Raised |
|-------|-------------|
| `MedicineCreatedEvent` | New medicine created |
| `MedicineUpdatedEvent` | Medicine details updated |
| `MedicineStockUpdatedEvent` | Stock added or removed |
| `MedicinePriceChangedEvent` | Price changed |
| `MedicineAvailabilityChangedEvent` | Made available/unavailable |
| `MedicineExpiredEvent` | Medicine expired |
| `MedicineDeletedEvent` | Medicine deleted/discontinued |

### Event Structure

```csharp
public record MedicineCreatedEvent(
    string MedicineId,
    string Name,
    string GenericName,
    string Manufacturer,
    decimal Price,
  int StockQuantity
) : DomainEvent;
```

All events inherit from `DomainEvent`:
```csharp
public abstract record DomainEvent
{
  public Guid EventId { get; init; }
    public DateTime OccurredOn { get; init; }
}
```

## Usage Examples

### Creating a Medicine

```csharp
var medicine = MedicineAggregateRoot.Create(
    name: "Aspirin",
    genericName: "Acetylsalicylic Acid",
    manufacturerName: "Bayer",
 description: "Pain reliever and fever reducer",
    dosageForm: "Tablet",
  strengthValue: "500mg",
    price: 9.99m,
    stockQuantity: 100,
    requiresPrescription: false,
    expiryDate: DateTime.UtcNow.AddYears(2),
    category: "Pain Relief",
    sideEffects: new List<string> { "Nausea", "Stomach upset" },
    storageInstructions: "Store at room temperature");

// Domain event raised: MedicineCreatedEvent
```

### Updating Medicine Details

```csharp
medicine.UpdateDetails(
    name: "Aspirin Plus",
    description: "Enhanced pain relief formula",
  category: "Pain Management");

// Domain event raised: MedicineUpdatedEvent
```

### Changing Price

```csharp
medicine.ChangePrice(12.99m, "Market price adjustment");

// Domain event raised: MedicinePriceChangedEvent
```

### Managing Stock

```csharp
// Add stock
medicine.AddStock(50, "Restock from supplier");
// Event: MedicineStockUpdatedEvent

// Remove stock (for sales)
medicine.RemoveStock(10, "Sale order #12345");
// Event: MedicineStockUpdatedEvent
// If out of stock: MedicineAvailabilityChangedEvent
```

### Availability Management

```csharp
// Make available
medicine.MakeAvailable("Back in stock");
// Event: MedicineAvailabilityChangedEvent

// Make unavailable
medicine.MakeUnavailable("Recalled");
// Event: MedicineAvailabilityChangedEvent
```

### Expiry Management

```csharp
// Check expiry
medicine.CheckExpiry();
// If expired: MedicineExpiredEvent, MedicineAvailabilityChangedEvent

// Manual expiry
medicine.MarkAsExpired();
// Event: MedicineExpiredEvent
```

### Validation

```csharp
// Validate if can be sold
try
{
    medicine.ValidateCanSell();
    // Proceed with sale
}
catch (InvalidOperationException ex)
{
    // Handle: "Cannot sell medicine: Medicine is not available, Medicine expired"
}
```

### Domain Properties

```csharp
// Computed properties
bool canSell = medicine.CanBeSold; // Available && !Expired && !OutOfStock
bool isExpired = medicine.IsExpired;
bool isLowStock = medicine.IsLowStock;
bool isOutOfStock = medicine.IsOutOfStock;
```

## Domain Service

The `MedicineDomainService` handles complex operations involving:
- Multiple aggregates
- External dependencies
- Repository interactions

### Creating Medicine (with uniqueness check)

```csharp
var domainService = new MedicineDomainService(repository);

var medicine = await domainService.CreateMedicineAsync(
    name: "Aspirin",
    genericName: "Acetylsalicylic Acid",
    // ... other parameters
    cancellationToken);
```

### Processing Sales

```csharp
var medicine = await domainService.ProcessSaleAsync(
    medicineId: "123",
    quantity: 5,
    cancellationToken);
```

Validates:
- Medicine exists
- Is available
- Not expired
- Has sufficient stock

### Restocking

```csharp
var medicine = await domainService.RestockAsync(
    medicineId: "123",
 quantity: 100,
    cancellationToken);
```

### Price Changes

```csharp
var medicine = await domainService.ChangePriceAsync(
    medicineId: "123",
    newPrice: 15.99m,
    reason: "Supplier price increase",
    cancellationToken);
```

### Getting Alerts

```csharp
var alerts = await domainService.GetMedicineAlertsAsync(cancellationToken);

Console.WriteLine($"Low Stock: {alerts.LowStockMedicines.Count}");
Console.WriteLine($"Out of Stock: {alerts.OutOfStockMedicines.Count}");
Console.WriteLine($"Expired: {alerts.ExpiredMedicines.Count}");
Console.WriteLine($"Expiring Soon: {alerts.ExpiringSoonMedicines.Count}");
```

### Checking Expired Medicines

```csharp
// Run periodically (e.g., daily job)
await domainService.CheckAndMarkExpiredMedicinesAsync(cancellationToken);
```

## Repository Interface

```csharp
public interface IMedicineRepository
{
    Task<MedicineAggregateRoot?> GetByIdAsync(string id, ...);
  Task<MedicineAggregateRoot?> GetByNameAsync(string name, ...);
    Task<IEnumerable<MedicineAggregateRoot>> GetAllAsync(...);
    Task<IEnumerable<MedicineAggregateRoot>> GetByCategoryAsync(string category, ...);
    Task<IEnumerable<MedicineAggregateRoot>> GetLowStockMedicinesAsync(...);
    Task<IEnumerable<MedicineAggregateRoot>> GetExpiredMedicinesAsync(...);
    
    Task AddAsync(MedicineAggregateRoot medicine, ...);
    Task UpdateAsync(MedicineAggregateRoot medicine, ...);
    Task DeleteAsync(string id, ...);
    Task<bool> ExistsAsync(string name, ...);
    Task<int> SaveChangesAsync(...);
}
```

## Business Rules Enforced

### Invariants

1. ? **Name Required**: Medicine must have a name
2. ? **Generic Name Required**: Generic name is mandatory
3. ? **Non-Negative Price**: Price cannot be negative
4. ? **Non-Negative Stock**: Stock cannot be negative
5. ? **No Pre-Expired Creation**: Cannot create already expired medicine
6. ? **Expired Medicine Cannot Be Available**: Automatically unavailable when expired
7. ? **Out of Stock Auto-Unavailable**: Automatically unavailable when stock = 0
8. ? **Sufficient Stock for Sales**: Cannot sell more than available
9. ? **Unique Name**: No duplicate medicine names

### Business Logic

1. **Auto-Availability**:
   - Out of stock ? Auto unavailable
   - Restocked ? Auto available (if was unavailable due to stock)

2. **Expiry Management**:
   - Expired medicines automatically marked unavailable
   - Periodic check for expiry dates

3. **Stock Management**:
   - Low stock threshold tracking
   - Out of stock detection
   - Validation before sales

4. **Price Management**:
   - Price change tracking with reason
   - Historical price changes (via events)

## Integration with Application Layer

### In Command Handlers

```csharp
public class CreateMedicineCommandHandler
{
    private readonly MedicineDomainService _domainService;

    public async Task<Result<MedicineResponse>> HandleAsync(
        CreateMedicineCommand command, 
   CancellationToken cancellationToken)
    {
  var medicine = await _domainService.CreateMedicineAsync(
  command.Name,
            command.GenericName,
         // ... map all properties
            cancellationToken);

        // Domain events are automatically raised
        // Convert to response DTO
return Result.Success(MapToResponse(medicine));
    }
}
```

### Publishing Domain Events

```csharp
// After saving
var events = medicine.DomainEvents;
foreach (var domainEvent in events)
{
await eventBus.PublishAsync(domainEvent);
}
medicine.ClearDomainEvents();
```

## Benefits of This Approach

### ? Rich Domain Model
- Business logic in domain, not in services
- Self-contained and testable
- Clear business intent

### ? Encapsulation
- Internal state hidden
- Controlled access through methods
- Invariants always maintained

### ? Type Safety
- Value objects prevent primitive obsession
- Compile-time validation
- Clear semantics

### ? Auditability
- Domain events track all changes
- Complete audit trail
- Integration points clear

### ? Testability
- Pure domain logic
- No infrastructure dependencies
- Easy to unit test

### ? Maintainability
- Business rules in one place
- Clear boundaries
- Easy to extend

## Testing

### Unit Testing Aggregate

```csharp
[Fact]
public void Create_ValidMedicine_RaisesCreatedEvent()
{
    // Arrange & Act
    var medicine = MedicineAggregateRoot.Create(
        "Aspirin", "Acetylsalicylic Acid", "Bayer",
        "Pain reliever", "Tablet", "500mg",
        9.99m, 100, false, null, "Pain Relief",
        new List<string>(), "Store cool");

    // Assert
    Assert.NotNull(medicine);
    Assert.Equal("Aspirin", medicine.Name);
    Assert.Single(medicine.DomainEvents);
  Assert.IsType<MedicineCreatedEvent>(medicine.DomainEvents.First());
}

[Fact]
public void RemoveStock_InsufficientStock_ThrowsException()
{
    // Arrange
    var medicine = MedicineAggregateRoot.Create(/* with stock = 10 */);

    // Act & Assert
  Assert.Throws<InvalidOperationException>(() =>
   medicine.RemoveStock(20, "Sale"));
}
```

## Summary

? **Complete DDD implementation** with aggregate root, value objects, and domain events  
? **Rich domain model** with business logic  
? **Invariant protection** - impossible to create invalid state  
? **Type-safe** value objects  
? **Event-driven** integration  
? **Testable** domain logic  
? **Production-ready** with all business rules enforced  

**This is a textbook example of Domain-Driven Design!** ???
