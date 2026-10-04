using ModernTechnics.App.Controls;
using ModernTechnics.App.Localization;
using ModernTechnics.App.Ui;
using ModernTechnics.Core.Common;

namespace ModernTechnics.App.Forms;

/// <summary>
/// A modal editor assembled from field definitions. Validation errors returned by the
/// submit callback are shown next to the field they belong to.
/// </summary>
internal sealed class FormDialog : Form
{
    private const int FieldWidth = 320;
    private const int Gap = 16;

    private readonly int _columns;
    private readonly TableLayoutPanel _grid;
    private readonly Label _description;
    private readonly Label _summary;
    private readonly AppButton _submit;
    private readonly AppButton _cancel;
    private readonly Dictionary<string, FormField> _fields = [];
    private int _cell;

    public FormDialog(string title, string? submitText = null, int columns = 1, ButtonKind submitKind = ButtonKind.Primary)
    {
        _columns = columns;
        var contentWidth = Theme.Px((FieldWidth * columns) + (Gap * (columns - 1)));

        Text = title;
        Font = Theme.Body;
        BackColor = Theme.Card;
        ForeColor = Theme.Text;
        AutoScaleMode = AutoScaleMode.None;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowInTaskbar = false;
        ShowIcon = false;
        StartPosition = FormStartPosition.CenterParent;
        AutoSize = true;
        AutoSizeMode = AutoSizeMode.GrowAndShrink;
        Padding = Theme.Pad(24, 20, 24 - Gap, 20);

        var root = new TableLayoutPanel
        {
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            ColumnCount = 1,
            Location = new Point(Padding.Left, Padding.Top),
            BackColor = Theme.Card,
        };

        var heading = new Label
        {
            Text = title,
            Font = Theme.H2,
            AutoSize = true,
            UseMnemonic = false,
            Margin = new Padding(0, 0, 0, Theme.Px(4)),
        };

        _description = new Label
        {
            Font = Theme.Body,
            ForeColor = Theme.TextMuted,
            AutoSize = true,
            UseMnemonic = false,
            MaximumSize = new Size(contentWidth, 0),
            Margin = new Padding(0, 0, 0, Theme.Px(4)),
            Visible = false,
        };

        _grid = new TableLayoutPanel
        {
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            ColumnCount = columns,
            Margin = new Padding(0, Theme.Px(12), 0, 0),
            BackColor = Theme.Card,
        };

        _summary = new Label
        {
            Font = Theme.Body,
            ForeColor = Theme.Danger,
            AutoSize = true,
            UseMnemonic = false,
            MaximumSize = new Size(contentWidth, 0),
            Margin = new Padding(0, 0, 0, Theme.Px(8)),
            Visible = false,
        };

        _submit = new AppButton(submitText ?? L.T("Action.Save"), submitKind);
        _submit.Width = Math.Max(_submit.Width, Theme.Px(104));
        _submit.Click += OnSubmitClick;

        _cancel = new AppButton(L.T("Action.Cancel")) { DialogResult = DialogResult.Cancel };
        _cancel.Width = Math.Max(_cancel.Width, Theme.Px(104));
        _cancel.Margin = new Padding(0, 0, Theme.Px(8), 0);
        _submit.Margin = new Padding(0, 0, Theme.Px(Gap), 0);

        var buttons = new FlowLayoutPanel
        {
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            FlowDirection = FlowDirection.RightToLeft,
            WrapContents = false,
            Anchor = AnchorStyles.Right,
            Margin = new Padding(0, Theme.Px(6), 0, 0),
            BackColor = Theme.Card,
        };
        buttons.Controls.Add(_submit);
        buttons.Controls.Add(_cancel);

        root.Controls.Add(heading);
        root.Controls.Add(_description);
        root.Controls.Add(_grid);
        root.Controls.Add(_summary);
        root.Controls.Add(buttons);
        Controls.Add(root);

        AcceptButton = _submit;
        CancelButton = _cancel;
    }

    /// <summary>Invoked when the user confirms; a successful result closes the dialog.</summary>
    public Func<Task<Result>>? Submit { get; set; }

    public string Description
    {
        get => _description.Text;
        set
        {
            _description.Text = value;
            _description.Visible = value.Length > 0;
        }
    }

    /// <summary>Turns the dialog into a read-only viewer with a single close button.</summary>
    public void MakeReadOnly()
    {
        _submit.Visible = false;
        _cancel.Text = L.T("Action.Close");
        _cancel.Margin = new Padding(0, 0, Theme.Px(Gap), 0);
    }

    public TextBox AddText(
        string field, string? value = null, bool password = false, bool readOnly = false, string? label = null)
    {
        var box = new TextBox
        {
            BorderStyle = BorderStyle.None,
            Text = value ?? string.Empty,
            UseSystemPasswordChar = password,
            ReadOnly = readOnly,
            TabStop = !readOnly,
        };
        Add(field, box, label: label);
        return box;
    }

    public TextBox AddMultiline(string field, string? value = null, bool readOnly = false, int height = 140)
    {
        var box = new TextBox
        {
            BorderStyle = BorderStyle.None,
            Multiline = true,
            AcceptsReturn = true,
            ScrollBars = ScrollBars.Vertical,
            Text = value ?? string.Empty,
            ReadOnly = readOnly,
        };
        Add(field, box, span: _columns, hostHeight: height, stretch: true, verticalInset: 8);
        return box;
    }

    public DateTimePicker AddDate(string field, DateOnly value)
    {
        var picker = new DateTimePicker
        {
            Format = DateTimePickerFormat.Short,
            MinDate = new DateTime(1900, 1, 1),
            MaxDate = DateTime.Today,
            Value = value.ToDateTime(TimeOnly.MinValue),
        };
        Add(field, picker, clipInset: 2);
        return picker;
    }

