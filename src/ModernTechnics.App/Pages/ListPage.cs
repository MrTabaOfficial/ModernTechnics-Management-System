using ModernTechnics.App.Controls;
using ModernTechnics.App.Localization;
using ModernTechnics.App.Ui;

namespace ModernTechnics.App.Pages;

/// <summary>
/// One grid column. <see cref="Value"/> returns the raw value so sorting is numeric or
/// chronological; <see cref="Format"/> controls how it is displayed.
/// </summary>
internal sealed record GridColumn<T>(
    string Header,
    Func<T, object?> Value,
    float Weight = 100,
    bool AlignRight = false,
    string? Format = null,
    Func<T, Color?>? Color = null,
    bool Emphasis = false);

/// <summary>Searchable list with a toolbar of actions: the shape of most screens.</summary>
internal abstract class ListPage<T>(IShell shell) : PageBase
    where T : class
{
    private readonly AppGrid _grid = new();
    private readonly TextBox _search = new() { BorderStyle = BorderStyle.None };
    private FieldHost? _searchHost;
    private readonly FlowLayoutPanel _actions = new();
    private readonly Label _footer = new();
    private readonly List<AppButton> _selectionButtons = [];
    private readonly System.Windows.Forms.Timer _debounce = new() { Interval = 250 };
    private IReadOnlyList<GridColumn<T>> _columns = [];
    private int _requestVersion;

    protected IShell Shell => shell;

    protected T? Selected => _grid.CurrentRow?.Tag as T;

    /// <summary>Action run on double-click or Enter; usually "edit".</summary>
    protected virtual Func<Task>? DefaultAction => null;

    protected abstract IReadOnlyList<GridColumn<T>> DefineColumns();

    protected abstract void DefineActions();

    protected abstract Task<IReadOnlyList<T>> FetchAsync(string? search);

    protected virtual string FooterText(IReadOnlyList<T> items) => L.T("List.Count", items.Count);

    protected void AddAction(
        string text, string glyph, Func<Task> handler, ButtonKind kind = ButtonKind.Secondary,
        bool needsSelection = false)
    {
        var button = new AppButton(text, kind, glyph) { Margin = new Padding(Theme.Px(8), 0, 0, 0) };
        button.Click += (_, _) => UiTask.Run(this, handler);
        _actions.Controls.Add(button);

        if (needsSelection)
        {
            button.Enabled = false;
            _selectionButtons.Add(button);
        }
    }

    protected override void Build()
    {
        var card = new Card { Dock = DockStyle.Fill, Padding = Theme.Pad(12, 12, 12, 4) };

        _footer.Dock = DockStyle.Bottom;
        _footer.Height = Theme.Px(38);
        _footer.Font = Theme.Small;
        _footer.ForeColor = Theme.TextMuted;
        _footer.TextAlign = ContentAlignment.MiddleLeft;
        _footer.Padding = new Padding(Theme.Px(8), 0, 0, 0);
        _footer.UseMnemonic = false;

        _columns = DefineColumns();
        foreach (var column in _columns)
        {
            var gridColumn = _grid.AddColumn(column.Header, column.Weight, column.AlignRight);
            if (column.Format is not null)
            {
                gridColumn.DefaultCellStyle.Format = column.Format;
            }
        }

        _grid.SelectionChanged += (_, _) => UpdateSelectionButtons();
        _grid.CellDoubleClick += (_, e) =>
        {
            if (e.RowIndex >= 0)
            {
                RunDefaultAction();
            }
        };
        _grid.KeyDown += (_, e) =>
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.Handled = true;
                RunDefaultAction();
            }
        };

        card.Controls.Add(_grid);
        card.Controls.Add(_footer);

        var toolbar = new Panel { Dock = DockStyle.Top, Height = Theme.Px(54), BackColor = Theme.Surface };

        _search.PlaceholderText = L.T("List.Search");
        _search.AccessibleName = L.T("List.Search");
        _search.TextChanged += (_, _) =>
        {
            _debounce.Stop();
            _debounce.Start();
        };
        _debounce.Tick += (_, _) =>
        {
            _debounce.Stop();
            UiTask.Run(this, ReloadAsync);
        };

        _searchHost = new FieldHost(_search, Glyphs.Search) { Width = Theme.Px(300), Location = new Point(0, 0) };

        _actions.Dock = DockStyle.Right;
        _actions.AutoSize = true;
        _actions.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        _actions.WrapContents = false;
        _actions.BackColor = Theme.Surface;
        DefineActions();

        toolbar.Controls.Add(_searchHost);
        toolbar.Controls.Add(_actions);
        toolbar.Layout += (_, _) => FitSearchBox(toolbar);

        Controls.Add(card);
        Controls.Add(toolbar);
    }

    protected override Task LoadAsync() => ReloadAsync();

    protected async Task ReloadAsync()
    {
        var version = ++_requestVersion;
        var term = _search.Text;
        var items = await FetchAsync(string.IsNullOrWhiteSpace(term) ? null : term);

        // A newer search finished first; drop this stale response.
        if (version != _requestVersion || IsDisposed)
        {
            return;
        }

        var selectedIndex = _grid.CurrentRow?.Index ?? 0;
        _grid.SuspendLayout();
        _grid.Rows.Clear();
        foreach (var item in items)
        {
            var row = _grid.Rows[_grid.Rows.Add(_columns.Select(c => c.Value(item) ?? string.Empty).ToArray())];
            row.Tag = item;
            for (var i = 0; i < _columns.Count; i++)
            {
                if (_columns[i].Color?.Invoke(item) is { } color)
                {
                    row.Cells[i].Style.ForeColor = color;
                    row.Cells[i].Style.SelectionForeColor = color;
                    if (_columns[i].Emphasis)
                    {
                        row.Cells[i].Style.Font = Theme.BodyBold;
                    }
                }
            }
        }

        if (_grid.Rows.Count > 0)
        {
            var index = Math.Min(selectedIndex, _grid.Rows.Count - 1);
            _grid.CurrentCell = _grid.Rows[index].Cells[0];
        }

        _grid.ResumeLayout();
        _footer.Text = items.Count == 0 ? L.T("List.Empty") : FooterText(items);
        UpdateSelectionButtons();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _debounce.Dispose();
        }

        base.Dispose(disposing);
    }

    /// <summary>The search box gives way when translated button captions need the room.</summary>
    private void FitSearchBox(Panel toolbar)
    {
        if (_searchHost is null)
        {
            return;
        }

        var available = toolbar.ClientSize.Width - _actions.Width - Theme.Px(8);
        _searchHost.Width = Math.Clamp(available, Theme.Px(140), Theme.Px(300));
    }

    private void UpdateSelectionButtons()
    {
        var hasSelection = Selected is not null;
        foreach (var button in _selectionButtons)
        {
            button.Enabled = hasSelection;
        }
    }

    private void RunDefaultAction()
    {
        if (Selected is not null && DefaultAction is { } action)
        {
            UiTask.Run(this, action);
        }
    }
}
