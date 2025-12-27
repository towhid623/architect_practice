using System;
using medicine_command_worker_host.Domain.Common;
using medicine_command_worker_host.Domain.Events;
using medicine_command_worker_host.Domain.ValueObjects;
using MongoDB.Bson;
using SharedKernel.Commands.Medicine;

namespace medicine_command_worker_host.Domain;

/// <summary>
/// Medicine Aggregate Root - simplified version with essential business rules
/// </summary>
public class MedicineAggregateRoot : AggregateRoot
{
    // Essential properties
    private string _name = string.Empty;
    private string _genericName = string.Empty;
    private string _manufacturer = string.Empty;
    private Strength? _strength;
    private Money? _price;
    private StockQuantity? _stock;
    private bool _isAvailable = true;

    // Public read-only properties
    public string Name => _name;
    public string GenericName => _genericName;
    public string Manufacturer => _manufacturer;
    public Strength? Strength => _strength;
    public Money? Price => _price;
    public StockQuantity? Stock => _stock;
    public bool IsAvailable => _isAvailable;

    // Computed properties
    public bool IsLowStock => _stock?.IsLowStock ?? false;
 public bool IsOutOfStock => _stock?.IsOutOfStock ?? true;
    public bool CanBeSold => _isAvailable && !IsOutOfStock;

    private MedicineAggregateRoot() { }

    // Factory method from command
    public static MedicineAggregateRoot CreateFromCommand(CreateMedicineCommand command)
    {
        if (command == null)
      throw new ArgumentNullException(nameof(command));

        // Validate
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
       _strength = !string.IsNullOrWhiteSpace(command.Strength) ? new Strength(command.Strength) : null,
          _price = new Money(command.Price),
    _stock = new StockQuantity(command.StockQuantity),
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

    // Original factory method for flexibility
    public static MedicineAggregateRoot Create(
        string name,
   string genericName,
        string manufacturer,
    string strength,
decimal price,
   int stockQuantity)
    {
        if (string.IsNullOrWhiteSpace(name))
 throw new ArgumentException("Name is required", nameof(name));

    if (string.IsNullOrWhiteSpace(genericName))
      throw new ArgumentException("Generic name is required", nameof(genericName));

        var medicine = new MedicineAggregateRoot
        {
       Id = ObjectId.GenerateNewId().ToString(),
            _name = name.Trim(),
            _genericName = genericName.Trim(),
            _manufacturer = manufacturer?.Trim() ?? string.Empty,
   _strength = !string.IsNullOrWhiteSpace(strength) ? new Strength(strength) : null,
            _price = new Money(price),
         _stock = new StockQuantity(stockQuantity),
            _isAvailable = true
    };

        medicine.AddDomainEvent(new MedicineCreatedEvent(
    medicine.Id,
      medicine.Name,
  medicine.GenericName,
     manufacturer,
            price,
      stockQuantity));

    return medicine;
    }

    // Business methods
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
        if (_price == null)
            throw new InvalidOperationException("Price not set");

        var oldPrice = _price.Amount;
        _price = new Money(newPrice);

        MarkAsUpdated();
        AddDomainEvent(new MedicinePriceChangedEvent(Id, oldPrice, newPrice, "Price updated"));
    }

    public void AddStock(int quantity)
    {
        if (_stock == null)
   throw new InvalidOperationException("Stock not initialized");

        var oldStock = _stock.Available;
        _stock = _stock.Add(quantity);

  MarkAsUpdated();
        AddDomainEvent(new MedicineStockUpdatedEvent(Id, oldStock, _stock.Available, $"Added {quantity} units"));
    }

    public void RemoveStock(int quantity)
    {
        if (_stock == null)
            throw new InvalidOperationException("Stock not initialized");

        if (!CanBeSold)
   throw new InvalidOperationException("Medicine cannot be sold");

        var oldStock = _stock.Available;
    _stock = _stock.Remove(quantity);

    MarkAsUpdated();
  AddDomainEvent(new MedicineStockUpdatedEvent(Id, oldStock, _stock.Available, $"Removed {quantity} units"));
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

    public override string ToString() => $"{Name} ({GenericName}) - {_price} - Stock: {_stock?.Available ?? 0}";
}
