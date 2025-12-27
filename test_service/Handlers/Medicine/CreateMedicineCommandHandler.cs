using SharedKernel.CQRS;
using SharedKernel.Commands.Medicine;
using SharedKernel.DTOs.Medicine;
using SharedKernel.Common;
using test_service.Services;

namespace test_service.Handlers.Medicine;

public class CreateMedicineCommandHandler : ICommandHandler<CreateMedicineCommand, Result<MedicineResponse>>
{
    private readonly IMedicineService _medicineService;
    private readonly ILogger<CreateMedicineCommandHandler> _logger;

    public CreateMedicineCommandHandler(IMedicineService medicineService, ILogger<CreateMedicineCommandHandler> logger)
    {
        _medicineService = medicineService;
        _logger = logger;
  }

    public async Task<Result<MedicineResponse>> HandleAsync(CreateMedicineCommand command, CancellationToken cancellationToken = default)
    {
        try
     {
   var medicine = new Models.Medicine
        {
                Name = command.Name,
      GenericName = command.GenericName,
   Manufacturer = command.Manufacturer,
         Description = command.Description,
     DosageForm = command.DosageForm,
   Strength = command.Strength,
           Price = command.Price,
        StockQuantity = command.StockQuantity,
   RequiresPrescription = command.RequiresPrescription,
    ExpiryDate = command.ExpiryDate,
    Category = command.Category,
      SideEffects = command.SideEffects ?? new List<string>(),
        StorageInstructions = command.StorageInstructions
         };

    var created = await _medicineService.CreateMedicineAsync(medicine);

            var response = new MedicineResponse(
         created.Id,
            created.Name,
          created.GenericName,
         created.Manufacturer,
 created.Description,
          created.DosageForm,
                created.Strength,
                created.Price,
       created.StockQuantity,
                created.RequiresPrescription,
 created.IsAvailable,
         created.ExpiryDate,
    created.Category,
     created.SideEffects,
    created.StorageInstructions,
       created.CreatedAt,
           created.UpdatedAt
      );

            return Result.Success(response);
        }
        catch (ArgumentException ex)
        {
      _logger.LogWarning(ex, "Validation error creating medicine");
            return Result.Failure<MedicineResponse>(ex.Message);
 }
        catch (Exception ex)
      {
       _logger.LogError(ex, "Error creating medicine");
            return Result.Failure<MedicineResponse>("Failed to create medicine");
        }
    }
}
