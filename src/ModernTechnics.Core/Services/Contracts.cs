using ModernTechnics.Core.Common;
using ModernTechnics.Core.Domain;

namespace ModernTechnics.Core.Services;

public interface IAuthService
{
    /// <summary>Returns the account when the credentials match; otherwise a single generic error.</summary>
    Task<Result<UserAccount>> SignInAsync(string email, string password, CancellationToken ct = default);
}

public interface IUserService
{
    Task<IReadOnlyList<UserAccount>> ListAsync(string? search = null, CancellationToken ct = default);

    Task<Result<UserAccount>> CreateAsync(string email, string password, Role role, CancellationToken ct = default);

    /// <summary>Changes the role and, when <paramref name="newPassword"/> is not empty, the password.</summary>
    Task<Result> UpdateAsync(int id, Role role, string? newPassword, CancellationToken ct = default);

    Task<Result> DeleteAsync(int id, int actingUserId, CancellationToken ct = default);
}

public interface IEmployeeService
{
    Task<IReadOnlyList<Employee>> ListAsync(string? search = null, CancellationToken ct = default);

    Task<IReadOnlyList<Position>> ListPositionsAsync(CancellationToken ct = default);

    Task<Result> CreateAsync(Employee employee, CancellationToken ct = default);

    Task<Result> UpdateAsync(Employee employee, CancellationToken ct = default);

    Task<Result> SetSalaryAsync(string personalId, decimal salary, CancellationToken ct = default);

    Task<Result> DeleteAsync(string personalId, CancellationToken ct = default);
}

public interface ICustomerService
{
    Task<IReadOnlyList<Customer>> ListAsync(string? search = null, CancellationToken ct = default);

    Task<Result> CreateAsync(Customer customer, CancellationToken ct = default);

    Task<Result> UpdateAsync(Customer customer, CancellationToken ct = default);

    Task<Result> DeleteAsync(string personalId, CancellationToken ct = default);
}

public interface IJobApplicationService
{
    Task<IReadOnlyList<JobApplication>> ListAsync(string? search = null, CancellationToken ct = default);

    Task<Result> SubmitAsync(JobApplication application, CancellationToken ct = default);

    Task<Result> SetStatusAsync(int id, ApplicationStatus status, CancellationToken ct = default);

    Task<Result> DeleteAsync(int id, CancellationToken ct = default);
}

public interface IInventoryService
{
    Task<IReadOnlyList<Product>> ListAsync(string? search = null, CancellationToken ct = default);

    Task<Result> CreateAsync(Product product, CancellationToken ct = default);

    Task<Result> UpdateAsync(Product product, CancellationToken ct = default);

    Task<Result> DeleteAsync(int id, CancellationToken ct = default);

    /// <summary>Books a delivery into the warehouse.</summary>
    Task<Result> ReceiveAsync(int productId, int quantity, CancellationToken ct = default);

    /// <summary>Moves units from the warehouse to the store floor.</summary>
    Task<Result> TransferToStoreAsync(int productId, int quantity, CancellationToken ct = default);
}

public interface ISalesService
{
    Task<IReadOnlyList<Order>> ListOrdersAsync(string? search = null, CancellationToken ct = default);

    /// <summary>Sells units from the store floor and records the order atomically.</summary>
    Task<Result<Order>> PlaceOrderAsync(
        int productId, int quantity, string? customerPersonalId = null, CancellationToken ct = default);
}

public interface IDashboardService
{
    Task<DashboardSummary> GetSummaryAsync(CancellationToken ct = default);
}
