namespace test_service.Models.DTOs;

public record CreateMedicineDto
{
  public string Name { get; init; } = string.Empty;
    public string GenericName { get; init; } = string.Empty;
    public string Manufacturer { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string DosageForm { get; init; } = string.Empty;
    public string Strength { get; init; } = string.Empty;
    public decimal Price { get; init; }
    public int StockQuantity { get; init; }
    public bool RequiresPrescription { get; init; }
    public DateTime? ExpiryDate { get; init; }
    public string Category { get; init; } = string.Empty;
    public List<string>? SideEffects { get; init; }
    public string StorageInstructions { get; init; } = string.Empty;
}

public record UpdateMedicineDto
{
    public string? Name { get; init; }
    public string? GenericName { get; init; }
    public string? Manufacturer { get; init; }
    public string? Description { get; init; }
    public string? DosageForm { get; init; }
    public string? Strength { get; init; }
    public decimal? Price { get; init; }
    public int? StockQuantity { get; init; }
    public bool? RequiresPrescription { get; init; }
    public bool? IsAvailable { get; init; }
    public DateTime? ExpiryDate { get; init; }
 public string? Category { get; init; }
    public List<string>? SideEffects { get; init; }
    public string? StorageInstructions { get; init; }
}
