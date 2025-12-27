# ? Queue Naming Convention Updated

## Changes Applied

### Old Format (Hyphen-based)
```
thanos_create-medicine-command
thanos_update-medicine-command
thanos_delete-medicine-command
```

### New Format (Dot notation)
```
thanos.medicine.created.queue
thanos.medicine.updated.queue
thanos.medicine.deleted.queue
```

## Updated Files

### 1. Infrastructure/Messaging/RabbitMqMessageBus.cs
- ? Changed `QueuePrefix` from `thanos_` to `thanos.`
- ? Updated `GetQueueNameFromType()` method to use dot notation
- ? Converts `CreateMedicineCommand` ? `thanos.medicine.created.queue`

### 2. test_service/BackgroundServices/CommandConsumerService.cs
- ? Updated queue names:
  - `thanos_create-medicine-command` ? `thanos.medicine.created.queue`
  - `thanos_update-medicine-command` ? `thanos.medicine.updated.queue`
  - `thanos_delete-medicine-command` ? `thanos.medicine.deleted.queue`

### 3. test_service/BackgroundServices/MessageConsumerService.cs
- ? Updated example queue names:
  - `thanos_orders` ? `thanos.orders.queue`
  - `thanos_notifications` ? `thanos.notifications.queue`

### 4. medicine_command_worker_host/Services/CreateMedicineConsumerService.cs
- ? Updated queue name:
  - `thanos_create-medicine-command` ? `thanos.medicine.created.queue`

### 5. medicine_command_worker_host/Program.cs
- ? Updated console message to show new queue name

## Naming Rules

```
Format: thanos.{entity}.{action}.queue
```

### Command Mapping
- `Create*Command` ? `thanos.{entity}.created.queue`
- `Update*Command` ? `thanos.{entity}.updated.queue`
- `Delete*Command` ? `thanos.{entity}.deleted.queue`

### Examples
```csharp
CreateMedicineCommand     ? thanos.medicine.created.queue
UpdateMedicineCommand     ? thanos.medicine.updated.queue
DeleteMedicineCommand     ? thanos.medicine.deleted.queue
CreateOrderCommand        ? thanos.order.created.queue
CreateInventoryItemCommand ? thanos.inventory.item.created.queue
```

## How It Works

### Automatic Conversion
```csharp
// You send a command
await _messageBus.SendAsync(new CreateMedicineCommand(...));

// It automatically routes to
// Queue: thanos.medicine.created.queue
```

### Manual Subscription
```csharp
// Subscribe to specific queue
await _messageBus.SubscribeAsync<CreateMedicineCommand>(
    "thanos.medicine.created.queue", 
    handler
);
```

## Testing

### 1. Start Worker
```bash
cd medicine_command_worker_host
dotnet run
```

**Expected Output:**
```
?? Medicine Command Worker Host Starting...
?? Listening for CreateMedicineCommand on queue: thanos.medicine.created.queue
Press Ctrl+C to stop
```

### 2. Start API
```bash
cd test_service
dotnet run
```

### 3. Send Command
```bash
POST http://localhost:5035/api/medicine
```

### 4. Check Logs
**Worker should show:**
```
?????? EVENT FIRED! Message received from queue thanos.medicine.created.queue
```

## RabbitMQ Management UI

### View Queues
Look for queues with new naming:
- `thanos.medicine.created.queue`
- `thanos.medicine.updated.queue`
- `thanos.medicine.deleted.queue`

### Exchange
- Exchange: `medicine.events`
- Type: `direct`
- Bindings: Each queue bound with its name as routing key

## Benefits

### ? Clear Hierarchy
```
thanos.
??? medicine.
    ??? created.queue
    ??? updated.queue
 ??? deleted.queue
```

### ? Easy Filtering
- Filter by `thanos.medicine` to see all medicine queues
- Filter by `.created.queue` to see all creation queues

### ? Self-Documenting
- Queue name tells you exactly what it contains
- No need to refer to documentation

### ? Topic Exchange Ready
Can upgrade to topic exchange for pattern-based routing:
```
thanos.medicine.*    ? All medicine events
thanos.*.created.queue ? All creation events
thanos.#     ? Everything
```

## Migration Notes

?? **Breaking Change**

Old queues with hyphen format will NOT be automatically migrated:
- `thanos_create-medicine-command` will NOT receive new messages
- Messages published go to `thanos.medicine.created.queue` instead

### To Migrate
1. Process or discard messages in old queues
2. Delete old queues in RabbitMQ Management UI
3. Deploy new version

### No Backward Compatibility
- Old consumers will NOT work with new queue names
- Update all consumers to use new queue names

## Summary

? **Naming convention updated** to dot notation  
? **All files updated** with new queue names  
? **Build successful** - ready to deploy  
? **Automatic conversion** from command names  
? **Self-documenting** queue names  

**New queue format: `thanos.{entity}.{action}.queue`** ??

See `QUEUE_NAMING_CONVENTION.md` for complete documentation!
