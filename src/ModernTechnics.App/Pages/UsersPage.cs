using ModernTechnics.App.Controls;
using ModernTechnics.App.Forms;
using ModernTechnics.App.Localization;
using ModernTechnics.App.Ui;
using ModernTechnics.Core.Common;
using ModernTechnics.Core.Domain;
using ModernTechnics.Core.Security;
using ModernTechnics.Core.Services;

namespace ModernTechnics.App.Pages;

internal sealed class UsersPage(IUserService users, IShell shell) : ListPage<UserAccount>(shell)
{
    protected override Func<Task>? DefaultAction => EditAsync;

    protected override IReadOnlyList<GridColumn<UserAccount>> DefineColumns() =>
    [
        new(L.Field(nameof(UserAccount.Email)), u => u.Email, 200),
        new(L.Field(nameof(UserAccount.Role)), u => L.Enum(u.Role), 140,
            Color: u => u.Role == Role.Administrator ? Theme.Accent : null, Emphasis: true),
        new(L.T("Users.Access"), Access, 600),
    ];

    protected override void DefineActions()
    {
        AddAction(L.T("Action.Edit"), Glyphs.Edit, EditAsync, needsSelection: true);
        AddAction(L.T("Action.Delete"), Glyphs.Delete, DeleteAsync, ButtonKind.Danger, needsSelection: true);
        AddAction(L.T("Users.Add"), Glyphs.Add, AddAsync, ButtonKind.Primary);
    }

    protected override Task<IReadOnlyList<UserAccount>> FetchAsync(string? search) => users.ListAsync(search);

    private static string Access(UserAccount user)
    {
        var modules = AccessPolicy.ModulesFor(user.Role);
        return modules.Count == Enum.GetValues<Module>().Length
            ? L.T("Users.AllModules")
            : string.Join(", ", modules.Select(m => L.T($"Module.{m}")));
    }

    private async Task AddAsync()
    {
        using var dialog = new FormDialog(L.T("Users.Add"));
        var email = dialog.AddText(nameof(UserAccount.Email));
        var password = dialog.AddText(nameof(UserAccount.PasswordHash), password: true);
        var role = dialog.AddChoice(nameof(UserAccount.Role), Enum.GetValues<Role>(), L.Enum, r => r == Role.SalesAssociate);
        dialog.Submit = async () => await users.CreateAsync(email.Text, password.Text, role.Value);

        if (dialog.ShowDialog(this) == DialogResult.OK)
        {
            Shell.Toast(L.T("Toast.Saved"));
            await ReloadAsync();
        }
    }

    private async Task EditAsync()
    {
        if (Selected is not { } user)
        {
            return;
        }

        using var dialog = new FormDialog(L.T("Users.Edit")) { Description = user.Email };
        var role = dialog.AddChoice(nameof(UserAccount.Role), Enum.GetValues<Role>(), L.Enum, r => r == user.Role);
        var password = dialog.AddText(nameof(UserAccount.PasswordHash), password: true, label: L.T("Users.NewPassword"));
        dialog.Submit = () => users.UpdateAsync(user.Id, role.Value, password.Text);

        if (dialog.ShowDialog(this) == DialogResult.OK)
        {
            Shell.Toast(L.T("Toast.Saved"));
            await ReloadAsync();
        }
    }

    private async Task DeleteAsync()
    {
        if (Selected is not { } user || !Dialogs.Confirm(this, L.T("Confirm.Delete", user.Email)))
        {
            return;
        }

        Result result = await users.DeleteAsync(user.Id, Shell.User.Id);
        if (result.IsFailure)
        {
            Dialogs.Error(this, L.Errors(result));
        }
        else
        {
            Shell.Toast(L.T("Toast.Deleted"));
        }

        await ReloadAsync();
    }
}
