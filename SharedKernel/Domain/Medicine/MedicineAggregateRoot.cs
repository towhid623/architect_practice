using SharedKernel.Domain.Common;
using SharedKernel.Domain.Events;
using SharedKernel.Domain.ValueObjects;
using SharedKernel.Commands.Medicine;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace SharedKernel.Domain.Medicine;

/// <summary>
/// Medicine Aggregate Root - DDD pattern
/// </summary>
public class MedicineAggregateRoot : AggregateRoot
{
    [BsonElement("name")]
    private string _name = string.Empty;
    
    [BsonElement("generic_name")]
    private string _genericName = string.Empty;
    
    [BsonElement("manufacturer")]
    private string _manufacturer = string.Empty;
    
    [BsonElement("strength")]
    private string? _strengthValue;
    
    [BsonElement("price")]
    private decimal _priceAmount;
    
    [BsonElement("stock_quantity")]
    private int _stockAvailable;
  
    [BsonElement("is_available")]
    private bool _isAvailable = true;

    [BsonIgnore]
    public string Name => _name;
    
    [BsonIgnore]
    public string GenericName => _genericName;
    
    [BsonIgnore]
    public string Manufacturer => _manufacturer;
    
    [BsonIgnore]
    public Strength? Strength => _strengthValue != null ? new Strength(_strengthValue) : null;
    
    [BsonIgnore]
    public Money? Price => new Money(_priceAmount);
    
    [BsonIgnore]
    public StockQuantity? Stock => new StockQuantity(_stockAvailable);
    
    [BsonIgnore]
    public bool IsAvailable => _isAvailable;

    [BsonIgnore]
    public bool IsLowStock => _stockAvailable <= 10;
    
    [BsonIgnore]
    public bool IsOutOfStock => _stockAvailable <= 0;
    
    [BsonIgnore]
    public bool CanBeSold => _isAvailable && !IsOutOfStock;

    private MedicineAggregateRoot() { }

    public static MedicineAggregateRoot CreateFromCommand(CreateMedicineCommand command)
    {
        if (command == null)
          throw new ArgumentNullException(nameof(command));

     if (string.IsNullOrWhiteSpace(command.Name))
       throw new ArgumentException("Name is required", nameof(command));

        if (string.IsNullOrWhiteSpace(command.GenericName))
          throw new ArgumentException("Generic name is required", nameof(command));

        var medicine = new MedicineAggregateRoot
        {
       Id = ObjectId.GenerateNewId().ToString(),
          _name = command.Name.Trim(),
     _genericName = command.GenericName.Trim(),
 _manufacturer = command.Manufacturer?.Trim() ?? string.Empty,
  _strengthValue = command.Strength?.Trim(),
            _priceAmount = command.Price,
         _stockAvailable = command.StockQuantity,
      _isAvailable = true
        };

        medicine.AddDomainEvent(new MedicineCreatedEvent(
            medicine.Id,
            medicine.Name,
    medicine.GenericName,
            command.Manufacturer,
            command.Price,
       command.StockQuantity));

        return medicine;
    }

  public void UpdateDetails(string? name, string? genericName, string? manufacturer)
 {
        var changed = new Dictionary<string, object>();

        if (!string.IsNullOrWhiteSpace(name) && name != _name)
        {
   changed["Name"] = name;
    _name = name.Trim();
        }

        if (!string.IsNullOrWhiteSpace(genericName) && genericName != _genericName)
        {
 changed["GenericName"] = genericName;
            _genericName = genericName.Trim();
        }

        if (!string.IsNullOrWhiteSpace(manufacturer) && manufacturer != _manufacturer)
        {
      changed["Manufacturer"] = manufacturer;
          _manufacturer = manufacturer.Trim();
        }

        if (changed.Any())
        {
    MarkAsUpdated();
        AddDomainEvent(new MedicineUpdatedEvent(Id, Name, changed));
 }
    }

    public void ChangePrice(decimal newPrice)
    {
        if (newPrice < 0)
      throw new ArgumentException("Price cannot be negative");

        var oldPrice = _priceAmount;
  _priceAmount = newPrice;

  MarkAsUpdated();
        AddDomainEvent(new MedicinePriceChangedEvent(Id, oldPrice, newPrice, "Price updated"));
    }

    public void AddStock(int quantity)
    {
        if (quantity <= 0)
      throw new ArgumentException("Quantity must be positive");

        var oldStock = _stockAvailable;
        _stockAvailable += quantity;

   MarkAsUpdated();
        AddDomainEvent(new MedicineStockUpdatedEvent(Id, oldStock, _stockAvailable, $"Added {quantity} units"));
    }

    public void RemoveStock(int quantity)
    {
        if (quantity <= 0)
       throw new ArgumentException("Quantity must be positive");

     if (!CanBeSold)
            throw new InvalidOperationException("Medicine cannot be sold");

        if (quantity > _stockAvailable)
            throw new InvalidOperationException($"Insufficient stock. Available: {_stockAvailable}, Requested: {quantity}");

     var oldStock = _stockAvailable;
        _stockAvailable -= quantity;

        MarkAsUpdated();
        AddDomainEvent(new MedicineStockUpdatedEvent(Id, oldStock, _stockAvailable, $"Removed {quantity} units"));
  }

    public void MakeAvailable()
  {
        if (!_isAvailable)
        {
       _isAvailable = true;
     MarkAsUpdated();
       AddDomainEvent(new MedicineAvailabilityChangedEvent(Id, true, "Made available"));
        }
    }

 public void MakeUnavailable()
    {
        if (_isAvailable)
        {
   _isAvailable = false;
            MarkAsUpdated();
       AddDomainEvent(new MedicineAvailabilityChangedEvent(Id, false, "Made unavailable"));
      }
    }

    public override string ToString() => $"{Name} ({GenericName}) - ${_priceAmount:N2} - Stock: {_stockAvailable}";
}
