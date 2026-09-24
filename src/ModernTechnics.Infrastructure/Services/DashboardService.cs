using Microsoft.EntityFrameworkCore;
using ModernTechnics.Core.Domain;
using ModernTechnics.Core.Services;
using ModernTechnics.Infrastructure.Data;

namespace ModernTechnics.Infrastructure.Services;

public sealed class DashboardService(IDbContextFactory<AppDbContext> contextFactory, TimeProvider clock)
    : IDashboardService
{
    public async Task<DashboardSummary> GetSummaryAsync(CancellationToken ct = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(ct);

        // SQLite stores decimals as text and cannot SUM them, so money is totalled in memory.
        // The data set of a single shop is small enough for that to be cheap.
        var employees = await db.Employees.AsNoTracking()
            .Select(e => new { e.Salary, Position = e.Position!.Name })
            .ToListAsync(ct);

        var products = await db.Products.AsNoTracking().ToListAsync(ct);

        var now = clock.GetLocalNow();
        var today = DateOnly.FromDateTime(now.DateTime);
        var since = clock.GetUtcNow().UtcDateTime.AddDays(-30);
        var recentOrders = await db.Orders.AsNoTracking()
            .Where(o => o.PlacedAtUtc >= since)
            .Select(o => new { o.PlacedAtUtc, o.UnitPrice, o.Quantity })
            .ToListAsync(ct);

        var revenueByDay = recentOrders
            .GroupBy(o => DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(o.PlacedAtUtc, clock.LocalTimeZone)))
            .ToDictionary(g => g.Key, g => g.Sum(o => o.UnitPrice * o.Quantity));

        var chart = Enumerable.Range(0, DashboardSummary.RevenueChartDays)
            .Select(offset => today.AddDays(offset - DashboardSummary.RevenueChartDays + 1))
            .Select(day => new DailyRevenue(day, revenueByDay.GetValueOrDefault(day)))
            .ToList();

        return new DashboardSummary(
            EmployeeCount: employees.Count,
            MonthlyPayroll: employees.Sum(e => e.Salary),
            CustomerCount: await db.Customers.CountAsync(ct),
            OpenApplications: await db.JobApplications.CountAsync(
                a => a.Status == ApplicationStatus.New || a.Status == ApplicationStatus.Interview, ct),
            ProductCount: products.Count,
            UnitsInStock: products.Sum(p => p.TotalStock),
            RevenueLast30Days: recentOrders.Sum(o => o.UnitPrice * o.Quantity),
            RevenueByDay: chart,
            HeadcountByPosition: [.. employees
                .GroupBy(e => e.Position)
                .Select(g => new Headcount(g.Key, g.Count()))
                .OrderByDescending(h => h.Count)
                .ThenBy(h => h.Position)],
            LowStock: [.. products
                .Where(p => p.StoreStock <= DashboardSummary.LowStockThreshold)
                .OrderBy(p => p.StoreStock)
                .ThenBy(p => p.Name)]);
    }
}
