using ModernTechnics.App.Controls;
using ModernTechnics.App.Forms;
using ModernTechnics.App.Localization;
using ModernTechnics.App.Ui;
using ModernTechnics.Core.Common;
using ModernTechnics.Core.Domain;
using ModernTechnics.Core.Services;

namespace ModernTechnics.App.Pages;

internal sealed class ApplicationsPage(IJobApplicationService applications, IEmployeeService employees, IShell shell)
    : ListPage<JobApplication>(shell)
{
    protected override Func<Task>? DefaultAction => ViewAsync;

    protected override IReadOnlyList<GridColumn<JobApplication>> DefineColumns() =>
    [
        new(L.Field(nameof(JobApplication.FullName)), a => a.FullName, 130),
        new(L.Field(nameof(JobApplication.PositionId)), a => a.Position?.Name, 130),
        new(L.Field(nameof(JobApplication.Phone)), a => a.Phone, 110),
        new(L.Field(nameof(JobApplication.BirthDate)), a => a.BirthDate, 80, Format: "d"),
        new(L.T("Field.SubmittedAt"), a => a.SubmittedAtUtc.ToLocalTime(), 130, Format: "g"),
        new(L.Field(nameof(JobApplication.Status)), a => L.Enum(a.Status), 80, Color: a => StatusColor(a.Status), Emphasis: true),
    ];

    protected override void DefineActions()
    {
        AddAction(L.T("Applications.View"), Glyphs.View, ViewAsync, needsSelection: true);
        AddAction(L.T("Applications.SetStatus"), Glyphs.Flag, SetStatusAsync, needsSelection: true);
        AddAction(L.T("Action.Delete"), Glyphs.Delete, DeleteAsync, ButtonKind.Danger, needsSelection: true);
        AddAction(L.T("Applications.Add"), Glyphs.Add, AddAsync, ButtonKind.Primary);
    }

    protected override Task<IReadOnlyList<JobApplication>> FetchAsync(string? search) => applications.ListAsync(search);

    private static Color? StatusColor(ApplicationStatus status) => status switch
    {
        ApplicationStatus.New => Theme.Info,
        ApplicationStatus.Interview => Theme.Warning,
        ApplicationStatus.Hired => Theme.Success,
        _ => Theme.TextMuted,
    };

    private async Task AddAsync()
    {
        var positions = await employees.ListPositionsAsync();

        using var dialog = new FormDialog(L.T("Applications.Add"), columns: 2);
        var firstName = dialog.AddText(nameof(JobApplication.FirstName));
        var lastName = dialog.AddText(nameof(JobApplication.LastName));
        var birthDate = dialog.AddDate(nameof(JobApplication.BirthDate), DateOnly.FromDateTime(DateTime.Today.AddYears(-25)));
        var phone = dialog.AddText(nameof(JobApplication.Phone), "+995 ");
        var position = dialog.AddChoice(nameof(JobApplication.PositionId), positions, p => p.Name);
        var letter = dialog.AddMultiline(nameof(JobApplication.MotivationLetter));

        dialog.Submit = () => applications.SubmitAsync(new JobApplication
        {
            FirstName = firstName.Text,
            LastName = lastName.Text,
            BirthDate = DateOnly.FromDateTime(birthDate.Value),
            Phone = phone.Text,
            PositionId = position.Value?.Id ?? 0,
            MotivationLetter = letter.Text,
        });

        if (dialog.ShowDialog(this) == DialogResult.OK)
        {
            Shell.Toast(L.T("Toast.Saved"));
            await ReloadAsync();
        }
    }

    private Task ViewAsync()
    {
        if (Selected is not { } application)
        {
            return Task.CompletedTask;
        }

        using var dialog = new FormDialog(application.FullName, columns: 2)
        {
            Description = L.T(
                "Applications.Summary", application.Position?.Name, L.Enum(application.Status),
                L.DateTime(application.SubmittedAtUtc), application.Phone),
        };
        dialog.AddMultiline(nameof(JobApplication.MotivationLetter), application.MotivationLetter, readOnly: true, height: 200);
        dialog.MakeReadOnly();
        dialog.ShowDialog(this);
        return Task.CompletedTask;
    }

    private async Task SetStatusAsync()
    {
        if (Selected is not { } application)
        {
            return;
        }

        using var dialog = new FormDialog(L.T("Applications.SetStatus"))
        {
            Description = L.T("Applications.StatusFor", application.FullName, application.Position?.Name),
        };
        var status = dialog.AddChoice(
            nameof(JobApplication.Status), Enum.GetValues<ApplicationStatus>(), L.Enum, s => s == application.Status);
        dialog.Submit = () => applications.SetStatusAsync(application.Id, status.Value);

        if (dialog.ShowDialog(this) == DialogResult.OK)
        {
            Shell.Toast(L.T("Toast.Saved"));
            await ReloadAsync();
        }
    }

    private async Task DeleteAsync()
    {
        if (Selected is not { } application || !Dialogs.Confirm(this, L.T("Confirm.Delete", application.FullName)))
        {
            return;
        }

        Result result = await applications.DeleteAsync(application.Id);
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
