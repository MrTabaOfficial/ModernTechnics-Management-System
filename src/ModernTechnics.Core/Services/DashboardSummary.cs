using ModernTechnics.Core.Domain;

namespace ModernTechnics.Core.Services;

public sealed record DashboardSummary(
    int EmployeeCount,
    decimal MonthlyPayroll,
    int CustomerCount,
    int OpenApplications,
    int ProductCount,
    int UnitsInStock,
    decimal RevenueLast30Days,
    IReadOnlyList<DailyRevenue> RevenueByDay,
    IReadOnlyList<Headcount> HeadcountByPosition,
    IReadOnlyList<Product> LowStock)
{
    /// <summary>Store-floor quantity at or below which a product is flagged for restocking.</summary>
    public const int LowStockThreshold = 5;

    public const int RevenueChartDays = 7;
}

public sealed record DailyRevenue(DateOnly Day, decimal Amount);

public sealed record Headcount(string Position, int Count);
