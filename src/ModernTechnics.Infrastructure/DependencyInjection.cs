using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using ModernTechnics.Core.Security;
using ModernTechnics.Core.Services;
using ModernTechnics.Infrastructure.Data;
using ModernTechnics.Infrastructure.Security;
using ModernTechnics.Infrastructure.Services;

namespace ModernTechnics.Infrastructure;

public static class DependencyInjection
{
    /// <summary>Registers the SQLite database and every application service.</summary>
    public static IServiceCollection AddModernTechnics(this IServiceCollection services, string connectionString)
    {
        services.AddDbContextFactory<AppDbContext>(options => options.UseSqlite(connectionString));

        services.TryAddSingleton(TimeProvider.System);
        services.TryAddSingleton<IPasswordHasher, Pbkdf2PasswordHasher>();

        services.AddSingleton<DatabaseInitializer>();
        services.AddSingleton<IAuthService, AuthService>();
        services.AddSingleton<IUserService, UserService>();
        services.AddSingleton<IEmployeeService, EmployeeService>();
        services.AddSingleton<ICustomerService, CustomerService>();
        services.AddSingleton<IJobApplicationService, JobApplicationService>();
        services.AddSingleton<IInventoryService, InventoryService>();
        services.AddSingleton<ISalesService, SalesService>();
        services.AddSingleton<IDashboardService, DashboardService>();

        return services;
    }
}
