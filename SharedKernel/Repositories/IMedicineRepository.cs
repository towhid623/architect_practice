using SharedKernel.Domain.Medicine;

namespace SharedKernel.Repositories;

/// <summary>
/// Repository interface for Medicine Aggregate Root
/// </summary>
public interface IMedicineRepository
{
    Task<MedicineAggregateRoot?> GetByIdAsync(string id, CancellationToken cancellationToken = default);
    Task<MedicineAggregateRoot?> GetByNameAsync(string name, CancellationToken cancellationToken = default);
    Task<IEnumerable<MedicineAggregateRoot>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<MedicineAggregateRoot>> GetLowStockMedicinesAsync(CancellationToken cancellationToken = default);
    Task AddAsync(MedicineAggregateRoot medicine, CancellationToken cancellationToken = default);
    Task UpdateAsync(MedicineAggregateRoot medicine, CancellationToken cancellationToken = default);
    Task DeleteAsync(string id, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(string name, CancellationToken cancellationToken = default);
  Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
