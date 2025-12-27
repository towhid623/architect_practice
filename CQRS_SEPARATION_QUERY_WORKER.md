# ? CQRS Separation: Query Worker with Domain Event Consumer

## Architecture Overview

```
????????????????????????????????????????????????????????????????
?            CQRS Pattern     ?
????????????????????????????????????????????????????????????????

???????????????????????????         ???????????????????????????
?   WRITE SIDE            ?       ?   READ SIDE  ?
?   (Command Worker)      ?         ?   (Query Worker)        ?
???????????????????????????   ???????????????????????????
?           ?         ?      ?
?  ?? Receive Commands    ?         ?  ?? Listen to Events    ?
?  ??  Execute Business   ?         ?  ?? Update Read Models  ?
?     Logic            ?    ?    ?  ?? Serve Queries       ?
?  ?? Save to Write DB    ?  Events ?  ? Fast Reads          ?
?  ?? Publish Events      ?         ?            ?
?                ?    ?             ?
???????????????????????????    ???????????????????????????
  ?                 ?
 MongoDB   Read Model DB
  (Write Store)      (Optimized for reads)
```

## Projects Structure

### medicine_command_worker_host (Write Side)
```
medicine_command_worker_host/
??? Handlers/
?   ??? CreateMedicineCommandHandler.cs
??? Services/
?   ??? CreateMedicineConsumerService.cs
??? Program.cs

Purpose:
  ? Process commands
  ? Execute business logic (aggregate roots)
  ? Save to write database (MongoDB)
  ? Publish domain events
```

### medicine_query_worker_host (Read Side) ? NEW!
```
medicine_query_worker_host/
??? Services/
?   ??? DomainEventConsumerService.cs
??? Program.cs
??? appsettings.json

Purpose:
  ? Listen to domain events
  ? Update read models
  ? Maintain denormalized views
  ? Update search indexes
  ? Invalidate caches
```

## Message Flow

### 1. Command Flow (Write Side)
```
API POST /api/medicine
  ?
thanos.medicine queue
  ?
CreateMedicineConsumerService
  ?
CreateMedicineCommandHandler
  ?
MedicineAggregateRoot.CreateFromCommand()
  ?
Save to MongoDB (Write Store)
  ?
Publish MedicineCreatedEvent to thanos.domain.events
```

### 2. Event Flow (Read Side)
```
thanos.domain.events (fanout exchange)
  ? broadcasts to
thanos.medicine.query.domain.events queue
  ?
DomainEventConsumerService (Query Worker)
  ?
Update Read Model
  ?
  - Update denormalized views
  - Update search indexes (Elasticsearch, etc.)
  - Invalidate caches (Redis, etc.)
  - Update aggregated data
```

## Queue Configuration

### Command Worker Queues
```
Queue: thanos.medicine
Purpose: Receive commands from API
Exchange: medicine.events (direct)
Consumer: CreateMedicineConsumerService
```

### Query Worker Queues
```
Queue: thanos.medicine.query.domain.events
Purpose: Receive domain events for read model updates
Exchange: thanos.domain.events (fanout)
Consumer: DomainEventConsumerService
```

### Event Broadcasting
```
thanos.domain.events (fanout)
  ??? thanos.medicine.query.domain.events (Query Worker)
  ??? thanos.notification.service.events (Notification Service)
  ??? thanos.analytics.service.events (Analytics Service)
  ??? ... (other consumers)
```

## Domain Event Handling (Read Side)

### MedicineCreatedEvent
```csharp
case nameof(MedicineCreatedEvent):
    var evt = JsonSerializer.Deserialize<MedicineCreatedEvent>(json);
    
    // TODO: Implement read model updates
    await _readModelRepository.CreateAsync(evt);
    await _searchIndexService.IndexAsync(evt);
    await _cacheService.SetAsync(evt.MedicineId, evt);
    break;
```

**What to Update:**
- ? Read database (denormalized medicine view)
- ? Search index (Elasticsearch, Algolia, etc.)
- ? Cache (Redis, Memcached)
- ? Materialized views

### MedicineStockUpdatedEvent
```csharp
case nameof(MedicineStockUpdatedEvent):
    var evt = JsonSerializer.Deserialize<MedicineStockUpdatedEvent>(json);
    
    await _readModelRepository.UpdateStockAsync(evt.MedicineId, evt.NewStock);
    await _cacheService.InvalidateAsync(evt.MedicineId);
    await _notificationService.NotifyIfLowStock(evt);
    break;
```

### MedicinePriceChangedEvent
```csharp
case nameof(MedicinePriceChangedEvent):
    var evt = JsonSerializer.Deserialize<MedicinePriceChangedEvent>(json);
    
    await _readModelRepository.UpdatePriceAsync(evt.MedicineId, evt.NewPrice);
 await _searchIndexService.UpdatePriceAsync(evt.MedicineId, evt.NewPrice);
    await _cacheService.InvalidateAsync(evt.MedicineId);
    break;
```

## Running the Workers

### 1. Start Command Worker (Write Side)
```bash
cd medicine_command_worker_host
dotnet run
```

**Output:**
```
???????????????????????????????????????????????????????
??  Medicine Command Worker - CQRS Write Side
???????????????????????????????????????????????????????

?? Command Queue: thanos.medicine
?? Event Exchange: thanos.domain.events (fanout)

? Ready to process commands
? Ready to publish domain events
```

### 2. Start Query Worker (Read Side)
```bash
cd medicine_query_worker_host
dotnet run
```

