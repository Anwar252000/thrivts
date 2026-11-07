using Microsoft.EntityFrameworkCore;
using Thrivts.Application.Common.Interfaces;

namespace Thrivts.Infrastructure.Persistence;

/// <summary>An atomic UPSERT ... RETURNING on serial_counters. Inside a transaction the row stays
/// locked until commit, so concurrent requests for the same series queue up instead of colliding;
/// a rolled-back transaction gives its number back.</summary>
public sealed class SerialGenerator(ThrivtsDbContext db) : ISerialGenerator
{
    public async Task<long> NextAsync(string counter, CancellationToken cancellationToken)
    {
        var values = await db.Database
            .SqlQuery<long>($"""
                INSERT INTO serial_counters (name, value) VALUES ({counter}, 1)
                ON CONFLICT (name) DO UPDATE SET value = serial_counters.value + 1
                RETURNING value AS "Value"
                """)
            .ToListAsync(cancellationToken);

        return values[0];
    }

    public async Task<long> PeekAsync(string counter, CancellationToken cancellationToken)
    {
        var current = await db.SerialCounters.AsNoTracking()
            .Where(c => c.Name == counter)
            .Select(c => (long?)c.Value)
            .FirstOrDefaultAsync(cancellationToken);

        return (current ?? 0) + 1;
    }
}
