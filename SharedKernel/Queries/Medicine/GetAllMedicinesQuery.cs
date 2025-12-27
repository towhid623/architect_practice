using SharedKernel.CQRS;
using SharedKernel.Common;
using SharedKernel.DTOs.Medicine;

namespace SharedKernel.Queries.Medicine;

public record GetAllMedicinesQuery : IQuery<Result<List<MedicineResponse>>>;
