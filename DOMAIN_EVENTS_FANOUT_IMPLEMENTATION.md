# ? Domain Events Published to RabbitMQ Fanout Exchange

## Architecture Overview

```
Command ? Handler ? Aggregate ? Domain Events ? Fanout Exchange ? All Subscribers
```

## Components Implemented

### 1. **IDomainEventPublisher Interface** (SharedKernel)
```csharp
public interface IDomainEventPublisher
{
  Task PublishAsync<TEvent>(TEvent domainEvent, CancellationToken cancellationToken = default)
  where TEvent : DomainEvent;

    Task PublishManyAsync(IEnumerable<DomainEvent> domainEvents, CancellationToken cancellationToken = default);
}
```

### 2. **RabbitMqDomainEventPublisher** (Infrastructure)
- Publishes domain events to **fanout exchange**: `thanos.domain.events`
- Fanout broadcasts to **ALL bound queues**
- No routing key needed

### 3. **DomainEventConsumerService** (Application)
- Sample consumer showing how to listen to domain events
- Creates queue: `thanos.medicine.worker.domain.events`
- Binds to fanout exchange
- Handles different event types

## RabbitMQ Topology with Thanos Prefix

### Exchange
```
Name: thanos.domain.events
Type: fanout
Durable: true
Auto-delete: false
```

### How Fanout Works
```
thanos.domain.events (fanout)
    ??? thanos.medicine.worker.domain.events (medicine worker)
    ??? thanos.notification.service.events (notification service)
    ??? thanos.analytics.service.events (analytics service)
    ??? ... (any service that binds a queue)
```

**All bound queues receive ALL events!**

## Message Flow

### 1. Create Medicine Command
```
POST /api/medicine
{
  "name": "Aspirin",
  "genericName": "Acetylsalicylic Acid",
  "manufacturer": "Bayer",
  "strength": "500mg",
  "price": 9.99,
  "stockQuantity": 100
}
```

### 2. Command Processing
```
API Controller
  ? publishes to
Command Queue: thanos.medicine
  ? consumed by
CreateMedicineCommandHandler
  ? creates
MedicineAggregateRoot
  ? raises
MedicineCreatedEvent
  ? published to
thanos.domain.events (fanout exchange)
  ? broadcasts to
ALL bound queues
```

### 3. Event Message Structure
```json
{
  "medicineId": "507f1f77bcf86cd799439011",
  "name": "Aspirin",
  "genericName": "Acetylsalicylic Acid",
  "manufacturer": "Bayer",
  "price": 9.99,
  "stockQuantity": 100,
  "eventId": "a3d5c2b1-1234-5678-90ab-cdef12345678",
  "occurredOn": "2024-01-15T10:30:00Z"
}
```

**Message Properties:**
- `Type`: Event class name (e.g., "MedicineCreatedEvent")
- `MessageId`: Event ID (GUID)
- `Timestamp`: When event occurred
- `ContentType`: "application/json"
- `Persistent`: true (durable)

## Domain Events Published

### MedicineCreatedEvent
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

**Published When:** New medicine is created

### MedicineStockUpdatedEvent
```csharp
public record MedicineStockUpdatedEvent(
    string MedicineId,
    int OldStock,
    int NewStock,
    string Reason
) : DomainEvent;
```

**Published When:** Stock quantity changes

### MedicinePriceChangedEvent
```csharp
public record MedicinePriceChangedEvent(
    string MedicineId,
    decimal OldPrice,
    decimal NewPrice,
    string Reason
) : DomainEvent;
```

**Published When:** Price is updated

### MedicineAvailabilityChangedEvent
```csharp
public record MedicineAvailabilityChangedEvent(
    string MedicineId,
    bool IsAvailable,
    string Reason
) : DomainEvent;
```

**Published When:** Medicine becomes available/unavailable

### MedicineUpdatedEvent
```csharp
public record MedicineUpdatedEvent(
 string MedicineId,
    string Name,
    Dictionary<string, object> ChangedProperties
) : DomainEvent;
```

