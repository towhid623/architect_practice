using medicine_command_worker_host.Domain;

namespace medicine_command_worker_host.Domain.Repositories;

/// <summary>
/// Repository interface for Medicine Aggregate Root
/// </summary>
public interface IMedicineRepository
{
    /// <summary>
/// Gets a medicine by its ID
    /// </summary>
    Task<MedicineAggregateRoot?> GetByIdAsync(string id, CancellationToken cancellationToken = default);

    /// <summary>
  /// Gets a medicine by its name
    /// </summary>
    Task<MedicineAggregateRoot?> GetByNameAsync(string name, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all medicines
 /// </summary>
    Task<IEnumerable<MedicineAggregateRoot>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets medicines by category
    /// </summary>
    Task<IEnumerable<MedicineAggregateRoot>> GetByCategoryAsync(string category, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets medicines that are low in stock
    /// </summary>
    Task<IEnumerable<MedicineAggregateRoot>> GetLowStockMedicinesAsync(CancellationToken cancellationToken = default);

    /// <summary>
 /// Gets expired medicines
    /// </summary>
    Task<IEnumerable<MedicineAggregateRoot>> GetExpiredMedicinesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a new medicine
    /// </summary>
    Task AddAsync(MedicineAggregateRoot medicine, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing medicine
    /// </summary>
 Task UpdateAsync(MedicineAggregateRoot medicine, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a medicine
    /// </summary>
    Task DeleteAsync(string id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a medicine with the given name already exists
    /// </summary>
    Task<bool> ExistsAsync(string name, CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves changes and publishes domain events
    /// </summary>
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
