using ModernTechnics.App.Ui;

namespace ModernTechnics.App.Controls;

/// <summary>A caption, an input and a line reserved for that input's validation message.</summary>
internal sealed class FormField : Panel
{
    private readonly FieldHost _host;
    private readonly Label _error;

    public FormField(string caption, FieldHost host, int width)
    {
        _host = host;
        BackColor = Theme.Card;

        var label = new Label
        {
            Text = caption,
            Font = Theme.SmallBold,
            ForeColor = Theme.Text,
            AutoSize = true,
            Location = new Point(0, 0),
            UseMnemonic = false,
        };

        host.Location = new Point(0, Theme.Px(22));
        host.Width = width;

        _error = new Label
        {
            Font = Theme.Small,
            ForeColor = Theme.Danger,
            AutoSize = false,
            AutoEllipsis = true,
            UseMnemonic = false,
            Bounds = new Rectangle(0, host.Bottom + Theme.Px(2), width, Theme.Px(18)),
        };

        Size = new Size(width, _error.Bottom + Theme.Px(2));
        Margin = new Padding(0, 0, Theme.Px(16), 0);
        Controls.Add(label);
        Controls.Add(host);
        Controls.Add(_error);
    }

    public string Error
    {
        get => _error.Text;
        set
        {
            _error.Text = value;
            _host.HasError = value.Length > 0;
        }
    }
}