**Published When:** Medicine details are updated

## Handler Integration

```csharp
public class CreateMedicineCommandHandler
{
    private readonly IMedicineRepository _repository;
    private readonly IDomainEventPublisher _eventPublisher;

    public async Task<Result<MedicineResponse>> HandleAsync(CreateMedicineCommand command)
    {
     // 1. Create aggregate
        var medicineAggregate = MedicineAggregateRoot.CreateFromCommand(command);
  
        // 2. Save to repository
     await _repository.AddAsync(medicineAggregate);
        await _repository.SaveChangesAsync();
        
        // 3. Publish domain events to fanout exchange
    if (medicineAggregate.DomainEvents.Any())
    {
            await _eventPublisher.PublishManyAsync(medicineAggregate.DomainEvents);
  medicineAggregate.ClearDomainEvents();
        }
     
        return Result.Success(response);
    }
}
```

## Creating a Domain Event Consumer

### Step 1: Create Consumer Service

```csharp
public class MyDomainEventConsumerService : BackgroundService
{
    private const string ExchangeName = "thanos.domain.events";
    private const string QueueName = "thanos.my.service.events";
    
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
{
        // 1. Declare fanout exchange
      _channel.ExchangeDeclare(
            exchange: ExchangeName,
      type: ExchangeType.Fanout,
   durable: true);
        
        // 2. Declare your queue with thanos prefix
    _channel.QueueDeclare(
  queue: QueueName,
 durable: true,
 exclusive: false,
            autoDelete: false);
        
        // 3. Bind queue to fanout exchange
        _channel.QueueBind(
         queue: QueueName,
            exchange: ExchangeName,
            routingKey: string.Empty);  // Fanout doesn't use routing keys
        
        // 4. Consume messages
        var consumer = new EventingBasicConsumer(_channel);
        consumer.Received += async (model, ea) =>
        {
    var eventType = ea.BasicProperties.Type;
     var json = Encoding.UTF8.GetString(ea.Body.ToArray());
   
            await HandleEventAsync(eventType, json);
            
  _channel.BasicAck(ea.DeliveryTag, false);
   };
        
        _channel.BasicConsume(QueueName, false, consumer);
    }
}
```

### Step 2: Handle Events

```csharp
private async Task HandleEventAsync(string eventType, string json)
{
    switch (eventType)
{
        case nameof(MedicineCreatedEvent):
          var evt = JsonSerializer.Deserialize<MedicineCreatedEvent>(json);
   // Update read model, send notification, etc.
          await UpdateReadModelAsync(evt);
            break;
        
        case nameof(MedicineStockUpdatedEvent):
 var stockEvt = JsonSerializer.Deserialize<MedicineStockUpdatedEvent>(json);
            await CheckLowStockAsync(stockEvt);
            break;
    }
}
```

### Step 3: Register in Program.cs

```csharp
builder.Services.AddHostedService<MyDomainEventConsumerService>();
```

## Thanos Naming Convention

### All RabbitMQ Resources Use Thanos Prefix

**Commands (Direct Exchange):**
- Exchange: `medicine.events`
- Queue: `thanos.medicine`
- Routing Key: `thanos.medicine`

**Domain Events (Fanout Exchange):**
- Exchange: `thanos.domain.events` ?
- Queues: `thanos.<service>.domain.events` ?
- Examples:
  - `thanos.medicine.worker.domain.events`
  - `thanos.notification.service.events`
  - `thanos.analytics.service.events`

## Example Use Cases

### Use Case 1: Notification Service
```
thanos.domain.events (fanout)
  ?
thanos.notification.service.events
  ?
Send email when medicine created
Send alert when stock is low
```

### Use Case 2: Analytics Service
```
thanos.domain.events (fanout)
  ?
thanos.analytics.service.events
  ?
Track medicine creation trends
Monitor price changes
Calculate inventory metrics
```

