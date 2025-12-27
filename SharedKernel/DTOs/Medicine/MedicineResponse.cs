namespace SharedKernel.DTOs.Medicine;

public record MedicineResponse(
    string Id,
    string Name,
    string GenericName,
    string Manufacturer,
    string Description,
    string DosageForm,
    string Strength,
    decimal Price,
    int StockQuantity,
    bool RequiresPrescription,
    bool IsAvailable,
    DateTime? ExpiryDate,
    string Category,
 List<string> SideEffects,
    string StorageInstructions,
    DateTime CreatedAt,
    DateTime? UpdatedAt
);
