using test_service.Models;

namespace test_service.Services;

public interface IMedicineService
{
    Task<IEnumerable<Medicine>> GetAllMedicinesAsync();
    Task<Medicine?> GetMedicineByIdAsync(string id);
  Task<IEnumerable<Medicine>> SearchMedicinesAsync(string searchTerm);
    Task<IEnumerable<Medicine>> GetMedicinesByCategoryAsync(string category);
    Task<IEnumerable<Medicine>> GetMedicinesByManufacturerAsync(string manufacturer);
    Task<Medicine> CreateMedicineAsync(Medicine medicine);
    Task<Medicine?> UpdateMedicineAsync(string id, Medicine medicine);
    Task<bool> DeleteMedicineAsync(string id);
}
