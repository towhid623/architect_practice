# MESSAGE BUS SEND COMMAND IMPLEMENTATION

## Overview
The message bus now has a `SendAsync` method that accepts any command object (implementing `ICommand`) and automatically publishes it to RabbitMQ with a queue name derived from the command type with the **"thanos_"** prefix.

## Changes Made

### 1. IMessageBus Interface (SharedKernel/Messaging/IMessageBus.cs)
Added new method:
```csharp
Task SendAsync<TCommand>(TCommand command, CancellationToken cancellationToken = default) 
    where TCommand : class, ICommand;
```

### 2. RabbitMqMessageBus Implementation (Infrastructure/Messaging/RabbitMqMessageBus.cs)
Implemented `SendAsync` with:
- Automatic queue name derivation from command type
- Convention: `CreateMedicineCommand` ? `thanos_create-medicine-command`
- All queues prefixed with **"thanos_"**
- Publishes command to the derived queue name

### 3. MedicineController Updated
**Async Endpoints (Send to RabbitMQ):**
- `POST /api/medicine` - Sends CreateMedicineCommand to RabbitMQ
- `PUT /api/medicine/{id}` - Sends UpdateMedicineCommand to RabbitMQ
- `DELETE /api/medicine/{id}` - Sends DeleteMedicineCommand to RabbitMQ

**Synchronous Endpoints (Direct Handler):**
- `POST /api/medicine/sync` - Directly handles CreateMedicineCommand
- `PUT /api/medicine/sync/{id}` - Directly handles UpdateMedicineCommand
- `DELETE /api/medicine/sync/{id}` - Directly handles DeleteMedicineCommand

### 4. CommandConsumerService (test_service/BackgroundServices/CommandConsumerService.cs)
New background service that:
- Subscribes to command queues in RabbitMQ (with thanos_ prefix)
- Automatically processes commands when received
- Uses dependency injection to resolve command handlers
- Handles all three medicine commands (Create, Update, Delete)

## How It Works

### Sending Commands from Controller

```csharp
[HttpPost]
public async Task<IActionResult> CreateMedicine([FromBody] CreateMedicineCommand command)
{
    // Send command to RabbitMQ
    await _messageBus.SendAsync(command);
    
    return Accepted(new 
    { 
        message = "Command accepted and sent to message bus for processing"
    });
}
```

**Flow:**
1. Controller receives HTTP POST request with command
2. Controller calls `_messageBus.SendAsync(command)`
3. Message bus derives queue name: `thanos_create-medicine-command`
4. Command is serialized to JSON and published to RabbitMQ
5. HTTP 202 Accepted returned immediately (async processing)

### Processing Commands (Background Service)

```csharp
// Subscribe to command queue with thanos_ prefix
await _messageBus.SubscribeAsync<CreateMedicineCommand>("thanos_create-medicine-command", async (command) =>
{
    // Resolve handler from DI
    var handler = scope.ServiceProvider
        .GetRequiredService<ICommandHandler<CreateMedicineCommand, Result<MedicineResponse>>>();

    // Execute command
    var result = await handler.HandleAsync(command);
});
```

**Flow:**
1. CommandConsumerService subscribes to all command queues on startup
2. When a command arrives in the queue:
   - Command is deserialized from JSON
   - Appropriate handler is resolved from DI
   - Command is executed
   - Result is logged
3. Message is acknowledged to RabbitMQ

## Queue Naming Convention

All commands are automatically mapped to queue names with **"thanos_"** prefix:

| Command Type | Queue Name |
|--------------|------------|
| CreateMedicineCommand | **thanos_**create-medicine-command |
| UpdateMedicineCommand | **thanos_**update-medicine-command |
| DeleteMedicineCommand | **thanos_**delete-medicine-command |

**Pattern:** `thanos_` + command-type-in-kebab-case

## Benefits

### 1. Asynchronous Processing
- Controller responds immediately with 202 Accepted
- Command is processed in the background
- No blocking of HTTP requests

### 2. Decoupling
- API layer decoupled from business logic execution
- Commands can be processed by separate services
- Horizontal scaling possible

### 3. Reliability
- Commands are persisted in RabbitMQ
- If processing fails, commands can be retried
- Durable queues survive service restarts

### 4. Convention-Based
- Queue names automatically derived from command types
- Consistent "thanos_" prefix for all queues
- No manual queue name management needed

### 5. Namespace Isolation
- All application queues prefixed with "thanos_"
- Easy to identify and filter queues
- Prevents naming conflicts with other applications

## Usage Examples

