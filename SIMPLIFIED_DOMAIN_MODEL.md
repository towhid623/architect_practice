# ?? Simplified Medicine Domain Model

## Overview
Simplified DDD implementation focusing on essential features only.

## Value Objects (2-3 properties each)

### Strength
```csharp
public record Strength
{
    public string Value { get; init; }  // e.g., "500mg"
}
```

**Usage:**
```csharp
var strength = new Strength("500mg");
Console.WriteLine(strength); // "500mg"
```

### Money
```csharp
public record Money
{
  public decimal Amount { get; init; }  // Dollar amount
}
```

**Usage:**
```csharp
var price = new Money(19.99m);
Console.WriteLine(price); // "$19.99"
```

### StockQuantity
```csharp
public record StockQuantity
{
    public int Available { get; init; }
    public bool IsLowStock { get; }      // <= 10 units
    public bool IsOutOfStock { get; }    // <= 0 units
}
```

**Usage:**
```csharp
var stock = new StockQuantity(100);
Console.WriteLine(stock.IsLowStock);  // false
Console.WriteLine(stock.IsOutOfStock); // false

var reduced = stock.Remove(95);
Console.WriteLine(reduced.IsLowStock);  // true (5 units)
```

## Medicine Aggregate Root

### Essential Properties
```csharp
- Name (string)
- GenericName (string)
- Manufacturer (string)
- Strength (Strength value object)
- Price (Money value object)
- Stock (StockQuantity value object)
- IsAvailable (bool)
```

### Computed Properties
```csharp
- IsLowStock
- IsOutOfStock
- CanBeSold (IsAvailable && !IsOutOfStock)
```

## Usage Examples

### Create Medicine
```csharp
var medicine = MedicineAggregateRoot.Create(
    name: "Aspirin",
    genericName: "Acetylsalicylic Acid",
    manufacturer: "Bayer",
    strength: "500mg",
    price: 9.99m,
    stockQuantity: 100);

// Event raised: MedicineCreatedEvent
```

### Update Details
```csharp
medicine.UpdateDetails(
    name: "Aspirin Plus",
    genericName: null,  // Keep existing
    manufacturer: "Bayer AG");

// Event raised: MedicineUpdatedEvent
```

### Manage Price
```csharp
medicine.ChangePrice(12.99m);
// Event raised: MedicinePriceChangedEvent
```

### Manage Stock
```csharp
// Add stock
medicine.AddStock(50);
// Event: MedicineStockUpdatedEvent

// Remove stock
medicine.RemoveStock(10);
// Event: MedicineStockUpdatedEvent
```

### Availability
```csharp
medicine.MakeUnavailable();
// Event: MedicineAvailabilityChangedEvent

medicine.MakeAvailable();
// Event: MedicineAvailabilityChangedEvent
```

## Domain Service

### Create with Validation
```csharp
var service = new MedicineDomainService(repository);

var medicine = await service.CreateMedicineAsync(
    "Aspirin",
    "Acetylsalicylic Acid",
    "Bayer",
    "500mg",
  9.99m,
    100);
```

### Update
```csharp
var updated = await service.UpdateMedicineAsync(
    medicineId,
name: "New Name",
    genericName: null,
    manufacturer: null);
```

### Change Price
```csharp
var medicine = await service.ChangePriceAsync(medicineId, 15.99m);
```

### Stock Operations
```csharp
// Add stock
await service.AddStockAsync(medicineId, 50);

// Remove stock (for sales)
await service.RemoveStockAsync(medicineId, 10);
```

## Domain Events

All operations raise events:
- `MedicineCreatedEvent`
- `MedicineUpdatedEvent`
- `MedicineStockUpdatedEvent`
- `MedicinePriceChangedEvent`
- `MedicineAvailabilityChangedEvent`

## Business Rules

### ? Enforced Invariants
1. Name and GenericName are required
2. Price cannot be negative
3. Stock cannot be negative
4. Cannot remove more stock than available
5. Cannot sell if unavailable or out of stock

### ? Auto-Behaviors
- Low stock detection (threshold: 10 units)
- Out of stock detection
- Can be sold validation

## Simplified Structure

```
Domain/
??? Common/
?   ??? AggregateRoot.cs
??? Events/
?   ??? DomainEvents.cs (5 events)
??? ValueObjects/
?   ??? MedicineValueObjects.cs (3 value objects)
??? Repositories/
?   ??? IMedicineRepository.cs
??? Services/
?   ??? MedicineDomainService.cs
??? MedicineAggregateRoot.cs
```

## Key Changes from Full Version

### Removed Features
- ? Expiry date management
- ? Storage requirements
- ? Side effects tracking
- ? Category management
- ? Dosage form details
- ? Prescription requirements
- ? Complex manufacturer object
- ? Temperature storage
- ? Alert system

### Kept Features
- ? Essential properties (name, manufacturer, price, stock)
- ? Value objects (3 simple ones)
- ? Domain events (5 core events)
- ? Stock management
- ? Price management
- ? Availability management
- ? Business rule validation

## Benefits

### ? Simplicity
- Easy to understand
- Quick to implement
- Minimal complexity

### ? Maintainability
- Less code to maintain
- Clear responsibilities
- Easy to test

### ? Still DDD-Compliant
- Aggregate root pattern
- Value objects
- Domain events
- Encapsulation
- Business rules

## Summary

? **Simplified to 2-3 properties per value object**  
? **Essential business logic only**  
? **Still follows DDD principles**  
? **Production-ready for basic scenarios**  
? **Easy to extend later if needed**  

**Perfect balance between simplicity and proper design!** ??
