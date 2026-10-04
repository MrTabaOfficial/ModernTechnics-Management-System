using ModernTechnics.App.Controls;
using ModernTechnics.App.Forms;
using ModernTechnics.App.Localization;
using ModernTechnics.App.Ui;
using ModernTechnics.Core.Domain;
using ModernTechnics.Core.Services;

namespace ModernTechnics.App.Pages;

internal sealed class CustomersPage(ICustomerService customers, IShell shell) : ListPage<Customer>(shell)
{
    protected override Func<Task>? DefaultAction => () => EditAsync(Selected);

    protected override IReadOnlyList<GridColumn<Customer>> DefineColumns() =>
    [
        new(L.Field(nameof(Customer.PersonalId)), c => c.PersonalId, 110),
        new(L.Field(nameof(Customer.FullName)), c => c.FullName, 140),
        new(L.Field(nameof(Customer.BirthDate)), c => c.BirthDate, 80, Format: "d"),
        new(L.Field(nameof(Customer.Phone)), c => c.Phone, 140),
        new(L.Field(nameof(Customer.Address)), c => c.Address, 200),
    ];

    protected override void DefineActions()
    {
        AddAction(L.T("Action.Edit"), Glyphs.Edit, () => EditAsync(Selected), needsSelection: true);
        AddAction(L.T("Action.Delete"), Glyphs.Delete, DeleteAsync, ButtonKind.Danger, needsSelection: true);
        AddAction(L.T("Customers.Add"), Glyphs.Add, () => EditAsync(null), ButtonKind.Primary);
    }

    protected override Task<IReadOnlyList<Customer>> FetchAsync(string? search) => customers.ListAsync(search);

    private async Task EditAsync(Customer? existing)
    {
        using var dialog = new FormDialog(L.T(existing is null ? "Customers.Add" : "Customers.Edit"), columns: 2);
        var personalId = dialog.AddText(nameof(Customer.PersonalId), existing?.PersonalId, readOnly: existing is not null);
        var birthDate = dialog.AddDate(
            nameof(Customer.BirthDate), existing?.BirthDate ?? DateOnly.FromDateTime(DateTime.Today.AddYears(-30)));
        var firstName = dialog.AddText(nameof(Customer.FirstName), existing?.FirstName);
        var lastName = dialog.AddText(nameof(Customer.LastName), existing?.LastName);
        var phone = dialog.AddText(nameof(Customer.Phone), existing?.Phone ?? "+995 ");
        var address = dialog.AddText(nameof(Customer.Address), existing?.Address);

        dialog.Submit = () =>
        {
            var customer = new Customer
            {
                PersonalId = personalId.Text,
                BirthDate = DateOnly.FromDateTime(birthDate.Value),
                FirstName = firstName.Text,
                LastName = lastName.Text,
                Phone = phone.Text,
                Address = address.Text,
            };
            return existing is null ? customers.CreateAsync(customer) : customers.UpdateAsync(customer);
        };

        if (dialog.ShowDialog(this) == DialogResult.OK)
        {
            Shell.Toast(L.T("Toast.Saved"));
            await ReloadAsync();
        }
    }

    private async Task DeleteAsync()
    {
        if (Selected is not { } customer || !Dialogs.Confirm(this, L.T("Confirm.Delete", customer.FullName)))
        {
            return;
        }

        var result = await customers.DeleteAsync(customer.PersonalId);
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
