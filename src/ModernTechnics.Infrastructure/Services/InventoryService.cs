using Microsoft.EntityFrameworkCore;
using ModernTechnics.Core.Common;
using ModernTechnics.Core.Domain;
using ModernTechnics.Core.Services;
using ModernTechnics.Core.Validation;
using ModernTechnics.Infrastructure.Data;

namespace ModernTechnics.Infrastructure.Services;

public sealed class InventoryService(IDbContextFactory<AppDbContext> contextFactory, TimeProvider clock)
    : IInventoryService
{
    public async Task<IReadOnlyList<Product>> ListAsync(string? search = null, CancellationToken ct = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(ct);
        var query = db.Products.AsNoTracking();

        if (Search.ToPattern(search) is { } pattern)
        {
            query = query.Where(p =>
                EF.Functions.Like(p.Name, pattern, Search.Escape)
                || EF.Functions.Like(p.Manufacturer, pattern, Search.Escape));
        }

        return await query.OrderBy(p => p.Name).ToListAsync(ct);
    }

    public async Task<Result> CreateAsync(Product product, CancellationToken ct = default)
    {
        Normalize(product);
        var validation = EntityValidators.Validate(product, CurrentYear());
        if (validation.IsFailure)
        {
            return validation;
        }

        await using var db = await contextFactory.CreateDbContextAsync(ct);
        product.Id = 0;
        db.Products.Add(product);
        await db.SaveChangesAsync(ct);
        return Result.Success();
    }

    public async Task<Result> UpdateAsync(Product product, CancellationToken ct = default)
    {
        Normalize(product);
        var validation = EntityValidators.Validate(product, CurrentYear());
        if (validation.IsFailure)
        {
            return validation;
        }

        await using var db = await contextFactory.CreateDbContextAsync(ct);
        var existing = await db.Products.FindAsync([product.Id], ct);
        if (existing is null)
        {
            return Result.Failure(new Error(ErrorCodes.NotFound));
        }

        existing.Name = product.Name;
        existing.Manufacturer = product.Manufacturer;
        existing.ReleaseYear = product.ReleaseYear;
        existing.Price = product.Price;
        existing.WarehouseStock = product.WarehouseStock;
        existing.StoreStock = product.StoreStock;
        await db.SaveChangesAsync(ct);
        return Result.Success();
    }

    public async Task<Result> DeleteAsync(int id, CancellationToken ct = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(ct);
        var product = await db.Products.FindAsync([id], ct);
        if (product is null)
        {
            return Result.Failure(new Error(ErrorCodes.NotFound));
        }

        if (await db.Orders.AnyAsync(o => o.ProductId == id, ct))
        {
            return Result.Failure(new Error(ErrorCodes.InUse));
        }

        db.Products.Remove(product);
        await db.SaveChangesAsync(ct);
        return Result.Success();
    }

    public async Task<Result> ReceiveAsync(int productId, int quantity, CancellationToken ct = default)
    {
        if (quantity <= 0)
        {
            return Result.Failure(new Error(ErrorCodes.MustBePositive, nameof(Order.Quantity)));
        }

        await using var db = await contextFactory.CreateDbContextAsync(ct);
        var updated = await db.Products
            .Where(p => p.Id == productId)
            .ExecuteUpdateAsync(s => s.SetProperty(p => p.WarehouseStock, p => p.WarehouseStock + quantity), ct);

        return updated > 0 ? Result.Success() : Result.Failure(new Error(ErrorCodes.NotFound));
    }

    public async Task<Result> TransferToStoreAsync(int productId, int quantity, CancellationToken ct = default)
    {
        if (quantity <= 0)
        {
            return Result.Failure(new Error(ErrorCodes.MustBePositive, nameof(Order.Quantity)));
        }

        await using var db = await contextFactory.CreateDbContextAsync(ct);

        // One conditional UPDATE: the stock check and the move cannot be interleaved by another writer.
        var updated = await db.Products
            .Where(p => p.Id == productId && p.WarehouseStock >= quantity)
            .ExecuteUpdateAsync(
                s => s.SetProperty(p => p.WarehouseStock, p => p.WarehouseStock - quantity)
                      .SetProperty(p => p.StoreStock, p => p.StoreStock + quantity),
                ct);

        if (updated > 0)
        {
            return Result.Success();
        }

        var available = await db.Products
            .Where(p => p.Id == productId)
            .Select(p => (int?)p.WarehouseStock)
            .SingleOrDefaultAsync(ct);

        return available is null
            ? Result.Failure(new Error(ErrorCodes.NotFound))
            : Result.Failure(new Error(ErrorCodes.InsufficientStock, nameof(Order.Quantity), available));
    }

    private static void Normalize(Product product)
    {
        product.Name = product.Name.Trim();
        product.Manufacturer = product.Manufacturer.Trim();
    }

    private int CurrentYear() => clock.GetLocalNow().Year;
}
