# ? Fixed: MongoDB Expression Translation Error

## The Problem

```
MongoDB.Driver.Linq.ExpressionNotSupportedException: Expression not supported: m.Name.
```

### Root Cause
MongoDB couldn't translate the LINQ expression `m => m.Name` because:
1. `Name` is a **read-only property** with a **private backing field** (`_name`)
2. MongoDB needs to know the **actual field name** stored in the database
3. Without proper serialization attributes, MongoDB can't map properties to fields

## The Fix

### 1. Added MongoDB Serialization Attributes

**Before (Caused Error):**
```csharp
public class MedicineAggregateRoot : AggregateRoot
{
    private string _name = string.Empty;
  
    public string Name => _name;  // ? MongoDB can't map this
}
```

**After (Works):**
```csharp
public class MedicineAggregateRoot : AggregateRoot
{
    [BsonElement("name")]  // ? Maps to "name" field in MongoDB
    private string _name = string.Empty;
    
    [BsonIgnore]  // ? Tells MongoDB to ignore this property
    public string Name => _name;
}
```

### 2. Changed Repository Filters

**Before (Used Expressions - Error):**
```csharp
// ? MongoDB can't translate m.Name expression
var filter = Builders<MedicineAggregateRoot>.Filter.Eq(m => m.Name, name);
```

**After (Used Field Names - Works):**
```csharp
// ? Uses actual field name in MongoDB
var filter = Builders<MedicineAggregateRoot>.Filter.Eq("name", name);
```

### 3. Simplified Value Objects Storage

**Before (Complex):**
```csharp
private Strength? _strength;
private Money? _price;
private StockQuantity? _stock;
```

**After (Simple Types):**
```csharp
[BsonElement("strength")]
private string? _strengthValue;

[BsonElement("price")]
private decimal _priceAmount;

[BsonElement("stock_quantity")]
private int _stockAvailable;

// Value objects created on-demand
[BsonIgnore]
public Strength? Strength => _strengthValue != null ? new Strength(_strengthValue) : null;

[BsonIgnore]
public Money? Price => new Money(_priceAmount);

[BsonIgnore]
public StockQuantity? Stock => new StockQuantity(_stockAvailable);
```

## Complete Changes

### MedicineAggregateRoot.cs

```csharp
public class MedicineAggregateRoot : AggregateRoot
{
    // ? MongoDB-serializable fields with attributes
    [BsonElement("name")]
    private string _name = string.Empty;
 
    [BsonElement("generic_name")]
    private string _genericName = string.Empty;
    
    [BsonElement("manufacturer")]
    private string _manufacturer = string.Empty;
    
    [BsonElement("strength")]
    private string? _strengthValue;
    
    [BsonElement("price")]
    private decimal _priceAmount;
    
    [BsonElement("stock_quantity")]
    private int _stockAvailable;
    
    [BsonElement("is_available")]
  private bool _isAvailable = true;

    // ? Ignored properties - not stored in MongoDB
    [BsonIgnore]
    public string Name => _name;
    
    [BsonIgnore]
    public Strength? Strength => _strengthValue != null ? new Strength(_strengthValue) : null;
    
    [BsonIgnore]
    public Money? Price => new Money(_priceAmount);
    
    [BsonIgnore]
    public StockQuantity? Stock => new StockQuantity(_stockAvailable);
}
```

### MedicineRepository.cs

