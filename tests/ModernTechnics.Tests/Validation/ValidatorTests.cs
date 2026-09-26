using ModernTechnics.Core.Common;
using ModernTechnics.Core.Domain;
using ModernTechnics.Core.Validation;
using ModernTechnics.Tests.Support;

namespace ModernTechnics.Tests.Validation;

public class ValidatorTests
{
    private static readonly DateOnly Today = new(2026, 10, 7);

    [Theory]
    [InlineData("01024058311", true)]
    [InlineData("0102405831", false)]
    [InlineData("010240583112", false)]
    [InlineData("0102405831a", false)]
    [InlineData("", false)]
    public void Personal_id_must_be_eleven_digits(string value, bool valid)
    {
        Assert.Equal(valid, new Validator().PersonalId(value, "Id").ToResult().IsSuccess);
    }

    [Theory]
    [InlineData("nino@moderntechnics.ge", true)]
    [InlineData("nino.k+shop@mail.example.com", true)]
    [InlineData("nino@moderntechnics", false)]
    [InlineData("nino moderntechnics.ge", false)]
    [InlineData("@moderntechnics.ge", false)]
    public void Email_must_look_like_an_address(string value, bool valid)
    {
        Assert.Equal(valid, new Validator().Email(value, "Email").ToResult().IsSuccess);
    }

    [Theory]
    [InlineData("Demo1234", true)]
    [InlineData("Short1", false)]
    [InlineData("onlyletters", false)]
    [InlineData("12345678", false)]
    public void Password_needs_length_a_letter_and_a_digit(string value, bool valid)
    {
        Assert.Equal(valid, new Validator().Password(value, "Password").ToResult().IsSuccess);
    }

    [Theory]
    [InlineData("+995 599 10 20 30", true)]
    [InlineData("(032) 2-11-22-33", true)]
    [InlineData("12345", false)]
    [InlineData("call me", false)]
    public void Phone_accepts_common_formats(string value, bool valid)
    {
        Assert.Equal(valid, new Validator().Phone(value, "Phone").ToResult().IsSuccess);
    }

    [Fact]
    public void Birth_date_enforces_the_minimum_age_to_the_day()
    {
        var eighteenToday = Today.AddYears(-18);

        Assert.True(new Validator().BirthDate(eighteenToday, "BirthDate", Today, minAge: 18).ToResult().IsSuccess);
        ResultAssert.Failed(
            new Validator().BirthDate(eighteenToday.AddDays(1), "BirthDate", Today, minAge: 18).ToResult(),
            ErrorCodes.InvalidBirthDate);
    }

    [Fact]
    public void Too_long_error_reports_the_limit()
    {
        var result = new Validator().Required(new string('x', 11), "Name", maxLength: 10).ToResult();

        var error = Assert.Single(result.Errors);
        Assert.Equal(ErrorCodes.TooLong, error.Code);
        Assert.Equal(10, error.Argument);
    }

    [Fact]
    public void Employee_validation_reports_every_broken_field_at_once()
    {
        var employee = new Employee { PersonalId = "123", Email = "nope", Salary = -1 };

        var result = EntityValidators.Validate(employee, Today);

        string[] expected =
        [
            nameof(Employee.PersonalId), nameof(Employee.FirstName), nameof(Employee.LastName),
            nameof(Employee.BirthDate), nameof(Employee.Address), nameof(Employee.Phone),
            nameof(Employee.Email), nameof(Employee.PositionId), nameof(Employee.Salary),
        ];
        Assert.Equal(expected.Order(), result.Errors.Select(e => e.Field!).Order());
    }

    [Fact]
    public void Product_release_year_cannot_be_far_in_the_future()
    {
        var product = new Product { Name = "Phone", Manufacturer = "Acme", ReleaseYear = 2030 };

        ResultAssert.Failed(EntityValidators.Validate(product, 2026), ErrorCodes.InvalidYear, nameof(Product.ReleaseYear));
    }
}
