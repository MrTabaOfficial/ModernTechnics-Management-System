using Microsoft.EntityFrameworkCore;
using ModernTechnics.Core.Common;
using ModernTechnics.Core.Domain;
using ModernTechnics.Core.Services;
using ModernTechnics.Infrastructure.Data;

namespace ModernTechnics.Infrastructure.Services;

public sealed class SalesService(IDbContextFactory<AppDbContext> contextFactory, TimeProvider clock)
    : ISalesService
{
    public async Task<IReadOnlyList<Order>> ListOrdersAsync(string? search = null, CancellationToken ct = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(ct);
        var query = db.Orders.AsNoTracking().Include(o => o.Product).Include(o => o.Customer).AsQueryable();

        if (Search.ToPattern(search) is { } pattern)
        {
            query = query.Where(o =>
                EF.Functions.Like(o.Product!.Name, pattern, Search.Escape)
                || EF.Functions.Like(o.CustomerPersonalId!, pattern, Search.Escape)
                || EF.Functions.Like(o.Customer!.FirstName, pattern, Search.Escape)
                || EF.Functions.Like(o.Customer!.LastName, pattern, Search.Escape));
        }

        return await query.OrderByDescending(o => o.PlacedAtUtc).ThenByDescending(o => o.Id).ToListAsync(ct);
    }

    public async Task<Result<Order>> PlaceOrderAsync(
        int productId, int quantity, string? customerPersonalId = null, CancellationToken ct = default)
    {
        if (quantity <= 0)
        {
            return Result.Failure<Order>(new Error(ErrorCodes.MustBePositive, nameof(Order.Quantity)));
        }

        customerPersonalId = string.IsNullOrWhiteSpace(customerPersonalId) ? null : customerPersonalId.Trim();

        await using var db = await contextFactory.CreateDbContextAsync(ct);
        await using var transaction = await db.Database.BeginTransactionAsync(ct);

        var product = await db.Products.AsNoTracking().SingleOrDefaultAsync(p => p.Id == productId, ct);
        if (product is null)
        {
            return Result.Failure<Order>(new Error(ErrorCodes.NotFound, nameof(Order.ProductId)));
        }

        if (customerPersonalId is not null
            && !await db.Customers.AnyAsync(c => c.PersonalId == customerPersonalId, ct))
        {
            return Result.Failure<Order>(new Error(ErrorCodes.NotFound, nameof(Order.CustomerPersonalId)));
        }

        // Conditional UPDATE so two tills can never sell the same last unit.
        var updated = await db.Products
            .Where(p => p.Id == productId && p.StoreStock >= quantity)
            .ExecuteUpdateAsync(s => s.SetProperty(p => p.StoreStock, p => p.StoreStock - quantity), ct);
        if (updated == 0)
        {
            return Result.Failure<Order>(
                new Error(ErrorCodes.InsufficientStock, nameof(Order.Quantity), product.StoreStock));
        }

        var order = new Order
        {
            ProductId = productId,
            CustomerPersonalId = customerPersonalId,
            Quantity = quantity,
            UnitPrice = product.Price,
            PlacedAtUtc = clock.GetUtcNow().UtcDateTime,
        };
        db.Orders.Add(order);
        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);

        return Result.Success(order);
    }
}
