using Microsoft.EntityFrameworkCore;
using ModernTechnics.Core.Common;
using ModernTechnics.Core.Domain;
using ModernTechnics.Core.Services;
using ModernTechnics.Core.Validation;
using ModernTechnics.Infrastructure.Data;

namespace ModernTechnics.Infrastructure.Services;

public sealed class CustomerService(IDbContextFactory<AppDbContext> contextFactory, TimeProvider clock)
    : ICustomerService
{
    public async Task<IReadOnlyList<Customer>> ListAsync(string? search = null, CancellationToken ct = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(ct);
        var query = db.Customers.AsNoTracking();

        if (Search.ToPattern(search) is { } pattern)
        {
            query = query.Where(c =>
                EF.Functions.Like(c.PersonalId, pattern, Search.Escape)
                || EF.Functions.Like(c.FirstName, pattern, Search.Escape)
                || EF.Functions.Like(c.LastName, pattern, Search.Escape)
                || EF.Functions.Like(c.Phone, pattern, Search.Escape));
        }

        return await query.OrderBy(c => c.LastName).ThenBy(c => c.FirstName).ToListAsync(ct);
    }

    public async Task<Result> CreateAsync(Customer customer, CancellationToken ct = default)
    {
        Normalize(customer);
        var validation = EntityValidators.Validate(customer, Today());
        if (validation.IsFailure)
        {
            return validation;
        }

        await using var db = await contextFactory.CreateDbContextAsync(ct);
        if (await db.Customers.AnyAsync(c => c.PersonalId == customer.PersonalId, ct))
        {
            return Result.Failure(new Error(ErrorCodes.Duplicate, nameof(Customer.PersonalId)));
        }

        db.Customers.Add(customer);
        await db.SaveChangesAsync(ct);
        return Result.Success();
    }

    public async Task<Result> UpdateAsync(Customer customer, CancellationToken ct = default)
    {
        Normalize(customer);
        var validation = EntityValidators.Validate(customer, Today());
        if (validation.IsFailure)
        {
            return validation;
        }

        await using var db = await contextFactory.CreateDbContextAsync(ct);
        var existing = await db.Customers.FindAsync([customer.PersonalId], ct);
        if (existing is null)
        {
            return Result.Failure(new Error(ErrorCodes.NotFound));
        }

        existing.FirstName = customer.FirstName;
        existing.LastName = customer.LastName;
        existing.BirthDate = customer.BirthDate;
        existing.Address = customer.Address;
        existing.Phone = customer.Phone;
        await db.SaveChangesAsync(ct);
        return Result.Success();
    }

    public async Task<Result> DeleteAsync(string personalId, CancellationToken ct = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(ct);
        var customer = await db.Customers.FindAsync([personalId], ct);
        if (customer is null)
        {
            return Result.Failure(new Error(ErrorCodes.NotFound));
        }

        // Past orders are kept for the books; the foreign key is cleared by the database.
        db.Customers.Remove(customer);
        await db.SaveChangesAsync(ct);
        return Result.Success();
    }

    private static void Normalize(Customer customer)
    {
        customer.PersonalId = customer.PersonalId.Trim();
        customer.FirstName = customer.FirstName.Trim();
        customer.LastName = customer.LastName.Trim();
        customer.Address = customer.Address.Trim();
        customer.Phone = customer.Phone.Trim();
    }

    private DateOnly Today() => DateOnly.FromDateTime(clock.GetLocalNow().DateTime);
}
