using SharedKernel.CQRS;
using SharedKernel.Commands.Medicine;
using SharedKernel.Common;
using test_service.Services;

namespace test_service.Handlers.Medicine;

public class DeleteMedicineCommandHandler : ICommandHandler<DeleteMedicineCommand, Result>
{
    private readonly IMedicineService _medicineService;
    private readonly ILogger<DeleteMedicineCommandHandler> _logger;

    public DeleteMedicineCommandHandler(IMedicineService medicineService, ILogger<DeleteMedicineCommandHandler> logger)
    {
        _medicineService = medicineService;
        _logger = logger;
    }

    public async Task<Result> HandleAsync(DeleteMedicineCommand command, CancellationToken cancellationToken = default)
    {
   try
    {
        var result = await _medicineService.DeleteMedicineAsync(command.Id);

  if (!result)
     {
   return Result.Failure("Medicine not found");
  }

   return Result.Success();
        }
        catch (Exception ex)
        {
_logger.LogError(ex, "Error deleting medicine {MedicineId}", command.Id);
     return Result.Failure("Failed to delete medicine");
        }
    }
}
