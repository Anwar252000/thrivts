using Microsoft.EntityFrameworkCore;
using Thrivts.Domain.Entities;

namespace Thrivts.Application.Common.Interfaces;

/// <summary>
/// Application-layer view of the DbContext. Infrastructure's ThrivtsDbContext implements this,
/// so handlers can be unit-tested against a mock without referencing EF Core's runtime/Npgsql.
/// </summary>
public interface IApplicationDbContext
{
    DbSet<Profile> Profiles { get; }
    DbSet<Deal> Deals { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}
