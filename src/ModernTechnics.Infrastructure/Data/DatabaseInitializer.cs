using Microsoft.EntityFrameworkCore;
using ModernTechnics.Core.Security;

namespace ModernTechnics.Infrastructure.Data;

/// <summary>Brings the database up to the current schema and fills an empty one with demo data.</summary>
public sealed class DatabaseInitializer(
    IDbContextFactory<AppDbContext> contextFactory, IPasswordHasher hasher, TimeProvider clock)
{
    public async Task InitializeAsync(CancellationToken ct = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(ct);
        await db.Database.MigrateAsync(ct);

        if (!await db.Users.AnyAsync(ct))
        {
            DemoData.Seed(db, hasher, clock.GetUtcNow().UtcDateTime);
            await db.SaveChangesAsync(ct);
        }
    }
}
