using ModernTechnics.App.Ui;

namespace ModernTechnics.App.Pages;

/// <summary>A screen shown in the main window's content area.</summary>
internal abstract class PageBase : UserControl
{
    private bool _built;

    protected PageBase()
    {
        AutoScaleMode = AutoScaleMode.None;
        Dock = DockStyle.Fill;
        BackColor = Theme.Surface;
        ForeColor = Theme.Text;
        Font = Theme.Body;
        DoubleBuffered = true;
    }

    /// <summary>Builds the controls on first use, then refreshes the data.</summary>
    public async Task ActivateAsync()
    {
        if (!_built)
        {
            _built = true;
            SuspendLayout();
            Build();
            ResumeLayout(performLayout: true);
        }

        await LoadAsync();
    }

    protected abstract void Build();

    protected abstract Task LoadAsync();
}
