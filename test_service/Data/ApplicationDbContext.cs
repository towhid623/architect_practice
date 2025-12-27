using Microsoft.EntityFrameworkCore;
using SharedKernel.Domain.Medicine;

namespace test_service.Data;

/// <summary>
/// Application database context for MongoDB
/// </summary>
public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<MedicineAggregateRoot> Medicines { get; init; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
    }
}
