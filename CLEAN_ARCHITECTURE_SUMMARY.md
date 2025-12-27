# ? Clean Architecture: Repository Pattern Implementation

## Structure Overview

### SharedKernel (Domain & Interfaces)
```
SharedKernel/
??? Domain/
?   ??? Common/
?   ?   ??? AggregateRoot.cs
?   ??? Events/
?   ?   ??? DomainEvents.cs
?   ??? ValueObjects/
?   ?   ??? MedicineValueObjects.cs
?   ??? Medicine/
?       ??? MedicineAggregateRoot.cs
??? Repositories/
    ??? IMedicineRepository.cs
```

### Infrastructure (Implementations)
```
Infrastructure/
??? Messaging/
?   ??? RabbitMqMessageBus.cs
?   ??? RabbitMqMessageBusV2.cs
??? Repositories/
    ??? MedicineRepository.cs
```

### Application (medicine_command_worker_host)
```
medicine_command_worker_host/
??? Handlers/
?   ??? CreateMedicineCommandHandler.cs
??? Services/
?   ??? CreateMedicineConsumerService.cs
??? Program.cs
```

### Presentation (test_service)
```
test_service/
??? Controllers/
?   ??? MedicineController.cs
??? Data/
?   ??? ApplicationDbContext.cs
??? Program.cs
```

## Dependency Flow

```
???????????????????????????????????????
?         Presentation Layer          ?
?        (test_service) ?
?      ?
?  Controllers ? IMessageBus     ?
???????????????????????????????????????
               ? depends on
     ?
???????????????????????????????????????
?   Application Layer   ?
?   (medicine_command_worker_host)    ?
?         ?
?  Handlers ? IMedicineRepository     ?
?  Handlers ? MedicineAggregateRoot   ?
???????????????????????????????????????
               ? depends on
   ?
???????????????????????????????????????
?    SharedKernel Layer          ?
?     (Domain & Interfaces)   ?
?        ?
?  • IMedicineRepository (interface)  ?
?  • MedicineAggregateRoot (domain)   ?
?  • Domain Events            ?
?  • Value Objects     ?
???????????????????????????????????????
    ?
             ? implements
???????????????????????????????????????
?        Infrastructure Layer         ?
?        (Implementations)       ?
?   ?
?  • MedicineRepository (implements)  ?
?  • RabbitMqMessageBus (implements)  ?
???????????????????????????????????????
```

## Clean Architecture Principles

### 1. **Dependency Rule** ?
- Inner layers don't depend on outer layers
- All dependencies point inward
- Domain is at the center (SharedKernel)

### 2. **Interface Segregation** ?
- Interfaces defined in SharedKernel
- Implementations in Infrastructure
- Loose coupling

### 3. **Dependency Inversion** ?
- High-level modules (Handlers) don't depend on low-level modules (Repository)
- Both depend on abstractions (IMedicineRepository)

## Layer Responsibilities

### SharedKernel (Core Domain)

**Purpose:** Contains domain logic and contracts

**Contents:**
- ? `MedicineAggregateRoot` - Domain model with business rules
- ? `DomainEvents` - Events raised by aggregate
- ? `ValueObjects` - Money, Strength, StockQuantity
- ? `IMedicineRepository` - Repository contract
- ? `AggregateRoot` - Base class for aggregates

**Dependencies:** None (pure .NET + MongoDB.Bson for ObjectId)

### Infrastructure (Technical Concerns)

**Purpose:** Implements technical details

**Contents:**
- ? `MedicineRepository` - MongoDB implementation
- ? `RabbitMqMessageBus` - RabbitMQ implementation

**Dependencies:** 
- SharedKernel (for interfaces and domain model)
- MongoDB.Driver
- RabbitMQ.Client

### Application (Use Cases)

**Purpose:** Orchestrates domain logic

**Contents:**
- ? `CreateMedicineCommandHandler` - Command handler
- ? `CreateMedicineConsumerService` - Background consumer

**Dependencies:**
- SharedKernel (for interfaces, domain, commands)
- Infrastructure (for implementations, registered via DI)

### Presentation (API)

**Purpose:** HTTP interface

**Contents:**
- ? `MedicineController` - REST API endpoint
- ? `ApplicationDbContext` - EF Core context

**Dependencies:**
- SharedKernel (for commands, messaging interface)
- Infrastructure (for implementations, registered via DI)

## Registration Flow

