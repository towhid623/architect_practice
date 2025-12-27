using Microsoft.EntityFrameworkCore;
using test_service.Data;
using test_service.Models;

namespace test_service.Repositories;

public class MedicineRepository : IMedicineRepository
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<MedicineRepository> _logger;

    public MedicineRepository(ApplicationDbContext context, ILogger<MedicineRepository> logger)
  {
        _context = context;
        _logger = logger;
    }

    public async Task<IEnumerable<Medicine>> GetAllAsync()
    {
      try
        {
     return await _context.Medicines.ToListAsync();
        }
catch (Exception ex)
        {
    _logger.LogError(ex, "Error retrieving all medicines");
   throw;
        }
 }

    public async Task<Medicine?> GetByIdAsync(string id)
    {
        try
   {
  return await _context.Medicines.FirstOrDefaultAsync(m => m.Id == id);
        }
        catch (Exception ex)
        {
_logger.LogError(ex, "Error retrieving medicine with ID {MedicineId}", id);
            throw;
        }
    }

    public async Task<IEnumerable<Medicine>> SearchAsync(string searchTerm)
 {
        try
        {
       return await _context.Medicines
     .Where(m => m.Name.Contains(searchTerm) || 
  m.GenericName.Contains(searchTerm) ||
      m.Description.Contains(searchTerm))
  .ToListAsync();
   }
    catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching medicines with term {SearchTerm}", searchTerm);
            throw;
        }
    }

    public async Task<IEnumerable<Medicine>> GetByCategoryAsync(string category)
    {
  try
        {
    return await _context.Medicines
        .Where(m => m.Category == category)
    .ToListAsync();
        }
  catch (Exception ex)
        {
      _logger.LogError(ex, "Error retrieving medicines by category {Category}", category);
            throw;
        }
    }

    public async Task<IEnumerable<Medicine>> GetByManufacturerAsync(string manufacturer)
    {
        try
    {
      return await _context.Medicines
       .Where(m => m.Manufacturer == manufacturer)
            .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving medicines by manufacturer {Manufacturer}", manufacturer);
  throw;
   }
    }

 public async Task<Medicine> AddAsync(Medicine medicine)
    {
        try
    {
      _context.Medicines.Add(medicine);
    await _context.SaveChangesAsync();
            return medicine;
      }
        catch (Exception ex)
  {
            _logger.LogError(ex, "Error adding medicine {MedicineName}", medicine.Name);
   throw;
        }
    }

    public async Task<Medicine> UpdateAsync(Medicine medicine)
    {
        try
   {
            medicine.UpdatedAt = DateTime.UtcNow;
   await _context.SaveChangesAsync();
            return medicine;
        }
     catch (Exception ex)
      {
            _logger.LogError(ex, "Error updating medicine {MedicineId}", medicine.Id);
    throw;
  }
    }

    public async Task<bool> DeleteAsync(string id)
    {
        try
        {
            var medicine = await GetByIdAsync(id);
          if (medicine == null)
       return false;

   _context.Medicines.Remove(medicine);
       await _context.SaveChangesAsync();
        return true;
 }
     catch (Exception ex)
        {
          _logger.LogError(ex, "Error deleting medicine {MedicineId}", id);
    throw;
        }
    }

    public async Task<bool> ExistsAsync(string id)
    {
   try
    {
         return await _context.Medicines.AnyAsync(m => m.Id == id);
      }
        catch (Exception ex)
   {
   _logger.LogError(ex, "Error checking medicine existence {MedicineId}", id);
            throw;
        }
    }
}
