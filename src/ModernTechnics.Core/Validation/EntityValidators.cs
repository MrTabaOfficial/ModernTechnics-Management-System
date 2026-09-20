using ModernTechnics.Core.Common;
using ModernTechnics.Core.Domain;

namespace ModernTechnics.Core.Validation;

/// <summary>Validation rules for each entity, shared by the services and the tests.</summary>
public static class EntityValidators
{
    public const int MinimumEmployeeAge = 18;

    public static Result Validate(Employee employee, DateOnly today) => new Validator()
        .PersonalId(employee.PersonalId, nameof(Employee.PersonalId))
        .Required(employee.FirstName, nameof(Employee.FirstName), 100)
        .Required(employee.LastName, nameof(Employee.LastName), 100)
        .BirthDate(employee.BirthDate, nameof(Employee.BirthDate), today, MinimumEmployeeAge)
        .Required(employee.Address, nameof(Employee.Address))
        .Phone(employee.Phone, nameof(Employee.Phone))
        .Email(employee.Email, nameof(Employee.Email))
        .Positive(employee.PositionId, nameof(Employee.PositionId))
        .NotNegative(employee.Salary, nameof(Employee.Salary))
        .ToResult();

    public static Result Validate(Customer customer, DateOnly today) => new Validator()
        .PersonalId(customer.PersonalId, nameof(Customer.PersonalId))
        .Required(customer.FirstName, nameof(Customer.FirstName), 100)
        .Required(customer.LastName, nameof(Customer.LastName), 100)
        .BirthDate(customer.BirthDate, nameof(Customer.BirthDate), today)
        .Optional(customer.Address, nameof(Customer.Address))
        .Phone(customer.Phone, nameof(Customer.Phone))
        .ToResult();

    public static Result Validate(Product product, int currentYear) => new Validator()
        .Required(product.Name, nameof(Product.Name), 100)
        .Required(product.Manufacturer, nameof(Product.Manufacturer), 100)
        .Year(product.ReleaseYear, nameof(Product.ReleaseYear), currentYear)
        .NotNegative(product.Price, nameof(Product.Price))
        .NotNegative(product.WarehouseStock, nameof(Product.WarehouseStock))
        .NotNegative(product.StoreStock, nameof(Product.StoreStock))
        .ToResult();

    public static Result Validate(JobApplication application, DateOnly today) => new Validator()
        .Required(application.FirstName, nameof(JobApplication.FirstName), 100)
        .Required(application.LastName, nameof(JobApplication.LastName), 100)
        .BirthDate(application.BirthDate, nameof(JobApplication.BirthDate), today, MinimumEmployeeAge)
        .Phone(application.Phone, nameof(JobApplication.Phone))
        .Positive(application.PositionId, nameof(JobApplication.PositionId))
        .Optional(application.MotivationLetter, nameof(JobApplication.MotivationLetter), 4000)
        .ToResult();
}
