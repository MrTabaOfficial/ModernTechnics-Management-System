using System.Drawing.Drawing2D;
using ModernTechnics.App.Controls;
using ModernTechnics.App.Localization;
using ModernTechnics.App.Ui;
using ModernTechnics.Core.Domain;
using ModernTechnics.Core.Services;
using ModernTechnics.Infrastructure.Data;

namespace ModernTechnics.App.Forms;

internal sealed class LoginForm : Form
{
    private const int BrandWidth = 400;
    private const int FormLeft = BrandWidth + 56;
    private const int FormWidth = 344;

    private readonly IAuthService _auth;
    private readonly AppSettings _settings;
    private readonly TextBox _email = new() { BorderStyle = BorderStyle.None };
    private readonly TextBox _password = new() { BorderStyle = BorderStyle.None, UseSystemPasswordChar = true };
    private readonly FormField _emailField;
    private readonly FormField _passwordField;
    private readonly Label _error;
    private readonly AppButton _signIn;

    public LoginForm(IAuthService auth, AppSettings settings)
    {
        _auth = auth;
        _settings = settings;

        Text = L.T("App.Name");
        Icon = Brand.LoadIcon();
        Font = Theme.Body;
        BackColor = Theme.Card;
        ForeColor = Theme.Text;
        AutoScaleMode = AutoScaleMode.None;
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox = false;
        StartPosition = FormStartPosition.CenterScreen;
        ClientSize = new Size(Theme.Px(BrandWidth + 56 + FormWidth + 56), Theme.Px(580));

        Controls.Add(new BrandPanel { Bounds = new Rectangle(0, 0, Theme.Px(BrandWidth), ClientSize.Height) });
        AddLanguageSwitch();

        var top = 84;
        Controls.Add(new Label
        {
            Text = L.T("Login.Title"),
            Font = Theme.H1,
            AutoSize = true,
            Location = new Point(Theme.Px(FormLeft - 3), Theme.Px(top)),
        });
        Controls.Add(new Label
        {
            Text = L.T("Login.Subtitle"),
            ForeColor = Theme.TextMuted,
            AutoSize = true,
            Location = new Point(Theme.Px(FormLeft), Theme.Px(top + 40)),
        });

        _email.Text = settings.LastEmail;
        _emailField = new FormField(L.Field(nameof(UserAccount.Email)), new FieldHost(_email, Glyphs.Mail), Theme.Px(FormWidth))
        {
            Location = new Point(Theme.Px(FormLeft), Theme.Px(top + 84)),
        };
        _passwordField = new FormField(L.Field(nameof(UserAccount.PasswordHash)), new FieldHost(_password, Glyphs.Key), Theme.Px(FormWidth))
        {
            Location = new Point(Theme.Px(FormLeft), Theme.Px(top + 168)),
        };
        Controls.Add(_emailField);
        Controls.Add(_passwordField);

        _error = new Label
        {
            ForeColor = Theme.Danger,
            AutoSize = false,
            UseMnemonic = false,
            Bounds = new Rectangle(Theme.Px(FormLeft), Theme.Px(top + 250), Theme.Px(FormWidth), Theme.Px(22)),
        };
        Controls.Add(_error);

        _signIn = new AppButton(L.T("Login.SignIn"), ButtonKind.Primary)
        {
            Bounds = new Rectangle(Theme.Px(FormLeft), Theme.Px(top + 278), Theme.Px(FormWidth), Theme.Px(42)),
        };
        _signIn.Click += OnSignInClick;
        Controls.Add(_signIn);
        AcceptButton = _signIn;

        AddDemoAccounts(top + 350);
    }

    public UserAccount? User { get; private set; }

    protected override void OnShown(EventArgs e)
    {
        base.OnShown(e);
        (_email.TextLength == 0 ? _email : _password).Select();
    }

    private void AddLanguageSwitch()
    {
        var right = ClientSize.Width - Theme.Px(20);
        foreach (var (code, caption) in new[] { (L.Georgian, "ქართული"), (L.English, "English") })
        {
            var active = L.Language == code;
            var button = new AppButton(caption, active ? ButtonKind.Secondary : ButtonKind.Ghost)
            {
                Font = Theme.Small,
                Height = Theme.Px(30),
                TabStop = false,
                AccessibleName = caption,
            };
            button.FitWidth();
            button.Location = new Point(right - button.Width, Theme.Px(16));
            right = button.Left - Theme.Px(6);

            if (!active)
            {
                button.Click += (_, _) =>
                {
                    _settings.Language = code;
                    _settings.LastEmail = _email.Text;
                    _settings.Save();
                    L.SetLanguage(code);

                    // The caller rebuilds the form so every caption picks up the new language.
                    DialogResult = DialogResult.Retry;
                };
            }

            Controls.Add(button);
        }
    }