    public NumericUpDown AddNumber(string field, decimal value, decimal maximum, int decimals = 0, decimal minimum = 0)
    {
        var number = new NumericUpDown
        {
            BorderStyle = BorderStyle.None,
            Minimum = minimum,
            Maximum = maximum,
            DecimalPlaces = decimals,
            ThousandsSeparator = decimals > 0,
            Increment = decimals > 0 ? 50 : 1,
            Value = Math.Clamp(value, minimum, maximum),
            TextAlign = HorizontalAlignment.Left,
        };
        number.Enter += (_, _) => number.Select(0, number.Text.Length);
        Add(field, number);
        return number;
    }

    public Choice<T> AddChoice<T>(
        string field, IEnumerable<T> items, Func<T, string> display, Func<T, bool>? selected = null)
    {
        var box = new ComboBox
        {
            DropDownStyle = ComboBoxStyle.DropDownList,
            FlatStyle = FlatStyle.Flat,
            DrawMode = DrawMode.OwnerDrawFixed,
            ItemHeight = Theme.Px(22),
        };
        box.DrawItem += DrawChoiceItem;
        var choice = new Choice<T>(box, items, display, selected);
        Add(field, box, clipInset: 1);
        return choice;
    }

    protected override void OnShown(EventArgs e)
    {
        base.OnShown(e);
        var first = _fields.Values.SelectMany(f => f.Controls.OfType<FieldHost>())
            .SelectMany(h => h.Controls.OfType<Panel>())
            .SelectMany(p => p.Controls.Cast<Control>())
            .FirstOrDefault(c => c.TabStop && c.Enabled);
        first?.Select();
    }

    private void Add(
        string field, Control inner, string? label = null, int clipInset = 0, int span = 1,
        int hostHeight = 38, bool stretch = false, int verticalInset = 4)
    {
        var width = Theme.Px((FieldWidth * span) + (Gap * (span - 1)));
        var host = new FieldHost(inner, clipInset: clipInset, stretch: stretch, verticalInset: verticalInset)
        {
            Height = Theme.Px(hostHeight),
        };
        var formField = new FormField(label ?? L.Field(field), host, width);
        _fields[field] = formField;

        if (span > 1 && _cell % _columns != 0)
        {
            _cell += _columns - (_cell % _columns);
        }

        _grid.Controls.Add(formField, _cell % _columns, _cell / _columns);
        if (span > 1)
        {
            _grid.SetColumnSpan(formField, span);
        }

        _cell += span;
    }

    /// <summary>Paints drop-down entries in the application palette instead of system blue.</summary>
    private static void DrawChoiceItem(object? sender, DrawItemEventArgs e)
    {
        if (sender is not ComboBox box || e.Index < 0)
        {
            return;
        }

        var inList = !e.State.HasFlag(DrawItemState.ComboBoxEdit);
        var highlighted = inList && e.State.HasFlag(DrawItemState.Selected);
        using (var background = new SolidBrush(highlighted ? Theme.AccentSoft : Theme.Card))
        {
            e.Graphics.FillRectangle(background, e.Bounds);
        }

        var bounds = inList ? Rectangle.Inflate(e.Bounds, -Theme.Px(4), 0) : e.Bounds;
        TextRenderer.DrawText(
            e.Graphics, box.GetItemText(box.Items[e.Index]), box.Font, bounds, Theme.Text, Draw.Left);
    }

    private async void OnSubmitClick(object? sender, EventArgs e)
    {
        if (Submit is null)
        {
            DialogResult = DialogResult.OK;
            return;
        }

        foreach (var field in _fields.Values)
        {
            field.Error = string.Empty;
        }

        _summary.Visible = false;
        _submit.Enabled = false;
        UseWaitCursor = true;
        try
        {
            var result = await Submit();
            if (result.IsSuccess)
            {
                DialogResult = DialogResult.OK;
                return;
            }

            ShowErrors(result);
        }
        catch (Exception exception)
        {
            if (!IsDisposed)
            {
                _summary.Text = exception.Message;
                _summary.Visible = true;
            }
        }
        finally
        {
            if (!IsDisposed)
            {
                _submit.Enabled = true;
                UseWaitCursor = false;
            }
        }
    }

    private void ShowErrors(Result result)
    {
        var general = new List<string>();
        foreach (var error in result.Errors)
        {
            if (error.Field is not null && _fields.TryGetValue(error.Field, out var field))
            {
                if (field.Error.Length == 0)
                {
                    field.Error = L.Error(error);
                }
            }
            else
            {
                general.Add(L.Error(error));
            }
        }

        _summary.Text = string.Join(Environment.NewLine, general);
        _summary.Visible = general.Count > 0;
    }
}

/// <summary>Typed view over a drop-down list.</summary>
internal sealed class Choice<T>
{
    private readonly ComboBox _box;

    public Choice(ComboBox box, IEnumerable<T> items, Func<T, string> display, Func<T, bool>? selected)
    {
        _box = box;
        foreach (var item in items)
        {
            var index = box.Items.Add(new Item(item, display(item)));
            if (selected?.Invoke(item) == true)
            {
                box.SelectedIndex = index;
            }
        }

        if (box.SelectedIndex < 0 && box.Items.Count > 0)
        {
            box.SelectedIndex = 0;
        }
    }

    public T? Value => _box.SelectedItem is Item item ? item.Value : default;

    private sealed record Item(T Value, string Text)
    {
        public override string ToString() => Text;
    }
}
