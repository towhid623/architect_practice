# VERIFYING MESSAGE CONSUMPTION

## Current Issue
- Messages are published successfully (ConsumerCount: 1)
- But consumer event handler might not be firing
- Queue shows 0 messages (could mean instant processing OR no processing)

## Enhanced Logging Added

The updated RabbitMqMessageBus now logs:
```
? "Raw message received from queue {QueueName}. Size: {Size} bytes"
? "Message JSON: {Json}" (Debug level)
? "Message deserialized successfully. Calling handler..."
? "Processed and acknowledged message from queue {QueueName}"
```

## Test Steps

### Step 1: Restart Application
```bash
# Stop current app (Ctrl+C)
cd test_service
dotnet run
```

### Step 2: Wait for Startup Logs
You should see:
```
? Successfully connected to RabbitMQ
? Subscribed to queue thanos_create-medicine-command and waiting for messages
? Subscribed to queue thanos_update-medicine-command and waiting for messages
? Subscribed to queue thanos_delete-medicine-command and waiting for messages
? Command Consumer Service is running and listening for commands
```

### Step 3: Send Test Message
```bash
POST http://localhost:5000/api/medicine
Content-Type: application/json

{
  "name": "Test Aspirin DEBUG",
  "genericName": "Acetylsalicylic Acid",
  "manufacturer": "Test Pharma",
  "description": "Pain reliever for testing",
  "dosageForm": "Tablet",
  "strength": "500mg",
  "price": 9.99,
  "stockQuantity": 100,
  "requiresPrescription": false,
  "category": "Pain Relief",
  "storageInstructions": "Store at room temperature"
}
```

### Step 4: Check Logs - You Should See

#### Publishing Side:
```
info: Infrastructure.Messaging.RabbitMqMessageBus[0]
    Sending command CreateMedicineCommand to queue thanos_create-medicine-command
info: Infrastructure.Messaging.RabbitMqMessageBus[0]
      Publishing message to queue thanos_create-medicine-command
info: Infrastructure.Messaging.RabbitMqMessageBus[0]
Queue thanos_create-medicine-command declared. MessageCount: 0, ConsumerCount: 1
info: Infrastructure.Messaging.RabbitMqMessageBus[0]
      Message published to queue thanos_create-medicine-command. Message is persistent and durable.
```

#### **NEW - Consuming Side (Should appear within milliseconds):**
```
info: Infrastructure.Messaging.RabbitMqMessageBus[0]
      Raw message received from queue thanos_create-medicine-command. Size: 523 bytes
dbug: Infrastructure.Messaging.RabbitMqMessageBus[0]
      Message JSON: {"Name":"Test Aspirin DEBUG",...}
info: Infrastructure.Messaging.RabbitMqMessageBus[0]
      Message deserialized successfully. Calling handler for queue thanos_create-medicine-command
info: test_service.BackgroundServices.CommandConsumerService[0]
      Received CreateMedicineCommand for medicine: Test Aspirin DEBUG
info: test_service.BackgroundServices.CommandConsumerService[0]
      Successfully processed CreateMedicineCommand. Medicine ID: 65abc123...
info: Infrastructure.Messaging.RabbitMqMessageBus[0]
      Processed and acknowledged message from queue thanos_create-medicine-command
```

## Expected vs Actual Behavior

### ? If Everything Works (You Should See This):
```
1. "Message published to queue..."
2. "Raw message received from queue..." ? NEW LOG
3. "Message deserialized successfully..."  ? NEW LOG
4. "Received CreateMedicineCommand for medicine: Test Aspirin DEBUG"
5. "Successfully processed CreateMedicineCommand. Medicine ID: ..."
6. "Processed and acknowledged message..." ? NEW LOG
```

### ? If Consumer Not Receiving (Current Issue):
```
1. "Message published to queue..."
2. "Successfully published message..."
3. (NOTHING ELSE - No "Raw message received" log)
```

### ?? If Deserialization Fails:
```
1. "Message published to queue..."
2. "Raw message received from queue..."
3. "Failed to deserialize message from queue..."
```

## Troubleshooting

### Issue 1: No "Raw message received" Log

**Meaning:** Consumer subscribed but event handler not firing

**Possible Causes:**
1. RabbitMQ connection dropped after subscribe
2. Channel closed
3. Consumer not actually registered

**Check RabbitMQ Management UI:**
- Queue: `thanos_create-medicine-command`
- Check "Consumers" tab
- Should show: `test_service` or your app name
- Should show: `ack mode: manual`

