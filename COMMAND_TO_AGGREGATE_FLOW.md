# ? Command Passed Directly to Aggregate Root

## Flow: Command ? Handler ? Aggregate Root

### Architecture
```
CreateMedicineCommand
    ?
CreateMedicineCommandHandler
    ?
MedicineAggregateRoot.CreateFromCommand(command)
    ?
Domain Events Raised
    ?
Save to Repository
    ?
Return Response
```

## Implementation

### 1. Aggregate Root Factory Method
```csharp
// MedicineAggregateRoot.cs
public static MedicineAggregateRoot CreateFromCommand(CreateMedicineCommand command)
{
  if (command == null)
 throw new ArgumentNullException(nameof(command));

    // Validate
    if (string.IsNullOrWhiteSpace(command.Name))
        throw new ArgumentException("Name is required");

    if (string.IsNullOrWhiteSpace(command.GenericName))
        throw new ArgumentException("Generic name is required");

    // Create aggregate from command properties
    var medicine = new MedicineAggregateRoot
    {
        Id = ObjectId.GenerateNewId().ToString(),
        _name = command.Name.Trim(),
        _genericName = command.GenericName.Trim(),
        _manufacturer = command.Manufacturer?.Trim() ?? string.Empty,
        _strength = new Strength(command.Strength),
        _price = new Money(command.Price),
     _stock = new StockQuantity(command.StockQuantity),
     _isAvailable = true
    };

    // Raise domain event
    medicine.AddDomainEvent(new MedicineCreatedEvent(...));

    return medicine;
}
```

### 2. Handler Usage
```csharp
// CreateMedicineCommandHandler.cs
public async Task<Result<MedicineResponse>> HandleAsync(
    CreateMedicineCommand command,
    CancellationToken cancellationToken = default)
{
    try
    {
        // Check uniqueness
        if (await _repository.ExistsAsync(command.Name, cancellationToken))
        {
      return Result.Failure<MedicineResponse>(
     $"Medicine '{command.Name}' already exists");
      }

        // ? Pass command directly to aggregate root
 var medicineAggregate = MedicineAggregateRoot.CreateFromCommand(command);

  // Save to repository
        await _repository.AddAsync(medicineAggregate, cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);

      // Log domain events
foreach (var domainEvent in medicineAggregate.DomainEvents)
        {
      _logger.LogInformation("Domain event: {EventType}", 
      domainEvent.GetType().Name);
      // TODO: Publish to event bus
   }

        // Clear events
     medicineAggregate.ClearDomainEvents();

        // Map to response
      var response = MapToResponse(medicineAggregate);

        return Result.Success(response);
    }
  catch (ArgumentException ex)
    {
        return Result.Failure<MedicineResponse>(ex.Message);
    }
}
```

## Benefits

### ? Clean Separation
```
Command (Application Layer)
    ?
Aggregate Root (Domain Layer)
    ?
Repository (Infrastructure Layer)
```

### ? Command as Input
- Command contains all data needed
- No manual property mapping in handler
- Aggregate validates and creates itself
- Single responsibility for each layer

### ? Domain Events
- Automatically raised during aggregate creation
- Handler logs and can publish them
- Events cleared after processing
- Audit trail maintained

### ? Type Safety
- Command properties validated in aggregate
- Value objects enforce business rules
- Compile-time safety

## Complete Flow

```
1. API receives POST /api/medicine
    ?
2. Command published to RabbitMQ queue: thanos.medicine
    ?
3. Worker picks up command
    ?
4. CreateMedicineCommandHandler receives command
    ?
5. Handler checks if medicine exists (uniqueness)
    ?
6. MedicineAggregateRoot.CreateFromCommand(command)
   - Validates command properties
   - Creates value objects (Money, Strength, StockQuantity)
   - Raises MedicineCreatedEvent
    ?
7. Handler saves aggregate to repository
    ?
8. Domain events logged (TODO: publish to event bus)
    ?
9. Response mapped from aggregate
    ?
10. Success result returned
```

## Example Execution

### Input Command
```json
{
  "name": "Aspirin",
  "genericName": "Acetylsalicylic Acid",
  "manufacturer": "Bayer",
  "strength": "500mg",
  "price": 9.99,
  "stockQuantity": 100
}
```

