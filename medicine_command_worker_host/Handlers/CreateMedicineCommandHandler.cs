using SharedKernel.CQRS;
using SharedKernel.Commands.Medicine;
using SharedKernel.DTOs.Medicine;
using SharedKernel.Common;
using SharedKernel.Repositories;
using SharedKernel.Domain.Medicine;
using SharedKernel.Messaging;
using Microsoft.Extensions.Logging;

namespace medicine_command_worker_host.Handlers;

/// <summary>
/// Handler for CreateMedicineCommand using Domain Aggregate Root
/// </summary>
public class CreateMedicineCommandHandler : ICommandHandler<CreateMedicineCommand, Result<MedicineResponse>>
{
    private readonly IMedicineRepository _repository;
    private readonly IDomainEventPublisher _eventPublisher;
    private readonly ILogger<CreateMedicineCommandHandler> _logger;

    public CreateMedicineCommandHandler(
 IMedicineRepository repository,
        IDomainEventPublisher eventPublisher,
        ILogger<CreateMedicineCommandHandler> logger)
    {
 _repository = repository ?? throw new ArgumentNullException(nameof(repository));
  _eventPublisher = eventPublisher ?? throw new ArgumentNullException(nameof(eventPublisher));
      _logger = logger ?? throw new ArgumentNullException(nameof(logger));
 }

public async Task<Result<MedicineResponse>> HandleAsync(
        CreateMedicineCommand command,
 CancellationToken cancellationToken = default)
    {
        try
  {
            _logger.LogInformation("?? Received CreateMedicineCommand: {Name}", command.Name);

            // Check uniqueness
 if (await _repository.ExistsAsync(command.Name, cancellationToken))
            {
     _logger.LogWarning("?? Medicine already exists: {Name}", command.Name);
   return Result.Failure<MedicineResponse>($"Medicine '{command.Name}' already exists");
            }

 // Create aggregate from command
    var medicineAggregate = MedicineAggregateRoot.CreateFromCommand(command);

     _logger.LogInformation(
  "? Aggregate created. ID: {Id}, Events: {EventCount}",
       medicineAggregate.Id,
medicineAggregate.DomainEvents.Count);

       // Save to repository
   await _repository.AddAsync(medicineAggregate, cancellationToken);
     await _repository.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("?? Medicine saved: {Id}", medicineAggregate.Id);

            // Publish domain events to fanout exchange
            if (medicineAggregate.DomainEvents.Any())
       {
 _logger.LogInformation(
         "?? Publishing {Count} domain events to fanout exchange",
       medicineAggregate.DomainEvents.Count);

        await _eventPublisher.PublishManyAsync(
           medicineAggregate.DomainEvents,
      cancellationToken);

    medicineAggregate.ClearDomainEvents();

 _logger.LogInformation("? Domain events published and cleared");
    }

   var response = MapToResponse(medicineAggregate);

  _logger.LogInformation("? Command completed: {Id}", medicineAggregate.Id);

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
   _logger.LogError(ex, "?? Unexpected error");
  return Result.Failure<MedicineResponse>("Failed to create medicine");
     }
  }

    private MedicineResponse MapToResponse(MedicineAggregateRoot aggregate)
  {
        return new MedicineResponse(
            Id: aggregate.Id,
Name: aggregate.Name,
   GenericName: aggregate.GenericName,
       Manufacturer: aggregate.Manufacturer,
       Description: string.Empty,
     DosageForm: string.Empty,
        Strength: aggregate.Strength?.ToString() ?? string.Empty,
       Price: aggregate.Price?.Amount ?? 0,
     StockQuantity: aggregate.Stock?.Available ?? 0,
  RequiresPrescription: false,
  IsAvailable: aggregate.IsAvailable,
            ExpiryDate: null,
       Category: string.Empty,
       SideEffects: new List<string>(),
        StorageInstructions: string.Empty,
        CreatedAt: aggregate.CreatedAt,
    UpdatedAt: aggregate.UpdatedAt
    );
  }
}