### test_service (API)
```csharp
// Register Infrastructure implementation
builder.Services.AddSingleton<IMessageBus, RabbitMqMessageBus>();

// API only publishes commands, doesn't need repository
```

### medicine_command_worker_host (Worker)
```csharp
// Register Infrastructure implementations
builder.Services.AddScoped<IMedicineRepository, MedicineRepository>();
builder.Services.AddSingleton<IMessageBus, RabbitMqMessageBus>();

// Register Application handler
builder.Services.AddScoped<ICommandHandler<CreateMedicineCommand, Result<MedicineResponse>>, 
    CreateMedicineCommandHandler>();
```

## Key Benefits

### ? Testability
```csharp
// Easy to mock repository for unit tests
var mockRepository = new Mock<IMedicineRepository>();
var handler = new CreateMedicineCommandHandler(mockRepository.Object, logger);
```

### ? Replaceability
Can easily switch MongoDB to SQL Server:
```csharp
// Just create new implementation
public class SqlMedicineRepository : IMedicineRepository
{
    // SQL Server implementation
}

// Register different implementation
builder.Services.AddScoped<IMedicineRepository, SqlMedicineRepository>();
```

### ? Domain Isolation
Domain logic (SharedKernel) has no dependencies on:
- Database technology
- Message bus technology
- Web framework

### ? Clean Separation
```
????????????????????????????????????????
? SharedKernel     ?
? • Pure domain logic      ?
? • No infrastructure dependencies     ?
? • Defines contracts (interfaces)     ?
????????????????????????????????????????
          ?         ?
     ?        ?
????????????????   ???????????????????
?Infrastructure?   ? Application     ?
? • MongoDB    ?   ? • Handlers      ?
? • RabbitMQ   ?   ? • Use cases     ?
????????????????   ???????????????????
      ?     ?
          ??????????????????
    ???????????????????
 ?  Presentation   ?
  ?  • API  ?
          ???????????????????
```

## Testing Strategy

### Unit Tests (Domain)
```csharp
// Test domain logic in isolation
[Fact]
public void CreateFromCommand_ValidCommand_CreatesAggregate()
{
    var command = new CreateMedicineCommand { /* ... */ };
    var aggregate = MedicineAggregateRoot.CreateFromCommand(command);
    
    Assert.NotNull(aggregate);
    Assert.Single(aggregate.DomainEvents);
}
```

### Unit Tests (Handler with Mock)
```csharp
[Fact]
public async Task HandleAsync_ValidCommand_SavesToRepository()
{
    var mockRepo = new Mock<IMedicineRepository>();
    var handler = new CreateMedicineCommandHandler(mockRepo.Object, logger);
    
    var result = await handler.HandleAsync(command);
    
    mockRepo.Verify(r => r.AddAsync(It.IsAny<MedicineAggregateRoot>(), default), Times.Once);
}
```

### Integration Tests (Repository)
```csharp
[Fact]
public async Task AddAsync_ValidAggregate_SavesToMongoDB()
{
    var repository = new MedicineRepository(mongoClient, logger);
    var aggregate = MedicineAggregateRoot.CreateFromCommand(command);
    
    await repository.AddAsync(aggregate);
    var saved = await repository.GetByIdAsync(aggregate.Id);
    
    Assert.NotNull(saved);
}
```

## Comparison: Before vs After

### Before (Anemic Model)
```
test_service/
??? Models/Entities.cs (anemic POCOs)
??? Repositories/MedicineRepository.cs
??? Services/MedicineService.cs
??? Handlers/CreateMedicineCommandHandler.cs

? Domain logic scattered
? Tight coupling
? Hard to test
? No clear boundaries
```

### After (Clean Architecture)
```
SharedKernel/
??? Domain/Medicine/MedicineAggregateRoot.cs (rich domain)
??? Repositories/IMedicineRepository.cs (interface)

Infrastructure/
??? Repositories/MedicineRepository.cs (implementation)

medicine_command_worker_host/
??? Handlers/CreateMedicineCommandHandler.cs

? Domain logic centralized
? Loose coupling via interfaces
? Easy to test
? Clear boundaries
```

## Summary

? **Repository Interface** in SharedKernel  
? **Repository Implementation** in Infrastructure  
? **Domain Model** in SharedKernel  
? **Handlers** use interfaces, not concrete classes  
? **Clean separation** of concerns  
? **Dependency Inversion** principle applied  
? **Easy to test** each layer independently  
? **Easy to replace** implementations  

**This is proper Clean Architecture!** ???
