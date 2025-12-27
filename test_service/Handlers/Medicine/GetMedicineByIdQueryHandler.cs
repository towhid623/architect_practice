using SharedKernel.CQRS;
using SharedKernel.Queries.Medicine;
using SharedKernel.DTOs.Medicine;
using SharedKernel.Common;
using test_service.Services;

namespace test_service.Handlers.Medicine;

public class GetMedicineByIdQueryHandler : IQueryHandler<GetMedicineByIdQuery, Result<MedicineResponse>>
{
    private readonly IMedicineService _medicineService;
    private readonly ILogger<GetMedicineByIdQueryHandler> _logger;

    public GetMedicineByIdQueryHandler(IMedicineService medicineService, ILogger<GetMedicineByIdQueryHandler> logger)
    {
        _medicineService = medicineService;
        _logger = logger;
    }

    public async Task<Result<MedicineResponse>> HandleAsync(GetMedicineByIdQuery query, CancellationToken cancellationToken = default)
    {
        try
        {
    var medicine = await _medicineService.GetMedicineByIdAsync(query.Id);

  if (medicine == null)
       {
       return Result.Failure<MedicineResponse>("Medicine not found");
      }

    var response = new MedicineResponse(
    medicine.Id,
     medicine.Name,
      medicine.GenericName,
     medicine.Manufacturer,
    medicine.Description,
      medicine.DosageForm,
     medicine.Strength,
      medicine.Price,
   medicine.StockQuantity,
  medicine.RequiresPrescription,
medicine.IsAvailable,
     medicine.ExpiryDate,
        medicine.Category,
    medicine.SideEffects,
   medicine.StorageInstructions,
   medicine.CreatedAt,
      medicine.UpdatedAt
   );

 return Result.Success(response);
        }
        catch (Exception ex)
        {
       _logger.LogError(ex, "Error retrieving medicine {MedicineId}", query.Id);
   return Result.Failure<MedicineResponse>("Failed to retrieve medicine");
     }
    }
}
