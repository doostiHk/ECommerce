using BuildingBlocks.Common.Entities;
using BuildingBlocks.Persistence.Configurations;
using Microsoft.EntityFrameworkCore;

namespace BuildingBlocks.Persistence.Extensions;

public static class ModelBuilderExtensions
{
    public static ModelBuilder ApplyAuditableEntityConfiguration(this ModelBuilder modelBuilder)
    {
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (!typeof(IAuditableEntity).IsAssignableFrom(entityType.ClrType))
                continue;

            var method = typeof(ModelBuilderExtensions)
                .GetMethod(nameof(ConfigureAuditable), System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)!
                .MakeGenericMethod(entityType.ClrType);

            method.Invoke(null, [modelBuilder]);
        }

        return modelBuilder;
    }

    private static void ConfigureAuditable<TEntity>(ModelBuilder modelBuilder)
        where TEntity : class, IAuditableEntity
    {
        AuditableEntityConfiguration.Configure(modelBuilder.Entity<TEntity>());
    }
}
