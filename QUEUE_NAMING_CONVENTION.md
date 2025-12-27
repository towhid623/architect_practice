# ?? Queue Naming Convention: Dot Notation

## New Naming Standard

All RabbitMQ queues now follow the **dot notation** naming convention:

```
thanos.{entity}.{action}.queue
```

## Examples

### Command Queues

| Command Type | Queue Name |
|--------------|------------|
| CreateMedicineCommand | `thanos.medicine.created.queue` |
| UpdateMedicineCommand | `thanos.medicine.updated.queue` |
| DeleteMedicineCommand | `thanos.medicine.deleted.queue` |
| CreateOrderCommand | `thanos.order.created.queue` |
| UpdateOrderCommand | `thanos.order.updated.queue` |
| DeleteOrderCommand | `thanos.order.deleted.queue` |

### Query Queues (if needed)

| Query Type | Queue Name |
|------------|------------|
| GetMedicineByIdQuery | `thanos.medicine.query.queue` |
| SearchMedicinesQuery | `thanos.medicine.query.queue` |

### Generic Message Queues

| Message Type | Queue Name |
|--------------|------------|
| OrderMessage | `thanos.orders.queue` |
| NotificationMessage | `thanos.notifications.queue` |

## Pattern Breakdown

```
thanos.medicine.created.queue
?     ?        ?       ?
?     ?        ?   ?? Suffix: Always ".queue"
?     ?    ?? Action: created/updated/deleted/query
?   ?? Entity: medicine/order/product
?? Prefix: thanos (namespace)
```

## Naming Rules

### 1. Prefix
- Always starts with `thanos.`
- Identifies all queues belonging to this application

### 2. Entity
- Extracted from command/query name
- Example: `CreateMedicineCommand` ? `medicine`
- Example: `UpdateOrderCommand` ? `order`
- Converted to lowercase
- Words separated by dots

### 3. Action
- **For Commands:**
  - `Create*` ? `.created`
  - `Update*` ? `.updated`
  - `Delete*` ? `.deleted`
- **For Queries:**
  - `Get*`, `Search*` ? `.query`
- **For Messages:**
  - Generic ? no action, just entity

### 4. Suffix
- Always ends with `.queue`
- Clearly identifies as a queue resource

## Conversion Examples

### Command Conversions

```csharp
CreateMedicineCommand
  ? Remove "Command" ? CreateMedicine
  ? Extract entity ? Medicine
  ? Convert to lowercase ? medicine
  ? Detect action ? Create ? created
  ? Result: thanos.medicine.created.queue

UpdateProductCommand
  ? Remove "Command" ? UpdateProduct
  ? Extract entity ? Product
? Convert to lowercase ? product
  ? Detect action ? Update ? updated
  ? Result: thanos.product.updated.queue

DeleteOrderCommand
  ? Remove "Command" ? DeleteOrder
  ? Extract entity ? Order
  ? Convert to lowercase ? order
  ? Detect action ? Delete ? deleted
  ? Result: thanos.order.deleted.queue
```

### Multi-Word Entities

```csharp
CreateInventoryItemCommand
  ? Remove "Command" ? CreateInventoryItem
  ? Extract entity ? InventoryItem
  ? Convert to lowercase with dots ? inventory.item
  ? Detect action ? Create ? created
  ? Result: thanos.inventory.item.created.queue

UpdateShoppingCartCommand
  ? Remove "Command" ? UpdateShoppingCart
  ? Extract entity ? ShoppingCart
  ? Convert to lowercase with dots ? shopping.cart
  ? Detect action ? Update ? updated
  ? Result: thanos.shopping.cart.updated.queue
```

## Benefits

### ? Clear Hierarchy
```
thanos.
??? medicine.
?   ??? created.queue
???? updated.queue
?   ??? deleted.queue
??? order.
?   ??? created.queue
?   ??? updated.queue
?   ??? deleted.queue
??? product.
    ??? created.queue
    ??? updated.queue
    ??? deleted.queue
```

### ? Easy Filtering
- All medicine queues: `thanos.medicine.*`
- All created events: `thanos.*.created.queue`
- All queues: `thanos.*.queue`

### ? Self-Documenting
- Queue name tells you exactly what it contains
- `thanos.medicine.created.queue` = Medicine creation commands

