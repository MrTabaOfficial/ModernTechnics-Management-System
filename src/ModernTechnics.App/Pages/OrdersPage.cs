using ModernTechnics.App.Localization;
using ModernTechnics.App.Ui;
using ModernTechnics.Core.Domain;
using ModernTechnics.Core.Services;

namespace ModernTechnics.App.Pages;

internal sealed class OrdersPage(ISalesService sales, IShell shell) : ListPage<Order>(shell)
{
    protected override IReadOnlyList<GridColumn<Order>> DefineColumns() =>
    [
        new("#", o => o.Id, 35, AlignRight: true),
        new(L.T("Field.PlacedAt"), o => o.PlacedAtUtc.ToLocalTime(), 150, Format: "g"),
        new(L.Field(nameof(Order.ProductId)), o => o.Product?.Name, 190),
        new(L.Field(nameof(Order.CustomerPersonalId)), o => o.Customer?.FullName ?? L.T("Store.WalkIn"), 140,
            Color: o => o.Customer is null ? Theme.TextMuted : null),
        new(L.Field(nameof(Order.Quantity)), o => o.Quantity, 60, AlignRight: true),
        new(L.T("Field.UnitPrice"), o => o.UnitPrice, 85, AlignRight: true, Format: "N2"),
        new(L.T("Field.Total"), o => o.Total, 90, AlignRight: true, Format: "N2"),
    ];

    protected override void DefineActions()
    {
    }

    protected override Task<IReadOnlyList<Order>> FetchAsync(string? search) => sales.ListOrdersAsync(search);

    protected override string FooterText(IReadOnlyList<Order> items) =>
        L.T("Orders.Footer", items.Count, items.Sum(o => o.Quantity), L.Money(items.Sum(o => o.Total)));
}
