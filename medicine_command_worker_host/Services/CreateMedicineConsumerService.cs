using SharedKernel.Messaging;
using SharedKernel.Commands.Medicine;
using SharedKernel.CQRS;
using SharedKernel.Common;
using SharedKernel.DTOs.Medicine;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;

namespace medicine_command_worker_host.Services;

public class CreateMedicineConsumerService : BackgroundService
{
    private readonly IMessageBus _messageBus;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<CreateMedicineConsumerService> _logger;

    public CreateMedicineConsumerService(
        IMessageBus messageBus,
        IServiceProvider serviceProvider,
        ILogger<CreateMedicineConsumerService> logger)
    {
     _messageBus = messageBus;
        _serviceProvider = serviceProvider;
        _logger = logger;
}

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
     _logger.LogInformation("?? CreateMedicine Consumer Service is starting");

    try
 {
      // Ensure connection is established
     await _messageBus.ConnectAsync();

          // Subscribe to Medicine queue (handles all medicine commands)
       await _messageBus.SubscribeAsync<CreateMedicineCommand>("thanos.medicine", async (command) =>
    {
  _logger.LogInformation("?? Received CreateMedicineCommand for medicine: {Name}", command.Name);
  await HandleCommandAsync(command, stoppingToken);
   }, stoppingToken);

   _logger.LogInformation("? CreateMedicine Consumer Service is running and listening for commands");

   // Keep the service alive
 await Task.Delay(Timeout.Infinite, stoppingToken);
}
        catch (OperationCanceledException)
    {
          _logger.LogInformation("?? CreateMedicine Consumer Service is stopping");
        }
    catch (Exception ex)
     {
  _logger.LogError(ex, "?? Fatal error in CreateMedicine Consumer Service");
          throw;
    }
    }

    private async Task HandleCommandAsync(CreateMedicineCommand command, CancellationToken cancellationToken)
    {
     _logger.LogInformation("?? HandleCommandAsync called for CreateMedicineCommand");

    using var scope = _serviceProvider.CreateScope();

      try
        {
        _logger.LogInformation("?? Resolving CreateMedicineCommandHandler from DI...");
  var handler = scope.ServiceProvider
   .GetRequiredService<ICommandHandler<CreateMedicineCommand, Result<MedicineResponse>>>();

    _logger.LogInformation("? Handler resolved. Executing command...");
   var result = await handler.HandleAsync(command, cancellationToken);

            if (result.IsSuccess)
     _logger.LogInformation("? Successfully processed CreateMedicineCommand. Medicine ID: {Id}, Name: {Name}",
   result.Value?.Id, result.Value?.Name);
     else
                _logger.LogError("? Failed to process CreateMedicineCommand. Error: {Error}", result.Error);

       _logger.LogInformation("? HandleCommandAsync completed for CreateMedicineCommand");
        }
      catch (Exception ex)
        {
            _logger.LogError(ex, "?? Error handling CreateMedicineCommand");
   throw;
     }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("?? CreateMedicine Consumer Service is stopping");

        // Disconnect from RabbitMQ
        await _messageBus.DisconnectAsync();

    await base.StopAsync(cancellationToken);
    }
}
