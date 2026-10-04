using Microsoft.Extensions.DependencyInjection;
using ModernTechnics.App.Controls;
using ModernTechnics.App.Localization;
using ModernTechnics.App.Pages;
using ModernTechnics.App.Ui;
using ModernTechnics.Core.Domain;
using ModernTechnics.Core.Security;

namespace ModernTechnics.App.Forms;

/// <summary>The application window: role-aware sidebar, page header and content area.</summary>
internal sealed class MainForm : Form, IShell
{
    private const int SidebarWidth = 248;

    private static readonly Dictionary<Module, (Type Page, string Glyph)> Modules = new()
    {
        [Module.Dashboard] = (typeof(DashboardPage), Glyphs.Home),
        [Module.Employees] = (typeof(EmployeesPage), Glyphs.People),
        [Module.Payroll] = (typeof(PayrollPage), Glyphs.Money),
        [Module.Customers] = (typeof(CustomersPage), Glyphs.Contact),
        [Module.Applications] = (typeof(ApplicationsPage), Glyphs.Document),
        [Module.Warehouse] = (typeof(WarehousePage), Glyphs.Package),
        [Module.Store] = (typeof(StorePage), Glyphs.Shop),
        [Module.Orders] = (typeof(OrdersPage), Glyphs.Cart),
        [Module.Users] = (typeof(UsersPage), Glyphs.Lock),
    };

    private readonly IServiceProvider _services;
    private readonly Dictionary<Module, NavButton> _navigation = [];
    private readonly Dictionary<Module, PageBase> _pages = [];
    private readonly Panel _content = new();
    private readonly Label _title = new();
    private readonly Label _subtitle = new();
    private readonly Toast _toast = new();
    private Module? _current;

    public MainForm(IServiceProvider services, UserAccount user)
    {
        _services = services;
        User = user;

        Text = L.T("App.Name");
        Icon = Brand.LoadIcon();
        Font = Theme.Body;
        BackColor = Theme.Surface;
        ForeColor = Theme.Text;
        AutoScaleMode = AutoScaleMode.None;
        StartPosition = FormStartPosition.CenterScreen;
        MinimumSize = new Size(Theme.Px(1080), Theme.Px(680));
        ClientSize = new Size(Theme.Px(1320), Theme.Px(820));

        _content.Dock = DockStyle.Fill;
        _content.Padding = Theme.Pad(28, 0, 28, 24);
        _content.BackColor = Theme.Surface;

        // Docking is resolved from the last control added to the first: sidebar, header, content.
        Controls.Add(_content);
        Controls.Add(BuildHeader());
        Controls.Add(BuildSidebar());
        Controls.Add(_toast);
    }

    public UserAccount User { get; }

    /// <summary>True when the window closed because the user chose to sign out.</summary>
    public bool SignedOut { get; private set; }

    public void Toast(string message) => _toast.Show(message);

    public async Task NavigateAsync(Module module)
    {
        if (!AccessPolicy.CanAccess(User.Role, module))
        {
            return;
        }

        if (!_pages.TryGetValue(module, out var page))
        {
            page = (PageBase)ActivatorUtilities.CreateInstance(new PageServices(_services, this), Modules[module].Page);
            _pages[module] = page;
        }

        _current = module;
        foreach (var (key, button) in _navigation)
        {
            button.Active = key == module;
        }

        _title.Text = L.T($"Module.{module}");
        _subtitle.Text = L.T($"Module.{module}.Hint");

        _content.SuspendLayout();
        _content.Controls.Clear();
        _content.Controls.Add(page);
        _content.ResumeLayout();

        await page.ActivateAsync();
    }

    protected override void OnShown(EventArgs e)
    {
        base.OnShown(e);
        if (_current is null)
        {
            UiTask.Run(this, () => NavigateAsync(AccessPolicy.ModulesFor(User.Role)[0]));
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            foreach (var page in _pages.Values)
            {
                page.Dispose();
            }
        }

        base.Dispose(disposing);
    }

    private Panel BuildHeader()
    {
        var header = new Panel { Dock = DockStyle.Top, Height = Theme.Px(92), BackColor = Theme.Surface };

        _title.Font = Theme.H1;
        _title.AutoSize = true;
        _title.UseMnemonic = false;
        _title.Location = new Point(Theme.Px(25), Theme.Px(20));

        _subtitle.ForeColor = Theme.TextMuted;
        _subtitle.AutoSize = true;
        _subtitle.UseMnemonic = false;
        _subtitle.Location = new Point(Theme.Px(28), Theme.Px(56));

        header.Controls.Add(_title);
        header.Controls.Add(_subtitle);
        return header;
    }