### ? Routing Flexibility
Can use **topic exchanges** for pattern-based routing:
```
thanos.medicine.# ? Route all medicine operations
thanos.*.created.queue ? Route all creation events
thanos.#.queue ? Route all queues
```

### ? Microservice Boundaries
Clear separation by entity:
- Medicine Service ? `thanos.medicine.*`
- Order Service ? `thanos.order.*`
- Product Service ? `thanos.product.*`

## Implementation

### Automatic Queue Naming

The `RabbitMqMessageBus` automatically converts command names to queue names:

```csharp
// When you call:
await _messageBus.SendAsync(new CreateMedicineCommand(...));

// It automatically routes to:
// Queue: thanos.medicine.created.queue
```

### Manual Queue Naming

For custom queues, use the naming convention:

```csharp
await _messageBus.PublishAsync("thanos.custom.entity.queue", message);
await _messageBus.SubscribeAsync<MyMessage>("thanos.custom.entity.queue", handler);
```

## RabbitMQ Management UI

### Queue List View
```
thanos.medicine.created.queue     Messages: 0   Consumers: 1
thanos.medicine.updated.queue     Messages: 0   Consumers: 1
thanos.medicine.deleted.queue     Messages: 0   Consumers: 1
thanos.order.created.queue      Messages: 5   Consumers: 0
thanos.notifications.queue      Messages: 2   Consumers: 1
```

### Filtering in UI
- Filter by `thanos.medicine` to see all medicine queues
- Filter by `created.queue` to see all creation queues
- Filter by `thanos.` to see all application queues

## Migration from Old Format

### Old Format ? New Format

| Old Queue Name | New Queue Name |
|----------------|----------------|
| `thanos_create-medicine-command` | `thanos.medicine.created.queue` |
| `thanos_update-medicine-command` | `thanos.medicine.updated.queue` |
| `thanos_delete-medicine-command` | `thanos.medicine.deleted.queue` |
| `thanos_orders` | `thanos.orders.queue` |
| `thanos_notifications` | `thanos.notifications.queue` |

### Breaking Changes
?? **Important:** This is a breaking change!

- Old queues will not be automatically migrated
- Old messages in old queues will not be consumed
- You need to:
  1. Process or discard messages in old queues
  2. Delete old queues
  3. Deploy new version with updated queue names

## Environment-Specific Prefixes

For different environments, you can extend the prefix:

### Development
```
thanos.dev.medicine.created.queue
```

### Staging
```
thanos.staging.medicine.created.queue
```

### Production
```
thanos.medicine.created.queue
```

## Advanced: Topic Exchange Routing

With dot notation, you can use **topic exchanges** for advanced routing:

```csharp
// Exchange declaration
channel.ExchangeDeclare(
    exchange: "medicine.events",
    type: "topic",  // Changed from "direct"
    durable: true
);

// Binding patterns
channel.QueueBind(
    queue: "medicine-command-worker",
    exchange: "medicine.events",
routingKey: "thanos.medicine.*"  // Match all medicine events
);

channel.QueueBind(
    queue: "audit-service",
    exchange: "medicine.events",
    routingKey: "thanos.*.created.queue"  // Match all created events
);

channel.QueueBind(
    queue: "notification-service",
    exchange: "medicine.events",
    routingKey: "thanos.#"  // Match everything
);
```

### Routing Patterns

| Pattern | Matches |
|---------|---------|
| `thanos.medicine.*` | `thanos.medicine.created.queue`, `thanos.medicine.updated.queue` |
| `thanos.*.created.queue` | `thanos.medicine.created.queue`, `thanos.order.created.queue` |
| `thanos.medicine.#` | `thanos.medicine.created.queue`, `thanos.medicine.inventory.updated.queue` |
| `thanos.#` | All queues |

## Best Practices

### ? DO
- Use lowercase for all parts
- Use dots to separate words
- Keep entity names singular (`medicine`, not `medicines`)
- Use past tense for actions (`created`, not `create`)
- Always end with `.queue`

### ? DON'T
- Don't use underscores or hyphens
- Don't mix naming conventions
- Don't use abbreviations
- Don't use uppercase
- Don't omit the `.queue` suffix

## Summary

? New format: `thanos.{entity}.{action}.queue`  
? Example: `thanos.medicine.created.queue`  
? Automatic conversion from command names  
? Self-documenting and hierarchical  
? Easy filtering and routing  
? Topic exchange compatible  
? Microservice-friendly  

**All queue names now follow this consistent convention!** ??
