using ModernTechnics.App.Controls;
using ModernTechnics.App.Forms;
using ModernTechnics.App.Localization;
using ModernTechnics.App.Ui;
using ModernTechnics.Core.Common;
using ModernTechnics.Core.Domain;
using ModernTechnics.Core.Services;

namespace ModernTechnics.App.Pages;

/// <summary>The shop floor: what is on the shelves, and the till.</summary>
internal sealed class StorePage(
    IInventoryService inventory, ISalesService sales, ICustomerService customers, IShell shell)
    : ListPage<Product>(shell)
{
    protected override Func<Task>? DefaultAction => SellAsync;

    protected override IReadOnlyList<GridColumn<Product>> DefineColumns() =>
    [
        new(L.Field(nameof(Product.Name)), p => p.Name, 200),
        new(L.Field(nameof(Product.Manufacturer)), p => p.Manufacturer, 110),
        new(L.Field(nameof(Product.ReleaseYear)), p => p.ReleaseYear, 60, AlignRight: true),
        new(L.Field(nameof(Product.Price)), p => p.Price, 90, AlignRight: true, Format: "N2"),
        new(L.T("Store.OnShelf"), p => p.StoreStock, 80, AlignRight: true, Color: p => StockColor(p.StoreStock), Emphasis: true),
        new(L.T("Store.Availability"), p => Availability(p.StoreStock), 100, Color: p => StockColor(p.StoreStock), Emphasis: true),
    ];

    protected override void DefineActions() =>
        AddAction(L.T("Store.Sell"), Glyphs.Cart, SellAsync, ButtonKind.Primary, needsSelection: true);

    protected override Task<IReadOnlyList<Product>> FetchAsync(string? search) => inventory.ListAsync(search);

    protected override string FooterText(IReadOnlyList<Product> items) =>
        L.T("Store.Footer", items.Count, items.Count(p => p.StoreStock == 0));

    private static Color? StockColor(int stock) => stock switch
    {
        0 => Theme.Danger,
        <= DashboardSummary.LowStockThreshold => Theme.Warning,
        _ => Theme.Success,
    };

    private static string Availability(int stock) => stock switch
    {
        0 => L.T("Store.SoldOut"),
        <= DashboardSummary.LowStockThreshold => L.T("Store.Low"),
        _ => L.T("Store.InStock"),
    };

    private async Task SellAsync()
    {
        if (Selected is not { } product)
        {
            return;
        }

        if (product.StoreStock == 0)
        {
            Dialogs.Error(this, L.Error(new Error(ErrorCodes.InsufficientStock, nameof(Order.Quantity), 0)));
            return;
        }

        var registered = await customers.ListAsync();
        Customer?[] buyers = [null, .. registered];

        using var dialog = new FormDialog(L.T("Store.Sell"), L.T("Store.Confirm"))
        {
            Description = L.T("Store.SellHint", product.Name, L.Money(product.Price), product.StoreStock),
        };
        var quantity = dialog.AddNumber(nameof(Order.Quantity), 1, product.StoreStock, minimum: 1);
        var customer = dialog.AddChoice(
            nameof(Order.CustomerPersonalId), buyers,
            c => c is null ? L.T("Store.WalkIn") : $"{c.FullName} · {c.PersonalId}");

        Order? order = null;
        dialog.Submit = async () =>
        {
            var result = await sales.PlaceOrderAsync(product.Id, (int)quantity.Value, customer.Value?.PersonalId);
            order = result.IsSuccess ? result.Value : null;
            return result;
        };

        if (dialog.ShowDialog(this) == DialogResult.OK && order is not null)
        {
            Shell.Toast(L.T("Toast.Sold", order.Quantity, product.Name, L.Money(order.Total)));
        }

        await ReloadAsync();
    }
}
