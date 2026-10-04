using ModernTechnics.App.Ui;

namespace ModernTechnics.App.Controls;

/// <summary>Read-only, full-row-select grid styled to match the rest of the application.</summary>
internal sealed class AppGrid : DataGridView
{
    public AppGrid()
    {
        DoubleBuffered = true;
        Dock = DockStyle.Fill;
        BorderStyle = BorderStyle.None;
        BackgroundColor = Theme.Card;
        GridColor = Theme.Border;
        CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
        ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None;
        EnableHeadersVisualStyles = false;
        RowHeadersVisible = false;
        ReadOnly = true;
        MultiSelect = false;
        SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        AllowUserToAddRows = false;
        AllowUserToDeleteRows = false;
        AllowUserToResizeRows = false;
        AllowUserToOrderColumns = false;
        AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
        ColumnHeadersHeight = Theme.Px(42);
        RowTemplate.Height = Theme.Px(40);
        StandardTab = true;

        var cellPadding = new Padding(Theme.Px(10), 0, Theme.Px(10), 0);

        ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
        {
            BackColor = Theme.Subtle,
            ForeColor = Theme.TextMuted,
            SelectionBackColor = Theme.Subtle,
            SelectionForeColor = Theme.TextMuted,
            Font = Theme.SmallBold,
            Padding = cellPadding,
            Alignment = DataGridViewContentAlignment.MiddleLeft,
            WrapMode = DataGridViewTriState.False,
        };

        DefaultCellStyle = new DataGridViewCellStyle
        {
            BackColor = Theme.Card,
            ForeColor = Theme.Text,
            SelectionBackColor = Theme.AccentSoft,
            SelectionForeColor = Theme.Text,
            Font = Theme.Body,
            Padding = cellPadding,
            Alignment = DataGridViewContentAlignment.MiddleLeft,
        };
    }

    public DataGridViewTextBoxColumn AddColumn(string header, float weight, bool alignRight = false)
    {
        var column = new DataGridViewTextBoxColumn
        {
            HeaderText = header,
            FillWeight = weight,
            MinimumWidth = Theme.Px(60),
            SortMode = DataGridViewColumnSortMode.Automatic,
        };

        if (alignRight)
        {
            // Sortable headers reserve room for the sort arrow; pad the cells to line up with them.
            column.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            column.DefaultCellStyle.Padding = new Padding(Theme.Px(10), 0, Theme.Px(20), 0);
            column.HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleRight;
            column.HeaderCell.Style.Padding = new Padding(Theme.Px(10), 0, 0, 0);
        }

        Columns.Add(column);
        return column;
    }
}
