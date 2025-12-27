using SharedKernel.CQRS;
using SharedKernel.Common;
using SharedKernel.DTOs.Medicine;

namespace SharedKernel.Commands.Medicine;

public record UpdateMedicineCommand(
    string Id,
    string? Name,
    string? GenericName,
    string? Manufacturer,
    string? Description,
    string? DosageForm,
    string? Strength,
    decimal? Price,
    int? StockQuantity,
    bool? RequiresPrescription,
    bool? IsAvailable,
    DateTime? ExpiryDate,
    string? Category,
    List<string>? SideEffects,
    string? StorageInstructions
) : ICommand<Result<MedicineResponse>>;
