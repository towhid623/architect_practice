using SharedKernel.CQRS;
using SharedKernel.Commands.Medicine;
using SharedKernel.DTOs.Medicine;
using SharedKernel.Common;
using test_service.Services;

namespace test_service.Handlers.Medicine;

public class UpdateMedicineCommandHandler : ICommandHandler<UpdateMedicineCommand, Result<MedicineResponse>>
{
    private readonly IMedicineService _medicineService;
    private readonly ILogger<UpdateMedicineCommandHandler> _logger;

    public UpdateMedicineCommandHandler(IMedicineService medicineService, ILogger<UpdateMedicineCommandHandler> logger)
    {
        _medicineService = medicineService;
        _logger = logger;
    }

    public async Task<Result<MedicineResponse>> HandleAsync(UpdateMedicineCommand command, CancellationToken cancellationToken = default)
    {
        try
        {
            var medicine = new Models.Medicine
            {
                Name = command.Name ?? string.Empty,
                GenericName = command.GenericName ?? string.Empty,
                Manufacturer = command.Manufacturer ?? string.Empty,
                Description = command.Description ?? string.Empty,
                DosageForm = command.DosageForm ?? string.Empty,
                Strength = command.Strength ?? string.Empty,
                Price = command.Price ?? 0,
                StockQuantity = command.StockQuantity ?? 0,
                RequiresPrescription = command.RequiresPrescription ?? false,
                IsAvailable = command.IsAvailable ?? true,
                ExpiryDate = command.ExpiryDate,
                Category = command.Category ?? string.Empty,
                SideEffects = command.SideEffects ?? new List<string>(),
                StorageInstructions = command.StorageInstructions ?? string.Empty
            };

            var updated = await _medicineService.UpdateMedicineAsync(command.Id, medicine);

            if (updated == null)
            {
                return Result.Failure<MedicineResponse>("Medicine not found");
            }

            var response = new MedicineResponse(
                updated.Id,
                updated.Name,
                updated.GenericName,
                updated.Manufacturer,
                updated.Description,
                updated.DosageForm,
                updated.Strength,
                updated.Price,
                updated.StockQuantity,
                updated.RequiresPrescription,
                updated.IsAvailable,
                updated.ExpiryDate,
                updated.Category,
                updated.SideEffects,
                updated.StorageInstructions,
                updated.CreatedAt,
                updated.UpdatedAt
            );

            return Result.Success(response);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Validation error updating medicine");
            return Result.Failure<MedicineResponse>(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating medicine {MedicineId}", command.Id);
            return Result.Failure<MedicineResponse>("Failed to update medicine");
        }
    }
}
