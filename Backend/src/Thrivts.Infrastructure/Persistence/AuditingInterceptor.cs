using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Thrivts.Application.Common.Interfaces;
using Thrivts.Domain.Common;
using Thrivts.Domain.Communication;
using Thrivts.Domain.Identity;
using Thrivts.Domain.Inventory;
using Thrivts.Domain.Operations;
using Thrivts.Domain.Sku;

namespace Thrivts.Infrastructure.Persistence;

/// <summary>
/// Runs inside every SaveChanges: stamps created/updated by + at, converts deletes of soft-deletable
/// rows into updates, and writes one audit_logs row per changed entity with the before/after of each
/// changed column. Registered as a singleton — it reads the user per call, so it is safe with
/// DbContext pooling.
/// </summary>
public sealed class AuditingInterceptor(ICurrentUser currentUser, IClock clock) : SaveChangesInterceptor
{
    private static readonly HashSet<Type> NotAudited =
    [
        typeof(AuditLog), typeof(ActivityEvent), typeof(StockMovement), typeof(RefreshToken),
        typeof(StoredFile), typeof(DailySubmission), typeof(SerialCounter), typeof(IdempotencyRecord),
        typeof(SkuDispatchComponent), typeof(SackComponent),
    ];

    private static readonly HashSet<string> SecretColumns =
    [
        nameof(AppUser.PinHash), nameof(StoredFile.Content), nameof(RefreshToken.TokenHash), nameof(IVersioned.Version),
    ];

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        if (eventData.Context is not null)
            Apply(eventData.Context);

        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        if (eventData.Context is not null)
            Apply(eventData.Context);

        return base.SavingChanges(eventData, result);
    }

    private void Apply(DbContext context)
    {
        var now = clock.UtcNow;
        var userId = currentUser.UserId;
        var audits = new List<AuditLog>();

        foreach (var entry in context.ChangeTracker.Entries().ToList())
        {
            if (entry.State is EntityState.Detached or EntityState.Unchanged)
                continue;

            // Safety net for leaf rows only. Aggregates with child collections must call MarkDeleted
            // themselves: Remove() would also cascade-mark their tracked children as hard deletes.
            if (entry.State == EntityState.Deleted && entry.Entity is ISoftDeletable softDeletable)
            {
                entry.State = EntityState.Modified;
                softDeletable.MarkDeleted(now, userId);
            }

            if (entry.Entity is IAuditable auditable)
            {
                if (entry.State == EntityState.Added)
                    auditable.StampCreated(now, userId);
                else if (entry.State == EntityState.Modified)
                    auditable.StampUpdated(now, userId);
            }

            if (entry.Entity is Entity entity && !NotAudited.Contains(entry.Metadata.ClrType) && !entry.Metadata.IsOwned())
            {
                var action = entry.State switch
                {
                    EntityState.Added => AuditAction.Created,
                    EntityState.Deleted => AuditAction.Deleted,
                    _ => entry.Entity is ISoftDeletable { IsDeleted: true } && WasJustDeleted(entry) ? AuditAction.Deleted : AuditAction.Updated,
                };

                var changes = DescribeChanges(entry);
                if (action != AuditAction.Updated || changes is not null)
                {
                    audits.Add(new AuditLog(now, userId, currentUser.Name, action, entry.Metadata.ClrType.Name, entity.Id,
                        changes, currentUser.CorrelationId, currentUser.IpAddress));
                }
            }
        }

        if (audits.Count > 0)
            context.Set<AuditLog>().AddRange(audits);
    }

    private static bool WasJustDeleted(EntityEntry entry) =>
        entry.Property(nameof(ISoftDeletable.IsDeleted)) is { IsModified: true, OriginalValue: false };

    private static string? DescribeChanges(EntityEntry entry)
    {
        var changes = new Dictionary<string, object?>();

        foreach (var property in entry.Properties)
        {
            var name = property.Metadata.Name;
            if (SecretColumns.Contains(name))
                continue;

            switch (entry.State)
            {
                case EntityState.Added:
                    if (property.CurrentValue is not null)
                        changes[name] = property.CurrentValue;
                    break;
                case EntityState.Modified when property.IsModified && !Equals(property.OriginalValue, property.CurrentValue):
                    changes[name] = new { from = property.OriginalValue, to = property.CurrentValue };
                    break;
            }
        }

        return changes.Count == 0 ? null : JsonSerializer.Serialize(changes);
    }
}
