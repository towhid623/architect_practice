# ? Simplified Queue Naming Convention

## New Format
```
thanos.{entity}
```

## Examples

| Command/Query Type | Queue Name |
|-------------------|------------|
| CreateMedicineCommand | `thanos.medicine` |
| UpdateMedicineCommand | `thanos.medicine` |
| DeleteMedicineCommand | `thanos.medicine` |
| GetMedicineByIdQuery | `thanos.medicine` |
| CreateOrderCommand | `thanos.order` |
| UpdateProductCommand | `thanos.product` |
| CreateInventoryItemCommand | `thanos.inventory.item` |

## Key Changes

### Before (Complex)
```
CreateMedicineCommand ? thanos.medicine.created.queue
UpdateMedicineCommand ? thanos.medicine.updated.queue
DeleteMedicineCommand ? thanos.medicine.deleted.queue
```

### After (Simplified)
```
CreateMedicineCommand ? thanos.medicine
UpdateMedicineCommand ? thanos.medicine
DeleteMedicineCommand ? thanos.medicine
```

## Benefits

### ? Simpler
- Single queue per entity
- No action-based separation
- Easier to understand

### ? Unified
- All medicine operations go to `thanos.medicine`
- Consumer handles all command types
- Single subscription point

### ? Flexible
- Easy to add new command types
- No queue proliferation
- Simpler routing

## How It Works

### Queue Naming Logic
```csharp
1. Start with command name: CreateMedicineCommand
2. Remove "Command" suffix: CreateMedicine
3. Remove action prefix: Medicine
4. Convert to lowercase with dots: medicine
5. Add prefix: thanos.medicine
```

### Examples
```csharp
CreateMedicineCommand
  ? Remove "Command" ? CreateMedicine
  ? Remove "Create" ? Medicine
  ? Lowercase ? medicine
  ? Add prefix ? thanos.medicine

UpdateProductCommand
  ? Remove "Command" ? UpdateProduct
  ? Remove "Update" ? Product
  ? Lowercase ? product
  ? Add prefix ? thanos.product

CreateInventoryItemCommand
  ? Remove "Command" ? CreateInventoryItem
  ? Remove "Create" ? InventoryItem
  ? Lowercase with dots ? inventory.item
  ? Add prefix ? thanos.inventory.item
```

## Consumer Pattern

### Single Queue, Multiple Command Types
```csharp
// Subscribe once to handle all medicine commands
await _messageBus.SubscribeAsync<CreateMedicineCommand>("thanos.medicine", HandleCreate);
await _messageBus.SubscribeAsync<UpdateMedicineCommand>("thanos.medicine", HandleUpdate);
await _messageBus.SubscribeAsync<DeleteMedicineCommand>("thanos.medicine", HandleDelete);
```

### Handler Distinguishes Command Type
```csharp
private async Task HandleCommandAsync<TCommand>(TCommand command)
{
    if (command is CreateMedicineCommand createCmd)
    {
     // Handle create
    }
    else if (command is UpdateMedicineCommand updateCmd)
    {
    // Handle update
    }
    else if (command is DeleteMedicineCommand deleteCmd)
    {
     // Handle delete
    }
}
```

## Publishing

### Automatic Routing
```csharp
// Publishes to thanos.medicine automatically
await _messageBus.SendAsync(new CreateMedicineCommand(...));
await _messageBus.SendAsync(new UpdateMedicineCommand(...));
await _messageBus.SendAsync(new DeleteMedicineCommand(...));
```

## RabbitMQ Topology

### Exchange
- Name: `medicine.events`
- Type: `direct`
- Durable: `true`

### Queues
```
thanos.medicine? All medicine commands
thanos.order       ? All order commands
thanos.product      ? All product commands
thanos.inventory.item   ? All inventory item commands
```

### Bindings
```
medicine.events ? thanos.medicine (routing key: thanos.medicine)
medicine.events ? thanos.order (routing key: thanos.order)
medicine.events ? thanos.product (routing key: thanos.product)
```

## Migration from Previous Format

### Old Queue Names (Delete These)
```
thanos.medicine.created.queue
thanos.medicine.updated.queue
thanos.medicine.deleted.queue
```

### New Queue Name (Use This)
```
thanos.medicine
```

### Steps to Migrate
1. Stop all consumers
2. Delete old queues from RabbitMQ Management UI
3. Deploy new code
4. Start consumers
5. New queue `thanos.medicine` will be created automatically

## Advantages Over Previous Format

### ? Less Queue Management
- 1 queue instead of 3+ per entity
- Fewer resources consumed
- Simpler monitoring

### ? Easier Scaling
- Scale consumers per entity, not per action
- Single consumer handles all operations
- Better resource utilization

### ? Cleaner Architecture
- Logical grouping by entity
- Matches domain boundaries
- Easier to understand

### ? Flexibility
- Easy to add new command types
- No need to create new queues
- Consumer logic handles routing

## When to Use Separate Queues

Consider separate queues if you need:

### Different Processing Requirements
```
thanos.medicine.high-priority
thanos.medicine.low-priority
```

### Different Consumers
```
thanos.medicine.create  ? Fast creation service
thanos.medicine.update    ? Slow update service with validation
thanos.medicine.delete    ? Audit-heavy deletion service
```

### Different Scaling Needs
```
thanos.medicine.bulk      ? Batch processing consumer
thanos.medicine.realtime  ? Real-time processing consumer
```

## Summary

? **New Format**: `thanos.{entity}`  
? **Example**: `CreateMedicineCommand` ? `thanos.medicine`  
? **Single Queue**: All medicine operations ? `thanos.medicine`  
? **Simpler**: Less queues, easier management  
? **Flexible**: Easy to extend  

**All command types for an entity now go to one queue!** ??