**Output:**
```
???????????????????????????????????????????????????????
?? Medicine Query Worker - CQRS Read Side
???????????????????????????????????????????????????????

?? Purpose: Listen to domain events and update read models
?? Exchange: thanos.domain.events (fanout)
?? Queue: thanos.medicine.query.domain.events

? Ready to consume domain events
? Ready to update read models
```

### 3. Start API
```bash
cd test_service
dotnet run
```

### 4. Send Command
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

## Complete Flow Example

### Step 1: Command Received
```
[API] ?? Command published to thanos.medicine
```

### Step 2: Command Processed (Write Side)
```
[Command Worker] ?? Received CreateMedicineCommand: Aspirin
[Command Worker] ? Aggregate created. ID: 507f...
[Command Worker] ?? Medicine saved to MongoDB
[Command Worker] ?? Publishing domain event: MedicineCreatedEvent
```

### Step 3: Event Broadcast
```
[Command Worker] ?? Published to thanos.domain.events (fanout)
```

### Step 4: Event Consumed (Read Side)
```
[Query Worker] ?? Received domain event: MedicineCreatedEvent
[Query Worker] ?? Updating Read Model for MedicineCreatedEvent
[Query Worker] ?? [READ MODEL] Medicine Created: Aspirin (ID: 507f...)
[Query Worker] ? Processed domain event: MedicineCreatedEvent
```

## Benefits of This Separation

### ? Clear Responsibility Separation
- **Command Worker**: Focus on business logic and consistency
- **Query Worker**: Focus on read performance and availability

### ? Independent Scaling
- Scale command workers for write load
- Scale query workers for read load
- Different scaling strategies

### ? Optimized Data Models
- **Write Side**: Normalized, consistent, enforces invariants
- **Read Side**: Denormalized, fast reads, eventually consistent

### ? Technology Flexibility
- **Write Side**: MongoDB (good for documents and aggregates)
- **Read Side**: Could use PostgreSQL, Elasticsearch, Redis, etc.

### ? Eventual Consistency
- Write completes immediately
- Read models update asynchronously
- Better performance and availability

### ? Multiple Read Models
One write model can feed multiple read models:
- Search index (Elasticsearch)
- Cache (Redis)
- Reporting database (PostgreSQL)
- Analytics (ClickHouse, BigQuery)

## Next Steps: Implement Read Model

### Option 1: MongoDB Read Model
```csharp
public class MedicineReadModel
{
    public string Id { get; set; }
    public string Name { get; set; }
    public decimal Price { get; set; }
    public int StockQuantity { get; set; }
    public bool IsAvailable { get; set; }
    // Denormalized data for fast reads
    public string SearchText { get; set; }  // For text search
    public DateTime LastUpdated { get; set; }
}
```

### Option 2: PostgreSQL Read Model
```sql
CREATE TABLE medicine_read_model (
    id VARCHAR(50) PRIMARY KEY,
    name VARCHAR(200) NOT NULL,
    generic_name VARCHAR(200),
    manufacturer VARCHAR(200),
    price DECIMAL(10,2),
    stock_quantity INT,
    is_available BOOLEAN,
    search_vector TSVECTOR,  -- Full-text search
    last_updated TIMESTAMP
);

CREATE INDEX idx_medicine_search ON medicine_read_model USING GIN(search_vector);
```

### Option 3: Elasticsearch Read Model
```json
{
  "mappings": {
    "properties": {
      "id": { "type": "keyword" },
      "name": { "type": "text", "analyzer": "standard" },
"genericName": { "type": "text" },
      "manufacturer": { "type": "keyword" },
      "price": { "type": "float" },
      "stockQuantity": { "type": "integer" },
      "isAvailable": { "type": "boolean" },
  "searchText": { "type": "text" }
    }
  }
}
```

## Configuration

### medicine_query_worker_host/appsettings.json
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information"
    }
  },
  "RabbitMq": {
    "ConnectionString": "amqps://user:pass@host.cloudamqp.com/vhost"
  },
  "ReadDatabase": {
    "ConnectionString": "..." // Your read DB connection
  },
  "Elasticsearch": {
    "Url": "http://localhost:9200"
  },
  "Redis": {
    "ConnectionString": "localhost:6379"
  }
}
```

## Monitoring & Observability

### Metrics to Track

**Command Worker (Write Side):**
- Commands processed per second
- Command processing time
- Domain events published per second
- Write database latency

**Query Worker (Read Side):**
- Events consumed per second
- Read model update latency
- Event processing errors
- Read model lag (time between event and update)

### Health Checks

**Command Worker:**
```csharp
- MongoDB connection
- RabbitMQ connection (commands)
- RabbitMQ connection (events publish)
```

**Query Worker:**
```csharp
- RabbitMQ connection (events consume)
- Read database connection
- Search index connection
- Cache connection
```

## Testing

### Integration Test: End-to-End Flow
```csharp
[Fact]
public async Task CreateMedicine_UpdatesReadModel()
{
    // 1. Send command
    await _api.PostAsync("/api/medicine", medicineCommand);
    
    // 2. Wait for event processing
    await Task.Delay(1000);
    
  // 3. Verify read model updated
    var readModel = await _readModelDb.GetByIdAsync(medicineId);
    Assert.NotNull(readModel);
    Assert.Equal("Aspirin", readModel.Name);
}
```

## Summary

? **Domain Event Consumer** moved to query worker  
? **CQRS Separation** - Command and Query workers separate  
? **Write Side** - Processes commands, publishes events  
? **Read Side** - Consumes events, updates read models  
? **Fanout Exchange** - Events broadcast to all consumers  
? **Independent Scaling** - Scale read and write separately  
? **Eventual Consistency** - Better performance and availability  

**This is proper CQRS implementation with event-driven architecture!** ??
