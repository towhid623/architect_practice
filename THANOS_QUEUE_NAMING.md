# THANOS QUEUE NAMING CONVENTION

## Overview
All RabbitMQ queues in this application use the **"thanos_"** prefix for namespace isolation and easy identification.

## Queue Naming Pattern

```
thanos_{command-type-in-kebab-case}
```

### Examples:
- `CreateMedicineCommand` ? `thanos_create-medicine-command`
- `UpdateMedicineCommand` ? `thanos_update-medicine-command`
- `MyCustomCommand` ? `thanos_my-custom-command`

## Current Queues

### Medicine Commands
| Command | Queue Name | Purpose |
|---------|------------|---------|
| CreateMedicineCommand | `thanos_create-medicine-command` | Create new medicine |
| UpdateMedicineCommand | `thanos_update-medicine-command` | Update existing medicine |
| DeleteMedicineCommand | `thanos_delete-medicine-command` | Delete medicine |

### Example Message Queues
| Message Type | Queue Name | Purpose |
|--------------|------------|---------|
| OrderMessage | `thanos_orders` | Process orders |
| NotificationMessage | `thanos_notifications` | Send notifications |

## Benefits of Thanos Prefix

### 1. Namespace Isolation
- All application queues grouped together
- Prevents naming conflicts with other applications
- Clear ownership and responsibility

### 2. Easy Identification
- Quick filtering in RabbitMQ Management UI
- Search for "thanos_*" to find all application queues
- Consistent naming across environments

### 3. Environment Separation
You can further extend this pattern:
- Development: `thanos_dev_create-medicine-command`
- Staging: `thanos_staging_create-medicine-command`
- Production: `thanos_create-medicine-command`

### 4. Monitoring and Alerting
- Set up alerts for all `thanos_*` queues
- Track metrics for application-specific queues
- Easier log filtering and debugging

## Implementation Details

### Automatic Queue Name Generation
The `RabbitMqMessageBus.GetQueueNameFromType()` method automatically adds the prefix:

```csharp
private const string QueuePrefix = "thanos_";

private static string GetQueueNameFromType(Type type)
{
    // Convert "CreateMedicineCommand" to "thanos_create-medicine-command"
    var name = type.Name;
    var result = new StringBuilder(QueuePrefix);

    for (int i = 0; i < name.Length; i++)
    {
      if (i > 0 && char.IsUpper(name[i]))
            result.Append('-');
        result.Append(char.ToLower(name[i]));
    }

    return result.ToString();
}
```

### Manual Queue Names
When using `PublishAsync` or `SubscribeAsync` directly, you should manually add the prefix:

```csharp
// Good - with prefix
await _messageBus.PublishAsync("thanos_custom-queue", message);

// Bad - without prefix (not following convention)
await _messageBus.PublishAsync("custom-queue", message);
```

### SendAsync Method
The `SendAsync` method automatically adds the prefix:

```csharp
// Automatically becomes: thanos_create-medicine-command
await _messageBus.SendAsync(new CreateMedicineCommand(...));
```

## Changing the Prefix

If you need to change the prefix (e.g., from "thanos_" to "myapp_"):

1. Update `RabbitMqMessageBus.cs`:
```csharp
private const string QueuePrefix = "myapp_";
```

2. Update all manual queue subscriptions in:
   - `CommandConsumerService.cs`
   - `MessageConsumerService.cs`
   - Any other places with hardcoded queue names

3. Update documentation

## RabbitMQ Management UI

### Viewing All Thanos Queues
1. Open RabbitMQ Management UI: `http://localhost:15672`
2. Go to "Queues" tab
3. Filter by "thanos_" to see all application queues

### Queue Metrics to Monitor
- **Messages Ready**: Number of messages waiting to be processed
- **Messages Unacknowledged**: Number of messages being processed
- **Message Rate**: Messages per second
- **Consumer Count**: Number of active consumers

### Typical Queue View
```
Name         Messages  Consumers
thanos_create-medicine-command    0         1
thanos_update-medicine-command    0         1
thanos_delete-medicine-command    0      1
thanos_orders       0         1
thanos_notifications        0         1
```

## Queue Configuration

All thanos queues are configured with:
- **Durable**: `true` (survive broker restart)
- **Exclusive**: `false` (can be accessed by multiple connections)
- **Auto-delete**: `false` (persist even when no consumers)
- **Arguments**: `null` (no special arguments)

## Dead Letter Queue (DLQ)

Consider creating a DLQ for failed messages:
```
thanos_dlq
```

Configure with:
- Capture messages that fail processing multiple times
- Manual inspection and reprocessing
- Alerting on DLQ depth

## Best Practices

### 1. Always Use Lowercase with Hyphens
? `thanos_create-medicine-command`  
? `thanos_CreateMedicineCommand`  
? `thanos_create_medicine_command`  

### 2. Use Descriptive Names
? `thanos_send-email-notification`  
? `thanos_email`  

### 3. Group Related Queues
```
thanos_order-created
thanos_order-updated
thanos_order-cancelled
```

### 4. Avoid Generic Names
? `thanos_medicine-inventory-updated`  
? `thanos_update`  

### 5. Document Custom Queues
Add any custom queues to this document with their purpose.

## Environment Variables

For different environments, you can configure the prefix via environment variable:

```csharp
private static readonly string QueuePrefix = 
    Environment.GetEnvironmentVariable("QUEUE_PREFIX") ?? "thanos_";
```

Then set in deployment:
- Development: `QUEUE_PREFIX=thanos_dev_`
- Staging: `QUEUE_PREFIX=thanos_staging_`
- Production: `QUEUE_PREFIX=thanos_`

## Summary

? All queues prefixed with **"thanos_"**  
? Automatic prefix for commands via `SendAsync`  
? Manual prefix required for custom queues  
? Consistent naming: lowercase + hyphens  
? Easy to identify and filter in RabbitMQ UI  
? Namespace isolation from other applications  
