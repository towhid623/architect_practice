using medicine_command_worker_host.Domain;
using medicine_command_worker_host.Domain.Repositories;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;

namespace medicine_command_worker_host.Infrastructure.Repositories;

/// <summary>
/// MongoDB implementation of Medicine Repository for Aggregate Root
/// </summary>
public class MedicineAggregateRepository : IMedicineRepository
{
 private readonly IMongoCollection<MedicineAggregateRoot> _collection;
    private readonly ILogger<MedicineAggregateRepository> _logger;

    public MedicineAggregateRepository(
    IMongoClient mongoClient,
        ILogger<MedicineAggregateRepository> logger)
    {
        if (mongoClient == null)
      throw new ArgumentNullException(nameof(mongoClient));
  
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

 // Get database and collection directly from MongoDB client
 var database = mongoClient.GetDatabase("test_service_db");
        _collection = database.GetCollection<MedicineAggregateRoot>("medicines");
    }

    public async Task<MedicineAggregateRoot?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
    try
    {
   var filter = Builders<MedicineAggregateRoot>.Filter.Eq(m => m.Id, id);
 return await _collection.Find(filter).FirstOrDefaultAsync(cancellationToken);
   }
        catch (Exception ex)
        {
 _logger.LogError(ex, "Error getting medicine by ID: {Id}", id);
 throw;
        }
  }

    public async Task<MedicineAggregateRoot?> GetByNameAsync(string name, CancellationToken cancellationToken = default)
    {
   try
     {
   var filter = Builders<MedicineAggregateRoot>.Filter.Eq(m => m.Name, name);
 return await _collection.Find(filter).FirstOrDefaultAsync(cancellationToken);
      }
        catch (Exception ex)
        {
  _logger.LogError(ex, "Error getting medicine by name: {Name}", name);
      throw;
 }
    }

    public async Task<IEnumerable<MedicineAggregateRoot>> GetAllAsync(CancellationToken cancellationToken = default)
 {
 try
        {
     return await _collection.Find(_ => true).ToListAsync(cancellationToken);
     }
        catch (Exception ex)
  {
   _logger.LogError(ex, "Error getting all medicines");
    throw;
  }
    }

  public async Task<IEnumerable<MedicineAggregateRoot>> GetByCategoryAsync(string category, CancellationToken cancellationToken = default)
    {
        // Category not in simplified model, return empty
     return await Task.FromResult(Enumerable.Empty<MedicineAggregateRoot>());
    }

    public async Task<IEnumerable<MedicineAggregateRoot>> GetLowStockMedicinesAsync(CancellationToken cancellationToken = default)
    {
        try
   {
   var allMedicines = await GetAllAsync(cancellationToken);
      return allMedicines.Where(m => m.IsLowStock);
        }
     catch (Exception ex)
        {
    _logger.LogError(ex, "Error getting low stock medicines");
throw;
        }
    }

    public async Task<IEnumerable<MedicineAggregateRoot>> GetExpiredMedicinesAsync(CancellationToken cancellationToken = default)
 {
  // Expiry not in simplified model, return empty
        return await Task.FromResult(Enumerable.Empty<MedicineAggregateRoot>());
    }

    public async Task AddAsync(MedicineAggregateRoot medicine, CancellationToken cancellationToken = default)
    {
 try
  {
 await _collection.InsertOneAsync(medicine, cancellationToken: cancellationToken);
            _logger.LogInformation("Medicine added to repository: {Id}", medicine.Id);
   }
        catch (Exception ex)
        {
      _logger.LogError(ex, "Error adding medicine: {Name}", medicine.Name);
   throw;
        }
    }

    public async Task UpdateAsync(MedicineAggregateRoot medicine, CancellationToken cancellationToken = default)
    {
        try
 {
var filter = Builders<MedicineAggregateRoot>.Filter.Eq(m => m.Id, medicine.Id);
 await _collection.ReplaceOneAsync(filter, medicine, cancellationToken: cancellationToken);
    _logger.LogInformation("Medicine updated in repository: {Id}", medicine.Id);
        }
catch (Exception ex)
        {
  _logger.LogError(ex, "Error updating medicine: {Id}", medicine.Id);
    throw;
  }
    }

    public async Task DeleteAsync(string id, CancellationToken cancellationToken = default)
    {
  try
 {
    var filter = Builders<MedicineAggregateRoot>.Filter.Eq(m => m.Id, id);
 await _collection.DeleteOneAsync(filter, cancellationToken);
   _logger.LogInformation("Medicine deleted from repository: {Id}", id);
}
     catch (Exception ex)
        {
   _logger.LogError(ex, "Error deleting medicine: {Id}", id);
 throw;
 }
    }

    public async Task<bool> ExistsAsync(string name, CancellationToken cancellationToken = default)
    {
        try
 {
   var filter = Builders<MedicineAggregateRoot>.Filter.Eq(m => m.Name, name);
      var count = await _collection.CountDocumentsAsync(filter, cancellationToken: cancellationToken);
return count > 0;
 }
      catch (Exception ex)
        {
 _logger.LogError(ex, "Error checking if medicine exists: {Name}", name);
  throw;
        }
    }

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
   // MongoDB doesn't have a unit of work pattern like EF Core
     // Changes are already saved in Add/Update/Delete methods
        // Return 1 to indicate success
   return await Task.FromResult(1);
    }
}