    private Panel BuildSidebar()
    {
        var sidebar = new Panel
        {
            Dock = DockStyle.Left,
            Width = Theme.Px(SidebarWidth),
            Padding = Theme.Pad(14, 0, 14, 14),
            BackColor = Theme.Sidebar,
        };
        var innerWidth = Theme.Px(SidebarWidth - 28);

        var navigation = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            BackColor = Theme.Sidebar,
        };
        foreach (var module in AccessPolicy.ModulesFor(User.Role))
        {
            var button = new NavButton(L.T($"Module.{module}"), Modules[module].Glyph) { Width = innerWidth };
            button.Click += (_, _) => UiTask.Run(this, () => NavigateAsync(module));
            _navigation[module] = button;
            navigation.Controls.Add(button);
        }

        var signOut = new NavButton(L.T("Action.SignOut"), Glyphs.SignOut) { Dock = DockStyle.Bottom };
        signOut.Click += (_, _) =>
        {
            SignedOut = true;
            Close();
        };

        var footer = new Panel { Dock = DockStyle.Bottom, Height = Theme.Px(116), BackColor = Theme.Sidebar };
        footer.Controls.Add(new UserChip(User) { Dock = DockStyle.Top });
        footer.Controls.Add(signOut);

        sidebar.Controls.Add(navigation);
        sidebar.Controls.Add(footer);
        sidebar.Controls.Add(new BrandHeader { Dock = DockStyle.Top });
        return sidebar;
    }

    /// <summary>Adds this window to the container so pages can ask for <see cref="IShell"/>.</summary>
    private sealed class PageServices(IServiceProvider services, IShell shell) : IServiceProvider
    {
        public object? GetService(Type serviceType) =>
            serviceType == typeof(IShell) ? shell : services.GetService(serviceType);
    }

    private sealed class BrandHeader : Control
    {
        public BrandHeader()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, true);
            Height = Theme.Px(92);
            TabStop = false;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var graphics = e.Graphics;
            graphics.Clear(Theme.Sidebar);
            Brand.DrawMark(graphics, new RectangleF(Theme.Px(6), Theme.Px(24), Theme.Px(36), Theme.Px(36)));
            TextRenderer.DrawText(
                graphics, L.T("App.Name"), Theme.H2,
                new Rectangle(Theme.Px(50), Theme.Px(22), Width - Theme.Px(50), Theme.Px(24)), Color.White, Draw.Left);
            TextRenderer.DrawText(
                graphics, L.T("App.Subtitle"), Theme.Small,
                new Rectangle(Theme.Px(52), Theme.Px(44), Width - Theme.Px(52), Theme.Px(18)), Theme.OnSidebarMuted, Draw.Left);
        }
    }

    private sealed class UserChip : Control
    {
        private readonly UserAccount _user;

        public UserChip(UserAccount user)
        {
            SetStyle(
                ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint
                | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            _user = user;
            Height = Theme.Px(62);
            TabStop = false;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var graphics = e.Graphics;
            graphics.Smooth();
            graphics.Clear(Theme.Sidebar);
            graphics.FillRounded(Theme.SidebarHover, new RectangleF(0, 0, Width, Height - Theme.Px(8)), Theme.Px(10f));

            var avatar = new Rectangle(Theme.Px(10), Theme.Px(9), Theme.Px(36), Theme.Px(36));
            graphics.FillRounded(Theme.Accent, avatar, avatar.Width / 2f);
            TextRenderer.DrawText(
                graphics, _user.Email[..1].ToUpperInvariant(), Theme.BodyBold, avatar, Color.White, Draw.Center);

            var textLeft = avatar.Right + Theme.Px(10);
            var textWidth = Width - textLeft - Theme.Px(8);
            TextRenderer.DrawText(
                graphics, _user.Email, Theme.SmallBold, new Rectangle(textLeft, Theme.Px(9), textWidth, Theme.Px(18)),
                Color.White, Draw.Left);
            TextRenderer.DrawText(
                graphics, L.Enum(_user.Role), Theme.Small, new Rectangle(textLeft, Theme.Px(27), textWidth, Theme.Px(18)),
                Theme.OnSidebarMuted, Draw.Left);
        }
    }
}
