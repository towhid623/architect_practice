# Medicine Command Worker Host

## Overview
This is a dedicated worker service that consumes `CreateMedicineCommand` messages from RabbitMQ and processes them.

## Architecture

```
RabbitMQ Queue: thanos_create-medicine-command
    ?
CreateMedicineConsumerService (Background Service)
    ?
CreateMedicineCommandHandler
    ?
MedicineService
    ?
MedicineRepository
    ?
MongoDB
```

## What It Does

1. **Listens** to the `thanos_create-medicine-command` RabbitMQ queue
2. **Receives** CreateMedicineCommand messages
3. **Processes** them using CreateMedicineCommandHandler
4. **Saves** medicines to MongoDB
5. **Acknowledges** messages back to RabbitMQ

## Configuration

### appsettings.json
```json
{
  "RabbitMq": {
 "ConnectionString": "amqps://user:pass@host/vhost"
  },
  "MongoDB": {
    "ConnectionString": "mongodb+srv://...",
    "DatabaseName": "test_service_db"
  }
}
```

## Components

### 1. CreateMedicineCommandHandler
- **Location**: `Handlers/CreateMedicineCommandHandler.cs`
- **Purpose**: Handles the business logic for creating medicines
- **Dependencies**: 
  - `IMedicineService` - Business logic
  - `ILogger` - Logging

### 2. CreateMedicineConsumerService
- **Location**: `Services/CreateMedicineConsumerService.cs`
- **Purpose**: Background service that consumes messages from RabbitMQ
- **Dependencies**:
  - `IMessageBus` - RabbitMQ connection
  - `IServiceProvider` - DI for resolving handlers
  - `ILogger` - Logging

### 3. Program.cs
- Sets up dependency injection
- Configures MongoDB
- Configures RabbitMQ
- Registers the background service
- Initializes connections

## How to Run

### Option 1: Visual Studio
1. Set `medicine_command_worker_host` as startup project
2. Press F5 or Ctrl+F5

### Option 2: Command Line
```bash
cd medicine_command_worker_host
dotnet run
```

### Option 3: Published Executable
```bash
dotnet publish -c Release
cd bin\Release\net9.0\publish
medicine_command_worker_host.exe
```

## Expected Output

### On Startup
```
? MongoDB database initialized successfully
? Successfully connected to RabbitMQ
?? Medicine Command Worker Host Starting...
?? Listening for CreateMedicineCommand on queue: thanos_create-medicine-command
Press Ctrl+C to stop

info: medicine_command_worker_host.Services.CreateMedicineConsumerService[0]
      ?? CreateMedicine Consumer Service is starting
info: Infrastructure.Messaging.RabbitMqMessageBus[0]
      Created dedicated consumer channel for queue thanos_create-medicine-command
info: Infrastructure.Messaging.RabbitMqMessageBus[0]
      ?? Exchange medicine.events declared for consumer
info: Infrastructure.Messaging.RabbitMqMessageBus[0]
      ?? Consumer: Queue thanos_create-medicine-command bound to exchange medicine.events
info: Infrastructure.Messaging.RabbitMqMessageBus[0]
      ? Consumer registered with tag amq.ctag-xxx for queue thanos_create-medicine-command
info: medicine_command_worker_host.Services.CreateMedicineConsumerService[0]
      ? CreateMedicine Consumer Service is running and listening for commands
```

### When Processing a Message
```
info: Infrastructure.Messaging.RabbitMqMessageBus[0]
      ?????? EVENT FIRED! Message received from queue thanos_create-medicine-command. DeliveryTag: 1
info: Infrastructure.Messaging.RabbitMqMessageBus[0]
      ?? Raw message size: 305 bytes from queue thanos_create-medicine-command
info: Infrastructure.Messaging.RabbitMqMessageBus[0]
      ?? Message JSON: {"Name":"Aspirin",...}
info: Infrastructure.Messaging.RabbitMqMessageBus[0]
      ? Deserialized to type CreateMedicineCommand. Calling handler...
info: medicine_command_worker_host.Services.CreateMedicineConsumerService[0]
  ?? Received CreateMedicineCommand for medicine: Aspirin
info: medicine_command_worker_host.Services.CreateMedicineConsumerService[0]
      ?? HandleCommandAsync called for CreateMedicineCommand
info: medicine_command_worker_host.Services.CreateMedicineConsumerService[0]
  ?? Resolving CreateMedicineCommandHandler from DI...
info: medicine_command_worker_host.Services.CreateMedicineConsumerService[0]
      ? Handler resolved. Executing command...
info: medicine_command_worker_host.Services.CreateMedicineConsumerService[0]
      ? Successfully processed CreateMedicineCommand. Medicine ID: 65abc123..., Name: Aspirin
info: medicine_command_worker_host.Services.CreateMedicineConsumerService[0]
      ? HandleCommandAsync completed for CreateMedicineCommand
info: Infrastructure.Messaging.RabbitMqMessageBus[0]
      ??? SUCCESS! Processed and ACK'd message DeliveryTag: 1 from queue thanos_create-medicine-command
```

