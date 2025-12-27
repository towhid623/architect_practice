using Microsoft.EntityFrameworkCore;
using MongoDB.EntityFrameworkCore.Extensions;
using test_service.Models;

namespace test_service.Data;

/// <summary>
/// MongoDB DbContext using Entity Framework Core
/// </summary>
public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    // DbSets for collections
    public DbSet<User> Users { get; init; }
    public DbSet<Product> Products { get; init; }
    public DbSet<Order> Orders { get; init; }
    public DbSet<Medicine> Medicines { get; init; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure MongoDB collections
    modelBuilder.Entity<User>().ToCollection("users");
        modelBuilder.Entity<Product>().ToCollection("products");
        modelBuilder.Entity<Order>().ToCollection("orders");
  modelBuilder.Entity<Medicine>().ToCollection("medicines");

   // Configure indexes
   modelBuilder.Entity<User>()
    .HasIndex(u => u.Email)
 .IsUnique();

     modelBuilder.Entity<User>()
.HasIndex(u => u.Username)
      .IsUnique();

      modelBuilder.Entity<Product>()
     .HasIndex(p => p.Name);

        modelBuilder.Entity<Product>()
         .HasIndex(p => p.Category);

      modelBuilder.Entity<Order>()
     .HasIndex(o => o.OrderNumber)
            .IsUnique();

        modelBuilder.Entity<Order>()
      .HasIndex(o => o.UserId);

        modelBuilder.Entity<Order>()
 .HasIndex(o => o.Status);

        modelBuilder.Entity<Medicine>()
            .HasIndex(m => m.Name);

        modelBuilder.Entity<Medicine>()
 .HasIndex(m => m.GenericName);

  modelBuilder.Entity<Medicine>()
     .HasIndex(m => m.Category);

 modelBuilder.Entity<Medicine>()
   .HasIndex(m => m.Manufacturer);
    }
}
