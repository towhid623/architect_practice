using SharedKernel.CQRS;
using SharedKernel.Queries.Medicine;
using SharedKernel.DTOs.Medicine;
using SharedKernel.Common;
using test_service.Services;

namespace test_service.Handlers.Medicine;

public class SearchMedicinesQueryHandler : IQueryHandler<SearchMedicinesQuery, Result<List<MedicineResponse>>>
{
    private readonly IMedicineService _medicineService;
    private readonly ILogger<SearchMedicinesQueryHandler> _logger;

    public SearchMedicinesQueryHandler(IMedicineService medicineService, ILogger<SearchMedicinesQueryHandler> logger)
    {
        _medicineService = medicineService;
        _logger = logger;
    }

    public async Task<Result<List<MedicineResponse>>> HandleAsync(SearchMedicinesQuery query, CancellationToken cancellationToken = default)
    {
        try
        {
            var medicines = await _medicineService.SearchMedicinesAsync(query.SearchTerm);

            var response = medicines.Select(m => new MedicineResponse(
                m.Id,
                m.Name,
                m.GenericName,
                m.Manufacturer,
                m.Description,
                m.DosageForm,
                m.Strength,
                m.Price,
                m.StockQuantity,
                m.RequiresPrescription,
                m.IsAvailable,
                m.ExpiryDate,
                m.Category,
                m.SideEffects,
                m.StorageInstructions,
                m.CreatedAt,
                m.UpdatedAt
            )).ToList();

            return Result.Success(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching medicines with term {SearchTerm}", query.SearchTerm);
            return Result.Failure<List<MedicineResponse>>("Failed to search medicines");
        }
    }
}