```csharp
public async Task<bool> ExistsAsync(string name, CancellationToken cancellationToken = default)
{
    try
    {
        // ? Use field name directly
 var filter = Builders<MedicineAggregateRoot>.Filter.Eq("name", name);
 var count = await _collection.CountDocumentsAsync(filter, cancellationToken: cancellationToken);
        return count > 0;
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error checking if medicine exists: {Name}", name);
        throw;
    }
}

public async Task<MedicineAggregateRoot?> GetByNameAsync(string name, CancellationToken cancellationToken = default)
{
    try
    {
   // ? Use field name
        var filter = Builders<MedicineAggregateRoot>.Filter.Eq("name", name);
   return await _collection.Find(filter).FirstOrDefaultAsync(cancellationToken);
 }
    catch (Exception ex)
    {
    _logger.LogError(ex, "Error getting medicine by name: {Name}", name);
 throw;
 }
}

public async Task<IEnumerable<MedicineAggregateRoot>> GetLowStockMedicinesAsync(CancellationToken cancellationToken = default)
{
    try
    {
     // ? Use field name
        var filter = Builders<MedicineAggregateRoot>.Filter.Lte("stock_quantity", 10);
        return await _collection.Find(filter).ToListAsync(cancellationToken);
    }
    catch (Exception ex)
 {
 _logger.LogError(ex, "Error getting low stock medicines");
        throw;
    }
}
```

## MongoDB Document Structure

```json
{
  "_id": "507f1f77bcf86cd799439011",
  "name": "Aspirin",
  "generic_name": "Acetylsalicylic Acid",
  "manufacturer": "Bayer",
  "strength": "500mg",
  "price": 9.99,
  "stock_quantity": 100,
  "is_available": true,
  "created_at": "2024-01-15T10:30:00Z",
  "updated_at": null,
  "version": 0
}
```

## Benefits of This Approach

### ? Clean Separation
- **Storage**: Simple types (string, decimal, int)
- **Domain Logic**: Rich value objects (Strength, Money, StockQuantity)
- **Persistence**: MongoDB serialization attributes

### ? Performance
- No complex object serialization
- Simple field lookups
- Efficient queries

### ? DDD Preserved
- Domain logic still uses value objects
- Encapsulation maintained
- Business rules enforced

### ? MongoDB Friendly
- Simple document structure
- Easy to query
- No expression translation issues

## How It Works

### Writing (Save)
```csharp
var medicine = MedicineAggregateRoot.CreateFromCommand(command);
// Creates: _name = "Aspirin", _priceAmount = 9.99, _stockAvailable = 100

await repository.AddAsync(medicine);
// MongoDB stores: { "name": "Aspirin", "price": 9.99, "stock_quantity": 100 }
```

### Reading (Load)
```csharp
var medicine = await repository.GetByNameAsync("Aspirin");
// MongoDB loads: { "name": "Aspirin", "price": 9.99, "stock_quantity": 100 }
// Populates: _name = "Aspirin", _priceAmount = 9.99, _stockAvailable = 100

var price = medicine.Price;  // Creates Money value object: new Money(9.99)
var stock = medicine.Stock;   // Creates StockQuantity: new StockQuantity(100)
```

### Querying
```csharp
// ? Works - uses field name
var filter = Builders<MedicineAggregateRoot>.Filter.Eq("name", "Aspirin");

// ? Fails - can't translate expression
var filter = Builders<MedicineAggregateRoot>.Filter.Eq(m => m.Name, "Aspirin");
```

## Testing

### Verify Fix
1. Start worker: `dotnet run --project medicine_command_worker_host`
2. Start API: `dotnet run --project test_service`
3. Send command:
```bash
POST http://localhost:5035/api/medicine
Content-Type: application/json

{
  "name": "Aspirin",
  "genericName": "Acetylsalicylic Acid",
  "manufacturer": "Bayer",
  "strength": "500mg",
  "price": 9.99,
  "stockQuantity": 100
}
```

### Expected Result
```
? Medicine aggregate created. ID: 507f..., Events: 1
? Medicine saved: 507f...
?? Event: MedicineCreatedEvent - guid-123
? Command completed: 507f...
```

## Summary

? **Added [BsonElement]** attributes to map private fields  
? **Added [BsonIgnore]** to ignore computed properties  
? **Changed filters** to use field names instead of expressions  
? **Simplified storage** to use primitive types  
? **Value objects** created on-demand for domain logic  
? **MongoDB queries** now work correctly  

**The aggregate root is now MongoDB-friendly while preserving DDD principles!** ??