    private void AddDemoAccounts(int top)
    {
        Controls.Add(new Label
        {
            Text = L.T("Login.Demo", DemoData.Password),
            Font = Theme.Small,
            ForeColor = Theme.TextMuted,
            AutoSize = true,
            UseMnemonic = false,
            Location = new Point(Theme.Px(FormLeft), Theme.Px(top)),
        });

        var row = new FlowLayoutPanel
        {
            Bounds = new Rectangle(Theme.Px(FormLeft - 4), Theme.Px(top + 24), Theme.Px(FormWidth + 16), Theme.Px(76)),
            BackColor = Theme.Card,
        };

        (Role Role, string Email)[] accounts =
        [
            (Role.Administrator, DemoData.AdministratorEmail),
            (Role.Manager, DemoData.ManagerEmail),
            (Role.WarehouseOperator, DemoData.WarehouseEmail),
            (Role.SalesAssociate, DemoData.SalesEmail),
        ];
        foreach (var (role, email) in accounts)
        {
            var button = new AppButton(L.Enum(role), ButtonKind.Ghost)
            {
                Font = Theme.SmallBold,
                Height = Theme.Px(30),
                Margin = new Padding(0, 0, Theme.Px(4), Theme.Px(4)),
                TabStop = false,
            };
            button.FitWidth();
            button.Click += (_, _) =>
            {
                _email.Text = email;
                _password.Text = DemoData.Password;
                _signIn.Select();
            };
            row.Controls.Add(button);
        }

        Controls.Add(row);
    }

    private async void OnSignInClick(object? sender, EventArgs e)
    {
        _emailField.Error = string.Empty;
        _passwordField.Error = string.Empty;
        _error.Text = string.Empty;

        if (string.IsNullOrWhiteSpace(_email.Text) || _password.TextLength == 0)
        {
            _error.Text = L.T("Login.Missing");
            return;
        }

        _signIn.Enabled = false;
        UseWaitCursor = true;
        try
        {
            var result = await _auth.SignInAsync(_email.Text, _password.Text);
            if (result.IsFailure)
            {
                _error.Text = L.Errors(result);
                _password.SelectAll();
                _password.Select();
                return;
            }

            User = result.Value;
            _settings.LastEmail = result.Value.Email;
            _settings.Save();
            DialogResult = DialogResult.OK;
        }
        catch (Exception exception)
        {
            if (!IsDisposed)
            {
                _error.Text = exception.Message;
            }
        }
        finally
        {
            if (!IsDisposed)
            {
                _signIn.Enabled = true;
                UseWaitCursor = false;
            }
        }
    }

    /// <summary>The illustrated left half of the sign-in window.</summary>
    private sealed class BrandPanel : Control
    {
        public BrandPanel()
        {
            SetStyle(
                ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint
                | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            TabStop = false;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var graphics = e.Graphics;
            graphics.Smooth();

            using (var background = new LinearGradientBrush(
                ClientRectangle, Theme.Sidebar, Theme.SidebarDeep, LinearGradientMode.ForwardDiagonal))
            {
                graphics.FillRectangle(background, ClientRectangle);
            }

            using (var glow = new SolidBrush(Color.FromArgb(28, Theme.Accent)))
            {
                graphics.FillEllipse(glow, Width - Theme.Px(190), -Theme.Px(110), Theme.Px(340), Theme.Px(340));
                graphics.FillEllipse(glow, -Theme.Px(140), Height - Theme.Px(200), Theme.Px(360), Theme.Px(360));
            }

            var left = Theme.Px(44);
            Brand.DrawMark(graphics, new RectangleF(left, Theme.Px(84), Theme.Px(52), Theme.Px(52)));

            var width = Width - (left * 2);
            TextRenderer.DrawText(
                graphics, L.T("App.Name"), Theme.Hero, new Rectangle(left - Theme.Px(4), Theme.Px(156), width, Theme.Px(52)),
                Color.White, Draw.Left);
            TextRenderer.DrawText(
                graphics, L.T("App.Tagline"), Theme.Body, new Rectangle(left, Theme.Px(212), width, Theme.Px(66)),
                Theme.OnSidebarMuted, TextFormatFlags.Left | TextFormatFlags.WordBreak | TextFormatFlags.NoPrefix);

            var top = Theme.Px(304);
            foreach (var key in new[] { "Login.Feature1", "Login.Feature2", "Login.Feature3", "Login.Feature4" })
            {
                var badge = new Rectangle(left, top + Theme.Px(3), Theme.Px(22), Theme.Px(22));
                graphics.FillRounded(Color.FromArgb(60, Theme.Accent), badge, badge.Width / 2f);
                graphics.Glyph(Glyphs.Check, 8f, Color.White, badge);
                TextRenderer.DrawText(
                    graphics, L.T(key), Theme.Body,
                    new Rectangle(left + Theme.Px(34), top, width - Theme.Px(34), Theme.Px(28)), Color.White, Draw.Left);
                top += Theme.Px(38);
            }

            TextRenderer.DrawText(
                graphics, L.T("App.Footer"), Theme.Small,
                new Rectangle(left, Height - Theme.Px(48), width, Theme.Px(20)), Theme.OnSidebarMuted, Draw.Left);
        }
    }
}
