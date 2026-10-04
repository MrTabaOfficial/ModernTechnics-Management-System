using Microsoft.Extensions.DependencyInjection;
using ModernTechnics.App.Forms;
using ModernTechnics.App.Localization;
using ModernTechnics.App.Ui;
using ModernTechnics.Core.Services;
using ModernTechnics.Infrastructure;
using ModernTechnics.Infrastructure.Data;

namespace ModernTechnics.App;

internal static class Program
{
    [STAThread]
    private static int Main(string[] args)
    {
        ApplicationConfiguration.Initialize();
        Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
        Application.ThreadException += (_, e) => Dialogs.Error(Form.ActiveForm, e.Exception.Message);
        Theme.Initialize();

        var screenshots = ScreenshotOptions.Parse(args);
        if (screenshots is not null)
        {
            // Documentation shots run against a throwaway database, never the user's data.
            AppPaths.UseDirectory(Directory.CreateTempSubdirectory("moderntechnics-shots-").FullName);
        }

        var settings = AppSettings.Load();
        L.SetLanguage(screenshots?.Language ?? settings.Language);

        using var services = new ServiceCollection()
            .AddModernTechnics($"Data Source={AppPaths.Database}")
            .BuildServiceProvider();

        try
        {
            Directory.CreateDirectory(AppPaths.DataDirectory);
            var initializer = services.GetRequiredService<DatabaseInitializer>();
            Task.Run(() => initializer.InitializeAsync()).GetAwaiter().GetResult();
        }
        catch (Exception exception)
        {
            Dialogs.Error(null, L.T("App.DatabaseError", AppPaths.Database, exception.Message));
            return 1;
        }

        if (screenshots is not null)
        {
            return ScreenshotRunner.Run(services, settings, screenshots);
        }

        var auth = services.GetRequiredService<IAuthService>();
        while (true)
        {
            using var login = new LoginForm(auth, settings);
            var outcome = login.ShowDialog();
            if (outcome == DialogResult.Retry)
            {
                continue;
            }

            if (outcome != DialogResult.OK || login.User is null)
            {
                return 0;
            }

            using var main = new MainForm(services, login.User);
            Application.Run(main);
            if (!main.SignedOut)
            {
                return 0;
            }
        }
    }
}
