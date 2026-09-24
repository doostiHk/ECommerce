using BuildingBlocks.Persistence.Extensions;
using Microsoft.EntityFrameworkCore;

namespace BuildingBlocks.Persistence.DbContext;

public abstract class BaseDbContext(DbContextOptions options) : Microsoft.EntityFrameworkCore.DbContext(options)
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyAuditableEntityConfiguration();
    }
}
