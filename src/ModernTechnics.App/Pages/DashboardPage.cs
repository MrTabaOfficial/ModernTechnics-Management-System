using System.Globalization;
using ModernTechnics.App.Controls;
using ModernTechnics.App.Localization;
using ModernTechnics.App.Ui;
using ModernTechnics.Core.Services;

namespace ModernTechnics.App.Pages;

internal sealed class DashboardPage(IDashboardService dashboard) : PageBase
{
    private readonly StatCard _employees = new(L.T("Dashboard.Employees"), Glyphs.People, Theme.Accent, Theme.AccentSoft);
    private readonly StatCard _payroll = new(L.T("Dashboard.Payroll"), Glyphs.Money, Theme.Info, Theme.InfoSoft);
    private readonly StatCard _revenue = new(L.T("Dashboard.Revenue"), Glyphs.Chart, Theme.Success, Theme.SuccessSoft);
    private readonly StatCard _stock = new(L.T("Dashboard.Stock"), Glyphs.Package, Theme.Warning, Theme.WarningSoft);
    private readonly BarChart _revenueChart = new() { EmptyText = L.T("Dashboard.NoSales") };
    private readonly RankChart _headcount = new();
    private readonly AppGrid _lowStock = new();

    protected override void Build()
    {
        var gap = Theme.Px(16);

        var stats = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 4, RowCount = 1, Margin = Padding.Empty };
        StatCard[] cards = [_employees, _payroll, _revenue, _stock];
        for (var i = 0; i < cards.Length; i++)
        {
            stats.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25));
            cards[i].Dock = DockStyle.Fill;
            cards[i].Margin = new Padding(0, 0, i == cards.Length - 1 ? 0 : gap, gap);
            stats.Controls.Add(cards[i], i, 0);
        }

        var revenueCard = new Card { Title = L.T("Dashboard.RevenueChart"), Dock = DockStyle.Fill, Margin = new Padding(0, 0, gap, gap) };
        revenueCard.Controls.Add(_revenueChart);

        var headcountCard = new Card { Title = L.T("Dashboard.Headcount"), Dock = DockStyle.Fill, Margin = new Padding(0, 0, 0, gap) };
        headcountCard.Controls.Add(_headcount);

        var charts = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1, Margin = Padding.Empty };
        charts.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 58));
        charts.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 42));
        charts.Controls.Add(revenueCard, 0, 0);
        charts.Controls.Add(headcountCard, 1, 0);

        _lowStock.AddColumn(L.T("Field.Name"), 220);
        _lowStock.AddColumn(L.T("Field.Manufacturer"), 120);
        _lowStock.AddColumn(L.T("Store.OnShelf"), 80, alignRight: true);
        _lowStock.AddColumn(L.T("Field.WarehouseStock"), 90, alignRight: true);
        _lowStock.AddColumn(L.T("Dashboard.Advice"), 200);
        _lowStock.DefaultCellStyle.SelectionBackColor = Theme.Card;
        _lowStock.TabStop = false;
        _lowStock.RowTemplate.Height = Theme.Px(36);
        _lowStock.ColumnHeadersHeight = Theme.Px(38);

        var lowStockCard = new Card
        {
            Title = L.T("Dashboard.LowStock", DashboardSummary.LowStockThreshold),
            Dock = DockStyle.Fill,
            Margin = Padding.Empty,
        };
        lowStockCard.Padding = Theme.Pad(12, 52, 12, 10);
        lowStockCard.Controls.Add(_lowStock);

        var root = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 3 };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, Theme.Px(124) + gap));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 50));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 50));
        root.Controls.Add(stats, 0, 0);
        root.Controls.Add(charts, 0, 1);
        root.Controls.Add(lowStockCard, 0, 2);
        Controls.Add(root);
    }

    protected override async Task LoadAsync()
    {
        var summary = await dashboard.GetSummaryAsync();
        if (IsDisposed)
        {
            return;
        }

        _employees.Show(
            summary.EmployeeCount.ToString("N0", CultureInfo.CurrentCulture),
            L.T("Dashboard.EmployeesCaption", summary.HeadcountByPosition.Count, summary.OpenApplications));
        _payroll.Show(
            L.MoneyRounded(summary.MonthlyPayroll),
            L.T("Dashboard.PayrollCaption", L.MoneyRounded(summary.EmployeeCount == 0 ? 0 : summary.MonthlyPayroll / summary.EmployeeCount)));
        _revenue.Show(L.MoneyRounded(summary.RevenueLast30Days), L.T("Dashboard.RevenueCaption", summary.CustomerCount));
        _stock.Show(
            summary.UnitsInStock.ToString("N0", CultureInfo.CurrentCulture),
            L.T("Dashboard.StockCaption", summary.ProductCount, summary.LowStock.Count));

        _revenueChart.Show([.. summary.RevenueByDay.Select(day => new ChartPoint(
            day.Day.ToString("ddd d", L.Culture), day.Amount, Compact(day.Amount)))]);

        _headcount.Show([.. summary.HeadcountByPosition.Select(h => new ChartPoint(
            h.Position, h.Count, h.Count.ToString(CultureInfo.CurrentCulture)))]);

        _lowStock.Rows.Clear();
        foreach (var product in summary.LowStock)
        {
            var advice = product.WarehouseStock > 0 ? L.T("Dashboard.AdviceTransfer") : L.T("Dashboard.AdviceReorder");
            var row = _lowStock.Rows[_lowStock.Rows.Add(
                product.Name, product.Manufacturer, product.StoreStock, product.WarehouseStock, advice)];
            var color = product.StoreStock == 0 ? Theme.Danger : Theme.Warning;
            row.Cells[2].Style.ForeColor = color;
            row.Cells[2].Style.SelectionForeColor = color;
            row.Cells[2].Style.Font = Theme.BodyBold;
            row.Cells[4].Style.ForeColor = Theme.TextMuted;
            row.Cells[4].Style.SelectionForeColor = Theme.TextMuted;
        }

        _lowStock.ClearSelection();
    }

    /// <summary>12 345 → "12.3k" so labels fit above narrow bars.</summary>
    private static string Compact(decimal amount) => amount >= 1000
        ? (amount / 1000m).ToString("0.#", CultureInfo.CurrentCulture) + "k"
        : amount.ToString("0", CultureInfo.CurrentCulture);
}
