using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace test_service.Models;

/// <summary>
/// Base entity with common properties
/// </summary>
public abstract class BaseEntity
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = ObjectId.GenerateNewId().ToString();

  [BsonElement("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [BsonElement("updated_at")]
 public DateTime? UpdatedAt { get; set; }
}

/// <summary>
/// Sample User entity
/// </summary>
[Collection("users")]
public class User : BaseEntity
{
    [BsonElement("username")]
    [Required]
    [StringLength(50)]
    public string Username { get; set; } = string.Empty;

    [BsonElement("email")]
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

[BsonElement("full_name")]
    public string FullName { get; set; } = string.Empty;

  [BsonElement("is_active")]
    public bool IsActive { get; set; } = true;

    [BsonElement("roles")]
 public List<string> Roles { get; set; } = new();
}

/// <summary>
/// Sample Product entity
/// </summary>
[Collection("products")]
public class Product : BaseEntity
{
    [BsonElement("name")]
    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [BsonElement("description")]
    public string Description { get; set; } = string.Empty;

    [BsonElement("price")]
    [Range(0, double.MaxValue)]
    public decimal Price { get; set; }

    [BsonElement("category")]
    public string Category { get; set; } = string.Empty;

    [BsonElement("stock_quantity")]
    public int StockQuantity { get; set; }

    [BsonElement("is_available")]
    public bool IsAvailable { get; set; } = true;

    [BsonElement("tags")]
public List<string> Tags { get; set; } = new();
}

/// <summary>
/// Medicine entity
/// </summary>
[Collection("medicines")]
public class Medicine : BaseEntity
{
    [BsonElement("name")]
    [Required]
    [StringLength(200)]
    public string Name { get; set; } = string.Empty;

    [BsonElement("generic_name")]
    public string GenericName { get; set; } = string.Empty;

    [BsonElement("manufacturer")]
    public string Manufacturer { get; set; } = string.Empty;

    [BsonElement("description")]
    public string Description { get; set; } = string.Empty;

    [BsonElement("dosage_form")]
    public string DosageForm { get; set; } = string.Empty;

    [BsonElement("strength")]
    public string Strength { get; set; } = string.Empty;

    [BsonElement("price")]
    [Range(0, double.MaxValue)]
    public decimal Price { get; set; }

    [BsonElement("stock_quantity")]
    public int StockQuantity { get; set; }

    [BsonElement("requires_prescription")]
    public bool RequiresPrescription { get; set; }

    [BsonElement("is_available")]
    public bool IsAvailable { get; set; } = true;

    [BsonElement("expiry_date")]
    public DateTime? ExpiryDate { get; set; }

    [BsonElement("category")]
    public string Category { get; set; } = string.Empty;

    [BsonElement("side_effects")]
    public List<string> SideEffects { get; set; } = new();

    [BsonElement("storage_instructions")]
    public string StorageInstructions { get; set; } = string.Empty;
}

/// <summary>
/// Sample Order entity
/// </summary>
[Collection("orders")]
public class Order : BaseEntity
{
    [BsonElement("order_number")]
    [Required]
    public string OrderNumber { get; set; } = string.Empty;

    [BsonElement("user_id")]
    [BsonRepresentation(BsonType.ObjectId)]
 public string UserId { get; set; } = string.Empty;

    [BsonElement("items")]
    public List<OrderItem> Items { get; set; } = new();

    [BsonElement("total_amount")]
    public decimal TotalAmount { get; set; }

    [BsonElement("status")]
public OrderStatus Status { get; set; } = OrderStatus.Pending;

    [BsonElement("shipping_address")]
    public Address? ShippingAddress { get; set; }
}

/// <summary>
/// Order item embedded document
/// </summary>
public class OrderItem
{
    [BsonElement("product_id")]
    [BsonRepresentation(BsonType.ObjectId)]
    public string ProductId { get; set; } = string.Empty;

    [BsonElement("product_name")]
    public string ProductName { get; set; } = string.Empty;

    [BsonElement("quantity")]
    public int Quantity { get; set; }

    [BsonElement("unit_price")]
    public decimal UnitPrice { get; set; }

    [BsonElement("subtotal")]
  public decimal Subtotal => Quantity * UnitPrice;
}

/// <summary>
/// Address embedded document
/// </summary>
public class Address
{
    [BsonElement("street")]
    public string Street { get; set; } = string.Empty;

    [BsonElement("city")]
 public string City { get; set; } = string.Empty;

    [BsonElement("state")]
    public string State { get; set; } = string.Empty;

  [BsonElement("postal_code")]
    public string PostalCode { get; set; } = string.Empty;

    [BsonElement("country")]
    public string Country { get; set; } = string.Empty;
}

/// <summary>
/// Order status enumeration
/// </summary>
public enum OrderStatus
{
    Pending = 0,
    Processing = 1,
    Shipped = 2,
    Delivered = 3,
    Cancelled = 4
}
