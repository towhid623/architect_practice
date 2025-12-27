using test_service.Models;
using test_service.Repositories;

namespace test_service.Services;

public class MedicineService : IMedicineService
{
    private readonly IMedicineRepository _repository;
    private readonly ILogger<MedicineService> _logger;

  public MedicineService(IMedicineRepository repository, ILogger<MedicineService> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<IEnumerable<Medicine>> GetAllMedicinesAsync()
    {
   try
  {
        return await _repository.GetAllAsync();
   }
   catch (Exception ex)
    {
       _logger.LogError(ex, "Service error: Failed to get all medicines");
     throw;
        }
    }

    public async Task<Medicine?> GetMedicineByIdAsync(string id)
    {
        try
     {
    return await _repository.GetByIdAsync(id);
        }
        catch (Exception ex)
  {
     _logger.LogError(ex, "Service error: Failed to get medicine by ID {MedicineId}", id);
   throw;
        }
    }

    public async Task<IEnumerable<Medicine>> SearchMedicinesAsync(string searchTerm)
    {
        try
   {
  if (string.IsNullOrWhiteSpace(searchTerm))
            {
       return await _repository.GetAllAsync();
   }

            return await _repository.SearchAsync(searchTerm);
        }
     catch (Exception ex)
      {
 _logger.LogError(ex, "Service error: Failed to search medicines with term {SearchTerm}", searchTerm);
   throw;
    }
    }

    public async Task<IEnumerable<Medicine>> GetMedicinesByCategoryAsync(string category)
    {
        try
   {
            return await _repository.GetByCategoryAsync(category);
        }
     catch (Exception ex)
 {
       _logger.LogError(ex, "Service error: Failed to get medicines by category {Category}", category);
            throw;
   }
    }

    public async Task<IEnumerable<Medicine>> GetMedicinesByManufacturerAsync(string manufacturer)
    {
    try
        {
     return await _repository.GetByManufacturerAsync(manufacturer);
        }
        catch (Exception ex)
  {
    _logger.LogError(ex, "Service error: Failed to get medicines by manufacturer {Manufacturer}", manufacturer);
      throw;
 }
    }

    public async Task<Medicine> CreateMedicineAsync(Medicine medicine)
    {
        try
        {
            // Business logic validations
if (string.IsNullOrWhiteSpace(medicine.Name))
       {
     throw new ArgumentException("Medicine name is required");
          }

         if (medicine.Price < 0)
   {
       throw new ArgumentException("Price cannot be negative");
   }

            if (medicine.StockQuantity < 0)
            {
    throw new ArgumentException("Stock quantity cannot be negative");
 }

            return await _repository.AddAsync(medicine);
    }
        catch (Exception ex)
      {
        _logger.LogError(ex, "Service error: Failed to create medicine {MedicineName}", medicine.Name);
            throw;
        }
    }

    public async Task<Medicine?> UpdateMedicineAsync(string id, Medicine medicine)
    {
  try
     {
       var existing = await _repository.GetByIdAsync(id);
   if (existing == null)
    {
     return null;
     }

            // Update properties
       if (!string.IsNullOrWhiteSpace(medicine.Name))
    existing.Name = medicine.Name;

      if (!string.IsNullOrWhiteSpace(medicine.GenericName))
   existing.GenericName = medicine.GenericName;

       if (!string.IsNullOrWhiteSpace(medicine.Manufacturer))
     existing.Manufacturer = medicine.Manufacturer;

   if (!string.IsNullOrWhiteSpace(medicine.Description))
       existing.Description = medicine.Description;

  if (!string.IsNullOrWhiteSpace(medicine.DosageForm))
       existing.DosageForm = medicine.DosageForm;

            if (!string.IsNullOrWhiteSpace(medicine.Strength))
   existing.Strength = medicine.Strength;

         if (medicine.Price >= 0)
   existing.Price = medicine.Price;

            if (medicine.StockQuantity >= 0)
    existing.StockQuantity = medicine.StockQuantity;

   existing.RequiresPrescription = medicine.RequiresPrescription;
            existing.IsAvailable = medicine.IsAvailable;

        if (medicine.ExpiryDate.HasValue)
       existing.ExpiryDate = medicine.ExpiryDate;

            if (!string.IsNullOrWhiteSpace(medicine.Category))
       existing.Category = medicine.Category;

  if (medicine.SideEffects != null && medicine.SideEffects.Any())
          existing.SideEffects = medicine.SideEffects;

   if (!string.IsNullOrWhiteSpace(medicine.StorageInstructions))
   existing.StorageInstructions = medicine.StorageInstructions;

          return await _repository.UpdateAsync(existing);
      }
        catch (Exception ex)
        {
        _logger.LogError(ex, "Service error: Failed to update medicine {MedicineId}", id);
          throw;
        }
    }

    public async Task<bool> DeleteMedicineAsync(string id)
    {
    try
        {
    return await _repository.DeleteAsync(id);
        }
        catch (Exception ex)
     {
   _logger.LogError(ex, "Service error: Failed to delete medicine {MedicineId}", id);
  throw;
      }
    }
}
