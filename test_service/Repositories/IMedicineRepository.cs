using test_service.Models;

namespace test_service.Repositories;

public interface IMedicineRepository
{
    Task<IEnumerable<Medicine>> GetAllAsync();
    Task<Medicine?> GetByIdAsync(string id);
    Task<IEnumerable<Medicine>> SearchAsync(string searchTerm);
    Task<IEnumerable<Medicine>> GetByCategoryAsync(string category);
    Task<IEnumerable<Medicine>> GetByManufacturerAsync(string manufacturer);
 Task<Medicine> AddAsync(Medicine medicine);
    Task<Medicine> UpdateAsync(Medicine medicine);
    Task<bool> DeleteAsync(string id);
    Task<bool> ExistsAsync(string id);
}
