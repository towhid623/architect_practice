using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using test_service.Data;
using test_service.Models;

namespace test_service.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<UsersController> _logger;

    public UsersController(ApplicationDbContext context, ILogger<UsersController> logger)
    {
        _context = context;
        _logger = logger;
    }

  /// <summary>
    /// Get all users
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<User>>> GetUsers()
 {
        try
        {
var users = await _context.Users.ToListAsync();
    return Ok(users);
        }
        catch (Exception ex)
        {
        _logger.LogError(ex, "Error retrieving users");
        return StatusCode(500, new { error = "Failed to retrieve users" });
        }
    }

    /// <summary>
    /// Get user by ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<User>> GetUser(string id)
    {
      try
{
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == id);
            
         if (user == null)
{
          return NotFound(new { error = "User not found" });
          }

            return Ok(user);
        }
        catch (Exception ex)
        {
   _logger.LogError(ex, "Error retrieving user {UserId}", id);
          return StatusCode(500, new { error = "Failed to retrieve user" });
    }
    }

    /// <summary>
    /// Get user by email
    /// </summary>
    [HttpGet("by-email/{email}")]
    public async Task<ActionResult<User>> GetUserByEmail(string email)
{
  try
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
            
            if (user == null)
    {
        return NotFound(new { error = "User not found" });
          }

         return Ok(user);
        }
        catch (Exception ex)
  {
     _logger.LogError(ex, "Error retrieving user by email {Email}", email);
            return StatusCode(500, new { error = "Failed to retrieve user" });
   }
    }

    /// <summary>
    /// Create a new user
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<User>> CreateUser([FromBody] CreateUserDto dto)
    {
        try
        {
      // Check if user already exists
          var existingUser = await _context.Users
   .FirstOrDefaultAsync(u => u.Email == dto.Email || u.Username == dto.Username);
 
   if (existingUser != null)
    {
          return Conflict(new { error = "User with this email or username already exists" });
            }

    var user = new User
      {
    Username = dto.Username,
         Email = dto.Email,
           FullName = dto.FullName,
                Roles = dto.Roles ?? new List<string> { "user" }
            };

            _context.Users.Add(user);
  await _context.SaveChangesAsync();

       return CreatedAtAction(nameof(GetUser), new { id = user.Id }, user);
        }
catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating user");
   return StatusCode(500, new { error = "Failed to create user" });
        }
    }

    /// <summary>
    /// Update an existing user
    /// </summary>
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateUser(string id, [FromBody] UpdateUserDto dto)
 {
    try
      {
    var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == id);
    
       if (user == null)
            {
       return NotFound(new { error = "User not found" });
         }

// Update properties
        if (!string.IsNullOrEmpty(dto.FullName))
    user.FullName = dto.FullName;
  
            if (dto.IsActive.HasValue)
         user.IsActive = dto.IsActive.Value;
      
   if (dto.Roles != null && dto.Roles.Any())
      user.Roles = dto.Roles;

            user.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return Ok(user);
   }
    catch (Exception ex)
        {
   _logger.LogError(ex, "Error updating user {UserId}", id);
   return StatusCode(500, new { error = "Failed to update user" });
        }
    }

    /// <summary>
 /// Delete a user
/// </summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteUser(string id)
    {
        try
  {
     var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == id);
  
      if (user == null)
         {
    return NotFound(new { error = "User not found" });
    }

            _context.Users.Remove(user);
            await _context.SaveChangesAsync();

      return NoContent();
        }
  catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting user {UserId}", id);
            return StatusCode(500, new { error = "Failed to delete user" });
        }
    }

    /// <summary>
    /// Search users by name or email
    /// </summary>
    [HttpGet("search")]
    public async Task<ActionResult<IEnumerable<User>>> SearchUsers([FromQuery] string query)
    {
        try
        {
   var users = await _context.Users
       .Where(u => u.FullName.Contains(query) || u.Email.Contains(query) || u.Username.Contains(query))
                .ToListAsync();

        return Ok(users);
        }
        catch (Exception ex)
        {
  _logger.LogError(ex, "Error searching users with query {Query}", query);
       return StatusCode(500, new { error = "Failed to search users" });
        }
    }
}

// DTOs
public record CreateUserDto
{
    public string Username { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string FullName { get; init; } = string.Empty;
    public List<string>? Roles { get; init; }
}

public record UpdateUserDto
{
    public string? FullName { get; init; }
    public bool? IsActive { get; init; }
    public List<string>? Roles { get; init; }
}
