using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using ModernTechnics.Core.Security;
using ModernTechnics.Infrastructure.Data;
using ModernTechnics.Infrastructure.Security;
using ModernTechnics.Infrastructure.Services;

namespace ModernTechnics.Tests.Support;

/// <summary>
/// A private in-memory SQLite database, migrated and seeded with the demo data.
/// The real provider is used so constraints, transactions and SQL translation are exercised.
/// </summary>
public sealed class TestDatabase : IDbContextFactory<AppDbContext>, IAsyncDisposable
{
    private readonly SqliteConnection _connection = new("Data Source=:memory:");
    private readonly DbContextOptions<AppDbContext> _options;

    private TestDatabase()
    {
        _connection.Open();
        _options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(_connection).Options;
    }

    public FixedClock Clock { get; } = new(new DateTimeOffset(2026, 10, 7, 12, 0, 0, TimeSpan.Zero));

    /// <summary>A deliberately cheap work factor keeps the suite fast.</summary>
    public IPasswordHasher Hasher { get; } = new Pbkdf2PasswordHasher(iterations: 1_000);

    public AuthService Auth => new(this, Hasher);

    public UserService Users => new(this, Hasher);

    public EmployeeService Employees => new(this, Clock);

    public CustomerService Customers => new(this, Clock);

    public JobApplicationService Applications => new(this, Clock);

    public InventoryService Inventory => new(this, Clock);

    public SalesService Sales => new(this, Clock);

    public DashboardService Dashboard => new(this, Clock);

    public DatabaseInitializer Initializer => new(this, Hasher, Clock);

    public static async Task<TestDatabase> CreateAsync()
    {
        var database = new TestDatabase();
        await database.Initializer.InitializeAsync(TestContext.Current.CancellationToken);
        return database;
    }

    public AppDbContext CreateDbContext() => new(_options);

    public ValueTask DisposeAsync() => _connection.DisposeAsync();
}

public sealed class FixedClock(DateTimeOffset now) : TimeProvider
{
    public DateTimeOffset Now { get; set; } = now;

    public override TimeZoneInfo LocalTimeZone => TimeZoneInfo.Utc;

    public override DateTimeOffset GetUtcNow() => Now;
}
