namespace SharedKernel.Domain.ValueObjects;

public record Strength
{
    public string Value { get; init; }

    public Strength(string value)
    {
      if (string.IsNullOrWhiteSpace(value))
   throw new ArgumentException("Strength cannot be empty", nameof(value));

    Value = value.Trim();
    }

    public override string ToString() => Value;
}

public record Money
{
    public decimal Amount { get; init; }

    public Money(decimal amount)
    {
        if (amount < 0)
            throw new ArgumentException("Amount cannot be negative", nameof(amount));

        Amount = amount;
    }

    public override string ToString() => $"${Amount:N2}";
}

public record StockQuantity
{
    public int Available { get; init; }
    public bool IsLowStock => Available <= 10;
    public bool IsOutOfStock => Available <= 0;

    public StockQuantity(int available)
    {
     if (available < 0)
     throw new ArgumentException("Stock quantity cannot be negative", nameof(available));

        Available = available;
    }

    public StockQuantity Add(int quantity)
    {
        if (quantity < 0)
            throw new ArgumentException("Cannot add negative quantity", nameof(quantity));

     return new StockQuantity(Available + quantity);
    }

    public StockQuantity Remove(int quantity)
    {
        if (quantity < 0)
            throw new ArgumentException("Cannot remove negative quantity", nameof(quantity));

        if (quantity > Available)
            throw new InvalidOperationException($"Insufficient stock. Available: {Available}, Requested: {quantity}");

     return new StockQuantity(Available - quantity);
    }

    public override string ToString() => $"{Available} units";
}
