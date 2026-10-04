using ModernTechnics.App.Controls;
using ModernTechnics.App.Forms;
using ModernTechnics.App.Localization;
using ModernTechnics.App.Ui;
using ModernTechnics.Core.Domain;
using ModernTechnics.Core.Services;

namespace ModernTechnics.App.Pages;

internal sealed class EmployeesPage(IEmployeeService employees, IShell shell) : ListPage<Employee>(shell)
{
    protected override Func<Task>? DefaultAction => () => EditAsync(Selected);

    protected override IReadOnlyList<GridColumn<Employee>> DefineColumns() =>
    [
        new(L.Field(nameof(Employee.PersonalId)), e => e.PersonalId, 112),
        new(L.Field(nameof(Employee.FullName)), e => e.FullName, 145),
        new(L.Field(nameof(Employee.PositionId)), e => e.Position?.Name, 145),
        new(L.Field(nameof(Employee.Phone)), e => e.Phone, 145),
        new(L.Field(nameof(Employee.Email)), e => e.Email, 255),
        new(L.Field(nameof(Employee.BirthDate)), e => e.BirthDate, 100, Format: "d"),
        new(L.Field(nameof(Employee.Salary)), e => e.Salary, 105, AlignRight: true, Format: "N2"),
    ];

    protected override void DefineActions()
    {
        AddAction(L.T("Action.Edit"), Glyphs.Edit, () => EditAsync(Selected), needsSelection: true);
        AddAction(L.T("Action.Delete"), Glyphs.Delete, DeleteAsync, ButtonKind.Danger, needsSelection: true);
        AddAction(L.T("Employees.Add"), Glyphs.Add, () => EditAsync(null), ButtonKind.Primary);
    }

    protected override Task<IReadOnlyList<Employee>> FetchAsync(string? search) => employees.ListAsync(search);

    private async Task EditAsync(Employee? existing)
    {
        var positions = await employees.ListPositionsAsync();

        using var dialog = new FormDialog(L.T(existing is null ? "Employees.Add" : "Employees.Edit"), columns: 2);
        var personalId = dialog.AddText(nameof(Employee.PersonalId), existing?.PersonalId, readOnly: existing is not null);
        var position = dialog.AddChoice(nameof(Employee.PositionId), positions, p => p.Name, p => p.Id == existing?.PositionId);
        var firstName = dialog.AddText(nameof(Employee.FirstName), existing?.FirstName);
        var lastName = dialog.AddText(nameof(Employee.LastName), existing?.LastName);
        var birthDate = dialog.AddDate(
            nameof(Employee.BirthDate), existing?.BirthDate ?? DateOnly.FromDateTime(DateTime.Today.AddYears(-25)));
        var maritalStatus = dialog.AddChoice(
            nameof(Employee.MaritalStatus), Enum.GetValues<MaritalStatus>(), L.Enum, s => s == existing?.MaritalStatus);
        var phone = dialog.AddText(nameof(Employee.Phone), existing?.Phone ?? "+995 ");
        var email = dialog.AddText(nameof(Employee.Email), existing?.Email);
        var address = dialog.AddText(nameof(Employee.Address), existing?.Address);
        var salary = dialog.AddNumber(nameof(Employee.Salary), existing?.Salary ?? 0, 1_000_000, decimals: 2);

        dialog.Submit = () =>
        {
            var employee = new Employee
            {
                PersonalId = personalId.Text,
                PositionId = position.Value?.Id ?? 0,
                FirstName = firstName.Text,
                LastName = lastName.Text,
                BirthDate = DateOnly.FromDateTime(birthDate.Value),
                MaritalStatus = maritalStatus.Value,
                Phone = phone.Text,
                Email = email.Text,
                Address = address.Text,
                Salary = salary.Value,
            };
            return existing is null ? employees.CreateAsync(employee) : employees.UpdateAsync(employee);
        };

        if (dialog.ShowDialog(this) == DialogResult.OK)
        {
            Shell.Toast(L.T("Toast.Saved"));
            await ReloadAsync();
        }
    }

    private async Task DeleteAsync()
    {
        if (Selected is not { } employee || !Dialogs.Confirm(this, L.T("Confirm.Delete", employee.FullName)))
        {
            return;
        }

        var result = await employees.DeleteAsync(employee.PersonalId);
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