## Testing the Worker

### Step 1: Start the Worker
```bash
dotnet run --project medicine_command_worker_host
```

### Step 2: Send a Command from test_service API
```bash
POST http://localhost:5035/api/medicine
Content-Type: application/json

{
  "name": "Test Medicine",
  "genericName": "Test Generic",
  "manufacturer": "Test Corp",
  "description": "Test from worker",
  "dosageForm": "Tablet",
  "strength": "100mg",
  "price": 9.99,
  "stockQuantity": 100,
  "requiresPrescription": false,
  "category": "Test",
  "storageInstructions": "Test"
}
```

### Step 3: Watch Worker Console
You should see the message being processed in real-time!

### Step 4: Verify in MongoDB
```bash
GET http://localhost:5035/api/medicine
```
You should see the newly created medicine.

## Deployment

### Windows Service
```bash
dotnet publish -c Release -r win-x64 --self-contained
sc create MedicineWorker binPath="C:\path\to\medicine_command_worker_host.exe"
sc start MedicineWorker
```

### Linux Systemd Service
```bash
dotnet publish -c Release -r linux-x64 --self-contained

# Create systemd service file
sudo nano /etc/systemd/system/medicine-worker.service

[Unit]
Description=Medicine Command Worker
After=network.target

[Service]
Type=notify
WorkingDirectory=/opt/medicine-worker
ExecStart=/opt/medicine-worker/medicine_command_worker_host
Restart=always

[Install]
WantedBy=multi-user.target

# Enable and start
sudo systemctl enable medicine-worker
sudo systemctl start medicine-worker
```

### Docker
```dockerfile
FROM mcr.microsoft.com/dotnet/runtime:9.0
WORKDIR /app
COPY bin/Release/net9.0/publish/ .
ENTRYPOINT ["dotnet", "medicine_command_worker_host.dll"]
```

## Monitoring

### Health Checks
- Monitor RabbitMQ connection status
- Monitor MongoDB connection status
- Track message processing rate
- Track error rate

### Logs
- All logs go to console
- Use logging aggregation (Seq, ELK, etc.)
- Monitor for errors and warnings

## Scaling

### Multiple Instances
You can run multiple instances of this worker:
- Each instance will consume from the same queue
- RabbitMQ will distribute messages evenly (round-robin)
- Increases throughput

### Example: 3 Workers
```
Worker 1 ? Message 1, 4, 7, 10...
Worker 2 ? Message 2, 5, 8, 11...
Worker 3 ? Message 3, 6, 9, 12...
```

## Advantages of Separate Worker

### ? Separation of Concerns
- API handles HTTP requests
- Worker handles message processing
- Clear responsibility boundaries

### ? Independent Scaling
- Scale API independently (more HTTP traffic)
- Scale workers independently (more messages)

### ? Independent Deployment
- Deploy API without affecting workers
- Deploy workers without affecting API
- Zero-downtime updates

### ? Fault Isolation
- If worker crashes, API still works
- If API crashes, workers keep processing
- Better resilience

### ? Resource Optimization
- API optimized for HTTP
- Worker optimized for background processing
- Different resource profiles

## Troubleshooting

### Worker Not Receiving Messages

**Check:**
1. RabbitMQ connection successful?
2. Queue `thanos_create-medicine-command` exists?
3. Exchange `medicine.events` exists?
4. Queue bound to exchange?
5. test_service publishing successfully?

**Solution:**
```bash
# Check RabbitMQ Management UI
http://localhost:15672

# Check queue
Queues ? thanos_create-medicine-command

# Check bindings
Should show binding to medicine.events
```

### MongoDB Connection Failed

**Check:**
1. Connection string correct in appsettings.json?
2. MongoDB accessible from worker machine?
3. Network firewall rules?

### Handler Errors

**Check:**
1. All dependencies registered in Program.cs?
2. MedicineService and Repository working?
3. Check error logs for specific issues

## Project Dependencies

```
medicine_command_worker_host
??? SharedKernel (Commands, Queries, DTOs, Interfaces)
??? Infrastructure (RabbitMQ, MongoDB implementations)
??? test_service (Services, Repositories, Models)
```

## Future Enhancements

1. **Add More Handlers**
 - UpdateMedicineCommandHandler
   - DeleteMedicineCommandHandler

2. **Add Retry Logic**
   - Automatic retry on failure
   - Dead letter queue for failed messages

3. **Add Metrics**
   - Processing time
   - Success/failure rates
   - Queue depth monitoring

4. **Add Health Checks**
   - HTTP endpoint for health status
   - Kubernetes liveness/readiness probes

## Summary

? **Dedicated worker** for processing CreateMedicineCommand  
? **Independent deployment** from main API  
? **Scalable** - can run multiple instances  
? **Resilient** - fault isolation from API  
? **Production-ready** - proper logging, error handling  

This worker completes your **distributed CQRS architecture**! ??
