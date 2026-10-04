using ModernTechnics.App.Controls;
using ModernTechnics.App.Forms;
using ModernTechnics.App.Localization;
using ModernTechnics.App.Ui;
using ModernTechnics.Core.Domain;
using ModernTechnics.Core.Services;

namespace ModernTechnics.App.Pages;

internal sealed class WarehousePage(IInventoryService inventory, IShell shell) : ListPage<Product>(shell)
{
    protected override Func<Task>? DefaultAction => () => EditAsync(Selected);

    protected override IReadOnlyList<GridColumn<Product>> DefineColumns() =>
    [
        new("#", p => p.Id, 35, AlignRight: true),
        new(L.Field(nameof(Product.Name)), p => p.Name, 190),
        new(L.Field(nameof(Product.Manufacturer)), p => p.Manufacturer, 100),
        new(L.Field(nameof(Product.ReleaseYear)), p => p.ReleaseYear, 60, AlignRight: true),
        new(L.Field(nameof(Product.Price)), p => p.Price, 85, AlignRight: true, Format: "N2"),
        new(L.Field(nameof(Product.WarehouseStock)), p => p.WarehouseStock, 85, AlignRight: true,
            Color: p => p.WarehouseStock == 0 ? Theme.Danger : null, Emphasis: true),
        new(L.Field(nameof(Product.StoreStock)), p => p.StoreStock, 85, AlignRight: true),
    ];

    protected override void DefineActions()
    {
        AddAction(L.T("Warehouse.Receive"), Glyphs.Download, ReceiveAsync, needsSelection: true);
        AddAction(L.T("Warehouse.Transfer"), Glyphs.Forward, TransferAsync, needsSelection: true);
        AddAction(L.T("Action.Edit"), Glyphs.Edit, () => EditAsync(Selected), needsSelection: true);
        AddAction(L.T("Action.Delete"), Glyphs.Delete, DeleteAsync, ButtonKind.Danger, needsSelection: true);
        AddAction(L.T("Warehouse.Add"), Glyphs.Add, () => EditAsync(null), ButtonKind.Primary);
    }

    protected override Task<IReadOnlyList<Product>> FetchAsync(string? search) => inventory.ListAsync(search);

    protected override string FooterText(IReadOnlyList<Product> items) =>
        L.T("Warehouse.Footer", items.Count, items.Sum(p => p.WarehouseStock), items.Sum(p => p.StoreStock));

    private async Task EditAsync(Product? existing)
    {
        using var dialog = new FormDialog(L.T(existing is null ? "Warehouse.Add" : "Warehouse.Edit"), columns: 2);
        var name = dialog.AddText(nameof(Product.Name), existing?.Name);
        var manufacturer = dialog.AddText(nameof(Product.Manufacturer), existing?.Manufacturer);
        var year = dialog.AddNumber(nameof(Product.ReleaseYear), existing?.ReleaseYear ?? DateTime.Today.Year, 2100, minimum: 1970);
        var price = dialog.AddNumber(nameof(Product.Price), existing?.Price ?? 0, 1_000_000, decimals: 2);
        var warehouseStock = dialog.AddNumber(nameof(Product.WarehouseStock), existing?.WarehouseStock ?? 0, 100_000);
        var storeStock = dialog.AddNumber(nameof(Product.StoreStock), existing?.StoreStock ?? 0, 100_000);

        dialog.Submit = () =>
        {
            var product = new Product
            {
                Id = existing?.Id ?? 0,
                Name = name.Text,
                Manufacturer = manufacturer.Text,
                ReleaseYear = (int)year.Value,
                Price = price.Value,
                WarehouseStock = (int)warehouseStock.Value,
                StoreStock = (int)storeStock.Value,
            };
            return existing is null ? inventory.CreateAsync(product) : inventory.UpdateAsync(product);
        };

        if (dialog.ShowDialog(this) == DialogResult.OK)
        {
            Shell.Toast(L.T("Toast.Saved"));
            await ReloadAsync();
        }
    }

    private async Task ReceiveAsync()
    {
        if (Selected is not { } product)
        {
            return;
        }

        using var dialog = new FormDialog(L.T("Warehouse.Receive"), L.T("Warehouse.Receive"))
        {
            Description = L.T("Warehouse.ReceiveHint", product.Name, product.WarehouseStock),
        };
        var quantity = dialog.AddNumber(nameof(Order.Quantity), 10, 100_000, minimum: 1);
        dialog.Submit = () => inventory.ReceiveAsync(product.Id, (int)quantity.Value);

        if (dialog.ShowDialog(this) == DialogResult.OK)
        {
            Shell.Toast(L.T("Toast.Received", (int)quantity.Value, product.Name));
            await ReloadAsync();
        }
    }

    private async Task TransferAsync()
    {
        if (Selected is not { } product)
        {
            return;
        }

        using var dialog = new FormDialog(L.T("Warehouse.Transfer"), L.T("Warehouse.Transfer"))
        {
            Description = L.T("Warehouse.TransferHint", product.Name, product.WarehouseStock, product.StoreStock),
        };
        var quantity = dialog.AddNumber(nameof(Order.Quantity), Math.Min(1, product.WarehouseStock), 100_000, minimum: 1);
        dialog.Submit = () => inventory.TransferToStoreAsync(product.Id, (int)quantity.Value);

        if (dialog.ShowDialog(this) == DialogResult.OK)
        {
            Shell.Toast(L.T("Toast.Transferred", (int)quantity.Value, product.Name));
            await ReloadAsync();
        }
    }

    private async Task DeleteAsync()
    {
        if (Selected is not { } product || !Dialogs.Confirm(this, L.T("Confirm.Delete", product.Name)))
        {
            return;
        }

        var result = await inventory.DeleteAsync(product.Id);
        if (result.IsFailure)
        {
            Dialogs.Error(this, L.Errors(result));
        }
        else
        {
            Shell.Toast(L.T("Toast.Deleted"));
        }

        await ReloadAsync();
    }
}
