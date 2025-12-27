using SharedKernel.Domain.Medicine;
using SharedKernel.Repositories;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;

namespace Infrastructure.Repositories;

/// <summary>
/// MongoDB implementation of Medicine Repository
/// </summary>
public class MedicineRepository : IMedicineRepository
{
    private readonly IMongoCollection<MedicineAggregateRoot> _collection;
    private readonly ILogger<MedicineRepository> _logger;

    public MedicineRepository(
      IMongoClient mongoClient,
        ILogger<MedicineRepository> logger)
    {
     if (mongoClient == null)
      throw new ArgumentNullException(nameof(mongoClient));

 _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        var database = mongoClient.GetDatabase("test_service_db");
  _collection = database.GetCollection<MedicineAggregateRoot>("medicines");
    }

    public async Task<MedicineAggregateRoot?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
   try
   {
  var filter = Builders<MedicineAggregateRoot>.Filter.Eq("_id", id);
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
   var filter = Builders<MedicineAggregateRoot>.Filter.Eq("name", name);
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

    public async Task<IEnumerable<MedicineAggregateRoot>> GetLowStockMedicinesAsync(CancellationToken cancellationToken = default)
    {
      try
  {
   var filter = Builders<MedicineAggregateRoot>.Filter.Lte("stock_quantity", 10);
      return await _collection.Find(filter).ToListAsync(cancellationToken);
        }
        catch (Exception ex)
     {
            _logger.LogError(ex, "Error getting low stock medicines");
   throw;
        }
    }

    public async Task AddAsync(MedicineAggregateRoot medicine, CancellationToken cancellationToken = default)
    {
   try
        {
       await _collection.InsertOneAsync(medicine, cancellationToken: cancellationToken);
    _logger.LogInformation("Medicine added: {Id}", medicine.Id);
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
     var filter = Builders<MedicineAggregateRoot>.Filter.Eq("_id", medicine.Id);
    await _collection.ReplaceOneAsync(filter, medicine, cancellationToken: cancellationToken);
          _logger.LogInformation("Medicine updated: {Id}", medicine.Id);
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
   var filter = Builders<MedicineAggregateRoot>.Filter.Eq("_id", id);
    await _collection.DeleteOneAsync(filter, cancellationToken);
        _logger.LogInformation("Medicine deleted: {Id}", id);
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
       // Use field name directly instead of expression
 var filter = Builders<MedicineAggregateRoot>.Filter.Eq("name", name);
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
        // MongoDB doesn't have unit of work pattern
        // Changes are saved immediately in Add/Update/Delete methods
    return await Task.FromResult(1);
    }
}
