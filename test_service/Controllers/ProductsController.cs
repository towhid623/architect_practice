using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using test_service.Data;
using test_service.Models;

namespace test_service.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProductsController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<ProductsController> _logger;

    public ProductsController(ApplicationDbContext context, ILogger<ProductsController> logger)
    {
        _context = context;
        _logger = logger;
    }

  /// <summary>
    /// Get all products
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Product>>> GetProducts([FromQuery] int? limit = null)
    {
        try
        {
var query = _context.Products.AsQueryable();
       
            if (limit.HasValue)
            {
       query = query.Take(limit.Value);
         }

            var products = await query.ToListAsync();
  return Ok(products);
        }
        catch (Exception ex)
        {
     _logger.LogError(ex, "Error retrieving products");
            return StatusCode(500, new { error = "Failed to retrieve products" });
      }
    }

    /// <summary>
    /// Get product by ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<Product>> GetProduct(string id)
    {
        try
        {
  var product = await _context.Products.FirstOrDefaultAsync(p => p.Id == id);
 
            if (product == null)
      {
          return NotFound(new { error = "Product not found" });
            }

  return Ok(product);
        }
        catch (Exception ex)
        {
   _logger.LogError(ex, "Error retrieving product {ProductId}", id);
            return StatusCode(500, new { error = "Failed to retrieve product" });
        }
    }

    /// <summary>
    /// Get products by category
    /// </summary>
    [HttpGet("category/{category}")]
    public async Task<ActionResult<IEnumerable<Product>>> GetProductsByCategory(string category)
    {
        try
        {
      var products = await _context.Products
 .Where(p => p.Category == category)
             .ToListAsync();

  return Ok(products);
        }
        catch (Exception ex)
        {
       _logger.LogError(ex, "Error retrieving products by category {Category}", category);
     return StatusCode(500, new { error = "Failed to retrieve products" });
  }
    }

    /// <summary>
    /// Create a new product
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<Product>> CreateProduct([FromBody] CreateProductDto dto)
    {
     try
        {
         var product = new Product
       {
                Name = dto.Name,
    Description = dto.Description,
     Price = dto.Price,
     Category = dto.Category,
      StockQuantity = dto.StockQuantity,
    Tags = dto.Tags ?? new List<string>()
      };

    _context.Products.Add(product);
await _context.SaveChangesAsync();

 return CreatedAtAction(nameof(GetProduct), new { id = product.Id }, product);
        }
   catch (Exception ex)
        {
  _logger.LogError(ex, "Error creating product");
 return StatusCode(500, new { error = "Failed to create product" });
        }
    }

    /// <summary>
    /// Update an existing product
    /// </summary>
 [HttpPut("{id}")]
    public async Task<IActionResult> UpdateProduct(string id, [FromBody] UpdateProductDto dto)
    {
        try
        {
            var product = await _context.Products.FirstOrDefaultAsync(p => p.Id == id);
       
      if (product == null)
            {
         return NotFound(new { error = "Product not found" });
            }

            // Update properties
   if (!string.IsNullOrEmpty(dto.Name))
      product.Name = dto.Name;
  
          if (!string.IsNullOrEmpty(dto.Description))
 product.Description = dto.Description;
            
            if (dto.Price.HasValue)
       product.Price = dto.Price.Value;
            
         if (!string.IsNullOrEmpty(dto.Category))
    product.Category = dto.Category;
            
      if (dto.StockQuantity.HasValue)
    product.StockQuantity = dto.StockQuantity.Value;
       
      if (dto.IsAvailable.HasValue)
  product.IsAvailable = dto.IsAvailable.Value;
            
        if (dto.Tags != null && dto.Tags.Any())
     product.Tags = dto.Tags;

   product.UpdatedAt = DateTime.UtcNow;

 await _context.SaveChangesAsync();

            return Ok(product);
   }
        catch (Exception ex)
        {
     _logger.LogError(ex, "Error updating product {ProductId}", id);
            return StatusCode(500, new { error = "Failed to update product" });
        }
    }

    /// <summary>
    /// Delete a product
  /// </summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteProduct(string id)
 {
        try
      {
          var product = await _context.Products.FirstOrDefaultAsync(p => p.Id == id);
    
   if (product == null)
     {
    return NotFound(new { error = "Product not found" });
            }

   _context.Products.Remove(product);
 await _context.SaveChangesAsync();

      return NoContent();
        }
      catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting product {ProductId}", id);
            return StatusCode(500, new { error = "Failed to delete product" });
        }
    }

    /// <summary>
    /// Search products
    /// </summary>
    [HttpGet("search")]
 public async Task<ActionResult<IEnumerable<Product>>> SearchProducts([FromQuery] string query)
 {
        try
        {
            var products = await _context.Products
         .Where(p => p.Name.Contains(query) || p.Description.Contains(query) || p.Category.Contains(query))
 .ToListAsync();

   return Ok(products);
        }
        catch (Exception ex)
        {
          _logger.LogError(ex, "Error searching products with query {Query}", query);
            return StatusCode(500, new { error = "Failed to search products" });
        }
    }
}

// DTOs
public record CreateProductDto
{
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public decimal Price { get; init; }
  public string Category { get; init; } = string.Empty;
    public int StockQuantity { get; init; }
    public List<string>? Tags { get; init; }
}

public record UpdateProductDto
{
    public string? Name { get; init; }
    public string? Description { get; init; }
    public decimal? Price { get; init; }
    public string? Category { get; init; }
    public int? StockQuantity { get; init; }
    public bool? IsAvailable { get; init; }
    public List<string>? Tags { get; init; }
}
