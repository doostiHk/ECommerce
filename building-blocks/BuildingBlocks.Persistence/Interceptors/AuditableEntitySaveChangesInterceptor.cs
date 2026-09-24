using BuildingBlocks.Common.Abstractions;
using BuildingBlocks.Common.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace BuildingBlocks.Persistence.Interceptors;

public sealed class AuditableEntitySaveChangesInterceptor(
    ICurrentUser currentUser,
    IDateTimeProvider dateTimeProvider) : SaveChangesInterceptor
{
    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result)
    {
        UpdateAuditableEntities(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        UpdateAuditableEntities(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void UpdateAuditableEntities(Microsoft.EntityFrameworkCore.DbContext? context)
    {
        if (context is null)
            return;

        var utcNow = dateTimeProvider.UtcNow;
        var userId = currentUser.IsAuthenticated ? currentUser.UserId : null;

        foreach (var entry in context.ChangeTracker.Entries<IAuditableEntity>())
        {
            if (entry.State is EntityState.Added)
            {
                SetCreated(entry, utcNow, userId);
                continue;
            }

            if (entry.State is EntityState.Modified)
                SetModified(entry, utcNow, userId);
        }
    }

    private static void SetCreated(EntityEntry<IAuditableEntity> entry, DateTime utcNow, string? userId)
    {
        entry.Entity.CreatedAtUtc = utcNow;
        entry.Entity.CreatedByUserId = userId;
        entry.Entity.LastModifiedAtUtc = null;
        entry.Entity.LastModifiedByUserId = null;
    }

    private static void SetModified(EntityEntry<IAuditableEntity> entry, DateTime utcNow, string? userId)
    {
        entry.Property(e => e.CreatedAtUtc).IsModified = false;
        entry.Property(e => e.CreatedByUserId).IsModified = false;
        entry.Entity.LastModifiedAtUtc = utcNow;
        entry.Entity.LastModifiedByUserId = userId;
    }
}