### Issue 2: "Raw message received" but No Handler Call

**Meaning:** Deserialization failing

**Check:**
- Debug log for JSON content
- Verify command structure matches CreateMedicineCommand

### Issue 3: Messages Accumulating in Queue

**Meaning:** Consumer receiving but handler failing

**Check:**
- Error logs in HandleCommandAsync
- Handler dependency resolution errors
- MongoDB connection issues

## Manual Queue Test

### Test 1: Check Queue via RabbitMQ Management UI

1. Go to queue: `thanos_create-medicine-command`
2. Section: **Get messages**
3. Settings:
   - Messages: 1
   - Ack mode: **Nack message requeue true**
4. Click **Get Message(s)**

**Expected:** See your JSON message payload
**If empty:** Messages are being consumed (check logs for processing)
**If you see message:** Consumer not picking up messages

### Test 2: Manually Publish via RabbitMQ UI

1. Go to queue: `thanos_create-medicine-command`
2. Section: **Publish message**
3. Payload:
```json
{
  "Name": "Manual Test Medicine",
  "GenericName": "Test Generic",
  "Manufacturer": "Test Corp",
  "Description": "Manually published",
  "DosageForm": "Tablet",
  "Strength": "100mg",
  "Price": 10.00,
  "StockQuantity": 50,
  "RequiresPrescription": false,
  "ExpiryDate": null,
  "Category": "Test",
  "SideEffects": [],
  "StorageInstructions": "Test storage"
}
```
4. Properties:
   - delivery_mode: 2 (persistent)
   - content_type: application/json
5. Click **Publish message**

**Expected Logs:**
```
info: Infrastructure.Messaging.RabbitMqMessageBus[0]
      Raw message received from queue thanos_create-medicine-command. Size: XXX bytes
```

**If you see this:** Consumer IS working, but something wrong with SendAsync
**If you don't see this:** Consumer NOT receiving messages

### Test 3: Check MongoDB

Even if logs are missing, check if data is being saved:

```javascript
// In MongoDB Compass or mongosh
use test_service_db
db.medicines.find().sort({CreatedAt: -1}).limit(5)
```

**If you see recent medicines:** Processing is working, just logs missing
**If no medicines:** Processing not working

## Connection Issues Check

### Check RabbitMQ Connection Health

In logs, look for:
```
? "RabbitMQ connection established. Connection: True, Channel: True"
```

If you see:
```
? "RabbitMQ connection established. Connection: False, Channel: False"
```

Connection is broken - check:
1. CloudAMQP dashboard
2. Connection limits
3. Network connectivity

## Advanced Debugging

### Enable Verbose Logging

In `appsettings.json`:
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Debug",
      "Microsoft.AspNetCore": "Warning",
      "Infrastructure.Messaging": "Debug"
    }
  }
}
```

This will show:
- Debug log with full JSON message
- More detailed RabbitMQ operations

### Test with Sync Endpoint

To rule out RabbitMQ issues, test the sync endpoint:

```bash
POST http://localhost:5000/api/medicine/sync
Content-Type: application/json

{
  "name": "Direct Test Medicine",
  "genericName": "Test",
  "manufacturer": "Test Corp",
  "description": "Direct handler test",
  "dosageForm": "Tablet",
  "strength": "100mg",
  "price": 10.00,
  "stockQuantity": 100,
  "requiresPrescription": false,
  "category": "Test",
  "storageInstructions": "Test"
}
```

**If this works:** Issue is with RabbitMQ/Consumer
**If this fails:** Issue is with Handler/Service/Repository

## What to Report

If issue persists after restart, please provide:

1. **Full application logs** from startup through sending message
2. **Look specifically for:**
   - ? "Raw message received..." (If missing = consumer not receiving)
   - ? "Received CreateMedicineCommand..." (If missing = handler not called)
   - ? "Successfully processed..." (If missing = handler failed)
3. **RabbitMQ Management UI screenshot** of queue showing:
 - Consumers tab
   - Messages section
4. **MongoDB check** - Do medicines get created?

## Expected Timeline

With consumer working properly:
```
00:00.000 - POST /api/medicine
00:00.050 - Message published to queue
00:00.051 - Raw message received by consumer
00:00.052 - Handler called
00:00.150 - MongoDB save complete
00:00.151 - Message acknowledged
00:00.152 - HTTP 202 Accepted returned
```

**Total time: ~150ms**

That's why queue shows 0 - everything happens in under 200 milliseconds!
