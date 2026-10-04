using ModernTechnics.App.Controls;
using ModernTechnics.App.Forms;
using ModernTechnics.App.Localization;
using ModernTechnics.App.Ui;
using ModernTechnics.Core.Domain;
using ModernTechnics.Core.Services;

namespace ModernTechnics.App.Pages;

internal sealed class PayrollPage(IEmployeeService employees, IShell shell) : ListPage<Employee>(shell)
{
    protected override Func<Task>? DefaultAction => ChangeSalaryAsync;

    protected override IReadOnlyList<GridColumn<Employee>> DefineColumns() =>
    [
        new(L.Field(nameof(Employee.PersonalId)), e => e.PersonalId, 90),
        new(L.Field(nameof(Employee.FullName)), e => e.FullName, 140),
        new(L.Field(nameof(Employee.PositionId)), e => e.Position?.Name, 140),
        new(L.Field(nameof(Employee.Salary)), e => e.Salary, 90, AlignRight: true, Format: "N2"),
        new(L.T("Payroll.Annual"), e => e.Salary * 12, 90, AlignRight: true, Format: "N2"),
    ];

    protected override void DefineActions() =>
        AddAction(L.T("Payroll.ChangeSalary"), Glyphs.Money, ChangeSalaryAsync, ButtonKind.Primary, needsSelection: true);

    protected override Task<IReadOnlyList<Employee>> FetchAsync(string? search) => employees.ListAsync(search);

    protected override string FooterText(IReadOnlyList<Employee> items) =>
        L.T("Payroll.Footer", items.Count, L.Money(items.Sum(e => e.Salary)));

    private async Task ChangeSalaryAsync()
    {
        if (Selected is not { } employee)
        {
            return;
        }

        using var dialog = new FormDialog(L.T("Payroll.ChangeSalary"))
        {
            Description = L.T("Payroll.Current", employee.FullName, employee.Position?.Name, L.Money(employee.Salary)),
        };
        var salary = dialog.AddNumber(nameof(Employee.Salary), employee.Salary, 1_000_000, decimals: 2);
        dialog.Submit = () => employees.SetSalaryAsync(employee.PersonalId, salary.Value);

        if (dialog.ShowDialog(this) == DialogResult.OK)
        {
            Shell.Toast(L.T("Toast.Saved"));
            await ReloadAsync();
        }
    }
}