### Handler Processing
```
?? Received CreateMedicineCommand: Aspirin
? Medicine aggregate created. ID: 65abc..., Name: Aspirin, Events: 1
?? Medicine saved to repository: 65abc...
?? Domain event: MedicineCreatedEvent - guid-123
? CreateMedicineCommand completed successfully: 65abc...
```

### Domain Event Raised
```csharp
new MedicineCreatedEvent(
    MedicineId: "65abc...",
    Name: "Aspirin",
    GenericName: "Acetylsalicylic Acid",
    Manufacturer: "Bayer",
    Price: 9.99m,
    StockQuantity: 100
)
```

### Response
```json
{
  "id": "65abc...",
  "name": "Aspirin",
  "genericName": "Acetylsalicylic Acid",
  "manufacturer": "Bayer",
  "strength": "500mg",
  "price": 9.99,
  "stockQuantity": 100,
  "isAvailable": true,
  "createdAt": "2024-01-15T10:30:00Z"
}
```

## Handler Responsibilities

### ? What Handler Does
1. Receives command
2. Checks business rules that require repository (uniqueness)
3. Passes command to aggregate root
4. Saves aggregate to repository
5. Processes domain events
6. Maps aggregate to response DTO
7. Returns result

### ? What Handler Doesn't Do
- ? Create value objects directly
- ? Validate business rules (aggregate does this)
- ? Manually map command properties
- ? Raise domain events (aggregate does this)
- ? Business logic (in aggregate)

## Aggregate Root Responsibilities

### ? What Aggregate Does
1. Receives command
2. Validates all business rules
3. Creates value objects
4. Sets internal state
5. Raises domain events
6. Maintains invariants

### ? What Aggregate Doesn't Do
- ? Access repository
- ? Check uniqueness constraints
- ? Publish events to event bus
- ? Map to DTOs
- ? Infrastructure concerns

## Comparison

### Before (Manual Mapping)
```csharp
// Handler
var aggregate = MedicineAggregateRoot.Create(
  command.Name,
    command.GenericName,
    command.Manufacturer,
command.Strength,
    command.Price,
    command.StockQuantity
);
```
? 6 parameters to pass manually

### After (Command Direct)
```csharp
// Handler
var aggregate = MedicineAggregateRoot.CreateFromCommand(command);
```
? Single parameter, clean and simple

## Testing

### Unit Test Handler
```csharp
[Fact]
public async Task HandleAsync_ValidCommand_CreatesAggregate()
{
    // Arrange
var command = new CreateMedicineCommand
    {
        Name = "Aspirin",
        GenericName = "Acetylsalicylic Acid",
        Manufacturer = "Bayer",
        Strength = "500mg",
        Price = 9.99m,
 StockQuantity = 100
    };

    // Act
    var result = await _handler.HandleAsync(command);

  // Assert
    Assert.True(result.IsSuccess);
    Assert.Equal("Aspirin", result.Value.Name);
    _repositoryMock.Verify(r => r.AddAsync(
        It.IsAny<MedicineAggregateRoot>(), 
        It.IsAny<CancellationToken>()), 
        Times.Once);
}
```

### Unit Test Aggregate
```csharp
[Fact]
public void CreateFromCommand_ValidCommand_CreatesAggregate()
{
    // Arrange
    var command = new CreateMedicineCommand { /* ... */ };

    // Act
    var aggregate = MedicineAggregateRoot.CreateFromCommand(command);

    // Assert
    Assert.NotNull(aggregate);
    Assert.Equal("Aspirin", aggregate.Name);
    Assert.Single(aggregate.DomainEvents);
    Assert.IsType<MedicineCreatedEvent>(aggregate.DomainEvents.First());
}
```

## Summary

? **Command passed directly** to aggregate root
? **Clean separation** of concerns  
? **Handler simplified** - just orchestration
? **Aggregate owns** business logic  
? **Domain events** automatically raised  
? **Type-safe** with value objects  
? **Easy to test** each layer independently  
? **DDD principles** properly applied  

**This is the proper way to handle commands in DDD!** ??