### Use Case 3: Read Model Update (CQRS)
```
thanos.domain.events (fanout)
  ?
thanos.read.model.events
  ?
Update denormalized views
Update search indexes
Refresh cached data
```

### Use Case 4: Audit Service
```
thanos.domain.events (fanout)
  ?
thanos.audit.service.events
  ?
Log all domain events
Track who changed what
Compliance reporting
```

## Logs During Execution

### Publisher (Handler)
```
?? Received CreateMedicineCommand: Aspirin
? Aggregate created. ID: 507f..., Events: 1
?? Medicine saved: 507f...
?? Publishing 1 domain events to fanout exchange thanos.domain.events
?? Published domain event: MedicineCreatedEvent (ID: a3d5...) to fanout exchange thanos.domain.events
? Domain events published and cleared
? Command completed: 507f...
```

### Consumer (Domain Event Consumer)
```
?? Domain Event Consumer Service starting...
? Domain Event Consumer listening on exchange: thanos.domain.events, queue: thanos.medicine.worker.domain.events
?? Received domain event: MedicineCreatedEvent (MessageId: a3d5...)
Processing MedicineCreatedEvent: {"medicineId":"507f...","name":"Aspirin",...}
?? Medicine Created: Aspirin (ID: 507f...)
? Processed domain event: MedicineCreatedEvent
```

## Benefits of Fanout Exchange

### ? Broadcast Pattern
- All subscribers receive all events
- New subscribers automatically get events
- No coordination needed

### ? Loose Coupling
- Publisher doesn't know about consumers
- Add/remove consumers without changing publisher
- Services are independent

### ? Scalability
- Multiple instances of same consumer can share a queue
- Each service can have its own queue
- Horizontal scaling supported

### ? Event Sourcing Ready
- All domain events captured
- Complete audit trail
- Can replay events

### ? CQRS Pattern
- Write side raises events
- Read side consumes events
- Eventual consistency

## Testing

### Test Event Publishing
1. Start worker: `dotnet run --project medicine_command_worker_host`
2. Start API: `dotnet run --project test_service`
3. Create medicine via API
4. Check logs for event publishing and consumption

### Verify RabbitMQ Management UI
1. Open: http://localhost:15672 (or CloudAMQP dashboard)
2. Go to **Exchanges**
3. Find `thanos.domain.events` (type: fanout)
4. Check **Bindings** - should show all consumer queues
5. Go to **Queues**
6. Find `thanos.medicine.worker.domain.events`
7. Should see messages if events were published

## Configuration

### appsettings.json
```json
{
  "RabbitMq": {
    "ConnectionString": "amqps://user:pass@host.cloudamqp.com/vhost"
  }
}
```

### Same Connection for Both
- **Commands**: Direct exchange (`medicine.events`)
  - Queue: `thanos.medicine`
- **Domain Events**: Fanout exchange (`thanos.domain.events`)
  - Queues: `thanos.<service>.domain.events`

## Comparison: Direct vs Fanout

### Direct Exchange (Commands)
```
medicine.events (direct)
  ? routing key: thanos.medicine
thanos.medicine queue
  ?
Single consumer (CreateMedicineCommandHandler)
```

**Use For:** Commands (1 consumer per command)

### Fanout Exchange (Domain Events)
```
thanos.domain.events (fanout)
  ??? thanos.medicine.worker.domain.events ? Worker Service
  ??? thanos.notification.service.events ? Notification Service
  ??? thanos.analytics.service.events ? Analytics Service
```

**Use For:** Events (multiple consumers per event)

## Summary

? **Thanos Prefix** used consistently across all RabbitMQ resources  
? **Domain Events** published to `thanos.domain.events` fanout exchange  
? **Fanout broadcasts** to all bound queues with thanos prefix  
? **Loose coupling** between publisher and consumers  
? **Sample consumer** demonstrates event handling  
? **All event types** supported (Created, Updated, StockChanged, etc.)  
? **CQRS ready** - events update read models  
? **Scalable** - multiple consumers can process events  

**This is proper event-driven architecture with domain events and consistent naming!** ??
