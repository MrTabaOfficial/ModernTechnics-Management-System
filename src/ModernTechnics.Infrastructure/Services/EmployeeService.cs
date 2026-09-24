using Microsoft.EntityFrameworkCore;
using ModernTechnics.Core.Common;
using ModernTechnics.Core.Domain;
using ModernTechnics.Core.Services;
using ModernTechnics.Core.Validation;
using ModernTechnics.Infrastructure.Data;

namespace ModernTechnics.Infrastructure.Services;

public sealed class EmployeeService(IDbContextFactory<AppDbContext> contextFactory, TimeProvider clock)
    : IEmployeeService
{
    public async Task<IReadOnlyList<Employee>> ListAsync(string? search = null, CancellationToken ct = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(ct);
        var query = db.Employees.AsNoTracking().Include(e => e.Position).AsQueryable();

        if (Search.ToPattern(search) is { } pattern)
        {
            query = query.Where(e =>
                EF.Functions.Like(e.PersonalId, pattern, Search.Escape)
                || EF.Functions.Like(e.FirstName, pattern, Search.Escape)
                || EF.Functions.Like(e.LastName, pattern, Search.Escape)
                || EF.Functions.Like(e.Email, pattern, Search.Escape)
                || EF.Functions.Like(e.Position!.Name, pattern, Search.Escape));
        }

        return await query.OrderBy(e => e.LastName).ThenBy(e => e.FirstName).ToListAsync(ct);
    }

    public async Task<IReadOnlyList<Position>> ListPositionsAsync(CancellationToken ct = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(ct);
        return await db.Positions.AsNoTracking().OrderBy(p => p.Name).ToListAsync(ct);
    }

    public async Task<Result> CreateAsync(Employee employee, CancellationToken ct = default)
    {
        Normalize(employee);
        var validation = EntityValidators.Validate(employee, Today());
        if (validation.IsFailure)
        {
            return validation;
        }

        await using var db = await contextFactory.CreateDbContextAsync(ct);
        if (await db.Employees.AnyAsync(e => e.PersonalId == employee.PersonalId, ct))
        {
            return Result.Failure(new Error(ErrorCodes.Duplicate, nameof(Employee.PersonalId)));
        }

        var conflict = await CheckReferencesAsync(db, employee, ct);
        if (conflict is not null)
        {
            return Result.Failure(conflict);
        }

        employee.Position = null;
        db.Employees.Add(employee);
        await db.SaveChangesAsync(ct);
        return Result.Success();
    }

    public async Task<Result> UpdateAsync(Employee employee, CancellationToken ct = default)
    {
        Normalize(employee);
        var validation = EntityValidators.Validate(employee, Today());
        if (validation.IsFailure)
        {
            return validation;
        }

        await using var db = await contextFactory.CreateDbContextAsync(ct);
        var existing = await db.Employees.FindAsync([employee.PersonalId], ct);
        if (existing is null)
        {
            return Result.Failure(new Error(ErrorCodes.NotFound));
        }

        var conflict = await CheckReferencesAsync(db, employee, ct);
        if (conflict is not null)
        {
            return Result.Failure(conflict);
        }

        existing.FirstName = employee.FirstName;
        existing.LastName = employee.LastName;
        existing.BirthDate = employee.BirthDate;
        existing.Address = employee.Address;
        existing.Phone = employee.Phone;
        existing.Email = employee.Email;
        existing.MaritalStatus = employee.MaritalStatus;
        existing.PositionId = employee.PositionId;
        existing.Salary = employee.Salary;
        await db.SaveChangesAsync(ct);
        return Result.Success();
    }

    public async Task<Result> SetSalaryAsync(string personalId, decimal salary, CancellationToken ct = default)
    {
        if (salary < 0)
        {
            return Result.Failure(new Error(ErrorCodes.MustNotBeNegative, nameof(Employee.Salary)));
        }

        await using var db = await contextFactory.CreateDbContextAsync(ct);
        var employee = await db.Employees.FindAsync([personalId], ct);
        if (employee is null)
        {
            return Result.Failure(new Error(ErrorCodes.NotFound));
        }

        employee.Salary = salary;
        await db.SaveChangesAsync(ct);
        return Result.Success();
    }

    public async Task<Result> DeleteAsync(string personalId, CancellationToken ct = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(ct);
        var deleted = await db.Employees.Where(e => e.PersonalId == personalId).ExecuteDeleteAsync(ct);
        return deleted > 0 ? Result.Success() : Result.Failure(new Error(ErrorCodes.NotFound));
    }

    private static async Task<Error?> CheckReferencesAsync(AppDbContext db, Employee employee, CancellationToken ct)
    {
        if (!await db.Positions.AnyAsync(p => p.Id == employee.PositionId, ct))
        {
            return new Error(ErrorCodes.NotFound, nameof(Employee.PositionId));
        }

        var emailTaken = await db.Employees.AnyAsync(
            e => e.Email == employee.Email && e.PersonalId != employee.PersonalId, ct);
        return emailTaken ? new Error(ErrorCodes.Duplicate, nameof(Employee.Email)) : null;
    }

    private static void Normalize(Employee employee)
    {
        employee.PersonalId = employee.PersonalId.Trim();
        employee.FirstName = employee.FirstName.Trim();
        employee.LastName = employee.LastName.Trim();
        employee.Address = employee.Address.Trim();
        employee.Phone = employee.Phone.Trim();
        employee.Email = UserService.NormalizeEmail(employee.Email);
    }

    private DateOnly Today() => DateOnly.FromDateTime(clock.GetLocalNow().DateTime);
}