### Example 1: Create Medicine (Async)
```bash
POST /api/medicine
Content-Type: application/json

{
    "name": "Aspirin",
 "genericName": "Acetylsalicylic Acid",
    "manufacturer": "Bayer",
    "description": "Pain reliever",
    "dosageForm": "Tablet",
    "strength": "500mg",
    "price": 9.99,
    "stockQuantity": 100,
    "requiresPrescription": false,
    "category": "Pain Relief",
    "storageInstructions": "Store at room temperature"
}

Response: 202 Accepted
{
    "message": "Command accepted and sent to message bus for processing",
    "commandType": "CreateMedicineCommand"
}

# Command sent to queue: thanos_create-medicine-command
```

### Example 2: Create Medicine (Sync)
```bash
POST /api/medicine/sync
Content-Type: application/json

{
    "name": "Aspirin",
    "genericName": "Acetylsalicylic Acid",
    // ... same as above
}

Response: 201 Created
{
    "id": "65abc123...",
    "name": "Aspirin",
    // ... full medicine response
}
```

### Example 3: Update Medicine (Async)
```bash
PUT /api/medicine/65abc123...
Content-Type: application/json

{
    "id": "65abc123...",
    "name": "Aspirin Updated",
    "price": 10.99,
    "stockQuantity": 150
}

Response: 202 Accepted
{
    "message": "Command accepted and sent to message bus for processing",
    "commandType": "UpdateMedicineCommand"
}

# Command sent to queue: thanos_update-medicine-command
```

### Example 4: Delete Medicine (Async)
```bash
DELETE /api/medicine/65abc123...

Response: 202 Accepted
{
    "message": "Command accepted and sent to message bus for processing",
    "commandType": "DeleteMedicineCommand"
}

# Command sent to queue: thanos_delete-medicine-command
```

## Enabling Command Processing

To enable background command processing, uncomment this line in `Program.cs`:

```csharp
builder.Services.AddHostedService<CommandConsumerService>();
```

**Note:** The service can run in:
- Same process as API (for development)
- Separate worker process (for production)
- Multiple instances (for high availability)

## Monitoring

Commands are logged at each stage:

**When Sending:**
```
Sending command CreateMedicineCommand to queue thanos_create-medicine-command
Published message to queue thanos_create-medicine-command
```

**When Processing:**
```
Received CreateMedicineCommand for medicine: Aspirin
Successfully processed CreateMedicineCommand. Medicine ID: 65abc123...
```

**When Errors Occur:**
```
Failed to process CreateMedicineCommand. Error: Name is required
Error handling command CreateMedicineCommand
```

## RabbitMQ Management

### Viewing Queues
All application queues will be visible in RabbitMQ Management UI with the **thanos_** prefix:
- thanos_create-medicine-command
- thanos_update-medicine-command
- thanos_delete-medicine-command
- thanos_orders (example)
- thanos_notifications (example)

### Queue Properties
- **Durable**: Yes (survives broker restart)
- **Auto-delete**: No
- **Exclusive**: No
- **Arguments**: None

## Best Practices

### 1. Use Async Endpoints for Long-Running Operations
- Database-heavy operations
- Operations involving external services
- Bulk operations

### 2. Use Sync Endpoints for Simple Operations
- Quick lookups
- Validation-only operations
- When immediate feedback is needed

### 3. Implement Idempotency
- Commands should be safe to retry
- Use unique identifiers to detect duplicates
- Handle partial failures gracefully

### 4. Add Monitoring
- Track queue depth for all thanos_* queues
- Monitor processing time
- Alert on failures

### 5. Consider DLQ (Dead Letter Queue)
- Create thanos_dlq for failed messages
- Capture commands that fail multiple times
- Allow manual inspection and reprocessing

## Future Enhancements

1. **Command Validation**: Add validation before sending to queue
2. **Command Routing**: Support for multiple consumers based on command type
3. **Priority Queues**: thanos_high_priority, thanos_low_priority
4. **Command Correlation**: Track command through entire pipeline
5. **Retry Policies**: Automatic retry with exponential backoff
6. **Circuit Breakers**: Stop sending if queue is unhealthy
7. **Saga Pattern**: Coordinate multi-step transactions
8. **Event Sourcing**: Store commands as events

## Summary

? All queues prefixed with **"thanos_"**  
? Commands can be sent to RabbitMQ using `SendAsync(command)`  
? Queue names automatically derived: `thanos_create-medicine-command`  
? Background service processes commands asynchronously  
? Both async and sync endpoints available  
? Proper error handling and logging  
? Follows CQRS and message-driven architecture patterns  
? Easy to identify application queues in RabbitMQ Management UI  
