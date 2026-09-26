using ModernTechnics.Core.Common;
using ModernTechnics.Core.Domain;
using ModernTechnics.Core.Services;
using ModernTechnics.Tests.Support;

namespace ModernTechnics.Tests.Services;

public class StockAndSalesTests
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private static async Task<Product> FindAsync(TestDatabase db, string name) =>
        (await db.Inventory.ListAsync(name, Ct)).Single();

    [Fact]
    public async Task Transfer_moves_units_from_warehouse_to_store()
    {
        await using var db = await TestDatabase.CreateAsync();
        var before = await FindAsync(db, "PlayStation");

        ResultAssert.Succeeded(await db.Inventory.TransferToStoreAsync(before.Id, 4, Ct));

        var after = await FindAsync(db, "PlayStation");
        Assert.Equal(before.WarehouseStock - 4, after.WarehouseStock);
        Assert.Equal(before.StoreStock + 4, after.StoreStock);
        Assert.Equal(before.TotalStock, after.TotalStock);
    }

    [Fact]
    public async Task Transfer_of_more_than_the_warehouse_holds_changes_nothing()
    {
        await using var db = await TestDatabase.CreateAsync();
        var before = await FindAsync(db, "ROG Strix");

        var result = await db.Inventory.TransferToStoreAsync(before.Id, before.WarehouseStock + 1, Ct);

        ResultAssert.Failed(result, ErrorCodes.InsufficientStock);
        Assert.Equal(before.WarehouseStock, result.Errors[0].Argument);
        var after = await FindAsync(db, "ROG Strix");
        Assert.Equal((before.WarehouseStock, before.StoreStock), (after.WarehouseStock, after.StoreStock));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-3)]
    public async Task Stock_movements_require_a_positive_quantity(int quantity)
    {
        await using var db = await TestDatabase.CreateAsync();
        var product = await FindAsync(db, "Pixel");

        ResultAssert.Failed(await db.Inventory.TransferToStoreAsync(product.Id, quantity, Ct), ErrorCodes.MustBePositive);
        ResultAssert.Failed(await db.Inventory.ReceiveAsync(product.Id, quantity, Ct), ErrorCodes.MustBePositive);
        ResultAssert.Failed(await db.Sales.PlaceOrderAsync(product.Id, quantity, ct: Ct), ErrorCodes.MustBePositive);
    }

    [Fact]
    public async Task Receive_adds_a_delivery_to_the_warehouse()
    {
        await using var db = await TestDatabase.CreateAsync();
        var before = await FindAsync(db, "Pixel");

        ResultAssert.Succeeded(await db.Inventory.ReceiveAsync(before.Id, 20, Ct));
        ResultAssert.Failed(await db.Inventory.ReceiveAsync(9_999, 20, Ct), ErrorCodes.NotFound);

        Assert.Equal(before.WarehouseStock + 20, (await FindAsync(db, "Pixel")).WarehouseStock);
    }

    [Fact]
    public async Task Order_reduces_store_stock_and_captures_the_price_at_sale_time()
    {
        await using var db = await TestDatabase.CreateAsync();
        var product = await FindAsync(db, "AirPods");
        var customer = (await db.Customers.ListAsync(ct: Ct))[0];

        var result = await db.Sales.PlaceOrderAsync(product.Id, 3, customer.PersonalId, Ct);

        ResultAssert.Succeeded(result);
        Assert.Equal(product.Price * 3, result.Value.Total);
        Assert.Equal(db.Clock.Now.UtcDateTime, result.Value.PlacedAtUtc);

        // A later price change must not rewrite history.
        var changed = await FindAsync(db, "AirPods");
        Assert.Equal(product.StoreStock - 3, changed.StoreStock);
        Assert.Equal(product.WarehouseStock, changed.WarehouseStock);
        changed.Price = 1m;
        ResultAssert.Succeeded(await db.Inventory.UpdateAsync(changed, Ct));

        var saved = (await db.Sales.ListOrdersAsync(ct: Ct)).Single(o => o.Id == result.Value.Id);
        Assert.Equal(product.Price, saved.UnitPrice);
        Assert.Equal(customer.PersonalId, saved.Customer!.PersonalId);
    }

    [Fact]
    public async Task Order_for_more_than_the_store_holds_is_refused_and_nothing_is_recorded()
    {
        await using var db = await TestDatabase.CreateAsync();
        var product = await FindAsync(db, "Galaxy Tab");
        var ordersBefore = (await db.Sales.ListOrdersAsync(ct: Ct)).Count;
        Assert.Equal(0, product.StoreStock);
        Assert.True(product.WarehouseStock > 0);

        var result = await db.Sales.PlaceOrderAsync(product.Id, 1, ct: Ct);

        ResultAssert.Failed(result, ErrorCodes.InsufficientStock);
        Assert.Equal(ordersBefore, (await db.Sales.ListOrdersAsync(ct: Ct)).Count);
        Assert.Equal(0, (await FindAsync(db, "Galaxy Tab")).StoreStock);
    }

    [Fact]
    public async Task Selling_the_last_units_twice_only_succeeds_once()
    {
        await using var db = await TestDatabase.CreateAsync();
        var product = await FindAsync(db, "ROG Strix");
        Assert.Equal(1, product.StoreStock);

        ResultAssert.Succeeded(await db.Sales.PlaceOrderAsync(product.Id, 1, ct: Ct));
        ResultAssert.Failed(await db.Sales.PlaceOrderAsync(product.Id, 1, ct: Ct), ErrorCodes.InsufficientStock);
    }

    [Fact]
    public async Task Order_rejects_unknown_product_or_customer_without_touching_stock()
    {
        await using var db = await TestDatabase.CreateAsync();
        var product = await FindAsync(db, "Pixel");

        ResultAssert.Failed(await db.Sales.PlaceOrderAsync(9_999, 1, ct: Ct), ErrorCodes.NotFound, nameof(Order.ProductId));
        ResultAssert.Failed(
            await db.Sales.PlaceOrderAsync(product.Id, 1, "00000000000", Ct),
            ErrorCodes.NotFound, nameof(Order.CustomerPersonalId));

        Assert.Equal(product.StoreStock, (await FindAsync(db, "Pixel")).StoreStock);
    }

    [Fact]
    public async Task Product_with_sales_history_cannot_be_deleted()
    {
        await using var db = await TestDatabase.CreateAsync();
        var sold = await FindAsync(db, "PlayStation");
        var neverSold = await FindAsync(db, "Galaxy Tab");

        ResultAssert.Failed(await db.Inventory.DeleteAsync(sold.Id, Ct), ErrorCodes.InUse);
        ResultAssert.Succeeded(await db.Inventory.DeleteAsync(neverSold.Id, Ct));
    }

    [Fact]
    public async Task Product_create_and_update_are_validated()
    {
        await using var db = await TestDatabase.CreateAsync();
        var product = new Product { Name = "Kindle Paperwhite", Manufacturer = "Amazon", ReleaseYear = 2024, Price = 549m, WarehouseStock = 10 };

        ResultAssert.Succeeded(await db.Inventory.CreateAsync(product, Ct));

        var saved = await FindAsync(db, "Kindle");
        saved.Price = -5m;
        ResultAssert.Failed(await db.Inventory.UpdateAsync(saved, Ct), ErrorCodes.MustNotBeNegative, nameof(Product.Price));
        Assert.Equal(549m, (await FindAsync(db, "Kindle")).Price);
    }

    [Fact]
    public async Task Dashboard_summarises_the_seeded_shop()
    {
        await using var db = await TestDatabase.CreateAsync();
        var employees = await db.Employees.ListAsync(ct: Ct);
        var products = await db.Inventory.ListAsync(ct: Ct);

        var summary = await db.Dashboard.GetSummaryAsync(Ct);

        Assert.Equal(employees.Count, summary.EmployeeCount);
        Assert.Equal(employees.Sum(e => e.Salary), summary.MonthlyPayroll);
        Assert.Equal(6, summary.CustomerCount);
        Assert.Equal(3, summary.OpenApplications);
        Assert.Equal(products.Count, summary.ProductCount);
        Assert.Equal(products.Sum(p => p.TotalStock), summary.UnitsInStock);
        Assert.Equal(employees.Count, summary.HeadcountByPosition.Sum(h => h.Count));
        Assert.Equal("Sales Consultant", summary.HeadcountByPosition[0].Position);
        Assert.All(summary.LowStock, p => Assert.True(p.StoreStock <= DashboardSummary.LowStockThreshold));
        Assert.Equal("Galaxy Tab S9 FE", summary.LowStock[0].Name);
    }

    [Fact]
    public async Task Dashboard_revenue_chart_covers_the_last_seven_days_ending_today()
    {
        await using var db = await TestDatabase.CreateAsync();
        var today = DateOnly.FromDateTime(db.Clock.Now.DateTime);
        var orders = await db.Sales.ListOrdersAsync(ct: Ct);

        var summary = await db.Dashboard.GetSummaryAsync(Ct);

        Assert.Equal(DashboardSummary.RevenueChartDays, summary.RevenueByDay.Count);
        Assert.Equal(today, summary.RevenueByDay[^1].Day);
        Assert.Equal(today.AddDays(-6), summary.RevenueByDay[0].Day);
        Assert.Equal(orders.Sum(o => o.Total), summary.RevenueLast30Days);
        Assert.True(summary.RevenueByDay.Sum(d => d.Amount) < summary.RevenueLast30Days);
    }

    [Fact]
    public async Task Dashboard_revenue_grows_by_exactly_the_value_of_a_new_sale()
    {
        await using var db = await TestDatabase.CreateAsync();
        var product = await FindAsync(db, "Redmi");
        var before = await db.Dashboard.GetSummaryAsync(Ct);

        ResultAssert.Succeeded(await db.Sales.PlaceOrderAsync(product.Id, 2, ct: Ct));

        var after = await db.Dashboard.GetSummaryAsync(Ct);
        Assert.Equal(before.RevenueLast30Days + (product.Price * 2), after.RevenueLast30Days);
        Assert.Equal(before.RevenueByDay[^1].Amount + (product.Price * 2), after.RevenueByDay[^1].Amount);
        Assert.Equal(before.UnitsInStock - 2, after.UnitsInStock);
    }
}
