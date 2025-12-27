using SharedKernel.CQRS;
using SharedKernel.Common;
using SharedKernel.DTOs.Medicine;

namespace SharedKernel.Queries.Medicine;

public record SearchMedicinesQuery(string SearchTerm) : IQuery<Result<List<MedicineResponse>>>;
