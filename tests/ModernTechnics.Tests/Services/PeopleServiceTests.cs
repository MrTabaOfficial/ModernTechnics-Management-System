using ModernTechnics.Core.Common;
using ModernTechnics.Core.Domain;
using ModernTechnics.Tests.Support;

namespace ModernTechnics.Tests.Services;

public class PeopleServiceTests
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private static Employee NewEmployee(int positionId) => new()
    {
        PersonalId = "01099912345",
        FirstName = " Vakho ",
        LastName = "Tsereteli",
        BirthDate = new DateOnly(1995, 6, 1),
        Address = "1 Freedom Sq, Tbilisi",
        Phone = "+995 599 00 11 22",
        Email = "Vakho.Tsereteli@ModernTechnics.ge",
        MaritalStatus = MaritalStatus.Single,
        PositionId = positionId,
        Salary = 2100m,
    };

    [Fact]
    public async Task Created_employee_is_listed_with_normalised_fields_and_position()
    {
        await using var db = await TestDatabase.CreateAsync();
        var position = (await db.Employees.ListPositionsAsync(Ct))[0];

        ResultAssert.Succeeded(await db.Employees.CreateAsync(NewEmployee(position.Id), Ct));

        var saved = (await db.Employees.ListAsync("01099912345", Ct)).Single();
        Assert.Equal("Vakho", saved.FirstName);
        Assert.Equal("vakho.tsereteli@moderntechnics.ge", saved.Email);
        Assert.Equal(position.Name, saved.Position!.Name);
        Assert.Equal(2100m, saved.Salary);
    }

    [Fact]
    public async Task Create_rejects_duplicate_personal_id_and_email()
    {
        await using var db = await TestDatabase.CreateAsync();
        var existing = (await db.Employees.ListAsync(ct: Ct))[0];

        var sameId = NewEmployee(existing.PositionId);
        sameId.PersonalId = existing.PersonalId;
        ResultAssert.Failed(await db.Employees.CreateAsync(sameId, Ct), ErrorCodes.Duplicate, nameof(Employee.PersonalId));

        var sameEmail = NewEmployee(existing.PositionId);
        sameEmail.Email = existing.Email;
        ResultAssert.Failed(await db.Employees.CreateAsync(sameEmail, Ct), ErrorCodes.Duplicate, nameof(Employee.Email));
    }

    [Fact]
    public async Task Create_rejects_minors_and_unknown_positions()
    {
        await using var db = await TestDatabase.CreateAsync();

        var minor = NewEmployee(1);
        minor.BirthDate = new DateOnly(2010, 1, 1);
        ResultAssert.Failed(await db.Employees.CreateAsync(minor, Ct), ErrorCodes.InvalidBirthDate);

        ResultAssert.Failed(
            await db.Employees.CreateAsync(NewEmployee(positionId: 9_999), Ct),
            ErrorCodes.NotFound, nameof(Employee.PositionId));
    }

    [Fact]
    public async Task Update_changes_details_but_keeps_own_email_valid()
    {
        await using var db = await TestDatabase.CreateAsync();
        var employee = (await db.Employees.ListAsync("Kapanadze", Ct)).Single();

        employee.Address = "New address 5, Tbilisi";
        employee.MaritalStatus = MaritalStatus.Divorced;
        ResultAssert.Succeeded(await db.Employees.UpdateAsync(employee, Ct));

        var reloaded = (await db.Employees.ListAsync("Kapanadze", Ct)).Single();
        Assert.Equal("New address 5, Tbilisi", reloaded.Address);
        Assert.Equal(MaritalStatus.Divorced, reloaded.MaritalStatus);
    }

    [Fact]
    public async Task Update_of_a_missing_employee_reports_not_found()
    {
        await using var db = await TestDatabase.CreateAsync();

        ResultAssert.Failed(await db.Employees.UpdateAsync(NewEmployee(1), Ct), ErrorCodes.NotFound);
    }

    [Fact]
    public async Task Set_salary_updates_only_the_salary()
    {
        await using var db = await TestDatabase.CreateAsync();
        var employee = (await db.Employees.ListAsync("Lomidze", Ct)).Single();

        ResultAssert.Succeeded(await db.Employees.SetSalaryAsync(employee.PersonalId, 2500.50m, Ct));
        ResultAssert.Failed(await db.Employees.SetSalaryAsync(employee.PersonalId, -1m, Ct), ErrorCodes.MustNotBeNegative);

        var reloaded = (await db.Employees.ListAsync("Lomidze", Ct)).Single();
        Assert.Equal(2500.50m, reloaded.Salary);
        Assert.Equal(employee.Email, reloaded.Email);
    }

    [Fact]
    public async Task Delete_removes_the_employee()
    {
        await using var db = await TestDatabase.CreateAsync();
        var before = await db.Employees.ListAsync(ct: Ct);

        ResultAssert.Succeeded(await db.Employees.DeleteAsync(before[0].PersonalId, Ct));
        ResultAssert.Failed(await db.Employees.DeleteAsync(before[0].PersonalId, Ct), ErrorCodes.NotFound);

        Assert.Equal(before.Count - 1, (await db.Employees.ListAsync(ct: Ct)).Count);
    }

    [Theory]
    [InlineData("beridze", 1)]
    [InlineData("Sales Consultant", 3)]
    [InlineData("%", 0)]
    [InlineData("_", 0)]
    [InlineData("'; DROP TABLE Employees; --", 0)]
    public async Task Search_matches_text_and_treats_wildcards_literally(string term, int expected)
    {
        await using var db = await TestDatabase.CreateAsync();

        Assert.Equal(expected, (await db.Employees.ListAsync(term, Ct)).Count);
        Assert.Equal(10, (await db.Employees.ListAsync(ct: Ct)).Count);
    }

    [Fact]
    public async Task Deleting_a_customer_keeps_their_orders_as_anonymous_sales()
    {
        await using var db = await TestDatabase.CreateAsync();
        var customer = (await db.Customers.ListAsync("Maisuradze", Ct)).Single();
        var ordersBefore = await db.Sales.ListOrdersAsync(ct: Ct);
        Assert.Contains(ordersBefore, o => o.CustomerPersonalId == customer.PersonalId);

        ResultAssert.Succeeded(await db.Customers.DeleteAsync(customer.PersonalId, Ct));

        var ordersAfter = await db.Sales.ListOrdersAsync(ct: Ct);
        Assert.Equal(ordersBefore.Count, ordersAfter.Count);
        Assert.DoesNotContain(ordersAfter, o => o.CustomerPersonalId == customer.PersonalId);
    }

    [Fact]
    public async Task Customer_create_validates_and_rejects_duplicates()
    {
        await using var db = await TestDatabase.CreateAsync();
        var customer = new Customer
        {
            PersonalId = "01077700011",
            FirstName = "Dato",
            LastName = "Kiknadze",
            BirthDate = new DateOnly(2001, 2, 3),
            Phone = "+995 555 12 12 12",
        };

        ResultAssert.Succeeded(await db.Customers.CreateAsync(customer, Ct));
        ResultAssert.Failed(await db.Customers.CreateAsync(customer, Ct), ErrorCodes.Duplicate, nameof(Customer.PersonalId));

        customer.PersonalId = "bad";
        ResultAssert.Failed(await db.Customers.CreateAsync(customer, Ct), ErrorCodes.InvalidPersonalId);
    }

    [Fact]
    public async Task Submitted_application_starts_as_new_and_can_be_progressed()
    {
        await using var db = await TestDatabase.CreateAsync();
        var position = (await db.Employees.ListPositionsAsync(Ct))[0];
        var application = new JobApplication
        {
            FirstName = "Gvantsa",
            LastName = "Bakradze",
            BirthDate = new DateOnly(1999, 3, 3),
            Phone = "+995 577 31 31 31",
            PositionId = position.Id,
            MotivationLetter = "Hello!",
            Status = ApplicationStatus.Hired,
        };

        ResultAssert.Succeeded(await db.Applications.SubmitAsync(application, Ct));

        var saved = (await db.Applications.ListAsync("Bakradze", Ct)).Single();
        Assert.Equal(ApplicationStatus.New, saved.Status);
        Assert.Equal(db.Clock.Now.UtcDateTime, saved.SubmittedAtUtc);
        Assert.Equal(saved.Id, (await db.Applications.ListAsync(ct: Ct))[0].Id);

        ResultAssert.Succeeded(await db.Applications.SetStatusAsync(saved.Id, ApplicationStatus.Interview, Ct));
        Assert.Equal(ApplicationStatus.Interview, (await db.Applications.ListAsync("Bakradze", Ct)).Single().Status);
    }
}
