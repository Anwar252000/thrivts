using Microsoft.EntityFrameworkCore;
using Thrivts.Application.Common.Interfaces;
using Thrivts.Domain.Entities;

namespace Thrivts.Infrastructure.Persistence;

public class ThrivtsDbContext : DbContext, IApplicationDbContext
{
    public ThrivtsDbContext(DbContextOptions<ThrivtsDbContext> options) : base(options)
    {
    }

    public DbSet<Profile> Profiles => Set<Profile>();
    public DbSet<Deal> Deals => Set<Deal>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ThrivtsDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
