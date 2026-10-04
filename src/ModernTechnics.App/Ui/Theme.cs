namespace ModernTechnics.App.Ui;

/// <summary>Colours, type scale and DPI-aware metrics shared by every screen.</summary>
internal static class Theme
{
    public static readonly Color Sidebar = Color.FromArgb(0x17, 0x12, 0x3A);
    public static readonly Color SidebarDeep = Color.FromArgb(0x2B, 0x1F, 0x7A);
    public static readonly Color SidebarHover = Color.FromArgb(0x24, 0x1D, 0x52);
    public static readonly Color SidebarActive = Color.FromArgb(0x33, 0x29, 0x73);
    public static readonly Color OnSidebar = Color.White;
    public static readonly Color OnSidebarMuted = Color.FromArgb(0xA9, 0xA4, 0xD0);

    public static readonly Color Accent = Color.FromArgb(0x7C, 0x5C, 0xFC);
    public static readonly Color AccentHover = Color.FromArgb(0x69, 0x47, 0xF0);
    public static readonly Color AccentPressed = Color.FromArgb(0x58, 0x37, 0xDB);
    public static readonly Color AccentSoft = Color.FromArgb(0xEE, 0xEA, 0xFE);
    public static readonly Color AccentMuted = Color.FromArgb(0xCB, 0xC0, 0xFD);

    public static readonly Color Surface = Color.FromArgb(0xF4, 0xF5, 0xFA);
    public static readonly Color Card = Color.White;
    public static readonly Color Border = Color.FromArgb(0xE3, 0xE6, 0xEF);
    public static readonly Color BorderStrong = Color.FromArgb(0xCD, 0xD2, 0xE0);
    public static readonly Color Subtle = Color.FromArgb(0xF8, 0xF9, 0xFC);

    public static readonly Color Text = Color.FromArgb(0x1C, 0x20, 0x33);
    public static readonly Color TextMuted = Color.FromArgb(0x6B, 0x72, 0x88);

    public static readonly Color Success = Color.FromArgb(0x0F, 0x9D, 0x76);
    public static readonly Color SuccessSoft = Color.FromArgb(0xE2, 0xF6, 0xF0);
    public static readonly Color Warning = Color.FromArgb(0xC9, 0x7A, 0x0B);
    public static readonly Color WarningSoft = Color.FromArgb(0xFD, 0xF1, 0xDC);
    public static readonly Color Danger = Color.FromArgb(0xD9, 0x3D, 0x42);
    public static readonly Color DangerHover = Color.FromArgb(0xC2, 0x2F, 0x34);
    public static readonly Color DangerSoft = Color.FromArgb(0xFD, 0xEC, 0xEC);
    public static readonly Color Info = Color.FromArgb(0x2B, 0x7F, 0xD9);
    public static readonly Color InfoSoft = Color.FromArgb(0xE4, 0xF0, 0xFC);

    public static readonly Font Body = new("Segoe UI", 10f);
    public static readonly Font BodyBold = new("Segoe UI Semibold", 10f);
    public static readonly Font Small = new("Segoe UI", 9f);
    public static readonly Font SmallBold = new("Segoe UI Semibold", 9f);
    public static readonly Font H1 = new("Segoe UI Semibold", 18f);
    public static readonly Font H2 = new("Segoe UI Semibold", 12.5f);
    public static readonly Font Display = new("Segoe UI Semibold", 21f);
    public static readonly Font Hero = new("Segoe UI Semibold", 26f);

    private static readonly Dictionary<float, Font> IconFonts = [];
    private static string _iconFontName = "Segoe MDL2 Assets";

    /// <summary>Ratio of the system DPI to the 96 DPI the layouts are designed at.</summary>
    public static float Scale { get; private set; } = 1f;

    public static void Initialize()
    {
        using var graphics = Graphics.FromHwnd(IntPtr.Zero);
        Scale = graphics.DpiX / 96f;

        // Windows 11 ships the newer icon set; Windows 10 only has MDL2. The code points match.
        using var probe = new Font("Segoe Fluent Icons", 10f);
        if (probe.Name == "Segoe Fluent Icons")
        {
            _iconFontName = probe.Name;
        }
    }

    public static int Px(int value) => (int)MathF.Round(value * Scale);

    public static float Px(float value) => value * Scale;

    public static Padding Pad(int all) => new(Px(all));

    public static Padding Pad(int horizontal, int vertical) =>
        new(Px(horizontal), Px(vertical), Px(horizontal), Px(vertical));

    public static Padding Pad(int left, int top, int right, int bottom) =>
        new(Px(left), Px(top), Px(right), Px(bottom));

    public static Font Icon(float size)
    {
        if (!IconFonts.TryGetValue(size, out var font))
        {
            font = new Font(_iconFontName, size);
            IconFonts[size] = font;
        }

        return font;
    }
}

/// <summary>Code points in the Segoe icon fonts.</summary>
internal static class Glyphs
{
    public const string Home = "";
    public const string People = "";
    public const string Money = "";
    public const string Contact = "";
    public const string Document = "";
    public const string Package = "";
    public const string Shop = "";
    public const string Cart = "";
    public const string Lock = "";
    public const string Refresh = "";
    public const string Add = "";
    public const string Edit = "";
    public const string Delete = "";
    public const string Search = "";
    public const string SignOut = "";
    public const string Mail = "";
    public const string Key = "";
    public const string Forward = "";
    public const string Download = "";
    public const string Chart = "";
    public const string View = "";
    public const string Flag = "";
    public const string Check = "";
}
