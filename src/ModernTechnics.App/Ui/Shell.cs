using ModernTechnics.App.Localization;
using ModernTechnics.Core.Domain;

namespace ModernTechnics.App.Ui;

/// <summary>What a page may ask of the window hosting it.</summary>
internal interface IShell
{
    UserAccount User { get; }

    void Toast(string message);
}

internal static class Dialogs
{
    public static bool Confirm(IWin32Window owner, string message) =>
        MessageBox.Show(
            owner, message, L.T("App.Name"), MessageBoxButtons.YesNo, MessageBoxIcon.Warning,
            MessageBoxDefaultButton.Button2) == DialogResult.Yes;

    public static void Error(IWin32Window? owner, string message) =>
        MessageBox.Show(owner, message, L.T("App.Name"), MessageBoxButtons.OK, MessageBoxIcon.Error);
}

internal static class UiTask
{
    /// <summary>
    /// Runs an async UI action from an event handler, showing a wait cursor and
    /// reporting unexpected failures instead of letting them crash the application.
    /// </summary>
    public static async void Run(Control owner, Func<Task> action)
    {
        owner.UseWaitCursor = true;
        try
        {
            await action();
        }
        catch (Exception exception)
        {
            if (!owner.IsDisposed)
            {
                Dialogs.Error(owner.FindForm(), exception.Message);
            }
        }
        finally
        {
            if (!owner.IsDisposed)
            {
                owner.UseWaitCursor = false;
            }
        }
    }
}
