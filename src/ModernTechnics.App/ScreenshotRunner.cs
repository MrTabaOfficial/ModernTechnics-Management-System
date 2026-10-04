using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using Microsoft.Extensions.DependencyInjection;
using ModernTechnics.App.Controls;
using ModernTechnics.App.Forms;
using ModernTechnics.App.Localization;
using ModernTechnics.Core.Domain;
using ModernTechnics.Core.Security;
using ModernTechnics.Core.Services;
using ModernTechnics.Infrastructure.Data;

namespace ModernTechnics.App;

internal sealed record ScreenshotOptions(string Directory, string Language)
{
    /// <summary>Recognises <c>--screenshots &lt;directory&gt; [--lang en|ka]</c>.</summary>
    public static ScreenshotOptions? Parse(string[] args)
    {
        var index = Array.IndexOf(args, "--screenshots");
        if (index < 0 || index + 1 >= args.Length)
        {
            return null;
        }

        var languageIndex = Array.IndexOf(args, "--lang");
        var language = languageIndex >= 0 && languageIndex + 1 < args.Length ? args[languageIndex + 1] : L.English;
        return new ScreenshotOptions(Path.GetFullPath(args[index + 1]), language);
    }
}

/// <summary>
/// Developer tool behind the README images: opens every screen against demo data and
/// saves a PNG of each, so documentation can be regenerated instead of captured by hand.
/// </summary>
internal static partial class ScreenshotRunner
{
    private const uint ClientOnly = 0x1;
    private const uint RenderFullContent = 0x2;

    public static int Run(IServiceProvider services, AppSettings settings, ScreenshotOptions options)
    {
        Directory.CreateDirectory(options.Directory);
        var exitCode = 0;

        using var context = new ApplicationContext();
        Application.Idle += Start;
        Application.Run(context);
        return exitCode;

        async void Start(object? sender, EventArgs e)
        {
            Application.Idle -= Start;
            try
            {
                await CaptureAllAsync(services, settings, options);
            }
            catch (Exception exception)
            {
                Console.Error.WriteLine(exception);
                exitCode = 1;
            }

            context.ExitThread();
        }
    }

    private static async Task CaptureAllAsync(IServiceProvider services, AppSettings settings, ScreenshotOptions options)
    {
        var auth = services.GetRequiredService<IAuthService>();

        using (var login = new LoginForm(auth, settings))
        {
            login.Show();
            await SettleAsync();
            Save(login, options, "login");
        }

        var admin = (await auth.SignInAsync(DemoData.AdministratorEmail, DemoData.Password)).Value;
        using (var main = new MainForm(services, admin))
        {
            main.Show();
            foreach (var module in AccessPolicy.ModulesFor(Role.Administrator))
            {
                await main.NavigateAsync(module);
                await SettleAsync();
                Save(main, options, module.ToString().ToLowerInvariant());

                if (module == Module.Employees)
                {
                    await CaptureDialogAsync(main, options, L.T("Action.Edit"), "employee-editor");
                }
            }
        }

        var sales = (await auth.SignInAsync(DemoData.SalesEmail, DemoData.Password)).Value;
        using (var main = new MainForm(services, sales))
        {
            main.Show();
            await main.NavigateAsync(Module.Store);
            await SettleAsync();
            Save(main, options, "store-sales-role");
            await CaptureDialogAsync(main, options, L.T("Store.Sell"), "sale-dialog");
        }
    }

    /// <summary>Presses a toolbar button, captures the dialog it opens and dismisses it.</summary>
    private static async Task CaptureDialogAsync(MainForm main, ScreenshotOptions options, string buttonText, string name)
    {
        var captured = new TaskCompletionSource();
        var seen = false;
        var timer = new System.Windows.Forms.Timer { Interval = 300 };
        timer.Tick += (_, _) =>
        {
            var dialog = Application.OpenForms.OfType<FormDialog>().LastOrDefault(d => d.Visible);
            if (dialog is null)
            {
                return;
            }

            if (!seen)
            {
                // Give the dialog one more tick to finish painting.
                seen = true;
                return;
            }

            timer.Stop();
            timer.Dispose();
            Save(dialog, options, name);
            dialog.DialogResult = DialogResult.Cancel;
            captured.TrySetResult();
        };
        timer.Start();

        Descendants(main).OfType<AppButton>().First(b => b.Text == buttonText).PerformClick();
        await captured.Task;
        await SettleAsync();
    }

    private static IEnumerable<Control> Descendants(Control root) =>
        root.Controls.Cast<Control>().SelectMany(child => Descendants(child).Prepend(child));

    /// <summary>Lets layout, painting and the first data load finish before capturing.</summary>
    private static async Task SettleAsync()
    {
        await Task.Delay(350);
        Application.DoEvents();
    }

    private static void Save(Form form, ScreenshotOptions options, string name)
    {
        using var bitmap = new Bitmap(form.ClientSize.Width, form.ClientSize.Height);
        using (var graphics = Graphics.FromImage(bitmap))
        {
            var deviceContext = graphics.GetHdc();
            try
            {
                PrintWindow(form.Handle, deviceContext, ClientOnly | RenderFullContent);
            }
            finally
            {
                graphics.ReleaseHdc(deviceContext);
            }
        }

        var suffix = options.Language == L.English ? string.Empty : "." + options.Language;
        bitmap.Save(Path.Combine(options.Directory, $"{name}{suffix}.png"), ImageFormat.Png);
    }

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool PrintWindow(IntPtr window, IntPtr deviceContext, uint flags);
}
