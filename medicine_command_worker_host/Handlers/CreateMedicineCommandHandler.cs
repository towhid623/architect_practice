using SharedKernel.CQRS;
using SharedKernel.Commands.Medicine;
using SharedKernel.DTOs.Medicine;
using SharedKernel.Common;
using Microsoft.Extensions.Logging;
using medicine_command_worker_host.Domain.Repositories;

namespace medicine_command_worker_host.Handlers;

/// <summary>
/// Handler for CreateMedicineCommand using Domain Aggregate Root
/// </summary>
public class CreateMedicineCommandHandler : ICommandHandler<CreateMedicineCommand, Result<MedicineResponse>>
{
    private readonly IMedicineRepository _repository;
    private readonly ILogger<CreateMedicineCommandHandler> _logger;

    public CreateMedicineCommandHandler(
        IMedicineRepository repository,
    ILogger<CreateMedicineCommandHandler> logger)
    {
     _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<Result<MedicineResponse>> HandleAsync(
        CreateMedicineCommand command,
        CancellationToken cancellationToken = default)
    {
     try
        {
            _logger.LogInformation("?? Received CreateMedicineCommand: {Name}", command.Name);

      // Check if medicine already exists
  if (await _repository.ExistsAsync(command.Name, cancellationToken))
 {
    _logger.LogWarning("?? Medicine already exists: {Name}", command.Name);
   return Result.Failure<MedicineResponse>($"Medicine '{command.Name}' already exists");
            }

            // Pass command directly to aggregate root
        var medicineAggregate = Domain.MedicineAggregateRoot.CreateFromCommand(command);

            _logger.LogInformation(
                "? Medicine aggregate created. ID: {Id}, Name: {Name}, Events: {EventCount}",
             medicineAggregate.Id,
   medicineAggregate.Name,
            medicineAggregate.DomainEvents.Count);

            // Save to repository
            await _repository.AddAsync(medicineAggregate, cancellationToken);
            await _repository.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("?? Medicine saved to repository: {Id}", medicineAggregate.Id);

            // Log domain events
            foreach (var domainEvent in medicineAggregate.DomainEvents)
            {
   _logger.LogInformation("?? Domain event: {EventType} - {EventId}", 
           domainEvent.GetType().Name, 
         domainEvent.EventId);
     // TODO: Publish domain events to event bus
    // await _eventBus.PublishAsync(domainEvent, cancellationToken);
      }

    // Clear events after processing
   medicineAggregate.ClearDomainEvents();

        // Map aggregate to response DTO
         var response = MapToResponse(medicineAggregate);

            _logger.LogInformation("? CreateMedicineCommand completed successfully: {Id}", medicineAggregate.Id);

            return Result.Success(response);
        }
 catch (ArgumentException ex)
        {
  _logger.LogWarning(ex, "? Validation error: {Message}", ex.Message);
   return Result.Failure<MedicineResponse>(ex.Message);
    }
      catch (InvalidOperationException ex)
  {
            _logger.LogWarning(ex, "? Business rule violation: {Message}", ex.Message);
            return Result.Failure<MedicineResponse>(ex.Message);
        }
        catch (Exception ex)
        {
       _logger.LogError(ex, "?? Unexpected error creating medicine");
            return Result.Failure<MedicineResponse>("Failed to create medicine due to an unexpected error");
      }
    }

    /// <summary>
  /// Maps Medicine Aggregate Root to MedicineResponse DTO
    /// </summary>
private MedicineResponse MapToResponse(Domain.MedicineAggregateRoot aggregate)
    {
        return new MedicineResponse(
   Id: aggregate.Id,
            Name: aggregate.Name,
          GenericName: aggregate.GenericName,
            Manufacturer: aggregate.Manufacturer,
        Description: string.Empty, // Not in simplified model
  DosageForm: string.Empty,  // Not in simplified model
 Strength: aggregate.Strength?.ToString() ?? string.Empty,
 Price: aggregate.Price?.Amount ?? 0,
        StockQuantity: aggregate.Stock?.Available ?? 0,
        RequiresPrescription: false, // Not in simplified model
 IsAvailable: aggregate.IsAvailable,
            ExpiryDate: null, // Not in simplified model
            Category: string.Empty, // Not in simplified model
         SideEffects: new List<string>(), // Not in simplified model
      StorageInstructions: string.Empty, // Not in simplified model
            CreatedAt: aggregate.CreatedAt,
      UpdatedAt: aggregate.UpdatedAt
        );
    }
}
