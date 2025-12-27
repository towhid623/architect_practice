using SharedKernel.CQRS;
using SharedKernel.Common;
using SharedKernel.DTOs.Medicine;

namespace SharedKernel.Queries.Medicine;

public record GetMedicinesByManufacturerQuery(string Manufacturer) : IQuery<Result<List<MedicineResponse>>>;
