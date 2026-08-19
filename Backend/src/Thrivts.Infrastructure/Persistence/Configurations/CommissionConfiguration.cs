using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Thrivts.Domain.Entities;

namespace Thrivts.Infrastructure.Persistence.Configurations;

public class CommissionConfiguration : IEntityTypeConfiguration<Commission>
{
    public void Configure(EntityTypeBuilder<Commission> builder)
    {
        builder.ToTable("commissions");

        builder.HasKey(c => c.Id);

        // Status maps to the native commission_status Postgres enum via Npgsql's own enum
        // support — see NpgsqlEnumMapping.Configure. No HasConversion here: a string converter
        // reads fine but breaks any WHERE-clause filter ("42883: operator does not exist").
    }
}
